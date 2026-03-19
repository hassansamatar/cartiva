using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Models.ViewModels;
using MyUtility;
using Stripe;
using Stripe.V2.Core;
using System.Security.Claims;

[Area("Customer")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly StripeSettings _stripeSettings;

    public OrderController(ApplicationDbContext db, IOptions<StripeSettings> stripeSettings)
    {
        _db = db;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
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
    // CONFIRM PAYMENT (Stripe Return)
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(string paymentIntentId, int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);

        if (order == null)
            return NotFound();

        var service = new PaymentIntentService();
        var paymentIntent = await service.GetAsync(paymentIntentId);

        if (paymentIntent.Status == "succeeded")
        {
            // Update order status
            order.PaymentStatus = SD.PaymentStatusApproved;
            order.OrderStatus = SD.StatusApproved;
            order.PaymentIntentId = paymentIntentId;
            order.PaymentDate = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Payment successful! Your order has been confirmed.";
            return RedirectToAction("Receipt", new { id = order.Id });
        }
        else if (paymentIntent.Status == "requires_payment_method")
        {
            TempData["Error"] = "Payment failed. Please try again with a different payment method.";
            return RedirectToAction("Payment", new { orderId });
        }

        TempData["Error"] = "Payment processing error. Please contact support.";
        return RedirectToAction("Details", new { id = order.Id });
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
            .Include(o => o.OrderDetails)
            .Where(o => o.ApplicationUserId == userId)
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

        // Check if order can be cancelled
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

        // Check if order can be cancelled
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

        // Update order status
        order.OrderStatus = SD.StatusCancelled;

        // Restore stock for each item
        foreach (var detail in order.OrderDetails)
        {
            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
            if (variant != null)
            {
                variant.Stock += detail.Count;
            }
        }

        await _db.SaveChangesAsync();

        // If it's an AJAX request
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
    // STRIPE WEBHOOK (Commented out - for future use)
    // =============================
    /*
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret
            );

            // Handle the event
            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    if (paymentIntent?.Metadata != null && paymentIntent.Metadata.ContainsKey("orderId"))
                    {
                        var orderId = int.Parse(paymentIntent.Metadata["orderId"]);
                        var order = await _db.OrderHeaders.FindAsync(orderId);

                        if (order != null)
                        {
                            order.PaymentStatus = SD.PaymentStatusApproved;
                            order.OrderStatus = SD.StatusApproved;
                            order.PaymentIntentId = paymentIntent.Id;
                            order.PaymentDate = DateTime.Now;
                            await _db.SaveChangesAsync();

                            Console.WriteLine($"Webhook: Order {orderId} payment succeeded");
                        }
                    }
                    break;

                case Events.PaymentIntentPaymentFailed:
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    Console.WriteLine($"Webhook: Payment failed for intent {failedIntent?.Id}");
                    break;

                case Events.ChargeRefunded:
                    var charge = stripeEvent.Data.Object as Charge;
                    Console.WriteLine($"Webhook: Refund processed for charge {charge?.Id}");
                    break;
            }

            return Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine($"Stripe webhook error: {e.Message}");
            return BadRequest();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Webhook error: {e.Message}");
            return BadRequest();
        }
    }
    */
}