using System;

namespace UnityHTTP
{
    public interface ILogger
    {
        void Log(string msg);
        void LogError(string msg);
        void LogException(Exception e);
        void LogWarning(string msg);
    }
#if UNITY_EDITOR
    public class UnityLogger : ILogger
    {
        public void Log(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }
        public void LogError(string msg)
        {
            UnityEngine.Debug.LogError(msg);
        }
        public void LogException(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
        public void LogWarning(string msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }
    }
#endif
    public class ConsoleLogger : ILogger
    {
        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }
        public void LogError(string msg)
        {
            Console.WriteLine(msg);
        }
        public void LogException(Exception e)
        {
            Console.WriteLine(e);
        }
        public void LogWarning(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}