
using System;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace KA_DMS
{
    public class DownloadHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireLogin(context))
                return;

            int documentId;

            if (!int.TryParse(
                context.Request.QueryString["id"],
                out documentId))
            {
                JsonHelper.Error(
                    context, 400, "รหัสเอกสารไม่ถูกต้อง");
                return;
            }

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT d.DocumentID, d.OriginalFileName,
                           d.StoredFileName, d.FilePath,
                           d.ContentType, d.DepartmentID,
                           d.Visibility, dep.DepartmentName
                    FROM dbo.Documents d
                    INNER JOIN dbo.Departments dep
                        ON d.DepartmentID = dep.DepartmentID
                    WHERE d.DocumentID = @DocumentID
                      AND d.IsDeleted = 0
                      AND (
                          @Role = 'Admin'
                          OR d.DepartmentID = @DepartmentID
                          OR (
                              @Role = 'Purchasing'
                              AND dep.DepartmentName = 'Sales'
                          )
                          OR d.Visibility = 'All'
                      );", conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@DocumentID", documentId);

                    cmd.Parameters.AddWithValue(
                        "@Role", AuthHelper.Role(context));

                    cmd.Parameters.AddWithValue(
                        "@DepartmentID",
                        AuthHelper.DepartmentId(context));

                    conn.Open();

                    string originalName;
                    string storedName;
                    string relativePath;
                    string contentType;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            JsonHelper.Error(
                                context, 404,
                                "ไม่พบเอกสารหรือคุณไม่มีสิทธิ์เข้าถึง"
                            );
                            return;
                        }

                        originalName = Convert.ToString(
                            reader["OriginalFileName"]);

                        storedName = Convert.ToString(
                            reader["StoredFileName"]);

                        relativePath = Convert.ToString(
                            reader["FilePath"]);

                        contentType = reader["ContentType"] ==
                            DBNull.Value ? "application/octet-stream" :
                            Convert.ToString(reader["ContentType"]);
                    }

                    // ยึดตำแหน่งโฟลเดอร์ของระบบ ไม่ใช้ Path จาก URL
                    string expectedFolder =
                        Path.GetFullPath(
                            context.Server.MapPath("~/App_Data/Uploads")
                        ) + Path.DirectorySeparatorChar;

                    // ใช้เฉพาะชื่อไฟล์ที่ระบบสร้างขึ้น
                    string safeStoredName =
                        Path.GetFileName(storedName);

                    if (safeStoredName != storedName)
                    {
                        JsonHelper.Error(
                            context, 400, "เส้นทางไฟล์ไม่ถูกต้อง");
                        return;
                    }

                    string fullPath =
                        Path.GetFullPath(
                            Path.Combine(
                                context.Server.MapPath("~/App_Data/Uploads"),
                                safeStoredName
                            )
                        );

                    if (!fullPath.StartsWith(
                        expectedFolder,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        JsonHelper.Error(
                            context, 400, "เส้นทางไฟล์ไม่ถูกต้อง");
                        return;
                    }

                    if (!File.Exists(fullPath))
                    {
                        JsonHelper.Error(
                            context, 404,
                            "ไม่พบไฟล์ในพื้นที่จัดเก็บ");
                        return;
                    }

                    using (SqlCommand log = new SqlCommand(@"
                        INSERT INTO dbo.AuditLogs
                            (UserID, Action, DocumentID, Details,
                             IPAddress, Result)
                        VALUES
                            (@UserID, 'Download', @DocumentID,
                             @Details, @IPAddress, 'Success');", conn))
                    {
                        log.Parameters.AddWithValue(
                            "@UserID", AuthHelper.UserId(context));

                        log.Parameters.AddWithValue(
                            "@DocumentID", documentId);

                        log.Parameters.AddWithValue(
                            "@Details", "ดาวน์โหลดเอกสารสำเร็จ");

                        log.Parameters.AddWithValue(
                            "@IPAddress",
                            context.Request.UserHostAddress ?? "");

                        log.ExecuteNonQuery();
                    }

                    context.Response.Clear();
                    context.Response.ContentType = contentType;
                    context.Response.AddHeader(
                        "X-Content-Type-Options", "nosniff");

                    context.Response.AddHeader(
                        "Content-Disposition",
                        "attachment; filename=\"" +
                        Uri.EscapeDataString(originalName) + "\"");

                    context.Response.TransmitFile(fullPath);
                    context.Response.Flush();
                    context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (Exception)
            {
                if (!context.Response.HeadersWritten)
                {
                    JsonHelper.Error(
                        context, 500,
                        "ไม่สามารถดาวน์โหลดเอกสารได้");
                }
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
