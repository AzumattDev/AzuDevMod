using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AzuDevMod;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

public class MissingFieldDetector : MonoBehaviour
{
    private static Dictionary<string, List<string>> modsUsingMissingFields = new Dictionary<string, List<string>>();
    private static List<Tuple<string, string>> fieldsToCheck = new List<Tuple<string, string>>();

    public static void Init(List<Tuple<string, string>> fields)
    {
        fieldsToCheck = fields;
        ScanPatchedMethodsForMissingField();
        ReportModsUsingMissingField();
    }

    private static void ScanPatchedMethodsForMissingField()
    {
        foreach (var field in fieldsToCheck)
        {
            string className = field.Item1;
            string fieldName = field.Item2;

            var allPatchedMethods = Harmony.GetAllPatchedMethods().Where(m => m.DeclaringType.FullName == className);

            foreach (var method in allPatchedMethods)
            {
                var patches = Harmony.GetPatchInfo(method);
                if (patches != null)
                {
                    foreach (var patch in patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers))
                    {
                        try
                        {
                            List<Instruction> instructions = MethodBodyReader.ReadInstructions(patch.PatchMethod);
                            if (InstructionsAccessField(instructions, className, fieldName))
                            {
                                var module = patch.PatchMethod.Module;
                                var assemblyName = module.Assembly.GetName().Name;
                                if (!modsUsingMissingFields.ContainsKey(assemblyName))
                                {
                                    modsUsingMissingFields[assemblyName] = new List<string>();
                                }
                                string fieldKey = $"{className}.{fieldName}";
                                if (!modsUsingMissingFields[assemblyName].Contains(fieldKey))
                                {
                                    modsUsingMissingFields[assemblyName].Add(fieldKey);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //AzuDevModPlugin.AzuDevModLogger.LogError($"Failed to read instructions for patch method {patch.PatchMethod.Name}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    private static bool InstructionsAccessField(IEnumerable<Instruction> instructions, string className, string fieldName)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Operand is FieldReference fieldReference)
            {
                if (fieldReference.DeclaringType.FullName == className && fieldReference.Name == fieldName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ReportModsUsingMissingField()
    {
        if (modsUsingMissingFields.Count > 0)
        {
            foreach (var mod in modsUsingMissingFields)
            {
                AzuDevModPlugin.AzuDevModLogger.LogWarning($"Mod '{mod.Key}' is using the following missing fields: {string.Join(", ", mod.Value)}");
            }
        }
        else
        {
            AzuDevModPlugin.AzuDevModLogger.LogInfo("No mods found using the missing fields.");
        }
    }
}

public static class MethodBodyReader
{
    public static List<Instruction> ReadInstructions(MethodBase method)
    {
        var modulePath = method.Module.FullyQualifiedName;
        var readerParams = new ReaderParameters { ReadSymbols = false }; // Do not read symbols
        var module = ModuleDefinition.ReadModule(modulePath, readerParams);
        var methodDefinition = module.GetType(method.DeclaringType.FullName).Methods.FirstOrDefault(m => m.Name == method.Name);
        return methodDefinition?.Body?.Instructions.ToList() ?? new List<Instruction>();
    }
}