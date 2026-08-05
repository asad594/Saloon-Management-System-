namespace SalonManagementSystem.Models
{
    public class BillViewModel
    {
        public string ClientName { get; set; }
        public string Phone { get; set; }
        public string ServiceName { get; set; }
        public string StaffName { get; set; }

        public DateTime AppDate { get; set; }
        public TimeSpan AppTime { get; set; }

        public decimal Total { get; set; }
    }
}