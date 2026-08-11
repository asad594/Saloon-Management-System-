namespace SalonManagementSystem.Models
{
    public class SalonService
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public TimeSpan ServiceTime { get; set; } = TimeSpan.FromMinutes(30);
        public int ServiceStatus { get; set; } = 1;

    }
}