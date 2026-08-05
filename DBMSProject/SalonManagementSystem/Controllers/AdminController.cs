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



        public IActionResult Home()
        {
            DashboardModel model = new DashboardModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                model.TodaySales = Convert.ToDecimal(new SqlCommand(
                    "SELECT ISNULL(SUM(TotalAmount),0) FROM bills WHERE CAST(BillDate AS DATE)=CAST(GETDATE() AS DATE)", conn).ExecuteScalar());

                model.TodayAppointments = (int)new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE CAST(AppDate AS DATE)=CAST(GETDATE() AS DATE)", conn).ExecuteScalar();

                model.StaffAvailable = (int)new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus=1", conn).ExecuteScalar();
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
        public IActionResult AddStaff(string name, string phone, string email, string address, decimal salary, string speciality)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                @"INSERT INTO staff 
        (UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, StaffSalary, StaffSpecialilty, JoiningDate, StaffStatus)
        VALUES ((SELECT TOP 1 UserID FROM users WHERE UserRole = 'Staff'), @n, @p, @e, @a, @s, @sp, GETDATE(), 1)", conn);

                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", phone);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@a", address);
                cmd.Parameters.AddWithValue("@s", salary);
                cmd.Parameters.AddWithValue("@sp", speciality);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Home");
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

                SqlCommand cmd = new SqlCommand(@"
                    SELECT LogId, UserId, UserRole, ActionType, LogMessage, LogTimestamp 
                    FROM UserActivityLog 
                    ORDER BY LogTimestamp DESC", conn);
                
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    logs.Add(new
                    {
                        id = r["LogId"],
                        userId = r["UserId"],
                        role = r["UserRole"],
                        action = r["ActionType"],
                        message = r["LogMessage"],
                        time = r["LogTimestamp"]
                    });
                }
            }

            return View(logs);
        }

    }
}