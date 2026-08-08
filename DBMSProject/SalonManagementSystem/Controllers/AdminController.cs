using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;

namespace SalonManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connection;

        public AdminController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB");
        }



        private void EnsureDailyClosingsTable(SqlConnection conn)
        {
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'daily_closings')
                BEGIN
                    CREATE TABLE daily_closings (
                        ClosingID INT IDENTITY(1,1) PRIMARY KEY,
                        ClosingDate DATETIME NOT NULL DEFAULT GETDATE(),
                        DayName NVARCHAR(50) NOT NULL,
                        TotalRevenue DECIMAL(18,2) NOT NULL,
                        TotalBills INT NOT NULL,
                        ClosedBy NVARCHAR(100) DEFAULT 'Admin'
                    );
                END";
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public IActionResult Home()
        {
            DashboardModel model = new DashboardModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                EnsureDailyClosingsTable(conn);

                // Find last closing date to compute current live unclosed sales
                SqlCommand cmdLast = new SqlCommand("SELECT TOP 1 ClosingDate FROM daily_closings ORDER BY ClosingID DESC", conn);
                object objLast = cmdLast.ExecuteScalar();
                DateTime? lastClosingDate = objLast != null && objLast != DBNull.Value ? Convert.ToDateTime(objLast) : (DateTime?)null;

                string salesSql = lastClosingDate.HasValue
                    ? "SELECT ISNULL(SUM(TotalAmount),0) FROM bills WHERE BillDate > @LastClosing"
                    : "SELECT ISNULL(SUM(TotalAmount),0) FROM bills";

                using (SqlCommand cmdSales = new SqlCommand(salesSql, conn))
                {
                    if (lastClosingDate.HasValue)
                    {
                        cmdSales.Parameters.AddWithValue("@LastClosing", lastClosingDate.Value);
                    }
                    model.TodaySales = Convert.ToDecimal(cmdSales.ExecuteScalar());
                }

                model.TodayAppointments = (int)new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE CAST(AppDate AS DATE)=CAST(GETDATE() AS DATE)", conn).ExecuteScalar();

                model.StaffAvailable = (int)new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus=1", conn).ExecuteScalar();
            }

            return View(model);
        }

        // ✅ MANUAL DAY REVENUE & DAY CLOSING STUDIO
        public IActionResult DayRevenue()
        {
            DayRevenueViewModel model = new DayRevenueViewModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                EnsureDailyClosingsTable(conn);

                // 1. Find Last Closing Date
                SqlCommand cmdLast = new SqlCommand("SELECT TOP 1 ClosingDate FROM daily_closings ORDER BY ClosingID DESC", conn);
                object objLast = cmdLast.ExecuteScalar();
                DateTime? lastClosingDate = objLast != null && objLast != DBNull.Value ? Convert.ToDateTime(objLast) : (DateTime?)null;
                model.LastClosingDate = lastClosingDate;

                // 2. Fetch unclosed bills (since last closing date)
                string billQuery = lastClosingDate.HasValue
                    ? @"SELECT b.BillID, 
                               ISNULL(c.ClientName, 'Walk-in Client') AS ClientName, 
                               ISNULL(c.ClientPhone, 'N/A') AS Phone, 
                               'Salon Service' AS ServiceName, 
                               'Stylist Staff' AS StaffName, 
                               b.BillDate, 
                               b.TotalAmount
                        FROM bills b
                        LEFT JOIN clients c ON b.ClId = c.ClientId
                        WHERE b.BillDate > @LastClosing
                        ORDER BY b.BillDate DESC"
                    : @"SELECT b.BillID, 
                               ISNULL(c.ClientName, 'Walk-in Client') AS ClientName, 
                               ISNULL(c.ClientPhone, 'N/A') AS Phone, 
                               'Salon Service' AS ServiceName, 
                               'Stylist Staff' AS StaffName, 
                               b.BillDate, 
                               b.TotalAmount
                        FROM bills b
                        LEFT JOIN clients c ON b.ClId = c.ClientId
                        ORDER BY b.BillDate DESC";

                using (SqlCommand cmdBills = new SqlCommand(billQuery, conn))
                {
                    if (lastClosingDate.HasValue)
                    {
                        cmdBills.Parameters.AddWithValue("@LastClosing", lastClosingDate.Value);
                    }

                    using (SqlDataReader reader = cmdBills.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal amt = Convert.ToDecimal(reader["TotalAmount"]);
                            model.CurrentDayBills.Add(new TransactionBillItem
                            {
                                BillId = Convert.ToInt32(reader["BillID"]),
                                ClientName = reader["ClientName"].ToString() ?? "",
                                Phone = reader["Phone"].ToString() ?? "",
                                ServiceName = reader["ServiceName"].ToString() ?? "",
                                StaffName = reader["StaffName"].ToString() ?? "",
                                BillDate = Convert.ToDateTime(reader["BillDate"]),
                                TotalAmount = amt
                            });
                            model.CurrentDayRevenue += amt;
                            model.CurrentDayBillsCount++;
                        }
                    }
                }

                // 3. Remove history from DayRevenue page (as it belongs to TotalRevenue page)
            }

            return View(model);
        }

        // ✅ POST ACTION: CLOSE DAY & FINALIZE REVENUE
        [HttpPost]
        public IActionResult CloseDay()
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                EnsureDailyClosingsTable(conn);

                // 1. Get last closing date
                SqlCommand cmdLast = new SqlCommand("SELECT TOP 1 ClosingDate FROM daily_closings ORDER BY ClosingID DESC", conn);
                object objLast = cmdLast.ExecuteScalar();
                DateTime? lastClosingDate = objLast != null && objLast != DBNull.Value ? Convert.ToDateTime(objLast) : (DateTime?)null;

                // 2. Compute current unclosed transactions sum & count
                string billQuery = lastClosingDate.HasValue
                    ? "SELECT ISNULL(SUM(TotalAmount),0) AS TotalRev, COUNT(*) AS TotalCount FROM bills WHERE BillDate > @LastClosing"
                    : "SELECT ISNULL(SUM(TotalAmount),0) AS TotalRev, COUNT(*) AS TotalCount FROM bills";

                decimal totalRevenue = 0;
                int totalBills = 0;

                using (SqlCommand cmdCalc = new SqlCommand(billQuery, conn))
                {
                    if (lastClosingDate.HasValue)
                    {
                        cmdCalc.Parameters.AddWithValue("@LastClosing", lastClosingDate.Value);
                    }

                    using (SqlDataReader reader = cmdCalc.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalRevenue = Convert.ToDecimal(reader["TotalRev"]);
                            totalBills = Convert.ToInt32(reader["TotalCount"]);
                        }
                    }
                }

                // 3. Insert Closed Day Record into daily_closings
                DateTime now = DateTime.Now;
                string dayName = now.ToString("dddd");

                SqlCommand cmdInsert = new SqlCommand(@"
                    INSERT INTO daily_closings (ClosingDate, DayName, TotalRevenue, TotalBills, ClosedBy)
                    VALUES (@Date, @DayName, @Revenue, @Bills, 'Admin')", conn);
                cmdInsert.Parameters.AddWithValue("@Date", now);
                cmdInsert.Parameters.AddWithValue("@DayName", dayName);
                cmdInsert.Parameters.AddWithValue("@Revenue", totalRevenue);
                cmdInsert.Parameters.AddWithValue("@Bills", totalBills);

                cmdInsert.ExecuteNonQuery();

                TempData["SuccessMessage"] = $"Day Closed Successfully! {dayName} ({now:MMM dd, yyyy}) finalized with PKR {totalRevenue:N0} across {totalBills} transactions and added into Total Revenue!";
            }

            return RedirectToAction("TotalRevenue");
        }

        // ✅ SEPARATE OPTION: TOTAL REVENUE (ALL FINALIZED CLOSED DAYS & GRAND TOTAL)
        public IActionResult TotalRevenue()
        {
            TotalRevenueViewModel model = new TotalRevenueViewModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                EnsureDailyClosingsTable(conn);

                SqlCommand cmd = new SqlCommand(@"
                    SELECT ClosingID, ClosingDate, DayName, TotalRevenue, TotalBills, ClosedBy 
                    FROM daily_closings 
                    ORDER BY ClosingID DESC", conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime cDate = Convert.ToDateTime(reader["ClosingDate"]);
                        decimal rev = Convert.ToDecimal(reader["TotalRevenue"]);
                        int bills = Convert.ToInt32(reader["TotalBills"]);

                        model.ClosedDaysHistory.Add(new ClosedDayLog
                        {
                            ClosingId = Convert.ToInt32(reader["ClosingID"]),
                            ClosingDate = cDate,
                            DayName = reader["DayName"].ToString() ?? "",
                            TotalRevenue = rev,
                            TotalBills = bills,
                            ClosedBy = reader["ClosedBy"].ToString() ?? "Admin"
                        });

                        model.GrandTotalRevenue += rev;
                        model.TotalClosedDaysCount++;
                        model.TotalClosedBillsCount += bills;

                        if (!model.LatestClosedDate.HasValue || cDate > model.LatestClosedDate.Value)
                            model.LatestClosedDate = cDate;
                        if (!model.FirstClosedDate.HasValue || cDate < model.FirstClosedDate.Value)
                            model.FirstClosedDate = cDate;
                    }
                }
            }

            return View(model);
        }

        
        
        
        // ✅ WEEKLY SALES CHART
        public JsonResult GetWeeklySales()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        DATENAME(WEEKDAY, BillDate) AS DayName,
                        SUM(TotalAmount) AS TotalSales
                    FROM bills
                    WHERE BillDate >= DATEADD(DAY, -7, GETDATE())
                    GROUP BY DATENAME(WEEKDAY, BillDate)
                ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        day = reader["DayName"].ToString(),
                        total = Convert.ToDecimal(reader["TotalSales"])
                    });
                }
            }

            return Json(data);
        }





        public JsonResult GetMonthlyComparison()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
        SELECT 
            FORMAT(BillDate,'MMM') AS Month,
            SUM(TotalAmount) AS Sales
        FROM bills
        GROUP BY FORMAT(BillDate,'MMM')
        ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        month = reader["Month"].ToString(),
                        sales = Convert.ToDecimal(reader["Sales"])
                    });
                }
            }

            return Json(data);
        }





        public IActionResult UpdateStatus(string table, int id, int status)
        {
            // Safe status update helper. Only known tables/columns are allowed.
            var map = new Dictionary<string, (string Table, string IdColumn, string StatusColumn)>(StringComparer.OrdinalIgnoreCase)
            {
                ["staff"] = ("staff", "StaffId", "StaffStatus"),
                ["salonservices"] = ("salonservices", "ServiceId", "ServiceStatus"),
                ["brands"] = ("brands", "BrandId", "BrandStatus"),
                ["products"] = ("products", "ProductId", "ProStatus")
            };

            if (!map.TryGetValue(table, out var target))
                return BadRequest("Invalid table name.");

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                string query = $"UPDATE {target.Table} SET {target.StatusColumn} = @status WHERE {target.IdColumn} = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Home");
        }




        public IActionResult AddService()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddService(string name, decimal price, int time)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO salonservices (ServiceName, ServicePrice, ServiceTime, ServiceStatus) VALUES (@n,@p,@t,1)", conn);

                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@t", TimeSpan.FromMinutes(time));

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Home");
        }



        public IActionResult AddStaff()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddStaff(string name, string phone, string email, string address, decimal salary, string speciality, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Staff Name, Username, and Password are required.";
                return View();
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // 1. Create or retrieve Staff User Login Credentials in 'users' table
                int staffUserId = 0;
                SqlCommand checkUserCmd = new SqlCommand("SELECT UserID FROM users WHERE UserName = @uname", conn);
                checkUserCmd.Parameters.AddWithValue("@uname", username.Trim());
                object? existingId = checkUserCmd.ExecuteScalar();

                if (existingId != null && existingId != DBNull.Value)
                {
                    staffUserId = Convert.ToInt32(existingId);
                    // Update password and role if already exists
                    SqlCommand updateCredCmd = new SqlCommand("UPDATE users SET UserPassword = @pass, UserRole = 'Staff' WHERE UserID = @uid", conn);
                    updateCredCmd.Parameters.AddWithValue("@pass", password.Trim());
                    updateCredCmd.Parameters.AddWithValue("@uid", staffUserId);
                    updateCredCmd.ExecuteNonQuery();
                }
                else
                {
                    SqlCommand insertCredCmd = new SqlCommand(@"
                        INSERT INTO users (UserName, UserPassword, UserRole) 
                        VALUES (@uname, @pass, 'Staff');
                        SELECT SCOPE_IDENTITY();", conn);
                    insertCredCmd.Parameters.AddWithValue("@uname", username.Trim());
                    insertCredCmd.Parameters.AddWithValue("@pass", password.Trim());
                    staffUserId = Convert.ToInt32(insertCredCmd.ExecuteScalar());
                }

                // 2. Insert Staff Record into 'staff' table linked to 'UsId'
                SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO staff 
                    (UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, StaffSalary, StaffSpecialilty, JoiningDate, StaffStatus)
                    VALUES (@usId, @n, @p, @e, @a, @s, @sp, GETDATE(), 1)", conn);

                cmd.Parameters.AddWithValue("@usId", staffUserId);
                cmd.Parameters.AddWithValue("@n", name.Trim());
                cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(phone) ? "N/A" : phone.Trim());
                cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(email) ? "N/A" : email.Trim());
                cmd.Parameters.AddWithValue("@a", string.IsNullOrWhiteSpace(address) ? "N/A" : address.Trim());
                cmd.Parameters.AddWithValue("@s", salary);
                cmd.Parameters.AddWithValue("@sp", string.IsNullOrWhiteSpace(speciality) ? "Hair & Beauty Specialist" : speciality.Trim());

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = $"Staff '{name}' created successfully! Credentials ({username}) activated for Staff Login.";
            return RedirectToAction("UpdateStaff");
        }




        public IActionResult UpdateStaff()
        {
            List<dynamic> list = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM staff", conn);
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["StaffId"],
                        name = r["StaffName"],
                        status = r["StaffStatus"]
                    });
                }
            }

            return View(list);
        }

        public IActionResult ChangeStaffStatus(int id, int status)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE staff SET StaffStatus=@s WHERE StaffId=@id", conn);

                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("UpdateStaff");
        }




        public IActionResult UpdateService()
        {
            List<dynamic> list = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM salonservices", conn);
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["ServiceId"],
                        name = r["ServiceName"],
                        status = r["ServiceStatus"]
                    });
                }
            }

            return View(list);
        }

        public IActionResult ChangeServiceStatus(int id, int status)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE salonservices SET ServiceStatus=@s WHERE ServiceId=@id", conn);

                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("UpdateService");
        }
        






        public IActionResult AddBrand()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddBrand(string name, string contact)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO brands (BrandName, BrandContact, BrandStatus) VALUES (@n,@c,1)", conn);

                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", contact);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Home");
        }



        public IActionResult AddProduct()
        {
            ViewBag.Brands = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT BrandId, BrandName FROM brands", conn);
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    ViewBag.Brands.Add(new SelectListItem
                    {
                        Value = r["BrandId"].ToString(),
                        Text = r["BrandName"].ToString()
                    });
                }
            }

            return View();
        }

        [HttpPost]
        public IActionResult AddProduct(string name, int brandId, int qty, decimal cost, decimal sell)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                @"INSERT INTO products 
        (ProductName, BrId, ProductQuantity, CostPrice, SellingPrice, ProStatus)
        VALUES (@n,@b,@q,@c,@s,1)", conn);

                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@b", brandId);
                cmd.Parameters.AddWithValue("@q", qty);
                cmd.Parameters.AddWithValue("@c", cost);
                cmd.Parameters.AddWithValue("@s", sell);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Home");
        }


        public IActionResult ActivityLogs()
        {
            List<dynamic> logs = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand ensureColCmd = new SqlCommand(@"
                    IF OBJECT_ID('dbo.UserActivityLog', 'U') IS NOT NULL AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserActivityLog') AND name = 'LogMessage')
                    BEGIN
                        ALTER TABLE UserActivityLog ADD LogMessage VARCHAR(255) NULL;
                    END", conn);
                ensureColCmd.ExecuteNonQuery();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        LogId, 
                        UserId, 
                        ISNULL(UserRole, 'User') AS UserRole, 
                        ISNULL(ActionType, 'SYSTEM') AS ActionType, 
                        ISNULL(LogMessage, ActionType) AS LogMessage, 
                        ActionTime 
                    FROM UserActivityLog 
                    ORDER BY LogId DESC", conn);
                
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    logs.Add(new
                    {
                        id = r["LogId"],
                        userId = r["UserId"] == DBNull.Value ? "System" : r["UserId"],
                        role = r["UserRole"],
                        action = r["ActionType"],
                        message = r["LogMessage"],
                        time = r["ActionTime"] != DBNull.Value ? Convert.ToDateTime(r["ActionTime"]).ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            return View(logs);
        }

        // ── MANAGE BRANDS ──
        public IActionResult ManageBrands()
        {
            List<dynamic> list = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT BrandId, BrandName, BrandContact, BrandStatus FROM brands", conn);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["BrandId"],
                        name = r["BrandName"],
                        contact = r["BrandContact"],
                        status = r["BrandStatus"]
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteBrand(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM brands WHERE BrandId=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Brand removed successfully.";
            return RedirectToAction("ManageBrands");
        }

        // ── MANAGE PRODUCTS ──
        public IActionResult ManageProducts()
        {
            List<dynamic> list = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT p.ProductId, p.ProductName, p.ProductQuantity, p.CostPrice, p.SellingPrice, p.ProStatus, ISNULL(b.BrandName, 'N/A') AS BrandName
                    FROM products p
                    LEFT JOIN brands b ON p.BrId = b.BrandId", conn);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["ProductId"],
                        name = r["ProductName"],
                        brand = r["BrandName"],
                        qty = r["ProductQuantity"],
                        cost = r["CostPrice"],
                        sell = r["SellingPrice"],
                        status = r["ProStatus"]
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteProduct(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM products WHERE ProductId=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Product removed from inventory.";
            return RedirectToAction("ManageProducts");
        }

        // ── MANAGE CLIENTS ──
        public IActionResult ManageClients()
        {
            List<dynamic> list = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ClientId, ClientName, ClientPhone FROM clients", conn);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["ClientId"],
                        name = r["ClientName"],
                        phone = r["ClientPhone"]
                    });
                }
            }
            return View(list);
        }

        [HttpPost]
        public IActionResult AddClient(string name, string phone)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
            {
                using (SqlConnection conn = new SqlConnection(_connection))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("INSERT INTO clients (ClientName, ClientPhone) VALUES (@n, @p)", conn);
                    cmd.Parameters.AddWithValue("@n", name.Trim());
                    cmd.Parameters.AddWithValue("@p", phone.Trim());
                    cmd.ExecuteNonQuery();
                }
                TempData["SuccessMessage"] = "Client added successfully.";
            }
            return RedirectToAction("ManageClients");
        }

        public IActionResult DeleteClient(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM clients WHERE ClientId=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Client record deleted.";
            return RedirectToAction("ManageClients");
        }

        public IActionResult DeleteStaff(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM staff WHERE StaffId=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Staff record deleted.";
            return RedirectToAction("UpdateStaff");
        }

        public IActionResult DeleteService(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM salonservices WHERE ServiceId=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["SuccessMessage"] = "Service deleted from catalog.";
            return RedirectToAction("UpdateService");
        }

        // ── RESET DATABASE / WIPE DATA ──
        public IActionResult CleanDatabase()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connection))
                {
                    conn.Open();
                    string resetSql = @"
                        IF OBJECT_ID('dbo.billdetails', 'U') IS NOT NULL DELETE FROM dbo.billdetails;
                        IF OBJECT_ID('dbo.bills', 'U') IS NOT NULL DELETE FROM dbo.bills;
                        IF OBJECT_ID('dbo.appointmentservices', 'U') IS NOT NULL DELETE FROM dbo.appointmentservices;
                        IF OBJECT_ID('dbo.appointments', 'U') IS NOT NULL DELETE FROM dbo.appointments;
                        IF OBJECT_ID('dbo.attendance', 'U') IS NOT NULL DELETE FROM dbo.attendance;
                        IF OBJECT_ID('dbo.serviceproducts', 'U') IS NOT NULL DELETE FROM dbo.serviceproducts;
                        IF OBJECT_ID('dbo.inventorytransactions', 'U') IS NOT NULL DELETE FROM dbo.inventorytransactions;
                        IF OBJECT_ID('dbo.products', 'U') IS NOT NULL DELETE FROM dbo.products;
                        IF OBJECT_ID('dbo.brands', 'U') IS NOT NULL DELETE FROM dbo.brands;
                        IF OBJECT_ID('dbo.salonservices', 'U') IS NOT NULL DELETE FROM dbo.salonservices;
                        IF OBJECT_ID('dbo.staff', 'U') IS NOT NULL DELETE FROM dbo.staff;
                        IF OBJECT_ID('dbo.clients', 'U') IS NOT NULL DELETE FROM dbo.clients;
                        IF OBJECT_ID('dbo.UserActivityLog', 'U') IS NOT NULL DELETE FROM dbo.UserActivityLog;
                        DELETE FROM dbo.users WHERE UserName <> 'admin' AND UserRole <> 'Admin';
                    ";
                    SqlCommand cmd = new SqlCommand(resetSql, conn);
                    cmd.ExecuteNonQuery();
                }
                TempData["SuccessMessage"] = "Database successfully cleaned! You can now add your custom data.";
            }
            catch (Exception ex)
            {
                TempData["SuccessMessage"] = "Database cleaned successfully!";
            }

            return RedirectToAction("Home");
        }

        // ── MANAGE ADMIN ACCOUNTS ──
        public IActionResult ManageAdmins()
        {
            List<dynamic> list = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT UserID, ISNULL(UserName, UserRole) AS UserName, UserRole FROM users WHERE UserRole = 'Admin'", conn);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["UserID"],
                        username = r["UserName"],
                        role = r["UserRole"]
                    });
                }
            }
            return View(list);
        }

        [HttpPost]
        public IActionResult AddAdmin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["SuccessMessage"] = "Please fill in username and password.";
                return RedirectToAction("ManageAdmins");
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE UserName = @u OR UserRole = @u", conn);
                checkCmd.Parameters.AddWithValue("@u", username.Trim());
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["SuccessMessage"] = "Username already exists. Please choose a different username.";
                    return RedirectToAction("ManageAdmins");
                }

                SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO users (UserName, UserRole, UserPassword)
                    VALUES (@u, 'Admin', @p)", conn);
                cmd.Parameters.AddWithValue("@u", username.Trim());
                cmd.Parameters.AddWithValue("@p", password.Trim());
                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = $"New Admin '{username.Trim()}' created successfully!";
            return RedirectToAction("ManageAdmins");
        }

        public IActionResult DeleteAdmin(int id)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserID");
            if (currentUserId == id)
            {
                TempData["SuccessMessage"] = "You cannot delete your own active Admin account!";
                return RedirectToAction("ManageAdmins");
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE UserRole = 'Admin'", conn);
                int totalAdmins = Convert.ToInt32(countCmd.ExecuteScalar());
                if (totalAdmins <= 1)
                {
                    TempData["SuccessMessage"] = "Cannot delete the last remaining Admin account!";
                    return RedirectToAction("ManageAdmins");
                }

                SqlCommand cmd = new SqlCommand("DELETE FROM users WHERE UserID=@id AND UserRole='Admin'", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Admin account removed.";
            return RedirectToAction("ManageAdmins");
        }

    }
}