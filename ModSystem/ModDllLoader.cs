using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ModSystem
{
    /// <summary>
    /// Loads a mod's compiled .dll, finds all IMod implementations,
    /// and instantiates them.
    /// </summary>
    public static class ModDllLoader
    {
        static readonly Dictionary<string, Assembly> _assemblies = new();

        /// <summary>
        /// Load the DLL and return all IMod instances found inside it.
        /// Returns empty list on failure.
        /// </summary>
        public static List<IMod> LoadMod(string modId, string dllPath)
        {
            var instances = new List<IMod>();

            if (!File.Exists(dllPath))
            {
                Debug.LogWarning($"[ModDllLoader] DLL not found: {dllPath}");
                return instances;
            }

            try
            {
                // Read bytes and load — avoids file locking issues
                byte[] dllBytes = File.ReadAllBytes(dllPath);

                // Load PDB if present for better stack traces
                string pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                byte[] pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

                Assembly asm = pdbBytes != null
                    ? Assembly.Load(dllBytes, pdbBytes)
                    : Assembly.Load(dllBytes);

                _assemblies[modId] = asm;

                // Find all concrete IMod implementations
                foreach (var type in asm.GetTypes())
                {
                    if (!typeof(IMod).IsAssignableFrom(type)) continue;
                    if (type.IsInterface || type.IsAbstract) continue;

                    try
                    {
                        var instance = (IMod)Activator.CreateInstance(type);
                        instances.Add(instance);
                        Debug.Log($"[ModDllLoader] Found IMod: {type.FullName} in mod '{modId}'.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ModDllLoader] Failed to instantiate {type.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModDllLoader] Failed to load DLL for mod '{modId}': {ex.Message}");
            }

            return instances;
        }


        #region NEEDUPDATE
        public static void UnloadMod(string modId)
        {
            // Note: .NET doesn't allow unloading individual assemblies in standard
            // This clears the reference so GC can collect if nothing else holds it.
            _assemblies.Remove(modId);
        }
        #endregion
        public static bool IsLoaded(string modId) => _assemblies.ContainsKey(modId);
    }
}