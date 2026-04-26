# 🔄 Cartiva System Flows - Complete Diagrams

## 📊 **Order & Return Management Flows**

```
═══════════════════════════════════════════════════════════════════════════════════
						  COMPLETE ORDER LIFECYCLE
═══════════════════════════════════════════════════════════════════════════════════

┌─────────────┐
│   CUSTOMER  │
│   BROWSING  │
└──────┬──────┘
	   │
	   ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                        SHOPPING PHASE                                    │
└─────────────────────────────────────────────────────────────────────────┘
	   │
	   ├─→ [Browse Products] → [ProductVariant] → [Add to Cart/Wishlist]
	   │                              │
	   │                              ↓
	   │                    ┌──────────────────┐
	   │                    │  ShoppingCart    │
	   │                    │  (per user)      │
	   │                    └──────────────────┘
	   │
	   ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                        CHECKOUT PHASE                                    │
└─────────────────────────────────────────────────────────────────────────┘
	   │
	   ├─→ Is Company User?
	   │       │
	   │       ├─→ YES → Choose Payment Method
	   │       │         ├─→ Pay Now (Upfront) → Stripe
	   │       │         └─→ Pay Later (Deferred) → Skip Stripe
	   │       │
	   │       └─→ NO → Individual → Stripe Payment
	   │
	   ↓
   [Create OrderHeader]
	   │
	   ├─→ PaymentStatus = Approved (upfront) or Deferred
	   ├─→ OrderStatus = Pending
	   ├─→ Create OrderDetail(s)
	   │
	   ↓
   [Create Invoice]
	   │
	   ├─→ InvoiceStatus = Paid (if upfront) or Outstanding (if deferred)
	   ├─→ Create InvoiceLine(s)
	   │
	   ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                      FULFILLMENT PHASE                                   │
└─────────────────────────────────────────────────────────────────────────┘
	   │
	   ├─→ Admin: Process Order
	   │       ↓
	   │   OrderStatus = Approved
	   │       ↓
	   │   [Create Shipment]
	   │       ↓
	   │   OrderStatus = Shipped
	   │       ↓
	   │   ShipmentStatus = Shipped
	   │       ↓
	   │   Track Delivery
	   │       ↓
	   │   OrderStatus = Delivered
	   │       ↓
	   │   ShipmentStatus = Delivered
	   │
	   ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                     POST-DELIVERY PHASE                                  │
└─────────────────────────────────────────────────────────────────────────┘
	   │
	   ├─→ Customer: Leave Review
	   │   [Create ProductReview]
	   │
	   └─→ Customer: Request Return
		   [See RETURN FLOW below]


═══════════════════════════════════════════════════════════════════════════════════
					RETURN MANAGEMENT FLOW (3-STAGE)
═══════════════════════════════════════════════════════════════════════════════════

┌──────────────────┐
│  CUSTOMER        │
│  Request Return  │
└────────┬─────────┘
		 │
		 ↓
	[Create ReturnRequest]
		 │
		 ├─→ Status = Pending
		 ├─→ Link to OrderDetail
		 ├─→ Specify: Reason, Quantity, Description
		 │
		 ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                        STAGE 1: PENDING                                  │
└─────────────────────────────────────────────────────────────────────────┘
		 │
		 ├─→ [Admin Reviews Return]
		 │
		 ├─→ Decision?
		 │       │
		 │       ├─→ REJECT → Status = Rejected → END
		 │       │
		 │       └─→ APPROVE
		 │               │
		 │               ↓
		 │          [Restore Stock]
		 │               │
		 │               ├─→ ProductVariant.Stock += Quantity
		 │               │
		 │               ↓
		 │          Check Customer Type & Payment
		 │               │
		 │               ├─────────────────┬─────────────────┐
		 │               │                 │                 │
		 │           Company           Company           Individual
		 │          + Deferred         + Upfront         
		 │               │                 │                 │
		 │               ↓                 ↓                 ↓
		 │      [Create AR Adj]   [Create CreditNote] [Create CreditNote]
		 │               │                 │                 │
		 │               ├─→ Status = Approved (AR Adj)
		 │               ├─→ Link to: Company, Invoice, ReturnRequest
		 │               ├─→ Amount = -(refund amount)
		 │               │
		 │               ↓
		 │          Status = Approved (Return)
		 │               │
		 │               ↓
		 ↓               ↓                 ↓                 ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                        STAGE 2: APPROVED                                 │
└─────────────────────────────────────────────────────────────────────────┘
		 │
		 ├─→ Display in "Approved" tab
		 │
		 ├─→ Show Action Button:
		 │       │
		 │       ├─→ Company Deferred: [Apply AR Adjustment]
		 │       └─→ Others: [Process Refund]
		 │
		 │
		 ├─────────────────┬─────────────────┐
		 │                 │                 │
	[Apply AR Adj]   [Process Refund]  [Process Refund]
		 │                 │                 │
		 ↓                 ↓                 ↓
	Call Stripe      Call Stripe      Call Stripe
	Customer         Refund           Refund
	Balance          Service          Service
	Transaction                           │
		 │                 │                 │
		 ├─→ StripeCreditBalanceApplied = true
		 ├─→ AR Adj Status = Applied
		 ├─→ AR Adj AppliedAt = now
		 │                 │                 │
		 ├─────────────────┴─────────────────┤
		 │                                   │
		 ↓                                   ↓
	Status = Refunded (Return)       Status = Refunded (Return)
	RefundDate = now                 RefundDate = now
		 │                                   │
		 ↓                                   ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                        STAGE 3: RESOLVED                                 │
└─────────────────────────────────────────────────────────────────────────┘
		 │
		 ├─→ Display in "Resolved" tab
		 │
		 ├─→ Show Completion Status:
		 │       │
		 │       ├─→ Company Deferred: "Balance Adjusted"
		 │       └─→ Others: "Refunded"
		 │
		 ↓
	   [END]


═══════════════════════════════════════════════════════════════════════════════════
					INVOICE & PAYMENT FLOW (COMPANY)
═══════════════════════════════════════════════════════════════════════════════════

┌──────────────────┐
│  Company Order   │
│  Placed          │
└────────┬─────────┘
		 │
		 ↓
	[Create Invoice]
		 │
		 ├─→ Link to OrderHeader
		 ├─→ Link to Company
		 ├─→ InvoiceNumber = auto-generated
		 ├─→ InvoiceDate = now
		 ├─→ DueDate = InvoiceDate + PaymentTerms
		 │
		 ↓
	Payment Method?
		 │
		 ├─────────────────┬─────────────────┐
		 │                 │                 │
	Pay Now          Pay Later        Pay Later
	(Upfront)        (Deferred)       (Deferred)
		 │                 │                 │
		 ↓                 ↓                 ↓
	Stripe          No Stripe       No Stripe
	Payment         Payment         Payment
		 │                 │                 │
		 ├─→ Status = Paid              Status = Outstanding
		 ├─→ IsPaid = true              IsPaid = false
		 ├─→ PaidDate = now
		 │                 │                 │
		 ↓                 ↓                 ↓
					[Invoice Sent]
						   │
						   ↓
					Admin/System Action?
						   │
						   ├─→ Mark as Paid
						   │   ├─→ Status = Paid
						   │   ├─→ IsPaid = true
						   │   └─→ PaidDate = now
						   │
						   ├─→ Invoice Overdue
						   │   └─→ Status = Overdue
						   │
						   └─→ Return Approved
							   └─→ [AR Adjustment Flow]
									   │
									   ↓
								   Reduces Balance
									   │
									   ↓
							   Invoice Amount Adjusted


═══════════════════════════════════════════════════════════════════════════════════
					AR ADJUSTMENT DETAILED FLOW
═══════════════════════════════════════════════════════════════════════════════════

[Return Approved for Company Deferred]
		 │
		 ↓
	[Create AccountsReceivableAdjustment]
		 │
		 ├─→ CompanyId = from Order
		 ├─→ InvoiceId = from Order
		 ├─→ ReturnRequestId = return ID
		 ├─→ Amount = -(refund amount)  [NEGATIVE to reduce AR]
		 ├─→ Status = Approved
		 ├─→ CreatedAt = now
		 │
		 ↓
	[Saved to Database]
		 │
		 ↓
	[Admin Clicks "Apply AR Adjustment"]
		 │
		 ↓
	[FinalizeARAdjustmentReturnAsync()]
		 │
		 ├─→ Find AR Adjustment
		 ├─→ Check Company has StripeCustomerId
		 │       │
		 │       ├─→ YES → Call Stripe API
		 │       │         │
		 │       │         ↓
		 │       │    [ApplyStripeCreditBalanceAsync()]
		 │       │         │
		 │       │         ├─→ Create CustomerBalanceTransaction
		 │       │         ├─→ Amount = abs(adjustment.Amount) * 100
		 │       │         ├─→ Currency = NOK/USD
		 │       │         ├─→ Description = reason
		 │       │         │
		 │       │         ↓
		 │       │    [Stripe Response]
		 │       │         │
		 │       │         ├─→ StripeCreditBalanceApplied = true
		 │       │         ├─→ StripeCustomerBalanceReference = transaction.Id
		 │       │         ├─→ Status = Applied
		 │       │         └─→ AppliedAt = now
		 │       │
		 │       └─→ NO → Skip Stripe
		 │                 │
		 │                 ├─→ Status = Applied
		 │                 └─→ AppliedAt = now
		 │
		 ↓
	[Update Return]
		 │
		 ├─→ ReturnRequest.Status = Refunded
		 ├─→ ReturnRequest.RefundDate = now
		 │
		 ↓
	[Move to Resolved Tab]


═══════════════════════════════════════════════════════════════════════════════════
					CREDIT NOTE DETAILED FLOW
═══════════════════════════════════════════════════════════════════════════════════

[Return Approved for Individual/Company Upfront]
		 │
		 ↓
	[Create CreditNote]
		 │
		 ├─→ OrderHeaderId = from return
		 ├─→ ReturnRequestId = return ID
		 ├─→ CreditNoteNumber = auto-generated
		 ├─→ IssueDate = now
		 ├─→ TotalAmount = refund amount
		 ├─→ Status = Issued
		 ├─→ Type = Return
		 │
		 ↓
	[Create CreditNoteLine(s)]
		 │
		 ├─→ CreditNoteId = credit note ID
		 ├─→ OrderDetailId = from return
		 ├─→ ProductName, Quantity, UnitPrice
		 ├─→ LineTotal = Quantity * UnitPrice
		 │
		 ↓
	[Saved to Database]
		 │
		 ↓
	[Admin Clicks "Process Refund"]
		 │
		 ↓
	[ProcessRefundAsync()]
		 │
		 ├─→ Find ReturnRequest
		 ├─→ Check PaymentIntentId exists
		 │       │
		 │       ├─→ YES → Call Stripe Refund
		 │       │         │
		 │       │         ↓
		 │       │    [Stripe RefundService.CreateAsync()]
		 │       │         │
		 │       │         ├─→ PaymentIntent = order.PaymentIntentId
		 │       │         ├─→ Amount = refundAmount * 100
		 │       │         │
		 │       │         ↓
		 │       │    [Stripe Response]
		 │       │         │
		 │       │         ├─→ RefundId = refund.Id
		 │       │         └─→ Status = succeeded/pending
		 │       │
		 │       └─→ NO → Manual Transfer Required
		 │
		 ↓
	[Update Return]
		 │
		 ├─→ ReturnRequest.Status = Refunded
		 ├─→ ReturnRequest.RefundDate = now
		 ├─→ ReturnRequest.RefundId = Stripe refund ID
		 │
		 ↓
	[Move to Resolved Tab]


═══════════════════════════════════════════════════════════════════════════════════
					DATA FLOW SUMMARY
═══════════════════════════════════════════════════════════════════════════════════

OrderHeader
	├─→ Invoice (1:1)
	├─→ OrderDetail (1:Many)
	│       ├─→ ReturnRequest (1:0..1)
	│       │       ├─→ AccountsReceivableAdjustment (1:0..1) [Company Deferred]
	│       │       └─→ CreditNote (1:0..1) [Individual/Company Upfront]
	│       │               └─→ CreditNoteLine (1:Many)
	│       │
	│       ├─→ InvoiceLine (1:1)
	│       └─→ ProductReview (1:0..1)
	│
	└─→ Shipment (1:Many)

Company
	├─→ ApplicationUser (1:Many)
	├─→ Invoice (1:Many)
	└─→ AccountsReceivableAdjustment (1:Many)

ApplicationUser
	├─→ OrderHeader (1:Many)
	├─→ ReturnRequest (1:Many)
	├─→ ProductReview (1:Many)
	├─→ ShoppingCart (1:Many)
	└─→ Wishlist (1:Many)

Product
	├─→ ProductVariant (1:Many)
	└─→ ProductReview (1:Many)

```

---

## 🎯 **Critical Flow Rules**

### **Return Flow Rules**
1. **One return per OrderDetail** - Can't return same item twice
2. **EITHER AR Adjustment OR Credit Note** - Never both
3. **3-Stage Process** - Pending → Approved → Resolved
4. **Stock restored on Approve** - Not on request
5. **Stripe called in Stage 2** - Not during approval

### **Invoice Flow Rules**
1. **One invoice per order** - Always created
2. **Invoice created on order placement** - Not on payment
3. **Status based on payment method** - Paid (upfront) or Outstanding (deferred)
4. **Due date = Invoice date + Payment terms** - Calculated automatically

### **AR Adjustment Rules**
1. **Only for company deferred** - Not for upfront or individual
2. **Negative amount** - Reduces accounts receivable
3. **Links to Invoice** - Adjusts specific invoice
4. **Stripe optional** - Depends on StripeCustomerId

### **Credit Note Rules**
1. **For individual + company upfront** - Not for deferred
2. **Has line items** - One per returned product
3. **Type: Return or Cancellation** - Different workflows
4. **Status: Issued → Applied/Voided** - Lifecycle tracking

---

**Version**: 5.0  
**Last Updated**: 2025  
**Status**: Complete Flows  
