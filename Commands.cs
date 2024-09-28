namespace GibsonCrabGameGlobalOffensive
{
    public class CommandPatchs
    {
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong param_0, string param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || param_0 < 101) return true; // ID < 101 -> ChatSystem Message (ignore)

            string msg = param_1.ToLower();
            if (param_0 == clientId && IsCommand(msg))
            {
                string[] parts = msg.Split(' '); // Split the command and its arguments

                // Admin commands
                switch (parts[0])
                {
                    case "!help":
                        HandleAdminHelpCommand();
                        return false;
                    case "!mutechat":
                        HandleGenericToggleCommand(ref muteChat, CommandMessages.CHAT_TOGGLE);
                        return false;
                    case "!start":
                        HandleStartCommand();
                        return false;
                    case "!rename":
                        HandleRenameCommand(param_1);
                        return false;
                    case "!reset":
                        HandleResetCommand();
                        return false;
                    case "!time":
                        HandleTimeCommand(parts);
                        return false;
                    case "!map":
                        HandleMapCommand(parts);
                        return false;
                    case "!ban":
                        HandleBanCommand(parts);
                        return false;
                    case "!give":
                        HandleGiveCommand(parts);
                        return false;
                    case "!kill":
                        HandleKillCommand(parts);
                        return false;
                    case "!modif":
                        HandleModifCommand(parts);
                        return false;
                    case "!get":
                        HandleGetCommand(parts);
                        return false;
                    case "!cggo":
                        HandleGenericToggleCommand(ref isCGGOActive, CommandMessages.CGGO_TOGGLE);
                        ResetCGGO(false); 
                        return false;
                    case "!team":
                        HandleTeamCommand(parts);
                        HandleStartCommand();
                        return false;
                    case "!tuto":
                        HandleGenericToggleCommand(ref tuto, CommandMessages.TUTO_TOGGLE);
                        return false;
                    case "!perms":
                        HandlePermsCommand(parts);
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
            if (!SteamManager.Instance.IsLobbyOwner() || param_0 < 101 || param_1.StartsWith("|")) return; // ID < 101 -> ChatSystem Message, | -> Server Message (ignore)

            string msg = param_1.ToLower();

            if (IsCommand(msg))
            {
                string[] parts = msg.Split(' '); // Split the command and its arguments
                var player = CGGOPlayer.GetCGGOPlayer(param_0);

                if (permsPlayers.Contains(param_0))
                {
                    // Modo commands
                    switch (parts[0])
                    {
                        case "!help":
                            HandleModoHelpCommand();
                            break;
                        case "!reset":
                            HandleResetCommand();
                            break;
                        case "!cggo":
                            HandleGenericToggleCommand(ref isCGGOActive, CommandMessages.CGGO_TOGGLE);
                            ResetCGGO(false);
                            break;
                        case "!tuto":
                            HandleGenericToggleCommand(ref tuto, CommandMessages.TUTO_TOGGLE);
                            break;
                        case "!team":
                            HandleTeamCommand(parts);
                            HandleStartCommand();
                            break;
                        case "!ban":
                            HandleBanCommand(parts);
                            break;
                        case "!start":
                            HandleStartCommand();
                            break;
                        case "!time":
                            HandleTimeCommand(parts);
                            break;
                        case "!mutechat":
                            HandleGenericToggleCommand(ref muteChat, CommandMessages.CHAT_TOGGLE);
                            break;
                        case "!map":
                            HandleMapCommand(parts);
                            break;
                        case "!kick":
                            HandleKickCommand(parts);
                            break;
                    }
                }
                // Player Commands
                switch (parts[0])
                {
                    case "!help" or "/help" or "!h" or "/h":
                        HandlePlayerHelpCommand(param_0);
                        break;
                    case "!discord" or "/discord" or "!d" or "/d" or "!disc" or "/disc" or "!cord" or "/cord":
                        HandleDiscordCommand(param_0);
                        break;
                    case "!dev" or "/dev":
                        HandleDevCommand(param_0);
                        break;
                    case "!report" or "/report":
                        HandleReportCommand(parts, param_0, param_1);
                        break;
                    case "!kd" or "/kd":
                        HandleKDCommand(parts, param_0);
                        break;
                    case "!win" or "/win":
                        HandleWinCommand(parts, param_0);
                        break;
                    case "!level" or "/level":
                        HandleLevelCommand(parts, param_0);
                        break;
                    case "!leaderboard" or "/leaderboard":
                        HandleLeaderboardCommand(parts, param_0);
                        break;
                    case "!elo" or "/elo":
                        HandleEloCommand(parts, param_0);
                        break;
                    case "!stats" or "/stats":
                        HandleStatsCommand(parts, param_0);
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
                        HandleGenericLanguageCommand(param_0,CommandMessages.MESSAGE_FR,CommandMessages.LANGUAGE_FR);
                        break;
                    case "!en" or "/en":
                        HandleGenericLanguageCommand(param_0, CommandMessages.MESSAGE_EN, CommandMessages.LANGUAGE_EN);
                        break;
                    case "!de" or "/de":
                        HandleGenericLanguageCommand(param_0, CommandMessages.MESSAGE_DE, CommandMessages.LANGUAGE_DE);
                        break;
                    case "!es" or "/es":
                        HandleGenericLanguageCommand(param_0, CommandMessages.MESSAGE_ES, CommandMessages.LANGUAGE_ES);
                        break;
                    case "!ru" or "/ru":
                        HandleGenericLanguageCommand(param_0, CommandMessages.MESSAGE_RU, CommandMessages.LANGUAGE_RU);
                        break;
                }
            }
        }
        static void HandleReportCommand(string[] arguments, ulong userId, string param_1)
        {
            if (arguments.Length < 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !report playerName message");
                return;
            }

            string steamId = (arguments.Length >= 2) ? GameData.commandPlayerFinder(arguments[1]) : null;

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var reported = GameData.GetPlayer(steamId);
            var reporter = GameData.GetPlayer(userId.ToString());

            if (reported != null && reporter != null)
            {
                Utility.Log(playersReportFilePath, $"[{userId}] {reporter.username} reported [{steamId}] {reported.username} | message: {param_1}");
                Utility.SendPrivateMessageWithWaterMark(userId, "Report sent");
            }
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);  
        }

        static void HandleEloCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !elo || !elo player");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : userId.ToString();

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            string elo = Utility.GetValue(steamId, "elo");
            string rank = Utility.GetValue(steamId, "rank");
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendPrivateMessageWithWaterMark(userId, $"[{rank}] #{player.playerNumber} {player.username} elo: {elo}");
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);           
        }
        static void HandleLeaderboardCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !leaderboard top || !leaderboard player");
                return;
            }

            if (arguments.Length == 2 && arguments[1].ToLower() == "top")
            {
                string[] partsNumber1 = Utility.GetSpecificLine(playersListFilePath, 1).Split(";");
                string[] partsNumber2 = Utility.GetSpecificLine(playersListFilePath, 2).Split(";");
                string[] partsNumber3 = Utility.GetSpecificLine(playersListFilePath, 3).Split(";");

                Utility.SendPrivateMessageWithWaterMark(userId, $"Top1: [{partsNumber1[2]}] {partsNumber1[1]}");
                Utility.SendPrivateMessageWithWaterMark(userId, $"Top2: [{partsNumber2[2]}] {partsNumber2[1]}");
                Utility.SendPrivateMessageWithWaterMark(userId, $"Top3: [{partsNumber3[2]}] {partsNumber3[1]}");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : userId.ToString();

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            int totalPlayersCount = Utility.GetLineCount(playersListFilePath);
            double placement = Utility.GetPlayerPlacement(playersListFilePath, steamId);
            string elo = Utility.GetValue(steamId, "elo");
            string rank = Utility.GetValue(steamId, "rank");
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendPrivateMessageWithWaterMark(userId, $"[{rank}] #{player.playerNumber} {player.username} elo: {elo} is number {placement}/{totalPlayersCount}");
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleLevelCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !level || !level player");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : userId.ToString();

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            string level = Utility.GetValue(steamId, "level");
            string rank = Utility.GetValue(steamId, "rank");
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendPrivateMessageWithWaterMark(userId, $"[{rank}] #{player.playerNumber} {player.username} is level {level}");
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleWinCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !win || !win player");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : userId.ToString();

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            string win = Utility.GetValue(steamId, "win");
            string rank = Utility.GetValue(steamId, "rank");
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendPrivateMessageWithWaterMark(userId, $"[{rank}] #{player.playerNumber} {player.username} has {win} wins");
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleKDCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 2)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !kd || !kd player");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : userId.ToString();

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            float death = float.Parse(Utility.GetValue(steamId, "death"));
            float kills = float.Parse(Utility.GetValue(steamId, "kills"));
            float kd = kills / death;
            string rank = Utility.GetValue(steamId, "rank");
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendPrivateMessageWithWaterMark(userId, $"[{rank}] #{player.playerNumber} {player.username} has {kills} kills, {death} deaths, KD: {kd}");       
            else Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);    
        }

        static void HandleStatsCommand(string[] arguments, ulong userId)
        {
            if (arguments.Length > 3)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"{CommandMessages.INVALID_ARGUMENT} !stats || !stats player page");
                return;
            }

            string steamId;

            if (arguments.Length >= 2)
            {
                steamId = GameData.commandPlayerFinder(arguments[1]);
            }
            else
            {
                steamId = userId.ToString();
            }

            if (steamId == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            Utility.PlayerData playerData = Utility.ReadPlayerData(playersDataFolderPath + steamId + ".txt");
            var player = GameData.GetPlayer(steamId);

            if (player == null)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            string page = (arguments.Length == 3) ? arguments[2].ToLower() : "general";

            switch (page)
            {
                case "0" or "cggo":
                    SendCGGOStats(userId, playerData);
                    break;
                case "1" or "more":
                    SendDetailedStats(userId, playerData, steamId);
                    break;
                case "general":
                default:
                    SendGeneralStats(userId, playerData, steamId);
                    break;
            }
        }

        private static void SendCGGOStats(ulong userId, Utility.PlayerData playerData)
        {
            Utility.SendPrivateMessageWithWaterMark(userId, $"CGGO Stats");

            if (playerData.CGGOPlayed > 0)
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"GamePlayed: {playerData.CGGOPlayed}, WinRate: {((float)(playerData.CGGOWon * 100) / playerData.CGGOPlayed):F1}%");
            }
            else
            {
                Utility.SendPrivateMessageWithWaterMark(userId, $"GamePlayed: {playerData.CGGOPlayed}, WinRate: N/A");
            }

            Utility.SendPrivateMessageWithWaterMark(userId, $"H.Shot%: {(playerData.CGGOHeadShotPercent * 100):F1}%, B.Shot%: {(playerData.CGGOBodyShotPercent * 100):F1}%, L.Shot%: {(playerData.CGGOLegsShotPercent * 100):F1}%");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Defuse%: {(playerData.CGGODefusePercent * 100):F1}%, EcoScore%: {((1 - playerData.AverageCGGOMoneyEfficiency) * 100):F1}%");
            Utility.SendPrivateMessageWithWaterMark(userId, $"AvgKills: {playerData.AverageCGGOKill:F1}, AvgDeaths: {playerData.AverageCGGODeath:F1}, AvgAssists: {playerData.AverageCGGOAssist:F1}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"AvgDam.Dealt: {playerData.AverageCGGODamageDealt:F1}, AvgDam.Received: {playerData.AverageCGGODamageReceived:F1}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"AvgBattleScore: {(playerData.AverageCGGOScore * 100):F1}");
        }
        private static void SendDetailedStats(ulong userId, Utility.PlayerData playerData, string steamId)
        {
            Utility.SendPrivateMessageWithWaterMark(userId, $"Detailed Stats");
            Utility.SendPrivateMessageWithWaterMark(userId, $"LeaderboardRank: {Utility.GetPlayerPlacement(playersListFilePath, steamId)}/{Utility.GetLineCount(playersListFilePath)}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Rank: {playerData.Rank}, Elo: {playerData.Elo:F1}, High Elo: {playerData.HighestElo:F1}");

            string kdRatio = playerData.Death == 0 ? "1" : (playerData.Kills / (float)playerData.Death).ToString("F1");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Win: {playerData.Win}, Kills: {playerData.Kills}, Death: {playerData.Death}, K/D: {kdRatio}");

            Utility.SendPrivateMessageWithWaterMark(userId, $"GamePlayed: {playerData.GamePlayed}, Level: {playerData.Level}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerData.TotalTimePlayed)}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Moonw%: {(playerData.MoonwalkPercent * 100):F1}%, AvgSpeed: {playerData.AverageSpeed:F1}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"totRound:{playerData.RoundPlayed}, SteamId:{steamId}");
        }
        private static void SendGeneralStats(ulong userId, Utility.PlayerData playerData, string steamId)
        {
            Utility.SendPrivateMessageWithWaterMark(userId, $"General Stats");
            Utility.SendPrivateMessageWithWaterMark(userId, $"LeaderboardRank: {Utility.GetPlayerPlacement(playersListFilePath, steamId)}/{Utility.GetLineCount(playersListFilePath)}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Rank: {playerData.Rank}, Elo: {playerData.Elo:F1}, High Elo: {playerData.HighestElo:F1}");

            string kdRatio = playerData.Death == 0 ? "1" : (playerData.Kills / (float)playerData.Death).ToString("F1");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Win: {playerData.Win}, Kills: {playerData.Kills}, Death: {playerData.Death}, K/D: {kdRatio}");

            Utility.SendPrivateMessageWithWaterMark(userId, $"GamePlayed: {playerData.GamePlayed}, Level: {playerData.Level}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"TimePlayed: {Utility.ConvertSecondsToFormattedTime((int)playerData.TotalTimePlayed)}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"Moonw%: {playerData.MoonwalkPercent:F1}, AvgSpeed: {playerData.AverageSpeed:F1}");
            Utility.SendPrivateMessageWithWaterMark(userId, $"totRound:{playerData.RoundPlayed}, SteamId:{steamId}");
        }

        static void HandleKillCommand(string[] arguments)
        {
            if (arguments.Length != 2)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !kill playerName || !kill *");
                return;
            }

            string steamId = (arguments[1] == "*") ? null : GameData.commandPlayerFinder(arguments[1]);

            if (arguments[1] == "*")
            {
                foreach (var players in playersList)
                {
                    if (!players.Value.dead)
                        ServerSend.PlayerDied(players.Value.steamProfile.m_SteamID, players.Value.steamProfile.m_SteamID, Vector3.zero);
                }
                return;
            }

            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var player = GameData.GetPlayer(steamId);
            if (player != null) ServerSend.PlayerDied(player.steamProfile.m_SteamID, player.steamProfile.m_SteamID, Vector3.zero);
            else Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleGiveCommand(string[] arguments)
        {
            if (arguments.Length < 3) return;

            string identifier = arguments[1];
            int weaponId = int.TryParse(arguments[2], out int id) ? id : -1;
            int ammo = (arguments.Length == 4 && int.TryParse(arguments[3], out int ammunitions)) ? ammunitions : -1;

            if (weaponId == -1 || weaponId >= 14)
            {
                Utility.SendServerMessage("Invalid Weapon Id");
                return;
            }

            string steamId = (identifier == "*") ? null : GameData.commandPlayerFinder(identifier);

            if (steamId == null && identifier != "*")
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            if (identifier == "*")
            {
                foreach (var player in playersList)
                {
                    if (!player.Value.dead)
                    {
                        Variables.weaponId += 1;
                        if (ammo >= 0) ServerSend.DropItem(player.Value.steamProfile.m_SteamID, weaponId, Variables.weaponId, ammo);
                        else GameServer.ForceGiveWeapon(player.Value.steamProfile.m_SteamID, weaponId, Variables.weaponId);
                    }
                }
            }
            else
            {
                Variables.weaponId += 1;
                if (ammo >= 0) ServerSend.DropItem(ulong.Parse(steamId), weaponId, Variables.weaponId, ammo);
                else GameServer.ForceGiveWeapon(ulong.Parse(steamId), weaponId, Variables.weaponId);
            }
        }

        static void HandleBanCommand(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !ban playerName || !ban playerName duration");
                return;
            }

            string steamId = GameData.commandPlayerFinder(arguments[1]);
            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var player = GameData.GetPlayer(steamId);
            if (player == null) return;

            if (arguments.Length == 3)
            {
                long unbanDateUnix = GetUnixTimeWithIncrement(arguments[2]);
                Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been banned for {arguments[2]}!");
                Utility.Log(playersBannedFilePath, $"{steamId}|{player.username}|{unbanDateUnix}");
            }
            else
            {
                Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been banned forever!");
                Utility.Log(playersBannedFilePath, $"{steamId}|{player.username}|-1");
            }

            LobbyManager.Instance.KickPlayer(player.steamProfile.m_SteamID);
        }

        static long GetUnixTimeWithIncrement(string timeIncrement)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int value = int.Parse(timeIncrement[..^1]);
            char unit = timeIncrement[^1];

            return unit switch
            {
                's' => now.AddSeconds(value).ToUnixTimeSeconds(),
                'm' => now.AddMinutes(value).ToUnixTimeSeconds(),
                'd' => now.AddDays(value).ToUnixTimeSeconds(),
                'y' => now.AddYears(value).ToUnixTimeSeconds(),
                _ => throw new ArgumentException("Invalid time unit. Use 's', 'm', 'd', or 'y'.")
            };
        }

        static void HandleKickCommand(string[] arguments)
        {
            if (arguments.Length != 2)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !kick playerName || !kick #playerNumber");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : null;

            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var player = GameData.GetPlayer(steamId);
            if (player != null)
            {
                Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been kicked!");
                LobbyManager.Instance.KickPlayer(player.steamProfile.m_SteamID);
            }
            else Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
            
        }

        static void HandlePermsCommand(string[] arguments)
        {
            if (arguments.Length != 2)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !perms playerName || !perms #playerNumber");
                return;
            }

            string steamId = (arguments.Length == 2) ? GameData.commandPlayerFinder(arguments[1]) : null;

            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var player = GameData.GetPlayer(steamId);
            if (player != null)
            {
                Utility.SendServerMessage($"#{player.playerNumber} {player.username} has been promoted to CGPD officer!");
                Utility.Log(permsFilePath, $"{steamId}|{player.username}");
            }
            else Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND); 
        }

        static void HandleModifCommand(string[] arguments)
        {
            if (arguments.Length != 4)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !modif player key value");
                return;
            }

            string steamId = (arguments.Length == 4) ? GameData.commandPlayerFinder(arguments[1]) : null;

            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            Utility.ModifValue(steamId, arguments[2], arguments[3]);
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendServerMessage($"#{player.playerNumber} {player.username} data successfully modified");
            
            else Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleTeamCommand(string[] arguments)
        {
            if (!isCGGOActive) return;

            List<CGGOPlayer> valorantPlayerList = [];
            System.Random random = new();
            List<string> currentGroup = [];
            bool changeTeam = false;
            int firstTeamId = random.Next(0, 2), secondTeamId = 1 - firstTeamId;

            for (int i = 1; i < arguments.Length; i++)
            {
                if (arguments[i].Equals("vs", StringComparison.OrdinalIgnoreCase))
                {
                    AssignPlayersToTeam(currentGroup, firstTeamId, valorantPlayerList);
                    currentGroup.Clear();
                    changeTeam = true;
                }
                else currentGroup.Add(arguments[i]);
                
            }

            if (currentGroup.Count > 0) AssignPlayersToTeam(currentGroup, changeTeam ? secondTeamId : firstTeamId, valorantPlayerList);

            cggoPlayersList.Clear();
            cggoPlayersList.AddRange(valorantPlayerList);
            isCGGORanked = false;
            ResetCGGO(true);
        }

        static void AssignPlayersToTeam(List<string> playerIdentifiers, int teamId, List<CGGOPlayer> valorantPlayerList)
        {
            foreach (var identifier in playerIdentifiers)
            {
                string steamId = GameData.commandPlayerFinder(identifier);
                if (steamId == null) continue;

                PlayerManager player = GameData.GetPlayer(steamId);
                if (player != null) valorantPlayerList.Add(new CGGOPlayer(player, teamId));
            }
        }

        static void HandleGetCommand(string[] arguments)
        {
            if (arguments.Length != 3)
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !get player key");
                return;
            }

            string steamId = (arguments.Length == 3) ? GameData.commandPlayerFinder(arguments[1]) : null;

            if (steamId == null)
            {
                Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
                return;
            }

            var value = Utility.GetValue(steamId, arguments[2]);
            var player = GameData.GetPlayer(steamId);

            if (player != null) Utility.SendServerMessage($"#{player.playerNumber} {player.username} {arguments[2]}: {value}");
            else Utility.SendServerMessage(CommandMessages.PLAYER_NOT_FOUND);
        }

        static void HandleResetCommand()
        {
            ResetCGGO(false);
            ServerSend.LoadMap(6, 0);
        }

        static void HandleTimeCommand(string[] arguments)
        {
            if (arguments.Length != 2 || !float.TryParse(arguments[1], out float time))
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !time number");
                return;
            }

            UnityEngine.Object.FindObjectOfType<GameManager>().gameMode.SetGameModeTimer(time, 1);
        }

        static void HandleMapCommand(string[] arguments)
        {
            if (arguments.Length != 3 || !int.TryParse(arguments[1], out int firstNumber) || !int.TryParse(arguments[2], out int secondNumber))
            {
                Utility.SendServerMessage($"{CommandMessages.INVALID_ARGUMENT} !map mapId(number) modId(number)");
                return;
            }

            ServerSend.LoadMap(firstNumber, secondNumber);
        }

        static void HandleWeaponPurchase(ulong userId, CGGOPlayer player, int cost, int weaponId, List<int> weaponList)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !isCGGOActive || !publicBuyPhase || player.Balance < cost) return;

            player.MoneyUsed += cost;
            player.Balance -= cost;
            Variables.weaponId++;
            weaponList.Add(weaponId);

            if (weaponId == 6) player.KatanaId = weaponId;

            try
            {
                GameServer.ForceGiveWeapon(userId, weaponId, weaponId);
            }
            catch (Exception ex)
            {
                Utility.Log(logFilePath, $"Error giving weapon {weaponId} to player {userId}: {ex}");
            }
        }

        static void HandleShieldPurchase(CGGOPlayer player, int cost, int shieldValue)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !isCGGOActive || !publicBuyPhase || player.Shield >= shieldValue || player.Balance < cost) return;

            player.Balance -= cost;
            player.Shield = shieldValue;
        }

        static bool IsCommand(string msg) => msg.StartsWith("!") || msg.StartsWith("/");

        static void HandleAdminHelpCommand()
        {
            Utility.SendServerMessage(CommandMessages.ADMIN_HELP_1);
            Utility.SendServerMessage(CommandMessages.ADMIN_HELP_2);
        }

        static void HandleModoHelpCommand()
        {
            Utility.SendServerMessage(CommandMessages.MODO_HELP_1);
            Utility.SendServerMessage(CommandMessages.MODO_HELP_2);
        }

        static void HandlePlayerHelpCommand(ulong userId)
        {
            Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_HELP_1);
            Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.PLAYER_HELP_2);
        }

        static void HandleGenericToggleCommand(ref bool flag, string msg)
        {
            flag = !flag;
            Utility.SendServerMessage(flag ? $"{msg} ON" : $"{msg} OFF");
        }

        static void HandleStartCommand() => GameLoop.Instance.StartGames();
        static void HandleRenameCommand(string msg)
        {
            SteamMatchmaking.SetLobbyData((CSteamID)SteamManager.Instance.currentLobby.m_SteamID, "LobbyName", msg.Replace("!rename", ""));
            Utility.SendServerMessage(CommandMessages.RENAME_SERVER);
        }

        static void HandleDiscordCommand(ulong userId) => Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.DISCORD);

        static void HandleDevCommand(ulong userId) => Utility.SendPrivateMessageWithWaterMark(userId, CommandMessages.DEV);

        static void HandleGenericLanguageCommand(ulong userId, string msg, string language)
        {
            Utility.SendPrivateMessageWithWaterMark(userId, msg);
            Utility.ModifValue(userId.ToString(), "language", language);
        }
    }
}
