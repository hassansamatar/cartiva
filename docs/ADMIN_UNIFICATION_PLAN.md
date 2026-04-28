# 📋 Admin Management Pages Unification - Implementation Plan (Revised)

## 🎯 Goal
Create a consistent, reusable UI pattern across all admin document management modules (Orders, Invoices, Credit Notes, AR Adjustments, Shipments) with unified actions and email functionality using the existing Notifications system.

---

## 📊 Current State Analysis

### ✅ **Invoice Module (Reference Implementation)**
- **Status**: Complete ✅
- **Actions**: View, Print, Send Email
- **Email**: Dynamic rendering (paid vs unpaid) via Notifications system ✅
- **UI**: Dashboard with cards, tabbed sections, DataTables ✅
- **Pattern**: **Use as baseline for UI and email**

### ✅ **Order Module**
- **Status**: Good shape ✅
- **Actions**: View, Print
- **Email**: Auto-sent via Notifications system ✅
- **Gap**: Just need "Resend Email" link (no new page)
- **Logic**: ✅ Keep as-is

### ✅ **Shipment Module**
- **Status**: Good shape ✅
- **Email**: Already implemented ✅
- **Gap**: UI consistency only
- **Logic**: ✅ Keep as-is

### ⚠️ **Credit Note Module**
- **Status**: Needs email + UI
- **Actions**: View, Print (partial)
- **Email**: **Not implemented** - Need to add using invoice pattern
- **UI**: Needs consistency
- **Logic**: Add email functionality

### ⚠️ **AR Adjustment Module**
- **Status**: Needs email + UI
- **Actions**: View (basic)
- **Email**: **Not implemented** - Need to add using invoice pattern
- **UI**: Needs consistency
- **Logic**: Add email functionality

---

## 🏗️ Revised Architecture Design

### **What We're NOT Changing**
- ❌ No new DocumentEmailService (use existing NotificationService)
- ❌ No changes to Order/Shipment logic (already good)
- ❌ No new confirmation pages

### **What We're Building**

```
src/
├── cartivaWeb/
│   ├── Areas/Admin/
│   │   ├── ViewComponents/
│   │   │   ├── DocumentActionBar.cs          # NEW - Reusable action bar UI
│   │   │   └── DocumentActionBarViewModel.cs # NEW - Action configuration
│   │   ├── Views/
│   │   │   └── Shared/
│   │   │       └── Components/
│   │   │           └── DocumentActionBar/
│   │   │               └── Default.cshtml     # NEW - Action bar view
│   │   ├── Controllers/
│   │   │   ├── CreditNoteController.cs        # UPDATE - Add SendEmail action
│   │   │   ├── ARAdjustmentController.cs      # UPDATE - Add SendEmail action
│   │   │   ├── OrderController.cs             # UPDATE - Add ResendEmail action
│   │   │   └── ShipmentController.cs          # UPDATE - UI only
│   └── wwwroot/css/
│       └── admin-document-actions.css         # NEW - Action bar styles
│
└── Cartiva.Infrastructure/
	└── Templates/
		├── CreditNoteGenerated.cshtml         # NEW - Follow invoice pattern
		└── ARAdjustmentNotification.cshtml    # NEW - Follow invoice pattern
```

---

## 📝 Implementation Steps (Revised)

### **Phase 1: Shared UI Components**

#### **Step 1.1: Create Document Action Bar Component**
**File**: `src/cartivaWeb/Areas/Admin/ViewComponents/DocumentActionBar.cs`

**Purpose**: Reusable UI component for consistent View/Print/Email actions

**ViewModel**:
```csharp
public class DocumentActionBarViewModel
{
	public int DocumentId { get; set; }
	public string DocumentType { get; set; } // "Invoice", "Order", "CreditNote", "ARAdjustment", "Shipment"
	public string DocumentNumber { get; set; }
	public bool ShowView { get; set; } = true;
	public bool ShowPrint { get; set; } = true;
	public bool ShowEmail { get; set; } = true;
	public bool ShowResend { get; set; } = false; // For Order/Shipment
	public bool EmailSent { get; set; } = false;
	public string? EmailRecipient { get; set; }
	public string? StatusBadgeClass { get; set; }
	public string? StatusText { get; set; }
}
```

**Actions**:
- View: `/{Area}/{DocumentType}/Details/{id}`
- Print: `/{Area}/{DocumentType}/Print/{id}`
- Email: `/{Area}/{DocumentType}/SendEmail/{id}` (POST)
- Resend: `/{Area}/{DocumentType}/ResendEmail/{id}` (POST) - Order/Shipment only

---

#### **Step 1.2: Create Action Bar View**
**File**: `src/cartivaWeb/Areas/Admin/Views/Shared/Components/DocumentActionBar/Default.cshtml`

