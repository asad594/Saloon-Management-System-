namespace SalonManagementSystem.Models
{
    public class AttendanceViewModel
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; }   // MUST HAVE

        public string CheckIn { get; set; }

        public string CheckOut { get; set; }
    }
}