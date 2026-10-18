
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class AuditLogHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireAdmin(context))
                return;

            if (!string.Equals(
                context.Request.HttpMethod, "GET",
                StringComparison.OrdinalIgnoreCase))
            {
                JsonHelper.Error(context, 405, "รองรับเฉพาะ GET");
                return;
            }

            try
            {
                var logs = new List<object>();

                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 500
                        a.AuditID, a.UserID,
                        u.Username, u.FullName,
                        a.Action, a.DocumentID,
                        d.DocumentName,
                        a.Details, a.IPAddress,
                        a.Result, a.CreatedAt
                    FROM dbo.AuditLogs a
                    LEFT JOIN dbo.Users u
                        ON a.UserID = u.UserID
                    LEFT JOIN dbo.Documents d
                        ON a.DocumentID = d.DocumentID
                    ORDER BY a.CreatedAt DESC;", conn))
                {
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new
                            {
                                auditId = Convert.ToInt64(
                                    reader["AuditID"]),
                                username = reader["Username"] ==
                                    DBNull.Value ? "ไม่ทราบผู้ใช้" :
                                    Convert.ToString(reader["Username"]),
                                fullName = reader["FullName"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["FullName"]),
                                action = Convert.ToString(reader["Action"]),
                                documentId = reader["DocumentID"] ==
                                    DBNull.Value ? (int?)null :
                                    Convert.ToInt32(reader["DocumentID"]),
                                documentName = reader["DocumentName"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["DocumentName"]),
                                details = reader["Details"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["Details"]),
                                ipAddress = reader["IPAddress"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["IPAddress"]),
                                result = Convert.ToString(reader["Result"]),
                                createdAt = Convert.ToDateTime(
                                    reader["CreatedAt"]).ToString(
                                        "yyyy-MM-dd HH:mm:ss")
                            });
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    data = logs
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถโหลดประวัติการใช้งานได้");
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
