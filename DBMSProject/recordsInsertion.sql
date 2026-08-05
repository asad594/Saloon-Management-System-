use salonmanagementsystem

INSERT INTO activestatus (StatusType) VALUES 
('Active'), 
('Inactive'), 
('Scheduled'), 
('Completed'), 
('Cancelled'), 
('On-Leave') 

INSERT INTO paymentmethods (methodType) VALUES 
('Cash'), 
('Card'), 
('Easypaisa'), 
('JazzCash'), 
('Bank Transfer');

INSERT INTO users (UserRole, UserPassword) VALUES 
('Admin', '123'),
('Staff', '987')

INSERT INTO clients (ClientName, ClientPhone) VALUES
('Ali Khan', '03001234567'),
('Sara Ahmed', '03111234567'),
('Usman Tariq', '03221234567'),
('Ayesha Noor', '03331234567'),
('Bilal Sheikh', '03441234567'),  
('Hina Malik', '03551234567'),
('Zain Ali', '03661234567'),
('Fatima Zahra', '03771234567'),
('Ahmed Raza', '03881234567'),
('Mariam Khan', '03991234567')

INSERT INTO staff(UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, JoiningDate, StaffSalary, StaffSpecialilty, StaffStatus) VALUES
(1, 'Asad', '03000000001', 'asad@mail.com', 'Karachi', '2023-01-01', 30000, 'Hair', 1),
(1, 'Nida', '03000000002', 'nida@mail.com', 'Karachi', '2023-02-01', 32000, 'Makeup', 1),
(2, 'Aliya', '03000000003', 'aliya@mail.com', 'Karachi', '2023-03-01', 28000, 'Nails', 1),
(2, 'Usman', '03000000004', 'usman@mail.com', 'Karachi', '2023-04-01', 35000, 'Hair', 2),
(2, 'Hira', '03000000005', 'hira@mail.com', 'Karachi', '2023-05-01', 30000, 'Facial', 1)

INSERT INTO salonservices (ServiceName, ServicePrice, ServiceTime, ServiceStatus) VALUES
('Hair Cut', 500, '00:30:00', 1),
('Facial', 1500, '01:00:00', 1),
('Manicure', 800, '00:45:00', 1),
('Pedicure', 900, '00:45:00', 2),
('Hair Color', 2500, '02:00:00', 1),
('Makeup', 3000, '01:30:00', 1)

INSERT INTO appointments(CId, App_Booked_For, AppTime, AppDate, AppStatus, App_Booked_By) VALUES
(1, 1, '10:00', '2026-05-01', 3, 2),
(2, 2, '11:00', '2026-05-01', 3, 3),
(3, 3, '12:00', '2026-05-01', 4, 4)

INSERT INTO bills(AppointId, ClId, BillDate, TotalAmount, PayId) VALUES
(1, 1, GETDATE(), 500, 1),
(NULL, 2, GETDATE(), 1500, 2),
(NULL, 3, GETDATE(), 800, 3)

INSERT INTO billdetails (BId, ServId, BDPrice) VALUES
(1, 1, 500),
(2, 2, 1500),
(3, 3, 800)

INSERT INTO brands (BrandName, BrandContact, BrandStatus) VALUES
('LOreal', '03001111111', 1),
('Garnier', '03002222222', 1),
('Dove', '03003333333', 1)

INSERT INTO products(ProductName, BrId, ProductQuantity, CostPrice, SellingPrice, ProStatus) VALUES
('Shampoo', 1, 50, 300, 500, 1),
('Conditioner', 2, 40, 250, 450, 1),
('Face Wash', 3, 60, 200, 350, 1)

INSERT INTO inventorytransactions(ProId, TransactionType, InventoryQuantity, InventoryDate) VALUES
(1, 'IN', 20, GETDATE()),
(2, 'OUT', 5, GETDATE()),
(3, 'IN', 15, GETDATE())

INSERT INTO serviceproducts (SerId, PId, SPQuantityUsed) VALUES
(1, 1, 2),
(2, 2, 1),
(3, 3, 1)

INSERT INTO appointmentservices (ApId, SeId) VALUES
(1, 1),
(2, 2),
(3, 3)
