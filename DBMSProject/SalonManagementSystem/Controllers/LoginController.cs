using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;

namespace SalonManagementSystem.Controllers
{
    public class LoginController : Controller
    {
        private readonly string _connection;

        public LoginController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB") ?? string.Empty;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Login l)
        {
            if (string.IsNullOrWhiteSpace(l.UserRole) || string.IsNullOrWhiteSpace(l.UserPassword))
            {
                ViewBag.Error = "Please select role and enter password.";
                return View(l);
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1 UserID, UserRole
                    FROM users
                    WHERE UserRole = @role AND UserPassword = @password", conn);
                cmd.Parameters.AddWithValue("@role", l.UserRole);
                cmd.Parameters.AddWithValue("@password", l.UserPassword);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    ViewBag.Error = "Invalid login!";
                    return View(l);
                }

                int userId = Convert.ToInt32(reader["UserID"]);
                string role = reader["UserRole"].ToString() ?? string.Empty;
                reader.Close();

                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                TryLogActivity(conn, userId, role, "LOGIN");
                TempData["SuccessMessage"] = "Login Successful! Welcome back.";

                return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                    ? RedirectToAction("Home", "Admin")
                    : RedirectToAction("Home", "Staff");
            }
        }

        public IActionResult Logout()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string role = HttpContext.Session.GetString("Role") ?? "Unknown";

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                TryLogActivity(conn, userId, role, "LOGOUT");
            }

            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logout Successful! Have a great day.";
            return RedirectToAction("Index");
        }

        private static void TryLogActivity(SqlConnection conn, int? userId, string role, string action)
        {
            try
            {
                SqlCommand log = new SqlCommand(@"
                    IF OBJECT_ID('dbo.UserActivityLog', 'U') IS NOT NULL
                    INSERT INTO UserActivityLog (UserId, UserRole, ActionType)
                    VALUES (@userId, @role, @action)", conn);
                log.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
                log.Parameters.AddWithValue("@role", role);
                log.Parameters.AddWithValue("@action", action);
                log.ExecuteNonQuery();
            }
            catch
            {
                // Login/logout should still work even if activity logging table is missing.
            }
        }
    }
}