**Pattern**:
```html
@model DocumentActionBarViewModel

<div class="document-action-bar">
	<div class="document-info">
		<h5>@Model.DocumentType #@Model.DocumentNumber</h5>
		@if (!string.IsNullOrEmpty(Model.StatusText))
		{
			<span class="badge @Model.StatusBadgeClass">@Model.StatusText</span>
		}
		@if (Model.EmailSent && !string.IsNullOrEmpty(Model.EmailRecipient))
		{
			<small class="text-success ms-2">
				<i class="bi bi-check-circle"></i> Sent to @Model.EmailRecipient
			</small>
		}
	</div>

	<div class="action-buttons">
		@if (Model.ShowView)
		{
			<a asp-area="Admin" asp-controller="@Model.DocumentType" asp-action="Details" asp-route-id="@Model.DocumentId" 
			   class="btn btn-info" title="View Details">
				<i class="bi bi-eye"></i> View
			</a>
		}
		@if (Model.ShowPrint)
		{
			<a asp-area="Admin" asp-controller="@Model.DocumentType" asp-action="Print" asp-route-id="@Model.DocumentId" 
			   class="btn btn-secondary" title="Print" target="_blank">
				<i class="bi bi-printer"></i> Print
			</a>
		}
		@if (Model.ShowEmail)
		{
			<form asp-area="Admin" asp-controller="@Model.DocumentType" asp-action="SendEmail" method="post" class="d-inline">
				<input type="hidden" name="id" value="@Model.DocumentId" />
				<button type="submit" class="btn btn-primary" title="Send Email">
					<i class="bi bi-envelope"></i> Send Email
				</button>
			</form>
		}
		@if (Model.ShowResend)
		{
			<form asp-area="Admin" asp-controller="@Model.DocumentType" asp-action="ResendEmail" method="post" class="d-inline">
				<input type="hidden" name="id" value="@Model.DocumentId" />
				<button type="submit" class="btn btn-outline-primary" title="Resend Email">
					<i class="bi bi-arrow-repeat"></i> Resend
				</button>
			</form>
		}
	</div>
</div>
```

---

#### **Step 1.3: Create Shared CSS**
**File**: `src/cartivaWeb/wwwroot/css/admin-document-actions.css`

```css
.document-action-bar {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 1.25rem;
	background: #fff;
	border-radius: 8px;
	box-shadow: 0 2px 4px rgba(0,0,0,0.1);
	margin-bottom: 1.5rem;
	border: 1px solid #e9ecef;
}

.document-info h5 {
	margin: 0;
	font-weight: 600;
	color: #1a1a2e;
}

.action-buttons {
	display: flex;
	gap: 0.5rem;
}

.action-buttons .btn {
	display: inline-flex;
	align-items: center;
	gap: 0.375rem;
}
```

---

### **Phase 2: Add Email Functionality**

#### **Step 2.1: Create Credit Note Email Template**
**File**: `src/Cartiva.Infrastructure/Templates/CreditNoteGenerated.cshtml`

**Purpose**: Email template for credit note notifications

**Pattern**: Follow `InvoiceGenerated.cshtml` structure

**Template Data** (passed to NotificationService):
```csharp
{
	["creditNoteId"] = creditNote.Id.ToString(),
	["creditNoteNumber"] = creditNote.CreditNoteNumber,
	["orderId"] = creditNote.OrderHeaderId?.ToString() ?? string.Empty,
	["issueDate"] = creditNote.IssueDate.ToString("yyyy-MM-dd"),
	["totalAmount"] = creditNote.TotalAmount.ToString(CultureInfo.InvariantCulture),
	["netAmount"] = creditNote.NetAmount.ToString(CultureInfo.InvariantCulture),
	["vatAmount"] = creditNote.VatAmount.ToString(CultureInfo.InvariantCulture),
	["currency"] = creditNote.Currency,
	["status"] = creditNote.Status.ToString(),
	["reason"] = creditNote.Reason ?? string.Empty,
	["customerName"] = creditNote.CustomerName,
	["customerEmail"] = creditNote.CustomerEmail ?? string.Empty,
	// ... other fields
}
```

**Subject**: `"Credit Note {creditNoteNumber} - {totalAmount} {currency}"`

---

#### **Step 2.2: Create AR Adjustment Email Template**
**File**: `src/Cartiva.Infrastructure/Templates/ARAdjustmentNotification.cshtml`

**Purpose**: Email template for AR adjustment notifications

**Template Data**:
```csharp
{
	["adjustmentId"] = adjustment.Id.ToString(),
	["companyName"] = company.Name,
	["amount"] = adjustment.Amount.ToString(CultureInfo.InvariantCulture),
	["currency"] = adjustment.Currency,
	["reason"] = adjustment.Reason,
	["status"] = adjustment.Status.ToString(),
	["createdAt"] = adjustment.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
	["appliedAt"] = adjustment.AppliedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
	["invoiceNumber"] = invoice?.InvoiceNumber ?? string.Empty,
	["notes"] = adjustment.Notes ?? string.Empty,
	// ... other fields
}
```

**Subject**: `"AR Adjustment Notification - {amount} {currency} - {companyName}"`

---

#### **Step 2.3: Update Credit Note Controller**
**File**: `src/cartivaWeb/Areas/Admin/Controllers/CreditNoteController.cs`

