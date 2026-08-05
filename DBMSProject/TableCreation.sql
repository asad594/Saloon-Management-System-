CREATE DATABASE salonmanagementsystem

USE salonmanagementsystem

-- USERS
CREATE TABLE users(
UserID INT IDENTITY(1,1) PRIMARY KEY,
UserRole VARCHAR(50),
UserPassword VARCHAR(50) NOT NULL
)

-- ACTIVE STATUS
CREATE TABLE activestatus(
StatusId INT IDENTITY(1,1) PRIMARY KEY,
StatusType VARCHAR(50)
)

-- CLIENTS
CREATE TABLE clients(
ClientId INT IDENTITY(1,1) PRIMARY KEY,
ClientName VARCHAR(50) NOT NULL,
ClientPhone VARCHAR(50) NOT NULL
)

-- STAFF
CREATE TABLE staff(
StaffId INT IDENTITY(1,1) PRIMARY KEY,
UsId INT NOT NULL,
StaffName VARCHAR(50) NOT NULL,
StaffPhone VARCHAR(50) NOT NULL,
StaffEmail VARCHAR(50),
StaffAddress VARCHAR(50),
JoiningDate DATE,
StaffSalary DECIMAL(10,2),
StaffSpecialilty VARCHAR(50),
StaffStatus INT NOT NULL,

FOREIGN KEY(StaffStatus) REFERENCES activestatus(StatusId),
FOREIGN KEY(UsId) REFERENCES users(UserId)
)

-- APPOINTMENTS
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
)

-- SERVICES
CREATE TABLE salonservices(
ServiceId INT IDENTITY(1,1) PRIMARY KEY,
ServiceName VARCHAR(50) NOT NULL,
ServicePrice DECIMAL(10,2),
ServiceTime TIME,
ServiceStatus INT NOT NULL,

FOREIGN KEY(ServiceStatus) REFERENCES activestatus(StatusId)
)

-- PAYMENT METHODS
CREATE TABLE paymentmethods(
methodId INT IDENTITY(1,1) PRIMARY KEY,
methodType VARCHAR(50)
)

-- BILLS
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
)

-- BILL DETAILS
CREATE TABLE billdetails(
BillDetailId INT IDENTITY(1,1) PRIMARY KEY,
BId INT NOT NULL,
ServId INT NOT NULL,
BDPrice DECIMAL(10,2),

FOREIGN KEY(BId) REFERENCES bills(BillId),
FOREIGN KEY(ServId) REFERENCES salonservices(ServiceId)
)

-- BRANDS
CREATE TABLE brands(
BrandId INT IDENTITY(1,1) PRIMARY KEY,
BrandName VARCHAR(50) NOT NULL,
BrandContact VARCHAR(50) NOT NULL,
BrandStatus INT NOT NULL,

FOREIGN KEY(BrandStatus) REFERENCES activestatus(StatusId)
)

-- PRODUCTS
CREATE TABLE products(
ProductId INT IDENTITY(1,1) PRIMARY KEY,
ProductName VARCHAR(50) NOT NULL,
BrId INT,
ProductQuantity INT NOT NULL,
CostPrice DECIMAL(10,2),
SellingPrice DECIMAL(10,2),
ProStatus INT NOT NULL,

FOREIGN KEY(ProStatus) REFERENCES activestatus(StatusId),
FOREIGN KEY(BrId) REFERENCES brands(BrandId)
)

-- INVENTORY TRANSACTIONS
CREATE TABLE inventorytransactions(
TransactionId INT IDENTITY(1,1) PRIMARY KEY,
ProId INT NOT NULL,
TransactionType VARCHAR(50) NOT NULL,
InventoryQuantity INT,
InventoryDate DATE DEFAULT GETDATE(),

FOREIGN KEY(ProId) REFERENCES products(ProductId)
)

-- SERVICE PRODUCTS
CREATE TABLE serviceproducts(
SerProId INT IDENTITY(1,1) PRIMARY KEY,
SerId INT NOT NULL,
PId INT NOT NULL,
SPQuantityUsed INT,

FOREIGN KEY(SerId) REFERENCES salonservices(ServiceId),
FOREIGN KEY(PId) REFERENCES products(ProductId)
)

-- APPOINTMENT SERVICES
CREATE TABLE appointmentservices(
AppsId INT IDENTITY(1,1) PRIMARY KEY,
ApId INT NOT NULL,
SeId INT NOT NULL,

FOREIGN KEY(SeId) REFERENCES salonservices(ServiceId),
FOREIGN KEY(ApId) REFERENCES appointments(AppId)
)

-- ATTENDENCE 
CREATE TABLE attendance(
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    StaffId INT NOT NULL,
    CheckIn DATETIME,
    CheckOut DATETIME,
    FOREIGN KEY (StaffId) REFERENCES staff(StaffId)
)