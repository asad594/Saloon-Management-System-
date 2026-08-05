SalonManagementSystem - Ready to Run

1) Open SQL Server Management Studio.
2) Open and run: SETUP_DATABASE_RUN_FIRST.sql
   This creates database salonmanagementsystem with sample data.

3) Open SalonManagementSystem/SalonManagementSystem.sln in Visual Studio 2022.
4) Check connection string in SalonManagementSystem/appsettings.json:
   Server=.;Database=salonmanagementsystem;Trusted_Connection=True;TrustServerCertificate=True;
   If your SQL Server is named instance, change Server=. to your server name, for example:
   Server=localhost\SQLEXPRESS;

5) Run the project.

Login:
Admin / 123
Staff / 987

Main fixes applied:
- Fixed C# column names to match SQL schema.
- Fixed staff/product/brand/service insert errors.
- Fixed appointment booking because ServiceId column was not present in appointments table.
- Added missing Staff/GenerateBill view.
- Fixed duplicated/broken staff layout.
- Removed dependency on missing login stored procedure by using direct parameterized login query.
- Added clean database setup script.
