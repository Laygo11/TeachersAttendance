using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace TeachersAttendance.Pages
{
    public class LoginModel : PageModel
    {
        private string connStr = "server=127.0.0.1;port=1108;database=TeacherAttendanceDB;user=root;password=besselink;";

        [BindProperty]
        public string? EmployeeNumber { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
                return RedirectToPage("/Schedule");

            return Page();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(EmployeeNumber) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please enter your employee number and password.";
                return Page();
            }

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT TeacherID, FirstName, LastName, Department 
                                 FROM Teachers 
                                 WHERE EmployeeNumber = @emp AND Password = @pwd";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", EmployeeNumber.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@pwd", Password);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        ErrorMessage = "Invalid employee number or password.";
                        return Page();
                    }

                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetInt32("TeacherID", Convert.ToInt32(reader["TeacherID"]));
                    HttpContext.Session.SetString("TeacherName", $"{reader["FirstName"]} {reader["LastName"]}");
                    HttpContext.Session.SetString("Department", reader["Department"].ToString()!);
                }
            }


            int teacherId = HttpContext.Session.GetInt32("TeacherID")!.Value;
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(*) FROM Attendance WHERE TeacherID = @id AND Date = CURDATE()";
                var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", teacherId);
                int existing = Convert.ToInt32(checkCmd.ExecuteScalar());

            }

            HttpContext.Session.SetInt32("TeacherID", teacherId);
            return RedirectToPage("/Schedule");
        }
    }
}   