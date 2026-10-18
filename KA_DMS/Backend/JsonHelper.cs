
using System;
using System.Web;
using System.Web.Script.Serialization;

namespace KA_DMS
{
    public static class JsonHelper
    {
        public static void Write(
            HttpContext context,
            object data,
            int statusCode = 200)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding =
                System.Text.Encoding.UTF8;

            context.Response.TrySkipIisCustomErrors = true;

            JavaScriptSerializer serializer =
                new JavaScriptSerializer();

            context.Response.Write(serializer.Serialize(data));
        }

        public static void Error(
            HttpContext context,
            int statusCode,
            string message)
        {
            Write(context, new
            {
                success = false,
                message = message
            }, statusCode);
        }
    }
}
