using System;
namespace JHSNetProtocol
{
    public interface IJHSLogger
    {
        void LogWarning(string v);

        void Log(object v);

        void LogError(string v);

        void LogError(object v);
    }

    public static class JHSDebug
    {
        public static IJHSLogger LogReciver = null;

        public static void LogWarning(string v)
        {
#if !UNITY_EDITOR
            Console.WriteLine(v);
#endif
            if (LogReciver != null)
                LogReciver.LogWarning(v);
        }

        public static void Log(object v)
        {
#if !UNITY_EDITOR
            Console.WriteLine(v.ToString());
#endif
            if (LogReciver != null)
                LogReciver.Log(v);
        }

        public static void LogError(string v)
        {
#if !UNITY_EDITOR
            Console.WriteLine(v);
#endif
            if (LogReciver != null)
                LogReciver.LogError(v);
        }

        public static void LogError(object v)
        {
#if !UNITY_EDITOR
            Console.WriteLine(v.ToString());
#endif
            if (LogReciver != null)
                LogReciver.LogError(v);
        }
    }
}

