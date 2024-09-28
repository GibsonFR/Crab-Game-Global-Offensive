namespace GibsonCrabGameGlobalOffensive
{
    public class Main : MonoBehaviour
    {
        public Text text;

        // Timers
        private float elapsedClientUpdate, elapsedConfigUpdate, elapsedMenuUpdate, elapsedLoadingState;
        private float loadingState;

        // Flags
        private bool sendLoading, spectatorsAdded;

        private readonly List<ulong> playersToRemove = [];

        void Awake()
        {
            InitializeGameVariables();
            LoadLastRoundMessages();
        }

        void Update()
        {
            if (!spectatorsAdded) BindSpectatorsToPlayerList();
            UpdateElapsedTimers();
            ManageLoadingAnimation();
            HandleMenu();
            UpdateClientData();
        }

        // Initialize some game variables
        private void InitializeGameVariables()
        {
            mapId = GameData.GetMapId();
            modeId = GameData.GetModeId();
            subMenuSelector = -1;
            playerIndex = 0;
            onButton = false;
            startRound = false;
        }

        // Bind spectators to the players list so they can use commands
        private void BindSpectatorsToPlayerList()
        {
            spectatorsAdded = true;
            foreach (var spectator in GameManager.Instance.spectators)
            {
                ulong steamId = spectator.value.steamProfile.m_SteamID;
                if (playersList.ContainsKey(steamId))
                {
                    playersList[steamId] = spectator.value;
                }
            }
        }

        // Update client data
        private void UpdateClientData()
        {
            if (elapsedClientUpdate < 1f) return;

            elapsedClientUpdate = 0f;

            clientBody = ClientData.GetClientBody();
            if (clientBody == null) return;

            clientObject = ClientData.GetClientObject();
        }

        // Load last round messages into the chat system
        private void LoadLastRoundMessages()
        {
            Utility.SendServerMessage("---- NEW ROUND ----");

            for (int i = 0; i < 9; i++)
            {
                messageSenderId = (messageSenderId < 100) ? messageSenderId + 1 : 2;

                string message = messagesList[8 - i];
                if (message != null)
                {
                    string cleanMessage = message.StartsWith("#srv#") ? message.Replace("#srv#", "") : message;
                    ServerSend.SendChatMessage((ulong)(message.StartsWith("#srv#") ? 1 : messageSenderId), cleanMessage);
                }
                else
                {
                    ServerSend.SendChatMessage((ulong)messageSenderId, ":");
                }
            }
        }

        // Handle menu display and updates
        private void HandleMenu()
        {
            if (Input.GetKeyDown(menuKey)) ToggleMenu();
            if (elapsedConfigUpdate > 3f) UpdateConfigFiles();
            if (elapsedMenuUpdate >= 0.05f) UpdateMenuDisplay();
        }

        // Update configuration files
        private void UpdateConfigFiles()
        {
            Utility.ReadConfigFile();
            Utility.ReadBanned(playersBannedFilePath);
            Utility.ReadPerms(permsFilePath);
            elapsedConfigUpdate = 0f;
        }

        // Update menu display based on the current state
        private void UpdateMenuDisplay()
        {
            text.text = menuTrigger ? MenuFunctions.FormatLayout() : "";
            lastOtherPlayerPosition = MultiPlayersData.GetOtherPlayerPosition();
            elapsedMenuUpdate = 0f;
        }

        // Toggle the menu on/off
        private void ToggleMenu()
        {
            Utility.PlayMenuSound();
            menuTrigger = !menuTrigger;

            // Use strings from GameMessages class
            string menuMessage = menuTrigger ? MainMessages.MENU_ON : MainMessages.MENU_OFF;
            Utility.ForceMessage(menuMessage);

            if (menuTrigger)
            {
                Utility.ForceMessage(MainMessages.NAVIGATION);
                Utility.ForceMessage(MainMessages.SELECTION);
                Utility.ForceMessage(MainMessages.SUBMENU_EXIT);
            }
        }

        // Update timers for various game functions
        private void UpdateElapsedTimers()
        {
            float deltaTime = Time.deltaTime;
            elapsedClientUpdate += deltaTime;
            elapsedConfigUpdate += deltaTime;
            elapsedMenuUpdate += deltaTime;
            elapsedLoadingState += deltaTime;
        }

        // Manage loading animations for players
        private void ManageLoadingAnimation()
        {
            if (elapsedLoadingState > 0.1f)
            {
                loadingState = (loadingState + 1) % 10;
                sendLoading = true;
                elapsedLoadingState = 0;
            }

            ProcessLoadingPlayers();
            playersToRemove.Clear();
            sendLoading = false;
        }

        // Process loading players and update their loading status
        private void ProcessLoadingPlayers()
        {
            foreach (var player in loadingPlayers)
            {
                if (HasPlayerCompletedLoading(player.Value))
                {
                    SendLoadingCompletionMessages(player.Key);
                    playersToRemove.Add(player.Key);
                }
                else if (sendLoading)
                {
                    string loadingStateMessage = GetLoadingStateMessage(loadingState);
                    SendLoadingMessages(player.Key, loadingStateMessage);
                }
            }

            foreach (var steamId in playersToRemove)
            {
                loadingPlayers.Remove(steamId);
            }
        }

        // Check if a player has completed loading
        private bool HasPlayerCompletedLoading(DateTime playerStartTime)
        {
            return (DateTime.Now - playerStartTime).TotalMilliseconds > 3000;
        }

        // Get a formatted loading state message based on the current state
        private string GetLoadingStateMessage(float state)
        {
            return state switch
            {
                0 => MainMessages.LOADING_STEP_0,
                1 => MainMessages.LOADING_STEP_1,
                2 => MainMessages.LOADING_STEP_2,
                3 => MainMessages.LOADING_STEP_3,
                4 => MainMessages.LOADING_STEP_4,
                5 => MainMessages.LOADING_STEP_5,
                6 => MainMessages.LOADING_STEP_6,
                7 => MainMessages.LOADING_STEP_7,
                8 => MainMessages.LOADING_STEP_8,
                9 => MainMessages.LOADING_STEP_9,
                _ => MainMessages.LOADING_STEP_0 // Default case if out of bounds
            };
        }

        // Send loading messages to the player
        private void SendLoadingMessages(ulong steamId, string loadingStateMessage)
        {
            for (int i = 0; i < 4; i++) Utility.SendPrivateMessageWithWaterMark(steamId, "#");
            Utility.SendPrivateMessageWithWaterMark(steamId, loadingStateMessage);
            for (int i = 0; i < 4; i++) Utility.SendPrivateMessageWithWaterMark(steamId, "#");
        }

        // Send loading completion messages to the player once loading is done
        private void SendLoadingCompletionMessages(ulong steamId)
        {
            PlayerManager playerManager = GameData.GetPlayer(steamId.ToString());
            Utility.PlayerData playerData = Utility.ReadPlayerData($"{playersDataFolderPath}{steamId}.txt");

            var messages = new List<string>
                {
                    "LOADING # # # # COMPLETE",
                    $"Welcome to CGGO, {playerManager.username}!",
                    $"LeaderboardRank: {Utility.GetPlayerPlacement(playersListFilePath, steamId.ToString())}/{Utility.GetLineCount(playersListFilePath)}",
                    $"Rank: {playerData.Rank}, Elo: {playerData.Elo:F1}, High Elo: {playerData.HighestElo:F1}",
                    $"Win: {playerData.Win}, Kills: {playerData.Kills}, Death: {playerData.Death}, K/D: {(playerData.Death == 0 ? "1" : (playerData.Kills / (float)playerData.Death).ToString("F1"))}",
                    $"GamePlayed: {playerData.GamePlayed}, Level: {playerData.Level}",
                    $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerData.TotalTimePlayed)}",
                    $"totRound: {playerData.RoundPlayed}, SteamId: {steamId}"
                };

            foreach (var message in messages)
            {
                Utility.SendPrivateMessageWithWaterMark(steamId, message);
            }
        }
    }

    public class MainPatchs
    {
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong param_0, string param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return true;
            if (param_0 < 101) return true;

            if (param_0 != 1 && !param_1.StartsWith("!") && !param_1.StartsWith("/"))
            {
                Utility.ReadWordsFilter(wordsFilterFilePath);

                string rank = Utility.GetValue(param_0.ToString(), "rank");
                string username = Utility.GetValue(param_0.ToString(), "username");

                string[] messageWord = param_1.Split(' ');

                foreach (var word in messageWord)
                {
                    if (wordsFilterList.Contains(word))
                    {
                        Utility.processNewMessage(messagesList, $"{username} -> *biip*");
                        return false;
                    }
                }
                if (!muteChat)
                {
                    if (displayRankInChat)
                        Utility.processNewMessage(messagesList, $"[{rank}] {username} -> {param_1}");
                    else
                        Utility.processNewMessage(messagesList, $"{username} -> {param_1}");

                    return false;
                }
                else return false;
            }
            else return false;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPostfix]
        public static void ServerSendSendChatMessagePost(ulong param_0, string param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return;
            if (param_0 < 101) return;
            if (param_1.StartsWith("|")) return;

            if (!param_1.StartsWith("!") && !param_1.StartsWith("/"))
            {
                // Send predefined messages
                for (int i = 0; i < 9; i++)
                {
                    if (messageSenderId < 100)
                        messageSenderId++;
                    else
                        messageSenderId = 2;

                    if (messagesList[8 - i] != null && messagesList[8 - i].StartsWith("#srv#"))
                    {
                        ServerSend.SendChatMessage((ulong)1, messagesList[8 - i].Replace("#srv#", ""));
                    }
                    else
                    {
                        if (messagesList[8 - i] != null)
                            ServerSend.SendChatMessage((ulong)messageSenderId, messagesList[8 - i]);
                        else
                            ServerSend.SendChatMessage((ulong)messageSenderId, ":");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Update))]
        [HarmonyPostfix]
        internal static void GameModeUpdatePost(GameMode __instance)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !isCGGOActive || startRound) return;

            double timer = GameData.GetCurrentGameTimer();

            if (GameData.GetGameState() == "Freeze")
            {
                if (mapId == 30 || mapId == 31)
                {
                    if (timer == 7)
                    {
                        SetStartRound();
                        LoadRandomOrDefaultMap();
                    }
                }
                else if (timer == 14)
                {
                    SetStartRound();
                }
            }

            void SetStartRound()
            {
                startRound = true;
            }

            void LoadRandomOrDefaultMap()
            {
                if (!tuto)
                {
                    System.Random random = new();
                    List<int> numbers = [0, 2, 7, 20];
                    int randomElement = numbers[random.Next(0, numbers.Count)];
                    ServerSend.LoadMap(randomElement, 9);
                }
                else ServerSend.LoadMap(0, 9);
            }
        }



        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.GameRequestToSpawn))]
        [HarmonyPrefix]
        public static void ServerHandleGameRequestToSpawnPre(ulong __0)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return;

            var client = LobbyManager.Instance.GetClient(__0);

            if (isCGGOActive && CGGOTeamSet == true)
            {
                if (CGGOPlayer.GetCGGOPlayer(__0) == null) client.field_Public_Boolean_0 = false;
                else client.field_Public_Boolean_0 = true;
            }

            //Always spawn host if CGGO is active
            if (__0 == clientId && isCGGOActive)
            {
                client.field_Public_Boolean_0 = true;
            }

        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.AddPlayerToLobby))]
        [HarmonyPostfix]
        public static void OnLobbyManagerAddPlayerToLobbyPost(CSteamID __0)
        {
            Utility.CreatePlayerFile(__0.ToString());

            //Update player Username on file.
            Utility.ModifValue(__0.ToString(), "username", SteamFriends.GetFriendPersonaName((CSteamID)__0).Replace(":", " "));

            newPlayerToSpawn.Add((ulong)__0, DateTime.Now);

            newPlayers.Add((ulong)__0);

            if (SteamManager.Instance.IsLobbyOwner())
            {
                if (bannedPlayers.ContainsKey(__0.ToString())) LobbyManager.Instance.KickPlayer((ulong)__0);
                return;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnPlayer))]
        [HarmonyPostfix]
        public static void OnGameManagerSpawnPlayerPost(ulong __0)
        {
            if (playersList.ContainsKey(__0))
                playersList[__0] = GameData.GetPlayerFirst(__0.ToString());

            if (newPlayers.Contains(__0))
            {
                newPlayers.Remove(__0);
                loadingPlayers.Add(__0, DateTime.Now);
                playersList.Add(__0, GameData.GetPlayerFirst(__0.ToString()));
            }
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            // Safely remove the player from the various dictionaries and lists
            if (playersList.ContainsKey(steamId)) playersList.Remove(steamId);

            if (loadingPlayers.ContainsKey(steamId)) loadingPlayers.Remove(steamId);

            if (newPlayerToSpawn.ContainsKey(steamId)) newPlayerToSpawn.Remove(steamId);

            if (newPlayers.Contains(steamId)) newPlayers.Remove(steamId);

            var cggoPlayer = CGGOPlayer.GetCGGOPlayer(steamId);
            if (cggoPlayer != null)
            {
                if (isCGGORanked) EloFunctions.UpdateEloCGGO(cggoPlayer, totalCGGOPlayer, totalCGGOGameExpectative, cggoPlayersList.Count(), averageCGGOElo, 50, -1);

                cggoPlayersList.Remove(cggoPlayer);
            }
        }

        // Set ClientId
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdatePost(SteamManager __instance)
        {
            clientId = (ulong)__instance.field_Private_CSteamID_0;
        }

        // Setup new lobby
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
        [HarmonyPrefix]
        public static void OnSteamManagerCreateLobbyPre()
        {
            newPlayers.Clear();
            playersList.Clear();
            loadingPlayers.Clear();
            newPlayers.Add(clientId);
        }

        // Damageable AK/Shotgun by lammas123 modified
        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.Awake))]
        [HarmonyPostfix]
        internal static void PostItemManagerAwake()
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return;

            ItemManager.idToItem[0].itemName = "Vandal";
            ItemManager.idToItem[3].itemName = "Shorty";
        }
        // Damageable AK/Shotgun by lammas123 modified

        // Floating player patch by lammas123 modified
        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.PlayerPosition))]
        [HarmonyPostfix]
        internal static void PostServerHandlePlayerPosition(ulong param_0, Packet param_1)
        {
            if (publicTutoPhase) return;
            if (SteamManager.Instance.IsLobbyOwner())
                ServerSend.PlayerPosition(param_0, new(BitConverter.ToSingle(param_1.field_Private_ArrayOf_Byte_0, 8), BitConverter.ToSingle(param_1.field_Private_ArrayOf_Byte_0, 12), BitConverter.ToSingle(param_1.field_Private_ArrayOf_Byte_0, 16)));
        }

        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.PlayerRotation))]
        [HarmonyPostfix]
        internal static void PostServerHandlePlayerRotation(ulong param_0, Packet param_1)
        {
            if (publicTutoPhase) return;
            if (SteamManager.Instance.IsLobbyOwner())
                ServerSend.PlayerRotation(param_0, BitConverter.ToSingle(param_1.field_Private_ArrayOf_Byte_0, 8), BitConverter.ToSingle(param_1.field_Private_ArrayOf_Byte_0, 12));
        }
        // Floating player patch by lammas123 modified
    }
}
