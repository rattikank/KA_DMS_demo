
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;

namespace KA_DMS
{
    public class DocumentsListHandler : IHttpHandler,
        System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (!AuthHelper.RequireLogin(context))
                return;

            if (!string.Equals(context.Request.HttpMethod, "GET",
                StringComparison.OrdinalIgnoreCase))
            {
                JsonHelper.Error(context, 405, "รองรับเฉพาะ GET");
                return;
            }

            try
            {
                var documents = new List<object>();

                using (SqlConnection conn = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        d.DocumentID,
                        d.DocumentName,
                        d.DocumentType,
                        d.MachineModel,
                        d.CustomerName,
                        d.Description,
                        d.Visibility,
                        d.OriginalFileName,
                        d.FileSize,
                        d.DepartmentID,
                        dep.DepartmentName,
                        u.FullName AS UploadedByName,
                        d.UploadedAt
                    FROM dbo.Documents d
                    INNER JOIN dbo.Departments dep
                        ON d.DepartmentID = dep.DepartmentID
                    INNER JOIN dbo.Users u
                        ON d.UploadedBy = u.UserID
                    WHERE d.IsDeleted = 0
                      AND (
                          @Role = 'Admin'
                          OR d.DepartmentID = @DepartmentID
                          OR (
                              @Role = 'Purchasing'
                              AND dep.DepartmentName = 'Sales'
                          )
                          OR d.Visibility = 'All'
                      )
                    ORDER BY d.UploadedAt DESC;", conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@Role", AuthHelper.Role(context));

                    cmd.Parameters.AddWithValue(
                        "@DepartmentID",
                        AuthHelper.DepartmentId(context));

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            documents.Add(new
                            {
                                documentId = Convert.ToInt32(
                                    reader["DocumentID"]),
                                documentName = Convert.ToString(
                                    reader["DocumentName"]),
                                documentType = Convert.ToString(
                                    reader["DocumentType"]),
                                machineModel = reader["MachineModel"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["MachineModel"]),
                                customerName = reader["CustomerName"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["CustomerName"]),
                                description = reader["Description"] ==
                                    DBNull.Value ? "" :
                                    Convert.ToString(reader["Description"]),
                                visibility = Convert.ToString(
                                    reader["Visibility"]),
                                originalFileName = Convert.ToString(
                                    reader["OriginalFileName"]),
                                fileSize = Convert.ToInt64(reader["FileSize"]),
                                departmentId = Convert.ToInt32(
                                    reader["DepartmentID"]),
                                departmentName = Convert.ToString(
                                    reader["DepartmentName"]),
                                uploadedBy = Convert.ToString(
                                    reader["UploadedByName"]),
                                uploadedAt = Convert.ToDateTime(
                                    reader["UploadedAt"]).ToString(
                                        "yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }

                JsonHelper.Write(context, new
                {
                    success = true,
                    data = documents
                });
            }
            catch (Exception)
            {
                JsonHelper.Error(
                    context, 500,
                    "ไม่สามารถโหลดรายการเอกสารได้"
                );
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
