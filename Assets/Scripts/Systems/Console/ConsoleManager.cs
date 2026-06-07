using System;
using System.Collections;
using System.Collections.Generic;
using Scripts.Core;
using UnityEngine;

namespace Scripts.Systems.Console
{
    public class ConsoleManager : MonoBehaviour
    {

        [SerializeField] private RectTransform linesList;

        private LinePanel[] _linesList;
        
        private void OnEnable() => UpdateConsoleSize();

        public void ClickedSendButton()
        {
            
        }

        public void ClickedPrintButton()
        {
            
        }

        /*
        private IEnumerator LoadStuff()
        {
            
        }
        */
        
        public void UpdateConsoleSize()
        {
            if (_linesList == null)
            {
                for (int i = linesList.childCount; i < 50; i++)
                    Instantiate(linesList.GetChild(0), linesList);
                _linesList = linesList.GetComponentsInChildren<LinePanel>();
            }
            int possibleLines = Mathf.CeilToInt(linesList.rect.height / Constants.CONSOLE_EACH_LINE_HEIGHT);
            int logCounter = 0;
            List<LogData> currentLogs = new List<LogData>(Journal.LatestLogs(possibleLines));
            float width = linesList.rect.width;
            foreach (LinePanel linePanel in _linesList)
            {
                if (logCounter >= possibleLines || logCounter >= currentLogs.Count)
                {
                    linePanel.gameObject.SetActive(false);
                    continue;
                }
                linePanel.gameObject.SetActive(true);
                LogData log = currentLogs[logCounter++];
                linePanel.UpdateText(log.LogType.Colored() + log.Message, width);
            }
        }
        
    }
}