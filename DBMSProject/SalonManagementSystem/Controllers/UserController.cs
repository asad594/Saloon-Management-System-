using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;

namespace SalonManagementSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly string _connection;

        public UserController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB") ?? string.Empty;
        }

        protected bool EnsureUserAuthorized(out int userId, out string userName)
        {
            userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            userName = HttpContext.Session.GetString("UserName") ?? string.Empty;
            string role = HttpContext.Session.GetString("Role") ?? string.Empty;

            if (userId <= 0 || !role.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        protected int GetLoggedInClientId(SqlConnection conn, int userId, string userName)
        {
            // 1. Try finding client by UsId link
            SqlCommand cmdUsId = new SqlCommand("SELECT TOP 1 ClientId FROM clients WHERE UsId = @uid", conn);
            cmdUsId.Parameters.AddWithValue("@uid", userId);
            object? resUsId = cmdUsId.ExecuteScalar();

            if (resUsId != null && resUsId != DBNull.Value)
            {
                return Convert.ToInt32(resUsId);
            }

            // 2. Try finding client by ClientName matching UserName
            SqlCommand cmdName = new SqlCommand("SELECT TOP 1 ClientId FROM clients WHERE ClientName = @name", conn);
            cmdName.Parameters.AddWithValue("@name", userName);
            object? resName = cmdName.ExecuteScalar();

            if (resName != null && resName != DBNull.Value)
            {
                int cId = Convert.ToInt32(resName);
                // Link UsId for future fast lookups
                SqlCommand updateLink = new SqlCommand("UPDATE clients SET UsId = @uid WHERE ClientId = @cid", conn);
                updateLink.Parameters.AddWithValue("@uid", userId);
                updateLink.Parameters.AddWithValue("@cid", cId);
                updateLink.ExecuteNonQuery();
                return cId;
            }

            // 3. Fallback: Auto-create Client entry linked to UserID
            SqlCommand createCmd = new SqlCommand(@"
                INSERT INTO clients (UsId, ClientName, ClientPhone) 
                VALUES (@uid, @name, '03000000000');
                SELECT SCOPE_IDENTITY();", conn);
            createCmd.Parameters.AddWithValue("@uid", userId);
            createCmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(userName) ? "Valued Client" : userName);
            return Convert.ToInt32(createCmd.ExecuteScalar());
        }

        public IActionResult Index()
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            UserDashboardViewModel model = new UserDashboardViewModel
            {
                UserName = userName,
                FullName = userName
            };

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int clientId = GetLoggedInClientId(conn, userId, userName);

                // Fetch Client Full Name
                SqlCommand cNameCmd = new SqlCommand("SELECT ClientName FROM clients WHERE ClientId = @cid", conn);
                cNameCmd.Parameters.AddWithValue("@cid", clientId);
                object? cNameObj = cNameCmd.ExecuteScalar();
                if (cNameObj != null && cNameObj != DBNull.Value)
                {
                    model.FullName = cNameObj.ToString()!;
                }

                // Booking Metrics
                SqlCommand totalCmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CId = @cid", conn);
                totalCmd.Parameters.AddWithValue("@cid", clientId);
                model.TotalBookingsCount = (int)totalCmd.ExecuteScalar();

                SqlCommand compCmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CId = @cid AND AppStatus = 4", conn);
                compCmd.Parameters.AddWithValue("@cid", clientId);
                model.CompletedBookingsCount = (int)compCmd.ExecuteScalar();

                SqlCommand actCmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CId = @cid AND AppStatus IN (1, 3)", conn);
                actCmd.Parameters.AddWithValue("@cid", clientId);
                model.ActiveBookingsCount = (int)actCmd.ExecuteScalar();

                // Fetch All Appointments for History Preview and Upcoming Search
                string query = @"
                    SELECT a.AppId, a.AppDate, a.AppTime, a.AppStatus, 
                           s.StaffId, ISNULL(s.StaffName, 'Assigned Stylist') AS StaffName, ISNULL(s.StaffSpecialilty, 'Specialist') AS StaffSpecialilty,
                           srv.ServiceId, ISNULL(srv.ServiceName, 'Salon Service') AS ServiceName, ISNULL(srv.ServicePrice, 0) AS ServicePrice,
                           act.StatusType
                    FROM appointments a
                    LEFT JOIN staff s ON a.App_Booked_For = s.StaffId
                    LEFT JOIN appointmentservices aps ON a.AppId = aps.ApId
                    LEFT JOIN salonservices srv ON aps.SeId = srv.ServiceId
                    LEFT JOIN activestatus act ON a.AppStatus = act.StatusId
                    WHERE a.CId = @cid
                    ORDER BY a.AppDate DESC, a.AppTime DESC";

                List<UserAppointmentViewModel> allApps = new List<UserAppointmentViewModel>();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", clientId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        DateTime appDate = Convert.ToDateTime(r["AppDate"]);
                        TimeSpan appTime = (TimeSpan)r["AppTime"];
                        int status = Convert.ToInt32(r["AppStatus"]);
                        string statusType = r["StatusType"] != DBNull.Value ? r["StatusType"].ToString()! : "Scheduled";

                        string badgeClass = status switch
                        {
                            3 => "badge-scheduled",
                            4 => "badge-completed",
                            5 => "badge-cancelled",
                            _ => "badge-pending"
                        };

                        allApps.Add(new UserAppointmentViewModel
                        {
                            AppId = Convert.ToInt32(r["AppId"]),
                            StaffId = r["StaffId"] != DBNull.Value ? Convert.ToInt32(r["StaffId"]) : 0,
                            StaffName = r["StaffName"].ToString()!,
                            StaffSpeciality = r["StaffSpecialilty"].ToString()!,
                            ServiceId = r["ServiceId"] != DBNull.Value ? Convert.ToInt32(r["ServiceId"]) : 0,
                            ServiceName = r["ServiceName"].ToString()!,
                            ServicePrice = Convert.ToDecimal(r["ServicePrice"]),
                            AppDate = appDate,
                            AppTime = appTime,
                            DisplayTime = DateTime.Today.Add(appTime).ToString("hh:mm tt"),
                            AppStatus = status,
                            StatusName = statusType,
                            StatusBadgeClass = badgeClass
                        });
                    }
                }

                model.RecentAppointments = allApps.Take(5).ToList();

                // Find next upcoming appointment
                DateTime now = DateTime.Now;
                model.UpcomingAppointment = allApps
                    .Where(x => (x.AppStatus == 1 || x.AppStatus == 3) && (x.AppDate.Date + x.AppTime) >= now)
                    .OrderBy(x => x.AppDate.Date + x.AppTime)
                    .FirstOrDefault();

                if (model.UpcomingAppointment != null)
                {
                    DateTime appDateTime = model.UpcomingAppointment.AppDate.Date + model.UpcomingAppointment.AppTime;
                    if ((appDateTime - now).TotalHours <= 24 && (appDateTime - now).TotalHours >= 0)
                    {
                        model.HasUpcomingReminder = true;
                        ViewBag.HasUpcomingReminder = true;
                    }
                }

                // Fetch Recommended Services
                SqlCommand rServiceCmd = new SqlCommand("SELECT TOP 4 ServiceId, ServiceName, ServicePrice, ServiceTime, ServiceStatus FROM salonservices WHERE ServiceStatus = 1", conn);
                using var sr = rServiceCmd.ExecuteReader();
                while (sr.Read())
                {
                    model.RecommendedServices.Add(new SalonService
                    {
                        ServiceId = Convert.ToInt32(sr["ServiceId"]),
                        ServiceName = sr["ServiceName"].ToString()!,
                        ServicePrice = Convert.ToDecimal(sr["ServicePrice"]),
                        ServiceTime = sr["ServiceTime"] != DBNull.Value ? (TimeSpan)sr["ServiceTime"] : TimeSpan.FromMinutes(30),
                        ServiceStatus = Convert.ToInt32(sr["ServiceStatus"])
                    });
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            UserProfileViewModel model = new UserProfileViewModel
            {
                UserId = userId,
                UserName = userName
            };

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int clientId = GetLoggedInClientId(conn, userId, userName);
                model.ClientId = clientId;

                SqlCommand cmd = new SqlCommand("SELECT ClientName, ClientPhone FROM clients WHERE ClientId = @cid", conn);
                cmd.Parameters.AddWithValue("@cid", clientId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    model.FullName = r["ClientName"].ToString()!;
                    model.Phone = r["ClientPhone"].ToString()!;
                }

                model.Email = userName.Contains("@") ? userName : userName + "@salon.com";
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Profile(UserProfileViewModel model)
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ViewBag.Error = "Full Name cannot be empty.";
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int clientId = GetLoggedInClientId(conn, userId, userName);

                SqlCommand cmd = new SqlCommand("UPDATE clients SET ClientName = @name, ClientPhone = @phone WHERE ClientId = @cid", conn);
                cmd.Parameters.AddWithValue("@name", model.FullName.Trim());
                cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(model.Phone) ? "03000000000" : model.Phone.Trim());
                cmd.Parameters.AddWithValue("@cid", clientId);
                cmd.ExecuteNonQuery();

                HttpContext.Session.SetString("UserName", model.FullName.Trim());
            }

            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide valid current and new password details.";
                return RedirectToAction("Profile");
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand checkCmd = new SqlCommand("SELECT UserPassword FROM users WHERE UserID = @uid", conn);
                checkCmd.Parameters.AddWithValue("@uid", userId);
                object? currentPassObj = checkCmd.ExecuteScalar();

                string currentPassInDb = currentPassObj != null && currentPassObj != DBNull.Value ? currentPassObj.ToString()! : string.Empty;

                if (!currentPassInDb.Equals(model.CurrentPassword.Trim()))
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Profile");
                }

                SqlCommand updateCmd = new SqlCommand("UPDATE users SET UserPassword = @newPass WHERE UserID = @uid", conn);
                updateCmd.Parameters.AddWithValue("@newPass", model.NewPassword.Trim());
                updateCmd.Parameters.AddWithValue("@uid", userId);
                updateCmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Services()
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            List<SalonService> services = new List<SalonService>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT ServiceId, ServiceName, ServicePrice, ServiceTime, ServiceStatus 
                    FROM salonservices 
                    WHERE ServiceStatus = 1
                    ORDER BY ServiceName ASC", conn);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    services.Add(new SalonService
                    {
                        ServiceId = Convert.ToInt32(reader["ServiceId"]),
                        ServiceName = reader["ServiceName"].ToString()!,
                        ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
                        ServiceTime = reader["ServiceTime"] != DBNull.Value ? (TimeSpan)reader["ServiceTime"] : TimeSpan.FromMinutes(30),
                        ServiceStatus = Convert.ToInt32(reader["ServiceStatus"])
                    });
                }
            }

            return View(services);
        }

        [HttpGet]
        public IActionResult Staff()
        {
            if (!EnsureUserAuthorized(out int userId, out string userName))
            {
                return RedirectToAction("Index", "Login");
            }

            List<dynamic> staffList = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT StaffId, StaffName, ISNULL(StaffSpecialilty, 'Hair & Beauty Specialist') AS StaffSpecialilty, StaffPhone, StaffEmail, JoiningDate 
                    FROM staff 
                    WHERE StaffStatus = 1
                    ORDER BY StaffName ASC", conn);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    staffList.Add(new
                    {
                        StaffId = Convert.ToInt32(r["StaffId"]),
                        StaffName = r["StaffName"].ToString()!,
                        StaffSpeciality = r["StaffSpecialilty"].ToString()!,
                        StaffPhone = r["StaffPhone"].ToString()!,
                        StaffEmail = r["StaffEmail"].ToString()!,
                        JoiningDate = r["JoiningDate"] != DBNull.Value ? Convert.ToDateTime(r["JoiningDate"]).ToString("MMM yyyy") : "N/A"
                    });
                }
            }

            return View(staffList);
        }






    }
}
