using Cartiva.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Cartiva.Domain;
using Cartiva.Domain.ViewModels;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.PaymentService;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Cartiva.Shared;
using Cartiva.Application.Abstractions;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
using Cartiva.Application.Services;

[Area("Customer")]
[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly StripeSettings _stripeSettings;
    private readonly Cartiva.Infrastructure.QrCodeServices.IQrCodeService _qrCodeService;
    private readonly ILogger<OrderController> _logger;
    private readonly IInvoiceService _invoiceService;
    private readonly IOrderService _orderService;
    private readonly IShipmentService _shipmentService;
    private readonly ICartService _cartService;
    private readonly IPaymentService _paymentService;

    public OrderController(
        ApplicationDbContext db,
        IOptions<StripeSettings> stripeSettings,
        Cartiva.Infrastructure.QrCodeServices.IQrCodeService qrCodeService,
        ILogger<OrderController> logger,
        IInvoiceService invoiceService,
        IOrderService orderService,
        IShipmentService shipmentService,
        ICartService cartService,
        IPaymentService paymentService)
    {
        _db = db;
        _stripeSettings = stripeSettings.Value;
        _qrCodeService = qrCodeService;
        _logger = logger;
        _invoiceService = invoiceService;
        _orderService = orderService;
        _shipmentService = shipmentService;
        _cartService = cartService;
        _paymentService = paymentService;
    }

    // =============================
    // CHECKOUT PAGE
    // =============================
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var checkoutResult = await _orderService.PrepareCheckoutAsync(userId);

        if (!checkoutResult.Success)
        {
            TempData["Error"] = checkoutResult.ErrorMessage;
            return RedirectToAction("Index", "Cart");
        }

        var vm = new CheckoutVM
        {
            OrderHeader = checkoutResult.OrderHeader!,
            ShoppingCartList = checkoutResult.CartItems,
            OrderTotal = checkoutResult.OrderTotal
        };

        ViewBag.PromotionDiscount = new
        {
            TotalDiscount = checkoutResult.TotalDiscount,
            AppliedPromotions = checkoutResult.AppliedPromotions
        };
        ViewBag.Subtotal = checkoutResult.Subtotal;
        ViewBag.SubtotalExVat = checkoutResult.SubtotalExVat;
        ViewBag.TotalVat = checkoutResult.TotalVat;

        return View(vm);
    }

    // =============================
    // CONFIRM ORDER (POST) – Displays the confirmation page
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOrder(CheckoutVM model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var checkoutResult = await _orderService.PrepareCheckoutAsync(userId);

        if (!checkoutResult.Success)
        {
            return RedirectToAction("Index", "Cart");
        }

        model.ShoppingCartList = checkoutResult.CartItems;
        model.OrderTotal = checkoutResult.OrderTotal;

        ViewBag.PromotionDiscount = new
        {
            TotalDiscount = checkoutResult.TotalDiscount,
            AppliedPromotions = checkoutResult.AppliedPromotions
        };
        ViewBag.Subtotal = checkoutResult.Subtotal;

        // Check company status for warning
        var companyStatus = await _orderService.CheckCompanyStatusAsync(userId);
        if (companyStatus.IsCompanyUser && !companyStatus.IsCompanyActive)
        {
            TempData["Warning"] = "Your company account is inactive. Payment must be completed immediately (upfront).";
            TempData["CompanyInactive"] = true;
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _orderService.PlaceOrderAsync(userId, model.OrderHeader, payNow);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Checkout");
        }

        var orderId = result.OrderId!.Value;

        // Create shipment for company orders
        if (result.IsCompanyOrder)
        {
            await _shipmentService.CreateShipmentForOrderAsync(orderId);

            // Generate invoice for deferred payment
            if (result.IsDeferredPayment)
            {
                try
                {
                    var invoice = await _invoiceService.GenerateInvoiceFromOrderAsync(orderId);
                    _logger.LogInformation("Generated invoice {InvoiceNumber} for company order {OrderId}",
                        invoice.InvoiceNumber, orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate invoice for order {OrderId}", orderId);
                }
            }

            // Order confirmation email is now handled by OrderService via notification system

            if (result.RequiresPayment)
            {
                return RedirectToAction("Payment", new { orderId });
            }
            else
            {
                return RedirectToAction("Receipt", new { id = orderId });
            }
        }
        else
        {
            // Regular customer – always go to payment
            return RedirectToAction("Payment", new { orderId });
        }
    }

    // =============================
    // REMAINING METHODS (Payment, Receipt, History, etc.)
    // These are kept as-is since they involve Stripe integration
    // =============================

    // GET: /Customer/Order/Payment
    public async Task<IActionResult> Payment(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound();
        }

        // Authorization: own order, or same-company colleague paying a deferred invoice
        if (order.ApplicationUserId != userId)
        {
            if (User.IsInRole(SD.Role_Company)
                && order.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Deferred)
            {
                var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser?.CompanyId == null || order.ApplicationUser?.CompanyId != currentUser.CompanyId)
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }
        }

        // Create payment intent using abstraction layer
        var paymentResult = await _paymentService.CreatePaymentIntentAsync(
            orderId: order.Id,
            amount: order.OrderTotal,
            currency: "NOK",
            userId: userId,
            description: $"Order #{order.Id}"
        );

        if (!paymentResult.Success)
        {
            _logger.LogError("Failed to create payment intent for order {OrderId}: {Error}",
                order.Id, paymentResult.ErrorMessage);
            TempData["Error"] = "Failed to initialize payment. Please try again.";
            return RedirectToAction("Details", new { orderId = order.Id });
        }

        // Store PaymentIntent ID on order for reconciliation
        order.PaymentIntentId = paymentResult.PaymentIntentId;
        await _db.SaveChangesAsync();

        var vm = new PaymentVM
        {
            Order = order,
            ClientSecret = paymentResult.ClientSecret!,
            PublishableKey = _stripeSettings.PublishableKey,
            PaymentIntentId = paymentResult.PaymentIntentId!
        };

        _logger.LogInformation("Payment intent {PaymentIntentId} created for order {OrderId} via {Provider}",
            paymentResult.PaymentIntentId, order.Id, _paymentService.GetProviderName());

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
            .Include(o => o.ApplicationUser)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            _logger.LogWarning($"Order not found for id {orderId}");
            return NotFound();
        }

        // Authorization: own order or same-company colleague
        if (order.ApplicationUserId != userId)
        {
            if (User.IsInRole(SD.Role_Company))
            {
                var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser?.CompanyId == null || order.ApplicationUser?.CompanyId != currentUser.CompanyId)
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }
        }

        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning("No payment_intent provided for order {OrderId}", orderId);
            TempData["Error"] = "Payment confirmation missing. Please contact support.";
            return RedirectToAction("Details", new { id = orderId });
        }

        try
        {
            // Use payment service abstraction to verify payment
            var paymentStatus = await _paymentService.GetPaymentStatusAsync(paymentIntentId);
            _logger.LogInformation("Payment status for order {OrderId}: {Status}", orderId, paymentStatus.Status);

            if (paymentStatus.Status == PaymentIntentStatus.Succeeded)
            {
                // Update payment status
                order.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Approved;
                order.PaymentIntentId = paymentIntentId;
                order.PaymentDate = DateTime.Now;

                // Create a shipment record
                var shipment = new Shipment
                {
                    OrderHeaderId = order.Id,
                    ShipmentStatus = Cartiva.Domain.Enums.ShipmentStatus.PendingApproval
                };
                _db.Shipments.Add(shipment);

                // Update order status to AwaitingShipmentApproval
                order.OrderStatus = Cartiva.Domain.Enums.OrderStatus.AwaitingShipmentApproval;

                await _db.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} updated to AwaitingShipmentApproval", orderId);

                // Record payment against the invoice
                try
                {
                    var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(order.Id)
                        ?? await _invoiceService.GenerateInvoiceFromOrderAsync(order.Id);

                    if (invoice.Status != Cartiva.Domain.Enums.InvoiceStatus.Paid)
                    {
                        await _invoiceService.RecordPaymentAsync(
                            invoiceId: invoice.Id,
                            amount: order.OrderTotal,
                            paymentMethod: Cartiva.Domain.PaymentMethod.Card,
                            transactionId: paymentIntentId,
                            paymentReference: paymentIntentId,
                            registeredBy: userId);

                        await _invoiceService.RefreshInvoiceStatusAsync(invoice.Id);
                        _logger.LogInformation("Recorded payment for invoice {InvoiceId}", invoice.Id);
                    }
                }
                catch (Exception invEx)
                {
                    _logger.LogError(invEx, "Failed to record invoice payment for order {OrderId}", order.Id);
                }

                TempData["Success"] = "Payment successful! Your order is being prepared for shipment.";
                return RedirectToAction("ShipmentPending", new { id = order.Id });
            }
            else
            {
                _logger.LogWarning("Payment not succeeded for order {OrderId}, Status: {Status}", 
                    orderId, paymentStatus.Status);
                TempData["Error"] = $"Payment not completed (status: {paymentStatus.Status}). Please try again.";
                return RedirectToAction("Payment", new { orderId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for order {OrderId}", orderId);
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

        // For company users, fetch colleague orders
        if (User.IsInRole(SD.Role_Company))
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.CompanyId != null)
            {
                var companyOrders = await _db.OrderHeaders
                    .Where(o => o.ApplicationUser!.CompanyId == currentUser.CompanyId
                                && o.ApplicationUserId != userId)
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(pv => pv.SizeValue)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                var company = await _db.Companies.FindAsync(currentUser.CompanyId);
                ViewBag.CompanyOrders = companyOrders;
                ViewBag.CompanyName = company?.Name;
            }
        }

        return View(orders);
    }

    // =============================
    // ORDER DETAILS
    // =============================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var orderHeader = await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
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

        // Authorization: own order, admin, or same-company colleague
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (orderHeader.ApplicationUserId != userId && !User.IsInRole(SD.Role_Admin))
        {
            if (User.IsInRole(SD.Role_Company))
            {
                var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var orderOwner = orderHeader.ApplicationUser;
                if (currentUser?.CompanyId == null || orderOwner?.CompanyId != currentUser.CompanyId)
                {
                    return Forbid();
                }
                ViewBag.IsColleagueOrder = true;
                ViewBag.OrderOwnerName = orderOwner?.Name ?? orderOwner?.Email;
            }
            else
            {
                return Forbid();
            }
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

        if (order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Pending && order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Approved && order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.AwaitingShipmentApproval)
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

        if (order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Pending && order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Approved && order.OrderStatus != Cartiva.Domain.Enums.OrderStatus.AwaitingShipmentApproval)
        {
            return Json(new
            {
                success = false,
                message = $"This order cannot be cancelled because it's already {order.OrderStatus}"
            });
        }

        var cancelResult = await _orderService.CancelOrderAsync(id, "Cancelled by customer");
        if (!cancelResult.Success)
        {
            return Json(new
            {
                success = false,
                message = cancelResult.Message
            });
        }

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
    [AllowAnonymous]
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
    [AllowAnonymous]
    public IActionResult TrackTest()
    {
        return Content("Track action is working!");
    }
}