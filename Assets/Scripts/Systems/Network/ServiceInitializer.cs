using System;
using System.Linq;
using System.Threading.Tasks;
using Scripts.Core;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scripts.Systems.Network
{
    public class ServiceInitializer : MonoBehaviour
    {
        
        public static ServiceInitializer Instance { get; private set; }

        public static Task ReadyTask => _readyTcs.Task;
        private static TaskCompletionSource<bool> _readyTcs = new TaskCompletionSource<bool>();

        private void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _ = InitializeServices();
        }

        public string PlayerId { get; private set; }
        
        private async Task InitializeServices()
        {
            _readyTcs = new TaskCompletionSource<bool>();

            string profile = "player1";
            
            if (Environment.GetCommandLineArgs().Length > 0)
                profile = GetArg("--profile") ?? "player1";
            
            if (CurrentPlayer.Tags.Count > 0)
                profile = CurrentPlayer.Tags[0].ToLower();
            
            Debug.Log("[Auth] Profile: " + profile);
            InitializationOptions options = new InitializationOptions();
            //options.SetEnvironmentName("newproduction");
            options.SetProfile(profile);
            
            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            PlayerId =  AuthenticationService.Instance.PlayerId;
            Debug.Log("[Auth] Signed in as PlayerID: " + PlayerId);
            
            AuthenticationService.Instance.Expired += async () =>
            {
                Debug.LogWarning("[Auth] Token expired, re-signing in...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Auth] Re-signed in successfully.");
            };

            if (string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName))
            {
                Debug.Log("Player has no name, giving random name.");
                string randomName = Constants.PLAYER_NAMES.ToList()[Random.Range(0, Constants.PLAYER_NAMES.Length)];
                await AuthenticationService.Instance.UpdatePlayerNameAsync(randomName);
                Debug.Log("[Auth] New Player Name: " + randomName);
            }
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log("[Auth] Player Logged in with name: " + playerName);
            
            _readyTcs.TrySetResult(true);
        }
        
        static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }
        
        private void OnApplicationQuit()
        {
            try
            {
                if (AuthenticationService.Instance.IsSignedIn)
                    AuthenticationService.Instance.SignOut();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auth] Cleanup on quit: {e.Message}");
            }
        }
    }
}