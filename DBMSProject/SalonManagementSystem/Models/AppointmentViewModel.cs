using Microsoft.AspNetCore.Mvc.Rendering;

namespace SalonManagementSystem.Models
{
    public class AppointmentViewModel
    {
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }

        public DateTime AppDate { get; set; }
        public TimeSpan AppTime { get; set; }

        public int SelectedServiceId { get; set; }
        public List<SelectListItem> Services { get; set; }

        // 🔥 ADD THESE (THIS FIXES YOUR ERROR)
        public int BookedForId { get; set; }
        public int BookedById { get; set; }
        public List<SelectListItem> StaffList { get; set; }

        public decimal TotalAmount { get; set; }
    }
}