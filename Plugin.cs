using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        internal const string ModVersion = "1.0.8";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"zzzzzzzzzz{Author}.{ModName}";
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
            LogDestroyedZNetViews = Config.Bind("1 - General User", "Log Destroyed ZNetViews", Toggle.On, "Logs invalid ZNetView destructions to the console. Useful for finding mods that has ZNetView's destroyed without being destroyed through the ZNetScene.");

            LogUnregisteredZNetViews = Config.Bind("1 - General User", "Log Unregistered ZNetViews", Toggle.On, "Logs unregistered ZNetViews to the console. Useful for finding mods that has ZNetView's with prefabs not registered in the ZNetScene.");

            LogUnpatchAll = Config.Bind("1 - General User", "Log Unpatch All", Toggle.On, "Logs mods that call UnpatchAll to the console. Useful for finding mods that are unpatching all patches at game close causing issues with other mods.");

            LogAssetBundleIssues = Config.Bind("1 - General User", "Log Asset Bundle Issues", Toggle.On, "Logs asset bundle issues to the console. Useful for identifying mods that load asset bundles incorrectly or attempt to retrieve prefabs from a bundle that doesn't contain them...etc.");

            LogDuplicateGameObjectAdditions = Config.Bind("1 - Mod Developer", "Log Duplicate GameObject Additions", Toggle.Off,
                "Logs duplicate GameObject additions to the console. Mainly intended for mod developer debugging. Note that this might not work " +
                "if your mod is obfuscated. Use this on a clean version of your mod. " +
                "Useful for finding duplicate key issues for ZNetScene, such as attempting to add duplicate GameObjects to ZNetScene's prefab list.");

            CheckForInvalidPatches();
            _harmony.PatchAll();
            SetupWatcher();
        }


        private void Start()
        {
            // ModDetect.DetectModsPatchingTerminal();
            AssetLoadTracker.MapPrefabsToBundles();
            AssetLoadTracker.MapBundlesToAssemblies();
            var fieldsToCheck = new List<Tuple<string, string>>();
            if (Version.CurrentVersion >= new GameVersion(0, 218, 16))
            {
                fieldsToCheck.Add(new Tuple<string, string>("Player", "m_firstSpawn"));
            }

            if (Version.CurrentVersion >= new GameVersion(0, 218, 15))
            {
                fieldsToCheck.Add(new Tuple<string, string>("UnityEngine.UI.InputField.Terminal", "m_input"));
            }
            
            fieldsToCheck.Add(new Tuple<string, string>("Hud", "m_pieceBarPosX"));
            fieldsToCheck.Add(new Tuple<string, string>("Pickable", "m_respawnTimeMinutes"));

            MissingFieldDetector.Init(fieldsToCheck);
        }

        private void CheckForInvalidPatches()
        {
            try
            {
                var allPatchedMethods = Harmony.GetAllPatchedMethods().ToList();
                AzuDevModLogger.LogInfo($"Total patched methods found: {allPatchedMethods.Count}");

                foreach (var method in allPatchedMethods)
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    if (patchInfo == null)
                    {
                        AzuDevModLogger.LogWarning($"No patch info found for method: {method.DeclaringType}.{method.Name}");
                        continue;
                    }

                    CheckPatches(patchInfo.Prefixes, method, "prefix");
                    CheckPatches(patchInfo.Postfixes, method, "postfix");
                    CheckPatches(patchInfo.Transpilers, method, "transpiler");
                    CheckPatches(patchInfo.Finalizers, method, "finalizer");
                }
            }
            catch (Exception ex)
            {
                AzuDevModLogger.LogError($"Error while checking for invalid patches: {ex.Message}");
            }
        }

        private void CheckPatches(IEnumerable<Patch> patches, MethodBase originalMethod, string patchType)
        {
            if (patches == null) return;

            foreach (var patch in patches)
            {
                AzuDevModLogger.LogInfo($"Checking {patchType} patch from {patch.owner} on {originalMethod.DeclaringType}.{originalMethod.Name}");

                try
                {
                    if (!MethodExists(patch.PatchMethod))
                    {
                        AzuDevModLogger.LogWarning($"Harmony instance {patch.owner} has a {patchType} patch on {originalMethod.DeclaringType}.{originalMethod.Name} that no longer exists.");
                    }
                }
                catch (Exception ex)
                {
                    AzuDevModLogger.LogError($"Error while checking {patchType} patch from {patch.owner} on {originalMethod.DeclaringType}.{originalMethod.Name}: {ex.Message}");
                }
            }
        }

        private bool MethodExists(MethodInfo method)
        {
            try
            {
                if (method == null)
                {
                    AzuDevModLogger.LogWarning("Patch method is null");
                    return false;
                }

                var declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    AzuDevModLogger.LogWarning($"Declaring type for method {method.Name} is null");
                    return false;
                }

                var existingMethod = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == method.Name && ParametersMatch(m.GetParameters(), method.GetParameters()));

                if (existingMethod == null)
                {
                    AzuDevModLogger.LogWarning($"Method {method.Name} with specified parameters not found in type {declaringType.FullName}");
                }

                return existingMethod != null;
            }
            catch (Exception ex)
            {
                AzuDevModLogger.LogError($"Error while checking if method exists: {ex.Message}");
                return false;
            }
        }

        private bool ParametersMatch(ParameterInfo[] parameters1, ParameterInfo[] parameters2)
        {
            if (parameters1.Length != parameters2.Length) return false;

            for (int i = 0; i < parameters1.Length; i++)
            {
                if (parameters1[i].ParameterType != parameters2[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDestroy()
        {
            /*AzuDevModLogger.LogWarning("Detected plugins calling UnpatchSelf, this will cause longer shutdown times:");
            foreach (string? plugin in HarmonyUnpatchSelfPatch.DetectedPlugins)
            {
                AzuDevModLogger.LogWarning(plugin);
            }*/

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