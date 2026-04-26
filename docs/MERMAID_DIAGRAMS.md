# 🎨 Cartiva ERD - Mermaid Diagrams

## 📊 **Complete Entity Relationship Diagram (Mermaid)**

```mermaid
erDiagram
	%% Core User & Company
	ApplicationUser ||--o{ OrderHeader : places
	ApplicationUser ||--o{ ReturnRequest : requests
	ApplicationUser ||--o{ ProductReview : writes
	ApplicationUser ||--o{ ShoppingCart : has
	ApplicationUser ||--o{ Wishlist : has
	ApplicationUser }o--|| Company : "belongs to (optional)"

	Company ||--o{ Invoice : "receives"
	Company ||--o{ AccountsReceivableAdjustment : "has"

	%% Product Catalog
	Category ||--o{ Product : contains
	Product ||--o{ ProductVariant : has
	Product ||--o{ ProductReview : "receives"
	ProductVariant ||--o{ ShoppingCart : "added to"
	ProductVariant ||--o{ Wishlist : "added to"
	ProductVariant ||--o{ OrderDetail : "ordered in"

	%% Order Management
	OrderHeader ||--|| Invoice : "generates"
	OrderHeader ||--o{ OrderDetail : contains
	OrderHeader ||--o{ Shipment : "shipped via"

	OrderDetail ||--o| ReturnRequest : "can be returned"
	OrderDetail ||--|| InvoiceLine : "billed as"
	OrderDetail ||--o| ProductReview : "can be reviewed"

	%% Return Management (Two Paths)
	ReturnRequest ||--o| AccountsReceivableAdjustment : "creates (company deferred)"
	ReturnRequest ||--o| CreditNote : "creates (individual/upfront)"

	%% AR Adjustment
	AccountsReceivableAdjustment }o--|| Company : "adjusts balance for"
	AccountsReceivableAdjustment }o--|| Invoice : "adjusts"

	%% Credit Note
	CreditNote }o--|| OrderHeader : "references"
	CreditNote ||--o{ CreditNoteLine : "contains"
	CreditNoteLine }o--|| OrderDetail : "references"

	%% Invoice
	Invoice ||--o{ InvoiceLine : "contains"
	Invoice ||--o{ AccountsReceivableAdjustment : "adjusted by"

	%% Entities

	ApplicationUser {
		string Id PK
		string UserName
		string Email
		string Name
		string PhoneNumber
		string StreetAddress
		string City
		string State
		string PostalCode
		string Country
		int CompanyId FK
		string Role
		decimal DiscountRate
	}

	Company {
		int Id PK
		string Name
		string Address
		string City
		string State
		string PostalCode
		string Country
		string PhoneNumber
		string OrganizationNumber
		bool IsActive
		string StripeCustomerId
		decimal CreditLimit
		decimal CurrentBalance
		int PaymentTerms
	}

	Category {
		int Id PK
		string Name
		int DisplayOrder
	}

	Product {
		int Id PK
		string Name
		string Description
		string Brand
		string Material
		int CategoryId FK
	}

	ProductVariant {
		int Id PK
		int ProductId FK
		string Color
		string Size
		decimal Price
		int Stock
		string ImageUrl
		string SKU
	}

	OrderHeader {
		int Id PK
		string ApplicationUserId FK
		DateTime OrderDate
		OrderStatus OrderStatus
		PaymentStatus PaymentStatus
		string PaymentMethod
		string PaymentIntentId
		string SessionId
		decimal OrderTotal
		string Name
		string PhoneNumber
		string StreetAddress
		string City
		string State
		string PostalCode
		string Country
	}

	OrderDetail {
		int Id PK
		int OrderHeaderId FK
		int ProductVariantId FK
		int Count
		decimal Price
		string ProductName
		string VariantDescription
		decimal LineTotalIncVat
	}

	ReturnRequest {
		int Id PK
		int OrderDetailId FK
		string ApplicationUserId FK
		string Reason
		string Description
		int Quantity
		ReturnStatus Status
		DateTime RequestDate
		DateTime ResolvedDate
		DateTime RefundDate
		decimal RefundAmount
		string RefundId
		string AdminNote
	}

	AccountsReceivableAdjustment {
		int Id PK
		int CompanyId FK
		int InvoiceId FK
		int ReturnRequestId FK
		decimal Amount
		string Currency
		string Reason
		ARAdjustmentStatus Status
		DateTime CreatedAt
		DateTime AppliedAt
		string CreatedByUserId
		string Notes
		bool StripeCreditBalanceApplied
		string StripeCustomerBalanceReference
	}

	CreditNote {
		int Id PK
		int OrderHeaderId FK
		int ReturnRequestId FK
		string CreatedByUserId
		string CreditNoteNumber
		DateTime IssueDate
		decimal TotalAmount
		CreditNoteStatus Status
		CreditNoteType Type
		string Reason
		string Notes
	}

	CreditNoteLine {
		int Id PK
		int CreditNoteId FK
		int OrderDetailId FK
		string ProductName
		int Quantity
		decimal UnitPrice
		decimal LineTotal
		string Reason
	}

	Invoice {
		int Id PK
		int OrderHeaderId FK
		int CompanyId FK
		string InvoiceNumber
		DateTime InvoiceDate
		DateTime DueDate
		decimal TotalAmount
		string Currency
		InvoiceStatus Status
		bool IsPaid
		DateTime PaidDate
		string Notes
	}

	InvoiceLine {
		int Id PK
		int InvoiceId FK
		int OrderDetailId FK
		string Description
		int Quantity
		decimal UnitPrice
		decimal VatRate
		decimal LineTotal
	}

	Shipment {
		int Id PK
		int OrderHeaderId FK
		string TrackingNumber
		string Carrier
		ShipmentStatus ShipmentStatus
		DateTime ShippedDate
		DateTime EstimatedDelivery
		DateTime DeliveredDate
		string Notes
	}

	ProductReview {
		int Id PK
		int ProductId FK
		string ApplicationUserId FK
		int OrderDetailId FK
		int Rating
		string Title
		string Comment
		DateTime ReviewDate
		bool IsVerifiedPurchase
		int HelpfulCount
	}

	ShoppingCart {
		int Id PK
		string ApplicationUserId FK
		int ProductVariantId FK
		int Count
	}

	Wishlist {
		int Id PK
		string ApplicationUserId FK
		int ProductVariantId FK
		DateTime AddedDate
	}

	Notification {
		int Id PK
		string Type
		string Channel
		string Status
		string Recipient
		string Subject
		string TemplateData
		string ErrorMessage
		int RetryCount
		DateTime CreatedAt
		DateTime ProcessedAt
		DateTime SentAt
		string UserId FK
		string ReferenceId
		string ReferenceType
	}
```

