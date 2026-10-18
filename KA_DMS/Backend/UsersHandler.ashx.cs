
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class UsersHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireAdmin(context))
                return;

            if (context.Request.HttpMethod == "GET")
            {
                ListUsers(context);
                return;
            }

            if (context.Request.HttpMethod == "POST")
            {
                CreateUser(context);
                return;
            }

            JsonHelper.Error(context, 405, "Method ไม่รองรับ");
        }

        private void ListUsers(HttpContext context)
        {
            try
            {
                var users = new List<object>();

                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName,
                           u.Email, u.Role, u.IsActive,
                           u.CreatedAt, u.DepartmentID,
                           d.DepartmentName
                    FROM dbo.Users u
                    LEFT JOIN dbo.Departments d
                        ON u.DepartmentID = d.DepartmentID
                    ORDER BY u.CreatedAt DESC;", conn))
                {
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new
                            {
                                userId = Convert.ToInt32(
                                    reader["UserID"]),
                                username = Convert.ToString(
                                    reader["Username"]),
                                fullName = Convert.ToString(
                                    reader["FullName"]),
                                email = reader["Email"] == DBNull.Value
                                    ? "" : Convert.ToString(reader["Email"]),
                                role = Convert.ToString(reader["Role"]),
                                isActive = Convert.ToBoolean(
                                    reader["IsActive"]),
                                departmentId = Convert.ToInt32(
                                    reader["DepartmentID"]),
                                departmentName =
                                    reader["DepartmentName"] == DBNull.Value
                                    ? "" : Convert.ToString(
                                        reader["DepartmentName"]),
                                createdAt = Convert.ToDateTime(
                                    reader["CreatedAt"]).ToString(
                                        "yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    data = users
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500, "โหลดรายชื่อผู้ใช้ไม่สำเร็จ");
            }
        }

        private void CreateUser(HttpContext context)
        {
            string username =
                (context.Request.Form["username"] ?? "").Trim();

            string password =
                context.Request.Form["password"] ?? "";

            string fullName =
                (context.Request.Form["fullName"] ?? "").Trim();

            string email =
                (context.Request.Form["email"] ?? "").Trim();

            string role =
                (context.Request.Form["role"] ?? "").Trim();

            int departmentId;

            if (username.Length == 0 ||
                password.Length < 10 ||
                fullName.Length == 0 ||
                !int.TryParse(
                    context.Request.Form["departmentId"],
                    out departmentId))
            {
                JsonHelper.Error(
                    context, 400,
                    "กรุณากรอกข้อมูลให้ครบ และตั้งรหัสผ่านอย่างน้อย 10 ตัวอักษร");
                return;
            }

            if (username.Length > 50 ||
                fullName.Length > 150 ||
                email.Length > 254)
            {
                JsonHelper.Error(
                    context, 400, "ข้อมูลยาวเกินขนาดที่กำหนด");
                return;
            }

            if (role != "Admin" &&
                role != "Sales" &&
                role != "Purchasing")
            {
                JsonHelper.Error(
                    context, 400, "Role ไม่ถูกต้อง");
                return;
            }

            string passwordHash =
                PasswordHelper.HashPassword(password);

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO dbo.Users
                        (Username, PasswordHash, FullName,
                         Email, DepartmentID, Role, IsActive)
                    VALUES
                        (@Username, @PasswordHash, @FullName,
                         @Email, @DepartmentID, @Role, 1);", conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@Username", username);

                    cmd.Parameters.AddWithValue(
                        "@PasswordHash", passwordHash);

                    cmd.Parameters.AddWithValue(
                        "@FullName", fullName);

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        email.Length == 0
                            ? (object)DBNull.Value : email);

                    cmd.Parameters.AddWithValue(
                        "@DepartmentID", departmentId);

                    cmd.Parameters.AddWithValue(
                        "@Role", role);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    message = "เพิ่มผู้ใช้สำเร็จ"
                });
            }
            catch (SqlException ex)
            {
                // 2601 / 2627 มักหมายถึงข้อมูลซ้ำตาม Unique Constraint
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    JsonHelper.Error(
                        context, 409,
                        "ชื่อผู้ใช้นี้มีอยู่แล้ว");
                    return;
                }

                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถเพิ่มผู้ใช้ได้");
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถเพิ่มผู้ใช้ได้");
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
