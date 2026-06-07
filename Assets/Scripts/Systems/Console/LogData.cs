using UnityEngine;

namespace Scripts.Systems.Console
{
    public struct LogData
    {
        public string Message { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }

        public LogData(string message, string stackTrace, LogType logType)
        {
            Message = message;
            StackTrace = stackTrace;
            LogType = logType;
        }
    }
}