namespace SalonManagementSystem.Models
{
    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Default role for new registrations is strictly User (client)
        /// </summary>
        public string Role { get; set; } = "User";
    }
}
