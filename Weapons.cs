using SteamworksNative;

namespace GibsonCrabGameGlobalOffensive
{
    // All vanilla weapons ID of the game
    public static class WeaponsId
    {
        public const int NON_WEAPON_ID = -1;
        public const int RIFLE_ID = 0;
        public const int PISTOL_ID = 1;
        public const int REVOLVER_ID = 2;
        public const int SHOTGUN_ID = 3;
        public const int BAT_ID = 4;
        public const int BOMB_ID = 5;
        public const int KATANA_ID = 6;
        public const int KNIFE_ID = 7;
        public const int PIPE_ID = 8;
        public const int SNOWBALL_ID = 9;
        public const int STICK_ID = 10;
        public const int MILK_ID = 11;
        public const int PIZZA_ID = 12;
        public const int GRENADE_ID = 13;
    }

    public static class WeaponsConstants
    {
        // Pistol damage values
        public const int PISTOL_LEGS_SHORT_DAMAGE = 14;
        public const int PISTOL_LEGS_LONG_DAMAGE = 12;
        public const int PISTOL_BODY_SHORT_DAMAGE = 22;
        public const int PISTOL_BODY_LONG_DAMAGE = 18;
        public const int PISTOL_HEAD_SHORT_DAMAGE = 58;
        public const int PISTOL_HEAD_LONG_DAMAGE = 42;

        // Shotgun damage values
        public const int SHOTGUN_LEGS_SHORT_DAMAGE = 42;
        public const int SHOTGUN_LEGS_MEDIUM_DAMAGE = 21;
        public const int SHOTGUN_LEGS_LONG_DAMAGE = 14;
        public const int SHOTGUN_BODY_SHORT_DAMAGE = 84;
        public const int SHOTGUN_BODY_MEDIUM_DAMAGE = 42;
        public const int SHOTGUN_BODY_LONG_DAMAGE = 35;
        public const int SHOTGUN_HEAD_SHORT_DAMAGE = 154;
        public const int SHOTGUN_HEAD_MEDIUM_DAMAGE = 77;
        public const int SHOTGUN_HEAD_LONG_DAMAGE = 63;

        // Revolver damage multipliers
        public const float REVOLVER_LEGS_MULTIPLIER_DAMAGE = 0.5f;
        public const float REVOLVER_BODY_MULTIPLIER_DAMAGE = 1.0f;
        public const float REVOLVER_HEAD_MULTIPLIER_DAMAGE = 2.0f;

        // Rifle damage values
        public const int RIFLE_LEGS_DAMAGE = 12;
        public const int RIFLE_BODY_DAMAGE = 20;
        public const int RIFLE_HEAD_DAMAGE = 62;

        // Katana damage value
        public const int KATANA_BASE_DAMAGE = 80;

        // Knife damage value
        public const int KNIFE_BASE_DAMAGE = 34;

        // Weapons Prices
        public const int RIFLE_PRICE = 2900;
        public const int PISTOL_PRICE = 900;
        public const int REVOLVER_PRICE = 4700;
        public const int SHOTGUN_PRICE = 1850;
        public const int KATANA_PRICE = 3200;
    }

    public static class PlayerConstants
    {
        public const int PLAYER_MAX_HEALTH = 100; 
        public const float PLAYER_HEIGHT = 3f; 
    }

    public static class ShieldConstants
    {
        public const float SHIELD_DAMAGE_RATIO = 0.66f; // 66% of damage goes to the shield
        public const int SMALL_SHIELD_VALUE = 25;
        public const int BIG_SHIELD_VALUE = 50;

        // Shields Prices
        public const int SMALL_SHIELD_PRICE = 400;
        public const int BIG_SHIELD_PRICE = 1000;
    }

    public static class WeaponsDistanceConstants
    {
        // Distance thresholds
        public const float MAX_PISTOL_DISTANCE = 80f;
        public const float SHORT_PISTOL_DISTANCE = 60f;

        public const float MAX_SHOTGUN_DISTANCE = 100f;
        public const float MEDIUM_SHOTGUN_DISTANCE = 30f;
        public const float SHORT_SHOTGUN_DISTANCE = 15f;

        public const float MAX_RIFLE_DISTANCE = 120f;

        // Height thresholds for different body parts
        public const float HEAD_THRESHOLD = 2.75f; // Threshold for headshot
        public const float BODY_THRESHOLD = 1.48f; // Threshold for body shot
    }

