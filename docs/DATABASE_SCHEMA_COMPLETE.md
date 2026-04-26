# 🗄️ Cartiva Database Schema - Complete ERD

## 📊 **Complete Entity Relationship Diagram**

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          CARTIVA E-COMMERCE SYSTEM                              │
│                     Complete Database Schema & Relationships                     │
└─────────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════════
							  CORE ENTITIES
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────────────────┐
│   ApplicationUser       │ (Identity - Extended)
├─────────────────────────┤
│ PK: Id (string)         │
│ ───────────────────────  │
│    UserName             │
│    Email                │
│    PhoneNumber          │
│    Name                 │
│    StreetAddress        │
│    City                 │
│    State                │
│    PostalCode           │
│    Country              │
│ FK: CompanyId (int?)    │──┐
│    Role                 │  │
│    DiscountRate         │  │
└─────────────────────────┘  │
							 │
							 │ 0..1 to Many
							 ↓
┌─────────────────────────┐  │
│      Company            │←─┘
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│    Name                 │
│    Address              │
│    City                 │
│    State                │
│    PostalCode           │
│    Country              │
│    PhoneNumber          │
│    OrganizationNumber   │
│    IsActive             │
│    StripeCustomerId     │◄──── (For AR Adjustments)
│    CreditLimit          │
│    CurrentBalance       │
│    PaymentTerms         │
└─────────────────────────┘


═══════════════════════════════════════════════════════════════════════════════════
							PRODUCT CATALOG
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────────────────┐
│       Category          │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│    Name                 │
│    DisplayOrder         │
└─────────────────────────┘
			│
			│ 1 to Many
			↓
┌─────────────────────────┐
│       Product           │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│    Name                 │
│    Description          │
│    Brand                │
│    Material             │
│ FK: CategoryId (int)    │
└─────────────────────────┘
			│
			│ 1 to Many
			↓
┌─────────────────────────┐
│    ProductVariant       │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: ProductId (int)     │
│    Color                │
│    Size                 │
│    Price                │
│    Stock                │
│    ImageUrl             │
│    SKU                  │
└─────────────────────────┘


═══════════════════════════════════════════════════════════════════════════════════
						  ORDER MANAGEMENT
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────────────────┐
│     OrderHeader         │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: ApplicationUserId   │──┐
│    OrderDate            │  │
│    OrderStatus (enum)   │  │
│    PaymentStatus (enum) │  │◄──── (Pending, Approved, Deferred, Paid, Refunded)
│    PaymentMethod        │  │
│    PaymentIntentId      │  │
│    SessionId            │  │
│    OrderTotal           │  │
│    Name                 │  │
│    PhoneNumber          │  │
│    StreetAddress        │  │
│    City, State          │  │
│    PostalCode, Country  │  │
└─────────────────────────┘  │
			│                 │
			│ 1 to Many       │ 1 to Many
			↓                 │
┌─────────────────────────┐  │
│     OrderDetail         │  │
├─────────────────────────┤  │
│ PK: Id (int)            │  │
│ ───────────────────────  │  │
│ FK: OrderHeaderId (int) │  │
│ FK: ProductVariantId    │  │
│    Count                │  │
│    Price                │  │
│    ProductName          │  │
│    VariantDescription   │  │
│    LineTotalIncVat      │  │
└─────────────────────────┘  │
			│                 │
			│ 1 to 0..1       │
			↓                 │
┌─────────────────────────┐  │
│    ReturnRequest        │  │
├─────────────────────────┤  │
│ PK: Id (int)            │  │
│ ───────────────────────  │  │
│ FK: OrderDetailId (int) │  │
│ FK: ApplicationUserId   │──┘
│    Reason               │
│    Description          │
│    Quantity             │
│    Status (enum)        │◄──── (Pending, Approved, Refunded, Rejected)
│    RequestDate          │
│    ResolvedDate         │
│    RefundDate           │
│    RefundAmount         │
│    RefundId             │
│    AdminNote            │
└─────────────────────────┘
			│
			│ 1 to 0..1 (Company Deferred)
			↓
