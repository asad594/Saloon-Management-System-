using System.Collections.Generic;

namespace SalonManagementSystem.Models
{
    public class StaffEarningsItem
    {
        public int BillId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime BillDate { get; set; }
    }

    public class StaffEarningsViewModel
    {
        public decimal TodayEarnings { get; set; }
        public decimal WeekEarnings { get; set; }
        public decimal MonthEarnings { get; set; }
        public decimal CommissionPercentage { get; set; } = 100m;
        public List<StaffEarningsItem> EarningsHistory { get; set; } = new List<StaffEarningsItem>();
    }
}
