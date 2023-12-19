using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AzuDevMod.Util;
using HarmonyLib;
using UnityEngine;

namespace AzuDevMod.Patches;

[HarmonyPatch(typeof(List<GameObject>), nameof(List<GameObject>.Add))]
public class CheckDuplicatePatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(List<GameObject> __instance, GameObject item)
    {
        if (AzuDevModPlugin.LogDuplicateGameObjectAdditions.Value == AzuDevModPlugin.Toggle.Off) return;
        if (DungeonDB.instance == null || ZoneSystemCheck.HasInit) return;
        if (item == null || !__instance.Contains(item)) return;

        string name = item.name;
        string nameLower = item.name.ToLower();

        Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(nameLower);
        string bundle = AssetLoadTracker.GetBundleForPrefab(nameLower);

        StringBuilder sb = new StringBuilder($"Attempting to add duplicate GameObject to a list of GameObjects: {name}. ");

        if (assembly != null && !string.IsNullOrEmpty(bundle))
        {
            sb.Append($"The prefab is in the bundle '{bundle}' and the assembly '{assembly.GetName().Name}'. ");
        }
        else if (!string.IsNullOrEmpty(bundle))
        {
            sb.Append($"The prefab is in the bundle '{bundle}'. ");
        }
        else if (assembly != null)
        {
            sb.Append($"The prefab is in the assembly '{assembly.GetName().Name}'. ");
        }
        else
        {
            sb.Append("Couldn't find full information for the prefab's mod. ");
        }

        sb.AppendLine($"Full Stack Trace :{Environment.NewLine}{Environment.NewLine}{Environment.StackTrace}{Environment.NewLine}");

        AzuDevModPlugin.AzuDevModLogger.LogError(sb.ToString());
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
static class ZoneSystemCheck
{
    internal static bool HasInit = false;

    static void Postfix(ZoneSystem __instance)
    {
        HasInit = true; // Only needed so the above code doesn't run after the ZoneSystem has initialized
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Update))]
public class WatchForDestroyedZNetViewsInScene
{
    internal static readonly ConditionalWeakTable<ZNetView, string> DestroyedZNetViews = new();

    private static void Postfix(ZNetScene __instance)
    {
        if (AzuDevModPlugin.LogDestroyedZNetViews.Value == AzuDevModPlugin.Toggle.Off) return;
        List<ZDO> zdoList = new List<ZDO>();
        foreach (KeyValuePair<ZDO, ZNetView> instance in __instance.m_instances)
        {
            ZNetView key = instance.Value;
            if (!key)
            {
                zdoList.Add(instance.Key);
                string? prefabName;
                if (!DestroyedZNetViews.TryGetValue(key, out string str)) continue;
                // Extract the prefab name from the stack trace
                int startIndex = str.IndexOf("ZNetScene: ", StringComparison.Ordinal) + "ZNetScene: ".Length;
                int endIndex = str.IndexOf("(Clone)", StringComparison.Ordinal);
                if (startIndex < endIndex && startIndex != -1)
                {
                    prefabName = str.Substring(startIndex, endIndex - startIndex);

                    Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabName);
                    string bundle = AssetLoadTracker.GetBundleForPrefab(prefabName);
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Potential for ZNetScene.RemoveObjects error spam. " +
                                                             $"ZNetView destroyed without being destroyed through the ZNetScene: " +
                                                             $"{prefabName} ({key.gameObject.name}). Bundle: {bundle}. Assembly: {assembly?.GetName().Name}");
                }
                else if (!string.IsNullOrWhiteSpace(key.GetPrefabName()))
                {
                    try
                    {
                        prefabName = key.GetPrefabName();
                    }
                    catch (Exception)
                    {
                        prefabName = key.gameObject.name;
                    }

                    Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabName);
                    string bundle = AssetLoadTracker.GetBundleForPrefab(prefabName);
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Potential for ZNetScene.RemoveObjects error spam. " +
                                                             $"ZNetView destroyed without being destroyed through the ZNetScene: " +
                                                             $"{prefabName}. Bundle: {bundle}. Assembly: {assembly?.GetName().Name}");
                }

                AzuDevModPlugin.AzuDevModLogger.LogWarning(str);
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
        if (AzuDevModPlugin.LogUnregisteredZNetViews.Value == AzuDevModPlugin.Toggle.Off) return;
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
        string? prefabName;
        try
        {
            prefabName = __instance.GetPrefabName();
        }
        catch (Exception)
        {
            prefabName = __instance.gameObject.name;
        }

        string prefabNameLower = prefabName.ToLower();
        Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabNameLower);
        string bundle = AssetLoadTracker.GetBundleForPrefab(prefabNameLower);

        StringBuilder sb = new StringBuilder($"ZNetView for '{prefabName}' has not been registered in ZNetScene. ");
        sb.Append("This can cause the ZNetScene.RemoveObjects error spam. ");

        if (assembly != null && !string.IsNullOrEmpty(bundle))
        {
            sb.Append($"The prefab is in the bundle '{bundle}' which is from the assembly '{assembly.GetName().Name}'. ");
        }
        else if (!string.IsNullOrEmpty(bundle))
        {
            sb.Append($"The prefab is in the bundle '{bundle}'. ");
        }
        else if (assembly != null)
        {
            sb.Append($"The prefab is in the assembly '{assembly.GetName().Name}'. ");
        }
        else
        {
            sb.Append("Couldn't find full information for the prefab's mod. ");
        }

        sb.AppendLine($"Full Stack Trace :{Environment.NewLine}{Environment.NewLine}{Environment.StackTrace}{Environment.NewLine}");

        AzuDevModPlugin.AzuDevModLogger.LogWarning(sb.ToString());
    }
}

[HarmonyPatch(typeof(ZNetView), nameof(ZNetView.OnDestroy))]
public class TrackZNetViewDestruction
{
    private static void Postfix(ZNetView __instance) => WatchForDestroyedZNetViewsInScene.DestroyedZNetViews.Add(__instance, $"{__instance.name}\n{Environment.StackTrace}{Environment.NewLine}");
}

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
static class CatchInvalidItemdrops
{
    [HarmonyPriority(-2147483648)]
    private static void Postfix(ObjectDB __instance)
    {
        foreach (GameObject gameObject in __instance.m_items)
        {
            if (gameObject.GetComponent<ItemDrop>() == null)
                AzuDevModPlugin.AzuDevModLogger.LogError($"Found null item drop component on {gameObject.name} when it shouldn't be.");
        }
    }
}