┌─────────────────────────┐
│  AccountsReceivable     │
│     Adjustment          │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: CompanyId (int)     │──────┐
│ FK: InvoiceId (int)     │      │
│ FK: ReturnRequestId(int)│      │
│    Amount (decimal)     │      │
│    Currency             │      │
│    Reason               │      │
│    Status (enum)        │◄──── (Approved, Applied, Rejected)
│    CreatedAt            │      │
│    AppliedAt            │      │
│    CreatedByUserId      │      │
│    Notes                │      │
│    StripeCreditBalance  │      │
│       Applied (bool)    │      │
│    StripeCustomerBalance│      │
│       Reference         │      │
└─────────────────────────┘      │
			│                     │
			│ (Alternative)       │
			│ 1 to 0..1 (Non-AR) │
			↓                     │
┌─────────────────────────┐      │
│      CreditNote         │      │
├─────────────────────────┤      │
│ PK: Id (int)            │      │
│ ───────────────────────  │      │
│ FK: OrderHeaderId (int) │      │
│ FK: ReturnRequestId(int)│      │
│ FK: CreatedByUserId     │      │
│    CreditNoteNumber     │      │
│    IssueDate            │      │
│    TotalAmount          │      │
│    Status (enum)        │◄──── (Issued, Applied, Voided)
│    Type (enum)          │◄──── (Return, Cancellation)
│    Reason               │      │
│    Notes                │      │
└─────────────────────────┘      │
			│                     │
			│ 1 to Many           │
			↓                     │
┌─────────────────────────┐      │
│   CreditNoteLine        │      │
├─────────────────────────┤      │
│ PK: Id (int)            │      │
│ ───────────────────────  │      │
│ FK: CreditNoteId (int)  │      │
│ FK: OrderDetailId (int) │      │
│    ProductName          │      │
│    Quantity             │      │
│    UnitPrice            │      │
│    LineTotal            │      │
│    Reason               │      │
└─────────────────────────┘      │
								  │
								  │
═══════════════════════════════════════════════════════════════════════════════════
						  INVOICING & PAYMENT
═══════════════════════════════════════════════════════════════════════════════════
								  │
								  │
┌─────────────────────────┐      │
│       Invoice           │◄─────┘
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: OrderHeaderId (int) │──────┐
│ FK: CompanyId (int?)    │      │
│    InvoiceNumber        │      │
│    InvoiceDate          │      │
│    DueDate              │      │
│    TotalAmount          │      │
│    Currency             │      │
│    Status (enum)        │◄──── (Outstanding, Paid, Overdue, Cancelled)
│    IsPaid               │      │
│    PaidDate             │      │
│    Notes                │      │
└─────────────────────────┘      │
			│                     │
			│ 1 to Many           │
			↓                     │
┌─────────────────────────┐      │
│      InvoiceLine        │      │
├─────────────────────────┤      │
│ PK: Id (int)            │      │
│ ───────────────────────  │      │
│ FK: InvoiceId (int)     │      │
│ FK: OrderDetailId (int) │      │
│    Description          │      │
│    Quantity             │      │
│    UnitPrice            │      │
│    VatRate              │      │
│    LineTotal            │      │
└─────────────────────────┘      │
								  │
								  │
═══════════════════════════════════════════════════════════════════════════════════
						  SHIPPING & LOGISTICS
═══════════════════════════════════════════════════════════════════════════════════
								  │
┌─────────────────────────┐      │
│       Shipment          │◄─────┘
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: OrderHeaderId (int) │
│    TrackingNumber       │
│    Carrier              │
│    ShipmentStatus (enum)│◄──── (Pending, Shipped, InTransit, Delivered)
│    ShippedDate          │
│    EstimatedDelivery    │
│    DeliveredDate        │
│    Notes                │
└─────────────────────────┘


═══════════════════════════════════════════════════════════════════════════════════
						  REVIEWS & FEEDBACK
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────────────────┐
│    ProductReview        │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: ProductId (int)     │
│ FK: ApplicationUserId   │
│ FK: OrderDetailId (int) │
│    Rating (1-5)         │
│    Title                │
│    Comment              │
│    ReviewDate           │
│    IsVerifiedPurchase   │
│    HelpfulCount         │
└─────────────────────────┘


