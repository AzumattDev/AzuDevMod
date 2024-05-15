using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;

namespace AzuDevMod.Patches;

[HarmonyPatch(typeof(Harmony), nameof(Harmony.UnpatchAll), new Type[] { })]
public class DumpStacktrace
{
    private static bool Prefix(Harmony __instance)
    {
        if (AzuDevModPlugin.LogUnpatchAll.Value == AzuDevModPlugin.Toggle.Off) return true;
        try
        {
            string stackTrace = Environment.StackTrace;

            List<string> modNames = ExtractModNames(stackTrace);
            if (modNames.Count <= 0) return true;
            foreach (string? modName in modNames)
            {
                if (!string.IsNullOrEmpty(modName))
                {
                    Chainloader.PluginInfos.TryGetValue(__instance.Id, out PluginInfo? pluginInfo);
                    string pluginInformation = string.Empty;
                    if (pluginInfo != null)
                    {
                        pluginInformation = $" Mod: [{pluginInfo.Metadata.Name}] GUID: [{pluginInfo.Metadata.GUID}]";
                    }
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Mod/Class causing the UnpatchAll, the UnpatchAll was prevented: {modName}{pluginInformation}");
                    return false; // Stop the UnpatchAll
                }

                AzuDevModPlugin.AzuDevModLogger.LogWarning($"Unable to determine the mod causing the UnpatchAll from the stack trace being parsed. Printing everything {Environment.NewLine}{Environment.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            AzuDevModPlugin.AzuDevModLogger.LogError($"Error while processing the UnpatchAll Prefix: {ex}");
        }

        return true;
    }

    private static List<string> ExtractModNames(string stackTrace)
    {
        MatchCollection? matches = Regex.Matches(stackTrace, @"at (?<modName>[\w\.]+)\.OnDestroy");

        return (from Match match in matches where match.Success select match.Groups["modName"].Value).ToList();
    }
}

[HarmonyPatch(typeof(Harmony), nameof(Harmony.UnpatchSelf))]
public class DumpStacktrace2
{
    private static bool Prefix(Harmony __instance)
    {
        if (AzuDevModPlugin.LogUnpatchAll.Value == AzuDevModPlugin.Toggle.Off) return true;
        try
        {
            string stackTrace = Environment.StackTrace;

            List<string> modNames = ExtractModNames(stackTrace);
            if (modNames.Count <= 0) return true;
            foreach (string? modName in modNames)
            {
                if (!string.IsNullOrEmpty(modName))
                {
                    Chainloader.PluginInfos.TryGetValue(__instance.Id, out PluginInfo? pluginInfo);
                    string pluginInformation = string.Empty;
                    if (pluginInfo != null)
                    {
                        pluginInformation = $" Mod: [{pluginInfo.Metadata.Name}] GUID: [{pluginInfo.Metadata.GUID}]";
                    }
                    AzuDevModPlugin.AzuDevModLogger.LogError($"UnpatchSelf prevented: {modName}{pluginInformation}");
                    return false; // Stop the UnpatchAll
                }

                AzuDevModPlugin.AzuDevModLogger.LogWarning($"Unable to determine the mod causing the UnpatchSelf from the stack trace being parsed. Printing everything {Environment.NewLine}{Environment.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            AzuDevModPlugin.AzuDevModLogger.LogError($"Error while processing the UnpatchSelf Prefix: {ex}");
        }

        return true;
    }

    private static List<string> ExtractModNames(string stackTrace)
    {
        MatchCollection? matches = Regex.Matches(stackTrace, @"at (?<modName>[\w\.]+)\.OnDestroy");

        return (from Match match in matches where match.Success select match.Groups["modName"].Value).ToList();
    }
}


/*[HarmonyPatch(typeof(Harmony), nameof(Harmony.UnpatchSelf))]
static class HarmonyUnpatchSelfPatch
{
    internal static readonly List<string> DetectedPlugins = new List<string>();

    [HarmonyPriority(Priority.VeryHigh)]
    static void Prefix(Harmony __instance)
    {
        var pluginDetails = $"Harmony ID: {__instance.Id}";
        if (Chainloader.PluginInfos.TryGetValue(__instance.Id, out PluginInfo? pluginInfo))
        {
            pluginDetails += $", Mod: {pluginInfo.Metadata.Name}";
        }

        if (!DetectedPlugins.Contains(pluginDetails))
        {
            DetectedPlugins.Add(pluginDetails);
        }
    }
}*/