    public class WeaponsPatchs
    {
        // Prevent players from using snowballs
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.UseItemAll))]
        [HarmonyPrefix]
        public static bool OnServerSendUseItemAllPre(ulong __0, int __1)
        {
            ulong steamId = __0;
            int weaponId = __1;

            if (!Utility.IsHostAndCGGOActive()) return true;

            // Block snowball usage
            return weaponId != WeaponsId.SNOWBALL_ID;
        }

        // Prevent players from dropping snowballs unless it's during the buy phase
        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPrefix]
        public static bool OnGameServerForceGiveWeaponPre(ulong __0, int __1, int __2)
        {
            ulong steamId = __0;
            int weaponId = __1, ammo = __2;

            if (!Utility.IsHostAndCGGOActive()) return true;

            return publicBuyPhase || weaponId != WeaponsId.SNOWBALL_ID;

        }

        // Update player's memory with newly acquired weapons via commands
        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPostfix]
        public static void OnGameServerForceGiveWeaponPost(ulong __0, int __1, int __2)
        {
            ulong steamId = __0;
            int weaponId = __1;

            if (!Utility.IsHostAndCGGOActive()) return;

            var player = CGGOPlayer.GetCGGOPlayer(steamId);
            if (player == null) return; // Exit early if the player isn't part of CGGO

            // Use helper method to add the weapon to the player's memory
            UpdatePlayerWeaponMemory(player, weaponId, true);
        }

        // Update player's memory to remove dropped weapons via commands
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.DropItem))]
        [HarmonyPostfix]
        public static void OnServerSendDropItemPost(ulong __0, int __1, int __2, int __3)
        {
            ulong steamId = __0;
            int weaponId = __1;

            if (!Utility.IsHostAndCGGOActive()) return;

            var player = CGGOPlayer.GetCGGOPlayer(steamId);
            if (player == null) return; // Exit early if the player isn't part of CGGO

            // Use helper method to remove the weapon from the player's memory
            UpdatePlayerWeaponMemory(player, weaponId, false);
        }

        // Manage advanced interactions such as picking up the bomb or removing duplicated items
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.Interact))]
        [HarmonyPrefix]
        public static bool OnServerSendInteractPre(ulong __0, int __1)
        {
            ulong steamId = __0;
            int sharedObjectId = __1;
            string sharedObjectName = FindSharedObjectName(sharedObjectId);

            if (!Utility.IsHostAndCGGOActive()) return true;

            // Check if the item being interacted with is the bomb, if no, return early
            if (sharedObjectName != "Bomb(Clone)" && sharedObjectName != "Grenade(Clone)") return true;

            var player = CGGOPlayer.GetCGGOPlayer(steamId);
            if (player == null) return true; // If the player isn't participating in CGGO, exit early

            Utility.ForceMessage($"{sharedObjectId}, {sharedObjectName}, {player.Team}");

            // Allow only attackers to interact with the bomb
            if (player.Team == (int)TeamsId.Attackers)
            {
                if (sharedObjectName == "Bomb(Clone)")
                {
                    originalBombId = sharedObjectId;
                    // Remove duplicated Katana that drops when picking up the bomb
                    if (player.Katana)
                    {
                        itemToDelete.Add(player.KatanaId, DateTime.Now);
                    }
                    else // Remove duplicated Knife if the player doesn't have a Katana
                    {
                        itemToDelete.Add(player.KnifeId, DateTime.Now);
                    }
                }
                return true; // Allow attackers to pick up the bomb
            }
            else return false; // Block defenders from picking up the bomb
      
        }

        // Update player's memory to reflect newly acquired weapons by PickUp, Convert the bomb to a grenade upon interaction
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.Interact))]
        [HarmonyPostfix]
        public static void OnServerSendInteractPost(ulong __0, int __1)
        {
            ulong steamId = __0;
            int sharedObjectId = __1;

            if (!Utility.IsHostAndCGGOActive()) return;

            var player = CGGOPlayer.GetCGGOPlayer(steamId);
            if (player == null) return; // If the player isn't participating in CGGO, exit early

            // If a attackers picks up the bomb, convert it to a grenade
            if (player.Team == (int)TeamsId.Attackers && originalBombId == sharedObjectId)
            {
                // Force the removal of the bomb from the player's inventory
                GameServer.ForceRemoveItem(steamId, sharedObjectId);

                // Reassign the bomb's ID to a new weapon ID
                weaponId++;
                publicBombId = weaponId;

                // Give the player a grenade, so it goes in 4th slot, instead of the bomb
                GameServer.ForceGiveWeapon(steamId, WeaponsId.GRENADE_ID, publicBombId);

                // Reassign the player's weapon based on whether they previously had a Katana or Knife
                if (player.Katana)
                {
                    weaponId++;
                    GameServer.ForceGiveWeapon(steamId, WeaponsId.KATANA_ID, weaponId);
                    player.KatanaId = weaponId; // Update the player's Katana ID
                    publicKatanaList.Add(weaponId); // Add the new Katana to the public list
                }
                else
                {
                    weaponId++;
                    GameServer.ForceGiveWeapon(steamId, WeaponsId.KNIFE_ID, weaponId); // Assign a knife if no Katana
                }

                return;
            }
            else
            {
                // Update the player's weapon memory based on the shared object ID
                UpdatePlayerWeaponMemoryOnPickUp(sharedObjectId, player);
                return;
            }
        }

        // Allow only attackers to drop the bomb, prevent defenders from dropping it
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.DropItem))]
        [HarmonyPrefix]
        public static bool OnDropItemPre(ulong __0, int __1, int __2, int __3)
        {
            ulong steamId = __0;
            int weaponId = __1, sharedObjectId = __2, ammo = __3;

            if (!Utility.IsHostAndCGGOActive()) return true;

            // Allow all other items to be dropped
            if (weaponId != WeaponsId.BOMB_ID && weaponId != WeaponsId.GRENADE_ID) return true;

            var player = CGGOPlayer.GetCGGOPlayer(steamId);
            if (player == null) return true; // Exit early if the player is not in CGGO

            // Allow attackers to drop the bomb, but block defenders
            if (player.Team == (int)TeamsId.Attackers) return true; // Allow bomb drop for attackers
            else return false; // Block bomb drop for defenders
        }

        // Don't let the game timer UI update; instead, display the player's shield value
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendGameModeTimer), new[] { typeof(ulong), typeof(float), typeof(int) })]
        [HarmonyPrefix]
        public static bool OnServerSendTimerPre(ulong __0, ref float __1, int __2)
        {
            if (!Utility.IsHostAndCGGOActive()) return true;

            var player = CGGOPlayer.GetCGGOPlayer(__0);
            if (player != null) return true; // If the player isn't participating in CGGO, exit early

            if (__1 == player.Shield) return true;// If the shield value matches the current timer (__1), allow the timer UI update
            else return false; // Otherwise, block the timer UI update to display the shield value instead of the round timer

        }


        // Custom Weapon Damage system
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDamage))]
        [HarmonyPrefix]
        internal static void OnPlayerDamagePre(ref ulong __0, ref ulong __1, ref int __2, ref Vector3 __3, ref int __4)
        {
            // __0 = damagerId (attacker), __1 = damagedId (victim), __2 = damageAmount, __3 = bulletDirection, __4 = weaponId

            // Validate execution if the weaponId is valid, (prevent infinite loop)
            if (!Utility.IsHostAndCGGOActive() || __4 == WeaponsId.NON_WEAPON_ID) return;

            // Retrieve the players (attacker and victim)
            CGGOPlayer damager = CGGOPlayer.GetCGGOPlayer(__0);
            CGGOPlayer damaged = CGGOPlayer.GetCGGOPlayer(__1);

            // If either player cannot be found, exit the method early
            if (damager == null || damaged == null) return;

            // Update the list of hit players, this helps to track the last person who hit a player
            UpdateHitPlayers(__0, __1);

            // Get PlayerManager objects to retrieve player positions and other important data
            PlayerManager damagerPlayer = GameData.GetPlayer(__0.ToString());
            PlayerManager damagedPlayer = GameData.GetPlayer(__1.ToString());

            // Exit if player managers cannot be retrieved
            if (damagerPlayer == null || damagedPlayer == null) return;

            // Retrieve attacker and victim positions and rotations
            Vector3 damagerPos = damagerPlayer.gameObject.transform.position;
            Vector3 damagedPos = damagedPlayer.gameObject.transform.position;
            Quaternion damagerRot = damagerPlayer.GetRotation();

            // Calculate the distance between the attacker and the victim
            float shootDistance = Vector3.Distance(damagerPos, damagedPos);

            // Check for friendly fire. If the attacker and victim are on the same team, no damage is dealt.
            if (IsFriendlyFire())
            {
                __2 = 0; // No damage
                __4 = WeaponsId.SNOWBALL_ID; // Weapon is set as a snowball (friendly fire)
            }
            else
            {
                // Weapon-based damage calculations based on weapon ID
                int damage;
                string type;
                float impactHeight;

                switch (__4) // Weapon ID
                {
                    case WeaponsId.RIFLE_ID:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateRifleDamage(shootDistance, type); // Rifle-specific damage calculation
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                    case (int)WeaponsId.PISTOL_ID:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculatePistolDamage(shootDistance, type); // Pistol-specific damage calculation
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                    case (int)WeaponsId.REVOLVER_ID:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateRevolverDamage(shootDistance, type); // Revolver-specific damage calculation
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                    case (int)WeaponsId.SHOTGUN_ID:
                        impactHeight = CalculateImpactHeight(damagerPos, damagerRot, damagedPos);
                        type = ImpactHeightToType(impactHeight);
                        damage = CalculateShotgunDamage(shootDistance, type); // Shotgun-specific damage calculation
                        AttributeDamage(damage);
                        ManageAssists(damage);
                        __2 = ManageDamage(damage);
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                    case (int)WeaponsId.KATANA_ID:
                        AttributeDamage(WeaponsConstants.KATANA_BASE_DAMAGE);
                        ManageAssists(WeaponsConstants.KATANA_BASE_DAMAGE);
                        __2 = ManageDamage(WeaponsConstants.KATANA_BASE_DAMAGE); // Apply calculated damage
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                    case (int)WeaponsId.KNIFE_ID:
                        AttributeDamage(WeaponsConstants.KNIFE_BASE_DAMAGE);
                        ManageAssists(WeaponsConstants.KNIFE_BASE_DAMAGE);
                        __2 = ManageDamage(WeaponsConstants.KNIFE_BASE_DAMAGE);
                        __4 = (int)WeaponsId.NON_WEAPON_ID; // marking the damage as coming from a non-weapon source
                        __0 = __1; // Set the damaged player as the damager
                        break;
                }
            }

            // Update the list of players involved in a hit, storing the attacker ID and timestamp
            void UpdateHitPlayers(ulong damagerId, ulong damagedId)
            {
                if (hitPlayers.ContainsKey(damagedId))
                {
                    hitPlayers[damagedId] = new KeyValuePair<ulong, DateTime>(damagerId, DateTime.Now);
                }
                else
                {
                    hitPlayers.Add(damagedId, new KeyValuePair<ulong, DateTime>(damagerId, DateTime.Now));
                }
            }

            // Check if the damage should be classified as friendly fire (same team but different players)
            bool IsFriendlyFire()
            {
                return damager.Team == damaged.Team && damager.SteamId != damaged.SteamId;
            }

            // Assign damage values to the damaged and damager players
            void AttributeDamage(int damage)
            {
                damaged.DamageReceived += damage;
                damager.DamageDealt += damage;
            }

            // Manage assists: if the attacker contributed to damage, mark them as an assister
            void ManageAssists(int damage)
            {
                if (damage == 0) return;
                if (!damaged.Assisters.Contains(damager))
                    damaged.Assisters.Add(damager);
            }

            // Example method for calculating pistol damage based on distance and impact type
            int CalculatePistolDamage(float distance, string type)
            {
                if (distance > WeaponsDistanceConstants.MAX_PISTOL_DISTANCE) return 0;

                return type switch
                {
                    "legs" => distance > WeaponsDistanceConstants.SHORT_PISTOL_DISTANCE ? WeaponsConstants.PISTOL_LEGS_LONG_DAMAGE : WeaponsConstants.PISTOL_LEGS_SHORT_DAMAGE,
                    "body" => distance > WeaponsDistanceConstants.SHORT_PISTOL_DISTANCE ? WeaponsConstants.PISTOL_BODY_LONG_DAMAGE : WeaponsConstants.PISTOL_BODY_SHORT_DAMAGE,
                    "head" => distance > WeaponsDistanceConstants.SHORT_PISTOL_DISTANCE ? WeaponsConstants.PISTOL_HEAD_LONG_DAMAGE : WeaponsConstants.PISTOL_HEAD_SHORT_DAMAGE,
                    _ => 0,
                };
            }

            // Example method for calculating shotgun damage
            int CalculateShotgunDamage(float distance, string type)
            {
                if (distance > WeaponsDistanceConstants.MAX_SHOTGUN_DISTANCE) return 0;

                return type switch
                {
                    "legs" => distance > WeaponsDistanceConstants.MEDIUM_SHOTGUN_DISTANCE
                        ? (distance > WeaponsDistanceConstants.SHORT_SHOTGUN_DISTANCE
                            ? WeaponsConstants.SHOTGUN_LEGS_MEDIUM_DAMAGE
                            : WeaponsConstants.SHOTGUN_LEGS_SHORT_DAMAGE)
                        : WeaponsConstants.SHOTGUN_LEGS_LONG_DAMAGE,
                    "body" => distance > WeaponsDistanceConstants.MEDIUM_SHOTGUN_DISTANCE
                        ? (distance > WeaponsDistanceConstants.SHORT_SHOTGUN_DISTANCE
                            ? WeaponsConstants.SHOTGUN_BODY_MEDIUM_DAMAGE
                            : WeaponsConstants.SHOTGUN_BODY_SHORT_DAMAGE)
                        : WeaponsConstants.SHOTGUN_BODY_LONG_DAMAGE,
                    "head" => distance > WeaponsDistanceConstants.MEDIUM_SHOTGUN_DISTANCE
                        ? (distance > WeaponsDistanceConstants.SHORT_SHOTGUN_DISTANCE
                            ? WeaponsConstants.SHOTGUN_HEAD_MEDIUM_DAMAGE
                            : WeaponsConstants.SHOTGUN_HEAD_SHORT_DAMAGE)
                        : WeaponsConstants.SHOTGUN_HEAD_LONG_DAMAGE,
                    _ => 0,
                };
            }

            // Example method for calculating revolver damage
            int CalculateRevolverDamage(float distance, string type)
            {
                return type switch
                {
                    "legs" => (int)(distance * WeaponsConstants.REVOLVER_LEGS_MULTIPLIER_DAMAGE),
                    "body" => (int)(distance * WeaponsConstants.REVOLVER_BODY_MULTIPLIER_DAMAGE),
                    "head" => (int)(distance * WeaponsConstants.REVOLVER_HEAD_MULTIPLIER_DAMAGE),
                    _ => 0,
                };
            }

            // Example method for calculating rifle damage
            int CalculateRifleDamage(float distance, string type)
            {
                if (distance > WeaponsDistanceConstants.MAX_RIFLE_DISTANCE) return 0;

                return type switch
                {
                    "legs" => WeaponsConstants.RIFLE_LEGS_DAMAGE,
                    "body" => WeaponsConstants.RIFLE_BODY_DAMAGE,
                    "head" => WeaponsConstants.RIFLE_HEAD_DAMAGE,
                    _ => 0,
                };
            }

            // Calculate the height of the impact point, relative to the victim's body
            float CalculateImpactHeight(Vector3 shooterPosition, Quaternion shooterRotation, Vector3 targetPosition)
            {
                Vector3 targetCenter = targetPosition + new Vector3(0, PlayerConstants.PLAYER_HEIGHT / 2, 0);
                Vector3 shotDirection = shooterRotation * Vector3.forward;

                float distance = Vector3.Distance(shooterPosition, targetCenter);
                Vector3 impactPosition = shooterPosition + shotDirection * distance;

                float impactHeightRelativeToFeet = impactPosition.y - targetPosition.y;
                return impactHeightRelativeToFeet + PlayerConstants.PLAYER_HEIGHT; // Adjust height relative to body parts
            }

            // Convert the calculated impact height to a body part type
            string ImpactHeightToType(float impactHeight)
            {
                if (impactHeight > WeaponsDistanceConstants.HEAD_THRESHOLD)
                {
                    damager.Headshot++;
                    return "head";
                }
                if (impactHeight > WeaponsDistanceConstants.BODY_THRESHOLD)
                {
                    damager.Bodyshot++;
                    return "body";
                }
                else
                {
                    damager.Legsshot++;
                    return "legs";
                }
            }

            // Manage the application of damage to both health and shield, logging the result
            int ManageDamage(int damage)
            {
                if (damaged.Shield > 0)
                {
                    // Calculate damage distribution between shield and health
                    int shieldDamage = (int)(damage * ShieldConstants.SHIELD_DAMAGE_RATIO);
                    int healthDamage = damage - shieldDamage;

                    // If the shield is not enough to absorb all the shield damage, convert the excess to health damage
                    if (damaged.Shield < shieldDamage)
                    {
                        healthDamage += (shieldDamage - damaged.Shield);
                        shieldDamage = damaged.Shield;
                    }

                    // Apply damage to the shield and ensure it doesn't go below zero
                    damaged.Shield -= shieldDamage;
                    if (damaged.Shield < 0) damaged.Shield = 0;

                    // Apply the remaining health damage
                    damaged.DamageTaken += healthDamage;

                    // Check if the player is dead (health damage reaches or exceeds the max health)
                    if (damaged.DamageTaken >= PlayerConstants.PLAYER_MAX_HEALTH) damaged.Killer = damager.SteamId;

                    return healthDamage;
                }
                else
                {
                    // Direct health damage when no shield remains
                    damaged.DamageTaken += damage;

                    // Check if the player is dead
                    if (damaged.DamageTaken >= PlayerConstants.PLAYER_MAX_HEALTH) damaged.Killer = damager.SteamId;

                    return damage;
                }
            }

        }
    }

    public class WeaponsFunctions
    {
        // Find the name of a SharedObject
        public static string FindSharedObjectName(int sharedObjectId)
        {
            return SharedObjectManager.Instance.GetSharedObject(sharedObjectId).gameObject.name;
        }

        // Update the player's memory for picked-up weapons
        public static void UpdatePlayerWeaponMemoryOnPickUp(int sharedObjectId, CGGOPlayer player)
        {
            // Check the shared object ID and update the player's weapon memory accordingly
            if (publicPistolList.Contains(sharedObjectId))
            {
                player.Pistol = true;
            }
            if (publicShotgunList.Contains(sharedObjectId))
            {
                player.Shotgun = true;
            }
            if (publicRifleList.Contains(sharedObjectId))
            {
                player.Rifle = true;
            }
            if (publicKatanaList.Contains(sharedObjectId))
            {
                player.Katana = true;
            }
            if (publicRevolverList.Contains(sharedObjectId))
            {
                player.Revolver = true;
            }
        }
        // Helper method to update the player's weapon memory (add or remove)
        public static void UpdatePlayerWeaponMemory(CGGOPlayer player, int weaponId, bool status)
        {
            switch (weaponId)
            {
                case WeaponsId.RIFLE_ID:
                    player.Rifle = status;
                    break;
                case WeaponsId.PISTOL_ID:
                    player.Pistol = status;
                    break;
                case WeaponsId.REVOLVER_ID:
                    player.Revolver = status;
                    break;
                case WeaponsId.SHOTGUN_ID:
                    player.Shotgun = status;
                    break;
                case WeaponsId.KATANA_ID:
                    player.Katana = status;
                    break;
                default:
                    break;
            }
        }

        // Handle the purchase of a weapon by a player
        public static void HandleWeaponPurchase(ulong userId, CGGOPlayer player, int cost, int weaponId, List<int> weaponList)
        {
            // Validate conditions: during buy phase, and player must have enough balance
            if (!Utility.IsHostAndCGGOActive() || !publicBuyPhase || player.Balance < cost) return;

            // Deduct the cost from the player's balance and update their money spent
            player.MoneyUsed += cost;
            player.Balance -= cost;

            // Generate a new unique ID for the weapon)
            Variables.weaponId++;

            // Add the newly purchased weapon ID to the player's weapon list
            weaponList.Add(Variables.weaponId);

            // Special case: if the weapon is a Katana, store its ID on the player
            if (weaponId == (int)WeaponsId.KATANA_ID) player.KatanaId = Variables.weaponId;

            // Attempt to give the weapon to the player, catching any exceptions to log errors
            try
            {
                GameServer.ForceGiveWeapon(userId, weaponId, Variables.weaponId);
            }
            catch (Exception ex)
            {
                // Log any errors encountered while trying to give the weapon to the player
                Utility.Log(logFilePath, $"Error giving weapon {weaponId} to player {userId}: {ex}");
            }
        }

        // Handle the purchase of a shield by a player
        public static void HandleShieldPurchase(CGGOPlayer player, int cost, int shieldValue)
        {
            // Validate conditions: during buy phase, player must not already have max shield, and must have enough balance
            if (!Utility.IsHostAndCGGOActive() || !publicBuyPhase || player.Shield >= shieldValue || player.Balance < cost) return;

            // Deduct the cost from the player's balance and update their shield value
            player.Balance -= cost;
            player.Shield = shieldValue;
        }
    }
}