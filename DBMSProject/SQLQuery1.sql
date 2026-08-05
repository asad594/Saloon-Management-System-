use salonmanagementsystem

INSERT INTO users(UserId, UserRole, UserPassword)
VALUES
(1, 'Admin', 'admin123'),
(2, 'Staff', 'staff123')


INSERT INTO activestatus VALUES
(101, 'Active'),
(102, 'Inactive'),
(103, 'Completed'),
(104, 'Cancelled'),
(105, 'On Leave'),
(106, 'Scheduled')


INSERT INTO clients(ClientId, ClientName, ClientPhone) VALUES
(201, 'Ali Khan', '03001234567'),
(202, 'Sara Ahmed', '03111234567'),
(203, 'Usman Tariq', '03221234567'),
(204, 'Ayesha Noor', '03331234567'),
(205, 'Bilal Sheikh', '03441234567'),
(206, 'Hina Malik', '03551234567'),
(207, 'Zain Ali', '03661234567'),
(208, 'Fatima Zahra', '03771234567'),
(209, 'Ahmed Raza', '03881234567'),
(210, 'Mariam Khan', '03991234567'),
(211, 'Client 211', '03002110001'),
(212, 'Client 212', '03002120002'),
(213, 'Client 213', '03002130003'),
(214, 'Client 214', '03002140004'),
(215, 'Client 215', '03002150005'),
(216, 'Client 216', '03002160006'),
(217, 'Client 217', '03002170007'),
(218, 'Client 218', '03002180008'),
(219, 'Client 219', '03002190009'),
(220, 'Client 220', '03002200010');


INSERT INTO staff(StaffId, UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, JoiningDate, StaffSalary, StaffSpecialilty, StaffStatus) VALUES
(301, 1, 'Asad', '03000000001', 'asad@mail.com', 'Karachi', '2023-01-01', 30000, 'Hair', 101),
(302, 1, 'Nida', '03000000002', 'nida@mail.com', 'Karachi', '2023-02-01', 32000, 'Makeup', 101),
(303, 2, 'Aliya', '03000000003', 'aliya@mail.com', 'Karachi', '2023-03-01', 28000, 'Nails', 101),
(304, 2, 'Usman', '03000000004', 'usman@mail.com', 'Karachi', '2023-04-01', 35000, 'Hair', 102),
(305, 2, 'Hira', '03000000005', 'hira@mail.com', 'Karachi', '2023-05-01', 30000, 'Facial', 101),
(306, 2, 'Zoya', '03000000006', 'zoya@mail.com', 'Karachi', '2023-06-01', 27000, 'Makeup', 105),
(307, 2, 'Ahmed', '03000000007', 'ahmed@mail.com', 'Karachi', '2023-07-01', 31000, 'Hair', 105),
(308, 2, 'Sana', '03000000008', 'sana@mail.com', 'Karachi', '2023-08-01', 29000, 'Nails', 101),
(309, 2, 'Bilal', '03000000009', 'bilal@mail.com', 'Karachi', '2023-09-01', 33000, 'Facial', 102),
(310, 2, 'Noor', '03000000010', 'noor@mail.com', 'Karachi', '2023-10-01', 30000, 'Hair', 101);


INSERT INTO salonservices(ServiceId, ServiceName, ServicePrice, ServiceTime, ServiceStatus) VALUES
(401, 'Hair Cut', 500, '00:30:00', 101),
(402, 'Facial', 1500, '01:00:00', 101),
(403, 'Manicure', 800, '00:45:00', 101),
(404, 'Pedicure', 900, '00:45:00', 102),
(405, 'Hair Color', 2500, '02:00:00', 101),
(406, 'Makeup', 3000, '01:30:00', 101),
(407, 'Hair Spa', 2000, '01:15:00', 101),
(408, 'Waxing', 1200, '01:00:00', 101),
(409, 'Threading', 200, '00:15:00', 101),
(410, 'Bleach', 700, '00:30:00', 101);


INSERT INTO paymentmethods(methodId, methodType) VALUES
(501, 'Cash'),
(502, 'Card'),
(503, 'Easypaisa'),
(504, 'JazzCash'),
(505, 'Bank Transfer');


INSERT INTO appointments(AppId, CId, App_Booked_For, AppTime, AppDate, AppStatus, App_Booked_By) VALUES
(601, 201, 301, '10:00', '2026-05-01', 103, 302),
(602, 205, 302, '11:00', '2026-05-01', 103, 303),
(603, 207, 303, '12:00', '2026-05-01', 104, 304),
(604, 202, 305, '01:00', '2026-05-02', 103, 305),
(605, 209, 306, '02:00', '2026-05-02', 104, 303),
(606, 203, 307, '03:00', '2026-05-02', 106, 307),
(607, 207, 308, '04:00', '2026-05-03', 106, 308),
(608, 201, 309, '05:00', '2026-05-03', 106, 309),
(609, 208, 301, '06:00', '2026-05-03', 106, 310),
(610, 210, 301, '07:00', '2026-05-04', 106, 302);


