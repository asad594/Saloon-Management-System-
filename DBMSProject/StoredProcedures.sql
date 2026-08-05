CREATE PROCEDURE sp_AddClient
@ClientName VARCHAR(50),
@ClientPhone VARCHAR(50)
AS
BEGIN
    INSERT INTO clients (ClientName, ClientPhone)
    VALUES (@ClientName, @ClientPhone)
END
GO


CREATE PROCEDURE sp_GetClientByPhone
@Phone VARCHAR(50)
AS
BEGIN
    SELECT *
    FROM clients
    WHERE ClientPhone = @Phone
END
GO


CREATE PROCEDURE sp_SearchClientByName
@Name VARCHAR(50)
AS
BEGIN
    SELECT TOP 10 ClientId, ClientName, ClientPhone
    FROM clients
    WHERE ClientName LIKE @Name + '%'
END
GO


CREATE PROCEDURE sp_GetStaffDropdown
AS
BEGIN
    SELECT StaffId, StaffName
    FROM staff
END
GO


CREATE PROCEDURE sp_SearchStaffByName
@Name VARCHAR(50)
AS
BEGIN
    SELECT StaffId, StaffName, StaffPhone
    FROM staff
    WHERE StaffName LIKE @Name + '%'
END
GO


CREATE PROCEDURE sp_AddStaff
@UserId INT,
@StaffName VARCHAR(50),
@StaffPhone VARCHAR(50),
@StaffEmail VARCHAR(50),
@StaffAddress VARCHAR(50),
@JoiningDate DATE,
@StaffSalary DECIMAL(10,2),
@StaffSpeciality VARCHAR(50),
@StaffStatus INT
AS
BEGIN
    INSERT INTO staff
    (UsId, StaffName, StaffPhone, StaffEmail, StaffAddress,
     JoiningDate, StaffSalary, StaffSpecialilty, StaffStatus)
    VALUES
    (@UserId, @StaffName, @StaffPhone, @StaffEmail, @StaffAddress,
     @JoiningDate, @StaffSalary, @StaffSpeciality, @StaffStatus)
END
GO


CREATE PROCEDURE sp_CreateAppointment
@ClientId INT,
@BookedFor INT,
@BookedBy INT,
@AppTime TIME,
@AppDate DATE
AS
BEGIN
    DECLARE @StatusId INT

    SELECT @StatusId = StatusId 
    FROM activestatus 
    WHERE StatusType = 'Scheduled'

    INSERT INTO appointments
    (CId, App_Booked_For, App_Booked_By, AppTime, AppDate, AppStatus)
    VALUES
    (@ClientId, @BookedFor, @BookedBy, @AppTime, @AppDate, @StatusId)
END
GO


CREATE PROCEDURE sp_GetAppointmentsDetailed
AS
BEGIN
    SELECT 
        a.AppId,
        c.ClientName,
        s1.StaffName AS BookedFor,
        s2.StaffName AS BookedBy,
        a.AppDate,
        a.AppTime,
        a.AppStatus
    FROM appointments a
    JOIN clients c ON a.CId = c.ClientId
    JOIN staff s1 ON a.App_Booked_For = s1.StaffId
    JOIN staff s2 ON a.App_Booked_By = s2.StaffId
END
GO



CREATE PROCEDURE sp_GetServices
AS
BEGIN
    SELECT ServiceId, ServiceName, ServicePrice
    FROM salonservices
END
GO


CREATE PROCEDURE sp_CreateBill
@AppointmentId INT = NULL,
@ClientId INT,
@TotalAmount DECIMAL(10,2),
@PaymentMethodId INT
AS
BEGIN
    INSERT INTO bills
    (AppointId, ClId, BillDate, TotalAmount, PayId)
    VALUES
    (@AppointmentId, @ClientId, GETDATE(), @TotalAmount, @PaymentMethodId)
END
GO

CREATE PROCEDURE sp_LoginUser
@Role VARCHAR(50),
@Password VARCHAR(50)
AS
BEGIN
    SELECT *
    FROM users
    WHERE UserRole = @Role AND UserPassword = @Password
END
GO


CREATE PROCEDURE sp_GetAttendanceHistory
AS
BEGIN
    SELECT 
        StaffId,
        CAST(CheckIn AS DATE) AS Date,
        COUNT(CheckIn) AS TotalCheckIns
    FROM attendance
    GROUP BY StaffId, CAST(CheckIn AS DATE)
END
GO



CREATE PROCEDURE sp_AddAppointmentService
@AppId INT,
@ServiceId INT
AS
BEGIN
    INSERT INTO appointmentservices (ApId, SeId)
    VALUES (@AppId, @ServiceId)
END
GO



CREATE PROCEDURE sp_AddBillDetails
@BillId INT,
@ServiceId INT,
@Price DECIMAL(10,2)
AS
BEGIN
    INSERT INTO billdetails (BId, ServId, BDPrice)
    VALUES (@BillId, @ServiceId, @Price)
END
Go


CREATE PROCEDURE sp_WeeklySales
AS
BEGIN
    SELECT 
        DATENAME(WEEKDAY, BillDate) AS DayName,
        SUM(TotalAmount) AS TotalSales
    FROM bills
    WHERE BillDate >= DATEADD(DAY, -7, GETDATE())
    GROUP BY DATENAME(WEEKDAY, BillDate)
END
GO



CREATE PROCEDURE sp_MonthlyComparison
AS
BEGIN
    SELECT 
        FORMAT(BillDate, 'yyyy-MM') AS Month,
        SUM(TotalAmount) AS TotalSales
    FROM bills
    GROUP BY FORMAT(BillDate, 'yyyy-MM')
END
GO


CREATE PROCEDURE sp_TotalProfit
AS
BEGIN
    SELECT 
        SUM(b.TotalAmount) 
        - ISNULL(SUM(p.CostPrice * sp.SPQuantityUsed),0) AS Profit
    FROM bills b
    LEFT JOIN appointmentservices aps ON aps.ApId = b.AppointId
    LEFT JOIN serviceproducts sp ON sp.SerId = aps.SeId
    LEFT JOIN products p ON p.ProductId = sp.PId
END
GO


CREATE PROCEDURE sp_UpdateStatus
@TableName VARCHAR(50),
@Id INT,
@Status INT
AS
BEGIN
    DECLARE @sql NVARCHAR(MAX)

    SET @sql = 'UPDATE ' + @TableName + ' SET Status = @Status WHERE Id = @Id'

    EXEC sp_executesql @sql,
    N'@Status INT, @Id INT',
    @Status=@Status, @Id=@Id
END
GO



CREATE TABLE UserActivityLog
(
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT,
    UserRole VARCHAR(50),
    ActionType VARCHAR(50),   -- LOGIN / LOGOUT
    ActionTime DATETIME DEFAULT GETDATE()
);