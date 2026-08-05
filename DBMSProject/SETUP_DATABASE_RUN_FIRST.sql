/*
  SalonManagementSystem database setup
  Run this file once in SQL Server Management Studio before starting the website.
*/

IF DB_ID('salonmanagementsystem') IS NULL
BEGIN
    CREATE DATABASE salonmanagementsystem;
END
GO

USE salonmanagementsystem;
GO

IF OBJECT_ID('dbo.UserActivityLog', 'U') IS NOT NULL DROP TABLE dbo.UserActivityLog;
IF OBJECT_ID('dbo.attendance', 'U') IS NOT NULL DROP TABLE dbo.attendance;
IF OBJECT_ID('dbo.appointmentservices', 'U') IS NOT NULL DROP TABLE dbo.appointmentservices;
IF OBJECT_ID('dbo.serviceproducts', 'U') IS NOT NULL DROP TABLE dbo.serviceproducts;
IF OBJECT_ID('dbo.inventorytransactions', 'U') IS NOT NULL DROP TABLE dbo.inventorytransactions;
IF OBJECT_ID('dbo.billdetails', 'U') IS NOT NULL DROP TABLE dbo.billdetails;
IF OBJECT_ID('dbo.bills', 'U') IS NOT NULL DROP TABLE dbo.bills;
IF OBJECT_ID('dbo.appointments', 'U') IS NOT NULL DROP TABLE dbo.appointments;
IF OBJECT_ID('dbo.products', 'U') IS NOT NULL DROP TABLE dbo.products;
IF OBJECT_ID('dbo.brands', 'U') IS NOT NULL DROP TABLE dbo.brands;
IF OBJECT_ID('dbo.salonservices', 'U') IS NOT NULL DROP TABLE dbo.salonservices;
IF OBJECT_ID('dbo.staff', 'U') IS NOT NULL DROP TABLE dbo.staff;
IF OBJECT_ID('dbo.clients', 'U') IS NOT NULL DROP TABLE dbo.clients;
IF OBJECT_ID('dbo.paymentmethods', 'U') IS NOT NULL DROP TABLE dbo.paymentmethods;
IF OBJECT_ID('dbo.activestatus', 'U') IS NOT NULL DROP TABLE dbo.activestatus;
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;
GO

CREATE TABLE users(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserRole VARCHAR(50) NOT NULL,
    UserPassword VARCHAR(50) NOT NULL
);

CREATE TABLE activestatus(
    StatusId INT IDENTITY(1,1) PRIMARY KEY,
    StatusType VARCHAR(50) NOT NULL
);

CREATE TABLE clients(
    ClientId INT IDENTITY(1,1) PRIMARY KEY,
    ClientName VARCHAR(50) NOT NULL,
    ClientPhone VARCHAR(50) NOT NULL
);

CREATE TABLE paymentmethods(
    methodId INT IDENTITY(1,1) PRIMARY KEY,
    methodType VARCHAR(50) NOT NULL
);

CREATE TABLE staff(
    StaffId INT IDENTITY(1,1) PRIMARY KEY,
    UsId INT NOT NULL,
    StaffName VARCHAR(50) NOT NULL,
    StaffPhone VARCHAR(50) NOT NULL,
    StaffEmail VARCHAR(50),
    StaffAddress VARCHAR(100),
    JoiningDate DATE,
    StaffSalary DECIMAL(10,2),
    StaffSpecialilty VARCHAR(50),
    StaffStatus INT NOT NULL,
    FOREIGN KEY(StaffStatus) REFERENCES activestatus(StatusId),
    FOREIGN KEY(UsId) REFERENCES users(UserID)
);

CREATE TABLE salonservices(
    ServiceId INT IDENTITY(1,1) PRIMARY KEY,
    ServiceName VARCHAR(50) NOT NULL,
    ServicePrice DECIMAL(10,2) NOT NULL,
    ServiceTime TIME NULL,
    ServiceStatus INT NOT NULL,
    FOREIGN KEY(ServiceStatus) REFERENCES activestatus(StatusId)
);

CREATE TABLE appointments(
    AppId INT IDENTITY(1,1) PRIMARY KEY,
    CId INT NOT NULL,
    App_Booked_For INT NOT NULL,
    AppTime TIME NOT NULL,
    AppDate DATE NOT NULL,
    AppStatus INT NOT NULL,
    App_Booked_By INT NOT NULL,
    FOREIGN KEY(AppStatus) REFERENCES activestatus(StatusId),
    FOREIGN KEY(CId) REFERENCES clients(ClientId),
    FOREIGN KEY(App_Booked_For) REFERENCES staff(StaffId),
    FOREIGN KEY(App_Booked_By) REFERENCES staff(StaffId)
);

CREATE TABLE bills(
    BillId INT IDENTITY(1,1) PRIMARY KEY,
    AppointId INT NULL,
    ClId INT NOT NULL,
    BillDate DATE DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2),
    PayId INT NOT NULL,
    FOREIGN KEY(PayId) REFERENCES paymentmethods(methodId),
    FOREIGN KEY(ClId) REFERENCES clients(ClientId),
    FOREIGN KEY(AppointId) REFERENCES appointments(AppId)
);

CREATE TABLE billdetails(
    BillDetailId INT IDENTITY(1,1) PRIMARY KEY,
    BId INT NOT NULL,
    ServId INT NOT NULL,
    BDPrice DECIMAL(10,2),
    FOREIGN KEY(BId) REFERENCES bills(BillId),
    FOREIGN KEY(ServId) REFERENCES salonservices(ServiceId)
);

