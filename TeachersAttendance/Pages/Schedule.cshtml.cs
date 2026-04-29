using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace TeachersAttendance.Pages
{
    public class ScheduleModel : PageModel
    {
        private string _connectionString = "server=127.0.0.1;port=1108;database=TeacherAttendanceDB;user=root;password=besselink;";

        // These properties are what the HTML "reads" to display info
        public string TeacherName { get; set; } = "Loading...";
        public string Department { get; set; } = "Faculty";
        public List<ScheduleVM> Schedules { get; set; } = new List<ScheduleVM>();
        public List<AttendanceVM> AttendanceLogs { get; set; } = new List<AttendanceVM>();

        public void OnGet()
        { 
            int teacherId = HttpContext.Session.GetInt32("TeacherID") ?? 1;
            LoadDashboard(teacherId);
        }

        private void LoadDashboard(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Get Teacher Details
                    using (MySqlCommand cmd = new MySqlCommand("SELECT FirstName, LastName, Department FROM Teachers WHERE TeacherID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                TeacherName = $"{r["FirstName"]} {r["LastName"]}";
                                Department = r["Department"].ToString();
                            }
                        }
                    }

                    // 2. Get Schedules Log (Using Subject ID with space)
                    string sQuery = "SELECT sub.SubjectName, sch.DayOfWeek, sch.StartTime, sch.EndTime, sch.Room " +
                                    "FROM Schedules sch JOIN Subjects sub ON sch.SubjectID = sub.SubjectID " +
                                    "WHERE sch.TeacherID = @id";
                    using (MySqlCommand cmd = new MySqlCommand(sQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                Schedules.Add(new ScheduleVM
                                {
                                    Subject = r["SubjectName"].ToString(),
                                    Day = r["DayOfWeek"].ToString(),
                                    Time = $"{r["StartTime"]} - {r["EndTime"]}",
                                    Room = r["Room"].ToString()
                                });
                            }
                        }
                    }

                    // 3. Get Recent Attendance Logs
                    using (MySqlCommand cmd = new MySqlCommand("SELECT AttendanceID,Date, TimeIn, Status FROM ATTENDANCE WHERE TeacherID = @id ORDER BY Date DESC, TimeIn DESC LIMIT 10", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                AttendanceLogs.Add(new AttendanceVM
                                {
                                    AttendanceID = Convert.ToInt32(r["AttendanceID"]),
                                    Date = Convert.ToDateTime(r["Date"]).ToString("MMMM dd, yyyy"),
                                    Time = r["TimeIn"].ToString(),
                                    Status = r["Status"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        
        public IActionResult OnPostTimeIn()
        {
            int teacherId = HttpContext.Session.GetInt32("TeacherID") ?? 1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO ATTENDANCE (TeacherID, Date, TimeIn, Status) VALUES (@tid, CURDATE(), CURTIME(), 'Present')";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teacherId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteLog(int logId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM ATTENDANCE WHERE AttendanceID = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", logId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Delete Error: " + ex.Message);
            }

           
            return RedirectToPage();
        }
    }

    public class ScheduleVM { public string Subject, Day, Time, Room; }
    public class AttendanceVM
    {
        public int AttendanceID { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Status { get; set; }
    }
}