
namespace GibsonCrabGameGlobalOffensive
{
    public class Utility
    {
        public static bool IsHostAndCGGOActive()
        {
            return SteamManager.Instance.IsLobbyOwner() && isCGGOActive;
        }
        public static int GetLineCount(string filePath)
        {
            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);
                // Return the length of the array (number of lines)
                return lines.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return -1; // or throw, depending on how you want to handle errors
            }
        }
        public static string ConvertSecondsToFormattedTime(int totalSeconds)
        {
            int days = totalSeconds / 86400; // 86400 seconds in a day
            totalSeconds %= 86400;

            int hours = totalSeconds / 3600; // 3600 seconds in an hour
            totalSeconds %= 3600;

            int minutes = totalSeconds / 60; // 60 seconds in a minute
            int seconds = totalSeconds % 60; // Remaining seconds

            return $"{days} d, {hours} h, {minutes} m, {seconds} s";
        }
        public static string GetValue(string steamId, string key)
        {
            string path = playersDataFolderPath + steamId + ".txt";

            string[] lignes = File.ReadAllLines(path);

            foreach (var ligne in lignes)
            {
                string[] elements = ligne.Split(':');

                if (elements.Length == 2)
                {
                    string cleActuelle = elements[0].Trim();
                    string valeur = elements[1].Trim();

                    if (cleActuelle.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return valeur;
                    }
                }
            }
            return null;
        }

        public static void ModifValue(string steamId, string key, string newValue)
        {
            string path = playersDataFolderPath + steamId + ".txt";
            string[] lignes = File.ReadAllLines(path);

            for (int i = 0; i < lignes.Length; i++)
            {
                string[] elements = lignes[i].Split(':');

                if (elements.Length == 2)
                {
                    string cleActuelle = elements[0].Trim();

                    if (cleActuelle.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        lignes[i] = $"{key}:{newValue}";
                        break;
                    }
                }
            }

            File.WriteAllLines(path, lignes);
        }
        public static PlayerData ReadPlayerData(string filePath)
        {
            PlayerData player = new();
            try
            {
                using StreamReader sr = new(filePath);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        try
                        {
                            switch (key)
                            {
                                case "username":
                                    player.Username = value;
                                    break;
                                case "rank":
                                    player.Rank = value;
                                    break;
                                case "elo":
                                    if (!float.TryParse(value, out float elo))
                                    {
                                        throw new FormatException($"Invalid value for elo: {value}");
                                    }
                                    player.Elo = elo;
                                    break;
                                case "highestElo":
                                    if (!float.TryParse(value, out float highestElo))
                                    {
                                        throw new FormatException($"Invalid value for highestElo: {value}");
                                    }
                                    player.HighestElo = highestElo;
                                    break;
                                case "gamePlayed":
                                    if (!float.TryParse(value, out float gamePlayed))
                                    {
                                        throw new FormatException($"Invalid value for gamePlayed: {value}");
                                    }
                                    player.GamePlayed = gamePlayed;
                                    break;
                                case "level":
                                    if (!float.TryParse(value, out float level))
                                    {
                                        throw new FormatException($"Invalid value for level: {value}");
                                    }
                                    player.Level = level;
                                    break;
                                case "win":
                                    if (!float.TryParse(value, out float win))
                                    {
                                        throw new FormatException($"Invalid value for win: {value}");
                                    }
                                    player.Win = win;
                                    break;
                                case "kills":
                                    if (!float.TryParse(value, out float kills))
                                    {
                                        throw new FormatException($"Invalid value for kills: {value}");
                                    }
                                    player.Kills = kills;
                                    break;
                                case "death":
                                    if (!float.TryParse(value, out float death))
                                    {
                                        throw new FormatException($"Invalid value for death: {value}");
                                    }
                                    player.Death = death;
                                    break;
                                case "averageRoundDuration":
                                    if (!float.TryParse(value, out float averageTimePlayed))
                                    {
                                        throw new FormatException($"Invalid value for averageRoundDuration: {value}");
                                    }
                                    player.AverageRoundDuration = averageTimePlayed;
                                    break;
                                case "totalTimePlayed":
                                    if (!float.TryParse(value, out float totalTimePlayed))
                                    {
                                        throw new FormatException($"Invalid value for totalTimePlayed: {value}");
                                    }
                                    player.TotalTimePlayed = totalTimePlayed;
                                    break;
                                case "roundPlayed":
                                    if (!int.TryParse(value, out int roundPlayed))
                                    {
                                        throw new FormatException($"Invalid value for roundPlayed: {value}");
                                    }
                                    player.RoundPlayed = roundPlayed;
                                    break;
                                case "averageSpeed":
                                    if (!float.TryParse(value, out float averageSpeed))
                                    {
                                        throw new FormatException($"Invalid value for averageSpeed: {value}");
                                    }
                                    player.AverageSpeed = averageSpeed;
                                    break;
                                case "averageConcurrent":
                                    if (!float.TryParse(value, out float averageConcurrent))
                                    {
                                        throw new FormatException($"Invalid value for averageConcurrent: {value}");
                                    }
                                    player.AverageConcurrent = averageConcurrent;
                                    break;
                                case "averageEloDelta":
                                    if (!float.TryParse(value, out float averageEloDelta))
                                    {
                                        throw new FormatException($"Invalid value for averageEloDelta: {value}");
                                    }
                                    player.AverageEloDelta = averageEloDelta;
                                    break;
                                case "moonwalkPercent":
                                    if (!float.TryParse(value, out float moonwalkPercent))
                                    {
                                        throw new FormatException($"Invalid value for moonwalkPercent: {value}");
                                    }
                                    player.MoonwalkPercent = moonwalkPercent;
                                    break;
                                case "CGGOPlayed":
                                    if (!int.TryParse(value, out int CGGOPlayed))
                                    {
                                        throw new FormatException($"Invalid value for CGGOPlayed: {value}");
                                    }
                                    player.CGGOPlayed = CGGOPlayed;
                                    break;
                                case "CGGOWon":
                                    if (!int.TryParse(value, out int CGGOWon))
                                    {
                                        throw new FormatException($"Invalid value for CGGOWon: {value}");
                                    }
                                    player.CGGOWon = CGGOWon;
                                    break;
                                case "averageCGGOHeadShot":
                                    if (!float.TryParse(value, out float averageCGGOHeadShot))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOHeadShot: {value}");
                                    }
                                    player.CGGOHeadShotPercent = averageCGGOHeadShot;
                                    break;
                                case "averageCGGOBodyShot":
                                    if (!float.TryParse(value, out float averageCGGOBodyShot))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOBodyShot: {value}");
                                    }
                                    player.CGGOBodyShotPercent = averageCGGOBodyShot;
                                    break;
                                case "averageCGGOLegsShot":
                                    if (!float.TryParse(value, out float averageCGGOLegsShot))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOLegsShot: {value}");
                                    }
                                    player.CGGOLegsShotPercent = averageCGGOLegsShot;
                                    break;
                                case "averageCGGODefuse":
                                    if (!float.TryParse(value, out float averageCGGODefuse))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGODefuse: {value}");
                                    }
                                    player.CGGODefusePercent = averageCGGODefuse;
                                    break;
                                case "averageCGGOKill":
                                    if (!float.TryParse(value, out float averageCGGOKill))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOKill: {value}");
                                    }
                                    player.AverageCGGOKill = averageCGGOKill;
                                    break;
                                case "averageCGGODeath":
                                    if (!float.TryParse(value, out float averageCGGODeath))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGODeath: {value}");
                                    }
                                    player.AverageCGGODeath = averageCGGODeath;
                                    break;
                                case "averageCGGOAssist":
                                    if (!float.TryParse(value, out float averageCGGOAssist))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOAssist: {value}");
                                    }
                                    player.AverageCGGOAssist = averageCGGOAssist;
                                    break;
                                case "averageCGGODamageDealt":
                                    if (!float.TryParse(value, out float averageCGGODamageDealt))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGODamageDealt: {value}");
                                    }
                                    player.AverageCGGODamageDealt = averageCGGODamageDealt;
                                    break;
                                case "averageCGGODamageReceived":
                                    if (!float.TryParse(value, out float averageCGGODamageReceived))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGODamageReceived: {value}");
                                    }
                                    player.AverageCGGODamageReceived = averageCGGODamageReceived;
                                    break;
                                case "averageCGGOMoneyEfficiency":
                                    if (!float.TryParse(value, out float averageCGGOMoneyEfficiency))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOMoneyEfficiency: {value}");
                                    }
                                    player.AverageCGGOMoneyEfficiency = averageCGGOMoneyEfficiency;
                                    break;
                                case "averageCGGOScore":
                                    if (!float.TryParse(value, out float averageCGGOScore))
                                    {
                                        throw new FormatException($"Invalid value for averageCGGOScore: {value}");
                                    }
                                    player.AverageCGGOScore = averageCGGOScore;
                                    break;
                            }
                        }
                        catch (FormatException ex)
                        {
                            Utility.Log(logFilePath, $"Error parsing player data for key '{key}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Error reading player data: " + ex.Message);
            }

            return player;
        }

        public class PlayerData
        {
            public string Username { get; set; }
            public float Elo { get; set; }
            public string SteamId { get; set; }
            public float HighestElo { get; set; }
            public string Rank { get; set; }
            public float GamePlayed { get; set; }
            public float Level { get; set; }
            public float Win { get; set; }
            public float Kills { get; set; }
            public float Death { get; set; }
            public float TotalTimePlayed { get; set; }
            public int RoundPlayed { get; set; }
            public float AverageRoundDuration { get; set; }
            public float AverageSpeed { get; set; }
            public float AverageConcurrent { get; set; }
            public float AverageEloDelta { get; set; }
            public float MoonwalkPercent { get; set; }
            public int CGGOPlayed { get; set; }
            public int CGGOWon { get; set; }
            public float CGGOHeadShotPercent { get; set; }
            public float CGGOBodyShotPercent { get; set; }
            public float CGGOLegsShotPercent { get; set; }
            public float CGGODefusePercent { get; set; }
            public float AverageCGGOKill { get; set; }
            public float AverageCGGODeath { get; set; }
            public float AverageCGGOAssist { get; set; }
            public float AverageCGGODamageDealt { get; set; }
            public float AverageCGGODamageReceived { get; set; }
            public float AverageCGGOMoneyEfficiency { get; set; }
            public float AverageCGGOScore { get; set; }
        }
        public static string GetSpecificLine(string filePath, int lineNumber)
        {
            try
            {
                // Lire toutes les lignes du fichier
                string[] lines = File.ReadAllLines(filePath);

                // Vérifier si le numéro de ligne est valide
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    // Retourner la ligne spécifique
                    return lines[lineNumber - 1]; // Soustraire 1 car les indices commencent à 0
                }
                else
                {
                    Utility.Log(logFilePath, "invalid line number.");
                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Error when reading file : " + ex.Message);
            }

            return null;
        }

        public static int GetPlayerPlacement(string filePath, string steamId)
        {
            int rank = -1; // Valeur par défaut si le joueur n'est pas trouvé

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(';');
                    string currentSteamId = parts[0].Trim();
                    if (currentSteamId == steamId)
                    {
                        rank = i + 1; // Classement commence à 1, pas 0
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Erreur lors de la recherche du joueur : " + ex.Message);
            }
            return rank;
        }

        public static void ProcessPlayerFiles(string outputFilePath)
        {
            Dictionary<string, PlayerData> playersData = [];

            // Remplacez le chemin d'accès avec votre chemin réel
            string folderPath = playersDataFolderPath;

            try
            {
                string[] files = Directory.GetFiles(folderPath, "*.txt");

                foreach (string filePath in files)
                {
                    string steamId = Path.GetFileNameWithoutExtension(filePath);
                    PlayerData player = ReadPlayerData(filePath);

                    if (!playersData.ContainsKey(steamId))
                    {
                        playersData.Add(steamId, player);
                    }
                    else
                    {
                    }
                }

                // Trier les joueurs par Elo en ordre décroissant
                var sortedPlayers = playersData.OrderByDescending(x => x.Value.Elo);

                // Écrire les données triées dans le fichier de sortie
                using StreamWriter sw = new(outputFilePath);
                foreach (var player in sortedPlayers)
                {
                    sw.WriteLine($"{player.Key};{player.Value.Username};{player.Value.Elo};{player.Value.CGGOWon};{player.Value.CGGOPlayed};{player.Value.AverageCGGOKill};{player.Value.AverageCGGODeath};{player.Value.AverageCGGOAssist};{player.Value.CGGOHeadShotPercent};{player.Value.CGGOBodyShotPercent};{player.Value.CGGOLegsShotPercent};{player.Value.AverageCGGODamageDealt};{player.Value.AverageCGGODamageReceived};{player.Value.AverageCGGOMoneyEfficiency};{player.Value.AverageCGGOScore}");
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error[ProcessPlayerFile] : " + ex.Message);
            }
        }

        public static void CreatePlayerFile(string steamId)
        {
            string path = playersDataFolderPath + steamId + ".txt";
            try
            {
                if (!File.Exists(path))
                {
                    using StreamWriter sw = File.CreateText(path);
                    sw.WriteLine("username:Unknow");
                    sw.WriteLine("elo:1000");
                    sw.WriteLine("lastElo:1000");
                    sw.WriteLine("highestElo:1000");
                    sw.WriteLine("rank:Gold");
                    sw.WriteLine("language:EN");
                    sw.WriteLine("gamePlayed:0");
                    sw.WriteLine("level:0");
                    sw.WriteLine("win:0");
                    sw.WriteLine("death:0");
                    sw.WriteLine("kills:0");
                    sw.WriteLine("banned:0");
                    sw.WriteLine("totalTimePlayed:0");
                    sw.WriteLine("CGGOPlayed:0");
                    sw.WriteLine("CGGOWon:0");
                    sw.WriteLine("moonwalkPercent:0");
                    sw.WriteLine("roundPlayed:0");
                    sw.WriteLine("averageRoundDuration:0");
                    sw.WriteLine("averageSpeed:0");
                    sw.WriteLine("averageConcurrent:0");
                    sw.WriteLine("averageEloDelta:0");
                    sw.WriteLine("averageCGGOHeadShot:0");
                    sw.WriteLine("averageCGGOBodyShot:0");
                    sw.WriteLine("averageCGGOLegsShot:0");
                    sw.WriteLine("averageCGGOKill:0");
                    sw.WriteLine("averageCGGODeath:0");
                    sw.WriteLine("averageCGGOAssist:0");
                    sw.WriteLine("averageCGGODefuse:0");
                    sw.WriteLine("averageCGGOMoneyEfficiency:0");
                    sw.WriteLine("averageCGGODamageReceived:0");
                    sw.WriteLine("averageCGGODamageDealt:0");
                    sw.WriteLine("averageCGGOScore:0");
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error[CreatePlayerFile] : " + ex.Message);
            }
        }
        public static void ProcessNewMessage(List<string> list, string newMessage)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                list[i] = list[i - 1];
            }
            list[0] = newMessage;
        }
        public static void ReadPerms(string filePath)
        {
            try
            {
                using StreamReader sr = new(filePath);
                permsPlayers.Clear();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Skip empty lines or lines that are just whitespace
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Ensure the line has at least one part before trying to split and parse it
                    string[] parts = line.Split('|');
                    if (parts.Length > 0 && ulong.TryParse(parts[0], out ulong playerId))
                    {
                        permsPlayers.Add(playerId);
                    }
                    else
                    {
                        Utility.Log(logFilePath, $"Error ReadPerms: Invalid line format or PlayerId in line '{line}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Error ReadPerms: " + ex.Message);
            }
        }

        public static void ReadBanned(string filePath)
        {
            try
            {
                using StreamReader sr = new(filePath);
                bannedPlayers.Clear();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Skip empty lines or lines that are just whitespace
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Ensure the line has at least 3 parts before trying to split and parse it
                    string[] parts = line.Split('|');
                    if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[0]) && long.TryParse(parts[2], out long unbanDate))
                    {
                        string playerId = parts[0];
                        bannedPlayers.Add(playerId, unbanDate);
                    }
                    else
                    {
                        Utility.Log(logFilePath, $"Error ReadBanned data: Invalid line format or unban date in line '{line}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Error ReadBanned data: " + ex.Message);
            }
        }

        public static void ReadWordsFilter(string filePath)
        {
            wordsFilterList.Clear();
            wordsFilterList.Add("ezrgpjbzj");
            try
            {
                using StreamReader sr = new(filePath);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string word = line;

                    wordsFilterList.Add(word);

                }
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, "Error reading word filter: " + ex.Message);
            }
        }
        public static string FindChildren(GameObject parent, string path)
        {
            Transform target = parent.transform.Find(path);
            if (target != null && target.childCount > 0)
            {
                return target.GetChild(0).name;
            }
            else
            {
                return "null";
            }
        }
        public static void SendPrivateMessageWithWaterMark(ulong clientId, string message)
        {
            string privateMessage = $"|{message}";
            List<byte> bytes = [];
            bytes.AddRange(BitConverter.GetBytes((int)ServerSendType.sendMessage));
            bytes.AddRange(BitConverter.GetBytes((ulong)1));

            string username = SteamFriends.GetFriendPersonaName(new CSteamID(1));
            bytes.AddRange(BitConverter.GetBytes(username.Length));
            bytes.AddRange(Encoding.ASCII.GetBytes(username));

            bytes.AddRange(BitConverter.GetBytes(privateMessage.Length));
            bytes.AddRange(Encoding.ASCII.GetBytes(privateMessage));

            bytes.InsertRange(0, BitConverter.GetBytes(bytes.Count));

            Packet packet = new()
            {
                field_Private_List_1_Byte_0 = new()
            };
            foreach (byte b in bytes)
                packet.field_Private_List_1_Byte_0.Add(b);

            SteamPacketManager.SendPacket(new CSteamID(clientId), packet, 8, SteamPacketDestination.ToClient);
        }

        //Cette fonction envoie un message dans le chat de la part du client en mode Force (seul le client peut voir le message)
        public static void ForceMessage(string message)
        {
            ChatBox.Instance.ForceMessage(message);
        }

        //Cette fonction envoie un message dans le chat de la part du server, marche uniquement en tant que Host de la partie
        public static void SendServerMessage(string message)
        {
            Utility.ProcessNewMessage(messagesList, $"#srv#|{message}");
            ServerSend.SendChatMessage(1, $"|{message}");
        }

        //Cette Fonction permet d'écrire une ligne dans un fichier txt
        public static void Log(string path, string line)
        {
            // Utiliser StreamWriter pour ouvrir le fichier et écrire à la fin
            using StreamWriter writer = new(path, true);
            writer.WriteLine(line.Trim()); // Écrire la nouvelle ligne sans les espaces à la fin
        }

        //Cette fonction vérifie si une fonction crash sans interrompre le fonctionnement d'une class/fonction, et retourne un booleen
        public static bool DoesFunctionCrash(Action function)
        {
            try
            {
                function.Invoke();
                return false;
            }
            catch
            {
                return true;
            }
        }

        //Cette fonction créer un dossier si il n'existe pas déjà
        public static void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch { }
        }

        //Cette fonction créer un fichier si il n'existe pas déjà
        public static void CreateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using StreamWriter sw = File.CreateText(path);
                    sw.WriteLine("");
                }
            }
            catch { }
        }

        //Cette fonction réinitialise un fichier
        public static void ResetFile(string path)
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(path))
                {
                    using StreamWriter sw = new(path, false);
                }
            }
            catch { }
        }

        public static void ReadConfigFile()
        {
            string[] lines = File.ReadAllLines(configFilePath);
            Dictionary<string, string> config = [];
            CultureInfo cultureInfo = new("fr-FR");
            bool parseSuccess;

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    config[key] = value;
                }
            }
            menuKey = config["menuKey"];

            parseSuccess = int.TryParse(config["messageTimer"], out int resultInt);
            messageTimer = parseSuccess ? resultInt : 30;

            parseSuccess = int.TryParse(config["playerToAutoStart"], out resultInt);
            playerToAutoStart = parseSuccess ? resultInt : 2;

            parseSuccess = bool.TryParse(config["displayRankInChat"], out bool resultBool);
            displayRankInChat = parseSuccess ? resultBool : false;

            parseSuccess = bool.TryParse(config["wordsFilter"], out resultBool);
            wordsFilter = parseSuccess ? resultBool : false;
        }

        public static void PlayMenuSound()
        {
            if (clientBody == null) return;
            PlayerInventory.Instance.woshSfx.pitch = 3;
            PlayerInventory.Instance.woshSfx.Play();
        }

        public static void SetConfigFile(string configFilePath)
        {
            Dictionary<string, string> configDefaults = new()
            {
                {"version", "v0.1.1"},
                {"menuKey", "f5"},
                {"messageTimer", "30"},
                {"playerToAutoStart", "5"},
                {"displayRankInChat", "true"},
                {"wordsFilter", "true"},
            };

            Dictionary<string, string> currentConfig = [];

            // If the file exists, read current config
            if (File.Exists(configFilePath))
            {
                string[] lines = File.ReadAllLines(configFilePath);

                foreach (string line in lines)
                {
                    string[] keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        currentConfig[keyValue[0]] = keyValue[1];
                    }
                }
            }

            // Merge current config with defaults
            foreach (KeyValuePair<string, string> pair in configDefaults)
            {
                if (!currentConfig.ContainsKey(pair.Key))
                {
                    currentConfig[pair.Key] = pair.Value;
                }
            }

            // Save merged config
            using StreamWriter sw = File.CreateText(configFilePath);
            foreach (KeyValuePair<string, string> pair in currentConfig)
            {
                sw.WriteLine(pair.Key + "=" + pair.Value);
            }

        }
    }

    //Cette class regroupe un ensemble de fonction relative aux données de la partie
    public class GameData
    {
        public static string CommandPlayerFinder(string identifier)
        {
            if (identifier.Contains("#"))
            {
                if (int.TryParse(identifier.Replace("#", ""), out int playerNumber))
                {
                    return GetPlayerSteamId(playerNumber);
                }
                else
                {
                    Utility.SendServerMessage("Invalid number");
                    return null;
                }
            }
            else
            {
                return GetPlayerSteamId(identifier);
            }
        }
        public static string GetPlayerSteamId(int playerNumber)
        {
            foreach (var player in playersList)
            {
                if (player.Value.playerNumber == playerNumber)
                {
                    return player.Value.steamProfile.m_SteamID.ToString();
                }
            }
            return null;
        }
        public static string GetPlayerSteamId(string username)
        {
            foreach (var player in playersList)
            {
                if (player.Value.username.Contains(username, StringComparison.OrdinalIgnoreCase))
                {
                    return player.Value.steamProfile.m_SteamID.ToString();
                }
            }
            return null;
        }
        public static PlayerManager GetPlayerFirst(string steamId)
        {
            foreach (var player in GameManager.Instance.activePlayers)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID.ToString() == steamId)
                        return player.value;
                }
                catch { }
            }
            foreach (var player in GameManager.Instance.spectators)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID.ToString() == steamId)
                        return player.value;
                }
                catch { }
            }
            return null;
        }

        public static PlayerManager GetPlayer(string steamId)
        {
            if (playersList.ContainsKey(ulong.Parse(steamId)))
                return playersList[ulong.Parse(steamId)];
            else
                return null;
        }

        public static int GetCurrentGameTimer()
        {
            return UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Seconds;
        }
        //Cette fonction retourne le GameState de la partie en cours
        public static string GetGameState()
        {
            return GameManager.Instance.gameMode.modeState.ToString();
        }

        public static void SetGameTime(int time)
        {
            GameManager.Instance.gameMode.SetGameModeTimer(time, 1);
        }

        //Cette fonction retourne le LobbyManager
        public static LobbyManager GetLobbyManager()
        {
            return LobbyManager.Instance;
        }

        public static SteamManager GetSteamManager()
        {
            return SteamManager.Instance;
        }

        //Cette fonction retourne l'id de la map en cours
        public static int GetMapId()
        {
            return GetLobbyManager().map.id;
        }

        //Cette fonction retourne l'id du mode en cours
        public static int GetModeId()
        {
            return GetLobbyManager().gameMode.id;
        }

        //Cette fonction retourne le nom du mode en cours
        public static string GetModeName()
        {
            return UnityEngine.Object.FindObjectOfType<LobbyManager>().gameMode.modeName;
        }

        //Cette fonction retourne le GameManager
        public static GameManager GetGameManager()
        {
            return GameManager.Instance;
        }
    }
    public class ClientData
    {
        //Cette fonction retourne le GameObject du client
        public static GameObject GetClientObject()
        {
            return GameObject.Find("/Player");
        }
        //Cette fonction retourne le Rigidbody du client
        public static Rigidbody GetClientBody()
        {
            clientObject = GetClientObject();
            return clientObject == null ? null : GetClientObject().GetComponent<Rigidbody>();
        }
        //Cette fonction retourne le PlayerManager du client
        public static PlayerManager GetClientManager()
        {
            return clientObject == null ? null : GetClientObject().GetComponent<PlayerManager>();
        }

        //Cette fonction retourne la class Movement qui gère les mouvements du client
        public static PlayerMovement GetClientMovement()
        {
            return clientObject == null ? null : GetClientObject().GetComponent<PlayerMovement>();
        }

        //Cette fonction retourne l'inventaire du client
        public static PlayerInventory GetClientInventory()
        {
            return clientObject == null ? null : PlayerInventory.Instance;
        }

        //Cette fonction retourne la Camera du client
        public static Camera GetClientCamera()
        {
            return clientObject == null ? null : UnityEngine.Object.FindObjectOfType<Camera>();
        }

        public static string GetClientRotationString()
        {
            return clientObject == null ? "(0,0,0,0)" : GetClientCamera().transform.rotation.ToString();
        }

        //Cette fonction retourne la position du client
        public static string GetClientPositionString()
        {
            return clientObject == null ? "(0,0,0)" : GetClientBody().transform.position.ToString();
        }

        public static string GetClientSpeedString()
        {
            return clientObject == null ? "N/A" : clientBody.velocity.ToString();
        }
    }

    public class ItemsUsageTracker
    {
        public static GameObject FindPlayerObjectFromWeapon(GameObject currentObject)
        {
            while (currentObject != null && currentObject.name != "OnlinePlayer(Clone)")
            {
                currentObject = currentObject.transform.parent?.gameObject;
            }
            return currentObject?.name == "OnlinePlayer(Clone)" ? currentObject : null;
        }
        public static class MeleeWeaponUsageTracker
        {
            private static readonly Dictionary<string, DateTime> lastMeleeWeaponUsage = [];

            public static string GetMessageForMeleeWeaponUse(string username, int playerNumber, string itemName, ulong steamId)
            {
                DateTime currentTime = DateTime.Now;
                CGGOPlayer valorantPlayer = CGGOPlayer.GetCGGOPlayer(steamId);
                if (lastMeleeWeaponUsage.TryGetValue(username, out DateTime lastUseTime))
                {
                    TimeSpan timeDifference = currentTime - lastUseTime;
                    lastMeleeWeaponUsage[username] = currentTime;
                    if (itemName != "null")
                        lastItemName = itemName;
                    var normalSpeed = lastItemName switch
                    {
                        "Bat(Clone)" => 25,
                        "Katana(Clone)" => 700,
                        "Knife(Clone)" => 700,
                        "MetalPipe(Clone)" => 25,
                        "Stick(Clone)" => 25,
                        "Bomb(Clone)" => 25,
                        _ => 0,
                    };
                    if (timeDifference.TotalMilliseconds <= normalSpeed)
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(logFilePath, $"Sus flag: {valorantPlayer.Username}, {valorantPlayer.SteamId} for MeleeUseTooFast time:{timeDifference.TotalMilliseconds}");
                            valorantPlayer.CheatFlag += 1;

                        }
                        return $"<color=red>[GAC]</color> [C] FastFire [{lastItemName.Replace("(Clone)", "")}] | #{playerNumber} {username} | last use: {timeDifference.TotalMilliseconds:F1} ms";
                    }
                    else
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(logFilePath, $"{valorantPlayer.Username}, {valorantPlayer.SteamId} for MeleeUseTooFast time:{timeDifference.TotalMilliseconds}");
                            if (valorantPlayer.CheatFlag > 0) valorantPlayer.CheatFlag -= 1;
                        }
                        return "null";
                    }
                }
                else
                {
                    lastMeleeWeaponUsage[username] = currentTime;
                    return "null";
                }
            }
        }
        public static class GunUsageTracker
        {
            private static readonly Dictionary<string, DateTime> lastGunUsage = [];

            public static string GetMessageForGunUse(string username, int playerNumber, ulong steamId)
            {
                DateTime currentTime = DateTime.Now;
                if (lastGunUsage.TryGetValue(username, out DateTime lastUseTime))
                {
                    TimeSpan timeDifference = currentTime - lastUseTime;
                    lastGunUsage[username] = currentTime;
                    CGGOPlayer valorantPlayer = CGGOPlayer.GetCGGOPlayer(steamId);
                    if (timeDifference.TotalMilliseconds <= 80f)
                    {
                        if (valorantPlayer != null)
                        {
                            if (timeDifference.TotalMilliseconds > 2f)
                                valorantPlayer.CheatFlag += 1;

                            Utility.Log(logFilePath, $"Sus flag: {valorantPlayer.Username}, {valorantPlayer.SteamId} for GunUsedTooFast time:{timeDifference.TotalMilliseconds}");
                        }
                        return $"<color=red>[GAC]</color>  [C] FastFire [Gun] | #{playerNumber} {username} | last use: {timeDifference.TotalMilliseconds:F1} ms";
                    }
                    else
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(logFilePath, $"{valorantPlayer.Username}, {valorantPlayer.SteamId} for GunUsedTooFast time:{timeDifference.TotalMilliseconds}");
                            if (valorantPlayer.CheatFlag > 0) valorantPlayer.CheatFlag -= 1;
                        }
                        return "null";
                    }
                }
                else
                {
                    lastGunUsage[username] = currentTime;
                    return "null";
                }
            }
        }
    }

    public static class MultiPlayersData
    {
        public static Rigidbody GetOtherPlayerBody()
        {
            Rigidbody rb = null;

            bool result = Utility.DoesFunctionCrash(() =>
            {
                GameData.GetGameManager().activePlayers.entries.ToList()[playerIndex].value.GetComponent<Rigidbody>();
            });

            if (result) rb = null;
            else rb = GameData.GetGameManager().activePlayers.entries.ToList()[playerIndex].value.GetComponent<Rigidbody>();
            

            return rb;
        }
        public static string GetOtherPlayerUsername()
        {
            try
            {
                var activePlayersList = GameData.GetGameManager().activePlayers.entries.ToList();
                var otherPlayerBody = GetOtherPlayerBody();

                if (GetOtherPlayerBody().transform.position == Vector3.zero)
                    return "<color=red>N/A</color>";

                return otherPlayerBody == null ? "<color=red>N/A</color>" : "#" + activePlayersList[playerIndex].value.playerNumber.ToString() + " " + activePlayersList[playerIndex].value.username;
            }
            catch { }
            return "<color=red>N/A</color>";

        }
        public static string GetOtherPlayerPositionAsString()
        {
            var otherPlayerBody = GetOtherPlayerBody();

            return otherPlayerBody == null
                ? Vector3.zero.ToString(customPrecisionFormatTargetPosition)
                : otherPlayerBody.position.ToString(customPrecisionFormatTargetPosition);
        }
        public static Vector3 GetOtherPlayerPosition()
        {
            var otherPlayerBody = GetOtherPlayerBody();

            return otherPlayerBody == null
                ? Vector3.zero
                : otherPlayerBody.position;
        }
        public static string GetOtherPlayerSpeed()
        {
            Vector3 pos = GetOtherPlayerPosition();
            double distance = Vector3.Distance(pos, lastOtherPlayerPosition);
            double speedDouble = distance / 0.05f;
            smoothedSpeed = (float)((smoothedSpeed * smoothingFactor + (1 - smoothingFactor) * speedDouble) * 1.005);
            return smoothedSpeed.ToString("F1");
        }
        public static string GetStatus()
        {
            string mode = GameData.GetModeName();

            if (smoothedSpeed > 45 && mode != "Race")
            {
                statusTrigger += 1;
                if (statusTrigger >= 5 && statusTrigger < 25)
                    return "CHEAT (or Sussy Slope)";
                statusTrigger = 0;
                return "";
            }
            else if (smoothedSpeed > 30 && mode != "Race")
            {
                statusTrigger += 1;
                if (statusTrigger >= 5 && statusTrigger < 25)
                    return "FAST";
                statusTrigger = 0;
                return "";
            }
            else if (smoothedSpeed > 21)
            {
                if (statusTrigger < 5)
                    statusTrigger += 1;
                if (statusTrigger > 5)
                    statusTrigger -= 1;
                if (statusTrigger >= 5)
                    return "MOONWALK";
                return "";
            }
            else if (smoothedSpeed > 5)
            {
                if (statusTrigger < 5)
                    statusTrigger += 1;
                if (statusTrigger > 5)
                    statusTrigger -= 1;
                if (statusTrigger >= 5)
                    return "MOVING";
                return "";
            }
            else if (smoothedSpeed <= 5)
            {
                if (statusTrigger < 5)
                    statusTrigger += 1;
                if (statusTrigger > 5)
                    statusTrigger -= 1;
                if (statusTrigger >= 5)
                    return "IDLE";
                return "";
            }

            if (statusTrigger > 0)
                statusTrigger -= 1;
            return "";
        }
    }

}
