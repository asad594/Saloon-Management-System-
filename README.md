<div align="center">

  <img src="banner.png" alt="StylOnyx Luxury Beauty Studio Banner" width="100%" style="border-radius: 16px; margin-bottom: 24px;" />

  # 💇‍♀️ StylOnyx &mdash; Luxury Beauty Studio 👑
  
  **An Executive Luxury Database Management System & Full-Stack Web Application**

  [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
  [![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
  [![Microsoft SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
  [![Bootstrap](https://img.shields.io/badge/Theme-Slate%20Gold%20Teal-d4af37?style=for-the-badge)](https://github.com/)

  <p align="center">
    <i>StylOnyx is a state-of-the-art enterprise salon management portal built for high-end beauty salons, spa parlors, and wellness centers. Featuring executive analytics, multi-admin management, appointment booking, inventory tracking, and real-time security audit logs.</i>
  </p>

</div>

---

## 🌟 Highlights & Key Features

### 🔐 1. Unified Authentication System
- **Single Portal Login**: Unified login interface for both **Executive Administrators** and **Customer/Staff Users**.
- **Role-Based Redirection**: Automatic role verification directing Admins to the Executive Studio and Users to the Booking Portal.
- **Dynamic Multi-Admin Management**: Existing Admins can create and delegate new Admin accounts with full privilege control.

### 🎨 2. Executive Luxury Admin Studio (`UI/UX Pro Max`)
- **Luxury Aesthetic Palette**: Crafted with Deep Slate Navy (`#131924`), Metallic Gold (`#d4af37`), and Vibrant Teal (`#0d9488`).
- **Live KPI Dashboard**: Real-time financial metrics including Today's Sales, Active Appointments, Staff Availability, and System Online status.
- **Chart.js Revenue Analytics**: Interactive curved visual charts tracking weekly revenue performance.
- **Complete Management Suite**:
  - 💇‍♀️ **Services Catalog**: Add, activate, deactivate, or remove beauty packages & prices.
  - 👩‍💼 **Staff Roster**: Manage specialist profiles, phone contacts, salaries, and specialties.
  - 🏷️ **Brand Vendors**: Track product manufacturers and supplier contacts.
  - 📦 **Inventory Products**: Monitor stock levels, cost prices, and retail rates.
  - 👥 **Client Directory**: Manage customer profile databases.
  - 📜 **Security Audit Logs**: Track user login/logout events powered by SQL Server Triggers.
  - 🧹 **One-Click Database Cleanup**: Reset test records instantly while preserving core system accounts.

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Backend Framework** | ASP.NET Core MVC (C#) |
| **Database Engine** | Microsoft SQL Server (MSSQL) |
| **Data Access Layer** | ADO.NET (`Microsoft.Data.SqlClient`) |
| **Frontend Styling** | Custom CSS3, Glassmorphism, Google Fonts (`Playfair Display` & `Outfit`) |
| **Icons & Alerts** | FontAwesome 6 Pro & SweetAlert2 Modals |
| **Data Analytics** | Chart.js 4.x |

---

## 🗄️ Database Architecture & Schema

The underlying database `salonmanagementsystem` contains 14 structured tables:

```
[ users ] ───────────► [ staff ] ───────────► [ appointments ] ───► [ bills ]
  │                      │                        │                  │
  ├──► [ UserActivityLog ] ├──► [ attendance ]    ├──► [ billdetails ] ◄─── [ paymentmethods ]
  │                      │                        │
  └──► [ clients ] ──────┴────────────────────────┴──► [ salonservices ] ◄──► [ serviceproducts ]
                                                                                   │
[ brands ] ──────────────────────────────────────────► [ products ] ───────────────┘
                                                           │
                                                           └──► [ inventorytransactions ]
```

---

## 🚀 Quick Setup & Installation Guide

### Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with .NET 8.0 SDK)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server/) & SQL Server Management Studio (SSMS)

### Step 1: Clone the Repository
```bash
git clone https://github.com/asad594/Saloon-Management-System-.git
cd Saloon-Management-System-
```

### Step 2: Setup Database
1. Open **SQL Server Management Studio (SSMS)**.
2. Open and execute the master SQL script:
   ```file
   DBMSProject/SETUP_DATABASE_RUN_FIRST.sql
   ```
3. This creates the database `salonmanagementsystem` with all tables, triggers, and default credentials.

### Step 3: Configure Connection String
Check `DBMSProject/SalonManagementSystem/appsettings.json`:
```json
"ConnectionStrings": {
  "SalonDB": "Server=.;Database=salonmanagementsystem;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
> *Note: If using SQL Express, change `Server=.` to `Server=localhost\\SQLEXPRESS`.*

### Step 4: Run the Application
Open terminal in `DBMSProject/SalonManagementSystem` and execute:
```bash
dotnet run --urls "http://localhost:5080"
```
Or open `SalonManagementSystem.sln` in Visual Studio 2022 and press **F5**.

---

---

## 🔐 Default Access Credentials

| Role | Username | Password | Accessible Features |
| :--- | :--- | :--- | :--- |
| **Executive Admin** | `admin` | `admin123` | Executive Dashboard, Analytics, Staff, Services, Brands, Products, Clients, Admins, Activity Logs, Database Reset |
| **Customer / User** | `user` | `user123` | Service Catalog, Staff Selection, Appointment Booking, Invoice Viewer |

---

## 🏆 Version 2.0 Major Enhancements
- 💎 **StylOnyx Branding**: Complete luxury rebranding across all executive & user views.
- 👑 **Multi-Admin Delegation**: Ability to create, manage, and revoke additional executive administrator accounts.
- 🧹 **One-Click Database Reset**: Clean database utility with automated lookup table preservation.
- 📊 **Chart.js Visual Analytics**: Real-time sales and revenue tracking graphs.
- 🛡️ **SQL Trigger Activity Audit**: Live security and authentication logging.

---

<div align="center">

  **StylOnyx Luxury Beauty Studio &mdash; Database Management System (DBMS) Project**  
  ⭐ *Star this repository if you find it helpful!* ⭐

</div>
