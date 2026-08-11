/*
  SalonManagementSystem Safe & Non-Destructive Database Setup
  This script safely initializes database 'salonmanagementsystem' and creates required tables if they do not exist.
  Existing tables, staff, clients, and transaction data will NEVER be dropped or removed.
*/

IF DB_ID('salonmanagementsystem') IS NULL
BEGIN
    CREATE DATABASE salonmanagementsystem;
END
GO

USE salonmanagementsystem;
GO

IF OBJECT_ID('dbo.users', 'U') IS NULL
BEGIN
    CREATE TABLE users(
        UserID INT IDENTITY(1,1) PRIMARY KEY,
        UserName VARCHAR(50) NOT NULL,
        UserRole VARCHAR(50) NOT NULL,
        UserPassword VARCHAR(50) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.activestatus', 'U') IS NULL
BEGIN
    CREATE TABLE activestatus(
        StatusId INT IDENTITY(1,1) PRIMARY KEY,
        StatusType VARCHAR(50) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.clients', 'U') IS NULL
BEGIN
    CREATE TABLE clients(
        ClientId INT IDENTITY(1,1) PRIMARY KEY,
        ClientName VARCHAR(50) NOT NULL,
        ClientPhone VARCHAR(50) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.paymentmethods', 'U') IS NULL
BEGIN
    CREATE TABLE paymentmethods(
        methodId INT IDENTITY(1,1) PRIMARY KEY,
        methodType VARCHAR(50) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.staff', 'U') IS NULL
BEGIN
    CREATE TABLE staff(
        StaffId INT IDENTITY(1,1) PRIMARY KEY,
        UsId INT NOT NULL,
        StaffName VARCHAR(50) NOT NULL,
        StaffPhone VARCHAR(50) NOT NULL,
        StaffEmail VARCHAR(50),
        StaffAddress VARCHAR(100),
        JoiningDate DATE DEFAULT GETDATE(),
        StaffSalary DECIMAL(10,2),
        StaffSpecialilty VARCHAR(50),
        StaffStatus INT NOT NULL,
        FOREIGN KEY(StaffStatus) REFERENCES activestatus(StatusId),
        FOREIGN KEY(UsId) REFERENCES users(UserID) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.salonservices', 'U') IS NULL
BEGIN
    CREATE TABLE salonservices(
        ServiceId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceName VARCHAR(50) NOT NULL,
        ServicePrice DECIMAL(10,2) NOT NULL,
        ServiceTime TIME NULL,
        ServiceStatus INT NOT NULL,
        FOREIGN KEY(ServiceStatus) REFERENCES activestatus(StatusId)
    );
END
GO

IF OBJECT_ID('dbo.appointments', 'U') IS NULL
BEGIN
    CREATE TABLE appointments(
        AppId INT IDENTITY(1,1) PRIMARY KEY,
        CId INT NOT NULL,
        App_Booked_For INT NOT NULL,
        AppTime TIME NOT NULL,
        AppDate DATE NOT NULL,
        AppStatus INT NOT NULL,
        App_Booked_By INT NOT NULL,
        FOREIGN KEY(AppStatus) REFERENCES activestatus(StatusId),
        FOREIGN KEY(CId) REFERENCES clients(ClientId) ON DELETE CASCADE,
        FOREIGN KEY(App_Booked_For) REFERENCES staff(StaffId),
        FOREIGN KEY(App_Booked_By) REFERENCES staff(StaffId)
    );
END
GO

IF OBJECT_ID('dbo.bills', 'U') IS NULL
BEGIN
    CREATE TABLE bills(
        BillId INT IDENTITY(1,1) PRIMARY KEY,
        AppointId INT NULL,
        ClId INT NOT NULL,
        BillDate DATE DEFAULT GETDATE(),
        TotalAmount DECIMAL(10,2),
        PayId INT NOT NULL,
        FOREIGN KEY(PayId) REFERENCES paymentmethods(methodId),
        FOREIGN KEY(ClId) REFERENCES clients(ClientId) ON DELETE CASCADE,
        FOREIGN KEY(AppointId) REFERENCES appointments(AppId)
    );
END
GO

IF OBJECT_ID('dbo.billdetails', 'U') IS NULL
BEGIN
    CREATE TABLE billdetails(
        BillDetailId INT IDENTITY(1,1) PRIMARY KEY,
        BId INT NOT NULL,
        ServId INT NOT NULL,
        BDPrice DECIMAL(10,2),
        FOREIGN KEY(BId) REFERENCES bills(BillId) ON DELETE CASCADE,
        FOREIGN KEY(ServId) REFERENCES salonservices(ServiceId)
    );
END
GO

IF OBJECT_ID('dbo.brands', 'U') IS NULL
BEGIN
    CREATE TABLE brands(
        BrandId INT IDENTITY(1,1) PRIMARY KEY,
        BrandName VARCHAR(50) NOT NULL,
        BrandContact VARCHAR(50) NOT NULL,
        BrandStatus INT NOT NULL,
        FOREIGN KEY(BrandStatus) REFERENCES activestatus(StatusId)
    );
END
GO

IF OBJECT_ID('dbo.products', 'U') IS NULL
BEGIN
    CREATE TABLE products(
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        ProductName VARCHAR(50) NOT NULL,
        BrId INT NOT NULL,
        ProductQuantity INT NOT NULL,
        CostPrice DECIMAL(10,2),
        SellingPrice DECIMAL(10,2),
        ProStatus INT NOT NULL,
        FOREIGN KEY(ProStatus) REFERENCES activestatus(StatusId),
        FOREIGN KEY(BrId) REFERENCES brands(BrandId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.inventorytransactions', 'U') IS NULL
BEGIN
    CREATE TABLE inventorytransactions(
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        ProId INT NOT NULL,
        TransactionType VARCHAR(50) NOT NULL,
        InventoryQuantity INT,
        InventoryDate DATE DEFAULT GETDATE(),
        FOREIGN KEY(ProId) REFERENCES products(ProductId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.serviceproducts', 'U') IS NULL
BEGIN
    CREATE TABLE serviceproducts(
        SerProId INT IDENTITY(1,1) PRIMARY KEY,
        SerId INT NOT NULL,
        PId INT NOT NULL,
        SPQuantityUsed INT,
        FOREIGN KEY(SerId) REFERENCES salonservices(ServiceId) ON DELETE CASCADE,
        FOREIGN KEY(PId) REFERENCES products(ProductId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.appointmentservices', 'U') IS NULL
BEGIN
    CREATE TABLE appointmentservices(
        AppsId INT IDENTITY(1,1) PRIMARY KEY,
        ApId INT NOT NULL,
        SeId INT NOT NULL,
        FOREIGN KEY(SeId) REFERENCES salonservices(ServiceId),
        FOREIGN KEY(ApId) REFERENCES appointments(AppId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.attendance', 'U') IS NULL
BEGIN
    CREATE TABLE attendance(
        AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
        StaffId INT NOT NULL,
        CheckIn DATETIME,
        CheckOut DATETIME,
        FOREIGN KEY (StaffId) REFERENCES staff(StaffId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.UserActivityLog', 'U') IS NULL
BEGIN
    CREATE TABLE UserActivityLog(
        LogId INT IDENTITY PRIMARY KEY,
        UserId INT NULL,
        UserRole VARCHAR(50),
        ActionType VARCHAR(50),
        LogMessage VARCHAR(255) NULL,
        ActionTime DATETIME DEFAULT GETDATE()
    );
END
GO

/* ── Essential System Lookup Seeds ── */
IF NOT EXISTS (SELECT 1 FROM activestatus WHERE StatusType = 'Active')
BEGIN
    INSERT INTO activestatus (StatusType) VALUES ('Active'),('Inactive'),('Scheduled'),('Completed'),('Cancelled'),('On-Leave');
END
GO

IF NOT EXISTS (SELECT 1 FROM paymentmethods WHERE methodType = 'Cash')
BEGIN
    INSERT INTO paymentmethods (methodType) VALUES ('Cash'),('Card'),('Easypaisa'),('JazzCash'),('Bank Transfer');
END
GO

/* ── Seed Default Executive Admin Account ── */
IF NOT EXISTS (SELECT 1 FROM users WHERE UserName = 'admin')
BEGIN
    INSERT INTO users (UserName, UserRole, UserPassword) VALUES ('admin', 'Admin', 'admin123');
END
GO
