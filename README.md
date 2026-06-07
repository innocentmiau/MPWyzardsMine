# MPWyzards

A top-down 2D multiplayer action game built in Unity 6.3 for the Sistemas de Redes para Jogos 2025/2026 course project. For this particular case very focused on the lobbies/matchmaking part, casual/custom lobbies, custom elo earned/lost in matches and leaderboard.

---

## Overview

MPWyzards is a 2-player online game where both players control a wizard that automatically shoots at enemies. You collect XP from kills to level up and pick up upgrades. The match is technically cooperative since you're both fighting the same enemies, but competitive because the first one to die loses.

There are two ways to play: **Casual**, where the host shares a code and the other player joins with it, and **Ranked**, which automatically finds an opponent and starts a countdown before the match.

---

## Technical Description

### Networking

The game uses **Unity Netcode for GameObjects (NGO)** for networking, with the **Unity Multiplayer Services SDK** managing the transport layer. The SDK handles the connection setup internally, including allocating the relay endpoint - none of that is done manually in code.

The architecture is a **listen-server**, meaning one of the two players is the host and also plays the game at the same time. There's no dedicated server. Everything that needs to be authoritative - damage, enemy movement, projectile hits, kill tracking - only runs on the host side, checked with `NetworkManager.IsServer`.

For casual lobbies the host creates a session and gets a join code, the other player types it in and connects. For ranked, both players call `MatchmakeSessionAsync` and the SDK figures out who becomes host. There's a small random delay (0 to 3 seconds) added before searching to avoid two players starting at the exact same time and both trying to host.

One thing worth noting: after matchmaking finishes, NGO doesn't start immediately. The SDK does it asynchronously in the background, so the code has to wait and poll `NetworkManager.Singleton.IsConnectedClient` every 50ms before it can safely send any RPCs.

### Authentication & Login

Players sign in anonymously using `AuthenticationService.Instance.SignInAnonymouslyAsync()` when the game starts. If the player doesn't have a name yet, one gets assigned randomly from a list in `Constants.PLAYER_NAMES`. There's also handling for when the auth token expires - the game re-signs in automatically so the player never notices.

To avoid timing issues where UI buttons could trigger network calls before Unity Services finishes loading, there's a `ReadyTask` in `ServiceInitializer` that everything else waits on before making any service calls.

### Matchmaking

Matchmaking uses `MatchmakeSessionAsync` with a filter of `AvailableSlots >= 1` to only join sessions that still have room. Once both players are connected they each send their ELO and player info to the host via RPC, and the lobby UI updates for both sides.

### Player Data & Persistence

Player ELO is stored in **Unity Leaderboards** under the ID `"elo-ranking"`. There are no local save files: everything lives in Unity's cloud, so it persists even if the server restarts or the player plays on a different machine.

When a player joins a lobby, they first fetch their own leaderboard score, then send it to the host. The host collects both players' data and broadcasts it back to everyone so the lobby screen shows both names and ratings.

### ELO / Ranking System

ELO is calculated on the host at the end of the match inside `SessionConnector.CalculateElo`. The formula takes into account three things: the rating difference between players, who got more kills, and how long the match lasted.

```
eWinner = 1f / (1f + 10f ^ ((loserRating - winnerRating) / 400f))
expMod  = (1f - eWinner) * 2f

killMod   = Lerp(0.65f, 1.35f, winnerKills / totalKills)
baseValue = Round(25f * expMod * killMod)

if duration < 5 min:  timeBonus = Lerp(-12, 0, duration / 5)
if duration >= 5 min: timeBonus = Lerp(0, 12, (duration - 5) / 7)

winnerValue = baseValue + timeBonus
loserValue  = baseValue - timeBonus
```

The key idea with the time bonus is that it's applied positively to both players: so a long match rewards the winner with a bit more and softens the loss for the loser. A short match does the opposite: the winner earns less and the loser loses more, which discourages dying early on purpose(or players who want to abuse some kind of trading rating and scaling infinitely together, this can still happen if both players actually play for a long time during the match but that would be impossible with the game balancing if it existed).

The rating difference matters too: if a lower-rated player wins, they gain a lot more than if a higher-rated player wins the same match.

Ranks go from Recruit (0) up to Commander (3000+), with thresholds at 800, 1000, 1200, 1450, 1750, 2000, 2300, 2600 and 3000. Players start at 1000, which puts them at Corporal.

### How Scenes Work

