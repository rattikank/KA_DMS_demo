
using System;
using System.Web;

namespace KA_DMS
{
    public class GenerateHash : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            // จำกัดให้ใช้ได้เฉพาะตอนพัฒนาในเครื่อง
            if (!context.Request.IsLocal)
            {
                context.Response.StatusCode = 403;
                context.Response.Write("Forbidden");
                return;
            }

            string password = "Amm123456";

            context.Response.Write(
                KA_DMS.PasswordHelper.HashPassword(password)
            );
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
