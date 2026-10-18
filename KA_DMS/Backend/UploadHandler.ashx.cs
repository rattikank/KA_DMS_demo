
using System;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace KA_DMS
{
    public class UploadHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        private const int MaxFileSize = 10 * 1024 * 1024;

        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireLogin(context))
                return;

            if (!string.Equals(context.Request.HttpMethod, "POST",
                StringComparison.OrdinalIgnoreCase))
            {
                JsonHelper.Error(context, 405, "รองรับเฉพาะ POST");
                return;
            }

            HttpPostedFile file = context.Request.Files["documentFile"];

            if (file == null || file.ContentLength <= 0)
            {
                JsonHelper.Error(context, 400, "กรุณาเลือกไฟล์");
                return;
            }

            if (file.ContentLength > MaxFileSize)
            {
                JsonHelper.Error(
                    context, 400,
                    "ไฟล์มีขนาดเกิน 10 MB"
                );
                return;
            }

            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            // ปรับรายการนี้ให้ตรงกับประเภทไฟล์ที่องค์กรอนุญาต
            string[] allowedExtensions =
            {
                ".pdf", ".doc", ".docx",
                ".xls", ".xlsx", ".csv",
                ".png", ".jpg", ".jpeg", ".txt"
            };

            if (Array.IndexOf(allowedExtensions, extension) < 0)
            {
                JsonHelper.Error(
                    context, 400,
                    "ไม่อนุญาตให้อัปโหลดไฟล์ประเภทนี้"
                );
                return;
            }

            string documentName =
                (context.Request.Form["docTitle"] ?? "").Trim();

            string documentType =
                (context.Request.Form["docType"] ?? "").Trim();

            string machineModel =
                (context.Request.Form["machine"] ?? "").Trim();

            string customerName =
                (context.Request.Form["customer"] ?? "").Trim();

            string description =
                (context.Request.Form["description"] ?? "").Trim();

            string visibility =
                (context.Request.Form["visibility"] ?? "Department").Trim();

            int departmentId;

            if (!int.TryParse(
                context.Request.Form["department"],
                out departmentId))
            {
                JsonHelper.Error(
                    context, 400,
                    "กรุณาเลือกแผนกให้ถูกต้อง"
                );
                return;
            }

            if (documentName.Length == 0 ||
                documentType.Length == 0)
            {
                JsonHelper.Error(
                    context, 400,
                    "กรุณากรอกชื่อเอกสารและประเภทเอกสาร"
                );
                return;
            }

            if (visibility != "Department" && visibility != "All")
            {
                JsonHelper.Error(
                    context, 400,
                    "รูปแบบการเข้าถึงเอกสารไม่ถูกต้อง"
                );
                return;
            }

            int userId = AuthHelper.UserId(context);
            bool isAdmin = AuthHelper.IsAdmin(context);

            // ผู้ใช้ทั่วไปอัปโหลดได้เฉพาะแผนกของตน
            if (!isAdmin &&
                departmentId != AuthHelper.DepartmentId(context))
            {
                JsonHelper.Error(
                    context, 403,
                    "คุณไม่มีสิทธิ์อัปโหลดเอกสารให้แผนกอื่น"
                );
                return;
            }

            // กำหนดสิทธิ์ให้การเผยแพร่ทุกแผนกเป็นของ Admin เท่านั้น
            if (visibility == "All" && !isAdmin)
            {
                JsonHelper.Error(
                    context, 403,
                    "เฉพาะ Admin เท่านั้นที่เผยแพร่เอกสารทุกแผนกได้"
                );
                return;
            }

            string originalFileName =
                Path.GetFileName(file.FileName);

            string storedFileName =
                Guid.NewGuid().ToString("N") + extension;

            string uploadFolder =
                context.Server.MapPath("~/App_Data/Uploads");

            string fullPath =
                Path.Combine(uploadFolder, storedFileName);

            string relativePath =
                "App_Data/Uploads/" + storedFileName;

            bool fileSaved = false;

            try
            {
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                file.SaveAs(fullPath);
                fileSaved = true;

                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO dbo.Documents
                    (
                        DocumentName, DocumentType,
                        MachineModel, CustomerName,
                        Description, Visibility,
                        OriginalFileName, StoredFileName,
                        FilePath, FileSize, ContentType,
                        DepartmentID, UploadedBy
                    )
                    VALUES
                    (
                        @DocumentName, @DocumentType,
                        @MachineModel, @CustomerName,
                        @Description, @Visibility,
                        @OriginalFileName, @StoredFileName,
                        @FilePath, @FileSize, @ContentType,
                        @DepartmentID, @UploadedBy
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@DocumentName", documentName);

                    cmd.Parameters.AddWithValue(
                        "@DocumentType", documentType);

                    cmd.Parameters.AddWithValue(
                        "@MachineModel",
                        string.IsNullOrWhiteSpace(machineModel)
                            ? (object)DBNull.Value : machineModel);

                    cmd.Parameters.AddWithValue(
                        "@CustomerName",
                        string.IsNullOrWhiteSpace(customerName)
                            ? (object)DBNull.Value : customerName);

                    cmd.Parameters.AddWithValue(
                        "@Description",
                        string.IsNullOrWhiteSpace(description)
                            ? (object)DBNull.Value : description);

                    cmd.Parameters.AddWithValue(
                        "@Visibility", visibility);

                    cmd.Parameters.AddWithValue(
                        "@OriginalFileName", originalFileName);

                    cmd.Parameters.AddWithValue(
                        "@StoredFileName", storedFileName);

                    cmd.Parameters.AddWithValue(
                        "@FilePath", relativePath);

                    cmd.Parameters.AddWithValue(
                        "@FileSize", file.ContentLength);

                    cmd.Parameters.AddWithValue(
                        "@ContentType",
                        file.ContentType ?? "application/octet-stream");

                    cmd.Parameters.AddWithValue(
                        "@DepartmentID", departmentId);

                    cmd.Parameters.AddWithValue(
                        "@UploadedBy", userId);

                    conn.Open();

                    int documentId =
                        Convert.ToInt32(cmd.ExecuteScalar());

                    WriteUploadLog(
                        conn, userId, documentId, context);

                    JsonHelper.Write(context, new
                    {
                        success = true,
                        message = "อัปโหลดเอกสารสำเร็จ",
                        documentId = documentId
                    });
                }
            }
            catch (Exception)
            {
                // หากบันทึกข้อมูลไม่สำเร็จ ให้ลบไฟล์ที่บันทึกไว้
                if (fileSaved && File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch
                    {
                        // ควรบันทึกรายละเอียดข้อผิดพลาดในระบบ Log ฝั่ง Server
                    }
                }

                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถบันทึกเอกสารได้"
                );
            }
        }

        private void WriteUploadLog(
            SqlConnection conn,
            int userId,
            int documentId,
            HttpContext context)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                INSERT INTO dbo.AuditLogs
                    (UserID, Action, DocumentID, Details,
                     IPAddress, Result)
                VALUES
                    (@UserID, 'Upload', @DocumentID, @Details,
                     @IPAddress, 'Success');", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@DocumentID", documentId);
                cmd.Parameters.AddWithValue(
                    "@Details", "อัปโหลดเอกสารสำเร็จ");
                cmd.Parameters.AddWithValue(
                    "@IPAddress",
                    context.Request.UserHostAddress ?? "");

                cmd.ExecuteNonQuery();
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
