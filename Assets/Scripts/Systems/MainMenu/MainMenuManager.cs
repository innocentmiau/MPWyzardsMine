using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scripts.Core;
using Scripts.Systems.Interface;
using Scripts.Systems.MainMenu.Lobby;
using Scripts.Systems.Network;
using Scripts.Systems.Network.Lobby;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Scripts.Systems.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        
        [SerializeField] private GameObject backgroundObject;
        [SerializeField] private TMP_Text tmpGameStatus;
        
        private void Start()
        {
            CloseJoinLobbyPanel();
            CloseLobbyPanel();
            CloseLeaderboardPanel();
            CloseHostLobbyPanel();
            CloseRankedPanel();
            CloseRankedLobbyPanel();
            _ = CheckGameStatus();
            _ = CleanupAfterMatchAsync();

            SessionConnector.Instance.GameStarting += GameStartingTimer;
        }

        private async Task CleanupAfterMatchAsync()
        {
            if (SessionManager.Instance.Session == null) return;
            try
            {
                if (SessionManager.Instance.IsLobbyHost)
                    await SessionManager.Instance.CancelLobbyAsHost();
                else
                    await SessionManager.Instance.LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MainMenu] Post-match cleanup: {e.Message}");
                SessionManager.Instance.ClearSession();
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
            }
        }

        private void GameStartingTimer()
        {
            if (_startingTimerCoro != null) StopCoroutine(_startingTimerCoro);
            _startingTimerCoro = StartCoroutine(GameStartingTimers());
        }

        private Coroutine _startingTimerCoro;
        private IEnumerator GameStartingTimers()
        {
            float toElapse = Constants.LOBBY_GAME_STARTING_TIMER;
            while (toElapse > 0f) 
            {
                toElapse -= Time.deltaTime;
                yield return null;
                lobbyMadePanel.UpdateLobbyStatus($"Starting in {toElapse:F0}s...");
            }
        }

        private async Task CheckGameStatus()
        {
            string applicationVersion = Application.version;
            //await Task.Delay(100);
            await ServiceInitializer.ReadyTask;
            string playerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "NotSignedIn";
            tmpGameStatus.text = $"v{applicationVersion}, {playerId}";
        }

        private void UpdateBlackBackground(bool value)
        {
            if (backgroundObject) backgroundObject.SetActive(value);
        }

        #region Ranked

        [Header("Ranked")]
        [SerializeField] private GameObject rankedPanel;
        [SerializeField] private TMP_Text tmpSearchingStatus;
        [SerializeField] private TMP_Text tmpSearchingTimer;

        private Coroutine _searchingTimerCoro;
        private CancellationTokenSource _matchmakingCts;
        private ISession _pendingRankedSession;

        [Header("Ranked Lobby Panel")]
        [SerializeField] private GameObject rankedLobbyPanelObject;
        [SerializeField] private RankedLobbyPanel rankedLobbyMadePanel;
        private Coroutine _rankedCountdownCoro;

        public void ClickSearchRankedMatch()
        {
            UpdateBlackBackground(true);
            if (rankedPanel) rankedPanel.SetActive(true);
            _ = SearchingRankedMatch();
        }

        private void CloseRankedPanel()
        {
            UpdateBlackBackground(false);
            if (rankedPanel) rankedPanel.SetActive(false);
        }

        public void ClickCancelRankedMatch()
        {
            // Phase 1 — still searching (MatchmakeSessionAsync not done yet)
            _matchmakingCts?.Cancel();

            // Phase 2 — search done, this player is host waiting for an opponent
            if (_pendingRankedSession != null)
                _ = CancelHostWaitingAsync();
        }

        private async Task SearchingRankedMatch()
        {
            _matchmakingCts?.Dispose();
            _matchmakingCts = new CancellationTokenSource();

            if (_searchingTimerCoro != null) StopCoroutine(_searchingTimerCoro);
            _searchingTimerCoro = StartCoroutine(SearchingTimerCoro());
            if (tmpSearchingStatus) tmpSearchingStatus.text = "Searching for a match...";

            try
            {
                float jitter = UnityEngine.Random.Range(0f, 3f);
                QuickJoinOptions quickJoinOptions = new QuickJoinOptions()
                {
                    Filters = new List<FilterOption>
                    {
                        new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
                    },
                    Timeout = TimeSpan.FromSeconds(5f + jitter),
                    CreateSession = true
                };

                SessionOptions sessionOptions = new SessionOptions()
                {
                    MaxPlayers = 2,
                    IsPrivate = false,
                    IsLocked = false,
                }.WithRelayNetwork();

                Task<ISession> matchmakeTask = MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
                Task cancelTask = Task.Delay(Timeout.Infinite, _matchmakingCts.Token);

                await Task.WhenAny(matchmakeTask, cancelTask);

                if (_matchmakingCts.IsCancellationRequested)
                {
                    if (matchmakeTask.IsCompletedSuccessfully)
                    {
                        await CleanupMatchmakingSessionAsync(matchmakeTask.Result);
                    }
                    else
                    {
                        _ = matchmakeTask
                            .ContinueWith(t => CleanupMatchmakingSessionAsync(t.Result),
                                CancellationToken.None,
                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                TaskScheduler.Default)
                            .Unwrap()
                            .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                    Debug.LogError($"[Ranked] Cleanup after cancel failed: {t.Exception?.GetBaseException().Message}");
                            });
                    }

                    if (tmpSearchingStatus) tmpSearchingStatus.text = "Search cancelled.";
                    CloseRankedPanel();
                    return;
                }

                ISession session = await matchmakeTask;
                SessionManager.Instance.SetSession(session);
                SessionConnector.Instance.ResetLobbyInformation();
                session.PlayerJoined += OnPlayerJoined;
                session.PlayerLeaving += OnPlayerLeaving;

                SessionConnector.Instance.NewPlayerJoined -= UpdateLobbyWithCurrentPlayers;
                SessionConnector.Instance.NewPlayerJoined += UpdateLobbyWithCurrentPlayers;
                NetworkManager.Singleton.OnServerStopped += OnNetworkServerStopped;

                if (session.IsHost)
                {
                    // Subscribe before the leaderboard await — PlayerJoined fires on the main thread
                    // and would be missed if the opponent connects while GetSelfPlayerRating is in flight
                    _pendingRankedSession = session;
                    if (tmpSearchingStatus) tmpSearchingStatus.text = "Waiting for an opponent...";
                    session.PlayerJoined += OnRankedOpponentJoined;
                }

                string playerId = AuthenticationService.Instance.PlayerId;
                LeaderboardEntry ourEntry = await GetSelfPlayerRating();

                // session.State reflects the MPS lobby, not the NGO relay handshake —
                // poll IsConnectedClient which is the actual gate for RPC calls
                for (int i = 0; i < 100 && !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost; i++)
                    await Task.Delay(50);

                SessionConnector.Instance?.SendToServerPlayerInformationRpc(new PlayerLobbyInformation(playerId, ourEntry.PlayerName, (int)ourEntry.Score, ourEntry.Rank, ourEntry.Tier));

                if (!session.IsHost)
                {
                    CloseRankedPanel();
                    OpenRankedLobbyPanel();
                }
                else if (_pendingRankedSession != null && session.Players.Count >= 2)
                {
                    // Catch-all: opponent joined while GetSelfPlayerRating was in flight but
                    // OnRankedOpponentJoined either missed the event or failed silently
                    OnRankedOpponentJoined(string.Empty);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (tmpSearchingStatus) tmpSearchingStatus.text = $"{StatusType.ERROR.Color()}Failed to find a match.";
                CloseRankedPanel();
            }
            finally
            {
                // Keep the timer running while host is waiting for an opponent
                if (_pendingRankedSession == null)
                {
                    if (_searchingTimerCoro != null)
                    {
                        StopCoroutine(_searchingTimerCoro);
                        _searchingTimerCoro = null;
                    }
                    if (tmpSearchingTimer) tmpSearchingTimer.text = "";
                }
            }
        }

        private void OnRankedOpponentJoined(string playerId)
        {
            if (_pendingRankedSession == null) return;
            _pendingRankedSession.PlayerJoined -= OnRankedOpponentJoined;
            _pendingRankedSession = null;

            if (_searchingTimerCoro != null)
            {
                StopCoroutine(_searchingTimerCoro);
                _searchingTimerCoro = null;
            }
            if (tmpSearchingTimer) tmpSearchingTimer.text = "";

            CloseRankedPanel();
            OpenRankedLobbyPanel();
        }

        private async Task CancelHostWaitingAsync()
        {
            ISession session = _pendingRankedSession;
            _pendingRankedSession = null;
            session.PlayerJoined -= OnRankedOpponentJoined;

            if (_searchingTimerCoro != null)
            {
                StopCoroutine(_searchingTimerCoro);
                _searchingTimerCoro = null;
            }
            if (tmpSearchingTimer) tmpSearchingTimer.text = "";

            try
            {
                await session.AsHost().DeleteAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Ranked] Cancel host session failed: {e.Message}");
            }
            finally
            {
                CloseRankedPanel();
            }
        }

        private async Task CleanupMatchmakingSessionAsync(ISession session)
        {
            try
            {
                if (session.IsHost)
                    await session.AsHost().DeleteAsync();
                else
                    await session.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Ranked] Session cleanup failed: {e.Message}");
            }
        }

        private IEnumerator SearchingTimerCoro()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                if (tmpSearchingTimer) tmpSearchingTimer.text = $"{elapsed:F0}s";
                yield return null;
            }
        }

        public void OpenRankedLobbyPanel()
        {
            if (rankedLobbyPanelObject) rankedLobbyPanelObject.SetActive(true);
            UpdateBlackBackground(true);
            if (_rankedCountdownCoro != null) StopCoroutine(_rankedCountdownCoro);
            _rankedCountdownCoro = StartCoroutine(RankedCountdownCoro());
        }

        public void CloseRankedLobbyPanel()
        {
            if (rankedLobbyPanelObject) rankedLobbyPanelObject.SetActive(false);
            if (_rankedCountdownCoro != null)
            {
                StopCoroutine(_rankedCountdownCoro);
                _rankedCountdownCoro = null;
            }
            UpdateBlackBackground(false);
        }

        private IEnumerator RankedCountdownCoro()
        {
            float remaining = Constants.RANKED_LOBBY_COUNTDOWN;
            while (remaining > 0f)
            {
                rankedLobbyMadePanel?.UpdateCountdown(remaining);
                remaining -= Time.deltaTime;
                yield return null;
            }
            rankedLobbyMadePanel?.UpdateCountdown(0f);
            _rankedCountdownCoro = null;

            if (SessionManager.Instance.IsLobbyHost)
                SessionConnector.Instance.RankedMatchIsStartingRpc();
        }

        private async Task CancelRankedLobbyAsync()
        {
            CloseRankedLobbyPanel();
            try
            {
                if (SessionManager.Instance.IsLobbyHost)
                    await SessionManager.Instance.CancelLobbyAsHost();
                else
                    await SessionManager.Instance.LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Ranked] Ranked lobby cancellation failed: {e.Message}");
            }
        }

        #endregion
        
        #region JoinLobbyPanel
        [Header("Join Lobby Panel")]
        [SerializeField] private GameObject joinLobbyPanel;
        [SerializeField] private TMP_InputField lobbyCodeInputField;
        [SerializeField] private TMP_Text joiningStatusText;
        [SerializeField] private BetterButton joinButton;
        
        public void OpenJoinLobbyPanel()
        {
            if (joinLobbyPanel) joinLobbyPanel.SetActive(true);
            UpdateBlackBackground(true);
            UpdateJoiningStatusText("");
        }

        public void ClickedJoinCodeLobbyButton()
        {
            joinButton?.SetInteractable(false);
            string lobbyCode = lobbyCodeInputField.text;
            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length > 6)
            {
                UpdateJoiningStatusText($"{StatusType.ERROR.Color()}Lobby code invalid.");
                joinButton?.SetInteractable(true);
                return;
            }
            _ = JoiningSession(lobbyCode);
        }
        
        private async Task JoiningSession(string lobbyCode) 
        {
            UpdateJoiningStatusText("Trying to join lobby...");
            try
            {

                await ServiceInitializer.ReadyTask;
                
                ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(lobbyCode);
                UpdateJoiningStatusText("Found lobby, setting up connection...");
                SessionManager.Instance.SetSession(session);
                SessionConnector.Instance.ResetLobbyInformation();
                session.PlayerJoined += OnPlayerJoined;
                session.PlayerLeaving += OnPlayerLeaving;
                //NetworkManager.Singleton.StartClient();
                
                
                SessionConnector.Instance.NewPlayerJoined -= UpdateLobbyWithCurrentPlayers;
                SessionConnector.Instance.NewPlayerJoined += UpdateLobbyWithCurrentPlayers;
                NetworkManager.Singleton.OnServerStopped += OnNetworkServerStopped;
                
                UpdateJoiningStatusText("Joined lobby successfully, moving to the lobby...");
                string playerId = AuthenticationService.Instance.PlayerId;
                LeaderboardEntry ourEntry = await GetSelfPlayerRating();
                SessionConnector.Instance?.SendToServerPlayerInformationRpc(new PlayerLobbyInformation(playerId, ourEntry.PlayerName, (int)ourEntry.Score, ourEntry.Rank, ourEntry.Tier));
                CloseJoinLobbyPanel();
                OpenLobbyPanel();

            } catch (Exception e) {
                Debug.LogError(e);
                UpdateJoiningStatusText($"{StatusType.ERROR.Color()}Couldn't join the lobby.\nError: " + e.Message);
                joinButton?.SetInteractable(true);
            }
        }

        public void CloseJoinLobbyPanel()
        {
            if (joinLobbyPanel) joinLobbyPanel.SetActive(false);
            UpdateBlackBackground(false);
        }

        private void UpdateJoiningStatusText(string msg, StatusType status = StatusType.BASIC)
        {
            if (joiningStatusText) joiningStatusText.text = $"{status.Color()}{msg}";
        }
        #endregion

        #region LobbyPanel

        [Header("Lobby Panel")]
        [SerializeField] private GameObject lobbyPanel;

        public void OpenLobbyPanel()
        {
            if (lobbyPanel) lobbyPanel.SetActive(true);
            UpdateBlackBackground(true);
            lobbyMadePanel.UpdateWindow();
        }

        public void ClickLeaveLobby()
        {
            if (SessionManager.Instance.IsLobbyHost) return;
            _ = LeavingLobby();
        }

        public void ClickCancelLobby()
        {
            if (!SessionManager.Instance.IsLobbyHost) return;
            /*
            NetworkManager.Singleton.Shutdown();
            await _session.LeaveAsync();
            _session = null;
             */
            _ = CancellingLobby();
        }

        public void ClickedStartGameButton()
        {
            if (!SessionManager.Instance || !SessionManager.Instance.IsLobbyHost) return;
            //_ = StartingLobbyMatch();
            //NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            SessionConnector.Instance.CasualLobbyIsStartingRpc();
        }
        
        
        private async Task LeavingLobby()
        {
            try
            {
                await SessionManager.Instance.LeaveLobby();
                CloseLobbyPanel();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private async Task CancellingLobby()
        {
            try
            {
                await SessionManager.Instance.CancelLobbyAsHost();
                
                CloseLobbyPanel();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        public void CloseLobbyPanel()
        {
            if (lobbyPanel) lobbyPanel.SetActive(false);
            UpdateBlackBackground(false);
        }

        #endregion

        #region Leaderboard

        [Header("Leaderboard Panel")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private RectTransform leaderboardListTransform;
        [SerializeField] private TMP_Text tmpSelfRank;
        [SerializeField] private TMP_Text tmpSelfName;
        [SerializeField] private TMP_Text tmpSelfRating;
        [SerializeField] private TMP_Text tmpSelfTier;

        private List<LeaderboardPlayerListed> _leaderboardRows;
        private List<LeaderboardPlayerListed> LeaderboardRows
        {
            get
            {
                if (_leaderboardRows == null)
                {
                    _leaderboardRows = new List<LeaderboardPlayerListed>();
                    _leaderboardRows.AddRange(leaderboardListTransform.GetComponentsInChildren<LeaderboardPlayerListed>(true));
                }
                return _leaderboardRows;
            }
        }

        public void OpenLeaderboardPanel()
        {
            if (leaderboardPanel) leaderboardPanel.SetActive(true);
            UpdateBlackBackground(true);
            _ = PopulateLeaderboardAsync();
        }

        private async Task PopulateLeaderboardAsync()
        {
            Task<LeaderboardScoresPage> pageTask = GetLeaderboard(0);
            Task<LeaderboardEntry> selfTask = GetSelfPlayerRating();
            await Task.WhenAll(pageTask, selfTask);

            LeaderboardScoresPage page = pageTask.Result;
            LeaderboardEntry self = selfTask.Result;

            if (page != null)
            {
                int rowCount = LeaderboardRows.Count;
                int entryCount = page.Results.Count;
                for (int i = 0; i < rowCount; i++)
                {
                    if (i < entryCount)
                    {
                        LeaderboardEntry entry = page.Results[i];
                        LeaderboardRows[i].UpdateWithEntry((int)entry.Rank + 1, entry.PlayerName, (int)entry.Score, entry.Tier);
                    }
                    else
                    {
                        LeaderboardRows[i].Clear();
                    }
                }
            }

            if (self != null)
            {
                if (tmpSelfRank)   tmpSelfRank.text   = self.Rank >= 0 ? $"#{self.Rank + 1}" : "-";
                if (tmpSelfName)   tmpSelfName.text   = self.PlayerName;
                if (tmpSelfRating) tmpSelfRating.text = $"{(int)self.Score}";
                if (tmpSelfTier)   tmpSelfTier.text   = self.Tier ?? "Unranked";
            }
        }

        public void CloseLeaderboardPanel()
        {
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            UpdateBlackBackground(false);
        }

        #endregion

        #region HostLobbyPanel

        [Header("Host Lobby Panel")]
        [SerializeField] private GameObject hostLobbyPanel;
        [SerializeField] private TMP_Text hostInstructionsText;
        [SerializeField] private GameObject hideObjectOnConfirmation;
        [SerializeField] private LobbyPanel lobbyMadePanel;

        public void ClickHostLobbyPanel()
        {
            if (hostLobbyPanel) hostLobbyPanel.SetActive(true);
            UpdateBlackBackground(true);
            if (hideObjectOnConfirmation) hideObjectOnConfirmation.SetActive(true);
            hostInstructionsText.text = "Are you sure you want to host a lobby?";

            // await authentication relay sht

            //string thisDudeId = "ThisDudeId";

            //CloseHostLobbyPanel();
            //OpenLobbyPanel(new LobbyInformation(thisDudeId, new []{thisDudeId}));
        }

        public void ClickConfirmHostLobby()
        {
            if (hideObjectOnConfirmation) hideObjectOnConfirmation.SetActive(false);
            _ = CreatingLobby();
        }

        private async Task CreatingLobby()
        {
            
            hostInstructionsText.text = "<i>Trying to host a lobby...";

            try
            {
                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                    await Task.Delay(500);
                }
                hostInstructionsText.text = "<i>Hosting the lobby...";

                SessionOptions options = new SessionOptions()
                {
                    MaxPlayers = 2,
                    IsPrivate = true,
                    IsLocked = false
                }.WithRelayNetwork();
                
                ISession session = await MultiplayerService.Instance.CreateSessionAsync(options);
                SessionManager.Instance.SetSession(session);
                SessionConnector.Instance.ResetLobbyInformation();
                session.PlayerJoined += OnPlayerJoined;
                session.PlayerLeaving += OnPlayerLeaving;
                hostInstructionsText.text = "<i>Lobby created successfully, code: </i><color=white>" + session.Code;

                
                SessionConnector.Instance.NewPlayerJoined -= UpdateLobbyWithCurrentPlayers;
                SessionConnector.Instance.NewPlayerJoined += UpdateLobbyWithCurrentPlayers;
                NetworkManager.Singleton.OnServerStopped += OnNetworkServerStopped;
                
                //NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                //await Task.Delay(1000);
                CloseHostLobbyPanel();
                //string playerId = ServiceInitializer.Instance.PlayerId ?? "PLAYER_ID_NOT_FOUND";
                OpenLobbyPanel();

                string ourId = session.Host;
                LeaderboardEntry ourEntry = await GetSelfPlayerRating();
                SessionConnector.Instance.SendToServerPlayerInformationRpc(new PlayerLobbyInformation(ourId, ourEntry.PlayerName, (int)ourEntry.Score, ourEntry.Rank, ourEntry.Tier));
            }
            catch (Exception e)
            {
                Debug.Log(e);
                hostInstructionsText.text = $"{StatusType.ERROR.Color()}Failed to create a lobby.";
            }
        }

        private void UpdateLobbyWithCurrentPlayers(LobbyInformation lobbyInformation)
        {
            lobbyMadePanel.UpdatePlayersInLobby(lobbyInformation);
            if (rankedLobbyPanelObject && rankedLobbyPanelObject.activeSelf)
                rankedLobbyMadePanel?.UpdatePlayersInLobby(lobbyInformation);
        }

        private void OnNetworkServerStopped(bool wasHost)
        {
            SessionConnector.Instance.NewPlayerJoined -= UpdateLobbyWithCurrentPlayers;
            NetworkManager.Singleton.OnServerStopped -= OnNetworkServerStopped;
            if (rankedLobbyPanelObject && rankedLobbyPanelObject.activeSelf)
                CloseRankedLobbyPanel();
        }
        
        public void OnPlayerJoined(string playerId)
        {
            Debug.Log("Player joined the lobby: " + playerId);
            //_ = GetPlayerInformation(playerId);
        }

        /*
        private async Task GetPlayerInformation(string playerId)
        {
            LeaderboardEntry ourEntry = await GetSinglePlayerLeaderboard(Constants.LEADERBOARD_ID, playerId);
            lobbyMadePanel.UpdateLobbyWithNewPlayer(new PlayerLobbyInformation(playerId, ourEntry.PlayerName, (int)ourEntry.Score, ourEntry.Rank, ourEntry.Tier));
        }
        */
        

        public void OnPlayerLeaving(string playerId)
        {
            Debug.Log("Player left the lobby: " + playerId);
            if (rankedLobbyPanelObject && rankedLobbyPanelObject.activeSelf)
                _ = CancelRankedLobbyAsync();
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log("Client disconnected: " + clientId);
        }
        
        private void OnClientConnected(ulong clientId)
        {
            // update lobby.
            Debug.Log("Client connected: " + clientId);
            //Task<LeaderboardEntry> entry = GetSinglePlayerLeaderboard(Constants.LEADERBOARD_ID, clientId);
            //lobbyMadePanel.AddNewPlayerConnected(new PlayerLobbyInformation());
        }
        

        public void CloseHostLobbyPanel()
        {
            if (hostLobbyPanel) hostLobbyPanel.SetActive(false);
            UpdateBlackBackground(false);
        }

        #endregion

        
        private ILeaderboardsService _leaderboards;
        private ILeaderboardsService Leaderboard => _leaderboards ??= UnityServices.Instance.GetLeaderboardsService();

        private async Task<LeaderboardEntry> GetSinglePlayerLeaderboard(string leaderboardId, string playerId)
        {
            //return await GetSeveralPlayersLeaderboard(leaderboardId, new List<string>() { playerId });
            while (!AuthenticationService.Instance.IsSignedIn)
                await ServiceInitializer.ReadyTask;
            try
            {
                LeaderboardScoresWithNotFoundPlayerIds entry =
                    await GetSeveralPlayersLeaderboard(leaderboardId, new List<string>() { playerId });
                return entry.Results[0];
            }
            catch (Exception e)
            {
                // couldn't find in leaderboard so give default. don't update yet on leaderboard until players does first game. waste of space lol
                //return (-1, Constants.DEFAULT_ELO, "Recruit", "NOT_FOUND");
                Debug.LogError(e);
                return null;
            }
        }
        
        private async Task<LeaderboardScoresWithNotFoundPlayerIds> GetSeveralPlayersLeaderboard(string leaderboardId, List<string> playerIds)
        {
            //Task<LeaderboardScoresWithNotFoundPlayerIds> GetScoresByPlayerIdsAsync
            while (!AuthenticationService.Instance.IsSignedIn)
                await ServiceInitializer.ReadyTask;
            try
            {
                return await Leaderboard.GetScoresByPlayerIdsAsync(leaderboardId, playerIds);
            }
            catch (Exception e)
            {
                // couldn't find in leaderboard so give default. don't update yet on leaderboard until players does first game. waste of space lol
                //return (-1, Constants.DEFAULT_ELO, "Recruit", "NOT_FOUND");
                Debug.LogError(e);
                return null;
            }
        }
        
        private async Task<LeaderboardScoresWithNotFoundPlayerIds> GetMultiplePlayersRating(List<string> playerIds)
        {
            try
            {
                return await Leaderboard.GetScoresByPlayerIdsAsync(Constants.LEADERBOARD_ID, playerIds);
            }
            catch (Exception e)
            {
                // couldn't find in leaderboard so give default. don't update yet on leaderboard until players does first game. waste of space lol
                //return (-1, Constants.DEFAULT_ELO, "Recruit", "NOT_FOUND");
                Debug.LogError(e);
                return null;
            }
        }
        private async Task<LeaderboardEntry> GetSelfPlayerRating()
        {
            while (!AuthenticationService.Instance.IsSignedIn)
                await ServiceInitializer.ReadyTask;
            try
            {
                return await Leaderboard.GetPlayerScoreAsync(Constants.LEADERBOARD_ID);
            }
            catch (Exception)
            {
                // couldn't find in leaderboard so give default. don't update yet on leaderboard until players does first game. waste of space lol
                //return (-1, Constants.DEFAULT_ELO, "Recruit", "NOT_FOUND");
                return new LeaderboardEntry(AuthenticationService.Instance.PlayerId, AuthenticationService.Instance.PlayerName, -1, Constants.DEFAULT_ELO, null);
            }
        }
        
        private async Task<LeaderboardScoresPage> GetLeaderboard(int page)
        {
            while (!AuthenticationService.Instance.IsSignedIn)
                await ServiceInitializer.ReadyTask;
            try
            {
                GetScoresOptions options = new GetScoresOptions();
                options.Offset = (page * 10);
                LeaderboardScoresPage scores = await Leaderboard.GetScoresAsync(Constants.LEADERBOARD_ID, options);
                return scores;
            }
            catch (Exception e)
            {
                Debug.LogError("Error finding leaderboard or scores: " + e);
                return null;
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
                // couldn't find in leaderboard so give default. don't update yet on leaderboard until players does first game. waste of space lol
                return Constants.DEFAULT_ELO;
            }
        }
        
        public void ClickQuitGame()
        {
            Application.Quit();
        }
    }
}
