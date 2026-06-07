using System;
using System.Threading.Tasks;
using Scripts.Core;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Scripts.Systems.Network
{
    public class SessionManager : MonoBehaviour
    {
        
        public static SessionManager Instance { get; private set; }

        public ISession Session { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetSession(ISession session) => Session = session;

        public void ClearSession() => Session = null;

        public async Task CancelLobbyAsHost()
        {
            try
            {
                if (IsLobbyHost)
                    await Session.AsHost().DeleteAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to delete lobby: {e.Message}");
            }
            finally
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
                ClearSession();
            }
        }

        public bool IsLobbyHost => Session != null && Session.IsHost;
        public bool IsRankedMatch => Session != null && !Session.IsPrivate;

        public async Task LeaveLobby()
        {
            if (Session != null)
            {
                //await LobbyService.Instance.RemovePlayerAsync(Session.Id, AuthenticationService.Instance.PlayerId);
                await Session.LeaveAsync();
                Session = null;
            }
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
        
        private ILeaderboardsService _leaderboards;
        private ILeaderboardsService Leaderboard => _leaderboards ??= UnityServices.Instance.GetLeaderboardsService();
        
        internal async Task UpdateEloAsync(bool won, int eloChanged)
        {
            await ServiceInitializer.ReadyTask;
            
            int currentScore = await GetCurrentScore();

            int newScore = won ? currentScore + eloChanged : currentScore - eloChanged;
            
            try
            {
                await Leaderboard.AddPlayerScoreAsync(Constants.LEADERBOARD_ID, newScore);
                Debug.Log($"New elo score({newScore}) submitted to the leaderboard!");
            }
            catch (Exception e)
            {
                Debug.LogError("Error updating elo scores: " + e);
            }
        }
        
        private async Task<int> GetCurrentScore()
        {
            while (!AuthenticationService.Instance.IsSignedIn)
                await ServiceInitializer.ReadyTask;
            try
            {
                LeaderboardEntry entry =
                    await Leaderboard.GetPlayerScoreAsync(Constants.LEADERBOARD_ID);
                return (int)entry.Score;
            }
            catch (Exception)
            {
                return Constants.DEFAULT_ELO;
            }
        }
        
    }
}