using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts.Systems.Console
{
    public class Journal
    {
        
        public event Action<LogData> OnLog;
        private static List<LogData> _logs;
        private static List<LogData> Logs => _logs ??= new List<LogData>();
        public static IReadOnlyList<LogData> LatestLogs(int amount = 60) => Enumerable.Reverse(Logs).Take(amount > Logs.Count ? Logs.Count : amount).ToList();

        public Journal()
        {
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
        {
            LogData loggedData = new LogData(condition, stackTrace, type);
            Logs.Add(loggedData);
            OnLog?.Invoke(loggedData);
        }
        
    }
}