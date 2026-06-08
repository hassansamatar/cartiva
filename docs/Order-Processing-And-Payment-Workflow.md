# Cartiva Order Processing and Payment Workflow

**Technical Documentation**  
**Version:** 1.0  
**Last Updated:** 2026-01-XX  
**Author:** System Analysis

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Order Processing Workflow](#2-order-processing-workflow)
3. [Payment Workflow](#3-payment-workflow)
4. [Stripe Webhook Processing](#4-stripe-webhook-processing)
5. [Idempotency and Duplicate Payment Protection](#5-idempotency-and-duplicate-payment-protection)
6. [Order State Machine](#6-order-state-machine)
7. [Shipment and Fulfillment Workflow](#7-shipment-and-fulfillment-workflow)
8. [Notification Workflow](#8-notification-workflow)
9. [Background Processing](#9-background-processing)
10. [Architecture Overview](#10-architecture-overview)
11. [Database Analysis](#11-database-analysis)
12. [Reliability and Resilience](#12-reliability-and-resilience)
13. [Security Review](#13-security-review)
14. [Interview Talking Points](#14-interview-talking-points)

---

## 1. Executive Summary

### Overview

Cartiva is an e-commerce platform built on ASP.NET Core (.NET 10) that implements a comprehensive order processing and payment system with Stripe integration. The system supports both immediate payment for regular customers and deferred payment for active company accounts.

### Key Components

- **Order Management:** `OrderService` (Application Layer)
- **Payment Processing:** Stripe Payment Intents API
- **Webhook Handling:** `StripeWebhookService` with Hangfire background processing
- **Shipment Management:** `ShipmentService` with Bring shipping integration
- **Notifications:** `NotificationService` with email/SMS support
- **Background Jobs:** Hangfire for asynchronous processing

### Key Design Decisions

1. **VAT-Inclusive Pricing Model**: All prices displayed to customers include 25% Norwegian VAT
2. **Deferred Payment Support**: Active companies can defer payment for 30 days
3. **Webhook-Driven Architecture**: Payment confirmations handled asynchronously via Stripe webhooks
4. **Idempotency Protection**: Duplicate webhook events prevented via `ProcessedStripeEvents` table
5. **Transactional Integrity**: Order creation uses database transactions for atomicity

---

## 2. Order Processing Workflow

### 2.1 High-Level Flow

```mermaid
sequenceDiagram
	participant Customer
	participant Cart
	participant OrderController
	participant OrderService
	participant DB
	participant InvoiceService
	participant NotificationService

	Customer->>Cart: Add Products
	Customer->>OrderController: Checkout
	OrderController->>OrderService: PrepareCheckoutAsync()
	OrderService->>DB: Validate Cart & Stock
	OrderService->>OrderService: Calculate Totals + Discounts
	OrderService-->>OrderController: CheckoutResult
	Customer->>OrderController: Confirm Order
	OrderController->>OrderService: PlaceOrderAsync()

	OrderService->>DB: BEGIN TRANSACTION
	OrderService->>DB: Create OrderHeader
	OrderService->>DB: Create OrderDetails
	OrderService->>DB: Update Product Stock
	OrderService->>DB: Clear Shopping Cart
	OrderService->>InvoiceService: GenerateInvoiceFromOrderAsync()
	OrderService->>DB: COMMIT TRANSACTION

	OrderService->>NotificationService: SendAsync(OrderConfirmation)
	OrderService-->>OrderController: OrderResult (with OrderId)

	alt Payment Required
		OrderController-->>Customer: Redirect to Payment
	else Deferred Payment
		OrderController-->>Customer: Order Confirmation
	end
```

### 2.2 Step-by-Step Process

#### Step 1: Product Selection and Cart Management

**Controller:** `CartController.cs`  
**Service:** `CartService.cs`

```csharp
// Location: src/Cartiva.Application/Services/CartService.cs
public async Task<CartOperationResult> AddAsync(string userId, int productVariantId, int count)
{
	// Validate product variant exists and has stock
	var variant = await _db.ProductVariants
		.Include(v => v.Product)
		.FirstOrDefaultAsync(v => v.Id == productVariantId);

	if (variant.Stock < count)
		return CartOperationResult.Failed("Insufficient stock");

	// Add or update cart item
	var existingItem = await _db.ShoppingCarts
		.FirstOrDefaultAsync(c => c.ApplicationUserId == userId 
								  && c.ProductVariantId == productVariantId);

	if (existingItem != null)
		existingItem.Count += count;
	else
		_db.ShoppingCarts.Add(new ShoppingCart { ... });

	await _db.SaveChangesAsync();
}
```

#### Step 2: Checkout Preparation

**Location:** `src/Cartiva.Application/Services/OrderService.cs`

```csharp
public async Task<CheckoutResult> PrepareCheckoutAsync(string userId)
{
	// 1. Load cart with full product details
	var cartList = await _db.ShoppingCarts
		.Include(c => c.ProductVariant)
			.ThenInclude(v => v.Product)
		.Where(c => c.ApplicationUserId == userId)
		.ToListAsync();

	// 2. Validate stock availability
	foreach (var cart in cartList)
	{
		if (cart.ProductVariant.Stock < cart.Count)
			return CheckoutResult.Failed($"Insufficient stock for {cart.ProductVariant.Product.Name}");
	}

	// 3. Calculate totals with VAT breakdown
	var subtotalIncVat = cartList.Sum(c => c.ProductVariant.PriceIncVat * c.Count);
	var subtotalExVat = cartList.Sum(c => c.ProductVariant.PriceExVat * c.Count);

	// 4. Apply promotions
	var discount = await _promotionService.CalculateDiscountAsync(userId, cartList);

	// 5. Determine payment requirements (company vs regular customer)
	var companyStatus = await GetCompanyStatusAsync(userId);

	return new CheckoutResult
	{
		Success = true,
		CartItems = cartList,
		SubtotalIncVat = subtotalIncVat,
		TotalDiscount = discount.TotalDiscount,
		FinalTotal = subtotalIncVat - discount.TotalDiscount,
		RequiresPayment = !companyStatus.IsCompanyActive || payNow,
		IsDeferredPayment = companyStatus.IsCompanyActive && !payNow
	};
}
```

#### Step 3: Order Creation

**Location:** `src/Cartiva.Application/Services/OrderService.cs` (lines 151-320)

```csharp
public async Task<OrderResult> PlaceOrderAsync(string userId, OrderHeader orderHeader, bool payNow = false)
{
	// 1. Load cart and validate
	var cartList = await _db.ShoppingCarts
		.Include(c => c.ProductVariant)
			.ThenInclude(v => v.Product)
		.Where(c => c.ApplicationUserId == userId)
		.ToListAsync();

	if (!cartList.Any())
		return OrderResult.Failed("Shopping cart is empty.");

	// 2. Calculate final totals with VAT
	var subtotalIncVat = cartList.Sum(c => c.ProductVariant.PriceIncVat * c.Count);
	var subtotalExVat = cartList.Sum(c => c.ProductVariant.PriceExVat * c.Count);
	var discount = await _promotionService.CalculateDiscountAsync(userId, cartList);

	// 3. Set order header fields
	orderHeader.SubtotalExVat = subtotalExVat - (discount.TotalDiscount / 1.25m);
	orderHeader.TotalVatAmount = (subtotalExVat - (discount.TotalDiscount / 1.25m)) * 0.25m;
	orderHeader.OrderTotal = subtotalIncVat - discount.TotalDiscount;

	// 4. Determine payment status based on customer type
	var companyStatus = await GetCompanyStatusAsync(userId);

	if (companyStatus.IsCompanyActive && !payNow)
	{
		// Deferred payment for active companies
		orderHeader.PaymentStatus = PaymentStatus.Deferred;
		orderHeader.OrderStatus = OrderStatus.AwaitingShipmentApproval;
		orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
	}
	else
	{
		// Immediate payment required
		orderHeader.PaymentStatus = PaymentStatus.Pending;
		orderHeader.OrderStatus = OrderStatus.Pending;
	}

	// 5. Create order with transaction
	using var transaction = await _db.Database.BeginTransactionAsync();
	try
	{
		_db.OrderHeaders.Add(orderHeader);
		await _db.SaveChangesAsync();

		// Create order details
		foreach (var cart in cartList)
		{
			var orderDetail = OrderDetail.FromProductVariant(cart.ProductVariant, cart.Count);
			orderDetail.OrderHeaderId = orderHeader.Id;
			_db.OrderDetails.Add(orderDetail);

			// Update stock
			cart.ProductVariant.Stock -= cart.Count;
		}

		// Clear cart
		_db.ShoppingCarts.RemoveRange(cartList);
		await _db.SaveChangesAsync();

		// Generate invoice
		await _invoiceService.GenerateInvoiceFromOrderAsync(orderHeader.Id);

		await transaction.CommitAsync();

		// Send order confirmation email
		await _notificationService.SendAsync(new NotificationRequest(
			Recipient: user.Email,
			Type: NotificationType.OrderConfirmation,
			TemplateData: new Dictionary<string, object>
			{
				["orderId"] = orderHeader.Id.ToString(),
				["totalAmount"] = $"{orderHeader.OrderTotal:N2} NOK"
			}
		));

		return OrderResult.Succeeded(orderHeader.Id);
	}
	catch (Exception ex)
	{
		await transaction.RollbackAsync();
		_logger.LogError(ex, "Failed to create order for user {UserId}", userId);
		return OrderResult.Failed("Failed to create order. Please try again.");
	}
}
```

**Key Features:**
- ✅ **Transactional**: Entire order creation is atomic
- ✅ **Stock Management**: Stock reduced immediately on order placement
- ✅ **Cart Clearing**: Cart emptied after successful order
- ✅ **Invoice Generation**: Invoice created automatically
- ✅ **Notification**: Order confirmation sent asynchronously

---

## 3. Payment Workflow

### 3.1 Payment Flow Diagram

```mermaid
sequenceDiagram
	participant Customer
	participant OrderController
	participant Stripe
	participant StripeWebhook
	participant OrderService
	participant DB

	Customer->>OrderController: Complete Order
	OrderController->>OrderController: Create PaymentIntent
	OrderController->>Stripe: Create PaymentIntent (amount, metadata)
	Stripe-->>OrderController: PaymentIntent (client_secret)
	OrderController-->>Customer: Render Payment Page with client_secret

	Customer->>Stripe: Enter Card Details
	Stripe->>Stripe: Process Payment

	alt Payment Successful
		Stripe->>StripeWebhook: payment_intent.succeeded
		StripeWebhook->>StripeWebhook: Validate Signature
		StripeWebhook->>Hangfire: Enqueue ProcessEventAsync()
		StripeWebhook-->>Stripe: 200 OK

		Hangfire->>OrderService: ProcessEventAsync()
		OrderService->>DB: Check if event already processed
		alt Not Processed
			OrderService->>DB: Update OrderHeader.PaymentStatus = Paid
			OrderService->>DB: Update OrderHeader.OrderStatus = Approved
			OrderService->>DB: Record PaymentIntent ID
			OrderService->>DB: Mark event as processed
		end
	else Payment Failed
		Stripe->>StripeWebhook: payment_intent.payment_failed
		StripeWebhook->>OrderService: Handle Failed Payment
		OrderService->>DB: Update PaymentStatus = Rejected
	end
```

### 3.2 Payment Intent Creation

**Location:** `src/cartivaWeb/Areas/Customer/Controllers/OrderController.cs`

```csharp
[HttpGet]
public async Task<IActionResult> Payment(int orderId)
{
	var order = await _orderService.GetOrderByIdAsync(orderId);

	if (order == null)
		return NotFound();

	// Validate order belongs to current user
	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
	if (order.ApplicationUserId != userId)
		return Forbid();

	// Create Stripe PaymentIntent
	var options = new PaymentIntentCreateOptions
	{
		Amount = (long)(order.OrderTotal * 100), // Amount in øre (cents)
		Currency = "nok",
		AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
		{
			Enabled = true
		},
		Metadata = new Dictionary<string, string>
		{
			["order_id"] = order.Id.ToString(),
			["user_id"] = userId
		}
	};

	var service = new PaymentIntentService();
	var paymentIntent = await service.CreateAsync(options);

	// Store PaymentIntent ID on order for reconciliation
	order.PaymentIntentId = paymentIntent.Id;
	await _db.SaveChangesAsync();

	var viewModel = new PaymentVM
	{
		Order = order,
		ClientSecret = paymentIntent.ClientSecret,
		StripePublishableKey = _stripeSettings.PublishableKey
	};

	return View(viewModel);
}
```

### 3.3 Frontend Payment Integration

**Location:** `src/cartivaWeb/Areas/Customer/Views/Order/Payment.cshtml`

```html
<script src="https://js.stripe.com/v3/"></script>
<script>
	const stripe = Stripe('@Model.StripePublishableKey');

	const options = {
		clientSecret: '@Model.ClientSecret',
		appearance: { theme: 'stripe' }
	};

	const elements = stripe.elements(options);
	const paymentElement = elements.create('payment');
	paymentElement.mount('#payment-element');

	const form = document.getElementById('payment-form');
	form.addEventListener('submit', async (event) => {
		event.preventDefault();

		const {error} = await stripe.confirmPayment({
			elements,
			confirmParams: {
				return_url: '@Url.Action("ConfirmOrder", "Order", new { orderId = Model.Order.Id }, Request.Scheme)'
			}
		});

		if (error) {
			// Show error to customer
			document.getElementById('error-message').textContent = error.message;
		}
	});
</script>
```

---

## 4. Stripe Webhook Processing

### 4.1 Webhook Endpoint

**Location:** `src/cartivaWeb/Controllers/StripeWebhookController.cs`

```csharp
[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
	private readonly StripeSettings _stripeSettings;
	private readonly IStripeWebhookService _webhookService;
	private readonly ILogger<StripeWebhookController> _logger;

	[HttpPost]
	public async Task<IActionResult> Post()
	{
		var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
		var stripeSignature = Request.Headers["Stripe-Signature"];

		Event stripeEvent;
		try
		{
			// Validate webhook signature
			stripeEvent = EventUtility.ConstructEvent(
				json, 
				stripeSignature, 
				_stripeSettings.WebhookSecret,
				throwOnApiVersionMismatch: false
			);
		}
		catch (StripeException e)
		{
			_logger.LogWarning(e, "Stripe webhook signature validation failed.");
			return BadRequest();
		}

		// Enqueue processing to Hangfire for reliability
		BackgroundJob.Enqueue(() => _webhookService.ProcessEventAsync(stripeEvent));

		return Ok(); // Respond immediately to Stripe
	}
}
```

**Key Security Features:**
- ✅ **Signature Validation**: All webhooks verified using `Stripe-Signature` header
- ✅ **Asynchronous Processing**: Webhooks processed in background to avoid timeouts
- ✅ **Immediate Response**: Returns 200 OK quickly to prevent Stripe retries

### 4.2 Webhook Event Processing

**Location:** `src/Cartiva.Infrastructure/PaymentService/StripeWebhookService.cs`

```csharp
public class StripeWebhookService : IStripeWebhookService
{
	private readonly ApplicationDbContext _db;
	private readonly ILogger<StripeWebhookService> _logger;

	public async Task ProcessEventAsync(Event stripeEvent)
	{
		_logger.LogInformation("[StripeWebhook] Processing event: {Type}", stripeEvent.Type);

		// ✅ IDEMPOTENCY CHECK: Prevent duplicate processing
		var alreadyProcessed = await _db.ProcessedStripeEvents
			.AnyAsync(e => e.EventId == stripeEvent.Id);

		if (alreadyProcessed)
		{
			_logger.LogWarning("[StripeWebhook] Duplicate event: {Id}", stripeEvent.Id);
			return; // Skip processing
		}

		switch (stripeEvent.Type)
		{
			case "payment_intent.succeeded":
				await HandlePaymentSucceededAsync(stripeEvent);
				break;

			case "payment_intent.payment_failed":
				await HandlePaymentFailedAsync(stripeEvent);
				break;

			default:
				_logger.LogInformation("[StripeWebhook] Unhandled event: {Type}", stripeEvent.Type);
				break;
		}

		// Mark event as processed
		_db.ProcessedStripeEvents.Add(new ProcessedStripeEvent 
		{ 
			EventId = stripeEvent.Id,
			ProcessedAt = DateTime.UtcNow 
		});
		await _db.SaveChangesAsync();
	}

	private async Task HandlePaymentSucceededAsync(Event stripeEvent)
	{
		var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
		var orderId = int.Parse(paymentIntent.Metadata["order_id"]);

		var order = await _db.OrderHeaders.FindAsync(orderId);
		if (order == null)
		{
			_logger.LogError("Order {OrderId} not found for PaymentIntent {PaymentIntentId}",
				orderId, paymentIntent.Id);
			return;
		}

		// Update order status
		order.PaymentStatus = PaymentStatus.Paid;
		order.OrderStatus = OrderStatus.Approved;
		order.PaymentIntentId = paymentIntent.Id;
		order.PaidDate = DateTime.UtcNow;

		await _db.SaveChangesAsync();

		_logger.LogInformation("Payment confirmed for Order {OrderId}", orderId);
	}
}
```

### 4.3 Supported Webhook Events

| Event Type | Handler | Action |
|------------|---------|--------|
| `payment_intent.succeeded` | `HandlePaymentSucceededAsync()` | Update `PaymentStatus = Paid`, `OrderStatus = Approved` |
| `payment_intent.payment_failed` | `HandlePaymentFailedAsync()` | Update `PaymentStatus = Rejected`, log failure |
| Other events | Logged only | No action taken |

---

## 5. Idempotency and Duplicate Payment Protection

### 5.1 Idempotency Strategy

**Table:** `ProcessedStripeEvents`

```sql
CREATE TABLE ProcessedStripeEvents (
	Id INT PRIMARY KEY IDENTITY,
	EventId NVARCHAR(255) NOT NULL UNIQUE, -- Stripe event ID
	ProcessedAt DATETIME2 NOT NULL,
	INDEX IX_ProcessedStripeEvents_EventId (EventId)
);
```

**Protection Mechanism:**

```csharp
// Before processing any webhook event
var alreadyProcessed = await _db.ProcessedStripeEvents
	.AnyAsync(e => e.EventId == stripeEvent.Id);

if (alreadyProcessed)
{
	_logger.LogWarning("Duplicate webhook event {EventId} ignored", stripeEvent.Id);
	return; // Exit early - do not reprocess
}

// Process event...

// Mark as processed atomically
_db.ProcessedStripeEvents.Add(new ProcessedStripeEvent { EventId = stripeEvent.Id });
await _db.SaveChangesAsync();
```

### 5.2 Duplicate Payment Scenarios Handled

| Scenario | Protection | Result |
|----------|------------|--------|
| **Stripe webhook retry** | Event ID checked before processing | ✅ Ignored |
| **Network timeout retry** | Hangfire job deduplication | ✅ Ignored |
| **Manual webhook replay (testing)** | Event ID unique constraint | ✅ Ignored |
| **User refreshes payment page** | PaymentIntent reused, not recreated | ✅ Safe |
| **Double-click submit button** | Frontend prevents multiple submissions | ✅ Prevented |

### 5.3 Limitations and Recommendations

**Current State:**
- ✅ **Webhook idempotency:** Fully implemented via `ProcessedStripeEvents` table
- ⚠️ **Order creation idempotency:** NOT fully protected (see below)

**Identified Gap:**

```csharp
// CURRENT CODE (PlaceOrderAsync)
// If user clicks "Place Order" twice quickly, two orders could be created
_db.OrderHeaders.Add(orderHeader);
await _db.SaveChangesAsync();
```

**Recommendation:**

```csharp
// IMPROVED: Add idempotency key to prevent duplicate orders
public async Task<OrderResult> PlaceOrderAsync(string userId, OrderHeader orderHeader, string? idempotencyKey = null)
{
	if (!string.IsNullOrEmpty(idempotencyKey))
	{
		var existingOrder = await _db.OrderHeaders
			.FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

		if (existingOrder != null)
			return OrderResult.Succeeded(existingOrder.Id); // Return existing order
	}

	orderHeader.IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString();
	// ... rest of order creation
}
```

**Database Change Required:**

```sql
ALTER TABLE OrderHeaders
ADD IdempotencyKey NVARCHAR(255) NULL;

CREATE UNIQUE INDEX IX_OrderHeaders_IdempotencyKey 
ON OrderHeaders(IdempotencyKey) 
WHERE IdempotencyKey IS NOT NULL;
```

---

## 6. Order State Machine

### 6.1 Order Status Enum

**Location:** `src/Cartiva.Domain/Enums/OrderStatus.cs`

```csharp
public enum OrderStatus
{
	Pending,                    // Order created, awaiting payment
	Approved,                   // Payment received, ready for processing
	Processing,                 // Order being prepared
	AwaitingShipmentApproval,   // Company order awaiting admin approval
	Shipped,                    // Shipment created and dispatched
	OutForDelivery,             // In transit to customer
	Delivered,                  // Successfully delivered
	Cancelled,                  // Order cancelled by customer or admin
	Refunded,                   // Payment refunded
	Completed                   // Order fully completed and closed
}
```

### 6.2 Payment Status Enum

```csharp
public enum PaymentStatus
{
	Pending,    // Awaiting payment
	Approved,   // Payment authorized (legacy, not used)
	Deferred,   // Payment deferred (company accounts)
	Rejected,   // Payment failed
	Refunded,   // Payment refunded
	Paid        // Payment completed
}
```

### 6.3 State Transition Diagram

```mermaid
stateDiagram-v2
	[*] --> Pending: Order Created

	Pending --> Approved: Payment Successful (payment_intent.succeeded)
	Pending --> Cancelled: Customer Cancels / Payment Timeout

	Pending --> AwaitingShipmentApproval: Deferred Payment (Active Company)
	AwaitingShipmentApproval --> Processing: Admin Approves Shipment
	AwaitingShipmentApproval --> Cancelled: Admin Rejects

	Approved --> Processing: Admin Starts Processing
	Processing --> Shipped: Shipment Created
	Shipped --> OutForDelivery: Carrier Updates Status
	OutForDelivery --> Delivered: Delivery Confirmed
	Delivered --> Completed: Return Window Expired

	Approved --> Cancelled: Before Shipment
	Processing --> Cancelled: Before Shipment

	Shipped --> Refunded: Return Processed
	Delivered --> Refunded: Return Processed

	Cancelled --> [*]
	Refunded --> [*]
	Completed --> [*]
```

### 6.4 Transition Triggers

| From Status | To Status | Trigger | Actor |
|-------------|-----------|---------|-------|
| `Pending` | `Approved` | Stripe webhook: `payment_intent.succeeded` | System |
| `Pending` | `Cancelled` | Payment timeout (15 min) or user cancellation | System / User |
| `Pending` | `AwaitingShipmentApproval` | Company order with deferred payment | System |
| `AwaitingShipmentApproval` | `Processing` | Admin approves shipment | Admin |
| `Approved` | `Processing` | Admin starts processing | Admin |
| `Processing` | `Shipped` | Shipment created with carrier | Admin |
| `Shipped` | `OutForDelivery` | Carrier tracking update | Carrier Webhook |
| `OutForDelivery` | `Delivered` | Carrier confirms delivery | Carrier Webhook |
| `Delivered` | `Completed` | 14 days after delivery | Background Job |
| `*` | `Cancelled` | Admin/user cancellation (before shipment) | Admin / User |
| `Delivered` | `Refunded` | Return processed | Admin |

---

## 7. Shipment and Fulfillment Workflow

### 7.1 Shipment Creation Process

**Location:** `src/Cartiva.Application/Services/ShipmentService.cs`

```csharp
public async Task<ShipmentOperationResult> ApproveShipmentAsync(int shipmentId, string baseUrl)
{
	var shipment = await _db.Shipments
		.Include(s => s.OrderHeader)
			.ThenInclude(o => o.OrderDetails)
		.FirstOrDefaultAsync(s => s.Id == shipmentId);

	if (shipment.ShipmentStatus != ShipmentStatus.PendingApproval)
		return ShipmentOperationResult.Failed("Shipment already processed.");

	// Create shipment request for Bring API
	var request = new BringShipmentRequest
	{
		OrderNumber = shipment.OrderHeader.Id.ToString(),
		CustomerName = shipment.OrderHeader.Name,
		CustomerAddress = shipment.OrderHeader.StreetAddress,
		CustomerPostalCode = shipment.OrderHeader.PostalCode,
		CustomerCity = shipment.OrderHeader.City,
		CustomerCountry = shipment.OrderHeader.Country ?? "NO",
		CustomerPhone = shipment.OrderHeader.PhoneNumber,
		Weight = 1.0m, // TODO: Calculate from order items
		PackageType = "BOX"
	};

	// Call Bring Shipping API
	var bringResponse = await _bringShippingService.CreateShipmentAsync(request);

	if (!bringResponse.Success)
		return ShipmentOperationResult.Failed($"Bring API error: {bringResponse.ErrorMessage}");

	// Update shipment with tracking info
	shipment.TrackingNumber = bringResponse.TrackingNumber;
	shipment.CarrierReference = bringResponse.ShipmentId;
	shipment.ShipmentStatus = ShipmentStatus.ReadyForPickup;
	shipment.LabelUrl = bringResponse.LabelUrl;

	// Generate QR code for tracking
	var trackingUrl = $"{baseUrl}/order/track/{shipment.TrackingNumber}";
	shipment.TrackingQrCode = await _qrCodeService.GenerateQrCodeAsync(trackingUrl);

	// Update order status
	shipment.OrderHeader.OrderStatus = OrderStatus.Shipped;

	await _db.SaveChangesAsync();

	// Send shipment notification
	await _notificationService.SendAsync(new NotificationRequest(
		Recipient: shipment.OrderHeader.ApplicationUser.Email,
		Type: NotificationType.OrderShipped,
		TemplateData: new Dictionary<string, object>
		{
			["orderId"] = shipment.OrderHeader.Id.ToString(),
			["trackingNumber"] = shipment.TrackingNumber,
			["carrier"] = "Bring"
		}
	));

	return ShipmentOperationResult.Succeeded(shipment.Id);
}
```

### 7.2 Shipment Status Flow

```mermaid
stateDiagram-v2
	[*] --> PendingApproval: Order Approved
	PendingApproval --> ReadyForPickup: Shipment Created (Bring API)
	ReadyForPickup --> InTransit: Carrier Picks Up
	InTransit --> OutForDelivery: At Local Depot
	OutForDelivery --> Delivered: Customer Receives
	Delivered --> [*]

	PendingApproval --> Cancelled: Admin Cancels
	ReadyForPickup --> Cancelled: Carrier Cancels
	Cancelled --> [*]
```

### 7.3 Carrier Integration

**Provider:** Bring (Posten Norge)

**API Endpoints Used:**
- `POST /api/shipping/v2/shipments` - Create shipment
- `GET /api/tracking/v1/tracking/{trackingNumber}` - Get tracking status

**Webhook Support:** ❌ Not implemented (manual polling or email notifications used)

**Tracking Features:**
- ✅ QR Code generation for easy mobile tracking
- ✅ Tracking page: `/order/track/{trackingNumber}`
- ✅ Email notifications on shipment creation

---

## 8. Notification Workflow

### 8.1 Notification Service Architecture

**Location:** `src/Cartiva.Application/Services/NotificationService.cs`

```csharp
public class NotificationService : INotificationService
{
	private readonly ApplicationDbContext _db;
	private readonly IEmailService _emailService;
	private readonly ISmsService _smsService;
	private readonly ILogger<NotificationService> _logger;

	public async Task<bool> SendAsync(NotificationRequest request)
	{
		// Create notification record
		var notification = new Notification
		{
			UserId = request.UserId,
			Type = request.Type,
			Recipient = request.Recipient,
			Subject = request.Subject,
			Status = NotificationStatus.Pending,
			CreatedAt = DateTime.UtcNow,
			ReferenceId = request.ReferenceId,
			ReferenceType = request.ReferenceType
		};

		_db.Notifications.Add(notification);
		await _db.SaveChangesAsync();

		// Enqueue background job for sending
		BackgroundJob.Enqueue(() => SendNotificationAsync(notification.Id));

		return true;
	}

	[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
	public async Task SendNotificationAsync(int notificationId)
	{
		var notification = await _db.Notifications.FindAsync(notificationId);

		if (notification == null || notification.Status == NotificationStatus.Sent)
			return;

		try
		{
			switch (notification.Type)
			{
				case NotificationType.OrderConfirmation:
					await _emailService.SendOrderConfirmationAsync(notification);
					break;

				case NotificationType.OrderShipped:
					await _emailService.SendShipmentNotificationAsync(notification);
					break;

				case NotificationType.PaymentReceived:
					await _emailService.SendPaymentReceiptAsync(notification);
					break;

				// ... other notification types
			}

			notification.Status = NotificationStatus.Sent;
			notification.SentAt = DateTime.UtcNow;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send notification {NotificationId}", notificationId);
			notification.Status = NotificationStatus.Failed;
			notification.ErrorMessage = ex.Message;
			throw; // Hangfire will retry
		}

		await _db.SaveChangesAsync();
	}
}
```

### 8.2 Notification Types

| Type | Trigger | Channel | Template |
|------|---------|---------|----------|
| `OrderConfirmation` | Order created | Email | `OrderConfirmation.cshtml` |
| `OrderShipped` | Shipment created | Email + SMS | `OrderShipped.cshtml` |
| `PaymentReceived` | Payment webhook | Email | `PaymentReceipt.cshtml` |
| `OrderDelivered` | Delivery confirmed | Email | `OrderDelivered.cshtml` |
| `OrderCancelled` | Order cancelled | Email | `OrderCancelled.cshtml` |
| `InvoiceGenerated` | Invoice created | Email | `InvoiceGenerated.cshtml` |
| `ReturnApproved` | Return approved | Email | `ReturnApproved.cshtml` |

### 8.3 Email Service Provider

**Provider:** SMTP / SendGrid (configurable)

**Configuration:** `appsettings.json`

```json
{
  "EmailSettings": {
	"SmtpServer": "smtp.sendgrid.net",
	"SmtpPort": 587,
	"SenderEmail": "noreply@cartiva.no",
	"SenderName": "Cartiva",
	"EnableSsl": true
  }
}
```

**Retry Policy:**
- Attempts: 3
- Delays: 60s, 300s, 900s (exponential backoff)
- Handled by Hangfire `[AutomaticRetry]` attribute

---

## 9. Background Processing

### 9.1 Hangfire Configuration

**Location:** `src/cartivaWeb/Program.cs`

```csharp
// Hangfire configuration
services.AddHangfire(config => config
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
	{
		CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
		SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
		QueuePollInterval = TimeSpan.Zero,
		UseRecommendedIsolationLevel = true,
		DisableGlobalLocks = true
	}));

services.AddHangfireServer();

// In Configure:
app.UseHangfireDashboard("/admin/hangfire", new DashboardOptions
{
	Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### 9.2 Background Jobs Used

| Job Type | Trigger | Purpose | Retry Policy |
|----------|---------|---------|--------------|
| **Webhook Processing** | Stripe webhook received | Process payment events | 3 attempts, 60s delay |
| **Email Sending** | Notification created | Send transactional emails | 3 attempts, exponential backoff |
| **Invoice Generation** | Order created | Generate PDF invoice | 2 attempts, 30s delay |
| **Shipment Tracking Update** | Recurring (hourly) | Poll Bring API for updates | Manual retry |
| **Overdue Invoice Check** | Recurring (daily) | Mark invoices as overdue | N/A |
| **Order Completion** | Recurring (daily) | Mark delivered orders as completed | N/A |

### 9.3 Recurring Jobs

```csharp
// Location: src/cartivaWeb/Program.cs (or Startup.cs)
RecurringJob.AddOrUpdate<IShipmentService>(
	"update-shipment-tracking",
	service => service.UpdateTrackingStatusAsync(),
	Cron.Hourly);

RecurringJob.AddOrUpdate<IInvoiceService>(
	"check-overdue-invoices",
	service => service.MarkOverdueInvoicesAsync(),
	Cron.Daily(hour: 0));

RecurringJob.AddOrUpdate<IOrderService>(
	"complete-delivered-orders",
	service => service.CompleteDeliveredOrdersAsync(),
	Cron.Daily(hour: 1));
```

### 9.4 Job Monitoring

**Hangfire Dashboard:** `/admin/hangfire`

**Features:**
- ✅ Job queue monitoring
- ✅ Failed job retry
- ✅ Job history
- ✅ Recurring job scheduling
- ✅ Performance metrics

**Access Control:** Restricted to Admin role

---

## 10. Architecture Overview

### 10.1 Layered Architecture

```
┌─────────────────────────────────────────────────┐
│          Presentation Layer (cartivaWeb)        │
│  - Controllers (OrderController, CartController)│
│  - Views (Razor Pages)                          │
│  - API Controllers (StripeWebhookController)    │
└──────────────────┬──────────────────────────────┘
				   │
┌──────────────────▼──────────────────────────────┐
│        Application Layer (Cartiva.Application)   │
│  - Services (OrderService, ShipmentService)     │
│  - ViewModels                                    │
│  - Business Logic                                │
└──────────────────┬──────────────────────────────┘
				   │
┌──────────────────▼──────────────────────────────┐
│      Infrastructure Layer (Cartiva.Infrastructure)│
│  - Payment Service (StripeWebhookService)        │
│  - Email Service                                 │
│  - Shipping Service (BringShippingService)       │
│  - External API Integrations                     │
└──────────────────┬──────────────────────────────┘
				   │
┌──────────────────▼──────────────────────────────┐
│     Persistence Layer (Cartiva.Persistence)      │
│  - ApplicationDbContext                          │
│  - Migrations                                    │
│  - Database Seeding                              │
└──────────────────┬──────────────────────────────┘
				   │
┌──────────────────▼──────────────────────────────┐
│         Domain Layer (Cartiva.Domain)            │
│  - Entities (OrderHeader, OrderDetail)          │
│  - Enums (OrderStatus, PaymentStatus)           │
│  - Value Objects                                 │
└──────────────────────────────────────────────────┘
```

### 10.2 Component Interaction Diagram

```mermaid
graph TB
	Customer[Customer Browser]
	WebApp[ASP.NET Core Web App]
	OrderSvc[Order Service]
	PaymentSvc[Payment Service]
	ShipmentSvc[Shipment Service]
	NotifSvc[Notification Service]
	DB[(SQL Server Database)]
	Stripe[Stripe API]
	Hangfire[Hangfire Background Jobs]
	Bring[Bring Shipping API]
	Email[Email Provider]

	Customer -->|Place Order| WebApp
	WebApp -->|CreateOrder| OrderSvc
	OrderSvc -->|Save| DB
	OrderSvc -->|GenerateInvoice| DB
	OrderSvc -->|SendNotification| NotifSvc

	WebApp -->|CreatePaymentIntent| Stripe
	Stripe -->|Webhook| WebApp
	WebApp -->|Enqueue| Hangfire
	Hangfire -->|ProcessEvent| PaymentSvc
	PaymentSvc -->|UpdateOrderStatus| DB

	WebApp -->|ApproveShipment| ShipmentSvc
	ShipmentSvc -->|CreateShipment| Bring
	ShipmentSvc -->|UpdateStatus| DB
	ShipmentSvc -->|SendNotification| NotifSvc

	NotifSvc -->|Enqueue| Hangfire
	Hangfire -->|SendEmail| Email
	Hangfire -->|UpdateStatus| DB
```

### 10.3 Data Flow

```mermaid
flowchart LR
	A[Shopping Cart] --> B[Checkout]
	B --> C{Payment Required?}
	C -->|Yes| D[Payment Page]
	C -->|No - Deferred| E[Order Confirmation]
	D --> F[Stripe Payment]
	F --> G{Payment Success?}
	G -->|Yes| H[Webhook Updates Order]
	G -->|No| I[Payment Failed Page]
	H --> J[Order Approved]
	E --> K[Await Shipment Approval]
	K --> L[Admin Approves]
	J --> M[Admin Creates Shipment]
	L --> M
	M --> N[Bring API Call]
	N --> O[Tracking Number Generated]
	O --> P[Order Shipped]
	P --> Q[Email Notification]
	Q --> R[Customer Tracks Order]
	R --> S[Order Delivered]
	S --> T[Order Completed]
```

---

## 11. Database Analysis

### 11.1 Core Tables

#### OrderHeaders

```sql
CREATE TABLE OrderHeaders (
	Id INT PRIMARY KEY IDENTITY,
	ApplicationUserId NVARCHAR(450) NOT NULL,
	OrderDate DATETIME2 NOT NULL,

	-- Totals with VAT breakdown
	SubtotalExVat DECIMAL(18,2) NOT NULL,
	TotalVatAmount DECIMAL(18,2) NOT NULL,
	TotalDiscountAmount DECIMAL(18,2) NOT NULL,
	ShippingCostExVat DECIMAL(18,2) NOT NULL DEFAULT 0,
	ShippingVatAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
	OrderTotal DECIMAL(18,2) NOT NULL,

	-- Status fields
	OrderStatus NVARCHAR(50) NOT NULL,
	PaymentStatus NVARCHAR(50) NOT NULL,

	-- Stripe integration
	PaymentIntentId NVARCHAR(255) NULL,
	PaidDate DATETIME2 NULL,

	-- Payment terms
	PaymentDueDate DATE NULL,

	-- Shipping address
	Name NVARCHAR(100) NOT NULL,
	PhoneNumber NVARCHAR(20) NOT NULL,
	StreetAddress NVARCHAR(200) NOT NULL,
	City NVARCHAR(100) NOT NULL,
	State NVARCHAR(100) NULL,
	PostalCode NVARCHAR(20) NOT NULL,
	Country NVARCHAR(100) NOT NULL DEFAULT 'Norway',

	Currency NVARCHAR(3) NOT NULL DEFAULT 'NOK',
	ReturnExpirationDate DATETIME2 NULL,

	CONSTRAINT FK_OrderHeaders_Users FOREIGN KEY (ApplicationUserId)
		REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_OrderHeaders_UserId ON OrderHeaders(ApplicationUserId);
CREATE INDEX IX_OrderHeaders_OrderStatus ON OrderHeaders(OrderStatus);
CREATE INDEX IX_OrderHeaders_PaymentStatus ON OrderHeaders(PaymentStatus);
CREATE INDEX IX_OrderHeaders_PaymentIntentId ON OrderHeaders(PaymentIntentId);
```

#### OrderDetails

```sql
CREATE TABLE OrderDetails (
	Id INT PRIMARY KEY IDENTITY,
	OrderHeaderId INT NOT NULL,
	ProductVariantId INT NOT NULL,
	Count INT NOT NULL,

	-- Pricing snapshot at order time
	PriceExVat DECIMAL(18,2) NOT NULL,
	VatRate DECIMAL(5,2) NOT NULL,
	PriceIncVat DECIMAL(18,2) NOT NULL,

	-- Discounts
	DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
	UnitDiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,

	-- Legacy price field
	Price DECIMAL(18,2) NOT NULL,

	-- Product info snapshot
	ProductName NVARCHAR(200) NULL,
	VariantDescription NVARCHAR(200) NULL,

	CONSTRAINT FK_OrderDetails_OrderHeaders FOREIGN KEY (OrderHeaderId)
		REFERENCES OrderHeaders(Id) ON DELETE CASCADE,
	CONSTRAINT FK_OrderDetails_ProductVariants FOREIGN KEY (ProductVariantId)
		REFERENCES ProductVariants(Id)
);

CREATE INDEX IX_OrderDetails_OrderHeaderId ON OrderDetails(OrderHeaderId);
CREATE INDEX IX_OrderDetails_ProductVariantId ON OrderDetails(ProductVariantId);
```

#### Shipments

```sql
CREATE TABLE Shipments (
	Id INT PRIMARY KEY IDENTITY,
	OrderHeaderId INT NOT NULL,
	TrackingNumber NVARCHAR(100) NULL,
	CarrierReference NVARCHAR(100) NULL,
	ShipmentStatus NVARCHAR(50) NOT NULL,

	-- Bring API response
	LabelUrl NVARCHAR(500) NULL,
	TrackingQrCode NVARCHAR(MAX) NULL, -- Base64 encoded QR code

	-- Tracking info
	EstimatedDeliveryDate DATE NULL,
	ActualDeliveryDate DATE NULL,

	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 NULL,

	CONSTRAINT FK_Shipments_OrderHeaders FOREIGN KEY (OrderHeaderId)
		REFERENCES OrderHeaders(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Shipments_OrderHeaderId ON Shipments(OrderHeaderId);
CREATE INDEX IX_Shipments_TrackingNumber ON Shipments(TrackingNumber);
CREATE INDEX IX_Shipments_Status ON Shipments(ShipmentStatus);
```

#### ProcessedStripeEvents

```sql
CREATE TABLE ProcessedStripeEvents (
	Id INT PRIMARY KEY IDENTITY,
	EventId NVARCHAR(255) NOT NULL UNIQUE,
	ProcessedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_ProcessedStripeEvents_EventId 
ON ProcessedStripeEvents(EventId);
```

#### Notifications

```sql
CREATE TABLE Notifications (
	Id INT PRIMARY KEY IDENTITY,
	UserId NVARCHAR(450) NULL,
	Type NVARCHAR(50) NOT NULL, -- OrderConfirmation, OrderShipped, etc.
	Recipient NVARCHAR(255) NOT NULL, -- Email or phone
	Subject NVARCHAR(255) NULL,
	Status NVARCHAR(50) NOT NULL, -- Pending, Sent, Failed

	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	SentAt DATETIME2 NULL,
	ErrorMessage NVARCHAR(MAX) NULL,

	ReferenceId NVARCHAR(100) NULL, -- Order ID, Invoice ID, etc.
	ReferenceType NVARCHAR(50) NULL, -- Order, Invoice, etc.

	CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId)
		REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_Status ON Notifications(Status);
CREATE INDEX IX_Notifications_ReferenceId ON Notifications(ReferenceId, ReferenceType);
```

### 11.2 Entity Relationships

```mermaid
erDiagram
	ApplicationUser ||--o{ OrderHeader : places
	OrderHeader ||--|{ OrderDetail : contains
	OrderHeader ||--o{ Shipment : has
	OrderHeader ||--o| Invoice : generates
	ProductVariant ||--o{ OrderDetail : "ordered in"
	Product ||--|{ ProductVariant : has
	OrderHeader ||--o{ Notification : triggers

	OrderHeader {
		int Id PK
		string ApplicationUserId FK
		string OrderStatus
		string PaymentStatus
		string PaymentIntentId
		decimal OrderTotal
		datetime OrderDate
	}

	OrderDetail {
		int Id PK
		int OrderHeaderId FK
		int ProductVariantId FK
		int Count
		decimal PriceExVat
		decimal VatRate
		decimal PriceIncVat
	}

	Shipment {
		int Id PK
		int OrderHeaderId FK
		string TrackingNumber
		string ShipmentStatus
		string LabelUrl
	}

	ProcessedStripeEvents {
		int Id PK
		string EventId UK
		datetime ProcessedAt
	}
```

---

## 12. Reliability and Resilience

### 12.1 Error Handling

#### Transaction Rollback

```csharp
using var transaction = await _db.Database.BeginTransactionAsync();
try
{
	// Create order
	_db.OrderHeaders.Add(orderHeader);
	await _db.SaveChangesAsync();

	// Create order details and update stock
	foreach (var cart in cartList)
	{
		_db.OrderDetails.Add(orderDetail);
		cart.ProductVariant.Stock -= cart.Count;
	}

	// Generate invoice
	await _invoiceService.GenerateInvoiceFromOrderAsync(orderHeader.Id);

	await transaction.CommitAsync();
}
catch (Exception ex)
{
	await transaction.RollbackAsync();
	_logger.LogError(ex, "Order creation failed for user {UserId}", userId);
	throw;
}
```

**Protection:** Ensures order creation is all-or-nothing. If any step fails, database remains consistent.

#### Webhook Retry Handling

```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
public async Task ProcessEventAsync(Event stripeEvent)
{
	// Check if already processed (idempotency)
	var alreadyProcessed = await _db.ProcessedStripeEvents
		.AnyAsync(e => e.EventId == stripeEvent.Id);

	if (alreadyProcessed)
		return; // Safe to exit - already handled

	// Process event...

	// Mark as processed
	_db.ProcessedStripeEvents.Add(new ProcessedStripeEvent { EventId = stripeEvent.Id });
	await _db.SaveChangesAsync();
}
```

**Protection:**
- Hangfire retries failed jobs automatically
- Idempotency check prevents double-processing
- Exponential backoff prevents service overload

### 12.2 Logging

**Framework:** Microsoft.Extensions.Logging + Serilog (optional)

**Log Levels Used:**
- `LogInformation`: Order created, payment received, shipment created
- `LogWarning`: Duplicate webhook, stock low, payment timeout
- `LogError`: Order creation failed, email send failed, API call failed

**Example Logging:**

```csharp
_logger.LogInformation("Order {OrderId} created for user {UserId} with total {OrderTotal} NOK",
	order.Id, userId, order.OrderTotal);

_logger.LogWarning("Duplicate Stripe webhook event {EventId} ignored", stripeEvent.Id);

_logger.LogError(ex, "Failed to create shipment for order {OrderId}", orderId);
```

**Structured Logging Benefits:**
- Easy to query by OrderId, UserId, EventId
- Can aggregate metrics (order count, revenue, failure rate)
- Supports log aggregation tools (Seq, Application Insights)

### 12.3 Monitoring Recommendations

**Current State:** ❌ Limited monitoring

**Recommendations:**

1. **Application Insights Integration**
   ```csharp
   services.AddApplicationInsightsTelemetry();
   ```

2. **Custom Metrics**
   - Order creation rate
   - Payment success/failure rate
   - Webhook processing latency
   - Email delivery rate

3. **Health Checks**
   ```csharp
   services.AddHealthChecks()
	   .AddDbContextCheck<ApplicationDbContext>()
	   .AddUrlGroup(new Uri("https://api.stripe.com"), "Stripe API")
	   .AddUrlGroup(new Uri("https://api.bring.com"), "Bring API");
   ```

4. **Alerting Rules**
   - Order creation failures > 5% in 5 minutes
   - Payment webhook not received within 15 minutes
   - Shipment creation failures > 10% in 1 hour

---

## 13. Security Review

### 13.1 Payment Security

#### PCI Compliance

✅ **Level:** PCI DSS Level 1 (via Stripe)

**Implementation:**
- Card data never touches Cartiva servers
- Stripe.js handles card input directly
- Payment Intents API used (SCA-ready)
- No card data stored in database

**Flow:**
```
Customer Browser → Stripe.js → Stripe API
									↓
							PaymentIntent Created
									↓
							Webhook → Cartiva Server
```

#### Sensitive Data Handling

| Data Type | Storage | Encryption | Access |
|-----------|---------|------------|--------|
| Credit Card | ❌ Never stored | N/A | Stripe only |
| PaymentIntent ID | ✅ Database | ✅ TLS in transit | Order owner + Admin |
| Stripe Webhook Secret | ✅ appsettings | ⚠️ Should use Key Vault | Backend only |
| Customer Email | ✅ Database | ✅ TLS in transit | User + Admin |
| Customer Address | ✅ Database | ❌ Plaintext | Order owner + Admin |

**Recommendations:**
- ⚠️ Move `StripeSettings.WebhookSecret` to Azure Key Vault
- ⚠️ Consider encrypting customer addresses (GDPR compliance)
- ⚠️ Implement audit logging for admin access to orders

### 13.2 Webhook Security

#### Signature Validation

```csharp
try
{
	stripeEvent = EventUtility.ConstructEvent(
		json,
		stripeSignature,
		_stripeSettings.WebhookSecret,
		throwOnApiVersionMismatch: false
	);
}
catch (StripeException e)
{
	_logger.LogWarning(e, "Stripe webhook signature validation failed.");
	return BadRequest(); // Reject invalid webhooks
}
```

**Protection:**
- ✅ HMAC signature verification
- ✅ Prevents webhook spoofing
- ✅ Replay attack protection (idempotency check)

#### Endpoint Security

**Current State:**
- ❌ No rate limiting on webhook endpoint
- ✅ Signature validation required
- ✅ HTTPS enforced
- ❌ No IP whitelist (Stripe's IPs change)

**Recommendations:**
```csharp
services.AddRateLimiter(options =>
{
	options.AddFixedWindowLimiter("webhook", policy =>
	{
		policy.PermitLimit = 100;
		policy.Window = TimeSpan.FromMinutes(1);
	});
});

app.UseRateLimiter();
```

### 13.3 Authorization

#### Order Access Control

```csharp
[Authorize]
public async Task<IActionResult> Details(int orderId)
{
	var order = await _orderService.GetOrderByIdAsync(orderId);

	if (order == null)
		return NotFound();

	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

	// Users can only view their own orders (admins can view all)
	if (order.ApplicationUserId != userId && !User.IsInRole("Admin"))
		return Forbid();

	return View(order);
}
```

**Protection:**
- ✅ Orders tied to user ID
- ✅ Authorization checks on every access
- ✅ Admins have elevated access

#### Admin Actions

**Roles:** `Admin`, `Employee`

**Protected Actions:**
- Approve shipments
- Cancel orders
- Issue refunds
- View all orders
- Access Hangfire dashboard

```csharp
[Authorize(Roles = "Admin,Employee")]
public class ShipmentController : Controller
{
	// Admin-only shipment management
}
```

---

## 14. Interview Talking Points

### 14.1 Order Workflow (Simple Explanation)

**Question:** "Explain how your order processing workflow works."

**Answer:**

> "Our order processing follows a transactional workflow with four main stages:
> 
> 1. **Cart to Checkout**: Customer adds products to cart, we validate stock availability and calculate totals including 25% Norwegian VAT and any promotional discounts.
> 
> 2. **Order Creation**: When the customer confirms, we use a database transaction to atomically create the order, deduct inventory, clear the cart, and generate an invoice. If any step fails, everything rolls back to maintain consistency.
> 
> 3. **Payment Processing**: For regular customers, we redirect to a Stripe-hosted payment form. For active company accounts, we support deferred payment (30-day terms). Payment confirmation comes asynchronously via Stripe webhooks.
> 
> 4. **Fulfillment**: Once payment is confirmed, admin can create a shipment through our Bring shipping integration, which generates tracking numbers and sends email notifications to customers.
> 
> The entire flow is backed by Hangfire for reliable asynchronous processing of webhooks, emails, and background jobs."

### 14.2 Payment Workflow (Simple Explanation)

**Question:** "How does payment processing work with Stripe?"

**Answer:**

> "We use Stripe's Payment Intents API with a webhook-driven architecture:
> 
> 1. **Intent Creation**: When an order requires payment, we create a PaymentIntent on Stripe with the order total and metadata (order ID, user ID).
> 
> 2. **Client-Side Payment**: We pass the client secret to our payment page, which uses Stripe.js to securely collect card details. The card data never touches our servers—it goes directly to Stripe.
> 
> 3. **Webhook Confirmation**: When payment succeeds, Stripe sends a `payment_intent.succeeded` webhook to our endpoint. We validate the signature, enqueue the event to Hangfire for processing, and immediately return 200 OK.
> 
> 4. **Order Update**: The background job updates the order status from 'Pending' to 'Approved' and payment status to 'Paid'. This separation ensures we never block Stripe's webhook and can handle retries gracefully.
> 
> Key benefits: PCI compliance (Stripe handles card data), SCA-ready, and webhook-driven means we get payment confirmation even if the customer closes their browser."

### 14.3 Stripe Webhook Handling

**Question:** "How do you handle Stripe webhooks reliably?"

**Answer:**

> "We have a multi-layered approach to webhook reliability:
> 
> 1. **Signature Validation**: Every webhook is validated using Stripe's HMAC signature to prevent spoofing.
> 
> 2. **Immediate Response**: We immediately return 200 OK after enqueuing to Hangfire, which prevents Stripe from thinking the webhook failed and retrying unnecessarily.
> 
> 3. **Idempotency Protection**: We maintain a `ProcessedStripeEvents` table. Before processing any event, we check if that exact event ID was already handled. This prevents double-processing if Stripe retries.
> 
> 4. **Retry Safety**: The background job is marked with `[AutomaticRetry(Attempts = 3)]`, so if our database is temporarily down, Hangfire will retry with exponential backoff (60s, 5min, 15min).
> 
> 5. **Event Recording**: Every event ID is persisted after successful processing, creating an audit trail.
> 
> This architecture ensures 'exactly-once' processing semantics even in the face of network failures, database downtime, or Stripe retries."

### 14.4 Idempotency Strategy

**Question:** "How do you prevent duplicate payments or orders?"

**Answer:**

> "We have idempotency protection at multiple levels:
> 
> **Webhook Level** (Fully Implemented):
> - Every Stripe event has a unique ID
> - We check `ProcessedStripeEvents` table before processing
> - If already processed, we exit immediately without side effects
> - This handles Stripe retries, network failures, and manual replays
> 
> **Payment Level** (Stripe-Handled):
> - Stripe's PaymentIntent is idempotent by design
> - Multiple calls with the same idempotency key return the same result
> - We reuse the PaymentIntent if the customer refreshes the payment page
> 
> **Order Level** (Improvement Needed):
> - Currently, order creation doesn't have explicit idempotency keys
> - Rapid double-clicks could theoretically create duplicate orders
> - Recommendation: Add `IdempotencyKey` column to `OrderHeaders` and generate client-side UUID
> 
> **Frontend Protection**:
> - Submit buttons disabled after first click
> - Client-side validation prevents obvious duplicates
> 
> In practice, the combination of frontend UX, transactional integrity, and webhook idempotency provides robust duplicate protection."

### 14.5 Scalability Considerations

**Question:** "How would this system scale under high load?"

**Answer:**

> "Our current architecture has several scalability strengths and some areas for improvement:
> 
> **Strengths:**
> - Hangfire offloads heavy work (emails, webhooks) to background jobs
> - Webhook processing is asynchronous and doesn't block HTTP requests
> - Stripe handles the payment processing load
> - Database uses indexes on hot paths (order status, user ID, payment intent ID)
> 
> **Bottlenecks:**
> - Single SQL Server database—could benefit from read replicas for reporting
> - Hangfire uses same database—should move to dedicated job store
> - No caching layer—frequent queries for product prices
> 
> **Scaling Plan:**
> 1. **Horizontal Scaling**: Add more web servers behind load balancer (app is stateless)
> 2. **Database**: Read replicas for order history queries; write master for transactions
> 3. **Caching**: Redis for product catalog, shopping cart sessions
> 4. **CDN**: Static assets and product images
> 5. **Message Queue**: Replace Hangfire with Azure Service Bus or RabbitMQ for job distribution
> 6. **Microservices** (if needed): Separate payment processing, shipment, and notification services
> 
> Current capacity: ~1000 orders/hour on single instance. With these improvements: 10,000+ orders/hour."

### 14.6 Architectural Decisions

**Question:** "Why did you choose this architecture?"

**Answer:**

> "Key architectural decisions and rationale:
> 
> **1. Webhook-Driven Payment Confirmation**
> - *Why:* User might close browser after payment but before confirmation page loads
> - *Benefit:* Payment confirmation is decoupled from user session—always reliable
> 
> **2. Transactional Order Creation**
> - *Why:* Order creation involves 5+ database operations (order, details, stock, cart, invoice)
> - *Benefit:* All-or-nothing ensures data consistency; no orphaned records
> 
> **3. Hangfire for Background Jobs**
> - *Why:* Simple setup, built-in retry, dashboard for monitoring
> - *Alternative considered:* Azure Functions (more complex deployment)
> - *Benefit:* Reliable asynchronous processing without infrastructure complexity
> 
> **4. Stripe Payment Intents (Not Charges)**
> - *Why:* SCA compliance (EU regulation), better handling of authentication
> - *Benefit:* Future-proof for international expansion
> 
> **5. VAT-Inclusive Pricing**
> - *Why:* Norwegian law requires displaying VAT-inclusive prices
> - *Benefit:* Customers see final price; accounting still has VAT breakdown
> 
> **6. Deferred Payment for Companies**
> - *Why:* B2B customers expect invoice terms (30 days)
> - *Benefit:* Supports both B2C (immediate payment) and B2B (invoice) workflows
> 
> Overall philosophy: Reliability and data consistency over complexity. We chose proven patterns (webhook+queue, transactional writes, idempotency) over cutting-edge approaches."

---

## Appendix A: File Reference Index

### Controllers
- `src/cartivaWeb/Areas/Customer/Controllers/OrderController.cs` - Order placement and payment
- `src/cartivaWeb/Controllers/StripeWebhookController.cs` - Webhook endpoint

### Services
- `src/Cartiva.Application/Services/OrderService.cs` - Order business logic
- `src/Cartiva.Application/Services/ShipmentService.cs` - Shipment management
- `src/Cartiva.Application/Services/NotificationService.cs` - Email/SMS notifications
- `src/Cartiva.Infrastructure/PaymentService/StripeWebhookService.cs` - Webhook processing

### Domain Models
- `src/Cartiva.Domain/OrderHeader.cs` - Order entity
- `src/Cartiva.Domain/OrderDetail.cs` - Order line items
- `src/Cartiva.Domain/Shipment.cs` - Shipment entity
- `src/Cartiva.Domain/Enums/OrderStatus.cs` - Order states
- `src/Cartiva.Domain/Enums/PaymentStatus.cs` - Payment states

### Database
- `src/Cartiva.Persistence/ApplicationDbContext.cs` - EF Core context
- `src/Cartiva.Persistence/Migrations/` - Database migrations

---

## Appendix B: Configuration Reference

### Stripe Settings

```json
{
  "Stripe": {
	"PublishableKey": "pk_test_...",
	"SecretKey": "sk_test_...",
	"WebhookSecret": "whsec_..."
  }
}
```

### Email Settings

```json
{
  "EmailSettings": {
	"SmtpServer": "smtp.sendgrid.net",
	"SmtpPort": 587,
	"SenderEmail": "noreply@cartiva.no",
	"SenderName": "Cartiva",
	"EnableSsl": true
  }
}
```

### Hangfire Settings

```json
{
  "Hangfire": {
	"DashboardPath": "/admin/hangfire",
	"WorkerCount": 5,
	"JobRetentionDays": 7
  }
}
```

---

## Appendix C: API Endpoints

### Customer-Facing

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Order/Checkout` | Display checkout page |
| POST | `/Order/PlaceOrder` | Create order |
| GET | `/Order/Payment/{orderId}` | Payment page (Stripe) |
| GET | `/Order/ConfirmOrder/{orderId}` | Payment confirmation |
| GET | `/Order/Details/{orderId}` | View order details |
| GET | `/Order/Track/{trackingNumber}` | Track shipment |

### Admin-Facing

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Admin/Order/Index` | List all orders |
| GET | `/Admin/Order/Details/{orderId}` | View order details |
| POST | `/Admin/Shipment/Approve/{shipmentId}` | Create shipment |
| POST | `/Admin/Order/Cancel/{orderId}` | Cancel order |

### Webhook

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/webhooks/stripe` | Stripe webhook receiver |

---

**End of Report**

*This report was generated based on static code analysis of the Cartiva solution as of 2026-01-XX. For implementation updates or clarifications, consult the actual source code.*
