using System;

namespace SalonManagementSystem.Models
{
    public class StaffAttendanceAdminViewModel
    {
        public int AttendanceId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Speciality { get; set; } = string.Empty;
        public DateTime SelectedDate { get; set; }
        public string FormattedDate { get; set; } = string.Empty;
        public string DayOfWeekName { get; set; } = string.Empty;
        public string CheckInTime { get; set; } = string.Empty;
        public string CheckOutTime { get; set; } = string.Empty;
        public string WorkingDuration { get; set; } = string.Empty;
        public bool IsActiveShift { get; set; }
        public string StatusBadge { get; set; } = string.Empty;
    }
}