CREATE TABLE brands(
    BrandId INT IDENTITY(1,1) PRIMARY KEY,
    BrandName VARCHAR(50) NOT NULL,
    BrandContact VARCHAR(50) NOT NULL,
    BrandStatus INT NOT NULL,
    FOREIGN KEY(BrandStatus) REFERENCES activestatus(StatusId)
);

CREATE TABLE products(
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName VARCHAR(50) NOT NULL,
    BrId INT NOT NULL,
    ProductQuantity INT NOT NULL,
    CostPrice DECIMAL(10,2),
    SellingPrice DECIMAL(10,2),
    ProStatus INT NOT NULL,
    FOREIGN KEY(ProStatus) REFERENCES activestatus(StatusId),
    FOREIGN KEY(BrId) REFERENCES brands(BrandId)
);

CREATE TABLE inventorytransactions(
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    ProId INT NOT NULL,
    TransactionType VARCHAR(50) NOT NULL,
    InventoryQuantity INT,
    InventoryDate DATE DEFAULT GETDATE(),
    FOREIGN KEY(ProId) REFERENCES products(ProductId)
);

CREATE TABLE serviceproducts(
    SerProId INT IDENTITY(1,1) PRIMARY KEY,
    SerId INT NOT NULL,
    PId INT NOT NULL,
    SPQuantityUsed INT,
    FOREIGN KEY(SerId) REFERENCES salonservices(ServiceId),
    FOREIGN KEY(PId) REFERENCES products(ProductId)
);

CREATE TABLE appointmentservices(
    AppsId INT IDENTITY(1,1) PRIMARY KEY,
    ApId INT NOT NULL,
    SeId INT NOT NULL,
    FOREIGN KEY(SeId) REFERENCES salonservices(ServiceId),
    FOREIGN KEY(ApId) REFERENCES appointments(AppId)
);

CREATE TABLE attendance(
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    StaffId INT NOT NULL,
    CheckIn DATETIME,
    CheckOut DATETIME,
    FOREIGN KEY (StaffId) REFERENCES staff(StaffId)
);

CREATE TABLE UserActivityLog(
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT NULL,
    UserRole VARCHAR(50),
    ActionType VARCHAR(50),
    ActionTime DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO activestatus (StatusType) VALUES ('Active'),('Inactive'),('Scheduled'),('Completed'),('Cancelled'),('On-Leave');
INSERT INTO paymentmethods (methodType) VALUES ('Cash'),('Card'),('Easypaisa'),('JazzCash'),('Bank Transfer');
INSERT INTO users (UserRole, UserPassword) VALUES ('Admin','123'),('Staff','987');

INSERT INTO clients (ClientName, ClientPhone) VALUES
('Ali Khan','03001234567'),('Sara Ahmed','03111234567'),('Usman Tariq','03221234567'),('Ayesha Noor','03331234567'),('Bilal Sheikh','03441234567');

INSERT INTO staff(UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, JoiningDate, StaffSalary, StaffSpecialilty, StaffStatus) VALUES
(1,'Asad','03000000001','asad@mail.com','Karachi','2023-01-01',30000,'Hair',1),
(1,'Nida','03000000002','nida@mail.com','Karachi','2023-02-01',32000,'Makeup',1),
(2,'Aliya','03000000003','aliya@mail.com','Karachi','2023-03-01',28000,'Nails',1),
(2,'Usman','03000000004','usman@mail.com','Karachi','2023-04-01',35000,'Hair',2),
(2,'Hira','03000000005','hira@mail.com','Karachi','2023-05-01',30000,'Facial',1);

INSERT INTO salonservices (ServiceName, ServicePrice, ServiceTime, ServiceStatus) VALUES
('Hair Cut',500,'00:30:00',1),('Facial',1500,'01:00:00',1),('Manicure',800,'00:45:00',1),
('Pedicure',900,'00:45:00',2),('Hair Color',2500,'02:00:00',1),('Makeup',3000,'01:30:00',1);

INSERT INTO appointments(CId, App_Booked_For, AppTime, AppDate, AppStatus, App_Booked_By) VALUES
(1,1,'10:00',CAST(GETDATE() AS DATE),3,2),(2,2,'11:00',CAST(GETDATE() AS DATE),3,3),(3,3,'12:00',CAST(GETDATE() AS DATE),4,4);

INSERT INTO bills(AppointId, ClId, BillDate, TotalAmount, PayId) VALUES
(1,1,GETDATE(),500,1),(NULL,2,GETDATE(),1500,2),(NULL,3,GETDATE(),800,3);

INSERT INTO billdetails (BId, ServId, BDPrice) VALUES (1,1,500),(2,2,1500),(3,3,800);

INSERT INTO brands (BrandName, BrandContact, BrandStatus) VALUES
('LOreal','03001111111',1),('Garnier','03002222222',1),('Dove','03003333333',1);

INSERT INTO products(ProductName, BrId, ProductQuantity, CostPrice, SellingPrice, ProStatus) VALUES
('Shampoo',1,50,300,500,1),('Conditioner',2,40,250,450,1),('Face Wash',3,60,200,350,1);

INSERT INTO inventorytransactions(ProId, TransactionType, InventoryQuantity, InventoryDate) VALUES
(1,'IN',20,GETDATE()),(2,'OUT',5,GETDATE()),(3,'IN',15,GETDATE());

INSERT INTO serviceproducts (SerId, PId, SPQuantityUsed) VALUES (1,1,2),(2,2,1),(3,3,1);
INSERT INTO appointmentservices (ApId, SeId) VALUES (1,1),(2,2),(3,3);
GO

-- Login credentials:
-- Admin / 123
-- Staff / 987
