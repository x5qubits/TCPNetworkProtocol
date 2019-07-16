namespace JHSNetProtocol
{
    public enum PerStage
    {
        NotConnected,
            Connecting,
            Verifying,
            Connected,
    }

    public enum JHSLogFilter
    {
        Log = 0,
        Error = 1,
        Warning = 2,
        Developer = 3
    }
}
