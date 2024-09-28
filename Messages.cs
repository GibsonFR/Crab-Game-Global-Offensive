namespace GibsonCrabGameGlobalOffensive
{
    public static class CommandMessages
    {
        public const string ADMIN_HELP_1 = "!reset, !map, !time, !start, !rename, !ban, !team";
        public const string ADMIN_HELP_2 = "!mutechat, !give, !tp, !kill, !modif, !get, !cggo, !tuto";
        public const string MODO_HELP_1 = "!reset, !map, !time, !start, !ban, !team";
        public const string MODO_HELP_2 = "!mutechat, !give, !tp, !kill, !cggo, !tuto";
        public const string PLAYER_HELP_1 = "!dev, !elo, !leaderboard, !win, !kd, !report, !level";
        public const string PLAYER_HELP_2 = "!shop, !buy, !stats, !discord";
        public const string CHAT_TOGGLE = "Chat is now";
        public const string RENAME_SERVER = "Server name updated";
        public const string CGGO_TOGGLE = "Crab Game : Global Offensive is now";
        public const string TUTO_TOGGLE = "CGGO Tutorial is now";
        public const string DISCORD = "https://discord.gg/SKKsJCfHtw";
        public const string DEV = "Mod created by Gibson, discord: gib_son, github: GibsonFR";
        public const string MESSAGE_FR = "Langue definie pour ce compte : Francais";
        public const string MESSAGE_EN = "Language defined for this account : English";
        public const string MESSAGE_RU = "Jazyk, opredelennyj dlja etoj ucetnoj zapisi: Russkij";
        public const string MESSAGE_DE = "Fur dieses Konto definierte Sprache : Deutsch";
        public const string MESSAGE_ES = "Idioma definido para esta cuenta : Espanol";
        public const string LANGUAGE_FR = "FR";
        public const string LANGUAGE_EN = "EN";
        public const string LANGUAGE_RU = "RU";
        public const string LANGUAGE_DE = "DE";
        public const string LANGUAGE_ES = "ES";
        public const string PLAYER_NOT_FOUND = "Player not found, use #number or playerName";
        public const string INVALID_ARGUMENT = "Invalid arg -> ";
    }
    public static class MainMessages
    {
        public const string MENU_ON = "■<color=orange>MenuManager <color=blue>ON</color></color>■";
        public const string MENU_OFF = "■<color=orange>MenuManager <color=red>OFF</color></color>■";
        public const string NAVIGATION = "■<color=orange>navigate the menu using the scrollWheel</color>■";
        public const string SELECTION = "■<color=orange>right click to select</color>■";
        public const string SUBMENU_EXIT = "■<color=orange>press scrollWheel to exit submenu</color>■";

        public const string LOADING_STEP_0 = "L O A D I N G[* - - - - - - - - -]L O A D I N G";
        public const string LOADING_STEP_1 = "L O A D I N G[- * - - - - - - - -]L O A D I N G";
        public const string LOADING_STEP_2 = "L O A D I N G[- - * - - - - - - -]L O A D I N G";
        public const string LOADING_STEP_3 = "L O A D I N G[- - - * - - - - - -]L O A D I N G";
        public const string LOADING_STEP_4 = "L O A D I N G[- - - - * - - - - -]L O A D I N G";
        public const string LOADING_STEP_5 = "L O A D I N G[- - - - - * - - - -]L O A D I N G";
        public const string LOADING_STEP_6 = "L O A D I N G[- - - - - - * - - -]L O A D I N G";
        public const string LOADING_STEP_7 = "L O A D I N G[- - - - - - - * - -]L O A D I N G";
        public const string LOADING_STEP_8 = "L O A D I N G[- - - - - - - - * -]L O A D I N G";
        public const string LOADING_STEP_9 = "L O A D I N G[- - - - - - - - - *]L O A D I N G";

    }
    public class CGGOMessages
    {
        public static string[] GetTutorialMessageInSpecificLanguage(string language)
        {
            return language switch
            {
                "RU" => ["Dobro pozalovat v CGGO",
                    "Pobedit pervym nabrav 6 ockov",
                    "Kupit oruzie primer lenty /vandal",
                    "Pomestite bombu v molocnuju zonu",
                    "Ctoby zalozit bombu, broste bombu",
                    "Obezvredit bombu do togo, kak ona vzorvetsja",
                    "Nazmite 4 i prisjadte na kortocki"],
                "DE" => ["Willkommen bei CGGO, custom mod CG",
                    "Die Ersten mit 6 Punkten gewinnen",
                    "Kaufe Waffen,Beispiel tape /vandal",
                    "Lege die Bombe in der Milkzone",
                    "Um die Bombe zu legen, lass die Bomb fallen",
                    "Entscharfe die Bombe, bevor sie explodiert",
                    "Drucke 4 und hocke dich darauf"],
                "ES" => ["Bienvenido a CGGO, custom mod CG",
                    "El primero en llegar a 6 puntos gana",
                    "Compra armas, ejemplo tipo /vandal",
                    "Coloca la bomba en la Milkzone",
                    "Para poner la bomba, tira la bomba",
                    "Desactiva la bomba antes de que explote",
                    "pulsa 4 y ponte en cuclillas sobre ella"],
                "FR" => ["Bienvenue dans CGGO, custom mod CG",
                    "Les premiers a 6 points gagne",
                    "Achetes des armes,exemple tape /vandal",
                    "Poses la bombe dans les Milkzone",
                    "Pour poser la bombe, laches la",
                    "Desamorcer la bombe avant l'explosion:",
                    "appuyes sur 4 et accroupis-toi dessus"],
                "EN" => ["Welcome in CGGO, custom mod CG",
                    "First team to 6 points win",
                    "Buy weapon in chat,ex: type /vandal",
                    "Plant the bomb on Milkzone",
                    "To plant drop the bomb, press Q",
                    "Defuse the bomb before it explode",
                    "To defuse press 4 and crouch on bomb"],
                _ => ["Welcome in CGGO, custom mod CG",
                    "First team to 6 points win",
                    "Buy weapon in chat,ex: type /vandal",
                    "Plant the bomb on Milkzone",
                    "To plant drop the bomb, press Q",
                    "Defuse the bomb before it explode",
                    "To defuse press 4 and crouch on bomb"],
            };
        }
        public static string[] GetBuyPhaseMessageInSpecificLanguage(CGGOPlayer player, int totalTeamScore)
        {
            return player.Language switch
            {
                "RU" => ["---- E T A P ---- P O K U P K I ",
                    $"KRUGLYJ {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Balans vasego sceta : {player.Balance}$",
                    "Ctoby kupit, naberite v cate:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
                "DE" => ["---- K A U F ---- P H A S E ",
                    $"RUNDE {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Dein saldo : {player.Balance}$",
                    "Um etwas zu kaufen,tippe auf den chat:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
                "ES" => ["---- C O M P R A R ---- F A S E ",
                    $"RONDA {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Tu saldo : {player.Balance}$",
                    "Para comprar, escribe en el chat:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
                "FR" => ["---- P H A S E ---- D ' A C H A T ",
                    $"MANCHE {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Ton solde : {player.Balance}$",
                    "Pour acheter, tape dans le chat:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
                "EN" => ["---- B U Y ---- P H A S E ",
                    $"ROUND {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Your Money : {player.Balance}$",
                    "To buy, type in chat:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
                _ => ["---- B U Y ---- P H A S E ",
                    $"ROUND {totalTeamScore + 1} | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"Your $Money$ : {player.Balance}$",
                    "To buy a weapon, type in chat:",
                    " /classic (900$) - /shield25 (400$)",
                    " /shorty (1850$) - /shield50 (1000$)",
                    " /vandal (2900$) - /revolver (4700$)",
                    " /katana (3200$)"],
            };
        }

        public static string[] GetNewPlayersBuyPhaseMessageInSpecificLanguage(string language, int totalTeamScore, int totalBalanceTeamAttackers, int totalBalanceTeamDefenders)
        {
            return language switch
            {
                "EN" => [
                                            "#",
                    " ---- B U Y ---- ---- P H A S E ---- ",
                    $"ROUND [{totalTeamScore + 1}] | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"ATTACKERS BALANCE[{cggoScore[0]}] : {totalBalanceTeamAttackers}$",
                    $"DEFENDERS BALANCE[{cggoScore[1]}] : {totalBalanceTeamDefenders}$",
                    "____ C H A T ____ ____ C H A T ____"

                                        ],
                _ => [
                                            "#",
                    " ---- B U Y ---- ---- P H A S E ---- ",
                    $"ROUND [{totalTeamScore + 1}] | SCORE: {cggoScore[0]} - {cggoScore[1]}",
                    $"ATTACKERS BALANCE[{cggoScore[0]}] : {totalBalanceTeamAttackers}$",
                    $"DEFENDERS BALANCE[{cggoScore[1]}] : {totalBalanceTeamDefenders}$",
                    "____ C H A T ____ ____ C H A T ____"

                                        ],
            };
        }

    }
}
