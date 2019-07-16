namespace JHSNetProtocol
{
    public static class JHSStatisiticsManager
    {
        public static void Remove(JHSConnection con)
        {
            if (!NetConfig.UseStatistics)
                return;

            JHSDebug.Log("JHSStatisiticsManager :: " + con.ToString());
        }
    }
}
