using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;

namespace SalonManagementSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly string _connection;

        public ClientController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB") ?? string.Empty;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Client c)
        {
            if (string.IsNullOrWhiteSpace(c.ClientName) || string.IsNullOrWhiteSpace(c.ClientPhone))
                return RedirectToAction("Index");

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO clients (ClientName, ClientPhone) VALUES (@name, @phone)", conn);
                cmd.Parameters.AddWithValue("@name", c.ClientName);
                cmd.Parameters.AddWithValue("@phone", c.ClientPhone);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
