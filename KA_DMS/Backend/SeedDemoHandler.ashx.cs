using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace KA_DMS
{
    public class SeedDemoHandler : IHttpHandler,
        IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireLogin(context))
                return;

            if (!AuthHelper.IsAdmin(context))
            {
                JsonHelper.Error(context, 403,
                    "เฉพาะ Admin เท่านั้น");
                return;
            }

            if (context.Request.HttpMethod != "POST")
            {
                JsonHelper.Error(context, 405,
                    "รองรับเฉพาะ POST");
                return;
            }

            List<string> createdFiles = new List<string>();

            string folder = context.Server.MapPath(
                "~/App_Data/Uploads");

            int adminId = AuthHelper.UserId(context);

            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                using (SqlConnection conn = Db.GetConnection())
                {
                    conn.Open();

                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // ป้องกันการเพิ่มเอกสาร Demo ซ้ำ
                            using (SqlCommand check =
                                new SqlCommand(@"
                                    SELECT COUNT(*)
                                    FROM dbo.Documents
                                    WHERE DocumentName
                                        LIKE N'[[]DEMO[]] %'
                                      AND IsDeleted = 0;",
                                    conn, tx))
                            {
                                if (Convert.ToInt32(
                                    check.ExecuteScalar()) > 0)
                                {
                                    tx.Rollback();

                                    JsonHelper.Error(context, 409,
                                        "มีเอกสาร Demo อยู่แล้ว");
                                    return;
                                }
                            }

                            int salesId = GetOrCreateUser(
                                conn, tx,
                                "demo_sales",
                                "ผู้ใช้ทดสอบฝ่ายขาย",
                                "sales.demo@example.test",
                                1, "Sales");

                            int purchasingId = GetOrCreateUser(
                                conn, tx,
                                "demo_purchasing",
                                "ผู้ใช้ทดสอบจัดซื้อ",
                                "purchasing.demo@example.test",
                                2, "Purchasing");

                            string[] titles =
                            {
                                "คู่มือการใช้งานเครื่องจักร",
                                "Datasheet รุ่น A100",
                                "ใบเสนอราคาตัวอย่าง",
                                "รายงานการซ่อมบำรุง",
                                "คู่มือการติดตั้ง",
                                "รายการอะไหล่",
                                "รายงานตรวจสอบคุณภาพ",
                                "เอกสารข้อมูลลูกค้า Demo",
                                "คู่มือการแก้ไขปัญหา",
                                "รายการสั่งซื้อ",
                                "รายงานบำรุงรักษาประจำเดือน",
                                "เอกสารสเปกอุปกรณ์",
                                "ใบตรวจรับสินค้า",
                                "รายงานการทดสอบระบบ",
                                "คู่มือความปลอดภัย",
                                "รายการวัสดุสิ้นเปลือง",
                                "รายงานบริการหลังการขาย",
                                "แบบฟอร์มตรวจสอบเครื่องจักร",
                                "เอกสารการตั้งค่าเครือข่าย",
                                "รายงานสรุปงานวิศวกรรม"
                            };

                            string[] types =
                            {
                                "คู่มือ", "Datasheet", "ใบเสนอราคา",
                                "รายงานซ่อม", "คู่มือ", "รายการอะไหล่",
                                "รายงาน", "เอกสารลูกค้า", "คู่มือ",
                                "ใบสั่งซื้อ", "รายงาน", "Datasheet",
                                "ใบตรวจรับ", "รายงาน", "คู่มือ",
                                "รายการวัสดุ", "รายงาน", "แบบฟอร์ม",
                                "เอกสาร IT", "รายงาน"
                            };

                            List<int> documentIds =
                                new List<int>();

                            for (int i = 0; i < 20; i++)
                            {
                                string originalName = String.Format(
                                    "DEMO_Document_{0:00}.txt", i + 1);

                                string storedName =
                                    Guid.NewGuid().ToString("N") + ".txt";

                                string fullPath = Path.Combine(
                                    folder, storedName);

                                string relativePath =
                                    "App_Data/Uploads/" + storedName;

                                string content =
                                    "KA Science DMS - DEMO DATA\r\n" +
                                    "เอกสารตัวอย่างสำหรับทดสอบระบบเท่านั้น\r\n" +
                                    "ชื่อเอกสาร: " + titles[i] + "\r\n" +
                                    "หมายเลขตัวอย่าง: " + (i + 1) + "\r\n" +
                                    "ห้ามใช้เป็นเอกสารธุรกิจจริง\r\n";

                                byte[] bytes =
                                    new UTF8Encoding(true).GetBytes(content);

                                File.WriteAllBytes(fullPath, bytes);
                                createdFiles.Add(fullPath);

                                int departmentId = (i % 5) + 1;

                                using (SqlCommand cmd =
                                    new SqlCommand(@"
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
                                            @Name, @Type,
                                            @Machine, @Customer,
                                            @Description, 'Department',
                                            @Original, @Stored,
                                            @Path, @Size, 'text/plain',
                                            @Department, @Uploader
                                        );

                                        SELECT CAST(
                                            SCOPE_IDENTITY() AS INT);",
                                        conn, tx))
                                {
                                    cmd.Parameters.AddWithValue(
                                        "@Name", "[DEMO] " + titles[i]);

                                    cmd.Parameters.AddWithValue(
                                        "@Type", types[i]);

                                    cmd.Parameters.AddWithValue(
                                        "@Machine", "DEMO-MODEL-" +
                                            (i + 1).ToString("00"));

                                    cmd.Parameters.AddWithValue(
                                        "@Customer", "Demo Customer");

                                    cmd.Parameters.AddWithValue(
                                        "@Description",
                                        "ข้อมูลสมมติสำหรับทดสอบเท่านั้น");

                                    cmd.Parameters.AddWithValue(
                                        "@Original", originalName);

                                    cmd.Parameters.AddWithValue(
                                        "@Stored", storedName);

                                    cmd.Parameters.AddWithValue(
                                        "@Path", relativePath);

                                    cmd.Parameters.AddWithValue(
                                        "@Size", (long)bytes.Length);

                                    cmd.Parameters.AddWithValue(
                                        "@Department", departmentId);

                                    cmd.Parameters.AddWithValue(
                                        "@Uploader", adminId);

                                    int id = Convert.ToInt32(
                                        cmd.ExecuteScalar());

                                    documentIds.Add(id);
                                }
                            }

                            // ประวัติจำลอง 20 เหตุการณ์
                            for (int i = 0; i < 20; i++)
                            {
                                string action;
                                object docId;
                                int userId;

                                if (i % 3 == 0)
                                {
                                    action = "Login";
                                    docId = DBNull.Value;
                                    userId = (i % 2 == 0)
                                        ? salesId : purchasingId;
                                }
                                else if (i % 3 == 1)
                                {
                                    action = "Upload";
                                    docId = documentIds[i];
                                    userId = adminId;
                                }
                                else
                                {
                                    action = "Download";
                                    docId = documentIds[i];
                                    userId = (i % 2 == 0)
                                        ? salesId : purchasingId;
                                }

                                using (SqlCommand log =
                                    new SqlCommand(@"
                                        INSERT INTO dbo.AuditLogs
                                        (
                                            UserID, Action, DocumentID,
                                            Details, IPAddress, Result
                                        )
                                        VALUES
                                        (
                                            @UserID, @Action, @DocumentID,
                                            @Details, '127.0.0.1', 'Success'
                                        );", conn, tx))
                                {
                                    log.Parameters.AddWithValue(
                                        "@UserID", userId);

                                    log.Parameters.AddWithValue(
                                        "@Action", action);

                                    log.Parameters.AddWithValue(
                                        "@DocumentID", docId);

                                    log.Parameters.AddWithValue(
                                        "@Details",
                                        "[DEMO] เหตุการณ์จำลองสำหรับทดสอบ");

                                    log.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            try { tx.Rollback(); } catch { }

                            foreach (string path in createdFiles)
                            {
                                try
                                {
                                    if (File.Exists(path))
                                        File.Delete(path);
                                }
                                catch { }
                            }

                            throw;
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    message = "สร้างข้อมูล Demo สำเร็จ",
                    documents = 20,
                    users = 2,
                    auditLogs = 20,
                    demoPassword = "Demo@12345"
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(context, 500,
                    "สร้างข้อมูล Demo ไม่สำเร็จ กรุณาตรวจสอบ Server Log");
            }
        }

        private int GetOrCreateUser(
            SqlConnection conn,
            SqlTransaction tx,
            string username,
            string fullName,
            string email,
            int departmentId,
            string role)
        {
            using (SqlCommand find = new SqlCommand(@"
                SELECT UserID
                FROM dbo.Users
                WHERE Username = @Username;", conn, tx))
            {
                find.Parameters.AddWithValue(
                    "@Username", username);

                object found = find.ExecuteScalar();

                if (found != null)
                    return Convert.ToInt32(found);
            }

            string hash =
                PasswordHelper.HashPassword("Demo@12345");

            using (SqlCommand insert = new SqlCommand(@"
                INSERT INTO dbo.Users
                (
                    Username, PasswordHash, FullName,
                    Email, DepartmentID, Role, IsActive
                )
                VALUES
                (
                    @Username, @Hash, @FullName,
                    @Email, @DepartmentID, @Role, 1
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                conn, tx))
            {
                insert.Parameters.AddWithValue(
                    "@Username", username);

                insert.Parameters.AddWithValue(
                    "@Hash", hash);

                insert.Parameters.AddWithValue(
                    "@FullName", fullName);

                insert.Parameters.AddWithValue(
                    "@Email", email);

                insert.Parameters.AddWithValue(
                    "@DepartmentID", departmentId);

                insert.Parameters.AddWithValue(
                    "@Role", role);

                return Convert.ToInt32(insert.ExecuteScalar());
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}