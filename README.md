# 🛒 Cartiva — Full-Stack E-Commerce Platform

A complete e-commerce solution built with **ASP.NET Core (.NET 10)** and **Razor Pages**, featuring role-based access control, Stripe payments, shipment tracking, product reviews, returns & refunds, and a full admin panel.

> 📊 [View ER Diagrams](https://hassansamatar.github.io/cartiva/) — Interactive entity-relationship diagrams hosted on GitHub Pages
>
> 🎬 [Watch Demo Video](https://1drv.ms/v/c/ea46c225c4735de5/IQA6wTKv6aIPSogRyCtW1EMZAU7krOhjw7RED2DtuGuykdE?e=BIjc1t) — 5-minute walkthrough of the full application

---

## ✨ Features

### 🛍️ Customer Experience
- **Product catalog** with categories, sizes, colors, and variants
- **Shopping cart** with session persistence
- **Checkout** with Stripe payment integration
- **Order tracking** with QR codes and shipment status
- **Product reviews** with star ratings (post-delivery)
- **Returns & refunds** within a 30-day return window
- **Order history** with detailed receipts
- **Address autocomplete** using Norwegian address lookup (Kartverket API)
- **Flexible address input**: users can either manually enter their address or use Kartverket API for autocomplete
- **Promotion management** — admins and employees can create flexible promotions targeting specific product categories or product variants for a defined time period, with the ability to enable or disable promotions at any time
- **Automatic discount visibility** — the cheapest product variant is automatically highlighted as “On Sale” with a visible badge for customers

### 🏢 Company (B2B)
- **Deferred payments** — company users can either pay upfront or use a 30-day invoice option
- **Company accounts** with multiple users under one organization
- **Order management** for company representatives, including sales-created orders and finance-approved billing

### 🔧 Admin Panel
- **Dashboard** with order and revenue management
- **Product management** — CRUD for products, variants, categories
- **Order processing** — approve, process, ship, deliver
- **Shipment management** with carrier tracking (Posten, Bring, Helthjem, DHL)
- **Review moderation** — approve, reject, delete customer reviews
- **Return management** — approve returns, process Stripe partial refunds
- **Promotion engine** — Buy X Get Y Free per category
- **User management** — roles, activation, deactivation
- **Company management** — B2B company accounts

### 👷 Employee Role
- Full content management (products, orders, shipments, reviews, returns)
- Read-only access to user accounts
- Company management (create/edit, no delete/deactivate)

---

## 🏗️ Architecture

```
Cartiva.sln
├── src/
│   ├── CartivaWeb/              → ASP.NET Core Razor Pages (UI)
│   │   ├── Areas/
│   │   │   ├── Admin/           → Admin features (controllers, views)
│   │   │   ├── Customer/        → Customer features
│   │   │   └── Identity/        → Authentication (ASP.NET Identity, 2FA)
│   │   └── wwwroot/             → Static files (CSS, JS, images)
│   │
│   ├── Cartiva.Application/     → Use cases (interfaces + services)
│   ├── Cartiva.Domain/          → Entities, enums, value objects (pure domain)
│   ├── Cartiva.Persistence/     → EF Core (DbContext, migrations, seeding)
│   ├── Cartiva.Infrastructure/  → External integrations (Stripe, Email, APIs)
│   └── Cartiva.Shared/          → Shared utilities, constants, configs
│
├── docs/                        → ER diagrams (GitHub Pages)
├── tests/                       → Unit & integration tests
└── README.md
              ```

### Project Layers

| Project                       | Purpose |
|-------------------------------|
| **CartivaWeb**                | Presentation layer — Razor Pages, controllers, views, routing |
| **Cartiva.Domain**            | Core domain — entities, enums, value objects |
| **Cartiva.Application**       | Application layer — use cases, service interfaces, DTOs |
| **Cartiva.Persistence**       | Data access layer — `ApplicationDbContext`, EF Core migrations, seeding |
| **Cartiva.Infrastructure**    | External services — APIs (Address, Email, QR Code, Shipping, Payments) |
| **Cartiva.Shared**            | Cross-cutting concerns — constants (`SD.cs`), helpers, shared utilities |

---

## 🗄️ Database Schema

**21 tables** — 7 ASP.NET Identity + 14 application models with 24 relationships.

📊 **[Interactive ER Diagrams →](https://hassansamatar.github.io/cartiva/)**

### Application Models

| Model | Purpose |
|---|---|
| `ApplicationUser` | Extends `IdentityUser` with address and company association |
| `Company` | B2B company accounts with multiple users |
| `Category` | Product categories supporting size systems |
| `Product` | Core product catalog entity |
| `ProductVariant` | Product variations (color, size, price, stock) |
| `SizeSystem` | Defines size types (Adult, Kids, Shoes) |
| `SizeValue` | Individual size values (S, M, L, 42, 104cm) |
| `Promotion` | Discount rules (e.g., Buy X Get Y Free per category) |
| `ShoppingCart` | Cart items before checkout |
| `OrderHeader` | Order summary (shipping and payment information) |
| `OrderDetail` | Individual line items within an order |
| `Shipment` | Shipping tracking (carrier, delivery status) |
| `Review` | Product ratings and customer comments |
| `ReturnRequest` | Handles product returns and refund requests |

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
---

## 🛠️ Tech Stack

| Technology | Usage |
|---|---|
| **.NET 10** | Runtime and SDK |
| **ASP.NET Core** | Web application framework |
| **Razor Pages** | Server-side UI rendering engine |
| **Entity Framework Core** | ORM, database access, and migrations |
| **SQL Server** | Relational database system |
| **ASP.NET Identity** | Authentication and authorization system |
| **Stripe** | Payment processing, subscriptions, and refunds |
| **HTML5 / CSS3 / JavaScript (ES6+)** | Frontend structure, styling, and client-side interactivity |
| **Bootstrap 5** | Responsive UI framework |
| **Bootstrap Icons** | Icon library for UI components |
| **SweetAlert2** | Modern alerts, confirmations, and toast notifications |
| **DataTables** | Advanced tables (sorting, filtering, pagination) |
| **Mermaid.js** | Diagram and ER model visualization |
| **Kartverket API** | Norwegian address lookup and autocomplete |
| **QRCoder** | QR code generation for order tracking |
| **Bring API** | Shipping rates, labels, and delivery integration |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or full instance)
- [Stripe account](https://stripe.com/) (for payment testing)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/hassansamatar/cartiva.git
   cd cartiva
   ```

2. **Configure the database connection**

   Update `CartivaWeb/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Cartiva;Trusted_Connection=True;"
     }
   }
   ```

3. **Configure Stripe keys**

   Add your Stripe test keys to `appsettings.json`:
   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_...",
       "PublishableKey": "pk_test_..."
     }
   }
   ```

4. **Apply migrations and seed data**
   ```bash
   dotnet ef database update --project DataAccess --startup-project CartivaWeb
   ```
   The database is automatically seeded on first run with:
   - Roles (Admin, Employee, Company, Customer)
   - Admin user (`admin@cartiva.com` / `Admin12#`)
   - Sample products, categories, size systems, and variants

5. **Run the application**
   ```bash
   dotnet run --project CartivaWeb
   ```

### Default Admin Credentials

| Email | Password |
|---|---|
| `admin@cartiva.com` | `Admin12#` |

---

## 📂 Key Files

| File | Purpose |
|---|---|
| `CartivaWeb/Program.cs` | Application startup, middleware pipeline, dependency injection |
| `CartivaWeb/Areas/*/Controllers` | UI controllers for Admin, Customer, and Identity areas |
| `Cartiva.Application/` | Application services, use cases, and interfaces |
| `Cartiva.Domain/Entities` | Core domain entities (Product, Order, User, etc.) |
| `Cartiva.Persistence/ApplicationDbContext.cs` | EF Core DbContext with all DbSets |
| `Cartiva.Persistence/Migrations` | Database schema migrations |
| `Cartiva.Persistence/DbInitializer.cs` | Database seeding (roles, admin user, sample data) |
| `Cartiva.Infrastructure/` | External integrations (Stripe, Kartverket, Email, Shipping APIs) |
| `Cartiva.Shared/SD.cs` | Shared constants (roles, statuses, carriers) |
---

## 📊 Documentation

- **[ER Diagrams](https://hassansamatar.github.io/cartiva/)** — Interactive entity-relationship diagrams
  - [Complete ER Diagram](https://hassansamatar.github.io/cartiva/ER-diagram/Entity-Relationship-Diagram.html) — All 21 tables
  - [Application Tables](https://hassansamatar.github.io/cartiva/ER-diagram/er-diagram-application.html) — 14 app models
  - [Identity Tables](https://hassansamatar.github.io/cartiva/ER-diagram/er-diagram-identity.html) — 7 identity tables

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

---

<p align="center">
  Built with ❤️ by <a href="https://github.com/hassansamatar">Hassan Samatar</a>
</p>