INSERT INTO bills(billId, AppointId, ClId, BillDate, TotalAmount, PayId) VALUES
(701, 601, 201, '2026-05-01', 500, 501),
(702, null, 211, '2026-05-01', 1500, 502),
(703, null, 217, '2026-05-01', 800, 503),
(704, 604, 202, '2026-05-02', 900, 504),
(705, 605, 209, '2026-05-02', 2500, 505),
(706, null, 216, '2026-05-02', 3000, 501),
(707, null, 217, '2026-05-03', 2000, 502),
(708, null, 218, '2026-05-03', 1200, 503),
(709, 609, 208, '2026-05-03', 200, 504),
(710, 610, 210, '2026-05-04', 700, 505);


INSERT INTO billdetails (BillDetailId, BId, ServId, BDPrice) VALUES
(801, 701, 401, 500),
(802, 703, 405, 1500),
(803, 702, 402, 800),
(804, 705, 404, 900),
(805, 704, 403, 2500),
(806, 707, 406, 3000),
(807, 706, 407, 2000),
(808, 709, 409, 1200),
(809, 708, 408, 200),
(810, 710, 410, 700);


INSERT INTO brands(BrandId, BrandName, BrandContact, BrandStatus) VALUES
(901, 'LOreal', '03001111111', 101),
(902, 'Garnier', '03002222222', 101),
(903, 'Dove', '03003333333', 101),
(904, 'Nivea', '03004444444', 102),
(905, 'Sunsilk', '03005555555', 102),
(906, 'Pantene', '03006666666', 101),
(907, 'Revlon', '03007777777', 102),
(908, 'Maybelline', '03008888888', 101),
(909, 'Olay', '03009999999', 101),
(910, 'Lakme', '03001010101', 101);

INSERT INTO products(ProductId, ProductName, BrId, ProductQuantity, CostPrice, SellingPrice, ProStatus) VALUES
(1001, 'Shampoo', 901, 50, 300, 500, 101),
(1002, 'Conditioner', 902, 40, 250, 450, 101),
(1003, 'Face Wash', 905, 60, 200, 350, 101),
(1004, 'Cream', 907, 30, 400, 600, 102),
(1005, 'Hair Oil', 905, 70, 150, 300, 101),
(1006, 'Hair Color', 902, 20, 800, 1200, 101),
(1007, 'Foundation', 907, 25, 900, 1500, 102),
(1008, 'Lipstick', 908, 35, 500, 900, 101),
(1009, 'Serum', 909, 45, 700, 1100, 101),
(1010, 'Face Mask', 901, 55, 300, 600, 101);


INSERT INTO inventorytransactions (TransactionId, ProId, TransactionType, InventoryQuantity, InventoryDate) VALUES
(2001, 1003, 'IN', 20, '2026-05-01'),
(2002, 1001, 'OUT', 10, '2026-05-01'),
(2003, 1005, 'IN', 15, '2026-05-02'),
(2004, 1002, 'OUT', 5, '2026-05-02'),
(2005, 1008, 'IN', 25, '2026-05-02'),
(2006, 1004, 'OUT', 8, '2026-05-03'),
(2007, 1007, 'IN', 12, '2026-05-03'),
(2008, 1006, 'OUT', 6, '2026-05-03'),
(2009, 1010, 'IN', 18, '2026-05-04'),
(2010, 1009, 'OUT', 7, '2026-05-04');


INSERT INTO serviceproducts (SerProId, SerId, PId, SPQuantityUsed) VALUES
(3001, 401, 1003, 2),
(3002, 405, 1001, 1),
(3003, 402, 1005, 1),
(3004, 404, 1002, 2),
(3005, 403, 1008, 1),
(3006, 406, 1004, 1),
(3007, 407, 1007, 2),
(3008, 409, 1006, 1),
(3009, 408, 1010, 1),
(3010, 410, 1009, 2);


INSERT INTO appointmentservices (AppsId, ApId, SeId) VALUES
(4001, 601, 401),
(4002, 603, 405),
(4003, 602, 402),
(4004, 605, 404),
(4005, 604, 403),
(4006, 607, 406),
(4007, 606, 407),
(4008, 609, 409),
(4009, 608, 408),
(4010, 610, 410);
