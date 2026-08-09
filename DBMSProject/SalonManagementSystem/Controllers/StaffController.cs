using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SalonManagementSystem.Models;
using System.Data;

namespace SalonManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly string _connection;

        /// <summary>
        /// Staff Controller providing personal dashboard, appointment tracking, earnings breakdown, client history with notes, and leave requests.
        /// </summary>
        public StaffController(IConfiguration config)
        {
            _connection = config.GetConnectionString("SalonDB");
        }

        private void EnsureStaffPortalTables()
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd1 = new SqlCommand(@"
                    IF OBJECT_ID('StaffNotes', 'U') IS NULL
                    BEGIN
                        CREATE TABLE StaffNotes (
                            NoteId INT IDENTITY(1,1) PRIMARY KEY,
                            ClientId INT NOT NULL,
                            StaffId INT NOT NULL,
                            Note NVARCHAR(MAX) NOT NULL,
                            CreatedDate DATETIME DEFAULT GETDATE()
                        );
                    END", conn);
                cmd1.ExecuteNonQuery();

                SqlCommand cmd2 = new SqlCommand(@"
                    IF OBJECT_ID('StaffLeaveRequests', 'U') IS NULL
                    BEGIN
                        CREATE TABLE StaffLeaveRequests (
                            LeaveId INT IDENTITY(1,1) PRIMARY KEY,
                            StaffId INT NOT NULL,
                            LeaveDate DATE NOT NULL,
                            Reason NVARCHAR(500) NOT NULL,
                            Status NVARCHAR(50) DEFAULT 'Pending',
                            RequestedOn DATETIME DEFAULT GETDATE()
                        );
                    END", conn);
                cmd2.ExecuteNonQuery();
            }
        }

        private int GetLoggedInStaffId(SqlConnection conn)
        {
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            string userName = HttpContext.Session.GetString("UserName") ?? string.Empty;

            if (userId > 0)
            {
                SqlCommand cmd = new SqlCommand("SELECT TOP 1 StaffId FROM staff WHERE UsId = @uid", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                object? res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return Convert.ToInt32(res);
                }
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                SqlCommand nameCmd = new SqlCommand(@"
                    SELECT TOP 1 s.StaffId 
                    FROM staff s 
                    INNER JOIN users u ON s.UsId = u.UserID 
                    WHERE u.UserName = @uname OR s.StaffName = @uname", conn);
                nameCmd.Parameters.AddWithValue("@uname", userName.Trim());
                object? nRes = nameCmd.ExecuteScalar();
                if (nRes != null && nRes != DBNull.Value)
                {
                    return Convert.ToInt32(nRes);
                }
            }

            SqlCommand fallbackCmd = new SqlCommand("SELECT TOP 1 StaffId FROM staff WHERE StaffStatus = 1 ORDER BY StaffId ASC", conn);
            object? fb = fallbackCmd.ExecuteScalar();
            return fb != null && fb != DBNull.Value ? Convert.ToInt32(fb) : 0;
        }



        public IActionResult Home()
        {
            EnsureStaffPortalTables();
            DashboardModel model = new DashboardModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int staffId = GetLoggedInStaffId(conn);
                model.StaffId = staffId;

                // Today's Total Appointments (Salon-wide)
                SqlCommand cmd1 = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE AppDate = CAST(GETDATE() AS DATE)", conn);
                model.TodayAppointments = (int)cmd1.ExecuteScalar();

                // Total Appointments (Salon-wide)
                SqlCommand cmd2 = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments", conn);
                model.TotalAppointments = (int)cmd2.ExecuteScalar();

                // Today's Sales (Salon-wide)
                SqlCommand cmd3 = new SqlCommand(
                    "SELECT ISNULL(SUM(TotalAmount),0) FROM bills WHERE CAST(BillDate AS DATE)=CAST(GETDATE() AS DATE)", conn);
                model.TodaySales = Convert.ToDecimal(cmd3.ExecuteScalar());

                SqlCommand cmd4 = new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus = 1", conn);
                model.StaffAvailable = (int)cmd4.ExecuteScalar();

                SqlCommand cmd5 = new SqlCommand(
                    "SELECT COUNT(*) FROM staff WHERE StaffStatus != 1", conn);
                model.StaffUnavailable = (int)cmd5.ExecuteScalar();

                SqlCommand cmd6 = new SqlCommand(
                    "SELECT COUNT(*) FROM salonservices", conn);
                model.TotalServices = (int)cmd6.ExecuteScalar();

                // ── Personal Staff Metrics ──
                SqlCommand cmdOwnToday = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE (App_Booked_For = @staffId OR @staffId = 0) AND AppDate = CAST(GETDATE() AS DATE)", conn);
                cmdOwnToday.Parameters.AddWithValue("@staffId", staffId);
                model.OwnTodayCount = Convert.ToInt32(cmdOwnToday.ExecuteScalar());

                SqlCommand cmdCompleted = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE (App_Booked_For = @staffId OR @staffId = 0) AND AppStatus = 3", conn);
                cmdCompleted.Parameters.AddWithValue("@staffId", staffId);
                model.CompletedCount = Convert.ToInt32(cmdCompleted.ExecuteScalar());

                SqlCommand cmdPending = new SqlCommand(
                    "SELECT COUNT(*) FROM appointments WHERE (App_Booked_For = @staffId OR @staffId = 0) AND (AppStatus = 1 OR AppStatus IS NULL)", conn);
                cmdPending.Parameters.AddWithValue("@staffId", staffId);
                model.PendingCount = Convert.ToInt32(cmdPending.ExecuteScalar());

                SqlCommand cmdOwnEarnings = new SqlCommand(@"
                    SELECT ISNULL(SUM(b.TotalAmount), 0)
                    FROM bills b
                    INNER JOIN appointments a ON b.AppointId = a.AppId
                    WHERE (a.App_Booked_For = @staffId OR @staffId = 0) AND CAST(b.BillDate AS DATE) = CAST(GETDATE() AS DATE)", conn);
                cmdOwnEarnings.Parameters.AddWithValue("@staffId", staffId);
                model.OwnTodayEarnings = Convert.ToDecimal(cmdOwnEarnings.ExecuteScalar());

                // ── Real Today Appointments Timeline Query ──
                var todayTimeline = new List<dynamic>();
                SqlCommand cmdTimeline = new SqlCommand(@"
                    SELECT a.AppId, c.ClientName, a.AppTime, a.AppStatus,
                           ISNULL((SELECT TOP 1 s.ServiceName 
                                   FROM salonservices s 
                                   INNER JOIN appointmentservices aps ON s.ServiceId = aps.SeId 
                                   WHERE aps.ApId = a.AppId), 'Salon Consultation') AS ServiceName
                    FROM appointments a
                    INNER JOIN clients c ON a.CId = c.ClientId
                    WHERE (a.App_Booked_For = @staffId OR @staffId = 0) AND a.AppDate = CAST(GETDATE() AS DATE)
                    ORDER BY a.AppTime ASC", conn);
                cmdTimeline.Parameters.AddWithValue("@staffId", staffId);
                using (var reader = cmdTimeline.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var appTimeSpan = reader["AppTime"] != DBNull.Value ? (TimeSpan)reader["AppTime"] : TimeSpan.Zero;
                        string timeStr = DateTime.Today.Add(appTimeSpan).ToString("hh:mm tt");
                        int status = reader["AppStatus"] != DBNull.Value ? Convert.ToInt32(reader["AppStatus"]) : 1;
                        string statusStr = status == 3 ? "Completed" : (status == 4 ? "Confirmed" : "Scheduled");

                        todayTimeline.Add(new {
                            AppId = Convert.ToInt32(reader["AppId"]),
                            ClientName = reader["ClientName"].ToString() ?? "Client",
                            AppTimeStr = timeStr,
                            ServiceName = reader["ServiceName"].ToString() ?? "Salon Service",
                            Status = statusStr
                        });
                    }
                }
                ViewBag.TodayTimeline = todayTimeline;
            }

            return View(model);
        }

        public IActionResult MyAppointments()
        {
            EnsureStaffPortalTables();
            List<StaffAppointmentViewModel> list = new List<StaffAppointmentViewModel>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int staffId = GetLoggedInStaffId(conn);

                SqlCommand cmd = new SqlCommand(@"
                    SELECT a.AppId, a.CId, c.ClientName, c.ClientPhone, 
                           ISNULL(s.ServiceName, 'Styling Service') AS ServiceName,
                           ISNULL(s.ServicePrice, 0) AS ServicePrice,
                           a.AppDate, a.AppTime, ISNULL(a.AppStatus, 1) AS AppStatus
                    FROM appointments a
                    LEFT JOIN clients c ON a.CId = c.ClientId
                    LEFT JOIN appointmentservices aps ON a.AppId = aps.ApId
                    LEFT JOIN salonservices s ON aps.SeId = s.ServiceId
                    WHERE a.App_Booked_For = @staffId OR @staffId = 0
                    ORDER BY a.AppDate DESC, a.AppId DESC", conn);
                cmd.Parameters.AddWithValue("@staffId", staffId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new StaffAppointmentViewModel
                    {
                        AppId = Convert.ToInt32(reader["AppId"]),
                        ClientId = Convert.ToInt32(reader["CId"]),
                        ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString()! : "VIP Client",
                        ClientPhone = reader["ClientPhone"] != DBNull.Value ? reader["ClientPhone"].ToString()! : "N/A",
                        ServiceName = reader["ServiceName"].ToString()!,
                        ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
                        AppDate = Convert.ToDateTime(reader["AppDate"]),
                        AppTime = reader["AppTime"] != DBNull.Value ? reader["AppTime"].ToString()! : "Scheduled",
                        AppStatus = Convert.ToInt32(reader["AppStatus"])
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult UpdateAppointmentStatus(int AppId, int NewStatus)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE appointments SET AppStatus = @status WHERE AppId = @appId", conn);
                cmd.Parameters.AddWithValue("@status", NewStatus);
                cmd.Parameters.AddWithValue("@appId", AppId);
                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Appointment status updated successfully!";
            return RedirectToAction("MyAppointments");
        }

        public IActionResult MyEarnings()
        {
            EnsureStaffPortalTables();
            var model = new StaffEarningsViewModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int staffId = GetLoggedInStaffId(conn);

                // Today's Earnings
                SqlCommand cmdToday = new SqlCommand(@"
                    SELECT ISNULL(SUM(b.TotalAmount), 0)
                    FROM bills b
                    INNER JOIN appointments a ON b.AppointId = a.AppId
                    WHERE (a.App_Booked_For = @staffId OR @staffId = 0) AND CAST(b.BillDate AS DATE) = CAST(GETDATE() AS DATE)", conn);
                cmdToday.Parameters.AddWithValue("@staffId", staffId);
                model.TodayEarnings = Convert.ToDecimal(cmdToday.ExecuteScalar());

                // Week's Earnings
                SqlCommand cmdWeek = new SqlCommand(@"
                    SELECT ISNULL(SUM(b.TotalAmount), 0)
                    FROM bills b
                    INNER JOIN appointments a ON b.AppointId = a.AppId
                    WHERE (a.App_Booked_For = @staffId OR @staffId = 0) AND b.BillDate >= DATEADD(day, -7, GETDATE())", conn);
                cmdWeek.Parameters.AddWithValue("@staffId", staffId);
                model.WeekEarnings = Convert.ToDecimal(cmdWeek.ExecuteScalar());

                // Month's Earnings
                SqlCommand cmdMonth = new SqlCommand(@"
                    SELECT ISNULL(SUM(b.TotalAmount), 0)
                    FROM bills b
                    INNER JOIN appointments a ON b.AppointId = a.AppId
                    WHERE (a.App_Booked_For = @staffId OR @staffId = 0) AND MONTH(b.BillDate) = MONTH(GETDATE()) AND YEAR(b.BillDate) = YEAR(GETDATE())", conn);
                cmdMonth.Parameters.AddWithValue("@staffId", staffId);
                model.MonthEarnings = Convert.ToDecimal(cmdMonth.ExecuteScalar());

                // Earnings Itemized List
                SqlCommand cmdHistory = new SqlCommand(@"
                    SELECT b.BillId, c.ClientName, ISNULL(s.ServiceName, 'Styling Service') AS ServiceName, b.TotalAmount, b.BillDate
                    FROM bills b
                    INNER JOIN appointments a ON b.AppointId = a.AppId
                    LEFT JOIN clients c ON b.ClId = c.ClientId
                    LEFT JOIN appointmentservices aps ON a.AppId = aps.ApId
                    LEFT JOIN salonservices s ON aps.SeId = s.ServiceId
                    WHERE a.App_Booked_For = @staffId OR @staffId = 0
                    ORDER BY b.BillDate DESC", conn);
                cmdHistory.Parameters.AddWithValue("@staffId", staffId);

                using SqlDataReader reader = cmdHistory.ExecuteReader();
                while (reader.Read())
                {
                    model.EarningsHistory.Add(new StaffEarningsItem
                    {
                        BillId = Convert.ToInt32(reader["BillId"]),
                        ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString()! : "VIP Client",
                        ServiceName = reader["ServiceName"].ToString()!,
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        BillDate = Convert.ToDateTime(reader["BillDate"])
                    });
                }
            }

            return View(model);
        }

        public IActionResult ClientHistory(int clientId = 0)
        {
            EnsureStaffPortalTables();
            var model = new ClientHistoryViewModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmdClients = new SqlCommand("SELECT ClientId, ClientName, ClientPhone FROM clients ORDER BY ClientName ASC", conn);
                using (SqlDataReader rClients = cmdClients.ExecuteReader())
                {
                    while (rClients.Read())
                    {
                        model.ClientDropdown.Add(new SelectListItem
                        {
                            Value = rClients["ClientId"].ToString(),
                            Text = $"{rClients["ClientName"]} ({rClients["ClientPhone"]})"
                        });
                    }
                }

                if (clientId <= 0 && model.ClientDropdown.Count > 0)
                {
                    clientId = Convert.ToInt32(model.ClientDropdown[0].Value);
                }

                model.ClientId = clientId;

                if (clientId > 0)
                {
                    SqlCommand cmdCInfo = new SqlCommand("SELECT TOP 1 ClientName, ClientPhone FROM clients WHERE ClientId = @cid", conn);
                    cmdCInfo.Parameters.AddWithValue("@cid", clientId);
                    using (SqlDataReader rC = cmdCInfo.ExecuteReader())
                    {
                        if (rC.Read())
                        {
                            model.ClientName = rC["ClientName"].ToString()!;
                            model.ClientPhone = rC["ClientPhone"].ToString()!;
                        }
                    }

                    SqlCommand cmdHistory = new SqlCommand(@"
                        SELECT a.AppId, ISNULL(s.ServiceName, 'Salon Treatment') AS ServiceName, a.AppDate, 
                               ISNULL(b.TotalAmount, ISNULL(s.ServicePrice, 0)) AS AmountPaid,
                               ISNULL(st.StaffName, 'Specialist') AS StaffName
                        FROM appointments a
                        LEFT JOIN appointmentservices aps ON a.AppId = aps.ApId
                        LEFT JOIN salonservices s ON aps.SeId = s.ServiceId
                        LEFT JOIN bills b ON a.AppId = b.AppointId
                        LEFT JOIN staff st ON a.App_Booked_For = st.StaffId
                        WHERE a.CId = @cid
                        ORDER BY a.AppDate DESC", conn);
                    cmdHistory.Parameters.AddWithValue("@cid", clientId);

                    using (SqlDataReader rHist = cmdHistory.ExecuteReader())
                    {
                        while (rHist.Read())
                        {
                            model.PastAppointments.Add(new ClientPastAppointmentItem
                            {
                                AppId = Convert.ToInt32(rHist["AppId"]),
                                ServiceName = rHist["ServiceName"].ToString()!,
                                AppDate = Convert.ToDateTime(rHist["AppDate"]),
                                AmountPaid = Convert.ToDecimal(rHist["AmountPaid"]),
                                StaffName = rHist["StaffName"].ToString()!
                            });
                        }
                    }

                    SqlCommand cmdNotes = new SqlCommand(@"
                        SELECT sn.NoteId, sn.Note, sn.CreatedDate, ISNULL(st.StaffName, 'Specialist') AS StaffName
                        FROM StaffNotes sn
                        LEFT JOIN staff st ON sn.StaffId = st.StaffId
                        WHERE sn.ClientId = @cid
                        ORDER BY sn.CreatedDate DESC", conn);
                    cmdNotes.Parameters.AddWithValue("@cid", clientId);

                    using (SqlDataReader rNotes = cmdNotes.ExecuteReader())
                    {
                        while (rNotes.Read())
                        {
                            model.StaffNotes.Add(new ClientStaffNoteItem
                            {
                                NoteId = Convert.ToInt32(rNotes["NoteId"]),
                                Note = rNotes["Note"].ToString()!,
                                CreatedDate = Convert.ToDateTime(rNotes["CreatedDate"]),
                                StaffName = rNotes["StaffName"].ToString()!
                            });
                        }
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult AddClientNote(int ClientId, string Note)
        {
            if (ClientId > 0 && !string.IsNullOrWhiteSpace(Note))
            {
                EnsureStaffPortalTables();
                using (SqlConnection conn = new SqlConnection(_connection))
                {
                    conn.Open();
                    int staffId = GetLoggedInStaffId(conn);

                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO StaffNotes (ClientId, StaffId, Note, CreatedDate)
                        VALUES (@cid, @sid, @note, GETDATE())", conn);
                    cmd.Parameters.AddWithValue("@cid", ClientId);
                    cmd.Parameters.AddWithValue("@sid", staffId);
                    cmd.Parameters.AddWithValue("@note", Note.Trim());
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Specialist note added to client history successfully!";
            }

            return RedirectToAction("ClientHistory", new { clientId = ClientId });
        }

        public IActionResult RequestLeave()
        {
            EnsureStaffPortalTables();
            var model = new StaffLeaveViewModel();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int staffId = GetLoggedInStaffId(conn);

                SqlCommand cmd = new SqlCommand(@"
                    SELECT LeaveId, LeaveDate, Reason, ISNULL(Status, 'Pending') AS Status, RequestedOn
                    FROM StaffLeaveRequests
                    WHERE StaffId = @staffId OR @staffId = 0
                    ORDER BY RequestedOn DESC", conn);
                cmd.Parameters.AddWithValue("@staffId", staffId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    model.LeaveHistory.Add(new StaffLeaveItem
                    {
                        LeaveId = Convert.ToInt32(reader["LeaveId"]),
                        LeaveDate = Convert.ToDateTime(reader["LeaveDate"]),
                        Reason = reader["Reason"].ToString()!,
                        Status = reader["Status"].ToString()!,
                        RequestedOn = Convert.ToDateTime(reader["RequestedOn"])
                    });
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult RequestLeave(StaffLeaveViewModel m)
        {
            if (!string.IsNullOrWhiteSpace(m.NewReason))
            {
                EnsureStaffPortalTables();
                using (SqlConnection conn = new SqlConnection(_connection))
                {
                    conn.Open();
                    int staffId = GetLoggedInStaffId(conn);

                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO StaffLeaveRequests (StaffId, LeaveDate, Reason, Status, RequestedOn)
                        VALUES (@sid, @ldate, @reason, 'Pending', GETDATE())", conn);
                    cmd.Parameters.AddWithValue("@sid", staffId);
                    cmd.Parameters.AddWithValue("@ldate", m.NewLeaveDate);
                    cmd.Parameters.AddWithValue("@reason", m.NewReason.Trim());
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Leave request submitted to Management for review successfully!";
            }

            return RedirectToAction("RequestLeave");
        }




        public IActionResult BookAppointment()
        {
            var model = new AppointmentViewModel();
            model.Services = new List<SelectListItem>();
            model.StaffList = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // ✅ LOAD SERVICES
                SqlCommand cmd = new SqlCommand(
                    "SELECT ServiceId, ServiceName FROM salonservices", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    model.Services.Add(new SelectListItem
                    {
                        Value = reader["ServiceId"].ToString(),
                        Text = reader["ServiceName"].ToString()
                    });
                }
                reader.Close();

                // ✅ LOAD STAFF
                SqlCommand cmd2 = new SqlCommand(
                    "SELECT StaffId, StaffName FROM staff", conn);

                SqlDataReader reader2 = cmd2.ExecuteReader();

                while (reader2.Read())
                {
                    model.StaffList.Add(new SelectListItem
                    {
                        Value = reader2["StaffId"].ToString(),
                        Text = reader2["StaffName"].ToString()
                    });
                }
            }

            return View(model);
        }



        [HttpPost]
        public IActionResult BookAppointment(AppointmentViewModel model)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                // ==============================
                // 1. INSERT CLIENT
                // ==============================
                SqlCommand cmd1 = new SqlCommand(@"
        INSERT INTO clients (ClientName, ClientPhone)
        OUTPUT INSERTED.ClientId
        VALUES (@n,@p)", conn);

                cmd1.Parameters.AddWithValue("@n", model.ClientName);
                cmd1.Parameters.AddWithValue("@p", model.ClientPhone);

                int clientId = (int)cmd1.ExecuteScalar();

                // ==============================
                // 2. GET SERVICE PRICE
                // ==============================
                SqlCommand priceCmd = new SqlCommand(
                    "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                priceCmd.Parameters.AddWithValue("@id", model.SelectedServiceId);

                decimal total = Convert.ToDecimal(priceCmd.ExecuteScalar());

                // ==============================
                // 3. INSERT APPOINTMENT
                // ==============================
                SqlCommand cmd2 = new SqlCommand(@"
INSERT INTO appointments
(CId, AppDate, AppTime, App_Booked_For, App_Booked_By, AppStatus)
OUTPUT INSERTED.AppId
VALUES
(@c,@d,@t,@bf,@bb,3)", conn);

                cmd2.Parameters.AddWithValue("@c", clientId);
                cmd2.Parameters.AddWithValue("@d", model.AppDate.Date);
                cmd2.Parameters.AddWithValue("@t", model.AppTime);
                cmd2.Parameters.AddWithValue("@bf", model.BookedForId);
                cmd2.Parameters.AddWithValue("@bb", model.BookedById);

                int appointmentId = (int)cmd2.ExecuteScalar();

                SqlCommand appServiceCmd = new SqlCommand(
                    "INSERT INTO appointmentservices (ApId, SeId) VALUES (@appId, @serviceId)", conn);
                appServiceCmd.Parameters.AddWithValue("@appId", appointmentId);
                appServiceCmd.Parameters.AddWithValue("@serviceId", model.SelectedServiceId);
                appServiceCmd.ExecuteNonQuery();

                // ==============================
                // 4. GET PAYMENT METHOD
                // ==============================
                SqlCommand payCmd = new SqlCommand(
                    "SELECT TOP 1 methodId FROM paymentmethods", conn);

                int payId = Convert.ToInt32(payCmd.ExecuteScalar());

                // ==============================
                // 5. INSERT BILL
                // ==============================
                SqlCommand cmd3 = new SqlCommand(@"
        INSERT INTO bills
        (AppointId, ClId, BillDate, TotalAmount, PayId)
        VALUES
        (@app, @c, GETDATE(), @t, @p)", conn);

                cmd3.Parameters.AddWithValue("@app", appointmentId);
                cmd3.Parameters.AddWithValue("@c", clientId);
                cmd3.Parameters.AddWithValue("@t", total);
                cmd3.Parameters.AddWithValue("@p", payId);

                cmd3.ExecuteNonQuery();

                // ==============================
                // 6. GET STAFF NAME
                // ==============================
                SqlCommand staffCmd = new SqlCommand(
                    "SELECT StaffName FROM staff WHERE StaffId=@id", conn);

                staffCmd.Parameters.AddWithValue("@id", model.BookedForId);

                string staffName = staffCmd.ExecuteScalar()?.ToString() ?? "N/A";

                // ==============================
                // 7. GET SERVICE NAME
                // ==============================
                SqlCommand serviceCmd = new SqlCommand(
                    "SELECT ServiceName FROM salonservices WHERE ServiceId=@id", conn);

                serviceCmd.Parameters.AddWithValue("@id", model.SelectedServiceId);

                string serviceName = serviceCmd.ExecuteScalar()?.ToString() ?? "N/A";

                // ==============================
                // 8. CREATE BILL MODEL
                // ==============================
                BillViewModel bill = new BillViewModel()
                {
                    ClientName = model.ClientName,
                    Phone = model.ClientPhone,
                    StaffName = staffName,
                    ServiceName = serviceName,
                    AppDate = model.AppDate,
                    AppTime = model.AppTime,
                    Total = total
                };

                // ==============================
                // 9. REDIRECT TO BILL PAGE
                // ==============================
                return View("GenerateBill", bill);
            }
        }



        public IActionResult GenerateBill()
        {
            List<SalonService> services = new List<SalonService>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT ServiceId, ServiceName, ServicePrice FROM salonservices WHERE ServiceStatus = 1", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    services.Add(new SalonService
                    {
                        ServiceId = (int)reader["ServiceId"],
                        ServiceName = reader["ServiceName"].ToString(),
                        ServicePrice = (decimal)reader["ServicePrice"]
                    });
                }
            }

            return View(services);
        }




        [HttpPost]
        public IActionResult SaveBill(int[] selectedServices, int PaymentMethodId)
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                foreach (var id in selectedServices ?? Array.Empty<int>())
                {
                    SqlCommand cmd = new SqlCommand(
                        "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                    cmd.Parameters.AddWithValue("@id", id);

                    total += (decimal)cmd.ExecuteScalar();
                }

                // Insert bill
                SqlCommand billCmd = new SqlCommand(
                    "INSERT INTO bills (ClId, BillDate, TotalAmount, PayId) VALUES (201, GETDATE(), @total, @pay)", conn);

                billCmd.Parameters.AddWithValue("@total", total);
                billCmd.Parameters.AddWithValue("@pay", PaymentMethodId);

                billCmd.ExecuteNonQuery();
            }

            return RedirectToAction("GenerateBill");
        }





        [HttpPost]
        public IActionResult MarkAttendance(int? SelectedStaffId, string Type)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                int staffId = GetLoggedInStaffId(conn);

                if (staffId <= 0 && SelectedStaffId.HasValue && SelectedStaffId.Value > 0)
                {
                    staffId = SelectedStaffId.Value;
                }

                if (staffId <= 0)
                {
                    TempData["ErrorMessage"] = "Staff profile not found for logged-in account!";
                    return RedirectToAction("Attendance");
                }

                if (Type == "CheckIn")
                {
                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM attendance WHERE StaffId = @sid AND CheckOut IS NULL AND CAST(CheckIn AS DATE) = CAST(GETDATE() AS DATE)", conn);
                    checkCmd.Parameters.AddWithValue("@sid", staffId);
                    int activeShiftCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (activeShiftCount > 0)
                    {
                        TempData["ErrorMessage"] = "You already have an active shift checked in today!";
                        return RedirectToAction("Attendance");
                    }

                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO attendance (StaffId, CheckIn) VALUES (@sid, GETDATE())", conn);
                    cmd.Parameters.AddWithValue("@sid", staffId);
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Shift Arrival (Check-In) logged successfully!";
                }
                else if (Type == "CheckOut")
                {
                    SqlCommand cmd = new SqlCommand(
                        @"UPDATE attendance 
                          SET CheckOut = GETDATE()
                          WHERE StaffId = @sid AND CheckOut IS NULL", conn);
                    cmd.Parameters.AddWithValue("@sid", staffId);
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        TempData["SuccessMessage"] = "Shift Departure (Check-Out) logged successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No active check-in shift found to check out.";
                    }
                }
            }

            return RedirectToAction("Attendance");
        }




        [HttpGet]
        public JsonResult GetServicePrice(int serviceId)
        {
            decimal price = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT ServicePrice FROM salonservices WHERE ServiceId = @id", conn);

                cmd.Parameters.AddWithValue("@id", serviceId);

                var result = cmd.ExecuteScalar();

                if (result != null)
                    price = Convert.ToDecimal(result);
            }

            return Json(price);
        }





        [HttpPost]
        public JsonResult GetMultipleServicePrice([FromBody] List<int> serviceIds)
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                foreach (var id in serviceIds)
                {
                    SqlCommand cmd = new SqlCommand(
                        "SELECT ServicePrice FROM salonservices WHERE ServiceId=@id", conn);

                    cmd.Parameters.AddWithValue("@id", id);

                    var price = cmd.ExecuteScalar();
                    if (price != null)
                        total += Convert.ToDecimal(price);
                }
            }

            return Json(total);
        }




        public IActionResult Attendance()
        {
            List<AttendanceViewModel> list = new List<AttendanceViewModel>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                string query = @"
            SELECT s.StaffName, a.CheckIn, a.CheckOut
            FROM attendance a
            JOIN staff s ON a.StaffId = s.StaffId
            WHERE CAST(a.CheckIn AS DATE) = CAST(GETDATE() AS DATE)
        ";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new AttendanceViewModel
                    {
                        StaffName = reader["StaffName"].ToString(),
                        CheckIn = reader["CheckIn"].ToString(),
                        CheckOut = reader["CheckOut"] == DBNull.Value ? null : reader["CheckOut"].ToString()
                    });
                }
            }

            // 🔥 ALSO LOAD DROPDOWN
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT StaffId, StaffName FROM staff", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                var staffList = new List<dynamic>();

                while (reader.Read())
                {
                    staffList.Add(new
                    {
                        Value = reader["StaffId"],
                        Text = reader["StaffName"]
                    });
                }

                ViewBag.StaffList = staffList;
            }

            return View(list);
        }




        public JsonResult GetAttendanceChart()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                CAST(CheckIn AS DATE) AS Date,
                COUNT(*) AS Total
            FROM attendance
            GROUP BY CAST(CheckIn AS DATE)
            ORDER BY Date
        ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                        total = (int)reader["Total"]
                    });
                }
            }

            return Json(data);
        }




        public JsonResult GetTodayAppointmentsChart()
        {
            var data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                CONVERT(VARCHAR, AppTime) AS TimeSlot,
                COUNT(*) AS Total
            FROM appointments
            WHERE CAST(AppDate AS DATE) = CAST(GETDATE() AS DATE)
            GROUP BY AppTime
            ORDER BY AppTime
        ", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        time = reader["TimeSlot"].ToString(),
                        total = (int)reader["Total"]
                    });
                }
            }

            return Json(data);
        }
    }
}

