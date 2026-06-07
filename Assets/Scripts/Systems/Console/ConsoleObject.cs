using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts.Systems.Console
{
    public class ConsoleObject : MonoBehaviour
    {

        public static ConsoleObject Object { get; private set; }
        
        private ConsoleManager _consoleObject;
        private void Start()
        {
            if (Object != null && Object != this)
            {
                Destroy(gameObject);
                return;
            }
            Object = this;
            DontDestroyOnLoad(gameObject);
            _consoleObject = GetComponentInChildren<ConsoleManager>();
            _consoleObject.gameObject.SetActive(false);
            new Journal().OnLog += NewLogReceived;
        }

        private void Update()
        {
            if (Keyboard.current.periodKey.wasPressedThisFrame)
                _consoleObject.gameObject.SetActive(!_consoleObject.gameObject.activeSelf);
        }

        private void NewLogReceived(LogData logData)
        {
            if (!_consoleObject.gameObject.activeSelf) return;
            _consoleObject.UpdateConsoleSize();
        }
        
        private void OnApplicationQuit()
        {
            new Journal().OnLog -= NewLogReceived;
        }
    }
}