**Add Action**:
```csharp
[HttpPost]
public async Task<IActionResult> SendEmail(int id)
{
	var creditNote = await _db.CreditNotes
		.Include(cn => cn.OrderHeader)
			.ThenInclude(o => o.ApplicationUser)
		.FirstOrDefaultAsync(cn => cn.Id == id);

	if (creditNote == null)
		return NotFound();

	if (string.IsNullOrWhiteSpace(creditNote.CustomerEmail))
	{
		TempData["Error"] = "Cannot send email: No customer email address.";
		return RedirectToAction(nameof(Index));
	}

	try
	{
		await _notificationService.SendAsync(new NotificationRequest(
			Recipient: creditNote.CustomerEmail,
			Type: NotificationType.CreditNoteGenerated,
			TemplateData: new Dictionary<string, object>
			{
				["creditNoteId"] = creditNote.Id.ToString(),
				["creditNoteNumber"] = creditNote.CreditNoteNumber,
				["orderId"] = creditNote.OrderHeaderId?.ToString() ?? string.Empty,
				["issueDate"] = creditNote.IssueDate.ToString("yyyy-MM-dd"),
				["totalAmount"] = creditNote.TotalAmount.ToString(CultureInfo.InvariantCulture),
				["netAmount"] = creditNote.NetAmount.ToString(CultureInfo.InvariantCulture),
				["vatAmount"] = creditNote.VatAmount.ToString(CultureInfo.InvariantCulture),
				["currency"] = creditNote.Currency,
				["status"] = creditNote.Status.ToString(),
				["reason"] = creditNote.Reason ?? string.Empty,
				["customerName"] = creditNote.CustomerName,
				["customerEmail"] = creditNote.CustomerEmail ?? string.Empty
			},
			UserId: creditNote.OrderHeader?.ApplicationUserId,
			ReferenceId: creditNote.Id.ToString(),
			ReferenceType: "CreditNote",
			Subject: $"Credit Note {creditNote.CreditNoteNumber} - {creditNote.TotalAmount:C} {creditNote.Currency}"
		));

		// Mark as sent (add EmailSent field to CreditNote if needed)
		creditNote.EmailSent = true;
		creditNote.EmailSentDate = DateTime.UtcNow;
		await _db.SaveChangesAsync();

		TempData["Success"] = $"Credit note {creditNote.CreditNoteNumber} sent successfully.";
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Failed to send credit note email for ID {Id}", id);
		TempData["Error"] = "Failed to send email.";
	}

	return RedirectToAction(nameof(Index));
}
```

---

#### **Step 2.4: Update AR Adjustment Controller**
**File**: `src/cartivaWeb/Areas/Admin/Controllers/ARAdjustmentController.cs`

**Add Action**:
```csharp
[HttpPost]
public async Task<IActionResult> SendEmail(int id)
{
	var adjustment = await _db.AccountsReceivableAdjustments
		.Include(a => a.Company)
		.Include(a => a.Invoice)
		.FirstOrDefaultAsync(a => a.Id == id);

	if (adjustment == null)
		return NotFound();

	// Determine recipient (company primary contact email)
	var companyEmail = adjustment.Company?.Email; // Or primary user email
	if (string.IsNullOrWhiteSpace(companyEmail))
	{
		TempData["Error"] = "Cannot send email: No company email address.";
		return RedirectToAction(nameof(Index));
	}

	try
	{
		await _notificationService.SendAsync(new NotificationRequest(
			Recipient: companyEmail,
			Type: NotificationType.ARAdjustmentApplied,
			TemplateData: new Dictionary<string, object>
			{
				["adjustmentId"] = adjustment.Id.ToString(),
				["companyName"] = adjustment.Company.Name,
				["amount"] = adjustment.Amount.ToString(CultureInfo.InvariantCulture),
				["currency"] = adjustment.Currency,
				["reason"] = adjustment.Reason,
				["status"] = adjustment.Status.ToString(),
				["createdAt"] = adjustment.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
				["appliedAt"] = adjustment.AppliedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
				["invoiceNumber"] = adjustment.Invoice?.InvoiceNumber ?? string.Empty,
				["notes"] = adjustment.Notes ?? string.Empty
			},
			UserId: null, // Company adjustment
			ReferenceId: adjustment.Id.ToString(),
			ReferenceType: "ARAdjustment",
			Subject: $"AR Adjustment Notification - {adjustment.Amount:C} {adjustment.Currency} - {adjustment.Company.Name}"
		));

		// Mark as sent
		adjustment.EmailSent = true;
		adjustment.EmailSentDate = DateTime.UtcNow;
		await _db.SaveChangesAsync();

		TempData["Success"] = $"AR adjustment notification sent to {adjustment.Company.Name}.";
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Failed to send AR adjustment email for ID {Id}", id);
		TempData["Error"] = "Failed to send email.";
	}

	return RedirectToAction(nameof(Index));
}
```

---

#### **Step 2.5: Update Order Controller - Add Resend Action**
**File**: `src/cartivaWeb/Areas/Admin/Controllers/OrderController.cs`

