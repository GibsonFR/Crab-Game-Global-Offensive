namespace GibsonCrabGameGlobalOffensive
{
    public static class EconomyConstants
    {
        public const int BOUNTY_PLANTING = 300;
        public const int STARTING_MONEY = 800;
        public const int BOUNTY_WIN_ROUND = 2500;
        public const int BOUNTY_LOSE_ROUND = 1800;
        public const int LOSE_STREAK_BONUS = 300;
        public const int BOUNTY_BASE_ROUND = 1000;
        public const int BOUNTY_ASSIST = 50;
        public const int BOUNTY_KILL = 200;
        public const int BALANCE_CAP = 6000;
        public const int AFK_BONUS = 1800;
    }

    public class EconomySystem
    {
        // Helper method to add money to player's balance and total money received
        private static void AddMoneyToPlayer(CGGOPlayer player, int amount)
        {
            player.Balance += amount;
            player.TotalMoneyReceived += amount;
        }

        // Resets the player's balance to zero
        public static void ResetPlayerBalance(CGGOPlayer player)
        {
            player.Balance = 0;
        }

        // Awards the planting bounty to all attacking players
        public static void GivePlantingBounty(List<CGGOPlayer> attackingPlayers)
        {
            foreach (var player in attackingPlayers)
            {
                AddMoneyToPlayer(player, EconomyConstants.BOUNTY_PLANTING);
            }
        }

        // Gives the starting money to each player in the list
        public static void GiveStartingMoney(List<CGGOPlayer> players)
        {
            foreach (var player in players)
            {
                AddMoneyToPlayer(player, EconomyConstants.STARTING_MONEY);
            }
        }

        // Awards money based on the result of the round
        public static void DistributeEndRoundMoney(List<CGGOPlayer> players, int winningTeam, int loseStreakMultiplier)
        {
            foreach (var player in players)
            {
                if (player.Team == winningTeam)
                {
                    // Winning team gets the win bounty
                    AddMoneyToPlayer(player, EconomyConstants.BOUNTY_WIN_ROUND);
                }
                else if (player.Dead)
                {
                    // Dead players on the losing team get the lose bounty plus lose streak bonus
                    int totalLoseBounty = EconomyConstants.BOUNTY_LOSE_ROUND + (EconomyConstants.LOSE_STREAK_BONUS * loseStreakMultiplier);
                    AddMoneyToPlayer(player, totalLoseBounty);
                }
                else
                {
                    // Surviving players on the losing team get the base round bounty
                    AddMoneyToPlayer(player, EconomyConstants.BOUNTY_BASE_ROUND);
                }
            }
        }

        // Awards the assist bounty to a player
        public static void GiveAssistBounty(CGGOPlayer player)
        {
            AddMoneyToPlayer(player, EconomyConstants.BOUNTY_ASSIST);
        }

        // Awards the kill bounty to a player
        public static void GiveKillBounty(CGGOPlayer player)
        {
            AddMoneyToPlayer(player, EconomyConstants.BOUNTY_KILL);
        }

        // Ensures player balance does not exceed the maximum allowed balance
        public static void CapPlayerBalances(List<CGGOPlayer> players)
        {
            foreach (var player in players)
            {
                if (player.Balance > EconomyConstants.BALANCE_CAP)
                {
                    player.Balance = EconomyConstants.BALANCE_CAP;
                }
            }
        }

        // Calculates an AFK bonus based on the number of players and number of afk players
        public static int CalculateAFKBonus(int playerCount, int afkCount)
        {
            return (int)((EconomyConstants.AFK_BONUS * (float)afkCount) / playerCount);
        }
    }
}
