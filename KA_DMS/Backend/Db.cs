
using System.Configuration;
using System.Data.SqlClient;

namespace KA_DMS
{
    public static class Db
    {
        public static SqlConnection GetConnection()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["KAScienceDMS"];

            if (settings == null)
            {
                throw new ConfigurationErrorsException(
                    "ไม่พบ Connection String ชื่อ KAScienceDMS"
                );
            }

            return new SqlConnection(settings.ConnectionString);
        }
    }
}
