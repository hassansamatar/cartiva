# 📚 Cartiva E-Commerce Platform - Documentation Index

Welcome to the Cartiva documentation! This folder contains comprehensive documentation for the Cartiva e-commerce platform.

---

## 📋 Table of Contents

1. [Entity Relationship Diagrams](#entity-relationship-diagrams)
2. [Architecture Documentation](#architecture-documentation)
3. [Screenshots & Features](#screenshots--features)
4. [System Components](#system-components)
5. [Setup & Configuration](#setup--configuration)

---

## 🗄️ Entity Relationship Diagrams

### Primary Diagrams

#### [📊 Complete ER Diagram (with Notifications)](ER-diagram/Complete-ER-Diagram-With-Notifications.md)
- **Latest version** with Notification System
- 20 core entities
- 30+ relationships
- Includes Mermaid diagrams and sequence flows
- **Recommended starting point**

#### [💰 Invoice System Relationships (Detailed)](ER-diagram/Invoice-System-Relationships.md) ⭐ NEW
- **Detailed explanation** of Invoice → InvoicePayment (1:M)
- **Detailed explanation** of Invoice → CreditNote (1:M)
- Complete examples with partial payments
- Multiple credit notes scenarios
- Balance calculation logic
- **Essential for understanding B2B invoicing**

#### [📊 Application Tables (HTML)](ER-diagram/er-diagram-application.html)
- Visual HTML diagram
- Excludes ASP.NET Identity tables
- Print-friendly format
- Interactive legend

#### [🔐 Identity Tables (HTML)](ER-diagram/er-diagram-identity.html)
- ASP.NET Core Identity schema
- User management tables
- Role-based access control

### Entity Summary

| Entity Category | Count | Key Entities |
|----------------|-------|--------------|
| User Management | 2 | Company, ApplicationUser |
| Product Catalog | 6 | Product, ProductVariant, Category, SizeSystem |
| Orders & Cart | 3 | OrderHeader, OrderDetail, ShoppingCart |
| Shipping | 1 | Shipment |
| Invoicing | 6 | Invoice, InvoiceLine, CreditNote, InvoicePayment |
| Returns | 1 | ReturnRequest |
| Reviews | 1 | Review |
| Notifications | 1 | Notification ⭐ NEW |
| Promotions | 1 | Promotion |
| Stripe | 1 | ProcessedStripeEvent |
| **Total** | **20** | |

---

## 🏗️ Architecture Documentation

### System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Cartiva Platform                      │
├─────────────────────────────────────────────────────────┤
│  Presentation Layer (cartivaWeb - Razor Pages)          │
│  ├─ Areas/                                              │
│  │  ├─ Admin/ (Management)                             │
│  │  ├─ Customer/ (Shopping)                            │
│  │  └─ Identity/ (Auth)                                │
│  └─ Pages/ (Home, About, etc.)                         │
├─────────────────────────────────────────────────────────┤
│  Application Layer (Cartiva.Application)                │
│  ├─ Services/ (Business Logic)                         │
│  ├─ Abstractions/ (Interfaces)                         │
│  └─ ViewModels/                                         │
├─────────────────────────────────────────────────────────┤
│  Domain Layer (Cartiva.Domain)                          │
│  ├─ Entities/ (20 core entities)                       │
│  ├─ Enums/                                              │
│  └─ Interfaces/                                         │
├─────────────────────────────────────────────────────────┤
│  Infrastructure Layer (Cartiva.Infrastructure)          │
│  ├─ Notifications/ ⭐ NEW                               │
│  │  ├─ NotificationService                             │
│  │  ├─ NotificationWorker (Background)                 │
│  │  ├─ Channels/ (Email, SMS, Push)                    │
│  │  ├─ Templates/ (RazorLight)                         │
│  │  └─ Queue/ (Channel<int>)                           │
│  ├─ EmailServices/ (Legacy, for Identity)              │
│  ├─ PaymentService/ (Stripe)                           │
│  ├─ ShippingServices/ (Bring API)                      │
│  ├─ QrCodeServices/                                     │
│  └─ ImageServices/                                      │
├─────────────────────────────────────────────────────────┤
│  Persistence Layer (Cartiva.Persistence)                │
│  ├─ ApplicationDbContext                                │
│  ├─ Migrations/                                         │
│  └─ DbInitializer (Seeding)                            │
├─────────────────────────────────────────────────────────┤
│  Shared Layer (Cartiva.Shared)                          │
│  └─ Constants (SD), Configuration                       │
└─────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

1. **Domain** - Core business entities (no dependencies)
2. **Application** - Use cases and business logic
3. **Infrastructure** - External concerns (DB, Email, APIs)
4. **Presentation** - Razor Pages UI

---

## 📸 Screenshots & Features

### Admin Management

| Feature | Screenshot | Description |
|---------|-----------|-------------|
| Dashboard | [admin-management.png](screenshots/admin-management.png) | Admin overview |
| Products | [product_management.png](screenshots/product_management.png) | Product CRUD |
| Categories | [category_management.png](screenshots/category_management.png) | Category management |
| Companies | [company_management.png](screenshots/company_management.png) | Company accounts |
| Users | [user_management.png](screenshots/user_management.png) | User management |
| Orders | [order_status.png](screenshots/order_status.png) | Order tracking |
| Invoices | [invoice_management.png](screenshots/invoice_management.png) | Invoice handling |
| Shipments | [shipment_management.png](screenshots/shipment_management.png) | Shipping mgmt |
| Returns | [return_management.png](screenshots/return_management.png) | Return processing |
| Reviews | [review_management.png](screenshots/review_management.png) | Review moderation |
| Promotions | [promation_management.png](screenshots/promation_management.png) | Promo campaigns |

### Customer Experience

| Feature | Screenshot | Description |
|---------|-----------|-------------|
| Home | [home.png](screenshots/home.png) | Landing page |
| Login | [login.png](screenshots/login.png) | Authentication |
| Register | [register.png](screenshots/register.png) | User signup |
| Profile | [user_profile.png](screenshots/user_profile.png) | Account settings |
| Cart | [items_added_shopping_cart.png](screenshots/items_added_shopping_cart.png) | Shopping cart |
| Checkout | [shipping_info_review.png](screenshots/shipping_info_review.png) | Order review |
| Order Confirmed | [order_confirmed.png](screenshots/order_confirmed.png) | Confirmation |
| Order History | [order_history.png](screenshots/order_history.png) | Past orders |
| Reviews | [product_review.png](screenshots/product_review.png) | Write reviews |
| Returns | [return_request.png](screenshots/return_request.png) | Request returns |

### Payment & Billing

| Feature | Screenshot | Description |
|---------|-----------|-------------|
| Payment | [payment_issued.png](screenshots/payment_issued.png) | Stripe checkout |
| Invoice | [invoice.png](screenshots/invoice.png) | Generated invoice |
| Company Bills | [company_bills_management.png](screenshots/company_bills_management.png) | B2B invoicing |
| Stripe Dashboard | [stripe_dashboard.png](screenshots/stripe_dashboard.png) | Payment stats |
| Refunds | [refund_processed.png](screenshots/refund_processed.png) | Return refunds |

### Background Jobs

| Feature | Screenshot | Description |
|---------|-----------|-------------|
| Hangfire Dashboard | [hangfire_jobs.png](screenshots/hangfire_jobs.png) | Scheduled jobs |
| History Graph | [history_graph_hangfire.png](screenshots/history_graph_hangfire.png) | Job analytics |

---

## 🔧 System Components

### 1. Notification System ⭐ NEW

**Purpose:** Unified email notification system for all customer communications

**Architecture:**
```
Service → NotificationService → Database → Queue → Worker → RazorLight → SMTP
```

**Components:**
- `NotificationService` - Main API for sending notifications
- `NotificationWorker` - Background service (Hosted Service)
- `NotificationQueue` - Channel-based queue (capacity: 1000)
- `RazorLightTemplateRenderer` - Template rendering engine
- `SmtpEmailSender` - Gmail SMTP with Polly retry
- `EmailNotificationChannel` - Email delivery channel

**Email Templates:**
- WelcomeEmail.cshtml
- OrderConfirmation.cshtml
- OrderShipped.cshtml
- OrderDelivered.cshtml
- OrderCancelled.cshtml
- PaymentReceived.cshtml
- PasswordReset.cshtml
- ReturnRequestReceived.cshtml
- ReturnRequestApproved.cshtml
- ReturnRequestRejected.cshtml
- InvoiceGenerated.cshtml
- Generic.cshtml

**Database Tracking:**
```sql
Notifications (
	Id, Type, Channel, Status, Recipient, Subject,
	TemplateData, UserId, ReferenceId, ReferenceType,
	CreatedAt, ProcessedAt, SentAt, ErrorMessage, RetryCount
)
```

**Features:**
- ✅ Background processing (non-blocking)
- ✅ Retry logic with Polly (3 attempts, exponential backoff)
- ✅ Template caching for performance
- ✅ Full audit trail in database
- ✅ Manual retry for failed notifications
- ✅ Support for future channels (SMS, Push)

### 2. Payment Processing

**Provider:** Stripe  
**Features:**
- Card payments
- Webhook handling
- Refund processing
- Payment intents
- Idempotency keys

### 3. Shipping Integration

**Provider:** Bring (Posten Norge)  
**Features:**
- Real-time shipping quotes
- Label generation
- Tracking numbers
- Package tracking

### 4. Background Jobs

**Framework:** Hangfire  
**Jobs:**
- Scheduled reporting
- Data cleanup
- Batch processing

### 5. Invoicing System

**Features:**
- KID number generation (Norwegian payment)
- VAT breakdown
- PDF generation
- Payment tracking
- Credit notes
- Multi-currency support

### 6. Return Management

**Workflow:**
1. Customer requests return
2. Admin reviews
3. Approve/Reject
4. Generate credit note
5. Process refund

---

## ⚙️ Setup & Configuration

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or full)
- Visual Studio 2026 or VS Code
- Gmail account (for SMTP)
- Stripe account (for payments)
- Bring API key (for shipping)

### Configuration Files

#### appsettings.Development.json

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CartivaDB;Trusted_Connection=True;"
  },
  "EmailSettings": {
	"SmtpServer": "smtp.gmail.com",
	"SmtpPort": 587,
	"SenderEmail": "your-email@gmail.com",
	"SenderName": "Cartiva",
	"Password": "your-app-password",
	"EnableSsl": true
  },
  "NotificationSettings": {
	"MaxRetryAttempts": 3,
	"RetryDelaySeconds": 2,
	"QueueCapacity": 1000,
	"EnableBackgroundProcessing": true
  },
  "Stripe": {
	"SecretKey": "sk_test_...",
	"PublishableKey": "pk_test_...",
	"WebhookSecret": "whsec_..."
  },
  "Bring": {
	"BaseUrl": "https://api.bring.com",
	"ApiKey": "your-bring-key"
  }
}
```

### Database Setup

```bash
# Create initial migration
dotnet ef migrations add InitialCreate -p src/Cartiva.Persistence -s src/cartivaWeb

# Update database
dotnet ef database update -p src/Cartiva.Persistence -s src/cartivaWeb

# Seed runs automatically on app start via DbInitializer
```

### Gmail App Password

For notification system to work:

1. Go to https://myaccount.google.com/security
2. Enable 2-Step Verification
3. Go to "App passwords"
4. Generate password for "Mail"
5. Use 16-character password in appsettings

---

## 📊 Database Statistics

- **20 Tables** (excluding Identity)
- **30+ Relationships**
- **5 Indexes** on Notifications table
- **JSON columns** for flexible data (TemplateData, CarrierData)
- **Audit fields** (CreatedAt, UpdatedAt where applicable)

---

## 🔍 Key Features

### For Customers
- ✅ User registration with email verification
- ✅ Product browsing with size/color variants
- ✅ Shopping cart with promotions
- ✅ Secure checkout (Stripe)
- ✅ Order tracking
- ✅ Email notifications (Welcome, Order, Shipping)
- ✅ Product reviews
- ✅ Return requests
- ✅ Order history

### For Companies (B2B)
- ✅ Company accounts
- ✅ Deferred payment terms
- ✅ Invoice generation with KID
- ✅ Multiple invoices per order
- ✅ Credit notes
- ✅ Company dashboard

### For Admins
- ✅ Complete CRUD for all entities
- ✅ Order management
- ✅ Shipment processing
- ✅ Return approval workflow
- ✅ Invoice management
- ✅ Review moderation
- ✅ User management
- ✅ Notification dashboard
- ✅ Hangfire job monitoring

---

## 📖 Additional Documentation

### API Documentation
- All services in `Cartiva.Application/Services/`
- Interfaces in `Cartiva.Application/Abstractions/`

### Troubleshooting Guides
- [Notification Troubleshooting](../NOTIFICATION_TROUBLESHOOTING.md)
- [Complete Diagnostic Tests](../COMPLETE_DIAGNOSTIC_TESTS.md)
- [Notification System Restored](../NOTIFICATION_SYSTEM_RESTORED.md)

### Migration Summaries
- [Notification Integration Summary](../NOTIFICATION_INTEGRATION_SUMMARY.md)
- [Legacy Email Cleanup](../LEGACY_EMAIL_CLEANUP_SUMMARY.md)
- [Duplicate Email Fix](../DUPLICATE_EMAIL_FIX_SUMMARY.md)

---

## 🎯 Quick Links

| Resource | Link |
|----------|------|
| ER Diagram (Latest) | [Complete-ER-Diagram-With-Notifications.md](ER-diagram/Complete-ER-Diagram-With-Notifications.md) |
| Application Tables | [er-diagram-application.html](ER-diagram/er-diagram-application.html) |
| Identity Tables | [er-diagram-identity.html](ER-diagram/er-diagram-identity.html) |
| Screenshots Gallery | [gallery.html](screenshots/gallery.html) |
| GitHub Repository | https://github.com/hassansamatar/cartiva |

---

## 🚀 Technology Stack Summary


| Layer | Technologies |
|-------|-------------|
| **Frontend** | Razor Pages, Bootstrap 5, jQuery |
| **Backend** | .NET 10, C# 13 |
| **Database** | SQL Server, Entity Framework Core 10 |
| **Authentication** | ASP.NET Core Identity |
| **Payment** | Stripe API |
| **Shipping** | Bring API (Posten Norge) |
| **Email** | Gmail SMTP, RazorLight Templates |
| **Background Jobs** | Hangfire, Hosted Services |
| **Retry Logic** | Polly |
| **QR Codes** | QRCoder |
| **Image Storage** | File system (wwwroot) |

---

## 📝 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | April 2026 | Added Notification System, Updated ER diagrams |
| 1.5 | March 2026 | Invoice system enhancements, Credit notes |
| 1.0 | February 2026 | Initial release |

---

## 👨‍💻 Contributors

 Hassan Samatar 
---
## 📄 License

This project is for educational purposes.

---

*Last Updated: April 23, 2026*  
*Documentation Version: 2.0*