There are two scenes. The **MainMenu** scene handles everything before the game: signing in, creating or joining lobbies, matchmaking, and showing the leaderboard. The **GameScene** is the actual match.

When the match ends, the host calls `NetworkManager.Singleton.SceneManager.LoadScene("MainMenu")` which loads the menu for both players at the same time. When `MainMenuManager` starts, it checks if there's a leftover session from the previous match and cleans it up before letting the player do anything.

The singletons that need to survive between scenes - `NetworkManager`, `SessionManager`, `SessionConnector`, and `ServiceInitializer` - are all set to `DontDestroyOnLoad`.

---

## Network Architecture Diagram

```
[ Unity Gaming Services ]
  - Authentication (sign-in tokens)
  - Multiplayer Services (sessions, transport, matchmaking)
  - Leaderboards (ELO ranking)

   [ HOST / Listen-Server ]
     runs all game logic (AI, damage, kills, ELO)
     sends state to client via NetworkVariables and RPCs
          ||
          || UDP transport (managed by MultiplayerServices)
          ||
      [ CLIENT ]
        sends input (move, shoot) to host
        receives health, position, level, match result
```

---

## Network Messages

These are all the RPCs used in the project:

`SendToServerPlayerInformationRpc`: sent by each client to the host when entering a lobby, carries the player's name, rating and rank.

`SendLobbyInformationToPlayerRpc`: host sends this back to everyone after collecting both players' info, so the lobby screen updates.

`CasualLobbyIsStartingRpc`: host fires this when pressing Start in a casual lobby, tells all clients to get ready.

`RankedMatchIsStartingRpc`: same thing but for ranked, fired when the countdown hits zero.

`ShootRpc`:client tells the host it wants to shoot; host spawns the projectile and owns it.

`LevelUpRpc`: host broadcasts to all clients when a player levels up, so the particle effect plays on both screens.

`SelectPowerupRpc`: host sends this only to the player who levelled up, to show them the upgrade choice UI.

`UpgradeRpc`: client sends back which upgrade was chosen; host applies it by changing the relevant NetworkVariable.

`SendEndScreenToPlayerRpc`: host sends each player their individual result (win/loss + ELO change) after the match.

`SendDebugToAllRpc`: host broadcasts a debug stats string to all clients at match end.

The main NetworkVariables are `HealthSystem.health`, `Wyzard.cooldown`, `Wyzard.damage`, `Wyzard._level/_xp/_maxXP`, and `Projectile.shotTime/origin`. All are server-authoritative and replicated to clients automatically by the netcode for gameobjects.

---

## Key Scripts

`ServiceInitializer` handles Unity Services startup and authentication, and exposes the `ReadyTask` that everything else waits on.

`SessionManager` wraps the MPS session object and exposes helper methods like `CancelLobbyAsHost()` and `LeaveLobby()`.

`SessionConnector` is the main network script: it's a `NetworkBehaviour` singleton that holds all RPCs, the lobby data, the ELO calculation, and the match end logic.

`MainMenuManager` runs the entire main menu as a state machine, handling all the panel transitions, lobby flows, and post-match cleanup.

`NetworkGameSetup` runs on the host when the game scene loads and spawns both player prefabs.

`GameManager` is a scene singleton that keeps track of how many kills each player has, stored in a `Dictionary<ulong, int>` keyed by client ID.

`Wyzard` is the player script: handles movement, auto-aim, shooting via `ShootRpc`, and the XP/upgrade system.

`Character` is the base class for anything with health. Calls `SessionConnector.PlayerLost` when health hits zero.

`HealthSystem` holds the `NetworkVariable<float>` for health and handles damage on the server side.

`Projectile` moves deterministically using `NetworkVariable` origin and server timestamp, does `LinecastAll` collision on the host, and records kills against `shooterClientId`.

`Enemy` is the AI: it just moves toward the nearest player and attacks on a timer, all server-side controlled.

`Spawner` spawns enemy waves on the host relative to where the players are.

`Constants` holds all the tunable numbers: ELO values, durations, the leaderboard ID, and the list of random player names.

---

## External Packages

- `com.unity.services.multiplayer`: the MPS SDK that handles sessions, relay, and matchmaking in one API
- `com.unity.services.authentication`: anonymous sign-in and token management
- `com.unity.services.leaderboards`: storing and reading ELO scores
- `com.unity.services.analytics`: logging kill and level-up events
- `com.unity.netcode.gameobjects`: the core networking layer (NGO)
- `com.unity.inputsystem`: player input
- `com.unity.multiplayer.playmode`: lets multiple editor instances sign in as different players for testing
- `TextMeshPro`: all UI text

