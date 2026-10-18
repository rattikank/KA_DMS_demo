
using System;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class TestDb : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    conn.Open();

                    context.Response.Write(
                        "เชื่อมต่อฐานข้อมูล KAScienceDMS สำเร็จ!"
                    );
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                context.Response.Write(
                    "เชื่อมต่อฐานข้อมูลไม่สำเร็จ " +
                    "กรุณาตรวจสอบ Web.config และ SQL Server"
                );
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
