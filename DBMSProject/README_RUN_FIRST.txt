========================================================================
                     SALON MANAGEMENT SYSTEM
========================================================================

Quick Setup Instructions:

1) Database Setup:
   - Open SQL Server Management Studio (SSMS).
   - Open and execute the script: SETUP_DATABASE_RUN_FIRST.sql
   - This creates database 'salonmanagementsystem' with all required tables,
     triggers, activity logs, and default executive admin account.

2) Configuration:
   - Open 'SalonManagementSystem/appsettings.json'
   - Verify connection string:
     "Server=.;Database=salonmanagementsystem;Trusted_Connection=True;TrustServerCertificate=True;"
   - (If using SQLEXPRESS, update Server=.\SQLEXPRESS)

3) Run the Application:
   - Open 'SalonManagementSystem/SalonManagementSystem.sln' in Visual Studio 2022.
   - Press F5 or Ctrl+F5 to launch the application.

4) Default Access Credentials:
   - Executive Admin Access:
     Username: admin
     Password: admin123

   - Customer / User Portal Access:
     Register a new account on the login page OR use:
     Username: user
     Password: user123

========================================================================
