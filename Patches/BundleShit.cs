using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AzuDevMod.Patches;

[HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), new Type[] { typeof(string), typeof(Type) })]
static class AssetBundleLoadAssetPatch
{
    static void Postfix(AssetBundle __instance, string name, Type type, ref UnityEngine.Object __result)
    {
        if(AzuDevModPlugin.LogAssetBundleIssues.Value == AzuDevModPlugin.Toggle.Off) return;
        if (__instance == null)
        {
            AzuDevModPlugin.AzuDevModLogger.LogError($"AssetBundle is null when loading asset '{name}' of type '{type}'."); // Not really sure how this could happen since the stream would be null first, but it's possible I guess.
            return;
        }

        if (__result == null)
        {
            AzuDevModPlugin.AzuDevModLogger.LogError($"Failed to load asset '{name}' of type '{type}'.");
        }
    }
}

[HarmonyPatch(typeof(Assembly), nameof(Assembly.GetManifestResourceStream), new Type[] { typeof(string) })]
public class GetManifestResourceStreamPatch
{
    public static void Postfix(Assembly __instance, string name, ref Stream __result)
    {
        if(AzuDevModPlugin.LogAssetBundleIssues.Value == AzuDevModPlugin.Toggle.Off) return;
        if (__result == null && name.Substring(name.LastIndexOf('.') + 1) != "png") // Ignore the missing pngs that are caused by LocationManager since they're not really errors, it just calls this method to check if the file is a texture.
        {
            AzuDevModPlugin.AzuDevModLogger.LogError($"Assembly '{__instance.GetName().Name}' failed to load resource/assetbundle '{name.Substring(name.LastIndexOf('.') + 1)}'.");
        }
    }
}