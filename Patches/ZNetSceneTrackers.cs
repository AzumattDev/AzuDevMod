using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AzuDevMod.Util;
using HarmonyLib;

namespace AzuDevMod.Patches;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Update))]
public class WatchForDestroyedZNetViewsInScene
{
    internal static readonly ConditionalWeakTable<ZNetView, string> DestroyedZNetViews = new();

    private static void Postfix(ZNetScene __instance)
    {
        List<ZDO> zdoList = new List<ZDO>();
        foreach (KeyValuePair<ZDO, ZNetView> instance in __instance.m_instances)
        {
            ZNetView key = instance.Value;
            if (!key)
            {
                zdoList.Add(instance.Key);
                string str;

                if (!DestroyedZNetViews.TryGetValue(key, out str)) continue;
                // Extract the prefab name from the stack trace
                var startIndex = str.IndexOf("ZNetScene: ", StringComparison.Ordinal) + "ZNetScene: ".Length;
                var endIndex = str.IndexOf("(Clone)", StringComparison.Ordinal);
                if (startIndex < endIndex && startIndex != -1)
                {
                    var prefabName = str.Substring(startIndex, endIndex - startIndex);

                    var assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabName);
                    var bundle = AssetLoadTracker.GetBundleForPrefab(prefabName);
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Potential for ZNetScene.RemoveObjects error spam. " +
                                                             $"ZNetView destroyed without being destroyed through the ZNetScene: " +
                                                             $"{prefabName}. Bundle: {bundle}. Assembly: {assembly?.GetName().Name}");
                }
            }
        }

        foreach (ZDO key in zdoList)
            __instance.m_instances.Remove(key);
    }
}

[HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
public class TrackUnregisteredZNetViews
{
    private static void Postfix(ZNetView __instance)
    {
        int? prefab = __instance.GetZDO()?.GetPrefab();
        int num;
        if (prefab.HasValue)
        {
            int valueOrDefault = prefab.GetValueOrDefault();
            if (valueOrDefault != 0)
            {
                num = ZNetScene.instance.GetPrefab(valueOrDefault) == null ? 1 : 0;
                goto endLabel;
            }
        }

        num = 0;
        endLabel:
        if (num == 0)
            return;

        var prefabName = __instance.GetPrefabName();
        var assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabName);
        var bundle = AssetLoadTracker.GetBundleForPrefab(prefabName);

        if (assembly != null && !string.IsNullOrEmpty(bundle))
        {
            AzuDevModPlugin.AzuDevModLogger.LogWarning($"ZNetView for '{prefabName}' has not been registered in ZNetScene. " +
                                                       $"This can cause the ZNetScene.RemoveObjects error spam. The prefab is in the bundle " +
                                                       $"'{bundle}' which is from the assembly '{assembly.GetName().Name}'. " +
                                                       $"Full Stack Trace :{Environment.NewLine}{Environment.NewLine}{Environment.StackTrace}");
        }
        else if (!string.IsNullOrEmpty(bundle))
        {
            AzuDevModPlugin.AzuDevModLogger.LogWarning($"ZNetView for '{prefabName}' has not been registered in ZNetScene. " +
                                                       $"This can cause the ZNetScene.RemoveObjects error spam. The prefab is in the bundle " +
                                                       $"'{bundle}'. " +
                                                       $"Full Stack Trace :{Environment.NewLine}{Environment.NewLine}{Environment.StackTrace}");
        }
        else
        {
            AzuDevModPlugin.AzuDevModLogger.LogWarning($"ZNetView for '{prefabName}' has not been registered in ZNetScene. " +
                                                       $"Couldn't find full information for the prefab's mod. " +
                                                       $"Full Stack Trace :{Environment.NewLine}{Environment.NewLine}{Environment.StackTrace}");
        }
    }
}

[HarmonyPatch(typeof(ZNetView), nameof(ZNetView.OnDestroy))]
public class TrackZNetViewDestruction
{
    private static void Postfix(ZNetView __instance) => WatchForDestroyedZNetViewsInScene.DestroyedZNetViews.Add(__instance, $"{__instance.name}\n{Environment.StackTrace}");
}