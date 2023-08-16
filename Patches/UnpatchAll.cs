using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Text.RegularExpressions;

namespace AzuDevMod.Patches;

[HarmonyPatch(typeof(Harmony), nameof(Harmony.UnpatchAll), new Type[] { })]
public class DumpStacktrace
{
    private static bool Prefix()
    {
        if(AzuDevModPlugin.LogUnpatchAll.Value == AzuDevModPlugin.Toggle.Off) return true;
        try
        {
            string stackTrace = Environment.StackTrace;

            List<string> modNames = ExtractModNames(stackTrace);
            if (modNames.Count <= 0) return true;
            foreach (var modName in modNames)
            {
                if (!string.IsNullOrEmpty(modName))
                {
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Mod/Class causing the UnpatchAll, the UnpatchAll was prevented: {modName}");
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
        var matches = Regex.Matches(stackTrace, @"at (?<modName>[\w\.]+)\.OnDestroy");

        return (from Match match in matches where match.Success select match.Groups["modName"].Value).ToList();
    }
}