**Add Action** (Reuse existing OrderConfirmation email):
```csharp
[HttpPost]
public async Task<IActionResult> ResendEmail(int id)
{
	var order = await _db.OrderHeaders
		.Include(o => o.ApplicationUser)
		.Include(o => o.OrderDetails)
			.ThenInclude(od => od.ProductVariant)
				.ThenInclude(pv => pv.Product)
		.FirstOrDefaultAsync(o => o.Id == id);

	if (order == null)
		return NotFound();

	if (string.IsNullOrWhiteSpace(order.ApplicationUser?.Email))
	{
		TempData["Error"] = "Cannot resend email: No customer email address.";
		return RedirectToAction(nameof(Index));
	}

	try
	{
		// Reuse existing order confirmation logic
		await _notificationService.SendAsync(new NotificationRequest(
			Recipient: order.ApplicationUser.Email,
			Type: NotificationType.OrderPlaced, // Same as original
			TemplateData: new Dictionary<string, object>
			{
				["orderId"] = order.Id.ToString(),
				["orderDate"] = order.OrderDate.ToString("yyyy-MM-dd HH:mm"),
				["totalAmount"] = order.OrderTotal.ToString(CultureInfo.InvariantCulture),
				["customerName"] = order.Name,
				["shippingAddress"] = $"{order.StreetAddress}, {order.City}, {order.PostalCode}",
				// ... include order details
			},
			UserId: order.ApplicationUserId,
			ReferenceId: order.Id.ToString(),
			ReferenceType: "Order",
			Subject: $"Order Confirmation Resent - Order #{order.Id}"
		));

		TempData["Success"] = $"Order confirmation email resent to {order.ApplicationUser.Email}.";
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Failed to resend order confirmation for Order ID {Id}", id);
		TempData["Error"] = "Failed to resend email.";
	}

	return RedirectToAction(nameof(Index));
}
```

---

### **Phase 3: Update Views for UI Consistency**

#### **Step 3.1: Update Invoice Views**
**File**: `src/cartivaWeb/Areas/Admin/Views/Invoice/Details.cshtml`

**Add Action Bar Component** (replace inline actions):
```razor
@await Component.InvokeAsync("DocumentActionBar", new DocumentActionBarViewModel
{
	DocumentId = Model.Id,
	DocumentType = "Invoice",
	DocumentNumber = Model.InvoiceNumber,
	ShowView = false, // Already on details page
	ShowPrint = true,
	ShowEmail = true,
	EmailSent = Model.EmailSent,
	EmailRecipient = Model.CustomerEmail,
	StatusBadgeClass = Model.Status.GetBadgeClass(),
	StatusText = Model.Status.ToString()
})
```

---

#### **Step 3.2: Update Order Views**
**File**: `src/cartivaWeb/Areas/Admin/Views/Order/Details.cshtml`

**Add Action Bar with Resend**:
```razor
@await Component.InvokeAsync("DocumentActionBar", new DocumentActionBarViewModel
{
	DocumentId = Model.Id,
	DocumentType = "Order",
	DocumentNumber = Model.Id.ToString(),
	ShowView = false,
	ShowPrint = true,
	ShowEmail = false,
	ShowResend = true, // Resend instead of Send
	EmailSent = true, // Orders are auto-sent
	EmailRecipient = Model.ApplicationUser?.Email,
	StatusBadgeClass = Model.OrderStatus.GetBadgeClass(),
	StatusText = Model.OrderStatus.ToString()
})
```

---

#### **Step 3.3: Update Credit Note Views**
**File**: `src/cartivaWeb/Areas/Admin/Views/CreditNote/Details.cshtml`

**Add Action Bar**:
```razor
@await Component.InvokeAsync("DocumentActionBar", new DocumentActionBarViewModel
{
	DocumentId = Model.Id,
	DocumentType = "CreditNote",
	DocumentNumber = Model.CreditNoteNumber,
	ShowView = false,
	ShowPrint = true,
	ShowEmail = true,
	EmailSent = Model.EmailSent,
	EmailRecipient = Model.CustomerEmail,
	StatusBadgeClass = Model.Status.GetBadgeClass(),
	StatusText = Model.Status.ToString()
})
```

---

#### **Step 3.4: Update AR Adjustment Views**
**File**: `src/cartivaWeb/Areas/Admin/Views/ARAdjustment/Details.cshtml`

**Add Action Bar**:
```razor
@await Component.InvokeAsync("DocumentActionBar", new DocumentActionBarViewModel
{
	DocumentId = Model.Id,
	DocumentType = "ARAdjustment",
	DocumentNumber = $"ADJ-{Model.Id}",
	ShowView = false,
	ShowPrint = true,
	ShowEmail = true,
	EmailSent = Model.EmailSent,
	EmailRecipient = Model.Company?.Email,
	StatusBadgeClass = Model.Status.GetBadgeClass(),
	StatusText = Model.Status.ToString()
})
```

---

#### **Step 3.5: Update Shipment Views (UI Only)**
**File**: `src/cartivaWeb/Areas/Admin/Views/Shipment/Details.cshtml`

