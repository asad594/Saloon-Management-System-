namespace SalonManagementSystem.Models
{
    public class DashboardModel
    {
        public int TodayAppointments { get; set; }
        public int TotalAppointments { get; set; }
        public decimal TodaySales { get; set; }
        public int StaffAvailable { get; set; }
        public int StaffUnavailable { get; set; }
        public int TotalServices { get; set; }

        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        // ── Personal Staff Metrics ──
        public int StaffId { get; set; }
        public int OwnTodayCount { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public decimal OwnTodayEarnings { get; set; }

    }
}