using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace AzuDevMod.Util;

public class AssetLoadTracker
{
    private static readonly Dictionary<string, string> PrefabToBundleMapping = new();
    private static readonly Dictionary<string, Assembly> BundleToAssemblyMapping = new();

    internal static void MapPrefabsToBundles()
    {
        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            var allAssetNames = bundle.GetAllAssetNames();
            var prefabNames = allAssetNames.Where(name => name.EndsWith(".prefab"));

            foreach (var prefab in prefabNames)
            {
                string simpleName = System.IO.Path.GetFileNameWithoutExtension(prefab);
                PrefabToBundleMapping[simpleName] = bundle.name;
            }
        }
    }

    internal static void MapBundlesToAssemblies()
    {
        // AppDomain.CurrentDomain.GetAssemblies() didn't work here since they are dynamically loaded. This worked though.
        var allAssemblies = Chainloader.PluginInfos.Select(keyValuePair => keyValuePair.Value.Instance.GetType().Assembly).ToList();


        foreach (var bundleName in PrefabToBundleMapping.Values.Distinct())
        {
            foreach (var assembly in allAssemblies)
            {
                try
                {
                    var resourceNames = assembly.GetManifestResourceNames();
                    if (resourceNames.Any(resourceName => resourceName.EndsWith(bundleName)))
                    {
                        BundleToAssemblyMapping[bundleName] = assembly;
                        break;
                    }
                }
                catch (Exception e)
                {
                    AzuDevModPlugin.AzuDevModLogger.LogError($"Error while getting manifest resource names for assembly {assembly.GetName().Name}: {e}");
                }
            }
        }
    }


    public static Assembly? GetAssemblyForPrefab(string prefabName)
    {
        if (PrefabToBundleMapping.TryGetValue(prefabName, out var bundleName))
        {
            if (BundleToAssemblyMapping.TryGetValue(bundleName, out var assembly))
            {
                return assembly;
            }
        }

        return null;
    }

    public static string GetBundleForPrefab(string prefabName)
    {
        return PrefabToBundleMapping.TryGetValue(prefabName, out var bundleName) ? bundleName : "";
    }
}