using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MoveToAssetsFolder
{
    private const string FirstTimeKey = "MOVE_SCRIPT_TEMPLATES_HAS_RUN";
    private const string targetFolder = "ScriptTemplates";
    private const string targetPath = "Assets/ScriptTemplates";

    static MoveToAssetsFolder()
    {
        if (!SessionState.GetBool(FirstTimeKey, false))
        {
            FindAndMoveScriptTemplatesFolder();
            SessionState.SetBool(FirstTimeKey, true);
        }
    }

    private static void FindAndMoveScriptTemplatesFolder()
    {
        var guids = AssetDatabase.FindAssets(targetFolder, null);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // Check if it's a folder and not some random asset
            if (AssetDatabase.IsValidFolder(path))
            {
                // Ensure exact match of the name and that it's not in the Assets folder already
                var folderName = Path.GetFileName(path);
                if (folderName == targetFolder && !path.StartsWith(targetPath))
                {
                    AssetDatabase.MoveAsset(path, targetPath);
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Moved {targetFolder} to Assets folder.");
                }
            }
        }

        AssetDatabase.Refresh();
    }
}