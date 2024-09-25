//Using (ici on importe des bibliothèques utiles)
global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using SteamworksNative;
global using System;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using UnhollowerRuntimeLib;
global using UnityEngine;
global using UnityEngine.UI;
global using Il2CppSystem.Text; 
global using static GibsonCrabGameGlobalOffensive.Plugin.CGGO;

namespace GibsonCrabGameGlobalOffensive
{
    [BepInPlugin("GibsonCrabGameGlobalOffensive", "GibsonCrabGameGlobalOffensive", "2.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Init>();
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<FlungDetector>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();
            ClassInjector.RegisterTypeInIl2Cpp<Selector>();
            ClassInjector.RegisterTypeInIl2Cpp<SpeakDuringAllGame>();
            ClassInjector.RegisterTypeInIl2Cpp<DeleteItem>();
            ClassInjector.RegisterTypeInIl2Cpp<CGGO>();
            

            Harmony.CreateAndPatchAll(typeof(Plugin));

            Utility.CreateFolder(Variables.mainFolderPath, Variables.logFilePath);
            Utility.CreateFolder(Variables.playersDataFolderPath, Variables.logFilePath);

            Utility.CreateFile(Variables.configFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.playersBannedFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.playersListFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.playersReportFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.wordsFilterFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.permsFilePath, Variables.logFilePath);
            Utility.CreateFile(Variables.logFilePath, Variables.logFilePath);

            Utility.ResetFile(Variables.logFilePath, Variables.logFilePath);
            Utility.SetConfigFile(Variables.configFilePath);



            MenuFunctions.CheckMenuFileExists();
            MenuFunctions.LoadMenuLayout();
            MenuFunctions.RegisterDefaultCallbacks();

            for (int i = 0; i < 10; i++)
            {
                Utility.processNewMessage(Variables.messagesList, "GibsonChatSystem Loaded!");
            }

            Log.LogInfo("Mod created by Gibson, discord: gib_son, github: GibsonFR");
        }

        private void Awake()
        {
            // Vérifiez si la classe cible existe avant d'appliquer le patch
            Type targetType = Type.GetType("GameModePublicDi2UIInidnTInUnique");
            if (targetType != null)
            {
                // Appliquez le patch si la classe existe
                Harmony harmony = new Harmony("com.gibson.cggo");
                harmony.PatchAll();
            }
        }

        public class Init : MonoBehaviour
        {
            private bool isInitialized;

            void Update()
            {
                if (isInitialized) return;

                isInitialized = true;

                InitializeGameVariables();

                BindSpectatorsToPlayerList();
            }

            // Initializes the basic game variables
            private void InitializeGameVariables()
            {
                Variables.mapId = GameData.GetMapId();
                Variables.modeId = GameData.GetModeId();
                Variables.subMenuSelector = -1;
                Variables.playerIndex = 0;
                Variables.onButton = false;
                Variables.startRound = false;
            }

            // Binds spectators to the playersList so they can use commands
            private void BindSpectatorsToPlayerList()
            {
                foreach (var spectator in GameManager.Instance.spectators)
                {
                    ulong steamId = spectator.value.steamProfile.m_SteamID;
                    if (Variables.playersList.ContainsKey(steamId))
                    {
                        Variables.playersList[steamId] = spectator.value;
                    }
                }
            }
        }

        public class Basics : MonoBehaviour
        {
            public Text text;

            // Timers
            private float elapsedClientUpdate, elapsedConfigUpdate, elapsedMenuUpdate, elapsedLoadingState;
            private float loadingState;

            // Flags
            private bool isChatInit;
            private bool sendLoading;

            // Constants
            private const string MENU_ON_MSG = "■<color=orange>MenuManager <color=blue>ON</color></color>■";
            private const string MENU_OFF_MSG = "■<color=orange>MenuManager <color=red>OFF</color></color>■";
            private const string NAVIGATION_MSG = "■<color=orange>navigate the menu using the scrollWheel</color>■";
            private const string SELECTION_MSG = "■<color=orange>right click to select</color>■";
            private const string SUBMENU_MSG = "■<color=orange>press scrollWheel to exit submenu</color>■";

            private readonly List<ulong> playersToRemove = new();

            void Update()
            {
                // Update the elapsed timers
                UpdateElapsedTimers();

                // Manage various components
                ManageLoadingAnimation();
                InitializeChatSystem();
                UpdateGameState();
                HandleMenu();
                UpdateClientData();
            }

            private void UpdateClientData()
            {
                if (elapsedClientUpdate < 1f) return;

                elapsedClientUpdate = 0f;

                if ((Variables.clientBody = ClientData.GetClientBody()) == null) return;

                Variables.clientObject = ClientData.GetClientObject();
                Variables.clientMovement = ClientData.GetClientMovement();
                Variables.clientInventory = ClientData.GetClientInventory();
                Variables.clientStatus = PlayerStatus.Instance;
            }

            private void UpdateGameState()
            {
                Variables.lastGameState = Variables.gameState;
                Variables.gameState = GameData.GetGameState();
            }

            private void InitializeChatSystem()
            {
                if (isChatInit) return;

                Utility.SendServerMessage("---- NEW ROUND ----");

                for (int i = 0; i < 9; i++)
                {
                    Variables.messageSenderId = (Variables.messageSenderId < 100) ? Variables.messageSenderId + 1 : 2;

                    string message = Variables.messagesList[8 - i];
                    if (message != null)
                    {
                        string cleanMessage = message.StartsWith("#srv#") ? message.Replace("#srv#", "") : message;
                        ServerSend.SendChatMessage((ulong)(message.StartsWith("#srv#") ? 1 : Variables.messageSenderId), cleanMessage);
                    }
                    else
                    {
                        ServerSend.SendChatMessage((ulong)Variables.messageSenderId, ":");
                    }
                }

                isChatInit = true;
            }

            private void HandleMenu()
            {
                if (Input.GetKeyDown(Variables.menuKey))
                {
                    ToggleMenu();
                }

                if (elapsedConfigUpdate > 3f)
                {
                    UpdateConfigFiles();
                }

                if (elapsedMenuUpdate >= 0.05f)
                {
                    UpdateMenuDisplay();
                }
            }

            private void UpdateConfigFiles()
            {
                Utility.ReadConfigFile();
                Utility.ReadBanned(Variables.playersBannedFilePath);
                Utility.ReadPerms(Variables.permsFilePath);
                elapsedConfigUpdate = 0f;
            }

            private void UpdateMenuDisplay()
            {
                text.text = Variables.menuTrigger ? MenuFunctions.FormatLayout() : "";
                Variables.lastOtherPlayerPosition = MultiPlayersData.GetOtherPlayerPosition();
                elapsedMenuUpdate = 0f;
            }

            private void ToggleMenu()
            {
                Utility.PlayMenuSound();
                Variables.menuTrigger = !Variables.menuTrigger;
                Utility.ForceMessage(Variables.menuTrigger ? MENU_ON_MSG : MENU_OFF_MSG);

                if (Variables.menuTrigger)
                {
                    Utility.ForceMessage(NAVIGATION_MSG);
                    Utility.ForceMessage(SELECTION_MSG);
                    Utility.ForceMessage(SUBMENU_MSG);
                }
            }

            private void UpdateElapsedTimers()
            {
                float deltaTime = Time.deltaTime;
                elapsedClientUpdate += deltaTime;
                elapsedConfigUpdate += deltaTime;
                elapsedMenuUpdate += deltaTime;
                elapsedLoadingState += deltaTime;
            }

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

            private void ProcessLoadingPlayers()
            {
                foreach (var player in Variables.loadingPlayers)
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
                    Variables.loadingPlayers.Remove(steamId);
                }
            }

            private bool HasPlayerCompletedLoading(DateTime playerStartTime)
            {
                return (DateTime.Now - playerStartTime).TotalMilliseconds > 3000;
            }

            private string GetLoadingStateMessage(float state)
            {
                return state switch
                {
                    0 => "L O A D I N G[* - - - - - - - - -]L O A D I N G",
                    1 => "L O A D I N G[- * - - - - - - - -]L O A D I N G",
                    2 => "L O A D I N G[- - * - - - - - - -]L O A D I N G",
                    3 => "L O A D I N G[- - - * - - - - - -]L O A D I N G",
                    4 => "L O A D I N G[- - - - * - - - - -]L O A D I N G",
                    5 => "L O A D I N G[- - - - - * - - - -]L O A D I N G",
                    6 => "L O A D I N G[- - - - - - * - - -]L O A D I N G",
                    7 => "L O A D I N G[- - - - - - - * - -]L O A D I N G",
                    8 => "L O A D I N G[- - - - - - - - * -]L O A D I N G",
                    9 => "L O A D I N G[- - - - - - - - - *]L O A D I N G",
                    _ => "L O A D I N G[- - - - - - - - - -]L O A D I N G"
                };
            }

            private void SendLoadingMessages(ulong steamId, string loadingStateMessage)
            {
                for (int i = 0; i < 4; i++) Utility.SendPrivateMessageWithWaterMark(steamId, "#");
                Utility.SendPrivateMessageWithWaterMark(steamId, loadingStateMessage);
                for (int i = 0; i < 4; i++) Utility.SendPrivateMessageWithWaterMark(steamId, "#");
            }

            private void SendLoadingCompletionMessages(ulong steamId)
            {
                PlayerManager playerManager = GameData.GetPlayer(steamId.ToString());
                Utility.PlayerData playerData = Utility.ReadPlayerData($"{Variables.playersDataFolderPath}{steamId}.txt");

                var messages = new List<string>
                {
                    "LOADING # # # # COMPLETE",
                    $"Welcome to CGGO, {playerManager.username}!",
                    $"LeaderboardRank: {Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId.ToString())}/{Utility.GetLineCount(Variables.playersListFilePath)}",
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



        public class CGGOMessages
        {
            public static string[] GetTutorialMessageInSpecificLanguage(string language)
            {
                switch (language)
                {
                    case "RU":
                        return
                            ["Dobro pozalovat v CGGO",
                            "Pobedit pervym nabrav 6 ockov",
                            "Kupit oruzie primer lenty /vandal",
                            "Pomestite bombu v molocnuju zonu",
                            "Ctoby zalozit bombu, broste bombu",
                            "Obezvredit bombu do togo, kak ona vzorvetsja",
                            "Nazmite 4 i prisjadte na kortocki"];
                    case "DE":
                        return
                            ["Willkommen bei CGGO, custom mod CG",
                            "Die Ersten mit 6 Punkten gewinnen",
                            "Kaufe Waffen,Beispiel tape /vandal",
                            "Lege die Bombe in der Milkzone",
                            "Um die Bombe zu legen, lass die Bomb fallen",
                            "Entscharfe die Bombe, bevor sie explodiert",
                            "Drucke 4 und hocke dich darauf"];

                    case "ES":
                        return
                            ["Bienvenido a CGGO, custom mod CG",
                            "El primero en llegar a 6 puntos gana",
                            "Compra armas, ejemplo tipo /vandal",
                            "Coloca la bomba en la Milkzone",
                            "Para poner la bomba, tira la bomba",
                            "Desactiva la bomba antes de que explote",
                            "pulsa 4 y ponte en cuclillas sobre ella"];
                    case "FR":
                        return
                            ["Bienvenue dans CGGO, custom mod CG",
                            "Les premiers a 6 points gagne",
                            "Achetes des armes,exemple tape /vandal",
                            "Poses la bombe dans les Milkzone",
                            "Pour poser la bombe, laches la",
                            "Desamorcer la bombe avant l'explosion:",
                            "appuyes sur 4 et accroupis-toi dessus"];
                    case "EN":
                        return 
                            ["Welcome in CGGO, custom mod CG", 
                            "First team to 6 points win", 
                            "Buy weapon in chat,ex: type /vandal", 
                            "Plant the bomb on Milkzone", 
                            "To plant drop the bomb, press Q", 
                            "Defuse the bomb before it explode", 
                            "To defuse press 4 and crouch on bomb"];
                    default:
                        return
                            ["Welcome in CGGO, custom mod CG",
                            "First team to 6 points win",
                            "Buy weapon in chat,ex: type /vandal",
                            "Plant the bomb on Milkzone",
                            "To plant drop the bomb, press Q",
                            "Defuse the bomb before it explode",
                            "To defuse press 4 and crouch on bomb"];


                }
            }

            public static string[] GetBuyPhaseMessageInSpecificLanguage(CGGOPlayer player, int totalTeamScore)
            {
                switch (player.Language)
                {
                    case "RU":
                        return
                            ["---- E T A P ---- P O K U P K I ",
                                $"KRUGLYJ {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                                $"Balans vasego sceta : {player.Balance}$",
                                "Ctoby kupit, naberite v cate:",
                                " /classic (900$) - /shield25 (400$)",
                                " /shorty (1850$) - /shield50 (1000$)",
                                " /vandal (2900$) - /revolver (4700$)",
                                " /katana (3200$)"];
                    case "DE":
                        return
                            ["---- K A U F ---- P H A S E ",
                                $"RUNDE {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                                $"Dein saldo : {player.Balance}$",
                                "Um etwas zu kaufen,tippe auf den chat:",
                                " /classic (900$) - /shield25 (400$)",
                                " /shorty (1850$) - /shield50 (1000$)",
                                " /vandal (2900$) - /revolver (4700$)",
                                " /katana (3200$)"];
                    case "ES":
                        return
                            ["---- C O M P R A R ---- F A S E ",
                                $"RONDA {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                                $"Tu saldo : {player.Balance}$",
                                "Para comprar, escribe en el chat:",
                                " /classic (900$) - /shield25 (400$)",
                                " /shorty (1850$) - /shield50 (1000$)",
                                " /vandal (2900$) - /revolver (4700$)",
                                " /katana (3200$)"];
                    case "FR":
                        return
                            ["---- P H A S E ---- D ' A C H A T ",
                            $"MANCHE {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                            $"Ton solde : {player.Balance}$",
                            "Pour acheter, tape dans le chat:",
                            " /classic (900$) - /shield25 (400$)",
                            " /shorty (1850$) - /shield50 (1000$)",
                            " /vandal (2900$) - /revolver (4700$)",
                            " /katana (3200$)"];
                    case "EN": 
                        return 
                            ["---- B U Y ---- P H A S E ",
                            $"ROUND {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                            $"Your Money : {player.Balance}$",
                            "To buy, type in chat:",
                            " /classic (900$) - /shield25 (400$)",
                            " /shorty (1850$) - /shield50 (1000$)",
                            " /vandal (2900$) - /revolver (4700$)",
                            " /katana (3200$)"];
                    default: 
                        return 
                            ["---- B U Y ---- P H A S E ",
                            $"ROUND {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                            $"Your $Money$ : {player.Balance}$",
                            "To buy a weapon, type in chat:",
                            " /classic (900$) - /shield25 (400$)",
                            " /shorty (1850$) - /shield50 (1000$)",
                            " /vandal (2900$) - /revolver (4700$)",
                            " /katana (3200$)"];
                }
            }

            public static string[] GetNewPlayersBuyPhaseMessageInSpecificLanguage(string language, int totalTeamScore,int totalBalanceTeamAttackers,int totalBalanceTeamDefenders)
            {
                switch (language)
                {
                    case "EN":
                        return
                            [
                                "#",
                                " ---- B U Y ---- ---- P H A S E ---- ",
                                $"ROUND [{totalTeamScore + 1}] | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                                $"ATTACKERS BALANCE[{Variables.cggoScore[0]}] : {totalBalanceTeamAttackers}$",
                                $"DEFENDERS BALANCE[{Variables.cggoScore[1]}] : {totalBalanceTeamDefenders}$",
                                "____ C H A T ____ ____ C H A T ____"

                            ];

                    default:
                        return
                            [
                                "#",
                                " ---- B U Y ---- ---- P H A S E ---- ",
                                $"ROUND [{totalTeamScore + 1}] | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}",
                                $"ATTACKERS BALANCE[{Variables.cggoScore[0]}] : {totalBalanceTeamAttackers}$",
                                $"DEFENDERS BALANCE[{Variables.cggoScore[1]}] : {totalBalanceTeamDefenders}$",
                                "____ C H A T ____ ____ C H A T ____"

                            ];
                }
            }

        }

        public class CGGO : MonoBehaviour
        {
            // string
            string cheaterUsername = "", currentPhase = "init";

            // string[]
            string[] tutorialCurrentMessages = [];

            // int
            int respawnIndexTuto = 0, winnerTeam, cinematicDummy = 0, CheaterDetectedMessageState = 0, BombDummy, bombId, buyPhaseMessageState = 0, plantingPhaseMessageState = 0, defusePhaseMessageState = 0, totalTeamScore = 0, tutorialCurrentMessageIndex = 0, siteId = -1;
            
            // public int
            public static int publicBombId, publicGrenadeId;
            
            // ulong
            ulong bombSpawnedId = 0, cheaterSteamId = 0;
            
            // float
            float elapsedGameEnded, elaspedRespawnTuto, elapsedTuto, elapsedTutoMessage, elapsedCheaterMessage, elapsedShield, elapsedRemoveBombDummy, elapsedMilkZone, elapsedInit, elapsedRoundEnded, elapsedBuyPhase, elapsedPlantingPhase, elapsedDefusingTimer, elapsedBuyMessage, elapsedDefusingPhase, elapsedPlantingMessage, elapsedDefusingMessage;     
            
            // bool
            bool freezeTimerInit, shouldRespawnTuto, nextPlayerTuto = true, loadLobby, rankPlayers, nextRoundLoaded, tutoStep0, tutoStep3, weaponTutoGiven, bombTutoTaken, bombTutoPlanted, bombTutoGoDefuse, bonusGiven, cheaterDetected, defusersDropped, roundWinMessageSent, milkZoneA, milkZoneB, lastRoundWeaponGiven, attackersWon, defendersWon, killBombSpawner, buyPhaseStart;
            
            // public bool
            public static bool publicEndPhase, publicTutoPhase, publicBuyPhase, publicAllAttackersDead, publicAllDefendersDead;
            
            // PlayerManager
            PlayerManager BombHandler = null;
            
            // public List<int>
            public static List<int> publicClassicList = [], publicShortyList = [], publicVandalList = [], publicKatanaList = [], publicRevolverList = [];
            
            // public List<CGGOPlayer>
            public static List<CGGOPlayer> publicAttackersList = [], publicDefendersList = [];
            
            // Vector3
            Vector3 respawnPositionTuto = Vector3.zero, bombPos = Vector3.zero, lastBombHandlerPosition = Vector3.zero;
            
            // Map
            Map currentMap = null;
            
            // GameObject
            GameObject bomb = null;
            
            // GameObject[]
            GameObject[] defusers = [];
            
            // ColorManager
            GameModeTileDrive colorManager = null;

            void Update()
            {
                if ((!SteamManager.Instance.IsLobbyOwner() || !IsCGGOModeOn()) || !IsPlayableMap()) return;

                SetFreezeTimerOnInit();
                SetPublicPhase();
                FindGameObjects();
                ManageElapsed(Time.deltaTime);
                GetAndSetTeamList();
                SendShieldValue();
                SendMessages();
                CheckIntegrityOfCGGOPlayers();
                CheckForCheaters();

                if (IsCurrentPhaseGameEnded())
                {
                    ManageGameEndedPhase();
                    return;
                }


                if (IsCurrentPhaseEnded())
                {
                    ManageEndingPhase();
                    return;
                }

                if (IsCurrentPhaseInit() && CanRoundStart())
                {
                    ManageInitPhase();
                    return;
                }

                if (IsCurrentPhaseTuto())
                {
                    ManageTutoPhase();
                    return;
                }

                if (IsCurrentPhaseBuying())
                {
                    ManageBuyingPhase();
                    return;
                }

                if (IsCurrentPhasePlanting())
                {
                    if (IsBombOnAorB()) ManagePlantingBomb();
                }

                if (IsCurrentPhaseDefusing())
                {
                    ManageDefusing();
                }


                if (IsBombDefused())
                {
                    ManageBombDefused();
                    return;
                }

                if (AreAttackersDeadDuringPlanting())
                {
                    ManageAllAttackersDeadDuringPlanting();
                    return;
                }

                if (publicAllDefendersDead)
                {
                    ManageAllDefendersDead();
                    return;
                }

                if (IsPlantingPhaseOver())
                {
                    ManagePlantingPhaseOver();
                    return;
                }

                if (IsDefusingPhaseOver())
                {
                    ManageBombNotDefusedInTime();
                    return;
                }
            }
            void SetFreezeTimerOnInit()
            {
                if (freezeTimerInit) return;
                else
                {
                    colorManager = GameManager.Instance.GetComponent<GameModeTileDrive>();
                    if (colorManager != null) colorManager.freezeTimer.field_Private_Single_0 = 19f;
                    else return;
                    freezeTimerInit = true;
                }
            }
            void ManageGameEndedPhase()
            {
                if (cheaterDetected) SendCheaterAnimation();

                if (elapsedGameEnded > 5f && !loadLobby)
                {
                    loadLobby = true;
                    ResetGameVariables();
                    LoadLobby();
                }
            }
            void ManageDefusing()
            {
                if (defusers == null)
                {
                    elapsedDefusingTimer = 0f;
                    return;
                }

                PlayerManager closestPlayer = GetClosestDefuser();
                if (closestPlayer == null) return;
                
                else if (CGGOPlayer.GetCGGOPlayer(closestPlayer.steamProfile.m_SteamID).Team != 1) return;
                

                

                float distanceToBomb = Vector3.Distance(closestPlayer.gameObject.transform.position, bombPos);
                if (distanceToBomb < 2.5f && closestPlayer.field_Private_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.field_Private_Boolean_0)
                {
                    ManageDefusingElapsed(Time.deltaTime);
                    SendDefusingMessage();
                }
                else
                {
                    elapsedDefusingTimer = 0f;
                }
                
            }

            PlayerManager GetClosestDefuser()
            {
                PlayerManager closestPlayer = null;
                float closestDistance = float.MaxValue;

                foreach (var defuser in defusers)
                {
                    if (defuser.transform.parent?.parent?.parent == null)
                    {
                        continue;
                    }

                    var playerDefuser = defuser.transform.parent.parent.parent.GetComponent<PlayerManager>();
                    float distanceToBomb = Vector3.Distance(playerDefuser.gameObject.transform.position, bombPos);

                    if (distanceToBomb < closestDistance)
                    {
                        closestPlayer = playerDefuser;
                        closestDistance = distanceToBomb;
                    }
                }

                return closestPlayer;
            }

            void ManagePlantingBomb()
            {
                if (BombHandler != null)
                {
                    SetBombPosition();
                    SpawnPlantedBomb(siteId);
                    SetCurrentPhaseDefusing();
                    GivePlantingBonus();

                }
            }

            void GivePlantingBonus()
            {
                foreach (var player in publicAttackersList)
                {
                    player.MoneyReceived += 300;
                    player.Balance += 300;
                }
            }

            void EndGameRankPlayers()
            {
                if (rankPlayers) return;
                rankPlayers = true;
                if (!Variables.isCGGORanked) return;

                var (rankedAttackers, rankedAttackersScores) = CGGOPlayerRanking.RankPlayers(publicAttackersList, true);
                var (rankedDefenders, rankedDefendersScores) = CGGOPlayerRanking.RankPlayers(publicDefendersList, true);
                var (rankedAll,rankedAllScores) = CGGOPlayerRanking.RankPlayers(Variables.cggoPlayersList, true);

                List<CGGOPlayer> rankedPlayers = new List<CGGOPlayer>();
                List<double> rankedPlayersScores = new List<double>();

                if (winnerTeam == 0)
                {

                    for (int i = 0; i < publicDefendersList.Count() && i < publicAttackersList.Count();i++)
                    {
                        rankedPlayers.Add(rankedAttackers[i]);
                        rankedPlayersScores.Add(rankedAttackersScores[i]);
                    }

                    for (int i = 0; i < rankedAll.Count(); i++)
                    {
                        if (!rankedPlayers.Contains(rankedAll[i]))
                        {
                            rankedPlayers.Add(rankedAll[i]);
                            rankedPlayersScores.Add(rankedAllScores[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < publicAttackersList.Count() && i < publicDefendersList.Count(); i++)
                    {
                        rankedPlayers.Add(rankedDefenders[i]);
                        rankedPlayersScores.Add(rankedDefendersScores[i]);
                    }

                    for (int i = 0; i < rankedAll.Count(); i++)
                    {
                        if (!rankedPlayers.Contains(rankedAll[i]))
                        {
                            rankedPlayers.Add(rankedAll[i]);
                            rankedPlayersScores.Add(rankedAllScores[i]);
                        }
                    }
                }


                SendEndGameMessage(rankedPlayers, rankedPlayersScores);

                for (int i = 0; i < rankedPlayers.Count; i++)
                {
                    EloFunctions.UpdateEloCGGO(rankedPlayers[i], Variables.totalCGGOPlayer, Variables.totalCGGOGameExpectative, i + 1, Variables.averageCGGOElo, 50, winnerTeam);
                    EloFunctions.UpdatePlayerRank(rankedPlayers[i].SteamId.ToString());
                }
                Utility.ProcessPlayerFiles(Variables.playersListFilePath);
            }
            void SendEndGameMessage(List<CGGOPlayer> rankedPlayers, List<double> rankedScoresPlayers)
            {
                if (winnerTeam == 0)
                    Utility.SendServerMessage("GAME ENDED ---- ATTACKERS WON ---- GAME ENDED");
                else
                    Utility.SendServerMessage("GAME ENDED ---- DEFENDERS WON ---- GAME ENDED");

                // Assurez-vous que les listes contiennent au moins 5 éléments avant d'accéder à leurs indices
                for (int i = 0; i < Math.Min(5, rankedPlayers.Count); i++)
                {
                    string rankMessage = $"#{i + 1} ";
                    if (i == 0)
                        rankMessage += "[MVP] ";

                    rankMessage += $"{TeamIdToString(rankedPlayers[i].Team)}{rankedPlayers[i].Username} Score = {(rankedScoresPlayers[i] * 100).ToString("F1")}";
                    Utility.SendServerMessage(rankMessage);
                }
            }

            string TeamIdToString(int  teamId)
            {
                if (teamId == 0) return "(Att.)";
                else return "(Def.)";
            }

            void SetGameRankData()
            {
                Variables.totalCGGOPlayer = Variables.cggoPlayersList.Count();
                Variables.averageCGGOElo = Variables.cggoPlayersList.Average(player => player.Elo);
                Variables.totalCGGOGameExpectative = CalculateCGGOTotalGameExpectative(Variables.cggoPlayersList, Variables.averageCGGOElo);
            }

            float CalculateCGGOTotalGameExpectative(List<CGGOPlayer> list, float averageElo)
            {
                float totalExpectative = 0;
                foreach (var player in list)
                {
                    try
                    {
                        float playerElo = 1000;
                        string playerEloCheck = Utility.GetValue(player.SteamId.ToString(), "elo");

                        if (playerEloCheck == "NaN")
                            playerEloCheck = Utility.GetValue(player.SteamId.ToString(), "lastElo");

                        if (playerEloCheck != "NaN")
                            playerElo = float.Parse(playerEloCheck);

                        float actualExpectative = (float)(1.0 / (1.0 + Math.Pow(10, (averageElo - playerElo) / 400.0)));
                        totalExpectative += actualExpectative;
                    }
                    catch
                    {
                    }

                }
                return totalExpectative;
            }

            void SetBombPosition()
            {
                if (bomb == null) return;

                if (siteId == 0) bombPos = new Vector3(bomb.transform.position.x, currentMap.MilkZoneAcorner1.y - 1.5f, bomb.transform.position.z);
                else bombPos = new Vector3(bomb.transform.position.x, currentMap.MilkZoneBcorner1.y - 1.5f, bomb.transform.position.z);

            }

            void ManageBuyingPhase()
            {
                ManageMilkZone();
                ManageBuyPhaseStart();
                KillBombSpawner();
                GiveLastRoundWeapons();
                DropDefusers();
                ManageBuyPhaseEnd();
            }
            bool IsCGGOModeOn()
            {
                return Variables.isCGGOActive;
            }

            bool IsPlayableMap()
            {
                return Variables.mapId != 30 && Variables.mapId != 31 && Variables.modeId != 0;
            }

            bool IsTotalTeamScore5()
            {
                return totalTeamScore == 5;
            }

            void ManageInitPhase()
            {
                Variables.hitPlayers.Clear();
                SetCurrentPhaseBuying();
                SetTimeSkippingFalse();
                SetCurrentMap();
                KillHost();
                SetBombPickable();
                AssignTeams();
                CalculateTotalTeamScore();
                ResetPlayersRoundStates();
                ResetItemsOnMap();
                AdjustBalance();
                if (IsTotalTeamScore5()) SwitchTeamSide();
                SpawnAndColorTeams();
            }

            void SetTimeSkippingFalse()
            {
                Variables.startRound = false;
            }
            void ManageDefusingElapsed(float deltaTime)
            {
                elapsedDefusingMessage += deltaTime;
                elapsedDefusingTimer += deltaTime;
            }

            bool CanRoundStart()
            {
                return Variables.startRound;
            }
            void ManageElapsed(float deltaTime)
            {
                elapsedShield += deltaTime;

                if (IsCurrentPhaseInit()) elapsedInit += deltaTime;

                if (IsCurrentPhaseTuto())
                {
                    elapsedTutoMessage += deltaTime;
                    elapsedTuto += deltaTime;
                    elapsedMilkZone += deltaTime;
                    elaspedRespawnTuto += deltaTime;
                }


                if (IsCurrentPhaseBuying())
                {
                    elapsedBuyPhase += deltaTime;
                    elapsedMilkZone += deltaTime;
                    elapsedBuyMessage += deltaTime;
                }
                else
                {
                    elapsedBuyPhase = 0f;
                    elapsedBuyMessage = 0f;
                }

                if (IsCurrentPhasePlanting())
                {
                    elapsedPlantingPhase += deltaTime;
                    elapsedPlantingMessage += deltaTime;
                }
                else
                {
                    elapsedPlantingPhase = 0f;
                    elapsedPlantingMessage = 0f;
                }

                if (IsCurrentPhaseDefusing())
                {
                    elapsedDefusingPhase += deltaTime;
                    elapsedDefusingMessage += deltaTime;
                    elapsedRemoveBombDummy += deltaTime;
                }
                else
                {
                    elapsedDefusingPhase = 0f;
                    elapsedDefusingMessage = 0f;
                    elapsedRemoveBombDummy = 0f;
                }

                if (IsCurrentPhaseEnded()) elapsedRoundEnded += deltaTime;

                if (IsCurrentPhaseGameEnded()) elapsedGameEnded += deltaTime;
                else elapsedGameEnded = 0f;

                if (cheaterDetected) elapsedCheaterMessage += deltaTime;
            }

            void SetPublicPhase()
            {
                publicBuyPhase = IsCurrentPhaseBuying();
                publicTutoPhase = IsCurrentPhaseTuto();
                publicEndPhase = IsCurrentPhaseEnded();
            }

            bool IsAttackersScoreMax()
            {
                return Variables.cggoScore[0] >= 6;
            }

            bool IsDefendersScoreMax()
            {
                return Variables.cggoScore[1] >= 6;
            }

            bool DidAttackersWon()
            {
                return attackersWon;
            }

            bool DidDefendersWon()
            {
                return defendersWon;
            }

            void ManageEndingPhase()
            {
                if (colorManager != null) colorManager.freezeTimer.field_Private_Single_0 = 20f;
                if (DidAttackersWon()) SendAttackersWonMessage();
                if (DidDefendersWon()) SendDefendersWonMessage();

                roundWinMessageSent = true;

                if (!nextRoundLoaded && elapsedRoundEnded > 7f)
                {
                    if (IsAttackersScoreMax() && !IsCurrentPhaseGameEnded())
                    {
                        winnerTeam = 0;
                        EndGameRankPlayers();
                        SetCurrentPhaseGameEnded();
                        return;
                    }
                    if (IsDefendersScoreMax() && !IsCurrentPhaseGameEnded())
                    {
                        winnerTeam = 1;
                        EndGameRankPlayers();
                        SetCurrentPhaseGameEnded();
                        return;
                    }

                    ResetPublicVariables();
                    LoadCGGOMap();
                }
            }

            public static void HandleWeaponDrops(CGGOPlayer player)
            {
                DropItemIfWeapon(player.Classic, player.SteamId, 1, 12, publicClassicList);
                DropItemIfWeapon(player.Vandal, player.SteamId, 0, 30, publicVandalList);
                DropItemIfWeapon(player.Shorty, player.SteamId, 3, 2, publicShortyList);
                DropItemIfWeapon(player.Katana, player.SteamId, 6, 1, publicKatanaList);
                DropItemIfWeapon(player.Revolver, player.SteamId, 2, 6, publicRevolverList);
            }

            public static void DropItemIfWeapon(bool hasWeapon, ulong steamId, int weaponType, int ammo, List<int> ItemList)
            {
                if (hasWeapon)
                {
                    Variables.weaponId++;
                    try
                    {
                        ServerSend.DropItem(steamId, weaponType, Variables.weaponId, ammo);
                        if (!ItemList.Contains(Variables.weaponId)) ItemList.Add(Variables.weaponId);
                    }
                    catch { }
                }
            }

            bool IsBombDefused()
            {
                return elapsedDefusingTimer > 5f;
            }

            bool IsPlantingPhaseOver()
            {
                return elapsedPlantingPhase >= 35f;
            }

            bool AreAttackersDeadDuringPlanting()
            {
                return IsCurrentPhasePlanting() && publicAllAttackersDead;
            }

            void SetCurrentPhaseInit()
            {
                currentPhase = "init";
            }

            bool IsCurrentPhaseInit()
            {
                return currentPhase == "init";
            }

            void SetCurrentPhaseBuying()
            {
                currentPhase = "buying";
            }

            bool IsCurrentPhaseBuying()
            {
                return currentPhase == "buying";
            }

            void SetCurrentPhasePlanting()
            {
                currentPhase = "planting";
            }

            bool IsCurrentPhasePlanting()
            {
                return currentPhase == "planting";
            }
            void SetCurrentPhaseDefusing()
            {
                currentPhase = "defusing";
            }

            bool IsCurrentPhaseDefusing()
            {
                return currentPhase == "defusing";
            }
            void SetCurrentPhaseTuto()
            {
                currentPhase = "tuto";
            }

            bool IsCurrentPhaseTuto()
            {
                return currentPhase == "tuto";
            }
            void SetCurrentPhaseEnded()
            {
                currentPhase = "ended";
            }

            bool IsCurrentPhaseEnded()
            {
                return currentPhase == "ended";
            }

            bool IsDefusingPhaseOver()
            {
                return elapsedDefusingPhase >= 35f;
            }
            void ManageAllAttackersDeadDuringPlanting()
            {
                SetRoundEnded();
                SetDefendersWon();
                GiveEndRoundMoney(1);
                ManageLoseStrike(0);
            }
            void ManageAllDefendersDead()
            {
                SetRoundEnded();
                SetAttackersWon();
                GiveEndRoundMoney(0);
                ManageLoseStrike(1);
            }
            void ManageBombNotDefusedInTime()
            {
                SetRoundEnded();
                SetAttackersWon();
                GiveEndRoundMoney(0);
                ManageLoseStrike(1);
                ManageBombExplosion();
                RemovePlantedBomb();

            }

            void ManagePlantingPhaseOver()
            {
                SetRoundEnded();
                SetDefendersWon();
                GiveEndRoundMoney(1);
                ManageLoseStrike(0);
            }

            void ManageBombDefused()
            {
                GiveDefusePoint();
                RemovePlantedBomb();
                SetRoundEnded();
                SetDefendersWon();
                ManageLoseStrike(0);
                GiveEndRoundMoney(1);
            }

            void GiveDefusePoint()
            {
                PlayerManager defuser = GetClosestDefuser();
                if (defuser != null) CGGOPlayer.GetCGGOPlayer(defuser.steamProfile.m_SteamID).Defuse += 1;
            }

            
            void RemovePlantedBomb()
            {
                ServerSend.PlayerActiveItem((ulong)BombDummy, 14);
            }
            void SetAttackersWon()
            {
                attackersWon = true;
                Variables.cggoScore[0] += 1;
            }

            void ManageBombExplosion()
            {
                List<CGGOPlayer> alivePlayers = Variables.cggoPlayersList.Where(p => !p.Dead).ToList();
                foreach (var player in alivePlayers)
                {
                    try
                    {
                        PlayerManager playerManager = GameData.GetPlayer(player.SteamId.ToString());
                        if (playerManager == null) continue;

                        float distanceFromBomb = Vector3.Distance(playerManager.transform.position, bombPos);
                        if (distanceFromBomb > 30) continue;

                        try
                        {
                            ServerSend.RespawnPlayer(player.SteamId, new Vector3(0f, 99999999999999f, 0f));
                            ServerSend.PlayerDied(player.SteamId, player.SteamId, Vector3.zero);
                        }
                        catch { }
                    }
                    catch { }
                }
            }
          
            void GiveEndRoundMoney(int winnerTeam)
            {
                foreach (var player in Variables.cggoPlayersList)
                {
                    if (player.Team == winnerTeam)
                    {
                        player.Balance += 2500;
                        player.MoneyReceived += 2500;
                    }
                    else if (player.Dead)
                    {
                        player.Balance += 1800 + (300 * Variables.loseStrike.Value);
                        player.MoneyReceived += 1800 + (300 * Variables.loseStrike.Value);
                    }
                    else
                    {
                        player.Balance += 1000;
                        player.MoneyReceived += 1000;
                    }
                } 
            }

            void ManageLoseStrike(int loserTeam)
            {
                if (Variables.loseStrike.Key == loserTeam && Variables.loseStrike.Value < 2) Variables.loseStrike.Value += 1;
                else if (Variables.loseStrike.Key != loserTeam) Variables.loseStrike.Key = loserTeam;
            }

            void SetDefendersWon()
            {
                defendersWon = true;
                Variables.cggoScore[1] += 1;
            }

            void SetRoundEnded()
            {
                SetCurrentPhaseEnded();
                SetGameStateFreeze();
            }
            void ManageTutoPhase()
            {
                Variables.loseStrike = new(-1, 0);
                Variables.cggoScore = [0, 0];
                Vector3 cinematicStartPos = new(-25f, -4.1f, 52.5f);
                Vector3 cinematicEndPos = new(-10f, -4.1f, 44f);
                milkZoneA = true;
                ManageMilkZone();
                CGGOTutoMessage();
                RespawnManagerCGGOTuto();
                HideOtherPlayers();

                if (elapsedTuto >= 0.5f && !killBombSpawner) KillBombSpawner();

                if (shouldRespawnTuto) return;

                if (tutorialCurrentMessageIndex == 2 && !weaponTutoGiven)
                {
                    weaponTutoGiven = true;
                    Variables.weaponId++;
                    ServerSend.PlayerRotation((ulong)cinematicDummy, 0, 0);
                    ServerSend.PlayerActiveItem((ulong)cinematicDummy, 0);
                }

                if (tutorialCurrentMessageIndex == 3 && !bombTutoTaken)
                {
                    bombTutoTaken = true;
                    tutorialStartTime = Time.time;
                    journeyLength = Vector3.Distance(cinematicStartPos, cinematicEndPos);
                    ServerSend.PlayerActiveItem((ulong)cinematicDummy, 13);
                    ServerSend.PlayerPosition((ulong)cinematicDummy, cinematicStartPos);
                }
                if (tutorialCurrentMessageIndex == 3)
                {
                    MoveDummy(cinematicStartPos, cinematicEndPos);
                }

                if (tutorialCurrentMessageIndex == 4 && !bombTutoPlanted)
                {
                    bombTutoPlanted = true;
                    ServerSend.PlayerActiveItem((ulong)cinematicDummy, 14);
                    Variables.weaponId++;
                    try 
                    {
                        ServerSend.DropItem((ulong)cinematicDummy, 5, Variables.weaponId, 0);
                    }
                    catch { }
                    tutorialStartTime = Time.time;
                    journeyLength = Vector3.Distance(cinematicEndPos, cinematicStartPos);
                    ServerSend.PlayerPosition((ulong)cinematicDummy, cinematicEndPos);
                }

                if (tutorialCurrentMessageIndex == 4)
                {
                    MoveDummy(cinematicEndPos, cinematicStartPos);
                }

                if (tutorialCurrentMessageIndex == 5 && !bombTutoGoDefuse)
                {
                    bombTutoGoDefuse = true;
                    ServerSend.PlayerActiveItem((ulong)cinematicDummy, 9);
                    tutorialStartTime = Time.time;
                    journeyLength = Vector3.Distance(cinematicStartPos, cinematicEndPos);
                    ServerSend.PlayerPosition((ulong)cinematicDummy, cinematicStartPos);
                }

                if (tutorialCurrentMessageIndex == 5)
                {
                    MoveDummy(cinematicStartPos, cinematicEndPos);
                }

                if (tutorialCurrentMessageIndex == 6)
                {
                    ServerSend.PlayerAnimation((ulong)cinematicDummy, 0, true);
                }
                return;

                void MoveDummy(Vector3 start, Vector3 end)
                {
                    float distCovered = (Time.time - tutorialStartTime) * cinematicSpeed;
                    float fractionOfJourney = distCovered / journeyLength;
                    Vector3 lerpPosition = Vector3.Lerp(start, end, fractionOfJourney);

                    ServerSend.PlayerPosition((ulong)cinematicDummy, lerpPosition);
                }
            }

            void LoadRandomMap()
            {
                System.Random random = new System.Random();
                List<int> numbers = new List<int> { 0, 2, 7, 20 };
                int randomIndex = random.Next(0, numbers.Count);
                int randomElement = numbers[randomIndex];

                ServerSend.LoadMap(randomElement, 9);
            }

            bool IsBombOnAorB()
            {
                if (bomb != null)
                {
                    MonoBehaviourPublicObRiSiupVeSiQuVeLiQuUnique bombComponent = bomb.GetComponent<MonoBehaviourPublicObRiSiupVeSiQuVeLiQuUnique>();
                    if (bombComponent != null)
                    {
                        if (BombHandler != null)
                        {
                            if (IsPointOnSite(bomb.transform.position + new Vector3(0, 1.5f, 0), currentMap.MilkZoneAcorner1, currentMap.MilkZoneAcorner2, -0.5f) && Vector3.Distance(bomb.transform.position, BombHandler.gameObject.transform.position) <= 3.5f)
                            {
                                siteId = 0;
                                return true;
                            }
                            else if (IsPointOnSite(bomb.transform.position + new Vector3(0, 1.5f, 0), currentMap.MilkZoneBcorner1, currentMap.MilkZoneBcorner2, -0.5f) && Vector3.Distance(bomb.transform.position, BombHandler.gameObject.transform.position) <= 3.5f)
                            {
                                siteId = 1;
                                return true;
                            }
                            else return false;
                        }
                        else return false;
                    }
                    else
                    {
                        try
                        {
                            PlayerManager player = bomb.transform.parent.transform.parent.transform.parent.GetComponent<PlayerManager>();
                            BombHandler = player;
                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                else return false;
            }

            void FindGameObjects()
            {
                bomb = GameObject.Find("Grenade(Clone)");
                defusers = FindObjectsOfType<GameObject>().Where(go => go.name == "Snowball(Clone)").ToArray();
            }
            void SendCheaterAnimation()
            {
                foreach (var player in Variables.cggoPlayersList)
                {
                    ServerSend.PlayerDamage(player.SteamId, player.SteamId, 0, Vector3.zero, -1);
                }
                if (elapsedCheaterMessage < 0.25f) return;
                elapsedCheaterMessage = 0f;
                string specialMessage = "";
                switch (CheaterDetectedMessageState)
                {
                    case 0:
                        specialMessage = "---- C H E A T E R ---- D E T E C T E D ----";
                        break;
                    case 1:
                        specialMessage = "---- X X X X X X X ---- X X X X X X X X ----";
                        break;
                }
                Utility.SendServerMessage("-");
                Utility.SendServerMessage(specialMessage);
                Utility.SendServerMessage("[G A C] ---- [G A C]");
                Utility.SendServerMessage("- A cheater in your game has been detected");
                Utility.SendServerMessage($"----> Username: {cheaterUsername}");
                Utility.SendServerMessage($"----> SteamId: {cheaterSteamId}");
                Utility.SendServerMessage("- Banned forever from CG:GO");
                Utility.SendServerMessage("[G A C] ---- [G A C]");
                Utility.SendServerMessage(specialMessage);



                CheaterDetectedMessageState++;

                if (CheaterDetectedMessageState > 1)
                    CheaterDetectedMessageState = 0;
            }
            void CheckForCheaters()
            {
                var playersToBan = new List<CGGOPlayer>(); 

                foreach (var player in Variables.cggoPlayersList) 
                {
                    if (player.CheatFlag > 5)
                    {
                        playersToBan.Add(player);
                    }
                }

                foreach (var player in playersToBan)
                {
                    Utility.Log(Variables.logFilePath, $"Cheater Kicked: {player.Username}, {player.SteamId}");
                    GameServer.ForceRemoveItemItemId(player.SteamId, 7);
                    if (player.Vandal) GameServer.ForceRemoveItemItemId(player.SteamId, 0);
                    if (player.Classic) GameServer.ForceRemoveItemItemId(player.SteamId, 1);
                    if (player.Revolver) GameServer.ForceRemoveItemItemId(player.SteamId, 2);
                    if (player.Shorty) GameServer.ForceRemoveItemItemId(player.SteamId, 3);
                    if (player.Katana) GameServer.ForceRemoveItemItemId(player.SteamId, 6);
                    player.Vandal = false;
                    player.Classic = false;
                    player.Revolver = false;
                    player.Shorty = false;
                    player.Katana = false;
                    player.Balance = 0;

                    SetGameStateFreeze();
                    cheaterSteamId = player.SteamId;
                    cheaterUsername = player.Username;
                    LobbyManager.Instance.KickPlayer(player.SteamId);
                    Variables.cggoScore = [0, 0];
                    cheaterDetected = true;
                    SetCurrentPhaseGameEnded();
                }
            }

            void SendMessages()
            {
                switch (currentPhase)
                {
                    case "buying":
                        SendBuyPhaseMessages();
                        break;
                    case "planting":
                        SendPlantingPhaseMessages();
                        break;
                    case "defusing":
                        SendDefusePhaseMessages();
                        break;
                    default:
                        break;
                }
            }

            void KillPlayer(ulong playerId)
            {
                ServerSend.RespawnPlayer(playerId, new Vector3(0, -300, 0));
            }

            void CreateMilkZoneA()
            {
                if (elapsedMilkZone < 0.05f) return;

                elapsedMilkZone = 0f;
                milkZoneA = CreateMilkZone(currentMap.MilkZoneAcorner1, currentMap.MilkZoneAcorner2, currentMap.MilkZoneAcorner1.y + 1f);
                if (milkZoneA) KillPlayer((ulong)dummyPerimeterId);

            }
            void CreateMilkZoneB()
            {
                if (elapsedMilkZone < 0.05f) return;

                milkZoneB = CreateMilkZone(currentMap.MilkZoneBcorner1, currentMap.MilkZoneBcorner2, currentMap.MilkZoneBcorner1.y + 1f);
                if (milkZoneB) KillPlayer((ulong)dummyPerimeterId);
                elapsedMilkZone = 0f;
            }


            void ManageMilkZone()
            {
                if (!milkZoneA) CreateMilkZoneA();
                if (milkZoneA && !milkZoneB) CreateMilkZoneB();
            }
            void SetMaxScore(int team)
            {
                Variables.cggoScore[team] = 6;
            }

            void ManageBuyPhaseStart()
            {
                if (buyPhaseStart) return;
                buyPhaseStart = true;
                SpawnBomb();
                if (IsRound1or6()) GiveStartMoney();
            }

            void KillBombSpawner()
            {
                if (elapsedBuyPhase < 1f || killBombSpawner) return;

                killBombSpawner = true;
                KillPlayer(bombSpawnedId);
            }

            void GiveLastRoundWeapons()
            {
                if (lastRoundWeaponGiven) return;
                lastRoundWeaponGiven = true;

                foreach (var player in Variables.cggoPlayersList)
                {
                    if (player.Classic)
                    {
                        Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 1, Variables.weaponId);
                        if (!publicClassicList.Contains(Variables.weaponId)) publicClassicList.Add(Variables.weaponId);
                    }
                    if (player.Shorty)
                    {
                        Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 3, Variables.weaponId);
                        if (!publicShortyList.Contains(Variables.weaponId)) publicShortyList.Add(Variables.weaponId);
                    }
                    if (player.Vandal)
                    {
                        Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 0, Variables.weaponId);
                        if (!publicVandalList.Contains(Variables.weaponId)) publicVandalList.Add(Variables.weaponId);
                    }
                    if (player.Revolver)
                    {
                        Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 2, Variables.weaponId);
                        if (!publicRevolverList.Contains(Variables.weaponId)) publicRevolverList.Add(Variables.weaponId);
                    }
                    if (player.Katana)
                    {
                        int weaponId = Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 6, weaponId);
                        player.KatanaId = weaponId;
                        if (!publicKatanaList.Contains(Variables.weaponId)) publicKatanaList.Add(weaponId);
                    }
                    else
                    {
                        int weaponId = Variables.weaponId++;
                        GameServer.ForceGiveWeapon(player.SteamId, 7, weaponId);
                        player.KnifeId = weaponId;
                    }             
                }
            }

            void AssignWeapon(ulong steamId, int weaponTypeId, List<int> weaponList)
            {
                Variables.weaponId++;
                weaponList?.Add(Variables.weaponId);
                try
                {
                    GameServer.ForceGiveWeapon(steamId, weaponTypeId, Variables.weaponId);
                }
                catch
                {
                }
            }

            void ManageBuyPhaseEnd()
            {
                if (elapsedBuyPhase >= 15f)
                {
                    SpawnAndColorTeams();
                    SetCurrentPhasePlanting();
                    GameData.SetGameTime(1000);
                    ServerSend.FreezePlayers(false);
                }
            }


            void SetCurrentMap()
            {
                switch (Variables.mapId)
                {
                    case 0:
                        currentMap = new MapBitterBeach();
                        break;
                    case 2:
                        currentMap = new MapCockyContainers();
                        break;
                    case 7:
                        currentMap = new MapFunkyField();
                        break;
                    case 20:
                        currentMap = new MapReturnToMonke();
                        break;
                        /*
                    case 29:
                        currentMap = new MapSnowTop();
                        break;*/
                }
            }

            void KillHost()
            {
                try
                {
                    ServerSend.RespawnPlayer(Variables.clientId, Variables.clientBody.transform.position + new Vector3(0, -300, 0));
                }
                catch { }
            }

            void SetBombPickable()
            {
                ItemManager.idToItem[5].type = ItemType.Throwable;
            }

            void SetBombUnpickable()
            {
                ItemManager.idToItem[5].type = ItemType.Other;
            }

            void CalculateTotalTeamScore()
            {
                totalTeamScore = Variables.cggoScore[0] + Variables.cggoScore[1];
            }

            void ResetPlayersRoundStates()
            {
                foreach (var player in Variables.cggoPlayersList)
                {

                    player.Assisters = [];
                    player.Dead = false;
                    player.Killer = 0;
                    player.CheatFlag = 0;
                    player.DamageTaken = 0;
                }
            }

            void SwitchTeamSide()
            {
                int scoreAttackers = Variables.cggoScore[0];
                int scoreDefenders = Variables.cggoScore[1];
                Variables.cggoScore[1] = scoreAttackers;
                Variables.cggoScore[0] = scoreDefenders;
                Variables.loseStrike = new(-1, 0);

                foreach (var player in Variables.cggoPlayersList)
                {
                    if (player.Team == 0) player.Team = 1;
                    else if (player.Team == 1) player.Team = 0;

                    player.Balance = 0;
                    player.Katana = false;
                    player.Classic = false;
                    player.Shorty = false;
                    player.Vandal = false;
                    player.Revolver = false;
                    player.Shield = 0;
                }
            }

            void ResetItemsOnMap()
            {
                publicClassicList = [];
                publicShortyList = [];
                publicVandalList = [];
                publicKatanaList = [];
                publicRevolverList = [];
            }
            void ResetPublicVariables()
            {
                publicAllAttackersDead = false;
                publicAllDefendersDead = false;
                publicClassicList = [];
                publicShortyList = [];
                publicVandalList = [];
                publicKatanaList = [];
                publicRevolverList = [];
                publicAttackersList = [];
                publicDefendersList = [];
            }
            void ResetGameVariables()
            {
                SetCurrentPhaseGameEnded();
                Variables.startRound = false;
                Variables.cggoPlayersList.Clear();
                Variables.cggoScore = [0, 0];
                Variables.makeCGGOTeam = false;
                Variables.loseStrike = new(-1, 0);
                Variables.totalCGGOGameExpectative = 0;
                Variables.totalCGGOPlayer = 0;
                Variables.averageCGGOElo = 0;
                Variables.isTutoDone = false;
                publicAllAttackersDead = false;
                publicAllDefendersDead = false;
                publicClassicList = [];
                publicShortyList = [];
                publicVandalList = [];
                publicKatanaList = [];
                publicRevolverList = [];
                publicAttackersList = [];
                publicDefendersList = [];
            }

            void ResetGameVariablesForTutorial()
            {
                Variables.startRound = false;
                Variables.cggoPlayersList.Clear();
                Variables.cggoScore = [0, 0];
                Variables.makeCGGOTeam = false;
                Variables.loseStrike = new(-1, 0);
                Variables.totalCGGOGameExpectative = 0;
                Variables.totalCGGOPlayer = 0;
                Variables.averageCGGOElo = 0;
                publicAllAttackersDead = false;
                publicAllDefendersDead = false;
                publicClassicList = [];
                publicShortyList = [];
                publicVandalList = [];
                publicKatanaList = [];
                publicRevolverList = [];
                publicAttackersList = [];
                publicDefendersList = [];

            }

            void SetCurrentPhaseGameEnded()
            {
                currentPhase = "gameEnded";
            }

            bool IsCurrentPhaseGameEnded()
            {
                return currentPhase == "gameEnded";
            }


            void LoadLobby()
            {
                ServerSend.LoadMap(6, 0);
            }

            void LoadCGGOMap()
            {
                try
                {
                    nextRoundLoaded = true;
                    ServerSend.LoadMap(currentMap.MapId, 9);
                }
                catch
                {
                    ServerSend.LoadMap(Variables.mapId, 9);
                }

            }
            public static void SetGameStateFreeze()
            {
                GameManager.Instance.gameMode.modeState = GameMode.EnumNPublicSealedvaFrPlEnGa5vUnique.Freeze;
            }

            void SpawnPlantedBomb(int site)
            {
                Vector3 dummyPos = Vector3.zero;
                if (site == 0) dummyPos = new Vector3(bombPos.x, currentMap.MilkZoneAcorner1.y - 3, bombPos.z);
                else dummyPos = new Vector3(bombPos.x, currentMap.MilkZoneBcorner1.y - 3, bombPos.z);

                Variables.weaponId++;
                int dummyId = Variables.weaponId;
                BombDummy = dummyId;

                CreateDummy(dummyPos, dummyId);
                ServerSend.PlayerRotation((ulong)dummyId, 0, -180);
                ServerSend.PlayerActiveItem((ulong)dummyId, 5);
                ServerSend.PlayerAnimation((ulong)dummyId, 0, true);


                DeleteItem.deleteTime = 200;
                Variables.itemToDelete.Add(publicGrenadeId, DateTime.Now);
            }

            void SendAttackersWonMessage() 
            {
                if (roundWinMessageSent) return;
                SendScoreBoardAndSpecialMessage("---- A T T A C K E R S ---- W O N ----");
                roundWinMessageSent = true;
            }
            void SendDefendersWonMessage()
            {
                if (roundWinMessageSent) return;
                SendScoreBoardAndSpecialMessage("---- D E F E N D E R S ---- W O N ----");
                roundWinMessageSent = true;
            }
            void SendScoreBoardAndSpecialMessage(string specialMessage) //Should fix scoreboard display AGAIN
            {
                List<string> messageList = [];
                List<CGGOPlayer> topAttackersPlayers = publicAttackersList.OrderByDescending(p => p.Kills).Take(6).ToList();
                List<CGGOPlayer> topDefendersPlayers = publicDefendersList.OrderByDescending(p => p.Kills).Take(6).ToList();

                while (topAttackersPlayers.Count < 6) topAttackersPlayers.Add(null);
                while (topDefendersPlayers.Count < 6) topDefendersPlayers.Add(null);

                string AdjustName(string name, int maxLength)
                {
                    if (name.Length > maxLength)
                        return name.Substring(0, maxLength - 3) + "...";

                    var adjustedName = name;
                    while (adjustedName.Length < maxLength)
                    {
                        adjustedName += "- ";
                    }

                    adjustedName = adjustedName.Replace("-----", "- - -");

                    return adjustedName;
                }

                int maxNameLength = 12; 

                for (int i = 0; i < 6; i++)
                {
                    string message = "";

                    if (topAttackersPlayers[i] != null)
                    {
                        var playerA = topAttackersPlayers[i];
                        string playerNameA = AdjustName(playerA.Username, maxNameLength);
                        message += $"{playerNameA} {playerA.Kills}/{playerA.Deaths}/{playerA.Assists}";
                    }
                    else
                    {
                        message += " - - - - - - - - - - - - - -"; 
                    }

                    message += " | ";

                    if (topDefendersPlayers[i] != null)
                    {
                        var playerB = topDefendersPlayers[i];
                        string playerNameB = AdjustName(playerB.Username, maxNameLength);
                        message += $"{playerNameB} {playerB.Kills}/{playerB.Deaths}/{playerB.Assists}";
                    }
                    else
                    {
                        message += "- - - - - - - - - - - - - -"; 
                    }
                    messageList.Add(message);
                }

                Utility.SendServerMessage("#");
                Utility.SendServerMessage(specialMessage);
                Utility.SendServerMessage($"- ATTACKERS[{Variables.cggoScore[0]}] | [{Variables.cggoScore[1]}]DEFENDERS -");
                Utility.SendServerMessage(messageList[0]);
                Utility.SendServerMessage(messageList[1]);
                Utility.SendServerMessage(messageList[2]);
                Utility.SendServerMessage(messageList[3]);
                Utility.SendServerMessage(messageList[4]);

            }
            void SendDefusingMessage()
            {
                if (elapsedDefusingMessage < 0.25f) return;
                foreach (var player in Variables.playersList)
                {
                    Utility.SendServerMessage("#");
                    Utility.SendServerMessage("-");
                    Utility.SendServerMessage("-");
                    Utility.SendServerMessage("-");
                    switch ((int)elapsedDefusingTimer)
                    {
                        case 0:
                            Utility.SendServerMessage("D E F U S I N G [_ _ _ _ _]");
                            break;
                        case 1:
                            Utility.SendServerMessage("D E F U S I N G [X _ _ _ _]");
                            break;
                        case 2:
                            Utility.SendServerMessage("D E F U S I N G [X X _ _ _]");
                            break;
                        case 3:
                            Utility.SendServerMessage("D E F U S I N G [X X X _ _]");
                            break;
                        case 4:
                            Utility.SendServerMessage("D E F U S I N G [X X X X _]");
                            break;
                        case 5:
                            Utility.SendServerMessage("D E F U S I N G [X X X X X]");
                            break;
                    }
                    Utility.SendServerMessage("-");
                    Utility.SendServerMessage("-");
                    Utility.SendServerMessage("-");
                    Utility.SendServerMessage("-");
                }
                elapsedDefusingMessage = 0f;
            }

            private Vector3[] perimeterExpandedCorners; // The corners after margin addition
            private int perimeterCurrentSegment = 0;    // Current segment
            private float perimeterCurrentDistance = 0; // Distance traveled on the current segment
            private Vector3 perimeterCurrentPosition;   // Current position on the perimeter
            private bool perimeterCompleted, perimeterInit; // Completed round indicator
            private int dummyPerimeterId = 0; // Dummy ID for perimeter object
            private readonly float heightOffset = 0; // Height offset for item placement

            void InitPerimeter(Vector3 corner1, Vector3 corner2)
            {
                // Calculate the cuboid corners with the margin
                perimeterExpandedCorners = new Vector3[4];
                perimeterExpandedCorners[0] = corner1;
                perimeterExpandedCorners[1] = new Vector3(corner2.x, corner1.y, corner1.z);
                perimeterExpandedCorners[2] = corner2;
                perimeterExpandedCorners[3] = new Vector3(corner1.x, corner2.y, corner2.z);
                perimeterCurrentPosition = perimeterExpandedCorners[0];
                perimeterInit = true;
                perimeterCurrentSegment = 0; // Reset segment
                perimeterCurrentDistance = 0; // Reset distance
                perimeterCompleted = false;
                dummyPerimeterId = 0;

                Variables.weaponId++;
                dummyPerimeterId = Variables.weaponId;
                CreateDummy(perimeterCurrentPosition, dummyPerimeterId);
            }

            void MoveAlongPerimeter()
            {
                if (perimeterExpandedCorners == null || perimeterExpandedCorners.Length < 4)
                {
                    return;
                }

                // Calculate the current segment
                Vector3 start = perimeterExpandedCorners[perimeterCurrentSegment];
                Vector3 end = perimeterExpandedCorners[(perimeterCurrentSegment + 1) % perimeterExpandedCorners.Length];
                Vector3 direction = (end - start).normalized;
                float segmentLength = Vector3.Distance(start, end);

                // Move 1 unit
                perimeterCurrentDistance += 1;

                if (perimeterCurrentDistance > segmentLength)
                {
                    // Move to the next segment
                    perimeterCurrentSegment = (perimeterCurrentSegment + 1) % perimeterExpandedCorners.Length;
                    perimeterCurrentDistance = 0;
                    perimeterCurrentPosition = perimeterExpandedCorners[perimeterCurrentSegment];

                    // Check if a full round is completed
                    if (perimeterCurrentSegment == 0)
                    {
                        perimeterCompleted = true;
                    }
                }
                else
                {
                    // Update the current position
                    perimeterCurrentPosition = start + direction * perimeterCurrentDistance;
                }

                // Update the object's position
                transform.position = perimeterCurrentPosition;
            }

            public bool HasCompletedPerimeter()
            {
                if (perimeterCompleted)
                {
                    perimeterCompleted = false;
                    return true;
                }
                return false;
            }

            bool CreateMilkZone(Vector3 corner1, Vector3 corner2, float height)
            {
                if (!perimeterInit)
                {
                    InitPerimeter(new Vector3(corner1.x, height, corner1.z), new Vector3(corner2.x, height, corner2.z));
                    return false;
                }
                else
                {
                    if (!HasCompletedPerimeter())
                    {
                        Variables.weaponId++;
                        try
                        {
                            ServerSend.DropItem((ulong)dummyPerimeterId, 11, Variables.weaponId, 0);
                        }
                        catch { }
                        MoveAlongPerimeter();
                        ServerSend.PlayerPosition((ulong)dummyPerimeterId, perimeterCurrentPosition + new Vector3(0, heightOffset, 0));
                        return false;
                    }
                    else
                    {
                        perimeterInit = false;
                        return true;
                    }
                }
            }


            void SpawnAndColorTeams()
            {
                int teamAttackersCount = 0, teamDefendersCount = 0;

                // Créer une liste temporaire pour stocker les joueurs à supprimer
                List<CGGOPlayer> playersToRemove = new List<CGGOPlayer>();

                foreach (var player in Variables.cggoPlayersList)
                {
                    if (GameData.GetPlayer(player.SteamId.ToString()) == null)
                    {
                        // Ajouter le joueur à la liste des suppressions
                        playersToRemove.Add(player);
                        try
                        {
                            Variables.playersList.Remove(player.SteamId);
                        }
                        catch (Exception ex)
                        {
                            Utility.Log(Variables.logFilePath, $"An error occurred while removing player {player.SteamId}: {ex}");
                        }
                        continue;
                    }

                    try
                    {
                        Vector3 spawnPosition;
                        if (player.Team == 0)
                        {
                            spawnPosition = currentMap.SpawnTeamAttackers + currentMap.SpawnDirectionTeamAttackers * teamAttackersCount;
                            ServerSend.RespawnPlayer(player.SteamId, spawnPosition);

                            try
                            {
                                colorManager?.MakeTeam(player.SteamId, 0); // Team 0 pour les attaquants
                            }
                            catch (Exception ex)
                            {
                                Utility.Log(Variables.logFilePath, $"An error occurred while setting the team color for player {player.SteamId} on Team A: {ex}");
                            }

                            teamAttackersCount++;
                        }
                        else if (player.Team == 1)
                        {
                            spawnPosition = currentMap.SpawnTeamDefenders + currentMap.SpawnDirectionTeamDefenders * teamDefendersCount;
                            ServerSend.RespawnPlayer(player.SteamId, spawnPosition);

                            try
                            {
                         
                                colorManager?.MakeTeam(player.SteamId, 2); // Team 2 pour les défenseurs
                         
                            }
                            catch (KeyNotFoundException ex)
                            {
                                Utility.Log(Variables.logFilePath, $"Key not found while setting the team color for player {player.SteamId} on Team B: {ex}");
                            }
                            catch (Exception ex)
                            {
                                Utility.Log(Variables.logFilePath, $"An error occurred while setting the team color for player {player.SteamId} on Team B: {ex}");
                            }

                            teamDefendersCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.Log(Variables.logFilePath, $"An error occurred while processing player {player.SteamId}: {ex}");
                    }
                }

                // Supprimer les joueurs de la liste après l'itération
                foreach (var player in playersToRemove)
                {
                    Variables.cggoPlayersList.Remove(player);
                }

                try
                {
                    colorManager?.SendTeam();
                }
                catch (Exception ex)
                {
                    Utility.Log(Variables.logFilePath, $"An error occurred while sending the team data: {ex}");
                }
            }
            void GiveStartMoney()
            {
                foreach (var player in Variables.cggoPlayersList)
                {
                    player.Balance += 800;
                    player.MoneyReceived += 800;
                }
            }

            void SendPlantingPhaseMessages()
            {
                string baseMessage = "---- P L A N T I N G ---- P H A S E ";
                string specialMessage = "";

                if (elapsedPlantingMessage > 0.25f)
                {
                    // Génération dynamique du message rotatif
                    specialMessage = GenerateRotatedMessage(baseMessage, plantingPhaseMessageState);

                    foreach (var vPlayer in Variables.cggoPlayersList)
                    {
                        Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, "");
                        Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, specialMessage);
                        if (vPlayer.Team == 0)
                        {
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"You are an ATTACKER!");
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"You have {35 - (int)elapsedPlantingPhase} secondes to plant the Bomb!");
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"Drop the Bomb on MilkZone to plant!");
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"____ C H A T ____ ____ C H A T ____");
                            SendChatMessages(vPlayer.SteamId, 3);
                        }
                        else
                        {
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"You are a DEFENDER!");
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"{35 - (int)elapsedPlantingPhase}s left to stop the planting of the Bomb!");
                            Utility.SendPrivateMessageWithWaterMark(vPlayer.SteamId, $"____ C H A T ____ ____ C H A T ____");
                            SendChatMessages(vPlayer.SteamId, 4);
                        }

                        foreach (var oPlayer in Variables.newPlayers)
                        {
                            ulong steamId = oPlayer;
                            Utility.SendPrivateMessageWithWaterMark(steamId, $"#");
                            Utility.SendPrivateMessageWithWaterMark(steamId, specialMessage);
                            Utility.SendPrivateMessageWithWaterMark(steamId, $"ROUND {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}");
                            Utility.SendPrivateMessageWithWaterMark(steamId, $"{35 - (int)elapsedPlantingPhase}s left to plant the Bomb");
                            Utility.SendPrivateMessageWithWaterMark(steamId, $"____ C H A T ____ ____ C H A T ____");
                            SendChatMessages(steamId, 4);
                        }
                    }

