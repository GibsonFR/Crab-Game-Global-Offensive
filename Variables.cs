namespace GibsonCrabGameGlobalOffensive
{
    //Ici on stock les variables "globale" pour la lisibilité du code dans Plugin.cs 
    internal class Variables
    {
        // folder
        public static string assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"CrabGameGlobalOffensive\";
        public static string playersDataFolderPath = mainFolderPath + @"PlayersData\";


        // file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string configFilePath = mainFolderPath + "config.txt";
        public static string menuFilePath = mainFolderPath + @"menu.txt";
        public static string playersBannedFilePath = mainFolderPath + @"bannedPlayers.txt";
        public static string playersListFilePath = mainFolderPath + @"playerList.txt";
        public static string playersReportFilePath = mainFolderPath + @"report.txt";
        public static string wordsFilterFilePath = mainFolderPath + @"wordsFilter.txt";
        public static string permsFilePath = mainFolderPath + @"perms.txt";

        // Camera
        public static Camera camera;

        // Dictionary
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

        // List
        public static List<ulong> newPlayers = [];
        public static List<ulong> modList = [];
        public static List<string> wordsFilterList = ["aaaaa"];
        public static List<CGGOPlayer> cggoPlayersList = [];
        public static List<string> messagesList = new(new string[9]);
        public static List<ulong> playersInRanked = [];
        public static List<int> cggoScore = [0,0];
        public static List<ulong> permsPlayers = [];
        public class LoseStrike(int key, int value)
        {
            public int Key { get; set; } = key;
            public int Value { get; set; } = value;
        }
        public static LoseStrike loseStrike = new(-1, 0);

        // Rigidbody
        public static Rigidbody clientBody;

        // GameObject
        public static GameObject clientObject;

        // int
        public static int totalCGGOPlayer = 0, weaponId = 5000, nextMapTime, roundCount, playersThisGame, messageSenderId = 2, spawnId, afkCheckDuration, mapTimer, playerToAutoStart, topPlayer = 40, mapId, modeId, messageTimer, menuSelector, subMenuSelector, playerIndex, statusTrigger, menuSpeed = 5, menuSpeedHelperFast, menuSpeedHelper, alertLevel = 0, kFactor = 50, clownRatingCeiling = 750, woodRatingCeiling = 850, silverRatingCeiling = 950, goldRatingCeiling = 1050, platinumRatingCeiling = 1100, diamondRatingCeiling = 1150, masterRatingCeiling = 1200, grandMasterRatingCeiling = 1300, challengerRatingCeiling = 2000, originalBombId;

        // float
        public static float averageCGGOElo=0f, totalCGGOGameExpectative=0f, averageGameElo, factorValue, totalGameExpectative, smoothedSpeed, smoothingFactor = 0.7f, checkFrequency = 0.02f, ratingDifferenceScale = 400f;
        
        // double
        public static double headshotWeight = 0.1, bodyshotWeight = 0.05, legsshotWeight = 0.05, moneyEfficiencyWeight = 0.1, defuseWeight = 0.05, damageEfficiencyWeight = 0.25, killContributionWeight = 0.15, deathContributionWeight = 0.15, assistContributionWeight = 0.05;

        // ulong
        public static ulong clientId;

        // string
        public static string menuKey = "f5", lastItemName = "null", otherPlayerUsername, layout, displayButton0, displayButton1, displayButton2, displayButton3, displayButton4, displayButton5, displayButton6, displayButton7, customPrecisionFormatTargetPosition = "F1", customPrecisionFormatClientRotation = "F1";

        // bool
        public static bool isTutoDone, isCGGORanked,tuto, startRound, CGGOTeamSet, isCGGOActive, muteChat, wordsFilter, displayRankInChat, snowballs, GAC, onButton, onSubButton, menuTrigger, clientIsDead;

        // bool[]
        public static bool[] buttonStates = new bool[8];

        // Vector3
        public static Vector3 lastOtherPlayerPosition;

        // DateTime
        public static DateTime roundStart;
    }
}
