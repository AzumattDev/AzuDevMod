using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using AzuDevMod.Patches;
using AzuDevMod.Util;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AzuDevMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class AzuDevModPlugin : BaseUnityPlugin

    {
        internal const string ModName = "AzuDevMod";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource AzuDevModLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);


        public static ConfigEntry<Toggle> LogDuplicateGameObjectAdditions = null!;
        public static ConfigEntry<Toggle> LogDestroyedZNetViews = null!;
        public static ConfigEntry<Toggle> LogUnregisteredZNetViews = null!;
        public static ConfigEntry<Toggle> LogUnpatchAll = null!;
        public static ConfigEntry<Toggle> LogAssetBundleIssues = null!;


        public enum Toggle
        {
            Off,
            On
        }

        private void Awake()
        {
            LogDestroyedZNetViews = Config.Bind("1 - General User", "Log Destroyed ZNetViews", Toggle.On,
                "Logs invalid ZNetView destructions to the console. Useful for finding mods that has ZNetView's destroyed without being destroyed through the ZNetScene.");

            LogUnregisteredZNetViews = Config.Bind("1 - General User", "Log Unregistered ZNetViews", Toggle.On,
                "Logs unregistered ZNetViews to the console. Useful for finding mods that has ZNetView's with prefabs not registered in the ZNetScene.");

            LogUnpatchAll = Config.Bind("1 - General User", "Log Unpatch All", Toggle.On,
                "Logs mods that call UnpatchAll to the console. Useful for finding mods that are unpatching all patches at game close causing issues with other mods.");

            LogAssetBundleIssues = Config.Bind("1 - General User", "Log Asset Bundle Issues", Toggle.On,
                "Logs asset bundle issues to the console. Useful for identifying mods that load asset bundles incorrectly or attempt to retrieve prefabs from a bundle that doesn't contain them...etc.");

            LogDuplicateGameObjectAdditions = Config.Bind("1 - Mod Developer", "Log Duplicate GameObject Additions", Toggle.Off,
                "Logs duplicate GameObject additions to the console. Mainly intended for mod developer debugging. Note that this might not work " +
                "if your mod is obfuscated. Use this on a clean version of your mod. " +
                "Useful for finding duplicate key issues for ZNetScene, such as attempting to add duplicate GameObjects to ZNetScene's prefab list.");

            _harmony.PatchAll();
            SetupWatcher();
        }


        private void Start()
        {
           // ModDetect.DetectModsPatchingTerminal();
            AssetLoadTracker.MapPrefabsToBundles();
            AssetLoadTracker.MapBundlesToAssemblies();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                AzuDevModLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
                Config.Save();
            }
            catch
            {
                AzuDevModLogger.LogError($"There was an issue loading your {ConfigFileName}");
                AzuDevModLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }
}