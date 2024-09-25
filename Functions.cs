
namespace GibsonCrabGameGlobalOffensive
{
    //Ici on stock les fonctions, dans des class pour la lisibilité du code dans Plugin.cs 

    public class EloFunctions
    {
        public static void RankFromElo(string steamId, string rankFR, string rankEN, int playerElo, int minElo, int maxElo)
        {
            if (playerElo > minElo && playerElo <= maxElo)
            {
                string lang = Utility.GetValue(steamId, "language");
                if (lang == "FR" || lang == "EN")
                {
                    Utility.ModifValue(steamId, "rank", (lang == "FR") ? rankFR : rankEN);
                }
            }
        }

        public static void UpdatePlayerRank(string steamId)
        {
            float playerEloInt = float.Parse(Utility.GetValue(steamId, "elo"));

            var rankInfos = new List<RankInfo>
            {
                new("Clown", "Clown", 0, 750),
                new("Bois", "Wood", 750, 850),
                new("Argent", "Silver", 850, 950),
                new("Or", "Gold", 950, 1050),
                new("Platine", "Platinum", 1050, 1100),
                new("Diamant", "Diamond", 1100, 1150),
                new("-]Maitre[-", "-]Master[-", 1150, 1200),
                new("=]GrandMaitre[=", "=]GrandMaster[=", 1200, 1300),
                new("|]Challenger[|", "|]Challenger[|", 1300, 20000)
            };

            var rankInfo = rankInfos.Find(info => playerEloInt >= info.EloMin && playerEloInt < info.EloMax);

            if (rankInfo != null)
            {
                RankFromElo(steamId, rankInfo.RankNameEN, rankInfo.RankNameFR, (int)playerEloInt, rankInfo.EloMin, rankInfo.EloMax);
            }
        }

        public static float Expectative(float elo1, float elo2)
        {
            return 1.0f / (1.0f + (float)Math.Pow(10.0, (elo2 - elo1) / 400.0));
        }

        public static void UpdateEloCGGO(CGGOPlayer player, int playersThisGame, float totalGameExpectative, int playerRank, float averageGameElo, int kFactor, int winnerTeam)
        {
            string steamId = player.SteamId.ToString();

            if (!int.TryParse(Utility.GetValue(steamId, "CGGOPlayed"), out int cggoPlayed))
            {
                cggoPlayed = 0; // Valeur par défaut si la valeur n'est pas valide
            }
            Utility.ModifValue(steamId, "CGGOPlayed", (cggoPlayed + 1).ToString());

            if (winnerTeam == player.Team)
            {
                if (!int.TryParse(Utility.GetValue(steamId, "CGGOWon"), out int cggoWon))
                {
                    cggoWon = 0; // Valeur par défaut si la valeur n'est pas valide
                }
                Utility.ModifValue(steamId, "CGGOWon", (cggoWon + 1).ToString());
            }

            // Calcul des ratios et efficacités
            float headshotRatio = player.Shot > 0 ? (float)player.Headshot / player.Shot : 0;
            float bodyshotRatio = player.Shot > 0 ? (float)player.Bodyshot / player.Shot : 0;
            float legsshotRatio = player.Shot > 0 ? (float)player.Legsshot / player.Shot : 0;
            float moneyEfficiency = player.MoneyReceived > 0 ? (float)player.MoneyUsed / player.MoneyReceived : 0;
            float defuseEfficiency = (float)player.Defuse / 6;

            // Mise à jour des moyennes des statistiques
            UpdateAverageStat("HeadShot", headshotRatio, cggoPlayed, steamId);
            UpdateAverageStat("BodyShot", bodyshotRatio, cggoPlayed, steamId);
            UpdateAverageStat("LegsShot", legsshotRatio, cggoPlayed, steamId);
            UpdateAverageStat("Kill", player.Kills, cggoPlayed, steamId);
            UpdateAverageStat("Death", player.Deaths, cggoPlayed, steamId);
            UpdateAverageStat("Assist", player.Assists, cggoPlayed, steamId);
            UpdateAverageStat("Defuse", defuseEfficiency, cggoPlayed, steamId);
            UpdateAverageStat("MoneyEfficiency", moneyEfficiency, cggoPlayed, steamId);
            UpdateAverageStat("DamageDealt", player.DamageDealt, cggoPlayed, steamId);
            UpdateAverageStat("DamageReceived", player.DamageReceived, cggoPlayed, steamId);
            UpdateAverageStat("Score", (float)player.Score, cggoPlayed, steamId);

            // Calcul du malus
            float malus = kFactor * ((playersThisGame - 2) - totalGameExpectative) * -1;

            // Récupération et gestion de l'Elo actuel
            string elo = Utility.GetValue(steamId, "elo");
            if (!float.TryParse(elo, out float playerElo) || float.IsNaN(playerElo))
            {
                elo = Utility.GetValue(steamId, "lastElo");
                if (!float.TryParse(elo, out playerElo) || float.IsNaN(playerElo))
                {
                    playerElo = 1000; // Valeur par défaut si l'Elo n'est pas valide
                }
            }

            if (!float.TryParse(Utility.GetValue(steamId, "highestElo"), out float highestElo))
            {
                highestElo = 0; // Valeur par défaut si la valeur n'est pas valide
            }

            Utility.ModifValue(steamId, "lastElo", playerElo.ToString());

            // Calcul du facteur de base et du gain d'Elo
            float baseFactor = (((float)playerRank - 1) / ((float)playersThisGame - 1)) / ((float)playersThisGame / 2);
            float factor = baseFactor;
            Variables.factorValue += factor;

            float eloGain = kFactor * (1 - (factor * 2) - Expectative((int)playerElo, (int)averageGameElo));
            eloGain += malus * factor;
            playerElo += eloGain;

            // Mise à jour de l'Elo et envoi d'un message au joueur
            if (eloGain > 0)
                Utility.SendPrivateMessageWithWaterMark(ulong.Parse(steamId), $"[+{eloGain.ToString("F1")}] --> your elo: {playerElo.ToString("F1")}");
            else
                Utility.SendPrivateMessageWithWaterMark(ulong.Parse(steamId), $"[{eloGain.ToString("F1")}] --> your elo: {playerElo.ToString("F1")}");

            // Limite inférieure de l'Elo
            if (playerElo < 100)
                playerElo = 100;

            // Mise à jour de l'Elo dans la base de données
            Utility.ModifValue(steamId, "elo", playerElo.ToString());

            // Mise à jour du plus haut Elo atteint
            if (highestElo < playerElo)
                Utility.ModifValue(steamId, "highestElo", playerElo.ToString());
        }

