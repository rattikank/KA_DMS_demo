
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

namespace KA_DMS
{
    public class DepartmentsHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            // ตรวจสอบการเข้าสู่ระบบ
            if (!AuthHelper.RequireLogin(context))
            {
                return;
            }

            // รองรับเฉพาะ GET
            if (context.Request.HttpMethod != "GET")
            {
                JsonHelper.Error(context, 405, "รองรับเฉพาะ GET");
                return;
            }

            try
            {
                var departments = new List<object>();

                using (SqlConnection conn = Db.GetConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT DepartmentID, DepartmentName
                        FROM dbo.Departments
                        ORDER BY DepartmentName;", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                departments.Add(new
                                {
                                    departmentId = Convert.ToInt32(
                                        reader["DepartmentID"]),

                                    departmentName = Convert.ToString(
                                        reader["DepartmentName"])
                                });
                            }
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    data = departments
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context,
                    500,
                    "ไม่สามารถโหลดรายการแผนกได้"
                );
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
