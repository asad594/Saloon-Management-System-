using System;
using System.Collections.Generic;

namespace SalonManagementSystem.Models
{
    public class ClientPastAppointmentItem
    {
        public int AppId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime AppDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string StaffName { get; set; } = string.Empty;
    }

    public class ClientStaffNoteItem
    {
        public int NoteId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class ClientHistoryViewModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public List<ClientPastAppointmentItem> PastAppointments { get; set; } = new List<ClientPastAppointmentItem>();
        public List<ClientStaffNoteItem> StaffNotes { get; set; } = new List<ClientStaffNoteItem>();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ClientDropdown { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    }
}
