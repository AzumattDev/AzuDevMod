using System;
using System.Reflection;
using System.Text;

namespace AzuDevMod.Util;

public class LoggingMethods
{
    public static void LogWithPrefabInfo(string messagePrefix, string identifier, string additionalInfo = "")
    {
        string prefabName = identifier; // Assuming identifier is already the best name we have
        string bundleName = AssetLoadTracker.GetBundleForPrefab(identifier.ToLowerInvariant()) ?? "Unknown Bundle";
        Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(identifier.ToLowerInvariant());

        StringBuilder sb = new StringBuilder($"{messagePrefix}: {prefabName}. ");

        if (assembly != null && !string.IsNullOrEmpty(bundleName))
        {
            sb.Append($"The prefab is in the bundle '{bundleName}' and the assembly '{assembly.GetName().Name}'. ");
        }
        else if (!string.IsNullOrEmpty(bundleName))
        {
            sb.Append($"The prefab is in the bundle '{bundleName}'. ");
        }
        else if (assembly != null)
        {
            sb.Append($"The prefab is in the assembly '{assembly.GetName().Name}'. ");
        }
        else
        {
            sb.Append("Couldn't find full information for the prefab's mod. ");
        }

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            sb.AppendLine().Append("Additional Info: ").Append(additionalInfo);
        }

        sb.AppendLine($"Full Stack Trace:{Environment.NewLine}{Environment.StackTrace}");

        AzuDevModPlugin.AzuDevModLogger.LogError(sb.ToString());
    }
}