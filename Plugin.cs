using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using AzuDevMod.Util;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AzuDevMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class AzuDevModPlugin : BaseUnityPlugin

    {
        internal const string ModName = "AzuDevMod";
        internal const string ModVersion = "1.0.1";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource AzuDevModLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private void Awake()
        {
            _harmony.PatchAll();
        }


        private void Start()
        {
            AssetLoadTracker.MapPrefabsToBundles();
            AssetLoadTracker.MapBundlesToAssemblies();
        }
    }
}