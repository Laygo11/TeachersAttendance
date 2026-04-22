using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace TeachersAttendance.Pages
{
    public class ScheduleItem
    {
        public string SubjectName { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string Room { get; set; } = "";
    }

    public class ScheduleModel : PageModel
    {
        private string connStr = "server=localhost;port=1108;database=TeacherAttendanceDB;user=root;password=;";

        public string TeacherName { get; set; } = "";
        public string Department { get; set; } = "";
        public List<ScheduleItem> TodaySchedule { get; set; } = new();
        public Dictionary<string, List<ScheduleItem>> WeeklySchedule { get; set; } = new();

        private static readonly List<string> DayOrder = new()
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToPage("/Login");

            int teacherId = HttpContext.Session.GetInt32("TeacherID")!.Value;
            TeacherName = HttpContext.Session.GetString("TeacherName")!;
            Department = HttpContext.Session.GetString("Department")!;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Today's schedule
                string todayQuery = @"
                    SELECT sub.SubjectName, s.StartTime, s.EndTime, s.Room
                    FROM Schedules s
                    JOIN Subjects sub ON s.SubjectID = sub.SubjectID
                    WHERE s.TeacherID = @id AND s.DayOfWeek = DAYNAME(CURDATE())
                    ORDER BY s.StartTime";
                var todayCmd = new MySqlCommand(todayQuery, conn);
                todayCmd.Parameters.AddWithValue("@id", teacherId);
                using (var r = todayCmd.ExecuteReader())
                {
                    while (r.Read())
                        TodaySchedule.Add(MapItem(r));
                }

                // Full weekly schedule
                string weekQuery = @"
                    SELECT sub.SubjectName, s.StartTime, s.EndTime, s.Room, s.DayOfWeek
                    FROM Schedules s
                    JOIN Subjects sub ON s.SubjectID = sub.SubjectID
                    WHERE s.TeacherID = @id
                    ORDER BY FIELD(s.DayOfWeek,'Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday'), s.StartTime";
                var weekCmd = new MySqlCommand(weekQuery, conn);
                weekCmd.Parameters.AddWithValue("@id", teacherId);
                using (var r = weekCmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string day = r["DayOfWeek"].ToString()!;
                        if (!WeeklySchedule.ContainsKey(day))
                            WeeklySchedule[day] = new List<ScheduleItem>();
                        WeeklySchedule[day].Add(MapItem(r));
                    }
                }
            }

            return Page();
        }

        private ScheduleItem MapItem(MySqlDataReader r)
        {
            return new ScheduleItem
            {
                SubjectName = r["SubjectName"].ToString()!,
                StartTime = TimeSpan.Parse(r["StartTime"].ToString()!).ToString(@"hh\:mm"),
                EndTime = TimeSpan.Parse(r["EndTime"].ToString()!).ToString(@"hh\:mm"),
                Room = r["Room"].ToString()!
            };
        }
    }
}