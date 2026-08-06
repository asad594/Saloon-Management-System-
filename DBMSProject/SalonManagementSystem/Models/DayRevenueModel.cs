using System;
using System.Collections.Generic;

namespace SalonManagementSystem.Models
{
    public class TransactionBillItem
    {
        public int BillId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ClosedDayLog
    {
        public int ClosingId { get; set; }
        public DateTime ClosingDate { get; set; }
        public string DayName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalBills { get; set; }
        public string ClosedBy { get; set; } = "Admin";
    }

    public class DayRevenueViewModel
    {
        public DateTime? LastClosingDate { get; set; }
        public decimal CurrentDayRevenue { get; set; }
        public int CurrentDayBillsCount { get; set; }

        public decimal TotalAllTimeClosedRevenue { get; set; }
        public int TotalClosedDaysCount { get; set; }

        public List<TransactionBillItem> CurrentDayBills { get; set; } = new List<TransactionBillItem>();
        public List<ClosedDayLog> ClosedDaysHistory { get; set; } = new List<ClosedDayLog>();
    }
}