---

## 🔄 **Return Management Flow (Mermaid)**

```mermaid
flowchart TD
	A[Customer Requests Return] --> B[Create ReturnRequest]
	B --> C{Status: Pending}

	C --> D[Admin Reviews]
	D --> E{Decision}

	E -->|Reject| F[Status: Rejected]
	F --> Z[END]

	E -->|Approve| G[Restore Stock]
	G --> H{Customer Type & Payment}

	H -->|Company + Deferred| I[Create AR Adjustment]
	H -->|Company + Upfront| J[Create Credit Note]
	H -->|Individual| K[Create Credit Note]

	I --> L{Status: Approved}
	J --> L
	K --> L

	L --> M[Display in Approved Tab]
	M --> N{Admin Action}

	N -->|Company Deferred| O[Click: Apply AR Adjustment]
	N -->|Others| P[Click: Process Refund]

	O --> Q[Call Stripe Customer Balance API]
	P --> R[Call Stripe Refund API]

	Q --> S[Update AR Adjustment<br/>Status: Applied]
	R --> T[Update RefundId]

	S --> U[Update Return<br/>Status: Refunded]
	T --> U

	U --> V[Move to Resolved Tab]
	V --> W{Display Label}

	W -->|Company Deferred| X[Balance Adjusted]
	W -->|Others| Y[Refunded]

	X --> Z
	Y --> Z

	style C fill:#ffd700
	style L fill:#87ceeb
	style U fill:#90ee90
	style X fill:#98fb98
	style Y fill:#87ceeb
```

---

## 📊 **Order Lifecycle (Mermaid)**

```mermaid
stateDiagram-v2
	[*] --> Pending: Order Created
	Pending --> Approved: Admin Approves
	Approved --> Processing: Start Processing
	Processing --> Shipped: Items Shipped
	Shipped --> Delivered: Customer Receives
	Delivered --> [*]: Order Complete

	Pending --> Cancelled: Customer/Admin Cancels
	Approved --> Cancelled: Admin Cancels
	Cancelled --> [*]

	Delivered --> ReturnRequested: Customer Requests Return
	ReturnRequested --> Refunded: Return Approved & Processed
	Refunded --> [*]

	note right of Pending
		Invoice Created
		Payment Processed (if upfront)
	end note

	note right of Delivered
		Customer can:
		- Leave Review
		- Request Return
	end note

	note right of ReturnRequested
		3-Stage Return Process:
		Pending → Approved → Resolved
	end note
```

