# 🛒 Cartiva — Full-Stack E-Commerce Platform

A complete e-commerce solution built with **ASP.NET Core (.NET 10)** and **Razor Pages**, featuring role-based access control, Stripe payments, shipment tracking, product reviews, returns & refunds, and a full admin panel.

> 📊 [View ER Diagrams](https://hassansamatar.github.io/cartiva/) — Interactive entity-relationship diagrams hosted on GitHub Pages

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
- **Address autocomplete** via Norwegian address lookup

### 🏢 Company (B2B)
- **Deferred payments** — order now, pay within 30 days
- **Company accounts** with multiple users under one organization
- **Order management** for company representatives

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
├── CartivaWeb/          → ASP.NET Core Razor Pages (UI + Controllers)
│   ├── Areas/
│   │   ├── Admin/       → Admin controllers & views
│   │   ├── Customer/    → Customer controllers & views
│   │   └── Identity/    → ASP.NET Identity (login, register, 2FA)
│   ├── Services/        → Application services
│   └── wwwroot/         → Static files (CSS, JS, images)
├── Models/              → Domain models, ViewModels, interfaces, services
├── DataAccess/          → EF Core DbContext, migrations, seeder
├── MyUtility/           → Constants (SD.cs), helpers
└── docs/                → ER diagrams (GitHub Pages)
```

### Project Layers

| Project | Purpose |
|---|---|
| **CartivaWeb** | Web layer — Razor Pages, controllers, views, routing |
| **Models** | Domain entities, ViewModels, service interfaces & implementations |
| **DataAccess** | `ApplicationDbContext`, EF Core migrations, `DbInitializer` |
| **MyUtility** | Shared constants (`SD.cs`), status helpers, configuration |

---

## 🗄️ Database Schema

**21 tables** — 7 ASP.NET Identity + 14 application models with 24 relationships.

📊 **[Interactive ER Diagrams →](https://hassansamatar.github.io/cartiva/)**

### Application Models

| Model | Purpose |
|---|---|
| `ApplicationUser` | Extends IdentityUser with address, company link |
| `Company` | B2B company accounts |
| `Category` | Product categories with size system |
| `Product` | Product catalog |
| `ProductVariant` | Color / size / price / stock per product |
| `SizeSystem` | Size type definitions (Adult, Kids, Shoes) |
| `SizeValue` | Individual size values (S, M, L, 42, 104cm) |
| `Promotion` | Buy X Get Y Free offers per category |
| `ShoppingCart` | Cart items before checkout |
| `OrderHeader` | Order with shipping / payment info |
| `OrderDetail` | Line items in an order |
| `Shipment` | Tracking, carrier, delivery status |
| `Review` | Product ratings and comments |
| `ReturnRequest` | Product returns and refunds |

---

## 🔐 Roles & Permissions

| Feature | Customer | Company | Employee | Admin |
|---|---|---|---|---|
| Browse & purchase | ✅ | ✅ | — | — |
| Deferred payments | — | ✅ | — | — |
| Write reviews | ✅ | ✅ | — | — |
| Request returns | ✅ | ✅ | — | — |
| Manage products | — | — | ✅ | ✅ |
| Manage orders & shipments | — | — | ✅ | ✅ |
| Moderate reviews | — | — | ✅ | ✅ |
| Process returns & refunds | — | — | ✅ | ✅ |
| Manage promotions | — | — | ✅ | ✅ |
| View users (read-only) | — | — | ✅ | ✅ |
| Manage users & roles | — | — | ❌ | ✅ |
| Delete / deactivate companies | — | — | ❌ | ✅ |

---

## 🛠️ Tech Stack

| Technology | Usage |
|---|---|
| **.NET 10** | Runtime & SDK |
| **ASP.NET Core** | Web framework |
| **Razor Pages** | Server-side UI rendering |
| **Entity Framework Core** | ORM & migrations |
| **SQL Server** | Database |
| **ASP.NET Identity** | Authentication & authorization |
| **Stripe** | Payment processing & refunds |
| **Bootstrap 5** | Responsive UI framework |
| **Bootstrap Icons** | Icon library |
| **SweetAlert2** | Confirmation dialogs & toasts |
| **DataTables** | Sortable, searchable admin tables |
| **Mermaid.js** | ER diagram rendering |
| **QRCoder** | QR code generation for order tracking |
| **Bring API** | Shipping rate calculation |

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
| `CartivaWeb/Program.cs` | App configuration, middleware, DI |
| `DataAccess/ApplicationDbContext.cs` | EF Core DbContext with all DbSets |
| `DataAccess/DbInitializer.cs` | Database seeder (roles, admin, products) |
| `MyUtility/SDcs.cs` | All constants (roles, statuses, carriers) |
| `Models/OrderHeader.cs` | Order model with status helpers |
| `Models/ReturnRequest.cs` | Return & refund tracking |

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
