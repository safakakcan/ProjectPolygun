using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mirror
{
    public static class EditorHelper
    {
        public static string FindPath<T>()
        {
            var typeName = typeof(T).Name;

            var guidsFound = AssetDatabase.FindAssets($"t:Script {typeName}");
            if (guidsFound.Length >= 1 && !string.IsNullOrWhiteSpace(guidsFound[0]))
            {
                if (guidsFound.Length > 1) Debug.LogWarning($"Found more than one{typeName}");

                var path = AssetDatabase.GUIDToAssetPath(guidsFound[0]);
                return Path.GetDirectoryName(path);
            }

            Debug.LogError($"Could not find path of {typeName}");
            return string.Empty;
        }


        public static IEnumerable<string> IterateOverProject(string filter)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter)) yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}