---

## 🔄 **Invoice & Payment Flow (Mermaid)**

```mermaid
flowchart TD
	A[Order Created] --> B[Create Invoice]
	B --> C{Payment Method}

	C -->|Pay Now| D[Stripe Payment]
	C -->|Pay Later| E[Skip Stripe]

	D --> F[Invoice Status: Paid]
	E --> G[Invoice Status: Outstanding]

	F --> Z[Order Processing]
	G --> H{Action}

	H -->|Manual Payment| I[Mark as Paid]
	H -->|Overdue| J[Status: Overdue]
	H -->|Return Approved| K[Create AR Adjustment]

	I --> Z
	J --> L[Send Reminder]
	K --> M[Apply Credit Balance]

	M --> N[Reduce Invoice Amount]
	N --> Z

	L --> H

	style F fill:#90ee90
	style G fill:#ffd700
	style J fill:#ff6b6b
	style M fill:#87ceeb
```

---

## 🎯 **AR Adjustment vs Credit Note Decision (Mermaid)**

```mermaid
flowchart TD
	A[Return Approved] --> B{Check Customer Type}

	B -->|Company| C{Check Payment Method}
	B -->|Individual| D[Create Credit Note]

	C -->|Deferred/Pending| E[Create AR Adjustment]
	C -->|Paid/Approved| F[Create Credit Note]

	E --> G[Link to: Company, Invoice, Return]
	E --> H[Amount: Negative]
	E --> I[Status: Approved]

	F --> J[Link to: Order, Return]
	D --> J
	J --> K[Create Credit Note Lines]
	J --> L[Status: Issued]

	I --> M[Display: Apply AR Adjustment Button]
	L --> N[Display: Process Refund Button]

	M --> O[Admin Clicks Apply]
	N --> P[Admin Clicks Process]

	O --> Q[Call Stripe Customer Balance API]
	P --> R[Call Stripe Refund API]

	Q --> S[Status: Applied]
	R --> T[Status: Refunded]

	S --> U[Display: Balance Adjusted]
	T --> V[Display: Refunded]

	style E fill:#ffeb3b
	style F fill:#87ceeb
	style D fill:#87ceeb
	style U fill:#4caf50
	style V fill:#2196f3
```

---

## 📊 **Database Table Relationships (Simplified)**

```mermaid
graph LR
	User[ApplicationUser] -->|0..1| Company
	User -->|1:N| Order[OrderHeader]
	User -->|1:N| Return[ReturnRequest]
	User -->|1:N| Notification

	Company -->|1:N| Invoice
	Company -->|1:N| AR[AR Adjustment]

	Order -->|1:1| Invoice
	Order -->|1:N| Detail[OrderDetail]
	Order -->|1:N| Ship[Shipment]
	Order -->|1:N| CN[Credit Note]

	Detail -->|1:0..1| Return
	Detail -->|1:1| ILine[InvoiceLine]
	Detail -->|1:0..1| Review[ProductReview]

	Return -->|1:0..1| AR
	Return -->|1:0..1| CN

	AR -->|N:1| Invoice
	CN -->|1:N| CNLine[Credit Note Line]

	Invoice -->|1:N| ILine
	Invoice -->|1:N| AR

	Product -->|1:N| Variant[Product Variant]
	Variant -->|1:N| Detail

	style Return fill:#ffeb3b
	style AR fill:#4caf50
	style CN fill:#2196f3
	style Invoice fill:#ff9800
```

---

## 🎯 **Key Takeaways**

### **Relationship Types**
- **1:1** (One-to-One): Order ↔ Invoice
- **1:N** (One-to-Many): Order → OrderDetail
- **N:1** (Many-to-One): OrderDetail → ProductVariant
- **0..1** (Optional): ReturnRequest → AR Adjustment

### **Critical Paths**
1. **Order Creation**: User → Order → Invoice → OrderDetail → InvoiceLine
2. **Return Flow**: OrderDetail → ReturnRequest → AR Adjustment OR Credit Note
3. **Company Flow**: Company → User → Order → Invoice → AR Adjustment

### **Exclusive Relationships**
- Return creates **EITHER** AR Adjustment **OR** Credit Note (never both)
- Determined by: Customer Type + Payment Method

---

**Version**: 5.0  
**Format**: Mermaid  
**Render**: Copy to Mermaid Live Editor or GitHub  
**Last Updated**: 2025  
