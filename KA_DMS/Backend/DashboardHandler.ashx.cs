
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.SessionState;

namespace KA_DMS
{
    public class DashboardHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            if (context.Session == null ||
                context.Session["UserID"] == null)
            {
                JsonHelper.Error(context, 401, "กรุณาเข้าสู่ระบบก่อน");
                return;
            }

            int userId = Convert.ToInt32(context.Session["UserID"]);
            int departmentId = Convert.ToInt32(context.Session["DepartmentID"]);
            string role = Convert.ToString(context.Session["Role"]);

            bool isAdmin = string.Equals(
                role, "Admin", StringComparison.OrdinalIgnoreCase);

            try
            {
                int documentCount;
                int monthlyDocumentCount;
                int activeUserCount;
                int todayActivityCount;

                var recentDocuments = new List<object>();
                var recentActivities = new List<object>();

                using (SqlConnection conn = Db.GetConnection())
                {
                    conn.Open();

                    // 1. จำนวนเอกสารทั้งหมดที่ยังไม่ถูกลบ
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.Documents
                        WHERE IsDeleted = 0
                          AND (@IsAdmin = 1 OR DepartmentID = @DepartmentID);",
                        conn))
                    {
                        cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = isAdmin;
                        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = departmentId;

                        documentCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 2. จำนวนเอกสารที่อัปโหลดในเดือนนี้
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.Documents
                        WHERE IsDeleted = 0
                          AND (@IsAdmin = 1 OR DepartmentID = @DepartmentID)
                          AND UploadedAt >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                          AND UploadedAt < DATEADD(
                              MONTH, 1,
                              DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                          );",
                        conn))
                    {
                        cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = isAdmin;
                        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = departmentId;

                        monthlyDocumentCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 3. จำนวนผู้ใช้งานที่เปิดใช้งาน
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.Users
                        WHERE IsActive = 1;",
                        conn))
                    {
                        activeUserCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 4. จำนวนกิจกรรมวันนี้
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.AuditLogs
                        WHERE CreatedAt >= CAST(GETDATE() AS date)
                          AND CreatedAt < DATEADD(DAY, 1, CAST(GETDATE() AS date))
                          AND (@IsAdmin = 1 OR UserID = @UserID);",
                        conn))
                    {
                        cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = isAdmin;
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

                        todayActivityCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 5. เอกสารล่าสุด 5 รายการ พร้อมชื่อแผนก
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 5
                            D.DocumentID,
                            D.DocumentName,
                            D.DocumentType,
                            D.MachineModel,
                            D.CustomerName,
                            D.UploadedAt,
                            DP.DepartmentName
                        FROM dbo.Documents AS D
                        LEFT JOIN dbo.Departments AS DP
                            ON D.DepartmentID = DP.DepartmentID
                        WHERE D.IsDeleted = 0
                          AND (@IsAdmin = 1 OR D.DepartmentID = @DepartmentID)
                        ORDER BY D.UploadedAt DESC, D.DocumentID DESC;",
                        conn))
                    {
                        cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = isAdmin;
                        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = departmentId;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recentDocuments.Add(new
                                {
                                    documentId = Convert.ToInt32(reader["DocumentID"]),
                                    documentName = Convert.ToString(reader["DocumentName"]),
                                    documentType = Convert.ToString(reader["DocumentType"]),
                                    machineModel = reader["MachineModel"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["MachineModel"]),
                                    customerName = reader["CustomerName"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["CustomerName"]),
                                    departmentName = reader["DepartmentName"] == DBNull.Value
                                        ? "-"
                                        : Convert.ToString(reader["DepartmentName"]),
                                    uploadedAt = Convert.ToDateTime(reader["UploadedAt"])
                                        .ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }

                    // 6. กิจกรรมล่าสุด 5 รายการ
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 5
                            A.AuditID,
                            A.Action,
                            A.Details,
                            A.Result,
                            A.CreatedAt,
                            U.Username,
                            U.FullName
                        FROM dbo.AuditLogs AS A
                        LEFT JOIN dbo.Users AS U
                            ON A.UserID = U.UserID
                        WHERE (@IsAdmin = 1 OR A.UserID = @UserID)
                        ORDER BY A.CreatedAt DESC, A.AuditID DESC;",
                        conn))
                    {
                        cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = isAdmin;
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recentActivities.Add(new
                                {
                                    auditId = Convert.ToInt64(reader["AuditID"]),
                                    action = Convert.ToString(reader["Action"]),
                                    details = reader["Details"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["Details"]),
                                    result = reader["Result"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["Result"]),
                                    username = reader["Username"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["Username"]),
                                    fullName = reader["FullName"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(reader["FullName"]),
                                    createdAt = Convert.ToDateTime(reader["CreatedAt"])
                                        .ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    summary = new
                    {
                        documentCount = documentCount,
                        monthlyDocumentCount = monthlyDocumentCount,
                        activeUserCount = activeUserCount,
                        todayActivityCount = todayActivityCount
                    },
                    recentDocuments = recentDocuments,
                    recentActivities = recentActivities
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500, "ไม่สามารถโหลดข้อมูล Dashboard ได้");
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
