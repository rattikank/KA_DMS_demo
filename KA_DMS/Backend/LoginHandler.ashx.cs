
using System;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class LoginHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!string.Equals(
                context.Request.HttpMethod, "POST",
                StringComparison.OrdinalIgnoreCase))
            {
                JsonHelper.Error(context, 405, "รองรับเฉพาะ POST");
                return;
            }

            string username =
                (context.Request.Form["username"] ?? "").Trim();

            string password =
                context.Request.Form["password"] ?? "";

            if (username.Length == 0 || password.Length == 0)
            {
                JsonHelper.Error(
                    context, 400, "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน"
                );
                return;
            }

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT UserID, Username, PasswordHash,
                           FullName, DepartmentID, Role
                    FROM dbo.Users
                    WHERE Username = @Username
                      AND IsActive = 1;", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            reader.Close();
                            WriteLoginLog(context, null, "Failure");
                            JsonHelper.Error(
                                context, 401,
                                "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง"
                            );
                            return;
                        }

                        int userId = Convert.ToInt32(reader["UserID"]);
                        string storedHash =
                            Convert.ToString(reader["PasswordHash"]);

                        string fullName =
                            Convert.ToString(reader["FullName"]);

                        int departmentId =
                            Convert.ToInt32(reader["DepartmentID"]);

                        string role = Convert.ToString(reader["Role"]);
                        string actualUsername =
                            Convert.ToString(reader["Username"]);

                        reader.Close();

                        if (!PasswordHelper.VerifyPassword(
                            password, storedHash))
                        {
                            WriteLoginLog(context, userId, "Failure");
                            JsonHelper.Error(
                                context, 401,
                                "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง"
                            );
                            return;
                        }

                        // สร้าง Session ใหม่หลังผ่านการยืนยันตัวตน
                        context.Session.Clear();
                        context.Session["UserID"] = userId;
                        context.Session["Username"] = actualUsername;
                        context.Session["FullName"] = fullName;
                        context.Session["DepartmentID"] = departmentId;
                        context.Session["Role"] = role;

                        WriteLoginLog(context, userId, "Success");

                        JsonHelper.Write(context, new
                        {
                            success = true,
                            message = "เข้าสู่ระบบสำเร็จ",
                            user = new
                            {
                                userId = userId,
                                username = actualUsername,
                                fullName = fullName,
                                departmentId = departmentId,
                                role = role
                            }
                        });
                    }
                }
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500,
                    "เกิดข้อผิดพลาดในการเข้าสู่ระบบ"
                );
            }
        }

        private void WriteLoginLog(
            HttpContext context, int? userId, string result)
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO dbo.AuditLogs
                        (UserID, Action, Details, IPAddress, Result)
                    VALUES
                        (@UserID, 'Login', @Details, @IPAddress, @Result);",
                    conn))
                {
                    if (userId.HasValue)
                        cmd.Parameters.AddWithValue("@UserID", userId.Value);
                    else
                        cmd.Parameters.AddWithValue("@UserID", DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Details",
                        result == "Success"
                            ? "เข้าสู่ระบบสำเร็จ"
                            : "เข้าสู่ระบบไม่สำเร็จ"
                    );

                    cmd.Parameters.AddWithValue(
                        "@IPAddress",
                        context.Request.UserHostAddress ?? ""
                    );

                    cmd.Parameters.AddWithValue("@Result", result);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ไม่ให้ปัญหาบันทึก Audit Log เปิดเผยรายละเอียดฐานข้อมูล
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
