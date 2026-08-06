using System;
using System.Collections.Generic;

namespace SalonManagementSystem.Models
{
    public class ShiftBillItem
    {
        public int BillId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DailyShiftSummary
    {
        public DateTime ShiftDate { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public int BillCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyShiftRevenueViewModel
    {
        public DateTime SelectedDate { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public decimal SelectedShiftRevenue { get; set; }
        public int SelectedShiftBillCount { get; set; }

        public decimal TodayShiftRevenue { get; set; }
        public int TodayShiftBillCount { get; set; }

        public List<ShiftBillItem> ShiftBills { get; set; } = new List<ShiftBillItem>();
        public List<DailyShiftSummary> ShiftHistory { get; set; } = new List<DailyShiftSummary>();
    }
}
