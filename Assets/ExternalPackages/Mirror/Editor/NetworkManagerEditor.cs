using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mirror
{
    [CustomEditor(typeof(NetworkManager), true)]
    [CanEditMultipleObjects]
    public class NetworkManagerEditor : Editor
    {
        protected NetworkManager networkManager;
        private ReorderableList spawnList;
        private SerializedProperty spawnListProperty;

        protected void Init()
        {
            if (spawnList == null)
            {
                networkManager = target as NetworkManager;
                spawnListProperty = serializedObject.FindProperty("spawnPrefabs");
                spawnList = new ReorderableList(serializedObject, spawnListProperty)
                {
                    drawHeaderCallback = DrawHeader,
                    drawElementCallback = DrawChild,
                    onReorderCallback = Changed,
                    onRemoveCallback = RemoveButton,
                    onChangedCallback = Changed,
                    onAddCallback = AddButton,
                    // this uses a 16x16 icon. other sizes make it stretch.
                    elementHeight = 16
                };
            }
        }

        public override void OnInspectorGUI()
        {
            Init();
            DrawDefaultInspector();
            EditorGUI.BeginChangeCheck();
            spawnList.DoLayoutList();
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Populate Spawnable Prefabs")) ScanForNetworkIdentities();

            // clicking the Populate button in a large project can add hundreds of entries.
            // have a clear button in case that wasn't intended.
            GUI.enabled = networkManager.spawnPrefabs.Count > 0;
            if (GUILayout.Button("Clear Spawnable Prefabs")) ClearNetworkIdentities();
            GUI.enabled = true;
        }

        private void ScanForNetworkIdentities()
        {
            var identities = new List<GameObject>();
            var cancelled = false;
            try
            {
                var paths = EditorHelper.IterateOverProject("t:prefab").ToArray();
                var count = 0;
                foreach (var path in paths)
                {
                    // ignore test & example prefabs.
                    // users sometimes keep the folders in their projects.
                    if (path.Contains("Mirror/Tests/") ||
                        path.Contains("Mirror/Examples/"))
                        continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Searching for NetworkIdentities..",
                            $"Scanned {count}/{paths.Length} prefabs. Found {identities.Count} new ones",
                            count / (float)paths.Length))
                    {
                        cancelled = true;
                        break;
                    }

                    count++;

                    var ni = AssetDatabase.LoadAssetAtPath<NetworkIdentity>(path);
                    if (!ni) continue;

                    if (!networkManager.spawnPrefabs.Contains(ni.gameObject)) identities.Add(ni.gameObject);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!cancelled)
                {
                    // RecordObject is needed for "*" to show up in Scene.
                    // however, this only saves List.Count without the entries.
                    Undo.RecordObject(networkManager, "NetworkManager: populated prefabs");

                    // add the entries
                    networkManager.spawnPrefabs.AddRange(identities);

                    // sort alphabetically for better UX
                    networkManager.spawnPrefabs = networkManager.spawnPrefabs.OrderBy(go => go.name).ToList();

                    // SetDirty is required to save the individual entries properly.
                    EditorUtility.SetDirty(target);
                }

                // Loading assets might use a lot of memory, so try to unload them after
                Resources.UnloadUnusedAssets();
            }
        }

        private void ClearNetworkIdentities()
        {
            // RecordObject is needed for "*" to show up in Scene.
            // however, this only saves List.Count without the entries.
            Undo.RecordObject(networkManager, "NetworkManager: cleared prefabs");

            // add the entries
            networkManager.spawnPrefabs.Clear();

            // SetDirty is required to save the individual entries properly.
            EditorUtility.SetDirty(target);
        }

        private static void DrawHeader(Rect headerRect)
        {
            GUI.Label(headerRect, "Registered Spawnable Prefabs:");
        }

        internal void DrawChild(Rect r, int index, bool isActive, bool isFocused)
        {
            var prefab = spawnListProperty.GetArrayElementAtIndex(index);
            var go = (GameObject)prefab.objectReferenceValue;

            GUIContent label;
            if (go == null)
            {
                label = new GUIContent("Empty", "Drag a prefab with a NetworkIdentity here");
            }
            else
            {
                var identity = go.GetComponent<NetworkIdentity>();
                label = new GUIContent(go.name, identity != null ? $"AssetId: [{identity.assetId}]" : "No Network Identity");
            }

            var newGameObject = (GameObject)EditorGUI.ObjectField(r, label, go, typeof(GameObject), false);

            if (newGameObject != go)
            {
                if (newGameObject != null && !newGameObject.GetComponent<NetworkIdentity>())
                {
                    Debug.LogError($"Prefab {newGameObject} cannot be added as spawnable as it doesn't have a NetworkIdentity.");
                    return;
                }

                prefab.objectReferenceValue = newGameObject;
            }
        }

        internal void Changed(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
        }

        internal void AddButton(ReorderableList list)
        {
            spawnListProperty.arraySize += 1;
            list.index = spawnListProperty.arraySize - 1;

            var obj = spawnListProperty.GetArrayElementAtIndex(spawnListProperty.arraySize - 1);
            obj.objectReferenceValue = null;

            spawnList.index = spawnList.count - 1;

            Changed(list);
        }

        internal void RemoveButton(ReorderableList list)
        {
            spawnListProperty.DeleteArrayElementAtIndex(spawnList.index);
            if (list.index >= spawnListProperty.arraySize) list.index = spawnListProperty.arraySize - 1;
        }
    }
}