using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Security.Claims;
using MyUtility;

[Area("Customer")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;

    public OrderController(ApplicationDbContext db)
    {
        _db = db;
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
            return RedirectToAction("Payment", new { orderId = model.OrderHeader.Id });
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
    // PAYMENT PAGE
    // =============================
    [HttpGet]
    public async Task<IActionResult> Payment(int orderId)
    {
        var orderHeader = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (orderHeader == null)
        {
            return NotFound();
        }

        return View(orderHeader);
    }

    // =============================
    // PROCESS PAYMENT (Mock)
    // =============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(int orderId)
    {
        var orderHeader = await _db.OrderHeaders
            .FindAsync(orderId);

        if (orderHeader == null)
        {
            return NotFound();
        }

        orderHeader.PaymentStatus = SD.PaymentStatusApproved;
        orderHeader.OrderStatus = SD.StatusApproved;
        orderHeader.PaymentDate = DateTime.Now;

        await _db.SaveChangesAsync();

        return RedirectToAction("Receipt", new { id = orderId });
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
}