**Add Action Bar with Resend**:
```razor
@await Component.InvokeAsync("DocumentActionBar", new DocumentActionBarViewModel
{
	DocumentId = Model.Id,
	DocumentType = "Shipment",
	DocumentNumber = Model.TrackingNumber,
	ShowView = false,
	ShowPrint = true,
	ShowEmail = false,
	ShowResend = true, // Shipment notifications can be resent
	EmailSent = Model.EmailSent,
	EmailRecipient = Model.OrderHeader?.ApplicationUser?.Email,
	StatusBadgeClass = Model.ShipmentStatus.GetBadgeClass(),
	StatusText = Model.ShipmentStatus.ToString()
})
```

---

#### **Step 3.6: Standardize Index Page Action Buttons**

**Pattern for all Index pages**:
```html
<td class="action-buttons">
	<a asp-action="Details" asp-route-id="@item.Id" 
	   class="btn btn-sm btn-info" title="View">
		<i class="bi bi-eye"></i>
	</a>
	<a asp-action="Print" asp-route-id="@item.Id" 
	   class="btn btn-sm btn-secondary" title="Print" target="_blank">
		<i class="bi bi-printer"></i>
	</a>
	<form asp-action="@(item.EmailSent ? "ResendEmail" : "SendEmail")" method="post" class="d-inline">
		<input type="hidden" name="id" value="@item.Id" />
		<button type="submit" class="btn btn-sm @(item.EmailSent ? "btn-outline-primary" : "btn-primary")" 
				title="@(item.EmailSent ? "Resend Email" : "Send Email")">
			<i class="bi bi-@(item.EmailSent ? "arrow-repeat" : "envelope")"></i>
		</button>
	</form>
</td>
```

**Apply to**:
- Invoice/Index.cshtml ✅
- Order/Index.cshtml ✅
- CreditNote/Index.cshtml ⚠️
- ARAdjustment/Index.cshtml ⚠️
- Shipment/Index.cshtml ⚠️

---

### **Phase 4: Database Schema Updates**

**Add email tracking fields** (if missing):

**CreditNote**:
```sql
ALTER TABLE CreditNotes
ADD EmailSent BIT DEFAULT 0,
	EmailSentDate DATETIME2 NULL;
```

**AccountsReceivableAdjustment**:
```sql
ALTER TABLE AccountsReceivableAdjustments
ADD EmailSent BIT DEFAULT 0,
	EmailSentDate DATETIME2 NULL,
	EmailRecipient NVARCHAR(256) NULL;
```

**Company** (if missing email):
```sql
ALTER TABLE Companies
ADD Email NVARCHAR(256) NULL; -- Primary contact email
```

---

### **Phase 5: Add Notification Types**

**File**: `src/Cartiva.Domain/Enums/NotificationType.cs`

**Add new types**:
```csharp
public enum NotificationType
{
	// Existing...
	OrderPlaced,
	OrderShipped,
	InvoiceGenerated,

	// NEW
	CreditNoteGenerated,
	ARAdjustmentApplied,
	// ... others
}
```

---

## 📦 Deliverables Summary (Revised)

### **New Files (5)**
1. `DocumentActionBar.cs` (ViewComponent)
2. `DocumentActionBarViewModel.cs` (Model)
3. `DocumentActionBar/Default.cshtml` (View)
4. `admin-document-actions.css` (Styles)
5. `CreditNoteGenerated.cshtml` (Email Template)
6. `ARAdjustmentNotification.cshtml` (Email Template)

### **Modified Files (~12)**
1. `CreditNoteController.cs` - Add SendEmail action
2. `ARAdjustmentController.cs` - Add SendEmail action
3. `OrderController.cs` - Add ResendEmail action
4. `Invoice/Details.cshtml` - Add action bar component
5. `Order/Details.cshtml` - Add action bar component
6. `CreditNote/Details.cshtml` - Add action bar component
7. `ARAdjustment/Details.cshtml` - Add action bar component
8. `Shipment/Details.cshtml` - Add action bar component
9. `Invoice/Index.cshtml` - Standardize action buttons
10. `Order/Index.cshtml` - Standardize action buttons
11. `CreditNote/Index.cshtml` - Standardize UI
12. `ARAdjustment/Index.cshtml` - Standardize UI
13. `Shipment/Index.cshtml` - Standardize UI

### **Database Migrations (1)**
1. Add EmailSent/EmailSentDate fields to CreditNote and ARAdjustment

---

## ⚙️ Technical Notes

### **Email System**
- ✅ Use existing `INotificationService`
- ✅ Follow invoice email pattern
- ✅ Reuse OrderConfirmation for resend
- ✅ Queue/retry handled by Notifications system

### **No Logic Changes**
- ✅ Order logic: Keep as-is (just add resend link)
- ✅ Shipment logic: Keep as-is (just add resend link)
- ✅ Invoice logic: Keep as-is (refactor to use action bar UI)

### **Enums Usage**
- ✅ Use `Cartiva.Domain.Enums` for all status values
- ✅ Add `NotificationType.CreditNoteGenerated`
- ✅ Add `NotificationType.ARAdjustmentApplied`

---

## 📅 Implementation Order (Revised)

1. **Phase 1**: Shared UI Components (2 hours)
   - DocumentActionBar component
   - CSS styles

