using System.ComponentModel.DataAnnotations;

namespace SalonManagementSystem.Models
{
    public class UserDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalBookingsCount { get; set; }
        public int CompletedBookingsCount { get; set; }
        public int ActiveBookingsCount { get; set; }
        public UserAppointmentViewModel? UpcomingAppointment { get; set; }
        public List<UserAppointmentViewModel> RecentAppointments { get; set; } = new List<UserAppointmentViewModel>();
        public List<SalonService> RecommendedServices { get; set; } = new List<SalonService>();
        public bool HasUpcomingReminder { get; set; }
    }

    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public int ClientId { get; set; }
        
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current Password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters long")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Multi-step booking model for appointment scheduling.
    /// </summary>
    public class BookAppointmentViewModel
    {
        [Required(ErrorMessage = "Please select a service")]
        public int SelectedServiceId { get; set; }

        public int SelectedStaffId { get; set; }
        
        [Required(ErrorMessage = "Please select an appointment date")]
        public DateTime AppDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a time slot")]
        public TimeSpan AppTime { get; set; }

        public List<SalonService> AvailableServices { get; set; } = new List<SalonService>();
        public List<dynamic> AvailableStaff { get; set; } = new List<dynamic>();
        public List<TimeSlotItem> AvailableTimeSlots { get; set; } = new List<TimeSlotItem>();
    }


    public class TimeSlotItem
    {
        public TimeSpan Time { get; set; }
        public string DisplayTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UserAppointmentViewModel
    {
        public int AppId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffSpeciality { get; set; } = string.Empty;
        public DateTime AppDate { get; set; }
        public TimeSpan AppTime { get; set; }
        public string DisplayTime { get; set; } = string.Empty;
        public int AppStatus { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public bool CanCancelOrReschedule { get; set; }
        public bool HasBeenReviewed { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewViewModel
    {
        public int ReviewId { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }

        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5 stars")]
        public int Rating { get; set; } = 5;

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }
    }
}
