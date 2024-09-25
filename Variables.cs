namespace GibsonCrabGameGlobalOffensive
{
    //Ici on stock les variables "globale" pour la lisibilité du code dans Plugin.cs 
    internal class Variables
    {
        //folder
        public static string assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"CrabGameGlobalOffensive\";
        public static string playersDataFolderPath = mainFolderPath + @"PlayersData\";


        //file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string configFilePath = mainFolderPath + "config.txt";
        public static string menuFilePath = mainFolderPath + @"menu.txt";
        public static string playersBannedFilePath = mainFolderPath + @"bannedPlayers.txt";
        public static string playersListFilePath = mainFolderPath + @"playerList.txt";
        public static string playersReportFilePath = mainFolderPath + @"report.txt";
        public static string wordsFilterFilePath = mainFolderPath + @"wordsFilter.txt";
        public static string permsFilePath = mainFolderPath + @"perms.txt";


        //Manager
        public static PlayerMovement clientMovement;
        public static PlayerInventory clientInventory;
        public static PlayerStatus clientStatus;

        //Camera
        public static Camera camera;

        //Dictionary
        public static Dictionary<string, Func<string>> DebugDataCallbacks = [];
        public static Dictionary<int, List<int>> playableMapOnMod = [];
        public static Dictionary<PlayerManager, Quaternion> playerRotationCheck = [];
        public static Dictionary<ulong, DateTime> newPlayerToSpawn = [];
        public static Dictionary<ulong, int> spawnIdDictionary = [];
        public static Dictionary<int, DateTime> itemToDelete = [];
        public static Dictionary<ulong, PlayerManager> playersList = [];
        public static Dictionary<ulong, DateTime> loadingPlayers = [];
        public static Dictionary<string, long> bannedPlayers = [];
        public static Dictionary<ulong, KeyValuePair<ulong,DateTime>> hitPlayers = [];

        //List
        public static List<ulong> newPlayers = [];
        public static List<ulong> modList = [];
        public static List<string> wordsFilterList = ["aaaaa"];
        public static List<CGGOPlayer> cggoPlayersList = [];
        public static List<string> messagesList = new(new string[9]);
        public static List<ulong> playersInRanked = [];
        public static List<int> cggoScore = [0,0];
        public static List<ulong> permsPlayers = [];
        public class LoseStrike
        {
            public int Key { get; set; }
            public int Value { get; set; }

            public LoseStrike(int key, int value)
            {
                Key = key;
                Value = value;
            }
        }
        public static LoseStrike loseStrike = new(-1, 0);

        //Rigidbody
        public static Rigidbody clientBody;

        //GameObject
        public static GameObject clientObject;

        //int
        public static int totalCGGOPlayer = 0, weaponId = 5000, nextMapTime, roundCount, playersThisGame, messageSenderId = 2, spawnId, afkCheckDuration, mapTimer, playerToAutoStart, topPlayer = 40, mapId, modeId, messageTimer, menuSelector, subMenuSelector, playerIndex, statusTrigger, menuSpeed = 5, menuSpeedHelperFast, menuSpeedHelper, alertLevel = 0;

        //float
        public static float averageCGGOElo=0f, totalCGGOGameExpectative=0f,  Kfactor = 32, averageGameElo, factorValue, totalGameExpectative, smoothedSpeed, smoothingFactor = 0.7f, checkFrequency = 0.02f;

        //ulong
        public static ulong clientId;

        //string
        public static string gameState, lastGameState, menuKey = "f5", lastItemName = "null", otherPlayerUsername, layout, displayButton0, displayButton1, displayButton2, displayButton3, displayButton4, displayButton5, displayButton6, displayButton7, customPrecisionFormatTargetPosition = "F1", customPrecisionFormatClientRotation = "F1";

        //bool
        public static bool isTutoDone, isCGGORanked,tuto, startRound, makeCGGOTeam, isCGGOActive, muteChat, wordsFilter, displayRankInChat, snowballs, GAC, onButton, onSubButton, menuTrigger, clientIsDead;

        //bool[]
        public static bool[] buttonStates = new bool[8];

        //Vector3
        public static Vector3 lastOtherPlayerPosition;

        //DateTime
        public static DateTime roundStart;
    }
}
