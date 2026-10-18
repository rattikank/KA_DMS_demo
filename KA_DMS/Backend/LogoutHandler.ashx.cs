
using System;
using System.Web;

namespace KA_DMS
{
    public class LogoutHandler : IHttpHandler,
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

            if (AuthHelper.IsLoggedIn(context))
            {
                // บันทึก Log ก่อนล้าง Session
                try
                {
                    using (var conn = Db.GetConnection())
                    using (var cmd = new System.Data.SqlClient.SqlCommand(@"
                        INSERT INTO dbo.AuditLogs
                            (UserID, Action, Details, IPAddress, Result)
                        VALUES
                            (@UserID, 'Logout', N'ออกจากระบบ',
                             @IPAddress, 'Success');", conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@UserID", AuthHelper.UserId(context));

                        cmd.Parameters.AddWithValue(
                            "@IPAddress",
                            context.Request.UserHostAddress ?? "");

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // การออกจากระบบยังต้องดำเนินต่อ
                }
            }

            context.Session.Clear();
            context.Session.Abandon();

            JsonHelper.Write(context, new
            {
                success = true,
                message = "ออกจากระบบสำเร็จ"
            });
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
