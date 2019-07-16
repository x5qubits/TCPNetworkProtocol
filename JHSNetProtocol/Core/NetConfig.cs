namespace JHSNetProtocol
{
    public static class NetConfig
    {
        public static JHSLogFilter logFilter = JHSLogFilter.Log;
        public static int Port = 10001;
        public static string IP = "91.134.249.101";   
        public static bool UseStatistics = true;
        public static short Key = 1985;
        public static uint Version = 1;
        public static int ReconnectAttempts = 3;
        public static int ReconnectTimeOut = 5;
    }
}
