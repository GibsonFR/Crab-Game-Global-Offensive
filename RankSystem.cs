namespace GibsonCrabGameGlobalOffensive
{
    public class CombatScore
    {
        public static (List<CGGOPlayer> rankedPlayers, List<double> playerScores) RankPlayers(List<CGGOPlayer> playersList, bool shouldEditScore)
        {
            double totalKills = playersList.Sum(p => p.Kills);
            double totalDeaths = playersList.Sum(p => p.Deaths);
            double totalAssists = playersList.Sum(p => p.Assists);

            var rankedPlayersWithScores = playersList
                .Select(p => new { Player = p, Score = CalculatePlayerScore(p, totalKills, totalDeaths, totalAssists) })
                .OrderByDescending(p => p.Score)
                .ToList();

            // Extract ranked players and their scores
            List<CGGOPlayer> rankedPlayers = new List<CGGOPlayer>(rankedPlayersWithScores.Count);
            List<double> playerScores = new List<double>(rankedPlayersWithScores.Count);

            foreach (var entry in rankedPlayersWithScores)
            {
                rankedPlayers.Add(entry.Player);
                playerScores.Add(entry.Score);

                if (shouldEditScore) entry.Player.Score = entry.Score;     
            }
            return (rankedPlayers, playerScores);
        }


        private static double CalculatePlayerScore(CGGOPlayer player, double totalKills, double totalDeaths, double totalAssists)
        {
            // Calculate player Stats
            double shotTotal = player.Shot > 0 ? (double)player.Shot : 1; // Avoid division by 0
            double headshotRatio = (double)player.Headshot / shotTotal;
            double bodyshotRatio = (double)player.Bodyshot / shotTotal;
            double legsshotRatio = (double)player.Legsshot / shotTotal;
            double moneyEfficiency = player.MoneyReceived > 0 ? (double)player.MoneyUsed / player.MoneyReceived : 1;
            double defuseEfficiency = (double)player.Defuse / 6;
            double damageEfficiency = player.DamageReceived > 0 ? (double)player.DamageDealt / (player.DamageDealt + player.DamageReceived) : 1;
            double killContribution = totalKills > 0 ? (double)player.Kills / totalKills : 0;
            double deathContribution = totalDeaths > 0 ? (double)player.Deaths / totalDeaths : 0;
            double assistContribution = totalAssists > 0 ? (double)player.Assists / totalAssists : 0;

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

    public class StatsFunctions
    {
        public static void UpdatePlayerStats(CGGOPlayer player, int winnerTeamId)
        {
            string steamId = player.SteamId.ToString();

            // Retrieve the number of games played
            int cggoPlayed = GetStatValueOrDefault(steamId, "CGGOPlayed", 0);

            // Increment games played
            Utility.ModifValue(steamId, "CGGOPlayed", (cggoPlayed + 1).ToString());

            // If the player is on the winning team, increment games won
            if (winnerTeamId == player.Team)
            {
                int cggoWon = GetStatValueOrDefault(steamId, "CGGOWon", 0);
                Utility.ModifValue(steamId, "CGGOWon", (cggoWon + 1).ToString());
            }

            // Calculate various player stats
            float headshotRatio = CalculateRatio(player.Headshot, player.Shot);
            float bodyshotRatio = CalculateRatio(player.Bodyshot, player.Shot);
            float legsshotRatio = CalculateRatio(player.Legsshot, player.Shot);
            float moneyEfficiency = CalculateRatio(player.MoneyUsed, player.MoneyReceived);
            float defuseEfficiency = (float)player.Defuse / 6;

            // Update player stats using helper method
            UpdateStatHelper("HeadShot", headshotRatio, cggoPlayed, steamId);
            UpdateStatHelper("BodyShot", bodyshotRatio, cggoPlayed, steamId);
            UpdateStatHelper("LegsShot", legsshotRatio, cggoPlayed, steamId);
            UpdateStatHelper("Kill", player.Kills, cggoPlayed, steamId);
            UpdateStatHelper("Death", player.Deaths, cggoPlayed, steamId);
            UpdateStatHelper("Assist", player.Assists, cggoPlayed, steamId);
            UpdateStatHelper("Defuse", defuseEfficiency, cggoPlayed, steamId);
            UpdateStatHelper("MoneyEfficiency", moneyEfficiency, cggoPlayed, steamId);
            UpdateStatHelper("DamageDealt", player.DamageDealt, cggoPlayed, steamId);
            UpdateStatHelper("DamageReceived", player.DamageReceived, cggoPlayed, steamId);
            UpdateStatHelper("Score", (float)player.Score, cggoPlayed, steamId);
        }

        // Helper method to calculate ratios and avoid division by zero
        private static float CalculateRatio(float numerator, float denominator)
        {
            return denominator > 0 ? numerator / denominator : 0;
        }

        // Helper method to get a stat value or a default if parsing fails
        private static int GetStatValueOrDefault(string steamId, string statKey, int defaultValue)
        {
            return int.TryParse(Utility.GetValue(steamId, statKey), out int result) ? result : defaultValue;
        }

        // Helper method to update a player's stat average
        private static void UpdateStatHelper(string statName, float averageStat, int cggoPlayed, string steamId)
        {
            float lastAverageStat = GetAverageStatOrDefault(steamId, statName, 0);
            float newAverageStat = ((lastAverageStat * cggoPlayed) + averageStat) / (cggoPlayed + 1);
            Utility.ModifValue(steamId, $"averageCGGO{statName}", newAverageStat.ToString());
        }

        // Helper method to retrieve the average stat or a default value
        private static float GetAverageStatOrDefault(string steamId, string statName, float defaultValue)
        {
            return float.TryParse(Utility.GetValue(steamId, $"averageCGGO{statName}"), out float result) ? result : defaultValue;
        }
    }

    public class EloFunctions
    {
        public static void RankFromElo(string steamId, string rankFR, string rankEN, int playerElo, int minElo, int maxElo)
        {
            if (playerElo > minElo && playerElo <= maxElo)
            {
                string language = Utility.GetValue(steamId, "language");
                if (language == "FR" || language == "EN") Utility.ModifValue(steamId, "rank", (language == "FR") ? rankFR : rankEN);
            }
        }
        public static void UpdatePlayerRank(string steamId)
        {
            float playerEloInt = float.Parse(Utility.GetValue(steamId, "elo"));

            var rankInfos = new List<RankInfo>
            {
                new("Clown", "Clown", 0, clownRatingCeiling),
                new("Bois", "Wood", clownRatingCeiling, woodRatingCeiling),
                new("Argent", "Silver", woodRatingCeiling, silverRatingCeiling),
                new("Or", "Gold", silverRatingCeiling, goldRatingCeiling),
                new("Platine", "Platinum", goldRatingCeiling, platinumRatingCeiling),
                new("Diamant", "Diamond", platinumRatingCeiling, diamondRatingCeiling),
                new("-]Maitre[-", "-]Master[-", diamondRatingCeiling, masterRatingCeiling),
                new("=]GrandMaitre[=", "=]GrandMaster[=", masterRatingCeiling, grandMasterRatingCeiling),
                new("|]Challenger[|", "|]Challenger[|", grandMasterRatingCeiling, challengerRatingCeiling)
            };

            var rankInfo = rankInfos.Find(info => playerEloInt >= info.EloMin && playerEloInt < info.EloMax);

            if (rankInfo != null) RankFromElo(steamId, rankInfo.RankNameEN, rankInfo.RankNameFR, (int)playerEloInt, rankInfo.EloMin, rankInfo.EloMax); 
        }

        // Calculation of probability of winning
        public static float Expectative(float elo1, float elo2)
        {
            return 1.0f / (1.0f + (float)Math.Pow(10.0, (elo2 - elo1) / ratingDifferenceScale)); 
        }

        public static void UpdateEloCGGO(CGGOPlayer player, int playersThisGame, float totalGameExpectative, int playerRank, float averageGameElo, int kFactor)
        {
            string steamId = player.SteamId.ToString();

            // Malus calculation
            float malus = kFactor * ((playersThisGame - 2) - totalGameExpectative) * -1;

            // Retrieving and managing the player's current Elo
            string elo = Utility.GetValue(steamId, "elo");
            if (!float.TryParse(elo, out float playerElo) || float.IsNaN(playerElo))
            {
                // If the Elo is invalid, fallback to 'lastElo', or default to 1000
                elo = Utility.GetValue(steamId, "lastElo");
                if (!float.TryParse(elo, out playerElo) || float.IsNaN(playerElo))
                {
                    playerElo = 1000; // Default value if Elo is invalid
                }
            }

            // Retrieve the highest Elo value, with a fallback to 0 if invalid
            if (!float.TryParse(Utility.GetValue(steamId, "highestElo"), out float highestElo)) highestElo = 0; // Default value if the value is invalid
            

            // Store the current Elo as 'lastElo' before updating it
            Utility.ModifValue(steamId, "lastElo", playerElo.ToString());

            // Base factor and Elo gain calculation
            float baseFactor = (((float)playerRank - 1) / ((float)playersThisGame - 1)) / ((float)playersThisGame / 2);
            float factor = baseFactor;
            factorValue += factor;

            // Elo gain calculation based on K-factor, player rank, and game expectation
            float eloGain = kFactor * (1 - (factor * 2) - Expectative((int)playerElo, (int)averageGameElo));
            eloGain += malus * factor;
            playerElo += eloGain;

            // Send a private message to the player with the Elo change
            string eloChangeMessage = eloGain > 0
                ? $"[+{eloGain:F1}] --> your Elo: {playerElo:F1}"
                : $"[{eloGain:F1}] --> your Elo: {playerElo:F1}";
            Utility.SendPrivateMessageWithWaterMark(ulong.Parse(steamId), eloChangeMessage);

            // Ensure the player's Elo doesn't drop below the minimum threshold
            if (playerElo < 100) playerElo = 100;

            // Update the player's Elo in the database
            Utility.ModifValue(steamId, "elo", playerElo.ToString());

            // Update the highest Elo achieved if the current Elo exceeds it
            if (highestElo < playerElo) Utility.ModifValue(steamId, "highestElo", playerElo.ToString());
        }

        // RankInfo class to store information about ranks
        private class RankInfo(string rankNameEN, string rankNameFR, int eloMin, int eloMax)
        {
            public string RankNameEN { get; } = rankNameEN;
            public string RankNameFR { get; } = rankNameFR;
            public int EloMin { get; } = eloMin;
            public int EloMax { get; } = eloMax;
        }
    }
}