2. **Phase 2**: Add Email Functionality (3 hours)
   - Credit Note email template + controller action
   - AR Adjustment email template + controller action
   - Order resend action

3. **Phase 3**: Update Views (2 hours)
   - Add action bar to all Details pages
   - Standardize Index page action buttons

4. **Phase 4**: Database Updates (30 min)
   - Migration for email tracking fields

5. **Phase 5**: Testing (2 hours)
   - Email functionality
   - UI consistency
   - Action buttons

**Total Estimated Time**: 9-10 hours

---

## 🚀 Next Steps

**Ready to start?** We can proceed with:

1. **Phase 1**: Create DocumentActionBar component (UI foundation)
2. **Phase 2**: Add Credit Note email functionality
3. **Phase 2**: Add AR Adjustment email functionality
4. Or tackle a specific module

**Which would you like to begin with?**

---

*This revised plan focuses on UI consistency and adding missing email functionality while preserving existing working logic.*


---

## 📝 Implementation Steps

### **Phase 1: Shared Infrastructure (Foundation)**

#### **Step 1.1: Create Document Action Bar Component**
**File**: `src/cartivaWeb/Areas/Admin/ViewComponents/DocumentActionBar.cs`

**Purpose**: Reusable component for View/Print/Email actions

**Actions Config**:
```csharp
public class DocumentActionBarViewModel
{
	public int DocumentId { get; set; }
	public string DocumentType { get; set; } // "Invoice", "Order", "CreditNote", "ARAdjustment"
	public string DocumentNumber { get; set; }
	public bool ShowView { get; set; } = true;
	public bool ShowPrint { get; set; } = true;
	public bool ShowEmail { get; set; } = true;
	public bool EmailSent { get; set; } = false;
	public string? EmailRecipient { get; set; }
	public string? StatusBadgeClass { get; set; }
	public string? StatusText { get; set; }
	public Dictionary<string, string>? AdditionalActions { get; set; } // Extensible
}
```

**Controller/Area/Action Routes**:
- View: `/{Area}/{DocumentType}/Details/{id}`
- Print: `/{Area}/{DocumentType}/Print/{id}`
- Email: `/{Area}/{DocumentType}/SendEmail/{id}` (POST)

---

#### **Step 1.2: Create Unified Document Email Service**
**File**: `src/Cartiva.Application/Services/DocumentEmailService.cs`

**Purpose**: Centralize email logic, reuse invoice email pattern

**Interface**:
```csharp
public interface IDocumentEmailService
{
	Task<bool> SendInvoiceAsync(int invoiceId, CancellationToken ct = default);
	Task<bool> SendCreditNoteAsync(int creditNoteId, CancellationToken ct = default);
	Task<bool> SendARAdjustmentAsync(int adjustmentId, CancellationToken ct = default);
	Task<bool> SendOrderConfirmationAsync(int orderId, bool isResend = false, CancellationToken ct = default);
}
```

**Pattern**:
- Extract invoice email logic
- Create template data builders per document type
- Reuse NotificationService
- Dynamic subject/template based on document state

---

#### **Step 1.3: Create Email Templates**
**Files**:
- `src/Cartiva.Infrastructure/Templates/CreditNoteGenerated.cshtml`
- `src/Cartiva.Infrastructure/Templates/ARAdjustmentNotification.cshtml`
- `src/Cartiva.Infrastructure/Templates/OrderConfirmationResend.cshtml`

**Pattern**: Follow InvoiceGenerated.cshtml structure
- Header with logo/company info
- Document details table
- Line items (where applicable)
- Payment/status info
- Footer with contact/legal

---

#### **Step 1.4: Create Shared CSS for Action Bar**
**File**: `src/cartivaWeb/wwwroot/css/admin-document-actions.css`

**Purpose**: Consistent action button styling

**Pattern**:
```css
.document-action-bar {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 1rem;
	background: #fff;
	border-radius: 8px;
	box-shadow: 0 2px 4px rgba(0,0,0,0.1);
	margin-bottom: 1.5rem;
}

.action-buttons {
	display: flex;
	gap: 0.5rem;
}

.action-btn {
	/* Consistent button styling */
}

.email-status {
	/* Email sent indicator */
}
```

---

### **Phase 2: Update Existing Modules**

#### **Step 2.1: Update Invoice Module**
**Changes**:
1. Replace inline actions with `DocumentActionBar` component
2. Migrate email logic to `DocumentEmailService`
3. Update controller to use new service
4. **Keep existing functionality** (this is the reference)

**Files**:
- `InvoiceController.cs` - Update Send action
- `Invoice/Details.cshtml` - Add action bar component
- `Invoice/Index.cshtml` - Update action buttons

---

#### **Step 2.2: Update Order Module**
**Changes**:
1. Add `DocumentActionBar` component
2. **Add manual email resend functionality**
3. Update controller with `SendEmail` action
4. Reuse existing OrderConfirmation template or create resend variant

**New Action**:
```csharp
[HttpPost]
public async Task<IActionResult> SendEmail(int id)
{
	var result = await _documentEmailService.SendOrderConfirmationAsync(id, isResend: true);
	if (result)
		TempData["Success"] = "Order confirmation email resent successfully.";
	else
		TempData["Error"] = "Failed to send email.";
	return RedirectToAction(nameof(Index));
}
```

