# 🛒 Cartiva — Full-Stack E-Commerce Platform

> 🚀 A production-ready e-commerce platform built with Clean Architecture, ASP.NET Core (.NET 10), and Stripe — supporting B2C, B2B invoicing, returns, and real-time shipment tracking.

📊 [View Interactive ER Diagrams](https://hassansamatar.github.io/cartiva/)  
🎬 <a href="https://github.com/hassansamatar/cartiva" target="_blank">💻 Source Code</a>

---

## ⚡ TL;DR – What You Get

- ✅ Stripe payments & refunds (idempotent webhooks)
- ✅ B2C + B2B (company accounts, 30-day invoicing)
- ✅ Role-based access (Admin, Employee, Customer, Company)
- ✅ Returns & 30-day refund workflow
- ✅ Shipment tracking with QR codes (Bring, Posten, DHL)
- ✅ Promotion engine – Buy X Get Y Free
- ✅ Clean Architecture + background jobs (Hangfire)
- ✅ Cookie-based cart (scalable, no server session)

---

## 💡 Why I Built This

This project demonstrates a **real-world, production-grade e-commerce system** with:

- **Clean Architecture** – maintainable, testable, scalable
- **Complex business logic** – deferred payments, returns, multi-user companies
- **Secure payments** – Stripe integration with idempotent webhook handling
- **Reliable async processing** – email notifications, return expiration checks via Hangfire
- **Complete documentation** – interactive ER diagrams, deployment guides, environment variables

---

## ✨ Features

### 🛍️ Customer Experience
- Product catalog – categories, sizes, colors, variants
- Shopping cart persisted via **cookies** (no server-side session)
- Stripe checkout integration
- Order tracking with QR codes & live shipment status
- Product reviews (post-delivery) – only customers & company users can write reviews
- Returns & refunds within a 30-day window
- Order history with printable receipts
- Address autocomplete – Norwegian Kartverket API
- Promotion management – flexible discounts per category/variant, enable/disable anytime
- Automatic "On Sale" badge on cheapest variant

### 🏢 Company (B2B)
- Deferred payments – 30-day invoice option
- Multi-user company accounts under one organization
- Finance-approved billing & order management for company reps

### 🔧 Admin Panel
- Dashboard – order & revenue insights
- Product management – CRUD for products, variants, categories
- Order lifecycle – approve, process, ship, deliver
- Shipment management – Posten, Bring, Helthjem, DHL
- Review moderation – approve, reject, delete
- Return management – approve returns, process Stripe partial refunds
- Promotion engine – Buy X Get Y Free per category
- User management – roles, activation, deactivation
- Company management – full B2B account control

### 👷 Employee Role
- Full content management – products, orders, shipments, reviews, returns
- Read-only access to user accounts
- Company management – create/edit companies (no delete/deactivate)

---

## 🏗️ Architecture

Clean Architecture with clear separation of concerns and dependency inversion.

Cartiva.sln
├── src/
│   ├── CartivaWeb/ → ASP.NET Core MVC with ASP.NET Identity (Razor Pages), views, wwwroot
│   ├── Cartiva.Application/ → Use cases, service interfaces, DTOs
│   ├── Cartiva.Domain/ → Entities, enums, value objects (pure domain)
│   ├── Cartiva.Persistence/ → EF Core DbContext, migrations, seeding
│   ├── Cartiva.Infrastructure/ → Stripe, Bring, Kartverket, Hangfire, Serilog
│   └── Cartiva.Shared/ → Constants, helpers, shared utilities
├── docs/ → ER diagrams (GitHub Pages)
├── tests/ → Unit & integration tests
└── README.md

### Project Layers

| Project | Purpose |
|---------|---------|
| **CartivaWeb** | Presentation – MVC controllers, views, routing |
| **Cartiva.Domain** | Core domain – entities, enums, value objects |
| **Cartiva.Application** | Application layer – use cases, interfaces, DTOs |
| **Cartiva.Persistence** | Data access – EF Core, migrations, seeding |
| **Cartiva.Infrastructure** | External services + background jobs (Hangfire) |
| **Cartiva.Shared** | Cross-cutting – constants, helpers |

---

## 🗄️ Database Schema

**21 tables** – 7 ASP.NET Identity + 14 application models, 24 relationships.  
📊 [Interactive ER Diagrams](https://hassansamatar.github.io/cartiva/)

### Application Models

| Model | Purpose |
|-------|---------|
| `ApplicationUser` | Extends IdentityUser with address & company association |
| `Company` | B2B company accounts |
| `Category` | Product categories with size systems |
| `Product` | Core product entity |
| `ProductVariant` | Color, size, price, stock |
| `SizeSystem`, `SizeValue` | Size definitions (S, M, L, 42, 104cm) |
| `Promotion` | Discount rules (Buy X Get Y Free) |
| `ShoppingCart` | Cookie-based cart items |
| `OrderHeader`, `OrderDetail` | Order summary and line items |
| `Shipment` | Tracking info (carrier, status) |
| `Review` | Product ratings & comments (customers & company only) |
| `ReturnRequest` | Returns and refund requests |

---

## 🔐 Roles & Permissions

| Feature                    | Customer | Company | Employee | Admin |
|---------------------------|:--------:|:-------:|:--------:|:-----:|
| Browse & purchase         |    ✅    |    ✅   |    ✅    |  ✅   |
| Deferred payments         |    —     |    ✅   |    —     |  —    |
| Write reviews             |    ✅    |    ✅   |    ✅    |  ✅   |
| Moderate reviews          |    —     |    —     |    ✅    |  ✅   |
| Request returns           |    ✅    |    ✅   |    —     |  —    |
| Manage products           |    —     |    —     |    ✅    |  ✅   |
| Manage orders & shipments |    —     |    —     |    ✅    |  ✅   |
| Process returns & refunds |    —     |    —     |    ✅    |  ✅   |
| Manage promotions         |    —     |    —     |    ✅    |  ✅   |
| View users (read-only)    |    —     |    —     |    ✅    |  ✅   |
| Manage users & roles      |    —     |    —     |    —     |  ✅   |
| Manage companies          |    —     |    —     | Limited  |  ✅   |

¹ Employees can create and edit companies but cannot delete or deactivate them.

---

## 🛠️ Tech Stack

| Technology | Usage |
|------------|-------|
| .NET 10 | Runtime & SDK |
| ASP.NET Core MVC | Web framework (Identity uses Razor Pages) |
| Entity Framework Core | ORM, migrations |
| SQL Server | Database |
| ASP.NET Identity | Auth, 2FA |
| Stripe | Payments, refunds |
| Bring API | Shipping rates, labels |
| Kartverket API | Address autocomplete |
| Hangfire | Background jobs (email, expiry, webhook retries) |
| Serilog | Structured logging |
| MemoryCache / IDistributedCache | Product catalog caching |
| **HTML5 / CSS3 / JavaScript (ES6+)** | Frontend structure, styling, and client-side interactivity |
| Bootstrap 5, SweetAlert2, DataTables | Frontend utilities |
| QRCoder | QR codes for tracking |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or full instance)
- [Stripe account](https://stripe.com/) (test mode)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/hassansamatar/cartiva.git
   cd cartiva
Configure secrets
Update appsettings.json or use User Secrets:

json
{
  "ConnectionStrings": { "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Cartiva;..." },
  "Stripe": { "SecretKey": "sk_test_...", "PublishableKey": "pk_test_..." },
  "Bring": { "ApiKey": "...", "CustomerId": "..." },
  "Kartverket": { "BaseUrl": "https://ws.geonorge.no/adresser/v1/" }
}
Apply migrations & seed data

bash
dotnet ef database update --project Cartiva.Persistence --startup-project CartivaWeb
Seeding creates:

Roles (Admin, Employee, Company, Customer)

Admin user: admin@cartiva.com / Admin12#

Sample products, categories, size systems, variants

Start background job server (Hangfire)
In Program.cs, ensure app.UseHangfireDashboard() and recurring jobs for:

Order confirmation emails

Return window expiration checks

Failed Stripe webhook retries

Run the application

bash
dotnet run --project CartivaWeb
🔐 Environment Variables (Production)
Use these instead of hardcoded secrets in production.

Variable	Purpose
DB_CONNECTION_STRING	SQL Server connection string
STRIPE_SECRET_KEY	Stripe API secret key
STRIPE_WEBHOOK_SECRET	Stripe webhook signing secret
BRING_API_KEY	Bring shipping API key
BRING_CUSTOMER_ID	Bring customer ID
SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS	Email delivery
ASPNETCORE_ENVIRONMENT	Production or Development
🧪 Testing
bash
dotnet test
Unit tests – tests/Cartiva.Tests.Unit (xUnit, Moq)

Integration tests – tests/Cartiva.Tests.Integration (Testcontainers for SQL Server)

📦 Deployment
Docker
dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "CartivaWeb.dll"]
Build and run:

bash
dotnet publish -c Release -o ./publish
docker build -t cartiva .
docker run -p 80:8080 -e ASPNETCORE_ENVIRONMENT=Production cartiva
Azure App Service
Set connection string & secrets in Application Settings

Enable "Always On" for Hangfire background jobs

Configure Stripe webhook endpoint: https://yourapp.com/webhooks/stripe

IIS
Install ASP.NET Core Hosting Bundle

Set application pool to No Managed Code

🔄 Idempotent Stripe Webhooks
All webhook handlers store StripeEventId in the database. Duplicate events are ignored, ensuring payment and refund actions are never processed twice.

📁 See: Cartiva.Infrastructure/Webhooks/StripeWebhookHandler.cs

📂 Key Files
File	Purpose
CartivaWeb/Program.cs	Startup, DI, middleware
CartivaWeb/Controllers/*	MVC controllers (Admin, Customer, etc.)
Cartiva.Application/Services/*	Business logic
Cartiva.Domain/Entities/*	Domain models
Cartiva.Persistence/ApplicationDbContext.cs	EF Core context
Cartiva.Infrastructure/BackgroundJobs/*	Hangfire jobs (email, expiry, webhooks)
Cartiva.Infrastructure/Logging/SerilogConfig.cs	Structured logging
Cartiva.Shared/SD.cs	Constants (roles, statuses, carriers)
🔮 Future Improvements

#	Improvement	Priority	Effort
1	Add background job processor (Hangfire) for async tasks – already planned ✅	🔴 High	Medium
2	Implement idempotent Stripe webhooks – already done ✅	🔴 High	Low
3	Add structured logging (Serilog) – integrated ✅	🟡 Medium	Low
4	Introduce caching for product catalog – in progress	🟡 Medium	Low
5	Expand deployment guide – done (Docker, Azure, IIS) ✅	🟡 Medium	Medium
6	Document promotion rule engine – detailed evaluation logic	🟡 Medium	Low
7	Add Swagger/OpenAPI for any JSON endpoints	🟢 Low	Medium
8	Increase integration test coverage for checkout & webhooks	🟡 Medium	High
9	Clarify employee company permissions – already detailed ✅	🟢 Low	Low
10	Add troubleshooting section – common issues (webhook failures, API rate limits)	🟢 Low	Low


📄 License
MIT © Hassan Samatar

🙌 Acknowledgments
Built with ❤️ using .NET 10, ASP.NET Core, and the open-source community.

📫 Questions or feedback? Open an issue on GitHub.