                    plantingPhaseMessageState++;

                    if (plantingPhaseMessageState > baseMessage.Length - 1)
                        plantingPhaseMessageState = 0;

                    elapsedPlantingMessage = 0f;
                }
            }

            // Fonction pour générer le message rotatif
            string GenerateRotatedMessage(string message, int shift)
            {
                int length = message.Length;
                shift %= length;
                return message.Substring(shift) + message.Substring(0, shift);
            }

            void AdjustBalance()
            {
                foreach (var player in Variables.cggoPlayersList) if (player.Balance > 6000) player.Balance = 6000;
            }

            void SendDefusePhaseMessages()
            {
                string baseMessage = "---- S P I K E ---- P L A N T E D ";
                string specialMessage = "";

                if (elapsedDefusingMessage > 0.25f)
                {
                    // Génération dynamique du message rotatif
                    specialMessage = GenerateRotatedMessage(baseMessage, defusePhaseMessageState);

                    foreach (var player in Variables.cggoPlayersList)
                    {
                        if (player.Team == 0 && elapsedDefusingTimer <= 0f) // Attaquant
                        {
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, specialMessage);
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "You are an ATTACKER!");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, $"Protect the Bomb for {35 - (int)elapsedDefusingPhase}s!");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "____ C H A T ____ ____ C H A T ____");
                            SendChatMessages(player.SteamId, 4);
                        }
                        else if (elapsedDefusingTimer <= 0f) // Défenseur
                        {
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, specialMessage);
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "You are a DEFENDER!");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, $"You have {35 - (int)elapsedDefusingPhase}s to defuse the Bomb!");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "Press 4 and crouch on the Bomb to defuse");
                            Utility.SendPrivateMessageWithWaterMark(player.SteamId, "____ C H A T ____ ____ C H A T ____");
                            SendChatMessages(player.SteamId, 3);
                        }
                    }

                    foreach (var player in Variables.newPlayers)
                    {
                        ulong steamId = player;
                        Utility.SendPrivateMessageWithWaterMark(steamId, "");
                        Utility.SendPrivateMessageWithWaterMark(steamId, specialMessage);
                        Utility.SendPrivateMessageWithWaterMark(steamId, $"ROUND {totalTeamScore + 1} | SCORE: {Variables.cggoScore[0]} - {Variables.cggoScore[1]}");
                        Utility.SendPrivateMessageWithWaterMark(steamId, $"{35 - (int)elapsedDefusingPhase}s left to defuse the Bomb");
                        Utility.SendPrivateMessageWithWaterMark(steamId, "____ C H A T ____ ____ C H A T ____");
                        SendChatMessages(steamId, 4);
                    }

                    defusePhaseMessageState++;

                    if (defusePhaseMessageState > baseMessage.Length - 1)
                    {
                        defusePhaseMessageState = 0;
                    }

                    elapsedDefusingMessage = 0f;
                }
            }
            void SendBuyPhaseMessages()
            {
                if (elapsedBuyMessage > 0.12f)
                {
                    elapsedBuyMessage = 0f;
                    int totalBalanceTeamAttackers = 0;
                    int totalBalanceTeamDefenders = 0;
                    int phaseLength = 1; // Initialisation par défaut à 1 pour éviter la division par zéro

                    foreach (var player in Variables.cggoPlayersList)
                    {
                        string[] messages = CGGOMessages.GetBuyPhaseMessageInSpecificLanguage(player, totalTeamScore);
                        if (messages.Length == 0) continue;

                        SendPlayerMessages(player, messages);

                        if (player.Team == 0)
                            totalBalanceTeamAttackers += player.Balance;
                        else if (player.Team == 1)
                            totalBalanceTeamDefenders += player.Balance;
                    }

                    foreach (var player in Variables.newPlayers)
                    {
                        SendNewPlayerMessages(player, totalBalanceTeamAttackers, totalBalanceTeamDefenders);
                    }

                    if (phaseLength > 0)
                    {
                        buyPhaseMessageState = (buyPhaseMessageState + 1) % phaseLength;
                    }
                    else
                    {
                        Utility.ForceMessage("[ERROR] phaseLength is zero, modulo operation skipped.");
                    }
                }
            }

            void SendPlayerMessages(CGGOPlayer player, string[] messages)
            {
                if (messages.Length == 0) return;

                string baseMessage = messages[0];
                int phaseLength = baseMessage.Length;

                string specialMessage = GenerateRotatedMessage(baseMessage, player.CurrentPhaseMessageState);

                for (int x = 0; x < 10; x++) Utility.SendPrivateMessageWithWaterMark(player.SteamId, "");
                Utility.SendPrivateMessageWithWaterMark(player.SteamId, specialMessage);
                for (int i = 1; i < messages.Length; i++)
                {
                    Utility.SendPrivateMessageWithWaterMark(player.SteamId, messages[i]);
                }

                player.CurrentPhaseMessageState = (player.CurrentPhaseMessageState + 1) % phaseLength;
            }

            void SendNewPlayerMessages(ulong steamId, int totalBalanceTeamA, int totalBalanceTeamB)
            {
                string[] messages = CGGOMessages.GetNewPlayersBuyPhaseMessageInSpecificLanguage(Utility.GetValue(steamId.ToString(), "language"), totalTeamScore, totalBalanceTeamA, totalBalanceTeamB);

                foreach (string message in messages)
                {
                    Utility.SendPrivateMessageWithWaterMark(steamId, message);
                }

                SendChatMessages(steamId, 3);
            }

            static void SendChatMessages(ulong steamId, int numberOfMessages)
            {
                List<string> validMessages = new List<string>();
                int index = 0;

                while (validMessages.Count < numberOfMessages && index <= 8)
                {
                    string message = Variables.messagesList[index];
                    if (!message.StartsWith("#srv#")) validMessages.Add(message);
                    index++;
                }

                int messageSent = 0;

                for (int i = validMessages.Count - 1; i >= 0; i--)
                {
                    Utility.SendPrivateMessageWithWaterMark(steamId, validMessages[i]);
                    messageSent++;
                }

                while (messageSent < numberOfMessages)
                {
                    Utility.SendPrivateMessageWithWaterMark(steamId, "-");
                    messageSent++;
                }
            }

            void AssignTeams()
            {
                if (Variables.makeCGGOTeam) return;

                Variables.makeCGGOTeam = true;
                List<PlayerManager> validPlayers = new();

                foreach (var player in Variables.playersList)
                {
                    if (player.Key != Variables.clientId)
                        validPlayers.Add(player.Value);
                }

                if (validPlayers.Count >= 4) Variables.isCGGORanked = true;
                else Variables.isCGGORanked = false;

                List<(PlayerManager player, float elo)> playersWithElo = new();
                foreach (var player in validPlayers)
                {
                    string steamId = player.steamProfile.m_SteamID.ToString();
                    if (float.TryParse(Utility.GetValue(steamId, "elo"), out float elo)) playersWithElo.Add((player, elo));
                    
                }

                playersWithElo.Sort((x, y) => y.elo.CompareTo(x.elo));

                List<CGGOPlayer> teamAttackers = new();
                List<CGGOPlayer> teamDefenders = new();
                float teamAttackersElo = 0;
                float teamDefendersElo = 0;

                for (int i = 0; i < playersWithElo.Count; i++)
                {
                    var (player, elo) = playersWithElo[i];
                    int teamId;

                    if (i % 2 == 0)
                    {
                        if (teamAttackersElo <= teamDefendersElo)
                        {
                            teamId = 0;
                            teamAttackers.Add(new CGGOPlayer(player, teamId));
                            teamAttackersElo += elo;
                        }
                        else
                        {
                            teamId = 1;
                            teamDefenders.Add(new CGGOPlayer(player, teamId));
                            teamDefendersElo += elo;
                        }
                    }
                    else
                    {
                        if (teamDefendersElo <= teamAttackersElo)
                        {
                            teamId = 1;
                            teamDefenders.Add(new CGGOPlayer(player, teamId));
                            teamDefendersElo += elo;
                        }
                        else
                        {
                            teamId = 0;
                            teamAttackers.Add(new CGGOPlayer(player, teamId));
                            teamAttackersElo += elo;
                        }
                    }
                }

                if (teamAttackers.Count > teamDefenders.Count + 1)
                {
                    var lastPlayer = teamAttackers[teamAttackers.Count - 1];
                    teamAttackers.RemoveAt(teamAttackers.Count - 1);
                    teamDefenders.Add(lastPlayer);
                }
                else if (teamDefenders.Count > teamAttackers.Count + 1)
                {
                    var lastPlayer = teamDefenders[teamDefenders.Count - 1];
                    teamDefenders.RemoveAt(teamDefenders.Count - 1);
                    teamAttackers.Add(lastPlayer);
                }

                Variables.cggoPlayersList.AddRange(teamAttackers);
                Variables.cggoPlayersList.AddRange(teamDefenders);

                float totalTeamAttackersElo = (teamAttackers.Sum(p => p.Elo) / (float)teamAttackers.Count());
                float totalTeamDefendersElo = (teamDefenders.Sum(p => p.Elo) / (float)teamDefenders.Count());
                float deltaElo = totalTeamAttackersElo - totalTeamDefendersElo;

                if (deltaElo > 200 || deltaElo < -200) Variables.isCGGORanked = false;

                SetGameRankData();
                Reset();
                if (Variables.tuto && !Variables.isTutoDone)
                {
                    if (Variables.tuto) StartTutorial();
                    Variables.isCGGORanked = false;
                }
            }

            public static void Reset()
            {
                Variables.cggoScore = [0, 0];
                Variables.loseStrike = new(-1, 0);
                publicAllAttackersDead = false;
                publicAllDefendersDead = false;
            }
            void StartTutorial()
            {
                SetCurrentPhaseTuto();
                CreateCinematicDummy();
                SpawnBomb();
                InitTutorialMessages();
            }

            //To update to add more language
            void InitTutorialMessages()
            {
                tutorialCurrentMessages = new string[CGGOMessages.GetTutorialMessageInSpecificLanguage("EN").Length];
                for (int i = 0; i < tutorialCurrentMessages.Length; i++)
                {
                    tutorialCurrentMessages[i] = "";
                }
                tutorialCurrentMessageIndex = 0;
                tutorialStartTime = Time.time;
            }

            void CreateCinematicDummy()
            {
                Variables.weaponId++;
                cinematicDummy = Variables.weaponId;
                CreateDummy(currentMap.SpawnTeamAttackers, cinematicDummy);
            }

            void SpawnBomb()
            {
                try 
                {
                    Variables.weaponId++;
                    bombSpawnedId = (ulong)Variables.weaponId;
                    CreateDummy(currentMap.BombSpawnPosition, Variables.weaponId);
                    try
                    {
                        ServerSend.DropItem(bombSpawnedId, 5, 500000000, 0);
                    }
                    catch { }
                    bombId = 500000000;
                    publicBombId = bombId;
                }
                catch { }
            }

            void DropDefusers()
            {
                if (defusersDropped) return;
                defusersDropped = true;
                foreach (var player in Variables.cggoPlayersList)
                {
                    try
                    {
                        Variables.weaponId++;
                        if (player.Team == 1)
                        {
                            GameServer.ForceGiveWeapon(player.SteamId, 9, Variables.weaponId);
                        }
                    }
                    catch { }
                }
            }

            void SendShieldValue()
            {
                if (elapsedShield < 0.5f) return;
                foreach (var player in Variables.cggoPlayersList) ServerSend.SendGameModeTimer(player.SteamId, player.Shield, 0);
                elapsedShield = 0f;
                
            }
            bool IsPointOnSite(Vector3 point, Vector3 corner1, Vector3 corner2, float marginOfError)
            {
                float minX = Mathf.Min(corner1.x, corner2.x) - marginOfError;
                float maxX = Mathf.Max(corner1.x, corner2.x) + marginOfError;
                float minY = Mathf.Min(corner1.y, corner2.y) - marginOfError;
                float maxY = Mathf.Max(corner1.y, corner2.y) + marginOfError;
                float minZ = Mathf.Min(corner1.z, corner2.z) - marginOfError;
                float maxZ = Mathf.Max(corner1.z, corner2.z) + marginOfError;

                bool isInX = point.x >= minX && point.x <= maxX;
                bool isInY = point.y >= minY && point.y <= maxY;
                bool isInZ = point.z >= minZ && point.z <= maxZ;

                return isInX && isInY && isInZ;
            }

            void KillGodModPlayers()
            {
                List<CGGOPlayer> GodModPlayers = Variables.cggoPlayersList.Where(p => !p.Dead && p.DamageTaken >= 100).ToList();

                foreach (var player in GodModPlayers) 
                {
                    try
                    {
                        ServerSend.PlayerDied(player.SteamId, player.SteamId, Vector3.zero);
                    }
                    catch { }
                }
            }

            void CheckAndRemoveDisconnectPlayers()
            {
                foreach (var player in Variables.cggoPlayersList)
                {
                    if (player == null) continue;
                    if (!Variables.playersList.ContainsKey(player.SteamId)) Variables.cggoPlayersList.Remove(player);
                }
            }

            void GetAndSetTeamList()
            {
                publicAttackersList.Clear();
                publicDefendersList.Clear();

                publicAttackersList = Variables.cggoPlayersList.Where(p => p.Team == 0).ToList();
                publicDefendersList = Variables.cggoPlayersList.Where(p => p.Team == 1).ToList();
            }

            bool IsRound1or6()
            {
                return totalTeamScore == 0 || totalTeamScore == 5;
            }
            void ManageBonus()
            {
                if (IsCurrentPhaseBuying() && elapsedBuyPhase > 2f)
                {
                    GetAndSetTeamList();

                    int playersDelta = publicAttackersList.Count - publicDefendersList.Count;
                    if (bonusGiven || playersDelta == 0) return;

                    if (playersDelta > 0)
                    {
                        ApplyBonus(publicDefendersList, playersDelta);
                    }
                    else if (playersDelta < 0)
                    {
                        ApplyBonus(publicAttackersList, -playersDelta);
                    }

                    bonusGiven = true;
                }
            }

            void ApplyBonus(List<CGGOPlayer> players, int delta)
            {
                foreach (var player in players)
                {
                    if (IsRound1or6())
                    {
                        player.Shield = CalculateShield(players.Count, delta);
                    }
                    else
                    {
                        player.Balance += CalculateBalance(players.Count, delta);
                    }

                    AdjustBalance();
                }
            }

            int CalculateBalance(int playersCount, int delta)
            {
                return (int)((2000 * (float)delta) / (float)playersCount);
            }

            int CalculateShield(int playersCount, int delta)
            {
                return (int)((125 * (float)delta) / (float)playersCount);
            }

            bool AreAllAttackersDisconnected()
            {
                return publicAttackersList.Count == 0;
            }

            bool AreAllDefendersDisconnected()
            {
                return publicDefendersList.Count == 0;
            }

            void CheckIntegrityOfCGGOPlayers()
            {

                if (!Variables.makeCGGOTeam) return;

                CheckAndRemoveDisconnectPlayers();
                KillGodModPlayers();
                ManageBonus();


                if (AreAllAttackersDisconnected()) 
                {
                    SetMaxScore(1);
                    SetGameStateFreeze();
                    GetAndSetTeamList();
                    EndGameRankPlayers();
                    SetCurrentPhaseGameEnded();
                }
                if (AreAllDefendersDisconnected())
                {
                    SetMaxScore(0);
                    SetGameStateFreeze();
                    GetAndSetTeamList();
                    EndGameRankPlayers();
                    SetCurrentPhaseGameEnded();
                }
            }

            public float cinematicSpeed = 4.0f; 
            private float tutorialStartTime; 
            private float journeyLength;

            void HideOtherPlayers()
            {
                Vector3 position = Vector3.zero;
                foreach (var player in Variables.cggoPlayersList)
                {
                  
                    if (tutorialCurrentMessageIndex < 3) position = new(56.8f, 1f, -26.2f);
                    else position = new(-1.1f, 1f, 45.5f);

                    var steamId = player.SteamId;
                    ServerSend.FreezePlayers(true);
                    ServerSend.PlayerPosition(steamId, position + new Vector3(0,500,0));
                }
            }
            class PlayerTutorialState
            {
                public int TutorialCurrentMessageIndex { get; set; } = 0;
                public int CurrentCharacterIndex { get; set; } = 0;
                public string[] TutorialCurrentMessages { get; set; }
                public bool HasFinishedCurrentMessage { get; set; } = false;
            }

            Dictionary<ulong, PlayerTutorialState> playerTutorialStates = new Dictionary<ulong, PlayerTutorialState>();
            int globalTutorialCurrentMessageIndex = 0; // Synchronisé pour tous les joueurs

            void CGGOTutoMessage()
            {
                if (elapsedTutoMessage < 0.1f || shouldRespawnTuto) return;
                elapsedTutoMessage = 0f;

                bool allPlayersFinished = true;

                foreach (var player in Variables.playersList)
                {
                    ulong steamId = player.Key;
                    string language = Utility.GetValue(steamId.ToString(), "language");
                    string[] tutorialMessages = CGGOMessages.GetTutorialMessageInSpecificLanguage(language);

                    if (!playerTutorialStates.ContainsKey(steamId))
                    {
                        playerTutorialStates[steamId] = new PlayerTutorialState
                        {
                            TutorialCurrentMessages = new string[tutorialMessages.Length]
                        };
                    }

                    PlayerTutorialState tutorialState = playerTutorialStates[steamId];

                    // Correction de la boucle d'envoi de messages vides
                    for (int i = 0; i < 10; i++)
                    {
                        Utility.SendPrivateMessageWithWaterMark(steamId, "");
                    }
                    Utility.SendPrivateMessageWithWaterMark(steamId, "---- CRAB GAME : GLOBAL OFFENSIVE ----");

                    // Vérification si l'index est dans les limites du tableau
                    if (tutorialState.TutorialCurrentMessageIndex < tutorialMessages.Length)
                    {
                        if (tutorialState.CurrentCharacterIndex < tutorialMessages[tutorialState.TutorialCurrentMessageIndex].Length)
                        {
                            tutorialState.TutorialCurrentMessages[tutorialState.TutorialCurrentMessageIndex] += tutorialMessages[tutorialState.TutorialCurrentMessageIndex][tutorialState.CurrentCharacterIndex];
                            tutorialState.CurrentCharacterIndex++;
                            tutorialState.HasFinishedCurrentMessage = false;
                        }
                        else
                        {
                            tutorialState.HasFinishedCurrentMessage = true;
                        }
                    }

                    if (!tutorialState.HasFinishedCurrentMessage)
                    {
                        allPlayersFinished = false;
                    }

                    DisplayCurrentMessages(steamId, tutorialState.TutorialCurrentMessages);
                }

                if (allPlayersFinished)
                {
                    globalTutorialCurrentMessageIndex++;

                    // Modification: vérification que tous les joueurs ont lu tous les messages avant de charger la carte
                    if (globalTutorialCurrentMessageIndex >= 6 && AllPlayersFinishedTutorial())
                    {
                        Variables.isTutoDone = true;
                        ResetGameVariablesForTutorial();
                        LoadRandomMap();
                    }

                    tutorialCurrentMessageIndex++;
                    foreach (var state in playerTutorialStates.Values)
                    {
                        state.TutorialCurrentMessageIndex = globalTutorialCurrentMessageIndex;
                        state.CurrentCharacterIndex = 0;
                        state.HasFinishedCurrentMessage = false;
                    }
                }
            }

            bool AllPlayersFinishedTutorial()
            {
                foreach (var tutorialState in playerTutorialStates.Values)
                {
                    // Vérification que chaque joueur a fini le dernier message
                    if (tutorialState.TutorialCurrentMessageIndex < 6 || !tutorialState.HasFinishedCurrentMessage)
                    {
                        return false;
                    }
                }
                return true;
            }

            void DisplayCurrentMessages(ulong steamId, string[] tutorialCurrentMessages)
            {
                foreach (string message in tutorialCurrentMessages)
                {
                    if (string.IsNullOrEmpty(message))
                    {
                        Utility.SendPrivateMessageWithWaterMark(steamId, "");
                    }
                    else
                    {
                        Utility.SendPrivateMessageWithWaterMark(steamId, message);
                    }
                }
            }

            void CreateDummy(Vector3 position, int dummyId)
            {
                // Utilisation de votre fonction createDummy pour créer un dummy à la position spécifiée
                Packet packet = new Packet(17); //17 est gameSpawnPlayer

                // Utilisation du dummyId passé en paramètre
                packet.Method_Public_Void_UInt64_0((ulong)dummyId);
                packet.Method_Public_Void_Vector3_0(position);
                // Ajouter une valeur incorrecte dans le packet pour le casser.
                packet.Method_Public_Void_Vector3_0(Vector3.zero);
                ServerSend.Method_Private_Static_Void_ObjectPublicIDisposableLi1ByInByBoUnique_0(packet);

                packet = new Packet(17);
            }
            void RespawnManagerCGGOTuto()
            {
                switch (tutorialCurrentMessageIndex)
                {
                    case 0:
                        if (!tutoStep0)
                        {
                            tutoStep0 = true;
                            respawnPositionTuto = new(56.8f, 1f, -26.2f);
                            shouldRespawnTuto = true;
                        }
                        break;
                    case 3:
                        if (!tutoStep3)
                        {
                            tutoStep3 = true;
                            respawnPositionTuto = new(-1.1f, 1f, 45.5f);
                            shouldRespawnTuto = true;
                        }
                        break;
                }

                if (shouldRespawnTuto)
                {
                    if (elaspedRespawnTuto > 0.8f)
                    {
                        nextPlayerTuto = true;
                        elaspedRespawnTuto = 0;
                        respawnIndexTuto++;
                    }

                    if (respawnIndexTuto == Variables.cggoPlayersList.Count)
                    {
                        shouldRespawnTuto = false;
                        respawnIndexTuto = 0;
                    }

                    if (Variables.cggoPlayersList[respawnIndexTuto] != null && nextPlayerTuto)
                    {
                        var steamId = Variables.cggoPlayersList[respawnIndexTuto].SteamId;
                        ServerSend.RespawnPlayer(steamId, respawnPositionTuto);
                        nextPlayerTuto = false;
                    }
                    return;
                }

            }
     
            public class MapBitterBeach : Map
            {
                public override int MapId => 0;
                public override Vector3 BombSpawnPosition => new(44.6f, 30f, -32f);
                public override Vector3 MilkZoneAcorner1 => new(-25.2f, 1.9f, -19.2f);
                public override Vector3 MilkZoneAcorner2 => new(-38.8f, 6f, -32.8f);
                public override Vector3 MilkZoneBcorner1 => new(-20.0f, -4.1f, 36.3f);
                public override Vector3 MilkZoneBcorner2 => new(3.8f, 2.6f, 51.8f);
                public override Vector3 SpawnTeamAttackers => new(49.0f, -4.1f, -41.0f);
                public override Vector3 SpawnDirectionTeamAttackers => new(-3, 0, 0);
                public override Vector3 SpawnTeamDefenders => new(-43.0f, -4.1f, 51.0f);
                public override Vector3 SpawnDirectionTeamDefenders => new(0, 0, -3);

            }

            public class MapCockyContainers : Map
            {
                public override int MapId => 2;
                public override Vector3 BombSpawnPosition => new(36.6f, -10f, 24f);
                public override Vector3 MilkZoneAcorner1 => new(36f, -25.1f, -10f);
                public override Vector3 MilkZoneAcorner2 => new(11.5f, -18.7f, -32f);
                public override Vector3 MilkZoneBcorner1 => new(-34.0f, -25.1f, 6f);
                public override Vector3 MilkZoneBcorner2 => new(-48f, -18.7f, 47f);
                public override Vector3 SpawnTeamAttackers => new(44f, -25.1f, 16.0f);
                public override Vector3 SpawnDirectionTeamAttackers => new(0, 0, 3);
                public override Vector3 SpawnTeamDefenders => new(-47.0f, -25.1f, 2.0f);
                public override Vector3 SpawnDirectionTeamDefenders => new(0, 0, -3);

            }

            public class MapReturnToMonke : Map
            {
                public override int MapId => 20;
                public override Vector3 BombSpawnPosition => new(51f, 15f, -20f);
                public override Vector3 MilkZoneAcorner1 => new(-49f, -5.1f, -21f);
                public override Vector3 MilkZoneAcorner2 => new(-33.5f, 0f, -7f);
                public override Vector3 MilkZoneBcorner1 => new(-38f, -1.1f, 11f);
                public override Vector3 MilkZoneBcorner2 => new(-17f, 0.3f, 33f);
                public override Vector3 SpawnTeamAttackers => new(54.0f, -5.1f, -31.0f);
                public override Vector3 SpawnDirectionTeamAttackers => new(0, 0, 3);
                public override Vector3 SpawnTeamDefenders => new(-48f, -5.1f, 49f);
                public override Vector3 SpawnDirectionTeamDefenders => new(0, 0, -3);

            }

            public class MapFunkyField : Map
            {
                public override int MapId => 7;
                public override Vector3 BombSpawnPosition => new(-47f, -5.8f, 3f);
                public override Vector3 MilkZoneAcorner1 => new(20.8f, -8.1f, -24.8f);
                public override Vector3 MilkZoneAcorner2 => new(41.2f, -5.7f, -41.2f);
                public override Vector3 MilkZoneBcorner1 => new (13.8f, -23.3f, 50f);
                public override Vector3 MilkZoneBcorner2 => new(-13.8f, -21.8f, 25.5f);
                public override Vector3 SpawnTeamAttackers => new(-48.0f, -23.1f, 11f);
                public override Vector3 SpawnDirectionTeamAttackers => new(0, 0, 3);
                public override Vector3 SpawnTeamDefenders => new(31.4f, -23.1f, 35.0f);
                public override Vector3 SpawnDirectionTeamDefenders => new(0, 0, -3);
            }
            public class MapSnowTop : Map
            {
                public override int MapId => 29;
                public override Vector3 BombSpawnPosition => new(-7f, 70f, -33f);
                public override Vector3 MilkZoneAcorner1 => new(31.8f, 73.9f, -46.8f);
                public override Vector3 MilkZoneAcorner2 => new(54.1f, 77f, -27.2f);
                public override Vector3 MilkZoneBcorner1 => new(-23.5f, 56.9f, 25f);
                public override Vector3 MilkZoneBcorner2 => new(-8.5f, 60.9f, 31.5f);
                public override Vector3 SpawnTeamAttackers => new(-19.0f, 55.9f, -40.0f);
                public override Vector3 SpawnDirectionTeamAttackers => new(3, 0, 0);
                public override Vector3 SpawnTeamDefenders => new(-39.0f, 69.9f, 46.0f);
                public override Vector3 SpawnDirectionTeamDefenders => new(3, 0, 0);
            }
            public abstract class Map
            {
                public abstract int MapId { get; } //Map Id
                public abstract Vector3 BombSpawnPosition { get; } //Define the spawn position of the spike (close to Attackers) add +20 to y
                public abstract Vector3 MilkZoneAcorner1 { get; } //MUST Always define the ground level very important !!  
                public abstract Vector3 MilkZoneAcorner2 { get; } //MUST Always define the max height of the zone very important !! (for exemple to avoid spawning spike on box)
                public abstract Vector3 MilkZoneBcorner1 { get; } //Same condition as MilkZoneA
                public abstract Vector3 MilkZoneBcorner2 { get; } //Same condition as MilkZoneA
                public abstract Vector3 SpawnTeamAttackers { get; } //Define where the first attacker spawn
                public abstract Vector3 SpawnDirectionTeamAttackers { get; } //Define the direction in which other attackers will spawn relative to first attacker
                public abstract Vector3 SpawnTeamDefenders { get; } //Define where the first defender spawn
                public abstract Vector3 SpawnDirectionTeamDefenders { get; } //Define the direction in which other defenders will spawn relative to first defender
            }
            public class CGGOPlayer
            {
                public ulong SteamId { get; set; }
                public string Username { get; set; }
                public int Balance { get; set; }
                public int Team { get; set; }
                public bool Dead { get; set; }
                public bool Katana { get; set; }
                public bool Classic { get; set; }
                public bool Shorty { get; set; }
                public bool Vandal { get; set; }
                public int Kills { get; set; }
                public int Deaths { get; set; }
                public int Assists { get; set; }
                public List<CGGOPlayer> Assisters { get; set; }
                public int KatanaId { get; set; }
                public int KnifeId { get; set; }
                public bool Revolver { get; set; }
                public int Shield { get; set; }
                public int DamageTaken { get; set; }
                public ulong Killer { get; set; }
                public int CheatFlag { get; set; }
                public string Language { get; set; }
                public int Defuse { get; set; }
                public int Shot { get; set; }
                public int Headshot { get; set; }
                public int Bodyshot { get; set; }
                public int Legsshot { get; set; }
                public int DamageDealt { get; set; }
                public int DamageReceived { get; set; }
                public int MoneyUsed {  get; set; }
                public int MoneyReceived { get; set; }
                public int EndGameRank { get; set; }
                public float Elo { get; set; }
                public double Score { get; set; }
                public int CurrentPhaseMessageState { get; set; }

            public CGGOPlayer(PlayerManager player, int team)
                {
                    ulong steamId = player.steamProfile.m_SteamID;
                    SteamId = steamId;
                    Username = player.username;
                    Balance = 0;
                    Team = team;
                    Dead = false;
                    Katana = false;
                    Classic = false;
                    Shorty = false;
                    Vandal = false;
                    Revolver = false;
                    Kills = 0;
                    Deaths = 0;
                    Assists = 0;
                    Assisters = [];
                    KatanaId = 0;
                    KnifeId = 0;
                    Shield = 0;
                    DamageTaken = 0;
                    Killer = 0;
                    CheatFlag = 0;
                    Language = Utility.GetValue(steamId.ToString(), "language");
                    Defuse = 0;
                    Shot = 0;
                    Headshot = 0;
                    Bodyshot = 0;
                    Legsshot = 0;
                    DamageDealt = 0;
                    DamageReceived = 0;
                    MoneyUsed = 0;
                    MoneyReceived = 0;
                    EndGameRank = 0;
                    Score = 0;
                    CurrentPhaseMessageState = 0;
                    float playerElo = 1000;
                    string playerEloCheck = Utility.GetValue(steamId.ToString(), "elo");

                    if (playerEloCheck == "NaN")
                        playerEloCheck = Utility.GetValue(steamId.ToString(), "lastElo");

                    if (playerEloCheck != "NaN")
                        playerElo = float.Parse(playerEloCheck);
                    Elo = playerElo;

                }
                public static CGGOPlayer GetCGGOPlayer(ulong id)
                {
                    return Variables.cggoPlayersList.Find(player => player.SteamId == id);
                }
            }
        }

        public class CGGOPlayerRanking
        {
            public static (List<CGGOPlayer> rankedPlayers, List<double> playerScores) RankPlayers(List<CGGOPlayer> playersList, bool shouldEditScore)
            {
                double totalKills = playersList.Sum(p => p.Kills);
                double totalDeaths = playersList.Sum(p => p.Deaths);
                double totalAssists = playersList.Sum(p => p.Assists);
                var playerScoresList = playersList
                .Select(p => new { Player = p, Score = CalculatePlayerScore(p, totalKills, totalDeaths, totalAssists) })
                .OrderByDescending(p => p.Score)
                .ToList();

                List<CGGOPlayer> rankedPlayers = playerScoresList.Select(p => p.Player).ToList();
                List<double> playerScores = playerScoresList.Select(p => p.Score).ToList();

                if (shouldEditScore)
                {
                    for (int i = 0; i < rankedPlayers.Count(); i++)
                    {
                        rankedPlayers[i].Score = playerScores[i];
                    }
                }
                return (rankedPlayers, playerScores);
            }

            private static double CalculatePlayerScore(CGGOPlayer player, double totalKills, double totalDeaths, double totalAssists)
            {
                double headshotRatio = player.Shot > 0 ? (double)player.Headshot / player.Shot : 0;
                double bodyshotRatio = player.Shot > 0 ? (double)player.Bodyshot / player.Shot : 0;
                double legsshotRatio = player.Shot > 0 ? (double)player.Legsshot / player.Shot : 0;
                double moneyEfficiency = player.MoneyReceived > 0 ? (double)player.MoneyUsed / player.MoneyReceived : 0;
                double defuseEfficiency = (double)player.Defuse / 6;
                double damageEfficiency = player.DamageReceived > 0 ? (double)player.DamageDealt / (double)(player.DamageReceived + player.DamageDealt) : 1;
                double killContribution = totalKills > 0 ? (double)player.Kills / totalKills : 0;
                double deathContribution = totalDeaths > 0 ? (double)player.Deaths / totalDeaths : 0;
                double assistContribution = totalAssists > 0 ? (double)player.Assists / totalAssists : 0;

                // Assigning weights to each criterion
                double headshotWeight = 0.2;
                double bodyshotWeight = 0.1;
                double legsshotWeight = 0.05;
                double moneyEfficiencyWeight = 0.1;
                double defuseWeight = 0.05;
                double damageEfficiencyWeight = 0.2;
                double killContributionWeight = 0.1;
                double deathContributionWeight = 0.15;
                double assistContributionWeight = 0.05;

                // Calculate the player's score
                return (headshotRatio * headshotWeight) +
                       (bodyshotRatio * bodyshotWeight) +
                       (legsshotRatio * legsshotWeight) +
                       ((1 - moneyEfficiency) * moneyEfficiencyWeight) +
                       (defuseEfficiency * defuseWeight) +
                       (damageEfficiency * damageEfficiencyWeight) +
                       (killContribution * killContributionWeight) +
                       ((1 - deathContribution) * deathContributionWeight) +
                       (assistContribution * assistContributionWeight);
            }
        }

        //OnSnowballUse (Defuser)
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.UseItemAll))]
        [HarmonyPrefix]
        public static bool OnSnowballUseItemAllPre(ref ulong __0, ref int __1)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return true;
            if (__1 == 9)
            {
                __0 = 2;
                __1 = -1;
                return false;
            }
            else return true;
        }

        //Disable snowballPiles
        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPrefix]
        public static bool OnForceGiveWeaponPre(ulong param_0, int param_1, int param_2)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return true;

            var player = CGGO.CGGOPlayer.GetCGGOPlayer(param_0);
            if (player == null) return true;
            else
            {
                if (!publicBuyPhase)
                {
                    if (param_1 == 9) return false;
                    else return true;
                }

                if (player.Team == 1 && param_1 == 5) return false;

            }
            return true;
        }

        //WeaponMemory
        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPostfix]
        public static void OnForceGiveWeaponPost(ulong param_0, int param_1, int param_2)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return;

            var player = CGGO.CGGOPlayer.GetCGGOPlayer(param_0);
            if (player == null) return;
            else
            {
                switch (param_1)
                {
                    case 0:
                        player.Vandal = true;
                        return;
                    case 1:
                        player.Classic = true;
                        return;
                    case 2:
                        player.Revolver = true;
                        return;
                    case 3:
                        player.Shorty = true;
                        return;
                    case 6:
                        player.Katana = true;
                        return;
                    default: return;
                }
            }
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.DropItem))]
        [HarmonyPostfix]
        public static void OnDropItemPost(ulong param_0, int param_1, int param_2, int param_3)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return;

            var player = CGGO.CGGOPlayer.GetCGGOPlayer(param_0);
            if (player == null) return;
            else
            {
                switch (param_1)
                {
                    case 0:
                        player.Vandal = false;
                        return;
                    case 1:
                        player.Classic = false;
                        return;
                    case 2:
                        player.Revolver = false;
                        return;
                    case 3:
                        player.Shorty = false;
                        return;
                    case 6:
                        player.Katana = false;
                        return;
                    default: return;
                }
            }
        }

        // Manage advanced interaction
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.Interact))]
        [HarmonyPrefix]
        public static bool OnInteractPre(ref ulong __0, ref int __1)
        {
            // __0 = steamId, __1 = itemId
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return true;

            var player = CGGOPlayer.GetCGGOPlayer(__0);
            if (player == null) return true;

            // Check if the item is the bomb
            if (__1 == 500000001 || __1 == 500000000)
            {
                if (player.Team == 0)
                {
                    if (__1 == 500000000)
                    {
                        if (player.Katana)
                        {
                            Variables.itemToDelete.Add(player.KatanaId, DateTime.Now);
                            DeleteItem.deleteTime = 100;
                        }
                        else
                        {
                            Variables.itemToDelete.Add(player.KnifeId, DateTime.Now);
                            DeleteItem.deleteTime = 100;
                        }
                    }
                    return true; // Let Attackers grab the bomb
                }
                else return false; // Do not let defenders grab the bomb
            }
            else return true;
        }


        // Bomb to Grenade
        // WeaponPickUp
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.Interact))]
        [HarmonyPostfix]
        public static void OnInteractPost(ref ulong param_0, ref int param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return;

            var player = CGGOPlayer.GetCGGOPlayer(param_0);
            if (player == null) return;

            if (player.Team == 0 && param_1 == 500000000)
            {
                // Force remove the item
                GameServer.ForceRemoveItem(param_0, param_1);


                // Assign new weapon IDs and update the player's weapon
                GameServer.ForceGiveWeapon(param_0, 13, 500000001);
                publicGrenadeId = 500000001;
                publicBombId = 500000002;

                // Give the player the appropriate weapon based on whether they have a Katana
                
                if (player.Katana)
                {
                    Variables.weaponId++;
                    GameServer.ForceGiveWeapon(param_0, 6, Variables.weaponId);
                    player.KatanaId = Variables.weaponId;
                    publicKatanaList.Add(Variables.weaponId);
                }
                else
                {
                    Variables.weaponId++;
                    GameServer.ForceGiveWeapon(param_0, 7, Variables.weaponId);
                }

                return;
            }
            else
            {
                UpdatePlayerWeaponStatus(param_1, player);
                return;
            }

            static void UpdatePlayerWeaponStatus(int param_1, CGGOPlayer player)
            {
                if (publicClassicList.Contains(param_1))
                {
                    player.Classic = true;
                }
                if (publicShortyList.Contains(param_1))
                {
                    player.Shorty = true;
                }
                if (publicVandalList.Contains(param_1))
                {
                    player.Vandal = true;
                }
                if (publicKatanaList.Contains(param_1))
                {
                    player.Katana = true;
                }
                if (publicRevolverList.Contains(param_1))
                {
                    player.Revolver = true;
                }
            }

        }

        

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.DropItem))]
        [HarmonyPrefix]
        public static bool OnDropItemPre(ulong param_0, int param_1,ref int param_2, int param_3)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return true;

            var player = CGGOPlayer.GetCGGOPlayer(param_0);
            if (player == null) return true;
            else
            {
                if (param_2 == 500000000 || param_2 == 500000001)
                {
                    if (player.Team == 0) return true;
                    else 
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        // Dont let the timer UI update, to display shield instead
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendGameModeTimer), [typeof(ulong), typeof(float), typeof(int)])]
        [HarmonyPrefix]
        public static bool OnServerSendTimerPre(ulong __0, ref float __1, int __2)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return true;

            var cggoPlayer = CGGOPlayer.GetCGGOPlayer(__0);
            if (cggoPlayer != null)
            {
                // Update shield value instead of round timer
                if (__1 == cggoPlayer.Shield) return true;
                else return false;
            }
            else return true;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDamage))]
        [HarmonyPrefix]
        internal static void OnPlayerDamagePre(ref ulong __0, ref ulong __1, ref int __2, ref Vector3 __3, ref int __4)
        {
            // __0 = damagerId, __1 = damagedId, __2 = damageAmount, __3 = bulletDirection, __4 = weaponId

            // Validate execution conditions
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive || __4 == -1) return;

            // Retrieve players
            CGGOPlayer damager = CGGOPlayer.GetCGGOPlayer(__0);
            CGGOPlayer damaged = CGGOPlayer.GetCGGOPlayer(__1);

            if (damager == null || damaged == null) return;

            // Update the list of hit players, help to find who is the last person who hitted a player
            UpdateHitPlayers(__0, __1);

            // Get PlayerManager to obtain position information, etc.   
            PlayerManager damagerPlayer = GameData.GetPlayer(__0.ToString());
            PlayerManager damagedPlayer = GameData.GetPlayer(__1.ToString());

            if (damagerPlayer == null || damagedPlayer == null) return;

            Vector3 damagerPos = damagerPlayer.gameObject.transform.position;
            Vector3 damagedPos = damagedPlayer.gameObject.transform.position;

            Quaternion damagerRot = damagerPlayer.GetRotation();
            float shootDistance = Vector3.Distance(damagerPos, damagedPos);


            // Check for friendly fire
            if (IsFriendlyFire())
            {
                __2 = 0; // No damage 
                __4 = 9; // Weapon is set as a snowball
            }
            else
            {
                int damage;
                string type;
                float impactHeight;

                switch (__4)
                {
                    case 6:
                        AttributeDamage(80);
                        ManageAssists(80);
                        __2 = ManageDamage(80);
                        __4 = -1;
                        __0 = __1;
                        break;
                    case 7:
                        AttributeDamage(34);
                        ManageAssists(34);
                        __2 = ManageDamage(34);
                        __4 = -1;
                        __0 = __1;
                        break;
                    case 0:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateVandalDamage(shootDistance, type);
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = -1;
                        __0 = __1;
                        break;
                    case 1:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateClassicDamage(shootDistance, type);
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = -1;
                        __0 = __1;
                        break;
                    case 2:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateRevolverDamage(shootDistance, type);
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = -1;
                        __0 = __1;
                        break;
                    case 3:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateShortyDamage(shootDistance, type);
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = -1;
                        __0 = __1;
                        break;
                }
            }

            void UpdateHitPlayers(ulong damagerId, ulong damagedId)
            {
                if (Variables.hitPlayers.ContainsKey(damagedId))
                {
                    Variables.hitPlayers[damagedId] = new KeyValuePair<ulong, DateTime>(damagerId, DateTime.Now);
                }
                else
                {
                    Variables.hitPlayers.Add(damagedId, new KeyValuePair<ulong, DateTime>(damagerId, DateTime.Now));
                }
            }

            bool IsFriendlyFire()
            {
                return damager.Team == damaged.Team && damager.SteamId != damaged.SteamId;
            }

            void AttributeDamage(int damage)
            {
                damaged.DamageReceived += damage;
                damager.DamageDealt += damage;
            }
            void ManageAssists(int damage)
            {
                if (damage == 0) return;
                if (!damaged.Assisters.Contains(damager))
                    damaged.Assisters.Add(damager);
            }

            int CalculateClassicDamage(float distance, string type)
            {
                if (distance > 100) return 0;

                switch (type)
                {
                    case "legs":
                        if (distance > 60) return 12;
                        else return 14;
                    case "body":
                        if (distance > 60) return 18;
                        else return 22;
                    case "head":
                        if (distance > 60) return 42;
                        else return 58;
                    default: return 0;
                }
            }

            int CalculateShortyDamage(float distance, string type)
            {
                if (distance > 100) return 0;

                switch (type)
                {
                    case "legs":
                        if (distance > 30) return 14;
                        else if (distance > 15) return 21;
                        else return 42;
                    case "body":
                        if (distance > 30) return 35;
                        else if (distance > 15) return 42;
                        else return 84;
                    case "head":
                        if (distance > 30) return 63;
                        else if (distance > 15) return 77;
                        else return 154;
                    default: return 0;
                }
            }

            int CalculateRevolverDamage(float distance, string type)
            {
                switch (type)
                {
                    case "legs":
                        return (int)(distance * 0.5f);
                    case "body":
                        return (int)(distance);
                    case "head":
                        return (int)(distance * 2f);
                    default: return 0;
                }
            }

            int CalculateVandalDamage(float distance, string type)
            {
                if (distance > 100) return 0;

                return type switch
                {
                    "legs" => 12,
                    "body" => 20,
                    "head" => 62,
                    _ => 0,
                };
            }

            float CalculateImpactHeight(Vector3 shooterPosition, Quaternion shooterRotation, Vector3 targetPosition)
            {
                float targetHeight = 3f;
                Vector3 targetCenter = targetPosition + new Vector3(0, targetHeight / 2, 0);

                Vector3 shotDirection = shooterRotation * Vector3.forward;

                float distance = Vector3.Distance(shooterPosition, targetCenter);
                Vector3 impactPosition = shooterPosition + shotDirection * distance;

                float impactHeightRelativeToFeet = impactPosition.y - targetPosition.y;

                return impactHeightRelativeToFeet + 3;
            }

            string ImpactHeightToType(float impactHeight)
            {
                if (impactHeight > 2.750f)
                {
                    damager.Headshot += 1;
                    return "head";
                }
                if (impactHeight > 1.48f)
                {
                    damager.Bodyshot += 1;
                    return "body";
                }
                else
                {
                    damager.Legsshot += 1;
                    return "legs";
                }
            }

            int ManageDamage(int damage)
            {
                if (damaged.Shield > 0)
                {
                    int shieldDamage = (int)(damage * 0.66);
                    int healthDamage = damage - shieldDamage;

                    if (damaged.Shield < shieldDamage)
                    {
                        healthDamage += (shieldDamage - damaged.Shield);
                        shieldDamage = damaged.Shield;
                    }

                    damaged.Shield -= shieldDamage;
                    if (damaged.Shield < 0) damaged.Shield = 0;

                    damaged.DamageTaken += healthDamage;

                    if (damaged.DamageTaken >= 100) damaged.Killer = damager.SteamId;

                    Utility.Log(Variables.logFilePath, $"{damager.Username} damaged {damaged.Username}, shield damage: {shieldDamage}, body damage: {healthDamage}");
                    return healthDamage;
                }
                else
                {
                    damaged.DamageTaken += damage;
                    if (damaged.DamageTaken >= 100) 
                    {
                        Utility.Log(Variables.logFilePath, $"{damager.Username} damaged and killed {damaged.Username}, shield damage: 0, body damage: {damage}");
                        damaged.Killer = damager.SteamId;
                    }
                    else Utility.Log(Variables.logFilePath, $"{damager.Username} damaged {damaged.Username}, shield damage: 0, body damage: {damage}");
                   
                    return damage;
                }
            }
        }

        public class DeleteItem : MonoBehaviour
        {
            bool init;
            public static int deleteTime = 3000;

            void Update()
            {
                if (!init)
                {
                    Variables.itemToDelete.Clear();
                    init = true;
                }

                var dico = SharedObjectManager.Instance.GetDictionary(); // Récupération du dictionnaire partagé
                List<int> itemsToRemove = new List<int>();

                // Vérification des items à supprimer
                foreach (var item in Variables.itemToDelete)
                {
                    if ((DateTime.Now - item.Value).TotalMilliseconds > deleteTime)
                    {
                        itemsToRemove.Add(item.Key);
                    }
                }

                // Suppression des objets
                foreach (var key in itemsToRemove)
                {
                    // On vérifie si la clé existe dans le dictionnaire
                    if (!dico.ContainsKey(key)) continue;

                    Variables.itemToDelete.Remove(key);

                    // Récupération de l'objet partagé à l'aide de la clé
                    var obj = SharedObjectManager.Instance.GetSharedObject(key);

                    // Si l'objet n'existe pas, on continue
                    if (obj == null) continue;

                    // Récupération et interaction avec les composants spécifiques
                    var comp1 = obj.GetComponent<ItemGun>();
                    var comp2 = obj.GetComponent<MonoBehaviour1PublicInamUnique>();
                    var comp3 = obj.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
                    var comp4 = obj.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();
                    var comp5 = obj.GetComponent<MonoBehaviour2PublicUnique>();

                    if (comp1 != null)
                    {
                        comp1.TryInteract();
                    }
                    if (comp2 != null)
                    {
                        comp2.TryInteract();
                    }
                    if (comp3 != null)
                    {
                        comp3.TryInteract();
                    }
                    if (comp4 != null)
                    {
                        comp4.TryInteract();
                    }
                    if (comp5 != null)
                    {
                        comp5.TryInteract();
                    }

                    // Forcer la suppression de l'item sur le serveur
                    GameServer.ForceRemoveItem(Variables.clientId, key);
                }
            }
        }


        public class MenuManager : MonoBehaviour
        {
            DateTime lastActionTime = DateTime.Now;

            private const int WAIT = 150;
            // Importation des fonctions de la librairie user32.dll
            [DllImport("user32.dll")]
            private static extern short GetKeyState(int nVirtKey);
            [DllImport("user32.dll")]
            private static extern short GetAsyncKeyState(int vKey);
            [DllImport("user32.dll")]
            private static extern int GetSystemMetrics(int nIndex);
            const int VK_RBUTTON = 0x02;
            void Update()
            {

                TimeSpan elapsed = DateTime.Now - lastActionTime;
                HandleMenuDisplays();
                HandleMenuActions(elapsed);
                HandleMenuSpeedHelper(elapsed);

                if (Input.GetMouseButtonDown(2))
                {
                    Variables.onSubButton = false;
                    Variables.onButton = false;
                    Variables.subMenuSelector = -1;
                }

            }

            private void HandleMenuDisplays()
            {
                Variables.displayButton0 = MenuFunctions.HandleMenuDisplay(0, () => "Skip Lobby", () => MenuFunctions.DisplayButtonState(0));
                Variables.displayButton1 = MenuFunctions.HandleMenuDisplay(1, () => "Skip Prematch Timer", () => MenuFunctions.DisplayButtonState(1));
                Variables.displayButton2 = MenuFunctions.HandleMenuDisplay(2, () => "Auto Server Message", () => MenuFunctions.DisplayButtonState(2));
                Variables.displayButton3 = MenuFunctions.HandleMenuDisplay(3, () => "Speak PostMortem", () => MenuFunctions.DisplayButtonState(3));
                Variables.displayButton4 = MenuFunctions.HandleMenuDisplay(4, () => "Host AFK", () => MenuFunctions.DisplayButtonState(4));
                Variables.displayButton5 = MenuFunctions.HandleMenuDisplay(5, () => "Flung Detector", () => MenuFunctions.DisplayButtonState(5)) + MenuFunctions.GetSelectedFlungDetectorParam();
                Variables.displayButton6 = MenuFunctions.HandleMenuDisplay(6, () => "Item Detector", () => MenuFunctions.DisplayButtonState(6));
                Variables.displayButton7 = MenuFunctions.HandleMenuDisplay(7, () => "Fast Fire Detector", () => MenuFunctions.DisplayButtonState(7));
            }

            private void HandleMenuActions(TimeSpan elapsed)
            {
                bool moletteH = false;
                bool moletteB = false;

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f && Variables.menuTrigger)
                {
                    moletteH = true;
                }
                else if (scroll < 0f && Variables.menuTrigger)
                {
                    moletteB = true;
                }

                if (!Variables.menuTrigger || elapsed.TotalMilliseconds < WAIT)
                {
                    if (!moletteB && !moletteH)
                        return;
                }

                Variables.menuSpeedHelperFast = 0;

                bool f5KeyPressed = moletteH;
                bool f6KeyPressed = GetAsyncKeyState(VK_RBUTTON) < 0;
                bool f7KeyPressed = moletteB;
                if (f5KeyPressed || f6KeyPressed || f7KeyPressed)
                {
                    UpdateMenuSpeed(elapsed);
                    HandleKeyActions(f5KeyPressed, f6KeyPressed, f7KeyPressed);
                    lastActionTime = DateTime.Now;
                }
            }

            private void HandleMenuSpeedHelper(TimeSpan elapsed)
            {
                if (elapsed.TotalMilliseconds >= WAIT + Variables.menuSpeedHelperFast)
                {
                    if (Variables.menuSpeedHelper > 0)
                        Variables.menuSpeedHelper -= 1;
                    Variables.menuSpeedHelperFast += WAIT;
                }
            }

            private void UpdateMenuSpeed(TimeSpan elapsed)
            {
                if (elapsed.TotalMilliseconds <= 200)
                    Variables.menuSpeedHelper += 2;
                if (Variables.menuSpeedHelper > 8)
                    Variables.menuSpeed = 5;
                else
                    Variables.menuSpeed = 1;

                Utility.PlayMenuSound();

            }

            private void HandleKeyActions(bool f5KeyPressed, bool f6KeyPressed, bool f7KeyPressed)
            {
                if (f5KeyPressed)
                {
                    HandleF5KeyPressed();
                }

                if (f6KeyPressed)
                {
                    HandleF6KeyPressed();
                }

                if (f7KeyPressed)
                {
                    HandleF7KeyPressed();
                }
            }

            private void HandleF5KeyPressed()
            {
                if (!Variables.onButton)
                    Variables.menuSelector = Variables.menuSelector > 0 ? Variables.menuSelector - 1 : Variables.buttonStates.Length - 1;
                else if (!Variables.onSubButton)
                {
                    switch (Variables.menuSelector)
                    {
                        case 4:
                            Variables.subMenuSelector = Variables.subMenuSelector > 0 ? Variables.subMenuSelector - 1 : 2;
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    switch (Variables.menuSelector)
                    {
                        case 5:
                            if (Variables.subMenuSelector == 0)
                            {
                                Variables.checkFrequency = Variables.checkFrequency - 0.01f * Variables.menuSpeed;
                                if (Variables.checkFrequency < 0)
                                    Variables.checkFrequency = 0;
                            }
                            if (Variables.subMenuSelector == 1)
                            {
                                Variables.alertLevel = Variables.alertLevel - 1 * Variables.menuSpeed;
                                if (Variables.alertLevel < 0)
                                    Variables.alertLevel = 0;
                                break;
                            }
                            break;
                        default:
                            return;
                    }
                }
            }

            private void HandleF7KeyPressed()
            {
                if (!Variables.onButton)
                    Variables.menuSelector = Variables.menuSelector < Variables.buttonStates.Length - 1 ? Variables.menuSelector + 1 : 0;
                else if (!Variables.onSubButton)
                {
                    switch (Variables.menuSelector)
                    {
                        case 5:
                            Variables.subMenuSelector = Variables.subMenuSelector < 2 ? Variables.subMenuSelector + 1 : 0;
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    switch (Variables.menuSelector)
                    {
                        case 5:
                            if (Variables.subMenuSelector == 0)
                            {
                                Variables.checkFrequency = Variables.checkFrequency < 1 ? Variables.checkFrequency + 0.01f * Variables.menuSpeed : 1;
                            }
                            if (Variables.subMenuSelector == 1)
                            {
                                Variables.alertLevel = Variables.alertLevel < 2 ? Variables.alertLevel + 1 * Variables.menuSpeed : 2;
                            }
                            break;
                        default:
                            return;
                    }
                }
            }

            public static void HandleF6KeyPressed()
            {
                if (Variables.menuSelector < Variables.buttonStates.Length)
                {
                    MenuFunctions.ExecuteSubMenuAction();

                    if (!Variables.onButton && IsSpecialMenu(Variables.menuSelector))
                        Variables.subMenuSelector = 0;

                    bool previousButtonState = Variables.buttonStates[Variables.menuSelector];
                    if (!IsSpecialMenu(Variables.menuSelector))
                    {
                        Variables.buttonStates[Variables.menuSelector] = !previousButtonState;
                        Variables.onButton = Variables.buttonStates[Variables.menuSelector];
                    }
                    else
                    {
                        Variables.onButton = true;
                    }

                    if (!IsSpecialMenu(Variables.menuSelector))
                        Variables.onButton = false;
                }
            }
            private static bool IsSpecialMenu(int menuSelector)
            {
                return menuSelector == 5;
            }
        }

        public class Selector : MonoBehaviour
        {
            float elapsed;

            void Update()
            {
                elapsed += Time.deltaTime;

                // Combine key check conditions for "left" and "right"
                if ((Input.GetKey("left") || Input.GetKey("right")) && elapsed >= 0.2f)
                {
                    Utility.PlayMenuSound();

                    if (Input.GetKey("left") && Variables.playerIndex > 0)
                        Variables.playerIndex -= 1;
                    else if (Input.GetKey("right") && Variables.playerIndex < 40)
                        Variables.playerIndex = (Variables.playerIndex < Variables.playersList.Count - 1) ? Variables.playerIndex + 1 : 0;

                    Variables.smoothedSpeed = 0;
                    Variables.lastOtherPlayerPosition = Vector3.zero;

                    elapsed = 0f;
                }
            }
        }

        public class FlungDetectorPlayersData
        {
            public string PlayerName { get; set; }
            public Vector3 ActualPosition { get; set; }
            public Vector3 LastPosition { get; set; }
            public int Actualisations { get; set; }
            public int PlayerId { get; set; }
            public Vector3 Direction { get; set; }
            public int DirectionChanges { get; set; }
        }

        public class FlungDetector : MonoBehaviour
        {
            private bool message;
            private float elapsed = 0f;
            private readonly Dictionary<string, FlungDetectorPlayersData> playersData = [];

            void Update()
            {
                if (!Variables.buttonStates[5]) return;
                if (Variables.tuto) return;
                elapsed += Time.deltaTime;

                if (elapsed > Variables.checkFrequency)
                {
                    GetAlivePlayersData();
                    elapsed = 0f;
                }
            }

            private void GetAlivePlayersData()
            {
                foreach (var player in Variables.playersList)
                {
                    var playerValue = player.Value;
                    if (playerValue != null && !playerValue.dead)
                    {
                        string playerName = playerValue.username.Trim();
                        if (!playersData.TryGetValue(playerName, out var playerData))
                        {
                            playerData = new FlungDetectorPlayersData { PlayerName = playerName };
                            playersData.Add(playerName, playerData);
                        }

                        UpdatePlayerData(playerData, playerValue);
                    }
                }
            }

            private void UpdatePlayerData(FlungDetectorPlayersData playerData, PlayerManager playerValue)
            {
                playerData.Actualisations += 1;
                playerData.LastPosition = playerData.ActualPosition;
                playerData.ActualPosition = playerValue.transform.position;
                playerData.PlayerId = playerValue.playerNumber;
                playerData.Direction = (playerData.ActualPosition - playerData.LastPosition).normalized;

                CheckPlayerMovement(playerData);
                CheckPlayerDirectionChanges(playerData);
            }

            private void CheckPlayerMovement(FlungDetectorPlayersData playerData)
            {
                float angle = Vector3.Angle(playerData.LastPosition, playerData.Direction);
                if (playerData.LastPosition != Vector3.zero && (angle >= 170 && angle <= 190))
                {
                    playerData.DirectionChanges++;
                    playerData.Actualisations = 0;
                    message = false;
                }
                else if (playerData.Actualisations > 30)
                {
                    if (playerData.DirectionChanges > 0)
                        playerData.DirectionChanges = 0;
                    playerData.Actualisations = 0;
                    message = false;
                }
            }

            private void CheckPlayerDirectionChanges(FlungDetectorPlayersData playerData)
            {
                string prob = GetProbabilityLevel(playerData.DirectionChanges);

                if (playerData.DirectionChanges > Variables.alertLevel && playerData.DirectionChanges < 4 && !message)
                {
                    if (Vector3.Distance(playerData.ActualPosition, playerData.LastPosition) > 2.5f)
                    {
                        Utility.ForceMessage("<color=red>[GAC] </color>[P] " + prob + " | [C] Flung |#" + playerData.PlayerId.ToString() + "  " + playerData.PlayerName);
                        message = true;

                        if (Variables.isCGGOActive)
                        {
                            var player = CGGOPlayer.GetCGGOPlayer(ulong.Parse(GameData.GetPlayerSteamId(playerData.PlayerName)));
                            if (player != null)
                            {
                                if (prob == "High")
                                {
                                    Utility.Log(Variables.logFilePath, $"Cheat Banned: {player.Username}, {player.SteamId} for Flung");
                                    player.CheatFlag = 10;
                                }
                            }
                        }
      
                    }
                    else playerData.DirectionChanges--;
                }
            }

            private string GetProbabilityLevel(int directionChanges)
            {
                return directionChanges switch
                {
                    1 => "Low",
                    2 => "Moderate",
                    3 => "High",
                    _ => "High",
                };
            }
        }
        
        //Cette classe gère les communications post-mortem de l'host
        public class SpeakDuringAllGame : MonoBehaviour
        {
            float elapsed = 0f;
            bool init = false, acted = false;
            void Update()
            {
                elapsed = Time.deltaTime;

                if (!Variables.buttonStates[3])
                {
                    Variables.clientIsDead = false;
                    return;
                }

                if (Variables.buttonStates[4])
                {
                    Variables.clientIsDead = true;
                }

                if (!init && Variables.clientBody != null)
                {
                    init = true;
                    ServerConfig.field_Public_Static_Single_0 = 300;
                }

                if (!init) return;


                if (Variables.gameState == "Ended")
                {
                    if (init && Variables.playersList[Variables.clientId].dead)
                        Variables.clientIsDead = true;
                }

                if (Variables.gameState == "Freeze" && Variables.modeId != 0 && Variables.clientIsDead && !acted)
                {
                    ServerConfig.field_Public_Static_Single_0 = 300;
                    if (Variables.clientBody != null)
                    {
                        var objToDestroy = GameObject.Find("GameUI/Vignettes/");
                        if (objToDestroy != null)
                        {
                            Destroy(objToDestroy);
                        }
                        PlayerStatus.Instance.DamagePlayer(999, new Vector3(0, 0, 0), 0, 0);
                        elapsed = 0;
                        acted = true;
                    }
                }
                if (Variables.gameState == "GameOver")
                {
                    Variables.clientIsDead = false;
                }
                if (Variables.clientIsDead && elapsed < 5f)
                {
                    GameObject ragdoll = GameObject.Find("PlayerRagdollNew(Clone)");
                    if (ragdoll != null)
                        Destroy(ragdoll);
                }
            }
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDied))]
        [HarmonyPrefix]
        public static void OnServerSendPlayerDiedPre(ref ulong __0, ref ulong __1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return;
            if (__0 < 1000000) return;

            if (Variables.isCGGOActive)
            {
                var cggoPlayer = CGGOPlayer.GetCGGOPlayer(__0);

                if (cggoPlayer == null) return;

                if (!cggoPlayer.Dead)
                {
                    cggoPlayer.Deaths += 1;
                    cggoPlayer.Dead = true;
                    cggoPlayer.Katana = false;
                    cggoPlayer.Classic = false;
                    cggoPlayer.Shorty = false;
                    cggoPlayer.Vandal = false;
                    cggoPlayer.Revolver = false;
                    cggoPlayer.Shield = 0;

                    int countAliveAttacker = publicAttackersList.Count(p => !p.Dead);
                    int countAliveDefender = publicDefendersList.Count(p => !p.Dead);

                    if (countAliveAttacker == 0)
                    {
                        publicAllAttackersDead = true;
                        SetGameStateFreeze();
                        HandleWeaponDrops(cggoPlayer);
                    }

                    if (countAliveDefender == 0)
                    {
                        publicAllDefendersDead = true;
                        SetGameStateFreeze();
                        HandleWeaponDrops(cggoPlayer);
                    }

                    if (cggoPlayer.Killer != 0) 
                    {
                        var cggoKiller = CGGOPlayer.GetCGGOPlayer(cggoPlayer.Killer);
                        if (cggoKiller != null)
                        {
                            foreach (var player in cggoPlayer.Assisters)
                            {
                                if (player.SteamId != cggoPlayer.Killer)
                                {
                                    player.Assists += 1;
                                    player.Balance += 50;
                                    player.MoneyReceived += 50;
                                }
                            }

                            if (cggoKiller != null)
                            {
                                cggoKiller.Balance += 200;
                                cggoKiller.MoneyReceived += 200;
                                cggoKiller.Kills += 1;
                            }
                            cggoPlayer.Killer = 0; 
                        }
                    }
                    else if (Variables.hitPlayers.ContainsKey(cggoPlayer.SteamId))
                    {
                        if ((DateTime.Now - Variables.hitPlayers[cggoPlayer.SteamId].Value).TotalMilliseconds <= 5000)
                        {
                            var cggoKiller = CGGOPlayer.GetCGGOPlayer(Variables.hitPlayers[cggoPlayer.SteamId].Key);

                            Variables.hitPlayers.Remove(cggoPlayer.SteamId);

                            if (cggoKiller.Team == cggoPlayer.Team) return;

                            if (cggoKiller != null)
                            {
                                foreach (var player in cggoPlayer.Assisters)
                                {
                                    if (player.SteamId != cggoPlayer.Killer)
                                    {
                                        player.Assists += 1;
                                        player.Balance += 50;
                                        player.MoneyReceived += 50;
                                    }
                                }

                                if (cggoKiller != null)
                                {
                                    cggoKiller.Balance += 200;
                                    cggoKiller.MoneyReceived += 200;
                                    cggoKiller.Kills += 1;
                                }
                                cggoPlayer.Killer = 0;
                            }
                        }
                        else
                        {
                            Variables.hitPlayers.Remove(cggoPlayer.SteamId);
                        }
                    }

                    cggoPlayer.Assisters = [];
                }
            }
        }


        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDied))]
        [HarmonyPostfix]
        public static void OnServerSendPlayerDiedPost(ulong __0, ulong __1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return;
            if (__0 < 1000000) return;

            PlayerManager killed = GameData.GetPlayer(__0.ToString());
            PlayerManager killer = GameData.GetPlayer(__1.ToString());

            if (killer == null || killed == null) return;

            if (__0 == __1 || __1 == 1) Utility.Log(Variables.logFilePath, $"{killed.username} died");
            else Utility.Log(Variables.logFilePath, $"{killer.username} killed {killed.username}");
   
            
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Update))]
        [HarmonyPostfix]
        internal static void GameModeUpdatePost(GameMode __instance)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive || Variables.startRound) return;

            double timer = GameData.GetCurrentGameTimer();

            if (GameData.GetGameState() == "Freeze")
            {
                if (Variables.mapId == 30 || Variables.mapId == 31)
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
                Variables.startRound = true;
            }

            void LoadRandomOrDefaultMap()
            {
                if (!Variables.tuto)
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

            if (Variables.isCGGOActive && Variables.makeCGGOTeam == true)
            {
                if (CGGOPlayer.GetCGGOPlayer(__0) == null) client.field_Public_Boolean_0 = false;
                else client.field_Public_Boolean_0 = true;
            }

            //Always spawn host if CGGO is active
            if (__0 == Variables.clientId && Variables.isCGGOActive)
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

            Variables.newPlayerToSpawn.Add((ulong)__0, DateTime.Now);

            Variables.newPlayers.Add((ulong)__0);

            if (SteamManager.Instance.IsLobbyOwner())
            {
                if (Variables.bannedPlayers.ContainsKey(__0.ToString())) LobbyManager.Instance.KickPlayer((ulong)__0);
                return;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnPlayer))]
        [HarmonyPostfix]
        public static void OnGameManagerSpawnPlayerPost(ulong __0)
        {
            if (Variables.playersList.ContainsKey(__0))
                Variables.playersList[__0] = GameData.GetPlayerFirst(__0.ToString());
        
            if (Variables.newPlayers.Contains(__0))
            {
                Variables.newPlayers.Remove(__0);
                Variables.loadingPlayers.Add(__0, DateTime.Now);
                Variables.playersList.Add(__0, GameData.GetPlayerFirst(__0.ToString()));
            }
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            // Safely remove the player from the various dictionaries and lists
            if (Variables.playersList.ContainsKey(steamId)) Variables.playersList.Remove(steamId);

            if (Variables.loadingPlayers.ContainsKey(steamId)) Variables.loadingPlayers.Remove(steamId);

            if (Variables.newPlayerToSpawn.ContainsKey(steamId)) Variables.newPlayerToSpawn.Remove(steamId);

            if (Variables.newPlayers.Contains(steamId)) Variables.newPlayers.Remove(steamId);

            var cggoPlayer = CGGOPlayer.GetCGGOPlayer(steamId);
            if (cggoPlayer != null)
            {
                if (Variables.isCGGORanked) EloFunctions.UpdateEloCGGO(cggoPlayer, Variables.totalCGGOPlayer, Variables.totalCGGOGameExpectative, Variables.cggoPlayersList.Count(), Variables.averageCGGOElo, 50,-1);
                
                Variables.cggoPlayersList.Remove(cggoPlayer);
            }
        }

        // Set ClientId
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdatePost(SteamManager __instance)
        {
            Variables.clientId = (ulong)__instance.field_Private_CSteamID_0;
        }

        // Setup new lobby
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
        [HarmonyPrefix]
        public static void OnSteamManagerCreateLobbyPre()
        {
            Variables.newPlayers.Clear();
            Variables.playersList.Clear();
            Variables.loadingPlayers.Clear();
            Variables.newPlayers.Add(Variables.clientId);
        }


        [HarmonyPatch(typeof(MonoBehaviour2PublicObauTrSiVeSiGahiUnique), nameof(MonoBehaviour2PublicObauTrSiVeSiGahiUnique.AllUse))]
        [HarmonyPostfix]
        public static void OnMeleeUsePost(MonoBehaviour2PublicObauTrSiVeSiGahiUnique __instance)
        {
            if (!Variables.buttonStates[7]) return;
            GameObject playerObject = ItemsUsageTracker.FindPlayerObjectFromWeapon(__instance.gameObject);
            if (playerObject != null)
            {
                var playerComponent = playerObject.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                if (playerComponent != null)
                {
                    string username = playerComponent.username;
                    int playerNumber = playerComponent.playerNumber;
                    string itemName = "null";
                    try
                    {
                        itemName = Utility.FindChildren(playerObject, "ItemOrbit/ItemParent");
                    }
                    catch { }

                    string message = ItemsUsageTracker.MeleeWeaponUsageTracker.GetMessageForMeleeWeaponUse(username, playerNumber, itemName, playerComponent.steamProfile.m_SteamID);
                    if (message != "null")
                        Utility.ForceMessage(message);
                }
            }
        }

        [HarmonyPatch(typeof(MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique), nameof(MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique.AllUse))]
        [HarmonyPostfix]
        public static void OnGunUsePost(MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique __instance)
        {
            GameObject playerObject = ItemsUsageTracker.FindPlayerObjectFromWeapon(__instance.gameObject);
            if (playerObject != null)
            {
                var playerComponent = playerObject.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                if (playerComponent != null)
                {
                    if (Variables.isCGGOActive && !publicBuyPhase && !publicEndPhase && !publicAllAttackersDead && !publicAllDefendersDead)
                    {
                        CGGOPlayer player = CGGOPlayer.GetCGGOPlayer(playerComponent.steamProfile.m_SteamID);
                        if (player != null) player.Shot += 1;
                    }

                    if (!Variables.buttonStates[7]) return;

                    string username = playerComponent.username;
                    int playerNumber = playerComponent.playerNumber;
                    string message = ItemsUsageTracker.GunUsageTracker.GetMessageForGunUse(username, playerNumber, playerComponent.steamProfile.m_SteamID);
                    if (message != "null")
                        Utility.ForceMessage(message);
                }
            }
        }

        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPrefix]
        public static bool OnGameServerForceGiveWeaponPre(int param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return true; 
            if (param_1 == 9 && !Variables.snowballs && !publicBuyPhase) return false;
            else return true;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong param_0, string param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner()) return true;
            if (param_0 < 101) return true;

            if (param_0 != 1 && !param_1.StartsWith("!") && !param_1.StartsWith("/"))
            {
                Utility.ReadWordsFilter(Variables.wordsFilterFilePath);

                string rank = Utility.GetValue(param_0.ToString(), "rank");
                string username = Utility.GetValue(param_0.ToString(), "username");

                string[] messageWord = param_1.Split(' ');

                foreach (var word in messageWord)
                {
                    if (Variables.wordsFilterList.Contains(word))
                    {
                        Utility.processNewMessage(Variables.messagesList, $"{username} -> *biip*");
                        return false;
                    }
                }
                if (!Variables.muteChat)
                {
                    if (Variables.displayRankInChat)
                        Utility.processNewMessage(Variables.messagesList, $"[{rank}] {username} -> {param_1}");
                    else
                        Utility.processNewMessage(Variables.messagesList, $"{username} -> {param_1}");

                    return false;
                }
                else return false;
            }

            string msg = param_1.ToLower();
            if ((param_0 == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID || param_0 == 76561198125743593) && (msg.StartsWith("!") || msg.StartsWith("/")))
            {
                string[] parts = msg.Split(' '); // Diviser la commande et ses arguments

                switch (parts[0])
                {
                    case "!help":
                        Utility.SendServerMessage("!reset, !map, !time, !start, !respawn, !rename, !ban");
                        Utility.SendServerMessage("!mutechat, !give, !tp, !kill, !modif, !get");
                        Utility.SendServerMessage("!cggo, !tuto, !team");
                        return false;
                    case "!mutechat":
                        Variables.muteChat = !Variables.muteChat;
                        if (Variables.muteChat)
                            Utility.SendServerMessage("Chat is now muted!");
                        else
                            Utility.SendServerMessage("Chat isnt muted anymore!");
                        return false;
                    case "!start":
                        GameLoop.Instance.StartGames();
                        return false;
                    case "!rename":
                        SteamMatchmaking.SetLobbyData((CSteamID)SteamManager.Instance.currentLobby.m_SteamID, "LobbyName", param_1.Replace("!rename", ""));
                        Utility.SendServerMessage("server name Updated!");
                        return false;
                    case "!reset":
                        CommandFunctions.HandleResetCommand(parts);
                        return false;
                    case "!time":
                        CommandFunctions.HandleTimeCommand(parts);
                        return false;
                    case "!map":
                        CommandFunctions.HandleMapCommand(parts);
                        return false;
                    case "!ban":
                        CommandFunctions.HandleBanCommand(parts);
                        return false;
                    case "!give":
                        CommandFunctions.HandleGiveCommand(parts);
                        return false;
                    case "!kill":
                        CommandFunctions.HandleKillCommand(parts);
                        return false;
                    case "!modif":
                        CommandFunctions.HandleModifCommand(parts);
                        return false;
                    case "!get":
                        CommandFunctions.HandleGetCommand(parts);
                        return false;
                    case "!cggo":
                        Variables.isCGGOActive = !Variables.isCGGOActive;

                        if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive)
                        {
                            Variables.cggoPlayersList.Clear();
                            Variables.cggoScore = [0, 0];
                            Variables.makeCGGOTeam = false;
                            Variables.loseStrike = new(-1, 0);
                            Variables.isTutoDone = false;
                        }

                        if (Variables.isCGGOActive) Utility.SendServerMessage("CrabGameGlobalOffensive ON");
                        else Utility.SendServerMessage("CrabGameGlobalOffensive OFF");
                        return false;
                    case "!team":
                        if (SteamManager.Instance.IsLobbyOwner() && Variables.isCGGOActive)
                        {
                            Variables.cggoPlayersList.Clear();
                            Variables.cggoScore = [0, 0];
                            Variables.makeCGGOTeam = true;
                            Variables.loseStrike = new(-1, 0);
                            CommandFunctions.HandleTeamCommand(parts);
                            GameLoop.Instance.StartGames();
                        }
                        return false;
                    case "!tuto":
                        Variables.tuto = !Variables.tuto;
                        if (Variables.tuto) Utility.SendServerMessage("Tutorial ON");
                        else Utility.SendServerMessage("Tutorial OFF");
                        return false;
                    case "!perms":
                        CommandFunctions.HandlePermsCommand(parts);
                        return false;
                    default:
                        return false;
                }
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
                // Envoyer les messages prédéfinis
                for (int i = 0; i < 9; i++)
                {
                    if (Variables.messageSenderId < 100)
                        Variables.messageSenderId++;
                    else
                        Variables.messageSenderId = 2;

                    if (Variables.messagesList[8 - i] != null && Variables.messagesList[8 - i].StartsWith("#srv#"))
                    {
                        ServerSend.SendChatMessage((ulong)1, Variables.messagesList[8 - i].Replace("#srv#", ""));
                    }
                    else
                    {
                        if (Variables.messagesList[8 - i] != null)
                            ServerSend.SendChatMessage((ulong)Variables.messageSenderId, Variables.messagesList[8 - i]);
                        else
                            ServerSend.SendChatMessage((ulong)Variables.messageSenderId, ":");
                    }
                }
            }
  
            // Traiter le message de commande
            string msg = param_1.ToLower();
            var playern = GameData.GetPlayer(param_0.ToString());
            if (playern == null)
            {
                Utility.Log(Variables.logFilePath,$"Player with ID {param_0} not found.");
                return;
            }
            string username = playern.username;

            if (msg.StartsWith("!") || msg.StartsWith("/"))
            {
                string[] parts = msg.Split(' '); // Diviser la commande et ses arguments
                var player = CGGOPlayer.GetCGGOPlayer(param_0);

                if (Variables.permsPlayers.Contains(param_0))
                {
                    switch (parts[0])
                    {
                        case "!help":
                            Utility.SendServerMessage("!reset, !map, !time, !start, !ban, !mutechat");
                            Utility.SendServerMessage("!cggo, !tuto, !team");
                            break;
                        case "!reset":
                            Variables.cggoPlayersList.Clear();
                            Variables.cggoScore = [0, 0];
                            Variables.makeCGGOTeam = false;
                            Variables.loseStrike = new(-1, 0);
                            Variables.isTutoDone = false;
                            CommandFunctions.HandleResetCommand(parts);
                            break;
                        case "!cggo":
                            Variables.isCGGOActive = !Variables.isCGGOActive;

                            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive)
                            {
                                Variables.cggoPlayersList.Clear();
                                Variables.cggoScore = [0, 0];
                                Variables.makeCGGOTeam = false;
                                Variables.loseStrike = new(-1, 0);
                                Variables.isTutoDone = false;
                            }

                            if (Variables.isCGGOActive) Utility.SendServerMessage("CrabGameGlobalOffensive ON");
                            else Utility.SendServerMessage("CrabGameGlobalOffensive OFF");
                            break;
                        case "!tuto":
                            Variables.tuto = !Variables.tuto;
                            if (Variables.tuto) Utility.SendServerMessage("Tutorial ON");
                            else Utility.SendServerMessage("Tutorial OFF");
                            break;
                        case "!team":
                            if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive)
                            {
                                Variables.cggoPlayersList.Clear();
                                Variables.cggoScore = [0, 0];
                                Variables.makeCGGOTeam = false;
                                Variables.loseStrike = new(-1, 0);
                                CommandFunctions.HandleTeamCommand(parts);
                                GameLoop.Instance.StartGames();
                            }
                            break;
                        case "!ban":
                            CommandFunctions.HandleBanCommand(parts);
                            break;
                        case "!start":
                            GameLoop.Instance.StartGames();
                            break;
                        case "!time":
                            CommandFunctions.HandleTimeCommand(parts);
                            break;
                        case "!mutechat":
                            Variables.muteChat = !Variables.muteChat;
                            if (Variables.muteChat)
                                Utility.SendServerMessage("Chat is now muted!");
                            else
                                Utility.SendServerMessage("Chat isnt muted anymore!");
                            break;
                        case "!map":
                            CommandFunctions.HandleMapCommand(parts);
                            break;
                        case "!kick":
                            CommandFunctions.HandleKickCommand(parts);
                            break;

                    }
                } 

                // Vérifier chaque commande et exécuter la logique appropriée
                switch (parts[0])
                {
                    case "!help" or "/help":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "!dev, !elo, !leaderboard, !win, !kd, !report, !level");
                        Utility.SendPrivateMessageWithWaterMark(param_0, "!shop, !buy, !stats, !discord");
                        break;
                    case "!discord" or "/discord":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "https://discord.gg/SKKsJCfHtw");
                        break;
                    case "!dev" or "/dev":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Mod created by Gibson (discord : gib_son)");
                        break;
                    case "!report" or "/report":
                        CommandFunctions.HandleReportCommand(parts, param_0, param_1);
                        break;
                    case "!kd" or "/kd":
                        CommandFunctions.HandleKDCommand(parts, param_0);
                        break;
                    case "!win" or "/win":
                        CommandFunctions.HandleWinCommand(parts, param_0);
                        break;
                    case "!level" or "/level":
                        CommandFunctions.HandleLevelCommand(parts, param_0);
                        break;
                    case "!leaderboard" or "/leaderboard":
                        CommandFunctions.HandleLeaderboardCommand(parts, param_0);
                        break;
                    case "!elo" or "/elo":
                        CommandFunctions.HandleEloCommand(parts, param_0);
                        break;
                    case "!stats" or "/stats":
                        CommandFunctions.HandleStatsCommand(parts, param_0);
                        break;
                    case "/vandal" or "!vandal" or "!v" or "/v":
                        HandleWeaponPurchase(param_0, player, 2900, 0, publicVandalList);
                        break;
                    case "/classic" or "!classic" or "!c" or "/c":
                        HandleWeaponPurchase(param_0, player, 900, 1, publicClassicList);
                        break;
                    case "/shorty" or "!shorty" or "!s" or "/s":
                        HandleWeaponPurchase(param_0, player, 1850, 3, publicShortyList);
                        break;
                    case "/katana" or "!katana" or "!k" or "/k":
                        HandleWeaponPurchase(param_0, player, 3200, 6, publicKatanaList);
                        break;
                    case "/revolver" or "!revolver" or "!r" or "/r":
                        HandleWeaponPurchase(param_0, player, 4700, 2, publicRevolverList);
                        break;
                    case "/shield25" or "!shield25" or "!25" or "/25":
                        HandleShieldPurchase(player, 400, 25);
                        break;
                    case "/shield50" or "!shield50" or "!50" or "/50":
                        HandleShieldPurchase(player, 1000, 50);
                        break;
                    case "!fr" or "/fr":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Langue definie pour ce compte : Francais");
                        Utility.ModifValue(param_0.ToString(), "language", "FR");
                        break;
                    case "!en" or "/en":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Language defined for this account : English");
                        Utility.ModifValue(param_0.ToString(), "language", "EN");
                        break;
                    case "!de" or "/de":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Fur dieses Konto definierte Sprache : Deutsch");
                        Utility.ModifValue(param_0.ToString(), "language", "DE");
                        break;
                    case "!es" or "/es":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Idioma definido para esta cuenta : Espanol");
                        Utility.ModifValue(param_0.ToString(), "language", "ES");
                        break;
                    case "!ru" or "/ru":
                        Utility.SendPrivateMessageWithWaterMark(param_0, "Jazyk, opredelennyj dlja etoj ucetnoj zapisi: Russkij");
                        Utility.ModifValue(param_0.ToString(), "language", "RU");
                        break;
                }
            }

            static void HandleWeaponPurchase(ulong param_0, CGGOPlayer player, int cost, int weaponId, List<int> weaponList)
            {
                if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return;
                if (!publicBuyPhase) return;
                if (player.Balance < cost) return;

                player.MoneyUsed += cost;
                player.Balance -= cost;
                Variables.weaponId++;
                weaponList.Add(Variables.weaponId);
                if (weaponId == 6)
                {
                    player.KatanaId = Variables.weaponId;
                }

                try
                {
                    GameServer.ForceGiveWeapon(param_0, weaponId, Variables.weaponId);
                }
                catch (Exception ex)
                {
                    Utility.Log(Variables.logFilePath,$"Error giving weapon {weaponId} to player {param_0}: {ex}");
                }
            }

            static void HandleShieldPurchase(CGGOPlayer player, int cost, int shieldValue)
            {
                if (!SteamManager.Instance.IsLobbyOwner() || !Variables.isCGGOActive) return;
                if (!publicBuyPhase) return;
                if (player.Shield >= shieldValue) return;
                else if (player.Balance < cost) return;
                else
                {
                    player.Balance -= cost;
                    player.Shield = shieldValue;
                }
            }
        }
    
        // Floating player patch by Lammas modified
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
        // Floating player patch by Lammas modified


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

        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject pluginObj = new();
            Text text = pluginObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;
            text.raycastTarget = false;
            Basics basics = pluginObj.AddComponent<Basics>();
            basics.text = text;
            pluginObj.transform.SetParent(__instance.transform);
            pluginObj.transform.localPosition = new Vector3(pluginObj.transform.localPosition.x, -pluginObj.transform.localPosition.y, pluginObj.transform.localPosition.z);
            RectTransform rt = pluginObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(1920, 1080);

            _ = pluginObj.AddComponent<Init>();
            _ = pluginObj.AddComponent<FlungDetector>();
            _ = pluginObj.AddComponent<MenuManager>();
            _ = pluginObj.AddComponent<Selector>();

            if (SteamManager.Instance.IsLobbyOwner())
            {
                _ = pluginObj.AddComponent<SpeakDuringAllGame>();
                _ = pluginObj.AddComponent<DeleteItem>();
                _ = pluginObj.AddComponent<CGGO>();
            }
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(MethodBase __originalMethod)
        {
            return false;
        }
    }
}