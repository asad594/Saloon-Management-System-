using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;
using System.Data;

namespace SalonManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly string _connection;

        public StaffController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB");
        }



        public IActionResult Home()
        {
            DashboardModel model = new DashboardModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd1 = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE AppDate = CAST(GETDATE() AS DATE)", conn);
                model.TodayAppointments = (int)cmd1.ExecuteScalar();

                SqlCommand cmd2 = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments", conn);
                model.TotalAppointments = (int)cmd2.ExecuteScalar();

                SqlCommand cmd3 = new SqlCommand(
                    "SELECT ISNULL(SUM(TotalAmount),0) FROM bills WHERE CAST(BillDate AS DATE)=CAST(GETDATE() AS DATE)", conn);
                model.TodaySales = Convert.ToDecimal(cmd3.ExecuteScalar());

                SqlCommand cmd4 = new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus = 1", conn);
                model.StaffAvailable = (int)cmd4.ExecuteScalar();

                SqlCommand cmd5 = new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus != 1", conn);
                model.StaffUnavailable = (int)cmd5.ExecuteScalar();

                SqlCommand cmd6 = new SqlCommand(
                    "SELECT COUNT(*) FROM salonservices", conn);
                model.TotalServices = (int)cmd6.ExecuteScalar();
            }

            return View(model);

        }




        public IActionResult BookAppointment()
        {
            var model = new AppointmentViewModel();
            model.Services = new List<SelectListItem>();
            model.StaffList = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // ✅ LOAD SERVICES
                SqlCommand cmd = new SqlCommand(
                    "SELECT ServiceId, ServiceName FROM salonservices", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    model.Services.Add(new SelectListItem
                    {
                        Value = reader["ServiceId"].ToString(),
                        Text = reader["ServiceName"].ToString()
                    });
                }
                reader.Close();

                // ✅ LOAD STAFF
                SqlCommand cmd2 = new SqlCommand(
                    "SELECT StaffId, StaffName FROM staff", conn);

                SqlDataReader reader2 = cmd2.ExecuteReader();

                while (reader2.Read())
                {
                    model.StaffList.Add(new SelectListItem
                    {
                        Value = reader2["StaffId"].ToString(),
                        Text = reader2["StaffName"].ToString()
                    });
                }
            }

            return View(model);
        }



        [HttpPost]
        public IActionResult BookAppointment(AppointmentViewModel model)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // ==============================
                // 1. INSERT CLIENT
                // ==============================
                SqlCommand cmd1 = new SqlCommand(@"
        INSERT INTO clients (ClientName, ClientPhone)
        OUTPUT INSERTED.ClientId
        VALUES (@n,@p)", conn);

                cmd1.Parameters.AddWithValue("@n", model.ClientName);
                cmd1.Parameters.AddWithValue("@p", model.ClientPhone);

                int clientId = (int)cmd1.ExecuteScalar();

                // ==============================
                // 2. GET SERVICE PRICE
                // ==============================
                SqlCommand priceCmd = new SqlCommand(
                    "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                priceCmd.Parameters.AddWithValue("@id", model.SelectedServiceId);

                decimal total = Convert.ToDecimal(priceCmd.ExecuteScalar());

                // ==============================
                // 3. INSERT APPOINTMENT
                // ==============================
                SqlCommand cmd2 = new SqlCommand(@"
INSERT INTO appointments
(CId, AppDate, AppTime, App_Booked_For, App_Booked_By, AppStatus)
OUTPUT INSERTED.AppId
VALUES
(@c,@d,@t,@bf,@bb,3)", conn);

                cmd2.Parameters.AddWithValue("@c", clientId);
                cmd2.Parameters.AddWithValue("@d", model.AppDate.Date);
                cmd2.Parameters.AddWithValue("@t", model.AppTime);
                cmd2.Parameters.AddWithValue("@bf", model.BookedForId);
                cmd2.Parameters.AddWithValue("@bb", model.BookedById);

                int appointmentId = (int)cmd2.ExecuteScalar();

                SqlCommand appServiceCmd = new SqlCommand(
                    "INSERT INTO appointmentservices (ApId, SeId) VALUES (@appId, @serviceId)", conn);
                appServiceCmd.Parameters.AddWithValue("@appId", appointmentId);
                appServiceCmd.Parameters.AddWithValue("@serviceId", model.SelectedServiceId);
                appServiceCmd.ExecuteNonQuery();

                // ==============================
                // 4. GET PAYMENT METHOD
                // ==============================
                SqlCommand payCmd = new SqlCommand(
                    "SELECT TOP 1 methodId FROM paymentmethods", conn);

                int payId = Convert.ToInt32(payCmd.ExecuteScalar());

                // ==============================
                // 5. INSERT BILL
                // ==============================
                SqlCommand cmd3 = new SqlCommand(@"
        INSERT INTO bills
        (AppointId, ClId, BillDate, TotalAmount, PayId)
        VALUES
        (@app, @c, GETDATE(), @t, @p)", conn);

                cmd3.Parameters.AddWithValue("@app", appointmentId);
                cmd3.Parameters.AddWithValue("@c", clientId);
                cmd3.Parameters.AddWithValue("@t", total);
                cmd3.Parameters.AddWithValue("@p", payId);

                cmd3.ExecuteNonQuery();

                // ==============================
                // 6. GET STAFF NAME
                // ==============================
                SqlCommand staffCmd = new SqlCommand(
                    "SELECT StaffName FROM staff WHERE StaffId=@id", conn);

                staffCmd.Parameters.AddWithValue("@id", model.BookedForId);

                string staffName = staffCmd.ExecuteScalar()?.ToString() ?? "N/A";

                // ==============================
                // 7. GET SERVICE NAME
                // ==============================
                SqlCommand serviceCmd = new SqlCommand(
                    "SELECT ServiceName FROM salonservices WHERE ServiceId=@id", conn);

                serviceCmd.Parameters.AddWithValue("@id", model.SelectedServiceId);

                string serviceName = serviceCmd.ExecuteScalar()?.ToString() ?? "N/A";

                // ==============================
                // 8. CREATE BILL MODEL
                // ==============================
                BillViewModel bill = new BillViewModel()
                {
                    ClientName = model.ClientName,
                    Phone = model.ClientPhone,
                    StaffName = staffName,
                    ServiceName = serviceName,
                    AppDate = model.AppDate,
                    AppTime = model.AppTime,
                    Total = total
                };

                // ==============================
                // 9. REDIRECT TO BILL PAGE
                // ==============================
                return View("GenerateBill", bill);
            }
        }



        public IActionResult GenerateBill()
        {
            List<SalonService> services = new List<SalonService>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT ServiceId, ServiceName, ServicePrice FROM salonservices WHERE ServiceStatus = 1", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    services.Add(new SalonService
                    {
                        ServiceId = (int)reader["ServiceId"],
                        ServiceName = reader["ServiceName"].ToString(),
                        ServicePrice = (decimal)reader["ServicePrice"]
                    });
                }
            }

            return View(services);
        }




        [HttpPost]
        public IActionResult SaveBill(int[] selectedServices, int PaymentMethodId)
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                foreach (var id in selectedServices ?? Array.Empty<int>())
                {
                    SqlCommand cmd = new SqlCommand(
                        "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                    cmd.Parameters.AddWithValue("@id", id);

                    total += (decimal)cmd.ExecuteScalar();
                }

                // Insert bill
                SqlCommand billCmd = new SqlCommand(
                    "INSERT INTO bills (ClId, BillDate, TotalAmount, PayId) VALUES (201, GETDATE(), @total, @pay)", conn);

                billCmd.Parameters.AddWithValue("@total", total);
                billCmd.Parameters.AddWithValue("@pay", PaymentMethodId);

                billCmd.ExecuteNonQuery();
            }

            return RedirectToAction("GenerateBill");
        }





        [HttpPost]
        public IActionResult MarkAttendance(int SelectedStaffId, string Type)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                if (Type == "CheckIn")
                {
                    // Insert new record
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO attendance (StaffId, CheckIn) VALUES (@sid, GETDATE())", conn);

                    cmd.Parameters.AddWithValue("@sid", SelectedStaffId);
                    cmd.ExecuteNonQuery();
                }
                else if (Type == "CheckOut")
                {
                    // Update existing record
                    SqlCommand cmd = new SqlCommand(
                        @"UPDATE attendance 
                  SET CheckOut = GETDATE()
                  WHERE StaffId=@sid AND CheckOut IS NULL", conn);

                    cmd.Parameters.AddWithValue("@sid", SelectedStaffId);
                    cmd.ExecuteNonQuery();
                }
            }

            // 🔥 THIS IS IMPORTANT
            return RedirectToAction("Attendance");
        }




        [HttpGet]
        public JsonResult GetServicePrice(int serviceId)
        {
            decimal price = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT ServicePrice FROM salonservices WHERE ServiceId = @id", conn);

                cmd.Parameters.AddWithValue("@id", serviceId);

                var result = cmd.ExecuteScalar();

                if (result != null)
                    price = Convert.ToDecimal(result);
            }

            return Json(price);
        }





        [HttpPost]
        public JsonResult GetMultipleServicePrice([FromBody] List<int> serviceIds)
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                foreach (var id in serviceIds)
                {
                    SqlCommand cmd = new SqlCommand(
                        "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                    cmd.Parameters.AddWithValue("@id", id);

                    var price = cmd.ExecuteScalar();
                    if (price != null)
                        total += Convert.ToDecimal(price);
                }
            }

            return Json(total);
        }




        public IActionResult Attendance()
        {
            List<AttendanceViewModel> list = new List<AttendanceViewModel>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                string query = @"
            SELECT s.StaffName, a.CheckIn, a.CheckOut
            FROM attendance a
            JOIN staff s ON a.StaffId = s.StaffId
            WHERE CAST(a.CheckIn AS DATE) = CAST(GETDATE() AS DATE)
        ";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new AttendanceViewModel
                    {
                        StaffName = reader["StaffName"].ToString(),
                        CheckIn = reader["CheckIn"].ToString(),
                        CheckOut = reader["CheckOut"] == DBNull.Value ? null : reader["CheckOut"].ToString()
                    });
                }
            }

            // 🔥 ALSO LOAD DROPDOWN
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT StaffId, StaffName FROM staff", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                var staffList = new List<dynamic>();

                while (reader.Read())
                {
                    staffList.Add(new
                    {
                        Value = reader["StaffId"],
                        Text = reader["StaffName"]
                    });
                }

                ViewBag.StaffList = staffList;
            }

            return View(list);
        }




        public JsonResult GetAttendanceChart()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                CAST(CheckIn AS DATE) AS Date,
                COUNT(*) AS Total
            FROM attendance
            GROUP BY CAST(CheckIn AS DATE)
            ORDER BY Date
        ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                        total = (int)reader["Total"]
                    });
                }
            }

            return Json(data);
        }




        public JsonResult GetTodayAppointmentsChart()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                CONVERT(VARCHAR, AppTime) AS TimeSlot,
                COUNT(*) AS Total
            FROM appointments
            WHERE CAST(AppDate AS DATE) = CAST(GETDATE() AS DATE)
            GROUP BY AppTime
            ORDER BY AppTime
        ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        time = reader["TimeSlot"].ToString(),
                        total = (int)reader["Total"]
                    });
                }
            }

            return Json(data);
        }
    }
}

