using UnityEngine;

namespace Scripts.Systems.Console
{
    public static class LogTypeExtensions
    {

        public static string Colored(this LogType logType) => logType switch
        {
            LogType.Error => "<color=red>",
            LogType.Warning => "<color=yellow>",
            _ => "<color=white>"
        };

    }
}