using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using System.Linq;
using SingularityGroup.HotReload.Newtonsoft.Json;
using UnityEditor.Compilation;

namespace SingularityGroup.HotReload.Editor {
    public static class AssemblyOmission {
        // [MenuItem("Window/Hot Reload Dev/List omitted projects")]
        private static void Check() {
            Log.Info("To compile C# files same as a Player build, we must omit projects which aren't part of the selected Player build.");
            var omitted = GetOmittedProjects(EditorUserBuildSettings.activeScriptCompilationDefines);
            Log.Info("---------");

            foreach (var name in omitted) {
                Log.Info("omitted editor/other project named: {0}", name);
            }
        }
        
        [JsonObject(MemberSerialization.Fields)]
        private class AssemblyDefinitionJson {
            public string name;
            public string[] defineConstraints;
        }
        
        // scripts in Assets/ (with no asmdef) are always compiled into Assembly-CSharp
        private static readonly string alwaysIncluded = "Assembly-CSharp";

        private class Cache : AssetPostprocessor {
            public static string[] ommitedProjects;
            
            private static void OnPostprocessAllAssets(string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths) {
                ommitedProjects = null;
            }
        }
        
        // main thread only
        public static IEnumerable<string> GetOmittedProjects(string allDefineSymbols, bool verboseLogs = false) {
            if (Cache.ommitedProjects != null) {
                return Cache.ommitedProjects;
            }
            var arr = allDefineSymbols.Split(';');
            var omitted = GetOmittedProjects(arr, verboseLogs);
            Cache.ommitedProjects = omitted.ToArray();
            return omitted;
        }

        private static IEnumerable<string> GetOmittedProjects(string[] allDefineSymbols, bool verboseLogs = false) {
            // HotReload uses names of assemblies.
            var editorAssemblies = GetEditorAssemblies();

            editorAssemblies.Remove(alwaysIncluded);
            var omittedByConstraint = DefineConstraints.GetOmittedAssemblies(allDefineSymbols);
            editorAssemblies.AddRange(omittedByConstraint);

            // Note: other platform player assemblies are also returned here, but I haven't seen it cause issues
            //   when using Hot Reload with IdleGame Android build. 
            var playerAssemblies = GetPlayerAssemblies().ToArray();

            if (verboseLogs) {
                foreach (var name in editorAssemblies) {
                    Log.Info("found project named {0}", name);
                }
                foreach (var playerAssemblyName in playerAssemblies) {
                    Log.Debug("player assembly named {0}", playerAssemblyName);
                }
            }
            // leaves the editor assemblies that are not built into player assemblies (e.g. editor and test assemblies)
            var toOmit = editorAssemblies.Except(playerAssemblies.Select(asm => asm.name));
            var unique = new HashSet<string>(toOmit);
            return unique.ToList();
        }

        // main thread only
        public static List<string> GetEditorAssemblies() {
            return CompilationPipeline
                .GetAssemblies(AssembliesType.Editor)
                .Select(asm => asm.name)
                .ToList();
        }

        public static Assembly[] GetPlayerAssemblies() {
            var playerAssemblyNames = CompilationPipeline
                #if UNITY_2019_3_OR_NEWER
                .GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies) // since Unity 2019.3
                #else
                .GetAssemblies(AssembliesType.Player)
                #endif
                .ToArray();
            

            return playerAssemblyNames;
        }
        
        public static class DefineConstraints {
            /// <summary>
            /// When define constraints evaluate to false, we need 
            /// </summary>
            /// <param name="defineSymbols"></param>
            /// <returns></returns>
            /// <remarks>
            /// Not aware of a Unity api to read defineConstraints, so we do it ourselves.<br/>
            /// Find any asmdef files where the define constraints evaluate to false.
            /// </remarks>
            public static string[] GetOmittedAssemblies(string[] defineSymbols) {
                var guids = AssetDatabase.FindAssets("t:asmdef");
                var asmdefFiles = guids.Select(AssetDatabase.GUIDToAssetPath);
                var shouldOmit = new List<string>();
                foreach (var asmdefFile in asmdefFiles) {
                    var asmdef = ReadDefineConstraints(asmdefFile);
                    if (asmdef == null) continue;
                    if (asmdef.defineConstraints == null || asmdef.defineConstraints.Length == 0) {
                        // Hot Reload already handles assemblies correctly if they have no define symbols.
                        continue;
                    }

                    var allPass = asmdef.defineConstraints.All(constraint => EvaluateDefineConstraint(constraint, defineSymbols));
                    if (!allPass) {
                        shouldOmit.Add(asmdef.name);
                    }
                }

                return shouldOmit.ToArray();
            }

            static AssemblyDefinitionJson ReadDefineConstraints(string path) {
                try {
                    var json = File.ReadAllText(path);
                    var asmdef = JsonConvert.DeserializeObject<AssemblyDefinitionJson>(json);
                    return asmdef;
                } catch (Exception) {
                    // ignore malformed asmdef
                    return null;
                }
            }
            
            /// <summary>
            /// Evaluate a define constraint like 'UNITY_ANDROID || UNITY_IOS'
            /// </summary>
            /// <param name="input"></param>
            /// <param name="defineSymbols"></param>
            /// <returns></returns>
            public static bool EvaluateDefineConstraint(string input, string[] defineSymbols) {
                foreach (var defineSymbol in defineSymbols) {
                    input = input.Replace(defineSymbol, "true");
                }

                // Unity Define Constraints syntax is described in the docs https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html
                var syntaxMap = new Dictionary<string, string> {
                    { "||", "OR" },
                    { "&&", "AND" },
                    { "!", "NOT" }
                };
                // map Unity defineConstraints syntax to DataTable syntax (unity supports both)
                foreach (var item in syntaxMap) {
                    // surround with space because || may not have spaces around it
                    input = input.Replace(item.Key, $" {item.Value} ");
                }

                // remove any extra spaces we just created
                input = input.Replace("  ", " ");

                var allPossibleSyntax = syntaxMap.Values.Concat(new[] { "true", "false" });

                var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // known define symbols already replaced, so any unknown syntax is a missing define symbol
                var notFoundDefineSymbols =
                    tokens.Where(token => !allPossibleSyntax.Contains(token));
                foreach (var defineSymbol in notFoundDefineSymbols) {
                    input = input.Replace(defineSymbol, "false");
                }

                var dt = new DataTable();
                return (bool)dt.Compute(input, "");
            }
        }
    }

}