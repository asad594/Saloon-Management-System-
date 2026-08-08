namespace SalonManagementSystem.Models
{
    public class StaffAppointmentViewModel
    {
        public int AppId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public DateTime AppDate { get; set; }
        public string AppTime { get; set; } = string.Empty;
        public int AppStatus { get; set; }
        public string StatusName
        {
            get
            {
                return AppStatus switch
                {
                    1 => "Pending",
                    2 => "In Progress",
                    3 => "Completed",
                    4 => "No-show",
                    _ => "Scheduled"
                };
            }
        }
    }
}
