using DataAccess;
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

    public OrderController(ApplicationDbContext db,
                           IOptions<StripeSettings> stripeSettings,
                           IQrCodeService qrCodeService,
                           ILogger<OrderController> logger)
    {
        _db = db;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        _qrCodeService = qrCodeService;
        _logger = logger;
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

        var user = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == userId);

        var vm = new CheckoutVM
        {
            OrderHeader = new OrderHeader
            {
                Name = user?.Name ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                StreetAddress = user?.StreetAddress,
                City = user?.City,
                State = user?.State,
                PostalCode = user?.PostalCode
            },
            ShoppingCartList = cartList,
            OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count)
        };

        return View(vm);
    }

    // =============================
    // CONFIRM ORDER (POST)
    // from Checkout form
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

        return View(model);
    }

    // =============================
    // PLACE ORDER
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutVM model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        // Validate stock before placing order
        foreach (var cart in cartList)
        {
            var variant = cart.ProductVariant;
            if (variant.Stock < cart.Count)
            {
                string sizeDisplay = variant.SizeValue != null
                    ? variant.SizeValue.DisplayText
                    : "No Size";

                TempData["Error"] = $"Not enough stock for {variant.Product?.Name} ({variant.Color}/{sizeDisplay}). Only {variant.Stock} left.";
                return RedirectToAction("Checkout");
            }
        }

        // Create OrderHeader
        model.OrderHeader.ApplicationUserId = userId;
        model.OrderHeader.OrderDate = DateTime.Now;
        model.OrderHeader.OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count);

        // Company vs Regular customer logic
        if (User.IsInRole(SD.Role_Company))
        {
            model.OrderHeader.PaymentStatus = SD.PaymentStatusDeferred;
            model.OrderHeader.OrderStatus = SD.StatusApproved;
            model.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }
        else
        {
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

        // Redirect based on role
        if (User.IsInRole(SD.Role_Company))
        {
            return RedirectToAction("Receipt", new { id = model.OrderHeader.Id });
        }
        else
        {
            // Create Stripe Payment Intent for regular customers
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
            Amount = (long)(order.OrderTotal * 100), // Convert to cents
            Currency = "nok", // Norwegian Krone
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
    // CONFIRM PAYMENT (Stripe Return) - FIXED
    // =============================
    [HttpGet] // Important: must be GET
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
                order.PaymentStatus = SD.PaymentStatusApproved;
                order.OrderStatus = SD.StatusApproved;
                order.PaymentIntentId = paymentIntentId;
                order.PaymentDate = DateTime.Now;
                await _db.SaveChangesAsync();
                _logger.LogInformation($"Order {orderId} updated to Approved");

                TempData["Success"] = "Payment successful! Your order has been confirmed.";
                return RedirectToAction("Receipt", new { id = order.Id });
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
            .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != SD.StatusPending && order.OrderStatus != SD.StatusApproved)
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
            .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != SD.StatusPending && order.OrderStatus != SD.StatusApproved)
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

    // =============================
    // STRIPE WEBHOOK (Commented out - for future use)
    // =============================
    /*
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        // ... webhook code remains unchanged
    }
    */
}