        private static void UpdateAverageStat(string statName, float averageStat, int cggoPlayed, string steamId)
        {
            if (!float.TryParse(Utility.GetValue(steamId, $"averageCGGO{statName}"), out float lastAverageStat))
            {
                lastAverageStat = 0; // Valeur par défaut si la valeur n'est pas valide
            }
            float newAverageStat = ((lastAverageStat * cggoPlayed) + averageStat) / (cggoPlayed + 1);
            Utility.ModifValue(steamId, $"averageCGGO{statName}", newAverageStat.ToString());
        }

        private class RankInfo
        {
            public string RankNameEN { get; }
            public string RankNameFR { get; }
            public int EloMin { get; }
            public int EloMax { get; }

            public RankInfo(string rankNameEN, string rankNameFR, int eloMin, int eloMax)
            {
                RankNameEN = rankNameEN;
                RankNameFR = rankNameFR;
                EloMin = eloMin;
                EloMax = eloMax;
            }
        }
    }

    public class CommandFunctions
    {
        public static void HandleReportCommand(string[] arguments, ulong param_0, string param_1)
        {
            string steamId = null;
            string identifier = arguments[1];
            steamId = GameData.commandPlayerFinder(identifier);
            if (steamId != null)
            {
                var reported = GameData.GetPlayer(steamId);
                var reporter = GameData.GetPlayer(param_0.ToString());

                Utility.Log(Variables.playersReportFilePath, $"[{param_0}] {reporter.username} reported [{steamId}] {reported.username} | message : {param_1}");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                return;
            }

            Utility.SendPrivateMessageWithWaterMark(param_0, "ReportSent!");
            return;
        }
        public static void HandleEloCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 2) return;
            if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];
                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    string elo = Utility.GetValue(steamId, "elo");
                    string rank = Utility.GetValue(steamId, "rank");
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} elo: {elo}");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                string elo = Utility.GetValue(steamId, "elo");
                string rank = Utility.GetValue(steamId, "rank");
                var player = GameData.GetPlayer(steamId);
                Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} your elo is: {elo}");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "!elo | !elo #playerNumber");
                return;
            }
        }
        public static void HandleLeaderboardCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 2) return;
            if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];

                if (identifier == "top")
                {
                    string[] partsNumber1 = Utility.GetSpecificLine(Variables.playersListFilePath, 1).Split(";");
                    string[] partsNumber2 = Utility.GetSpecificLine(Variables.playersListFilePath, 2).Split(";");
                    string[] partsNumber3 = Utility.GetSpecificLine(Variables.playersListFilePath, 3).Split(";");

                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Top1: [{partsNumber1[2]}] {partsNumber1[1]}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Top2: [{partsNumber2[2]}] {partsNumber2[1]}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Top3: [{partsNumber3[2]}] {partsNumber3[1]}");
                    return;
                }
                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    double placement = Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId);
                    string elo = Utility.GetValue(steamId, "elo");
                    string rank = Utility.GetValue(steamId, "rank");
                    int totalPlayersCount = Utility.GetLineCount(Variables.playersListFilePath);
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} elo: {elo}  is number {placement}/{totalPlayersCount}");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                int placement = Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId);
                int totalPlayersCount = Utility.GetLineCount(Variables.playersListFilePath);
                string elo = Utility.GetValue(steamId, "elo");
                string rank = Utility.GetValue(steamId, "rank");
                var player = GameData.GetPlayer(steamId);
                Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} elo: {elo} you are number {placement}/{totalPlayersCount}");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "!leaderboard top | !leaderboard #playerNumber | !leaderboard");
                return;
            }
        }
        public static void HandleLevelCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 2) return;
            if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    string level = Utility.GetValue(steamId, "level");
                    string rank = Utility.GetValue(steamId, "rank");
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} is level {level}");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                string level = Utility.GetValue(steamId, "level");
                string rank = Utility.GetValue(steamId, "rank");
                var player = GameData.GetPlayer(steamId);
                Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} you are level {level}");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "!win | !win #playerNumber");
                return;
            }
        }
        public static void HandleWinCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 2) return;
            if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];

                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    string win = Utility.GetValue(steamId, "win");
                    string rank = Utility.GetValue(steamId, "rank");
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} have {win} wins");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                string win = Utility.GetValue(steamId, "win");
                string rank = Utility.GetValue(steamId, "rank");
                var player = GameData.GetPlayer(steamId);
                Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} you have {win} win");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "!win | !win #playerNumber");
                return;
            }
        }
        public static void HandleKDCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 2) return;
            if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    float death = float.Parse(Utility.GetValue(steamId, "death"));
                    float kills = float.Parse(Utility.GetValue(steamId, "kills"));

                    float kd = kills / death;
                    string rank = Utility.GetValue(steamId, "rank");
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"[{rank}] #{player.playerNumber} {player.username} have {kills} kill, {death} death, KD: {kd}");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found!");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                float death = float.Parse(Utility.GetValue(steamId, "death"));
                float kills = float.Parse(Utility.GetValue(steamId, "kills"));

                float kd = kills / death;
                string rank = Utility.GetValue(steamId, "rank");
                var player = GameData.GetPlayer(steamId);
                Utility.SendServerMessage($"[{rank}] #{player.playerNumber} {player.username} you have {kills} kill, {death} death, KD: {kd}");
            }
            else
            {
                Utility.SendServerMessage("!kd | !kd #playerNumber");
                return;
            }
        }

        public static void HandleStatsCommand(string[] arguments, ulong param_0)
        {
            if (arguments.Length > 3) return;

            if (arguments.Length == 3)
            {
                string steamId;
                string identifier = arguments[1];
                string page = arguments[2];

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    Utility.PlayerData playerData = Utility.ReadPlayerData(Variables.playersDataFolderPath + steamId + ".txt");
                    var player = GameData.GetPlayer(steamId);
                    switch (page)
                    {
                        case "0" or "cggo":
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"[{19}/19] {"CGGO"} #{player.playerNumber} {player.username}");
                            if (playerData.CGGOPlayed > 0) Utility.SendPrivateMessageWithWaterMark(param_0, $"GamePlayed: {playerData.CGGOPlayed}, WinRate: {((float)(playerData.CGGOWon * 100) / (float)playerData.CGGOPlayed).ToString("F1")}%");
                            else Utility.SendPrivateMessageWithWaterMark(param_0, $"GamePlayed: {playerData.CGGOPlayed}, WinRate: N/A");

                            Utility.SendPrivateMessageWithWaterMark(param_0, $"H.Shot%: {(playerData.CGGOHeadShotPercent * 100).ToString("F1")}%, B.Shot%: {(playerData.CGGOBodyShotPercent * 100).ToString("F1")}%, L.Shot%: {(playerData.CGGOLegsShotPercent * 100).ToString("F1")}%");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"Defuse%: {(playerData.CGGODefusePercent * 100).ToString("F1")}%, EcoScore%: {((1 - playerData.AverageCGGOMoneyEfficiency) * 100).ToString("F1")}%");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"AvgKills: {playerData.AverageCGGOKill.ToString("F1")}, AvgDeaths: {playerData.AverageCGGODeath.ToString("F1")}, AvgAssists: {playerData.AverageCGGOAssist.ToString("F1")}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"AvgDam.Dealt: {playerData.AverageCGGODamageDealt.ToString("F1")}, AvgDam.Received: {playerData.AverageCGGODamageReceived.ToString("F1")}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"AvgBattleScore: {(playerData.AverageCGGOScore * 100).ToString("F1")}");
                            break;
                        case "1" or "more":
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"S T A T S [0/19] #{player.playerNumber} {player.username} [0/19] S T A T S");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"LeaderboardRank: {Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId)}/{Utility.GetLineCount(Variables.playersListFilePath)}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"Rank: {playerData.Rank}, Elo: {playerData.Elo.ToString("F1")}, High Elo: {playerData.HighestElo.ToString("F1")}");
                            string kdRatio = playerData.Death == 0 ? "1" : (playerData.Kills / (float)playerData.Death).ToString("F1");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"Win: {playerData.Win}, Kills: {playerData.Kills}, Death: {playerData.Death}, K/D: {kdRatio}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"GamePlayed: {playerData.GamePlayed}, Level: {playerData.Level}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerData.TotalTimePlayed)}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"Moonw%: {(playerData.MoonwalkPercent * 100).ToString("F1")}%, AvgSpeed: {playerData.AverageSpeed.ToString("F1")}");
                            Utility.SendPrivateMessageWithWaterMark(param_0, $"totRound:{playerData.RoundPlayed}, SteamId:{param_0}");
                            break;
                        default:
                            Utility.SendPrivateMessageWithWaterMark(param_0, "Invalid page -> !stats #playerNumber pageNumber(0-19)");
                            break;
                    }
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found -> !stats #playerNumber pageNumber");
                    return;
                }
            }
            else if (arguments.Length == 2)
            {
                string steamId = null;
                string identifier = arguments[1];

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    Utility.PlayerData playerD = Utility.ReadPlayerData(Variables.playersDataFolderPath + steamId + ".txt");
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"S T A T S [0/19] #{player.playerNumber} {player.username} [0/19] S T A T S");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"LeaderboardRank: {Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId)}/{Utility.GetLineCount(Variables.playersListFilePath)}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Rank: {playerD.Rank}, Elo: {playerD.Elo.ToString("F1")}, High Elo: {playerD.HighestElo.ToString("F1")}");
                    string kdRatio = playerD.Death == 0 ? "1" : (playerD.Kills / (float)playerD.Death).ToString("F1");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Win: {playerD.Win}, Kills: {playerD.Kills}, Death: {playerD.Death}, K/D: {kdRatio}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"GamePlayed: {playerD.GamePlayed}, Level: {playerD.Level}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerD.TotalTimePlayed)}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"Moonw%: {playerD.MoonwalkPercent.ToString("F1")}, AvgSpeed: {playerD.AverageSpeed.ToString("F1")}");
                    Utility.SendPrivateMessageWithWaterMark(param_0, $"totRound:{playerD.RoundPlayed}, SteamId:{param_0}");
                }
                else
                {
                    Utility.SendPrivateMessageWithWaterMark(param_0, "Player not found -> !stats #playerNumber pageNumber");
                    return;
                }
            }
            else if (arguments.Length == 1)
            {
                var steamId = param_0.ToString();
                Utility.PlayerData playerD = Utility.ReadPlayerData(Variables.playersDataFolderPath + steamId + ".txt");
                var player = GameData.GetPlayer(steamId);
                Utility.SendPrivateMessageWithWaterMark(param_0, $"S T A T S [0/19] #{player.playerNumber} {player.username} [0/19] S T A T S");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"LeaderboardRank: {Utility.GetPlayerPlacement(Variables.playersListFilePath, steamId)}/{Utility.GetLineCount(Variables.playersListFilePath)}");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"Rank: {playerD.Rank}, Elo: {playerD.Elo.ToString("F1")}, High Elo: {playerD.HighestElo.ToString("F1")}");
                string kdRatio = playerD.Death == 0 ? "1" : (playerD.Kills / (float)playerD.Death).ToString("F1");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"Win: {playerD.Win}, Kills: {playerD.Kills}, Death: {playerD.Death}, K/D: {kdRatio}");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"GamePlayed: {playerD.GamePlayed}, Level: {playerD.Level}");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerD.TotalTimePlayed)}");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"Moonw%: {playerD.MoonwalkPercent.ToString("F1")}, AvgSpeed: {playerD.AverageSpeed.ToString("F1")}");
                Utility.SendPrivateMessageWithWaterMark(param_0, $"totRound:{playerD.RoundPlayed}, SteamId:{param_0}");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(param_0, "!stats | !stats #playerNumber | !stats #playerNumber pageNumber");
                return;
            }
        }
        public static void HandleKillCommand(string[] arguments)
        {
            if (arguments.Length == 2) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];

                string steamId = null;

                if (identifier == "*")
                {
                    foreach (var players in Variables.playersList)
                    {
                        if (players.Value.dead) continue;

                        ServerSend.PlayerDied(players.Value.steamProfile.m_SteamID, players.Value.steamProfile.m_SteamID, Vector3.zero);

                    }
                    return;
                }

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    var player = GameData.GetPlayer(steamId);

                    ServerSend.PlayerDied(player.steamProfile.m_SteamID, player.steamProfile.m_SteamID, Vector3.zero);

                }
                else
                {
                    Utility.SendServerMessage("Player not found (#number or playerName)");
                }
            }
        }
        public static void HandleGiveCommand(string[] arguments)
        {
            if (arguments.Length >= 3) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                int weaponId = -1;
                int ammo = -1;
                if (int.TryParse(arguments[2], out int id))
                    weaponId = id;
                else
                    weaponId = -1;

                if (arguments.Length == 4)
                {
                    if (int.TryParse(arguments[3], out int ammunitions))
                        ammo = ammunitions;
                    else
                        ammo = -1;
                }

                string steamId = null;

                if (identifier == "*")
                {
                    foreach (var player in Variables.playersList)
                    {
                        if (player.Value.dead) continue;

                        if (weaponId != -1 && weaponId < 14)
                        {
                            Variables.weaponId += 1;
                            if (ammo >= 0)
                            {
                                ServerSend.DropItem(player.Value.steamProfile.m_SteamID, weaponId, Variables.weaponId, ammo);
                            }
                            else
                            {
                                GameServer.ForceGiveWeapon(player.Value.steamProfile.m_SteamID, weaponId, Variables.weaponId);
                            }


                        }
                        else
                        {
                            Utility.SendServerMessage("Invalid Weapon Id");
                            return;
                        }

                    }
                    return;
                }

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    var player = GameData.GetPlayer(steamId);

                    if (weaponId != -1 && weaponId < 14)
                    {
                        Variables.weaponId += 1;
                        if (ammo >= 0)
                            ServerSend.DropItem(ulong.Parse(steamId), weaponId, Variables.weaponId, ammo);
                        else
                            GameServer.ForceGiveWeapon(ulong.Parse(steamId), weaponId, Variables.weaponId);
                    }
                    else
                    {
                        Utility.SendServerMessage("Invalid Weapon Id");
                    }

                }
                else
                {
                    Utility.SendServerMessage("Player not found (#number or playerName)");
                }

            }
        }

        public static void HandleBanCommand(string[] arguments)
        {
            if (arguments.Length >= 2) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                string steamId;

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    var player = GameData.GetPlayer(steamId);

                    if (arguments.Length == 3)
                    {
                        string timeIncrement = arguments[2];

                        long unbanDateUnix = GetUnixTimeWithIncrement(timeIncrement);
                        Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been banned for {timeIncrement}!");
                        Utility.Log(Variables.playersBannedFilePath, $"{steamId}|{player.username}|{unbanDateUnix}");
                        LobbyManager.Instance.KickPlayer(player.steamProfile.m_SteamID);
                    }
                    else
                    {
                        Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been banned forever!");
                        Utility.Log(Variables.playersBannedFilePath, $"{steamId}|{player.username}|-1");
                        LobbyManager.Instance.KickPlayer(player.steamProfile.m_SteamID);
                    }

                }
                else
                {
                    Utility.ForceMessage("Player not found (#number or playerName)");
                }

            }
            else
            {
                Utility.ForceMessage("Invalid argument --> !ban #playerNumber or !ban playerUsername");
            }

            static long GetUnixTimeWithIncrement(string timeIncrement)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;

                int value = int.Parse(timeIncrement.Substring(0, timeIncrement.Length - 1));
                char unit = timeIncrement[timeIncrement.Length - 1];

                switch (unit)
                {
                    case 's':
                        now = now.AddSeconds(value);
                        break;
                    case 'm':
                        now = now.AddMinutes(value);
                        break;
                    case 'd':
                        now = now.AddDays(value);
                        break;
                    case 'y':
                        now = now.AddYears(value);
                        break;
                    default:
                        throw new ArgumentException("Invalid. Use 's', 'm', 'd', or 'y'.");
                }

                return now.ToUnixTimeSeconds();
            }

        }

        public static void HandleKickCommand(string[] arguments)
        {
            if (arguments.Length == 2) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                string steamId = null;

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    var player = GameData.GetPlayer(steamId);

                    Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been kicked!");
                    LobbyManager.Instance.KickPlayer(player.steamProfile.m_SteamID);


                }
                else
                {
                    Utility.ForceMessage("Player not found (#number or playerName)");
                }

            }
            else
            {
                Utility.ForceMessage("Invalid argument --> !kick #playerNumber or !kick playerUsername");
            }
        }

        public static void HandlePermsCommand(string[] arguments)
        {
            if (arguments.Length == 2) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                string steamId = null;

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been promoted to CGPD officer!");
                    Utility.Log(Variables.permsFilePath, $"{steamId}|{player.username}");
                }
                else
                {
                    Utility.ForceMessage("Player not found (#number or playerName)");
                }

            }
            else
            {
                Utility.ForceMessage("Invalid argument --> !perms #playerNumber or !perms playerUsername");
            }
        }
        public static void HandleModifCommand(string[] arguments)
        {
            if (arguments.Length == 4) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                string key = arguments[2];
                string value = arguments[3];


                string steamId = null;

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    // Appeler la méthode ModifValue avec les informations extraites
                    Utility.ModifValue(steamId, key, value);
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendServerMessage($"#{player.playerNumber} {player.username} data successfully modified");
                }
                else
                {
                    Utility.SendServerMessage("Player not found");
                }

            }
            else
            {
                Utility.SendServerMessage("!modif |player| |key| |value|");
            }
        }

        public static void HandleTeamCommand(string[] arguments)
        {
            List<CGGOPlayer> valorantPlayerList = new();
            System.Random random = new();
            List<string> currentGroup = new();
            bool changeTeam = false;
            int firstTeamId = random.Next(0, 2);
            int secondTeamId = 1 - firstTeamId;

            // Debug: Start of method
            Utility.Log(Variables.logFilePath, $"[DEBUG] Start of HandleTeamCommand with arguments: {string.Join(", ", arguments)}");
            Utility.Log(Variables.logFilePath, $"[DEBUG] Initial random team assignment: FirstTeamId = {firstTeamId}, SecondTeamId = {secondTeamId}");

            for (int i = 1; i < arguments.Length; i++)
            {
                string currentArg = arguments[i];
                Utility.Log(Variables.logFilePath, $"[DEBUG] Processing argument {i}: {currentArg}");

                if (currentArg.Equals("vs", StringComparison.OrdinalIgnoreCase))
                {
                    // Debug: Versus encountered, assigning players to the first team
                    Utility.Log(Variables.logFilePath, $"[DEBUG] 'vs' encountered, assigning players to Team {firstTeamId}");
                    AssignPlayersToTeam(currentGroup, firstTeamId, valorantPlayerList);
                    currentGroup.Clear();
                    changeTeam = true;
                    continue;
                }

                // Add player to the current group
                currentGroup.Add(currentArg);
                Utility.Log(Variables.logFilePath, $"[DEBUG] Added {currentArg} to the current group.");
            }

            // Assign remaining players to the appropriate team
            if (currentGroup.Count > 0)
            {
                int teamId = changeTeam ? secondTeamId : firstTeamId;
                Utility.Log(Variables.logFilePath, $"[DEBUG] Assigning remaining players to Team {teamId}");
                AssignPlayersToTeam(currentGroup, teamId, valorantPlayerList);
            }

            // Debug: Assigning the final list of players to the global variable
            Utility.Log(Variables.logFilePath, $"[DEBUG] Final team assignments completed. Total players assigned: {valorantPlayerList.Count}");

            Variables.cggoPlayersList.Clear();
            Variables.cggoPlayersList.AddRange(valorantPlayerList);

            // Debug: Resetting CGGO state
            Utility.Log(Variables.logFilePath, $"[DEBUG] Resetting CGGO state.");
            Variables.isCGGORanked = false;
            Plugin.CGGO.Reset();

            // Debug: End of method
            Utility.Log(Variables.logFilePath, $"[DEBUG] End of HandleTeamCommand");
        }

        private static void AssignPlayersToTeam(List<string> playerIdentifiers, int teamId, List<CGGOPlayer> valorantPlayerList)
        {
            foreach (var identifier in playerIdentifiers)
            {
                string steamId = GameData.commandPlayerFinder(identifier);
                if (steamId != null)
                {
                    PlayerManager player = GameData.GetPlayer(steamId);
                    if (player != null)
                    {
                        valorantPlayerList.Add(new CGGOPlayer(player, teamId));
                    }
                }
            }
        }

        public static void HandleGetCommand(string[] arguments)
        {
            if (arguments.Length == 3) // Vérifier le nombre d'arguments
            {
                string identifier = arguments[1];
                string key = arguments[2];


                string steamId = null;

                // Check if its number (#)
                steamId = GameData.commandPlayerFinder(identifier);

                if (steamId != null)
                {
                    // Appeler la méthode ModifValue avec les informations extraites
                    var value = Utility.GetValue(steamId, key);
                    var player = GameData.GetPlayer(steamId);
                    Utility.SendServerMessage($"#{player.playerNumber} {player.username} {key}: {value}");

                }
                else
                {
                    Utility.SendServerMessage("Player not found");
                }

            }
            else
            {
                Utility.SendServerMessage("!get |player| |key|");
            }
        }
        public static void HandleResetCommand(string[] arguments)
        {
            ChatBox.Instance.ForceMessage("<color=yellow>[Réinitialisation du jeu]</color>");
            Variables.clientIsDead = false;
            ServerSend.LoadMap(6, 0);
        }
        public static void HandleTimeCommand(string[] arguments)
        {

            if (float.TryParse(arguments[1], out float time))
            {
                UnityEngine.Object.FindObjectOfType<GameManager>().gameMode.SetGameModeTimer(time, 1);
            }
            else
            {
                Utility.ForceMessage("Invalid number --> !time number");
            }

        }
        public static void HandleMapCommand(string[] arguments)
        {

            if (int.TryParse(arguments[1], out int firstNumber) && int.TryParse(arguments[2], out int secondNumber))
            {
                ServerSend.LoadMap(firstNumber, secondNumber);
            }
            else
            {
                Utility.ForceMessage("Invalid numbers --> !map mapId(number) modId(number)");
            }

        }
    }

    //Cette class regroupe un ensemble de fonction plus ou moins utile
    public class Utility
    {
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
            string path = Variables.playersDataFolderPath + steamId + ".txt";

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
            string path = Variables.playersDataFolderPath + steamId + ".txt";
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
            PlayerData player = new PlayerData();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
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
                                Utility.Log(Variables.logFilePath, $"Error parsing player data for key '{key}': {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(Variables.logFilePath, "Error reading player data: " + ex.Message);
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
                    Utility.Log(Variables.logFilePath, "invalid line number.");
                }
            }
            catch (Exception ex)
            {
                Utility.Log(Variables.logFilePath, "Error when reading file : " + ex.Message);
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
                Utility.Log(Variables.logFilePath, "Erreur lors de la recherche du joueur : " + ex.Message);
            }
            return rank;
        }

        public static void ProcessPlayerFiles(string outputFilePath)
        {
            Dictionary<string, PlayerData> playersData = new Dictionary<string, PlayerData>();

            // Remplacez le chemin d'accès avec votre chemin réel
            string folderPath = Variables.playersDataFolderPath;

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
                using (StreamWriter sw = new StreamWriter(outputFilePath))
                {
                    foreach (var player in sortedPlayers)
                    {
                        sw.WriteLine($"{player.Key};{player.Value.Username};{player.Value.Elo};{player.Value.CGGOWon};{player.Value.CGGOPlayed};{player.Value.AverageCGGOKill};{player.Value.AverageCGGODeath};{player.Value.AverageCGGOAssist};{player.Value.CGGOHeadShotPercent};{player.Value.CGGOBodyShotPercent};{player.Value.CGGOLegsShotPercent};{player.Value.AverageCGGODamageDealt};{player.Value.AverageCGGODamageReceived};{player.Value.AverageCGGOMoneyEfficiency};{player.Value.AverageCGGOScore}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(Variables.logFilePath, "Error[ProcessPlayerFile] : " + ex.Message);
            }
        }

        public static void CreatePlayerFile(string steamId)
        {
            string path = Variables.playersDataFolderPath + steamId + ".txt";
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
                Log(Variables.logFilePath, "Error[CreatePlayerFile] : " + ex.Message);
            }
        }
        public static void processNewMessage(List<string> list, string newMessage)
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
                using (StreamReader sr = new StreamReader(filePath))
                {
                    Variables.permsPlayers.Clear();
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
                            Variables.permsPlayers.Add(playerId);
                        }
                        else
                        {
                            Utility.Log(Variables.logFilePath, $"Error ReadPerms: Invalid line format or PlayerId in line '{line}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(Variables.logFilePath, "Error ReadPerms: " + ex.Message);
            }
        }

        public static void ReadBanned(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    Variables.bannedPlayers.Clear();
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
                            Variables.bannedPlayers.Add(playerId, unbanDate);
                        }
                        else
                        {
                            Utility.Log(Variables.logFilePath, $"Error ReadBanned data: Invalid line format or unban date in line '{line}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(Variables.logFilePath, "Error ReadBanned data: " + ex.Message);
            }
        }

        public static void ReadWordsFilter(string filePath)
        {
            Variables.wordsFilterList.Clear();
            Variables.wordsFilterList.Add("ezrgpjbzj");
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string word = line;

                        Variables.wordsFilterList.Add(word);

                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(Variables.logFilePath, "Error reading word filter: " + ex.Message);
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

            Packet packet = new();
            packet.field_Private_List_1_Byte_0 = new();
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
            Utility.processNewMessage(Variables.messagesList, $"#srv#|{message}");
            ServerSend.SendChatMessage(1, $"|{message}");
        }

        //Cette Fonction permet d'écrire une ligne dans un fichier txt
        public static void Log(string path, string line)
        {
            // Utiliser StreamWriter pour ouvrir le fichier et écrire à la fin
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(line.Trim()); // Écrire la nouvelle ligne sans les espaces à la fin
            }
        }

        //Cette fonction vérifie si une fonction crash sans interrompre le fonctionnement d'une class/fonction, et retourne un booleen
        public static bool DoesFunctionCrash(Action function, string functionName, string logPath)
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
        public static void CreateFolder(string path, string logPath)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFolder] : " + ex.Message);
            }
        }

        //Cette fonction créer un fichier si il n'existe pas déjà
        public static void CreateFile(string path, string logPath)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFile] : " + ex.Message);
            }
        }

        //Cette fonction réinitialise un fichier
        public static void ResetFile(string path, string logPath)
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(path))
                {
                    using (StreamWriter sw = new StreamWriter(path, false))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [ResetFile] : " + ex.Message);
            }
        }

        public static void ReadConfigFile()
        {
            string[] lines = System.IO.File.ReadAllLines(Variables.configFilePath);
            Dictionary<string, string> config = new Dictionary<string, string>();
            CultureInfo cultureInfo = new CultureInfo("fr-FR");
            bool resultBool;
            int resultInt;
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
            Variables.menuKey = config["menuKey"];

            parseSuccess = int.TryParse(config["messageTimer"], out resultInt);
            Variables.messageTimer = parseSuccess ? resultInt : 30;

            parseSuccess = int.TryParse(config["playerToAutoStart"], out resultInt);
            Variables.playerToAutoStart = parseSuccess ? resultInt : 2;

            parseSuccess = bool.TryParse(config["displayRankInChat"], out resultBool);
            Variables.displayRankInChat = parseSuccess ? resultBool : false;

            parseSuccess = bool.TryParse(config["wordsFilter"], out resultBool);
            Variables.wordsFilter = parseSuccess ? resultBool : false;
        }

        public static void PlayMenuSound()
        {
            if (Variables.clientBody == null) return;
            PlayerInventory.Instance.woshSfx.pitch = 3;
            PlayerInventory.Instance.woshSfx.Play();
        }

        public static void SetConfigFile(string configFilePath)
        {
            Dictionary<string, string> configDefaults = new Dictionary<string, string>
            {
                {"version", "v0.1.1"},
                {"menuKey", "f5"},
                {"messageTimer", "30"},
                {"playerToAutoStart", "5"},
                {"displayRankInChat", "true"},
                {"wordsFilter", "true"},
            };

            Dictionary<string, string> currentConfig = new Dictionary<string, string>();

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
            using (StreamWriter sw = File.CreateText(configFilePath))
            {
                foreach (KeyValuePair<string, string> pair in currentConfig)
                {
                    sw.WriteLine(pair.Key + "=" + pair.Value);
                }
            }

        }
    }

    //Cette class regroupe un ensemble de fonction relative aux données de la partie
    public class GameData
    {
        public static string commandPlayerFinder(string identifier)
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
            foreach (var player in Variables.playersList)
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
            foreach (var player in Variables.playersList)
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
            if (Variables.playersList.ContainsKey(ulong.Parse(steamId)))
                return Variables.playersList[ulong.Parse(steamId)];
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
            Variables.clientObject = GetClientObject();
            return Variables.clientObject == null ? null : GetClientObject().GetComponent<Rigidbody>();
        }
        //Cette fonction retourne le PlayerManager du client
        public static PlayerManager GetClientManager()
        {
            return Variables.clientObject == null ? null : GetClientObject().GetComponent<PlayerManager>();
        }

        //Cette fonction retourne la class Movement qui gère les mouvements du client
        public static PlayerMovement GetClientMovement()
        {
            return Variables.clientObject == null ? null : GetClientObject().GetComponent<PlayerMovement>();
        }

        //Cette fonction retourne l'inventaire du client
        public static PlayerInventory GetClientInventory()
        {
            return Variables.clientObject == null ? null : PlayerInventory.Instance;
        }

        //Cette fonction retourne la Camera du client
        public static Camera GetClientCamera()
        {
            return Variables.clientObject == null ? null : UnityEngine.Object.FindObjectOfType<Camera>();
        }

        public static string GetClientRotationString()
        {
            return Variables.clientObject == null ? "(0,0,0,0)" : GetClientCamera().transform.rotation.ToString();
        }

        //Cette fonction retourne la position du client
        public static string GetClientPositionString()
        {
            return Variables.clientObject == null ? "(0,0,0)" : GetClientBody().transform.position.ToString();
        }

        public static string GetClientSpeedString()
        {
            return Variables.clientObject == null ? "N/A" : Variables.clientBody.velocity.ToString();
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
            private static readonly Dictionary<string, DateTime> lastMeleeWeaponUsage = new Dictionary<string, DateTime>();

            public static string GetMessageForMeleeWeaponUse(string username, int playerNumber, string itemName, ulong steamId)
            {
                DateTime currentTime = DateTime.Now;
                CGGOPlayer valorantPlayer = CGGOPlayer.GetCGGOPlayer(steamId);
                if (lastMeleeWeaponUsage.TryGetValue(username, out DateTime lastUseTime))
                {
                    TimeSpan timeDifference = currentTime - lastUseTime;
                    lastMeleeWeaponUsage[username] = currentTime;
                    int normalSpeed = 0;

                    if (itemName != "null")
                        Variables.lastItemName = itemName;

                    switch (Variables.lastItemName)
                    {
                        case "Bat(Clone)":
                            normalSpeed = 25;
                            break;
                        case "Katana(Clone)":
                            normalSpeed = 700;
                            break;
                        case "Knife(Clone)":
                            normalSpeed = 700;
                            break;
                        case "MetalPipe(Clone)":
                            normalSpeed = 25;
                            break;
                        case "Stick(Clone)":
                            normalSpeed = 25;
                            break;
                        case "Bomb(Clone)":
                            normalSpeed = 25;
                            break;
                        default:
                            normalSpeed = 0;
                            break;
                    }
                    if (timeDifference.TotalMilliseconds <= normalSpeed)
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(Variables.logFilePath, $"Sus flag: {valorantPlayer.Username}, {valorantPlayer.SteamId} for MeleeUseTooFast time:{timeDifference.TotalMilliseconds}");
                            valorantPlayer.CheatFlag += 1;

                        }
                        return $"<color=red>[GAC]</color> [C] FastFire [{Variables.lastItemName.Replace("(Clone)", "")}] | #{playerNumber} {username} | last use: {timeDifference.TotalMilliseconds.ToString("F1")} ms";
                    }
                    else
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(Variables.logFilePath, $"{valorantPlayer.Username}, {valorantPlayer.SteamId} for MeleeUseTooFast time:{timeDifference.TotalMilliseconds}");
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
            private static readonly Dictionary<string, DateTime> lastGunUsage = new Dictionary<string, DateTime>();

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

                            Utility.Log(Variables.logFilePath, $"Sus flag: {valorantPlayer.Username}, {valorantPlayer.SteamId} for GunUsedTooFast time:{timeDifference.TotalMilliseconds}");
                        }
                        return $"<color=red>[GAC]</color>  [C] FastFire [Gun] | #{playerNumber} {username} | last use: {timeDifference.TotalMilliseconds.ToString("F1")} ms";
                    }
                    else
                    {
                        if (valorantPlayer != null)
                        {
                            Utility.Log(Variables.logFilePath, $"{valorantPlayer.Username}, {valorantPlayer.SteamId} for GunUsedTooFast time:{timeDifference.TotalMilliseconds}");
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

    public class MenuFunctions
    {
        public static void CheckMenuFileExists()
        {
            string menuContent = "\t\r\n\tPosition : [POSITION]  |  Speed : [SPEED]  |  Rotation : [ROTATION]\t\t<b> \r\n\r\n\t______________________________________________________________________</b>\r\n\r\n\r\n\t<b><color=orange>[OTHERPLAYER]</color></b>  |  Position: [OTHERPOSITION]  |  Speed : [OTHERSPEED] | Selecteur :  [SELECTEDINDEX] | <b>Status : [STATUS]</b> \r\n\r\n\t\t\t\r\n\t\r\n\r\n\t______________________________________________________________________\r\n\r\n\t\t\r\n     <b>[MENUBUTTON0]\r\n\r\n\t[MENUBUTTON1]\r\n\r\n\t[MENUBUTTON2]\r\n\r\n\t[MENUBUTTON3]\r\n\r\n\t[MENUBUTTON4]\r\n\r\n\t_______________________________ANTICHEAT_______________________________\r\n\r\n\t[MENUBUTTON5]\r\n\r\n\t[MENUBUTTON6]\r\n\r\n\t[MENUBUTTON7]</b>";

            if (System.IO.File.Exists(Variables.menuFilePath))
            {
                string currentContent = System.IO.File.ReadAllText(Variables.menuFilePath, System.Text.Encoding.UTF8);


                if (currentContent != menuContent)
                {
                    System.IO.File.WriteAllText(Variables.menuFilePath, menuContent, System.Text.Encoding.UTF8);
                }
            }
            else
            {
                // Si le fichier n'existe pas, créez-le avec le contenu fourni
                System.IO.File.WriteAllText(Variables.menuFilePath, menuContent, System.Text.Encoding.UTF8);
            }
        }
        public static void RegisterDataCallbacks(System.Collections.Generic.Dictionary<string, System.Func<string>> dict)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in dict)
            {
                Variables.DebugDataCallbacks.Add(pair.Key, pair.Value);
            }
        }
        public static void LoadMenuLayout()
        {
            Variables.layout = System.IO.File.ReadAllText(Variables.menuFilePath, System.Text.Encoding.UTF8);
        }
        public static void RegisterDefaultCallbacks()
        {
            RegisterDataCallbacks(new System.Collections.Generic.Dictionary<string, System.Func<string>>(){
                {"POSITION", ClientData.GetClientPositionString},
                {"SPEED", ClientData.GetClientSpeedString},
                {"ROTATION", ClientData.GetClientRotationString},
                {"SELECTEDINDEX", () => Variables.playerIndex.ToString()},
                {"OTHERPLAYER", MultiPlayersData.GetOtherPlayerUsername},
                {"OTHERPOSITION", MultiPlayersData.GetOtherPlayerPositionAsString},
                {"OTHERSPEED", MultiPlayersData.GetOtherPlayerSpeed},
                {"STATUS", MultiPlayersData.GetStatus},
                {"MENUBUTTON0",() => Variables.displayButton0},
                {"MENUBUTTON1",() => Variables.displayButton1},
                {"MENUBUTTON2",() => Variables.displayButton2},
                {"MENUBUTTON3",() => Variables.displayButton3},
                {"MENUBUTTON4",() => Variables.displayButton4},
                {"MENUBUTTON5",() => Variables.displayButton5},
                {"MENUBUTTON6",() => Variables.displayButton6},
                {"MENUBUTTON7",() => Variables.displayButton7},
            });
        }

        public static string DisplayButtonState(int index)
        {
            if (Variables.buttonStates[index])
                return "<b><color=red>ON</color></b>";
            else
                return "<b><color=blue>OFF</color></b>";
        }
        public static string FormatLayout()
        {
            string formatted = Variables.layout;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in Variables.DebugDataCallbacks)
            {
                formatted = formatted.Replace("[" + pair.Key + "]", pair.Value());
            }
            return formatted;
        }
        public static string HandleMenuDisplay(int buttonIndex, Func<string> getButtonLabel, Func<string> getButtonSpecificData)
        {
            string buttonLabel = getButtonLabel();

            if (Variables.menuSelector != buttonIndex)
            {
                return $" {buttonLabel} <b>{getButtonSpecificData()}</b>";
            }

            if (!Variables.buttonStates[buttonIndex])
            {
                return $"■<color=yellow>{buttonLabel}</color>■  <b>{getButtonSpecificData()}</b>";
            }
            else
            {
                return $"<color=red>■</color><color=yellow>{buttonLabel}</color><color=red>■</color>  <b>{getButtonSpecificData()}</b>";
            }
        }
        public static string GetSelectedFlungDetectorParam()
        {
            if (Variables.menuSelector == 5)
            {
                switch (Variables.subMenuSelector)
                {
                    case 0:
                        if (Variables.onSubButton)
                            return "  |  " + $"<color=red>■</color><color=orange>Check Frequency : {Variables.checkFrequency.ToString("F2")}</color><color=red>■</color>" + $"  |  Alert Level" + $"  |  Flung Detector Status";
                        else
                            return "  |  " + $"■<color=orange>Check Frequency : {Variables.checkFrequency.ToString("F2")}</color>■" + $"  |  Alert Level" + $"  |  Flung Detector Status";
                    case 1:
                        if (Variables.onSubButton)
                            return "  |  " + $"Check Frequency" + $"  |  <color=red>■</color><color=orange>Alert Level : {Variables.alertLevel.ToString()}</color><color=red>■</color>" + $"  |  Flung Detector Status";
                        else
                            return "  |  " + $"Check Frequency" + $"  |  ■<color=orange>Alert Level : {Variables.alertLevel.ToString()}</color>■" + $"  |  Flung Detector Status";
                    case 2:
                        return "  |  " + $"Check Frequency" + $"  |  Alert Level" + $"  |  ■<color=orange>Flung Dector Status : {Variables.buttonStates[5].ToString()}</color>■";
                    default:
                        return "";
                }
            }
            else
                return "";
        }
        public static void ExecuteSubMenuAction()
        {
            if (!Variables.onButton)
            {
                var selectors = (Variables.menuSelector, Variables.subMenuSelector);

                switch (selectors)
                {
                    case (40, -1):
                        break;
                }
            }
            if (Variables.onButton)
            {
                var selectors = (Variables.menuSelector, Variables.subMenuSelector);

                switch (selectors)
                {
                    case (5, 0):
                        Variables.onSubButton = !Variables.onSubButton;
                        break;
                    case (5, 1):
                        Variables.onSubButton = !Variables.onSubButton;
                        break;
                    case (5, 2):
                        Variables.buttonStates[5] = !Variables.buttonStates[5];

                        if (Variables.buttonStates[5])
                            Utility.ForceMessage("■<color=yellow>(FD))Flung Detector ON</color>■");
                        else
                            Utility.ForceMessage("■<color=yellow>(FD)Flung Detector OFF</color>■");
                        break;
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
                GameData.GetGameManager().activePlayers.entries.ToList()[Variables.playerIndex].value.GetComponent<Rigidbody>();
            }, "GetOtherPlayerBody", Variables.logFilePath);

            if (result)
            {
                rb = null;
            }
            else
            {
                rb = GameData.GetGameManager().activePlayers.entries.ToList()[Variables.playerIndex].value.GetComponent<Rigidbody>();
            }

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

                return otherPlayerBody == null ? "<color=red>N/A</color>" : "#" + activePlayersList[Variables.playerIndex].value.playerNumber.ToString() + " " + activePlayersList[Variables.playerIndex].value.username;
            }
            catch { }
            return "<color=red>N/A</color>";

        }
        public static string GetOtherPlayerPositionAsString()
        {
            var otherPlayerBody = GetOtherPlayerBody();

            return otherPlayerBody == null
                ? Vector3.zero.ToString(Variables.customPrecisionFormatTargetPosition)
                : otherPlayerBody.position.ToString(Variables.customPrecisionFormatTargetPosition);
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
            double distance = Vector3.Distance(pos, Variables.lastOtherPlayerPosition);
            double speedDouble = distance / 0.05f;
            Variables.smoothedSpeed = (float)((Variables.smoothedSpeed * Variables.smoothingFactor + (1 - Variables.smoothingFactor) * speedDouble) * 1.005);
            return Variables.smoothedSpeed.ToString("F1");
        }
        public static string GetStatus()
        {
            string mode = GameData.GetModeName();

            if (Variables.smoothedSpeed > 45 && mode != "Race")
            {
                Variables.statusTrigger += 1;
                if (Variables.statusTrigger >= 5 && Variables.statusTrigger < 25)
                    return "CHEAT (or Sussy Slope)";
                Variables.statusTrigger = 0;
                return "";
            }
            else if (Variables.smoothedSpeed > 30 && mode != "Race")
            {
                Variables.statusTrigger += 1;
                if (Variables.statusTrigger >= 5 && Variables.statusTrigger < 25)
                    return "FAST";
                Variables.statusTrigger = 0;
                return "";
            }
            else if (Variables.smoothedSpeed > 21)
            {
                if (Variables.statusTrigger < 5)
                    Variables.statusTrigger += 1;
                if (Variables.statusTrigger > 5)
                    Variables.statusTrigger -= 1;
                if (Variables.statusTrigger >= 5)
                    return "MOONWALK";
                return "";
            }
            else if (Variables.smoothedSpeed > 5)
            {
                if (Variables.statusTrigger < 5)
                    Variables.statusTrigger += 1;
                if (Variables.statusTrigger > 5)
                    Variables.statusTrigger -= 1;
                if (Variables.statusTrigger >= 5)
                    return "MOVING";
                return "";
            }
            else if (Variables.smoothedSpeed <= 5)
            {
                if (Variables.statusTrigger < 5)
                    Variables.statusTrigger += 1;
                if (Variables.statusTrigger > 5)
                    Variables.statusTrigger -= 1;
                if (Variables.statusTrigger >= 5)
                    return "IDLE";
                return "";
            }

            if (Variables.statusTrigger > 0)
                Variables.statusTrigger -= 1;
            return "";
        }
    }

}
