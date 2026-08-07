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

        public IActionResult Index(string? role = null)
        {
            EnsureDatabaseSeeded();
            var model = new Login();
            if (!string.IsNullOrWhiteSpace(role))
            {
                model.UserRole = role;
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(Login l)
        {
            EnsureDatabaseSeeded();

            string inputUser = !string.IsNullOrWhiteSpace(l.UserName) ? l.UserName.Trim() : string.Empty;
            string inputPass = l.UserPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inputUser) || string.IsNullOrWhiteSpace(inputPass))
            {
                ViewBag.Error = "Please enter username and password.";
                return View(l);
            }

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1 UserID, UserRole, ISNULL(UserName, UserRole) AS UserName
                    FROM users
                    WHERE (UserName = @input OR UserRole = @input) AND UserPassword = @password", conn);
                cmd.Parameters.AddWithValue("@input", inputUser);
                cmd.Parameters.AddWithValue("@password", inputPass);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    ViewBag.Error = "Invalid username or password!";
                    return View(l);
                }

                int userId = Convert.ToInt32(reader["UserID"]);
                string role = reader["UserRole"].ToString() ?? "User";
                string userName = reader["UserName"].ToString() ?? inputUser;
                reader.Close();

                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetString("UserName", userName);
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                TryLogActivity(conn, userId, role, "LOGIN");
                TempData["SuccessMessage"] = $"Login Successful! Welcome, {userName} ({role}).";

                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Home", "Admin");
                }
                else if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Home", "Staff");
                }
                else
                {
                    // User / Client Role -> Redirect to main salon home landing page
                    return RedirectToAction("Index", "Home");
                }
            }
        }

        [HttpGet]
        public IActionResult Register(string? role = "User")
        {
            var model = new RegisterViewModel
            {
                Role = string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase) ? "Staff" : "User"
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel m)
        {
            if (string.IsNullOrWhiteSpace(m.UserName) || string.IsNullOrWhiteSpace(m.Password) || string.IsNullOrWhiteSpace(m.FullName))
            {
                ViewBag.Error = "Please fill in all required fields.";
                return View(m);
            }

            EnsureDatabaseSeeded();

            string assignedRole = string.Equals(m.Role, "Staff", StringComparison.OrdinalIgnoreCase) ? "Staff" : "User";

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // Check if username already exists
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE UserName = @uname", conn);
                checkCmd.Parameters.AddWithValue("@uname", m.UserName.Trim());
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    ViewBag.Error = "Username is already taken. Please choose another username.";
                    return View(m);
                }

                // Insert into users table
                SqlCommand userCmd = new SqlCommand(@"
                    INSERT INTO users (UserName, UserRole, UserPassword)
                    OUTPUT INSERTED.UserID
                    VALUES (@uname, @role, @pass)", conn);
                userCmd.Parameters.AddWithValue("@uname", m.UserName.Trim());
                userCmd.Parameters.AddWithValue("@role", assignedRole);
                userCmd.Parameters.AddWithValue("@pass", m.Password.Trim());

                int newUserId = Convert.ToInt32(userCmd.ExecuteScalar());

                if (assignedRole == "Staff")
                {
                    // Insert corresponding staff details
                    SqlCommand staffCmd = new SqlCommand(@"
                        INSERT INTO staff (UsId, StaffName, StaffPhone, StaffEmail, StaffAddress, JoiningDate, StaffSalary, StaffSpecialilty, StaffStatus)
                        VALUES (@usId, @name, @phone, @email, 'Karachi', GETDATE(), 35000, 'Beauty Specialist', 1)", conn);
                    staffCmd.Parameters.AddWithValue("@usId", newUserId);
                    staffCmd.Parameters.AddWithValue("@name", m.FullName.Trim());
                    staffCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(m.Phone) ? "03000000000" : m.Phone.Trim());
                    staffCmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(m.Email) ? m.UserName.Trim() + "@salon.com" : m.Email.Trim());
                    staffCmd.ExecuteNonQuery();
                }
                else
                {
                    // Insert into clients table if available
                    try
                    {
                        SqlCommand clientCmd = new SqlCommand(@"
                            IF OBJECT_ID('dbo.clients', 'U') IS NOT NULL
                            BEGIN
                                INSERT INTO clients (ClientName, ClientPhone)
                                VALUES (@name, @phone)
                            END", conn);
                        clientCmd.Parameters.AddWithValue("@name", m.FullName.Trim());
                        clientCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(m.Phone) ? "03000000000" : m.Phone.Trim());
                        clientCmd.ExecuteNonQuery();
                    }
                    catch { }
                }
            }

            TempData["SuccessMessage"] = $"Registration Successful! You are registered as {assignedRole}. Please log in.";
            return RedirectToAction("Index", new { role = assignedRole });
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

        private void EnsureDatabaseSeeded()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connection);
                conn.Open();

                // Add UserName column if missing
                SqlCommand alterCmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'UserName')
                    BEGIN
                        ALTER TABLE users ADD UserName VARCHAR(50) NULL;
                    END", conn);
                alterCmd.ExecuteNonQuery();

                // Seed Default User (user / user123) with Role = User
                SqlCommand seedUserCmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM users WHERE UserName = 'user')
                    BEGIN
                        INSERT INTO users (UserName, UserRole, UserPassword) VALUES ('user', 'User', 'user123');
                    END
                    ELSE
                    BEGIN
                        UPDATE users SET UserRole = 'User' WHERE UserName = 'user';
                    END", conn);
                seedUserCmd.ExecuteNonQuery();

                // Seed Default Staff (staff / staff123) with Role = Staff
                SqlCommand seedStaffCmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM users WHERE UserName = 'staff')
                    BEGIN
                        INSERT INTO users (UserName, UserRole, UserPassword) VALUES ('staff', 'Staff', 'staff123');
                    END", conn);
                seedStaffCmd.ExecuteNonQuery();

                // Seed Default Admin (admin / admin123) with Role = Admin
                SqlCommand seedAdminCmd = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM users WHERE UserName = 'admin')
                    BEGIN
                        INSERT INTO users (UserName, UserRole, UserPassword) VALUES ('admin', 'Admin', 'admin123');
                    END", conn);
                seedAdminCmd.ExecuteNonQuery();
            }
            catch
            {
                // Ignore seeding errors if DB connection is temporary unavailable
            }
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
                // Ignore activity log errors
            }
        }
    }
}
