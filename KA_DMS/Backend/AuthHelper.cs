
using System;
using System.Web;

namespace KA_DMS
{
    public static class AuthHelper
    {
        public static int UserId(HttpContext context)
        {
            object value = context.Session["UserID"];

            if (value == null)
                return 0;

            return Convert.ToInt32(value);
        }

        public static bool IsLoggedIn(HttpContext context)
        {
            return UserId(context) > 0;
        }

        public static bool IsAdmin(HttpContext context)
        {
            return string.Equals(
                Convert.ToString(context.Session["Role"]),
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );
        }

        public static bool RequireLogin(HttpContext context)
        {
            if (IsLoggedIn(context))
                return true;

            JsonHelper.Error(
                context, 401, "กรุณาเข้าสู่ระบบก่อนใช้งาน"
            );

            return false;
        }

        public static bool RequireAdmin(HttpContext context)
        {
            if (!RequireLogin(context))
                return false;

            if (IsAdmin(context))
                return true;

            JsonHelper.Error(
                context, 403, "คุณไม่มีสิทธิ์ใช้งานส่วนนี้"
            );

            return false;
        }

        public static int DepartmentId(HttpContext context)
        {
            object value = context.Session["DepartmentID"];

            if (value == null)
                return 0;

            return Convert.ToInt32(value);
        }

        public static string Role(HttpContext context)
        {
            return Convert.ToString(context.Session["Role"]);
        }
    }
}
