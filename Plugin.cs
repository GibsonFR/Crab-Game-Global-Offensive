//Using (ici on importe des bibliothèques utiles)
global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using SteamworksNative;
global using System;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using UnhollowerRuntimeLib;
global using UnityEngine;
global using UnityEngine.UI;
global using Il2CppSystem.Text; 
global using static GibsonCrabGameGlobalOffensive.Variables;
global using static GibsonCrabGameGlobalOffensive.CGGOManager;

namespace GibsonCrabGameGlobalOffensive
{
    [BepInPlugin("GibsonCrabGameGlobalOffensive", "GibsonCrabGameGlobalOffensive", "2.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Main>();
            ClassInjector.RegisterTypeInIl2Cpp<FlungDetector>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();
            ClassInjector.RegisterTypeInIl2Cpp<ItemsRemover>();
            ClassInjector.RegisterTypeInIl2Cpp<CGGOManager>();
            

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony harmony = new("gibson.cggo");
            harmony.PatchAll(typeof(MainPatchs));
            harmony.PatchAll(typeof(CommandPatchs));
            harmony.PatchAll(typeof(AntiCheatPatchs));
            harmony.PatchAll(typeof(CGGOPatchs));

            Utility.CreateFolder(mainFolderPath);
            Utility.CreateFolder(playersDataFolderPath);

            Utility.CreateFile(configFilePath);
            Utility.CreateFile(playersBannedFilePath);
            Utility.CreateFile(playersListFilePath);
            Utility.CreateFile(playersReportFilePath);
            Utility.CreateFile(wordsFilterFilePath);
            Utility.CreateFile(permsFilePath);
            Utility.CreateFile(logFilePath);

            Utility.ResetFile(logFilePath);
            Utility.SetConfigFile(configFilePath);

            MenuFunctions.CheckMenuFileExists();
            MenuFunctions.LoadMenuLayout();
            MenuFunctions.RegisterDefaultCallbacks();

            for (int i = 0; i < 10; i++)
            {
                Utility.processNewMessage(messagesList, "GibsonChatSystem Loaded!");
            }

            Log.LogInfo("Mod created by Gibson, discord: gib_son, github: GibsonFR");
        }


        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject pluginObj = new(); 
            Text text = pluginObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;
            text.raycastTarget = false;
            Main basics = pluginObj.AddComponent<Main>();
            basics.text = text;
            pluginObj.transform.SetParent(__instance.transform);
            pluginObj.transform.localPosition = new Vector3(pluginObj.transform.localPosition.x, -pluginObj.transform.localPosition.y, pluginObj.transform.localPosition.z);
            RectTransform rt = pluginObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(1920, 1080);

            _ = pluginObj.AddComponent<FlungDetector>();
            _ = pluginObj.AddComponent<MenuManager>();

            if (SteamManager.Instance.IsLobbyOwner())
            {
                _ = pluginObj.AddComponent<ItemsRemover>();
                _ = pluginObj.AddComponent<CGGOManager>();
            }
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(MethodBase __originalMethod)
        {
            return false;
        }
    }
}