**Files**:
- `OrderController.cs` - Add SendEmail action
- `Order/Details.cshtml` - Add action bar
- `Order/Index.cshtml` - Update action buttons

---

#### **Step 2.3: Update Credit Note Module**
**Changes**:
1. Add `DocumentActionBar` component
2. **Implement email functionality** (new)
3. Standardize Print view
4. Add filtering UI consistency

**New Actions**:
```csharp
[HttpPost]
public async Task<IActionResult> SendEmail(int id)
{
	var result = await _documentEmailService.SendCreditNoteAsync(id);
	if (result)
		TempData["Success"] = "Credit note sent successfully.";
	else
		TempData["Error"] = "Failed to send credit note.";
	return RedirectToAction(nameof(Index));
}

public async Task<IActionResult> Print(int id)
{
	var creditNote = await _creditNoteService.GetByIdAsync(id);
	if (creditNote == null) return NotFound();
	return View("PrintCreditNote", creditNote);
}
```

**Files**:
- `CreditNoteController.cs` - Add SendEmail, Print actions
- `CreditNote/Details.cshtml` - Add action bar
- `CreditNote/Index.cshtml` - Standardize UI
- `CreditNote/PrintCreditNote.cshtml` - Create print view

---

#### **Step 2.4: Update AR Adjustment Module**
**Changes**:
1. Add `DocumentActionBar` component
2. **Implement email functionality** (new)
3. **Create Print view** (new)
4. Standardize Index page

**New Actions**:
```csharp
[HttpPost]
public async Task<IActionResult> SendEmail(int id)
{
	var result = await _documentEmailService.SendARAdjustmentAsync(id);
	if (result)
		TempData["Success"] = "AR Adjustment notification sent successfully.";
	else
		TempData["Error"] = "Failed to send notification.";
	return RedirectToAction(nameof(Index));
}

public async Task<IActionResult> Print(int id)
{
	var adjustment = await _arAdjustmentService.GetByIdAsync(id);
	if (adjustment == null) return NotFound();
	return View("PrintAdjustment", adjustment);
}
```

**Files**:
- `ARAdjustmentController.cs` - Add SendEmail, Print actions
- `ARAdjustment/Details.cshtml` - Add action bar
- `ARAdjustment/Index.cshtml` - Standardize UI
- `ARAdjustment/PrintAdjustment.cshtml` - Create print view

---

### **Phase 3: UI Consistency**

#### **Step 3.1: Standardize Dashboard Layout**
**Pattern** (from Invoice module):
```html
<div class="admin-dashboard {module}-dashboard">
	<div class="container-fluid">
		<h2><i class="bi bi-{icon}"></i> {Module} Management</h2>

		<!-- Summary Cards (if applicable) -->
		<div class="row mb-4 g-3">
			<!-- 4 cards with metrics -->
		</div>

		<!-- Sections with tabs/filters -->
		<div class="{module}-section">
			<!-- DataTables or grouped views -->
		</div>
	</div>
</div>
```

**Apply to**:
- Orders ✅ (mostly done)
- Credit Notes ⚠️ (needs update)
- AR Adjustments ⚠️ (needs update)

---

#### **Step 3.2: Standardize DataTables Configuration**
**Create shared JS config**:
```javascript
// wwwroot/js/admin-datatables-config.js
function initAdminDataTable(tableId, options = {}) {
	const defaults = {
		responsive: true,
		pageLength: 25,
		order: [[0, 'desc']],
		language: {
			search: "Search:",
			lengthMenu: "Show _MENU_ entries",
			info: "Showing _START_ to _END_ of _TOTAL_ entries"
		}
	};
	return $('#' + tableId).DataTable({ ...defaults, ...options });
}
```

**Usage**:
```javascript
initAdminDataTable('invoicesTable');
initAdminDataTable('ordersTable', { order: [[2, 'desc']] });
```

---

#### **Step 3.3: Standardize Action Buttons in Tables**
**Pattern**:
```html
<td class="action-buttons">
	<a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-info" title="View">
		<i class="bi bi-eye"></i>
	</a>
	<a asp-action="Print" asp-route-id="@item.Id" class="btn btn-sm btn-secondary" title="Print" target="_blank">
		<i class="bi bi-printer"></i>
	</a>
	<form asp-action="SendEmail" method="post" class="d-inline">
		<input type="hidden" name="id" value="@item.Id" />
		<button type="submit" class="btn btn-sm btn-primary" title="Send Email">
			<i class="bi bi-envelope"></i>
		</button>
	</form>
</td>
```

**Apply consistently across all Index views**

---

### **Phase 4: Filtering Enhancements**

#### **Step 4.1: Create Shared Filter Component**
**Optional**: If filtering becomes complex, create ViewComponent

**For now**: Standardize inline filters
- Date range filters
- Status filters
- Company filters (where applicable)
- Search (via DataTables)