---

## Problems Encountered & Solutions

### 1. Ranked Matchmaking Cancellation - No CancellationToken Support

**Problem:** `MatchmakeSessionAsync` doesn't accept a `CancellationToken`, so there was no clean way to cancel a search once it started.

**Fix:** Used `Task.WhenAny` to race the matchmaking task against a `Task.Delay(Timeout.Infinite, cancellationToken)`. When the player cancels, the delay task finishes and `WhenAny` returns, effectively abandoning the wait. Since the matchmaking might have already created a session server-side by then, a `ContinueWith` checks if it completed after being abandoned and immediately deletes or leaves the session to stop ghost lobbies from accumulating.

---

### 2. Ghost Lobby After Cancel - Wrong Cleanup Method for Host

**Problem:** Calling `LeaveAsync()` as the host doesn't delete the session, it just removes the player. The session stays alive on Unity's servers with no owner and keeps showing up in searches.

**Fix:** Added an `IsHost` check - if the local player is the host, it calls `session.AsHost().DeleteAsync()` instead, which actually removes the session. `LeaveAsync()` is only used when the local player is a regular client.

---

### 3. Host Stuck on "Waiting for Opponent" After Opponent Connects

**Problem:** The `PlayerJoined` event handler was subscribed after `await GetSelfPlayerRating()`. If the opponent connected while that request was in flight, the event fired before the handler existed and was missed permanently.

**Fix:** Moved the subscription to before the `await`. Also added a check after all awaits finish - if `session.Players.Count >= 2` at that point, the opponent already connected and the handler is called manually to force the transition.

---

### 4. LobbyPanel Crash When Receiving RPC Before Panel Is Active

**Problem:** `LobbyPanel.UpdatePlayersInLobby` crashed with `ArgumentOutOfRangeException` because `GetComponentsInChildren<LobbyPlayerListed>()` returned empty. The panel GameObject was still inactive when the first RPC arrived, so Unity skipped inactive children and cached an empty list.

**Fix:** Changed the call to `GetComponentsInChildren<LobbyPlayerListed>(true)` - passing `true` for `includeInactive` so children are found regardless of whether the panel is visible.

---

### 5. RPC Called Before NetworkManager Was Started

**Problem:** Calling `SendToServerPlayerInformationRpc` right after `MatchmakeSessionAsync` completed threw `Rpc methods can only be invoked after starting the NetworkManager!`. The MPS SDK starts NGO asynchronously after the session task resolves, so checking `session.State == SessionState.Connected` wasn't enough - that reflects the lobby layer, not whether NGO is actually ready.

**Fix:** Replaced the state check with a loop that polls `NetworkManager.Singleton.IsConnectedClient` every 50ms for up to 5 seconds. That's the actual flag that means NGO is running and RPCs will work.

---

### 6. Kill Counts Always Zero in ELO Calculation

**Problem:** `CalculateElo` always got `winnerKills = 0, loserKills = 0` because projectiles had no idea who fired them, and nothing was keeping count.

**Fix:** Added a `shooterClientId` field to `Projectile`, assigned from `Wyzard.ShootRpc` using `OwnerClientId` when the projectile spawns. Added a `GameManager` singleton to the game scene with a `Dictionary<ulong, int>` for kill counts. When a projectile's linecast confirms a kill on an `Enemy`, it calls `GameManager.Instance.RecordKill(shooterClientId)`. `PlayerLost` now reads those counts before calling `CalculateElo`.

---

### 7. Players Stay Connected After Returning to Main Menu

**Problem:** After a match ends and both players load back to the main menu, `NetworkManager` and the MPS session were still alive because they're `DontDestroyOnLoad`. Starting a new match from that state would stack connections on top of each other.

**Fix:** Added `CleanupAfterMatchAsync()` called from `MainMenuManager.Start()`. It checks if a session from the previous match is still alive. If so, the host deletes it and shuts down NGO; the client leaves and shuts down NGO. There's a catch block for when the host deletes the session before the client can leave - NGO still gets shut down cleanly in that case.

---

## Bibliography

- [Unity Netcode for GameObjects Documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.12/manual/index.html)
- [Unity Multiplayer Services Documentation](https://docs.unity.com/en-us/mps-sdk)
- [Unity Authentication Documentation](https://docs.unity.com/en-us/authentication)
- [Unity Leaderboards Documentation](https://docs.unity.com/en-us/leaderboards)
