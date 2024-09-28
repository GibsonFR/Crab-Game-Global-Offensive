namespace GibsonCrabGameGlobalOffensive
{
    public class CombatScore
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
            double headshotWeight = 0.1;
            double bodyshotWeight = 0.05;
            double legsshotWeight = 0.05;
            double moneyEfficiencyWeight = 0.1;
            double defuseWeight = 0.05;
            double damageEfficiencyWeight = 0.25;
            double killContributionWeight = 0.15;
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
            factorValue += factor;

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
}