**Pattern**:
```html
<div class="filter-bar mb-3">
	<div class="btn-group" role="group">
		<a asp-action="Index" asp-route-status="" class="btn btn-sm @(string.IsNullOrEmpty(status) ? "btn-dark" : "btn-outline-dark")">All</a>
		<a asp-action="Index" asp-route-status="Status1" class="btn btn-sm @(status == "Status1" ? "btn-primary" : "btn-outline-primary")">Status1</a>
		<!-- ... -->
	</div>
</div>
```

---

### **Phase 5: Service Registration & DI**

#### **Step 5.1: Register New Services**
**File**: `src/cartivaWeb/Program.cs`

```csharp
// Document Email Service
builder.Services.AddScoped<IDocumentEmailService, DocumentEmailService>();
```

---

### **Phase 6: Testing & Validation**

#### **Step 6.1: Functional Testing**
- [ ] Invoice: View, Print, Email (paid & unpaid)
- [ ] Order: View, Print, Email resend
- [ ] Credit Note: View, Print, Email
- [ ] AR Adjustment: View, Print, Email

#### **Step 6.2: UI/UX Testing**
- [ ] Consistent layout across all modules
- [ ] Action buttons work uniformly
- [ ] Responsive design
- [ ] DataTables functionality

#### **Step 6.3: Email Testing**
- [ ] Invoice email (paid vs unpaid subject/content)
- [ ] Order confirmation resend
- [ ] Credit note email
- [ ] AR adjustment email
- [ ] Email queue/retry logic

---

## 📦 Deliverables Summary

### **New Files (11)**
1. `DocumentActionBar.cs` (ViewComponent)
2. `DocumentActionBarViewModel.cs` (Model)
3. `DocumentActionBar/Default.cshtml` (View)
4. `admin-document-actions.css` (Styles)
5. `DocumentEmailService.cs` (Service)
6. `IDocumentEmailService.cs` (Interface)
7. `DocumentEmailRequest.cs` (Model)
8. `CreditNoteGenerated.cshtml` (Template)
9. `ARAdjustmentNotification.cshtml` (Template)
10. `OrderConfirmationResend.cshtml` (Template)
11. `admin-datatables-config.js` (Shared JS)

### **Modified Files (~12)**
1. `InvoiceController.cs`
2. `OrderController.cs`
3. `CreditNoteController.cs`
4. `ARAdjustmentController.cs`
5. `Invoice/Index.cshtml`
6. `Invoice/Details.cshtml`
7. `Order/Index.cshtml`
8. `Order/Details.cshtml`
9. `CreditNote/Index.cshtml`
10. `CreditNote/Details.cshtml`
11. `ARAdjustment/Index.cshtml`
12. `ARAdjustment/Details.cshtml`

### **New Print Views (2)**
1. `CreditNote/PrintCreditNote.cshtml`
2. `ARAdjustment/PrintAdjustment.cshtml`

---

## ⚙️ Technical Considerations

### **Enums Usage**
- Use `Cartiva.Domain.Enums` for all status values
- Avoid SD constants ✅
- Examples: `InvoiceStatus`, `OrderStatus`, `CreditNoteStatus`, `ARAdjustmentStatus`

### **Email Template Data**
- Follow invoice pattern: Dictionary<string, object>
- Include all necessary fields for rendering
- Handle null/optional fields gracefully

### **Notification Service**
- Reuse existing `INotificationService`
- Leverage queue/retry logic
- Track sent status in database

### **Security**
- All actions require `[Authorize(Roles = "Admin")]`
- Validate document ownership/access where needed
- Sanitize email inputs

---

## 🎯 Success Criteria

1. ✅ All modules have consistent UI layout
2. ✅ All modules have View/Print/Email actions
3. ✅ Email functionality works for all document types
4. ✅ Action bar component is reusable
5. ✅ DataTables configuration is standardized
6. ✅ No SD constants used, only enums
7. ✅ Code duplication minimized via shared services
8. ✅ Manual order email resend works
9. ✅ Credit Note & AR Adjustment email implemented
10. ✅ Print views follow consistent format

---

## 📅 Implementation Order

1. **Phase 1**: Shared Infrastructure (2-3 hours)
   - Action bar component
   - Email service
   - Templates
   - CSS

2. **Phase 2**: Module Updates (4-5 hours)
   - Invoice (refactor)
   - Order (add resend)
   - Credit Note (add email/print)
   - AR Adjustment (add email/print)

3. **Phase 3**: UI Consistency (2 hours)
   - Dashboard layouts
   - DataTables config
   - Action buttons

4. **Phase 4**: Filtering (1 hour)
   - Standardize filters

5. **Phase 5**: DI Setup (30 min)
   - Service registration

6. **Phase 6**: Testing (2-3 hours)
   - Functional tests
   - Email tests
   - UI validation

**Total Estimated Time**: 12-15 hours

---

## 🚀 Next Steps

**Ready to start?** We can proceed with:

1. **Phase 1.1**: Create DocumentActionBar component
2. **Phase 1.2**: Create DocumentEmailService
3. Or any specific module you want to tackle first

**Let me know which phase/step you'd like to begin with!**

---

*This plan ensures a clean, maintainable, and scalable architecture for all admin document management modules.*
