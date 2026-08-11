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
    }
}
