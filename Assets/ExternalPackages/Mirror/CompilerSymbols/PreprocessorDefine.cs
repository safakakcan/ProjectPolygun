using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Mirror
{
    internal static class PreprocessorDefine
    {
        /// <summary>
        ///     Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void AddDefineSymbols()
        {
#if UNITY_2021_2_OR_NEWER
            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
#else
            // Deprecated in Unity 2023.1
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            // Remove oldest when adding next month's symbol.
            // Keep a rolling 12 months of symbols.
            var defines = new HashSet<string>(currentDefines.Split(';'))
            {
                "MIRROR",
                "MIRROR_89_OR_NEWER",
                "MIRROR_90_OR_NEWER",
                "MIRROR_93_OR_NEWER",
                "MIRROR_96_OR_NEWER"
            };

            // only touch PlayerSettings if we actually modified it,
            // otherwise it shows up as changed in git each time.
            var newDefines = string.Join(";", defines);
            if (newDefines != currentDefines)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), newDefines);
#else
                // Deprecated in Unity 2023.1
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
#endif
            }
        }
    }
}