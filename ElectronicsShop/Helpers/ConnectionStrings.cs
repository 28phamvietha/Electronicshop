using System.Configuration;

namespace ElectronicsShop.Helpers
{
    public static class ConnectionStrings
    {
        public static string DefaultConnection
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
                return cs ?? string.Empty;
            }
        }
    }
}