═══════════════════════════════════════════════════════════════════════════════════
						  SHOPPING & WISHLIST
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────────────────┐
│    ShoppingCart         │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: ApplicationUserId   │
│ FK: ProductVariantId    │
│    Count                │
└─────────────────────────┘

┌─────────────────────────┐
│      Wishlist           │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: ApplicationUserId   │
│ FK: ProductVariantId    │
│    AddedDate            │
└─────────────────────────┘

┌─────────────────────────┐
│     Notification        │
├─────────────────────────┤
│ PK: Id (int)            │
│ ───────────────────────  │
│ FK: UserId (string?)    │
│    Type (enum)          │◄──── (OrderPlaced, OrderShipped, etc.)
│    Channel (enum)       │◄──── (Email, SMS)
│    Status (enum)        │◄──── (Pending, Sent, Failed)
│    Recipient            │
│    Subject              │
│    TemplateData         │
│    ErrorMessage         │
│    RetryCount           │
│    CreatedAt            │
│    ProcessedAt          │
│    SentAt               │
│    ReferenceId          │
│    ReferenceType        │
└─────────────────────────┘
```

---

## 🔗 **Key Relationships Summary**

### **1. User & Company** (0..1 to Many)
```
Company (1) ←─────── ApplicationUser (Many)
- Company can have multiple users
- User can belong to 0 or 1 company
```

### **2. Order Hierarchy** (1 to Many)
```
ApplicationUser (1) ─→ OrderHeader (Many)
OrderHeader (1) ─→ OrderDetail (Many)
OrderDetail (1) ─→ ReturnRequest (0..1)
```

### **3. Product Hierarchy** (1 to Many)
```
Category (1) ─→ Product (Many)
Product (1) ─→ ProductVariant (Many)
```

### **4. Return Flow - Two Paths**

**Path A: Company Deferred (AR Adjustment)**
```
ReturnRequest (1) ─→ AccountsReceivableAdjustment (0..1)
						  ↓
					  Company (via FK)
						  ↓
					  Invoice (via FK)
```

**Path B: Individual/Company Upfront (Credit Note)**
```
ReturnRequest (1) ─→ CreditNote (0..1)
						  ↓
				   CreditNoteLine (Many)
```

### **5. Invoice & Order** (1 to 1)
```
OrderHeader (1) ←→ Invoice (1)
- Every order has one invoice
- Every invoice belongs to one order
```

### **6. Shipping** (1 to Many)
```
OrderHeader (1) ─→ Shipment (Many)
- Order can have multiple shipments
- Each shipment belongs to one order
```

---

## 📊 **Cardinality Reference**

| Symbol | Meaning |
|--------|---------|
| `(1)` | Exactly one |
| `(0..1)` | Zero or one (optional) |
| `(Many)` | One or more |
| `(0..Many)` | Zero or more |
| `─→` | One-to-Many |
| `←→` | One-to-One |
| `←─` | Many-to-One |

---

## 🎯 **Critical Relationships**

### **Return Management**
```
ReturnRequest
	├─→ OrderDetail (Many-to-One, Required)
	├─→ ApplicationUser (Many-to-One, Required)
	├─→ AccountsReceivableAdjustment (One-to-ZeroOrOne)
	└─→ CreditNote (One-to-ZeroOrOne)

Note: EITHER AR Adjustment OR Credit Note, never both
```

### **AR Adjustment**
```
AccountsReceivableAdjustment
	├─→ Company (Many-to-One, Required)
	├─→ Invoice (Many-to-One, Required)
	└─→ ReturnRequest (One-to-One, Optional)
```

### **Credit Note**
```
CreditNote
	├─→ OrderHeader (Many-to-One, Required)
	├─→ ReturnRequest (One-to-One, Optional)
	└─→ CreditNoteLine (One-to-Many)
```

---

**Version**: 5.0  
**Last Updated**: 2025  
**Status**: Complete Schema  
