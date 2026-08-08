using System;
using System.Collections.Generic;

namespace SalonManagementSystem.Models
{
    public class StaffLeaveItem
    {
        public int LeaveId { get; set; }
        public DateTime LeaveDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime RequestedOn { get; set; }
    }

    public class StaffLeaveViewModel
    {
        public DateTime NewLeaveDate { get; set; } = DateTime.Today.AddDays(1);
        public string NewReason { get; set; } = string.Empty;
        public List<StaffLeaveItem> LeaveHistory { get; set; } = new List<StaffLeaveItem>();
    }
}
