namespace GibsonCrabGameGlobalOffensive
{
    public class AntiCheatPatchs
    {
        // AntiCheat for Melee weapons
        [HarmonyPatch(typeof(MonoBehaviour2PublicObauTrSiVeSiGahiUnique), nameof(MonoBehaviour2PublicObauTrSiVeSiGahiUnique.AllUse))]
        [HarmonyPostfix]
        public static void OnMeleeUsePost(MonoBehaviour2PublicObauTrSiVeSiGahiUnique __instance)
        {
            if (!buttonStates[7]) return;
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
                        Utility.SendServerMessage(message);
                }
            }
        }

        // AntiCheat for Gun weapons
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
                    if (isCGGOActive && !publicBuyPhase && !publicEndPhase && !publicAllAttackersDead && !publicAllDefendersDead)
                    {
                        CGGOPlayer player = CGGOPlayer.GetCGGOPlayer(playerComponent.steamProfile.m_SteamID);
                        if (player != null) player.Shot += 1;
                    }

                    if (!buttonStates[7]) return;

                    string username = playerComponent.username;
                    int playerNumber = playerComponent.playerNumber;
                    string message = ItemsUsageTracker.GunUsageTracker.GetMessageForGunUse(username, playerNumber, playerComponent.steamProfile.m_SteamID);
                    if (message != "null")
                        Utility.SendServerMessage(message);
                }
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
            if (!buttonStates[5]) return;
            if (tuto) return;
            elapsed += Time.deltaTime;

            if (elapsed > checkFrequency)
            {
                GetAlivePlayersData();
                elapsed = 0f;
            }
        }

        private void GetAlivePlayersData()
        {
            foreach (var player in playersList)
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

            if (playerData.DirectionChanges > alertLevel && playerData.DirectionChanges < 4 && !message)
            {
                if (Vector3.Distance(playerData.ActualPosition, playerData.LastPosition) > 2.5f)
                {
                    Utility.ForceMessage("<color=red>[GAC] </color>[P] " + prob + " | [C] Flung |#" + playerData.PlayerId.ToString() + "  " + playerData.PlayerName);
                    message = true;

                    if (isCGGOActive)
                    {
                        var player = CGGOPlayer.GetCGGOPlayer(ulong.Parse(GameData.GetPlayerSteamId(playerData.PlayerName)));
                        if (player != null)
                        {
                            if (prob == "High")
                            {
                                Utility.Log(logFilePath, $"Cheat Banned: {player.Username}, {player.SteamId} for Flung");
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
}
