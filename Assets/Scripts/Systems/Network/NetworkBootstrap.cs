using UnityEngine;

namespace Scripts.Systems.Network
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}