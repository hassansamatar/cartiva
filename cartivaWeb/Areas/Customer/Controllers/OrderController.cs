using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Models.Interfaces;
using Models.ViewModels;
using MyUtility;
using Stripe;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

[Area("Customer")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly StripeSettings _stripeSettings;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<OrderController> _logger;
    private readonly IEmailSender _emailSender;

    public OrderController(ApplicationDbContext db,
                           IOptions<StripeSettings> stripeSettings,
                           IQrCodeService qrCodeService,
                           ILogger<OrderController> logger,
                           IEmailSender emailSender)
    {
        _db = db;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        _qrCodeService = qrCodeService;
        _logger = logger;
        _emailSender = emailSender;
    }

    // =============================
    // CHECKOUT PAGE
    // =============================
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var cartList = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv.SizeSystem)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartList.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var vm = new CheckoutVM
        {
            OrderHeader = new OrderHeader
            {
                Name = user?.Name ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                StreetAddress = user?.StreetAddress,
                City = user?.City,
                State = user?.State,
                PostalCode = user?.PostalCode,
                Country = user?.Country ?? "Norway"
            },
            ShoppingCartList = cartList,
            OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count)
        };

        return View(vm);
    }

    // =============================
    // CONFIRM ORDER (POST) – Displays the confirmation page
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOrder(CheckoutVM model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var cartList = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv.SizeSystem)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartList.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        model.ShoppingCartList = cartList;
        model.OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count);

        // --- NEW: Show warning for inactive company accounts ---
        if (User.IsInRole(SD.Role_Company))
        {
            var user = await _db.Users.FindAsync(userId);
            if (user?.CompanyId != null)
            {
                var company = await _db.Companies.FindAsync(user.CompanyId);
                if (company != null && !company.IsActive)
                {
                    TempData["Warning"] = "Your company account is inactive. Payment must be completed immediately (upfront).";
                    TempData["CompanyInactive"] = true;
                }
            }
        }

        return View(model);
    }

    // =============================
    // PLACE ORDER – Creates the order and redirects
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutVM model, bool payNow = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Fetch the cart items
        var cartList = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartList.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // Validate stock
        foreach (var cart in cartList)
        {
            var variant = cart.ProductVariant;
            if (variant.Stock < cart.Count)
            {
                string sizeDisplay = variant.SizeValue != null ? variant.SizeValue.DisplayText : "No Size";
                TempData["Error"] = $"Not enough stock for {variant.Product?.Name} ({variant.Color}/{sizeDisplay}). Only {variant.Stock} left.";
                return RedirectToAction("Checkout");
            }
        }

        var user = await _db.Users.FindAsync(userId);

        // Create OrderHeader
        model.OrderHeader.ApplicationUserId = userId;
        model.OrderHeader.OrderDate = DateTime.Now;
        model.OrderHeader.OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count);
        model.OrderHeader.Country = user?.Country ?? "Norway";

        // Determine payment logic
        if (User.IsInRole(SD.Role_Company) && user?.CompanyId != null)
        {
            var company = await _db.Companies.FindAsync(user.CompanyId);

            if (company != null && !company.IsActive)
            {
                // Inactive company – force upfront payment
                model.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                model.OrderHeader.OrderStatus = SD.StatusPending;
                model.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now);

                // Flag for UI logic (ConfirmOrder page)
                TempData["Warning"] = "Your company account is inactive. Payment must be completed immediately.";
                TempData["CompanyInactive"] = true;
            }
            else
            {
                // Active company – allow deferred payment
                model.OrderHeader.PaymentStatus = SD.PaymentStatusDeferred;
                model.OrderHeader.OrderStatus = SD.StatusAwaitingShipmentApproval;
                model.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }
        }
        else
        {
            // Regular customer – payment required
            model.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            model.OrderHeader.OrderStatus = SD.StatusPending;
            model.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now);
        }

        _db.OrderHeaders.Add(model.OrderHeader);
        await _db.SaveChangesAsync();

        // Create OrderDetails and update stock
        var orderDetails = new List<OrderDetail>();
        foreach (var cart in cartList)
        {
            var orderDetail = new OrderDetail
            {
                OrderHeaderId = model.OrderHeader.Id,
                ProductVariantId = cart.ProductVariantId,
                Count = cart.Count,
                Price = cart.ProductVariant.Price
            };
            orderDetails.Add(orderDetail);

            // Update stock
            cart.ProductVariant.Stock -= cart.Count;
        }

        _db.OrderDetails.AddRange(orderDetails);
        await _db.SaveChangesAsync();

        // Clear cart
        _db.ShoppingCarts.RemoveRange(cartList);
        await _db.SaveChangesAsync();

        // Redirect based on role and payNow flag
        if (User.IsInRole(SD.Role_Company))
        {
            // Create shipment record for company orders
            var shipment = new Shipment
            {
                OrderHeaderId = model.OrderHeader.Id,
                ShipmentStatus = SD.ShipmentStatusPendingApproval
            };
            _db.Shipments.Add(shipment);
            await _db.SaveChangesAsync();

            // Send order confirmation email
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                var trackingUrl = Url.Action("Track", "Order", new { id = model.OrderHeader.Id, area = "Customer" }, Request.Scheme);
                var qrCodeBase64 = _qrCodeService.GenerateOrderQrCode(model.OrderHeader.Id);
                var subject = "Order Confirmation";
                var body = $@"
<h2>Thank you for your order!</h2>
<p>Your order <strong>#{model.OrderHeader.Id}</strong> has been confirmed.</p>
<p>We'll notify you when it ships.</p>
<p>You can track your order status at any time: <a href='{trackingUrl}'>Track Order</a></p>
<p>Or scan the QR code below with your phone:</p>
<img src='data:image/png;base64,{qrCodeBase64}' width='150' />
<p>Thank you for shopping with us!</p>
";
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }

            // Decide redirect based on company status
            if (payNow || model.OrderHeader.PaymentStatus != SD.PaymentStatusDeferred)
            {
                return RedirectToAction("Payment", new { orderId = model.OrderHeader.Id });
            }
            else
            {
                return RedirectToAction("Receipt", new { id = model.OrderHeader.Id });
            }
        }
        else
        {
            // Regular customer – always go to payment
            return RedirectToAction("Payment", new { orderId = model.OrderHeader.Id });
        }
    }

    // =============================
    // PAYMENT PAGE (Stripe)
    // =============================
    [HttpGet]
    public async Task<IActionResult> Payment(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        // Create Stripe PaymentIntent
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(order.OrderTotal * 100),
            Currency = "nok",
            PaymentMethodTypes = new List<string> { "card" },
            Metadata = new Dictionary<string, string>
            {
                { "orderId", order.Id.ToString() },
                { "userId", userId }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        var vm = new PaymentVM
        {
            Order = order,
            ClientSecret = paymentIntent.ClientSecret,
            PublishableKey = _stripeSettings.PublishableKey,
            PaymentIntentId = paymentIntent.Id
        };

        return View(vm);
    }

    // =============================
    // CONFIRM PAYMENT (Stripe Return)
    // =============================
    [HttpGet]
    public async Task<IActionResult> ConfirmPayment(int orderId, [FromQuery(Name = "payment_intent")] string paymentIntentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation($"ConfirmPayment called with orderId={orderId}, paymentIntentId={paymentIntentId}, userId={userId}");

        var order = await _db.OrderHeaders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);
        if (order == null)
        {
            _logger.LogWarning($"Order not found for id {orderId} and user {userId}");
            return NotFound();
        }

        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning($"No payment_intent provided for order {orderId}");
            TempData["Error"] = "Payment confirmation missing. Please contact support.";
            return RedirectToAction("Details", new { id = orderId });
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);
            _logger.LogInformation($"PaymentIntent status: {paymentIntent.Status}");

            if (paymentIntent.Status == "succeeded")
            {
                // Update payment status
                order.PaymentStatus = SD.PaymentStatusApproved;
                order.PaymentIntentId = paymentIntentId;
                order.PaymentDate = DateTime.Now;

                // Create a shipment record
                var shipment = new Shipment
                {
                    OrderHeaderId = order.Id,
                    ShipmentStatus = SD.ShipmentStatusPendingApproval
                };
                _db.Shipments.Add(shipment);

                // Update order status to AwaitingShipmentApproval
                order.OrderStatus = SD.StatusAwaitingShipmentApproval;

                await _db.SaveChangesAsync();
                _logger.LogInformation($"Order {orderId} updated to AwaitingShipmentApproval with pending shipment.");

                // Send order confirmation email for regular customers
                var user = await _db.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var trackingUrl = Url.Action("Track", "Order", new { id = order.Id, area = "Customer" }, Request.Scheme);
                    var qrCodeBase64 = _qrCodeService.GenerateOrderQrCode(order.Id);
                    var subject = "Order Confirmation";
                    var body = $@"
<h2>Thank you for your order!</h2>
<p>Your order <strong>#{order.Id}</strong> has been confirmed.</p>
<p>We'll notify you when it ships.</p>
<p>You can track your order status at any time: <a href='{trackingUrl}'>Track Order</a></p>
<p>Or scan the QR code below with your phone:</p>
<img src='data:image/png;base64,{qrCodeBase64}' width='150' />
<p>Thank you for shopping with us!</p>
";
                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                }

                TempData["Success"] = "Payment successful! Your order is being prepared for shipment.";
                return RedirectToAction("ShipmentPending", new { id = order.Id });
            }
            else
            {
                _logger.LogWarning($"Payment not succeeded: {paymentIntent.Status}");
                TempData["Error"] = $"Payment not completed (status: {paymentIntent.Status}). Please try again.";
                return RedirectToAction("Payment", new { orderId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error confirming payment for order {orderId}");
            TempData["Error"] = "Payment confirmation failed. Please contact support.";
            return RedirectToAction("Details", new { id = orderId });
        }
    }

    // =============================
    // SHIPMENT PENDING PAGE
    // =============================
    [HttpGet]
    public async Task<IActionResult> ShipmentPending(int id)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // =============================
    // ORDER RECEIPT
    // =============================
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var orderHeader = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orderHeader == null)
        {
            return NotFound();
        }

        return View(orderHeader);
    }

    // =============================
    // ORDER HISTORY
    // =============================
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await _db.OrderHeaders
            .Where(o => o.ApplicationUserId == userId)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(pv => pv.SizeValue)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // =============================
    // ORDER DETAILS
    // =============================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var orderHeader = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orderHeader == null)
        {
            return NotFound();
        }

        return View(orderHeader);
    }

    // =============================
    // CANCEL ORDER - GET
    // =============================
    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != SD.StatusPending && order.OrderStatus != SD.StatusApproved && order.OrderStatus != SD.StatusAwaitingShipmentApproval)
        {
            TempData["Error"] = "This order cannot be cancelled because it's already " + order.OrderStatus;
            return RedirectToAction("Details", new { id });
        }

        return View(order);
    }

    // =============================
    // CONFIRM CANCEL ORDER - POST
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != SD.StatusPending && order.OrderStatus != SD.StatusApproved && order.OrderStatus != SD.StatusAwaitingShipmentApproval)
        {
            return Json(new
            {
                success = false,
                message = $"This order cannot be cancelled because it's already {order.OrderStatus}"
            });
        }

        // If payment was made, issue refund via Stripe
        if (!string.IsNullOrEmpty(order.PaymentIntentId) && order.PaymentStatus == SD.PaymentStatusApproved)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = order.PaymentIntentId
                };
                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                if (refund.Status == "succeeded" || refund.Status == "pending")
                {
                    order.PaymentStatus = SD.PaymentStatusRefunded;
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error processing refund: " + ex.Message
                });
            }
        }

        order.OrderStatus = SD.StatusCancelled;

        var shipment = order.Shipments?.FirstOrDefault();
        if (shipment != null && shipment.ShipmentStatus != SD.ShipmentStatusCancelled)
        {
            shipment.ShipmentStatus = SD.ShipmentStatusCancelled;
        }

        foreach (var detail in order.OrderDetails)
        {
            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
            if (variant != null)
            {
                variant.Stock += detail.Count;
            }
        }

        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                message = "Order cancelled successfully. Stock has been restored."
            });
        }

        TempData["Success"] = "Order cancelled successfully. Stock has been restored.";
        return RedirectToAction("Details", new { id });
    }

    // =============================
    // QR CODE TRACKING PAGE
    // =============================
    [HttpGet]
    public async Task<IActionResult> Track(int id)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public IActionResult TrackTest()
    {
        return Content("Track action is working!");
    }
}