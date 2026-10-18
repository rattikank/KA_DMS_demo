
using System;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class MeHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireLogin(context))
                return;

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName,
                           u.Role, u.DepartmentID, d.DepartmentName
                    FROM dbo.Users u
                    LEFT JOIN dbo.Departments d
                        ON u.DepartmentID = d.DepartmentID
                    WHERE u.UserID = @UserID
                      AND u.IsActive = 1;", conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@UserID", AuthHelper.UserId(context));

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            context.Session.Clear();
                            JsonHelper.Error(
                                context, 401,
                                "ไม่พบผู้ใช้หรือบัญชีถูกปิดใช้งาน"
                            );
                            return;
                        }

                        JsonHelper.Write(context, new
                        {
                            success = true,
                            user = new
                            {
                                userId = Convert.ToInt32(reader["UserID"]),
                                username = Convert.ToString(reader["Username"]),
                                fullName = Convert.ToString(reader["FullName"]),
                                role = Convert.ToString(reader["Role"]),
                                departmentId = Convert.ToInt32(
                                    reader["DepartmentID"]),
                                departmentName = reader["DepartmentName"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["DepartmentName"])
                            }
                        });
                    }
                }
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถตรวจสอบผู้ใช้ได้"
                );
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
