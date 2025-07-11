using System.Collections.Generic;
using System.Reflection;
using kcp2k;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Edgegap
{
    [CustomEditor(typeof(EdgegapLobbyKcpTransport))]
    public class EncryptionTransportInspector : Editor
    {
        private readonly List<SerializedProperty> kcpProperties = new();
        private SerializedProperty lobbyUrlProperty;
        private SerializedProperty lobbyWaitTimeoutProperty;


        // Assuming proper SerializedProperty definitions for properties
        // Add more SerializedProperty fields related to different modes as needed

        private void OnEnable()
        {
            lobbyUrlProperty = serializedObject.FindProperty("lobbyUrl");
            lobbyWaitTimeoutProperty = serializedObject.FindProperty("lobbyWaitTimeout");
            // Get public fields from KcpTransport
            kcpProperties.Clear();
            var fields = typeof(KcpTransport).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var prop = serializedObject.FindProperty(field.Name);
                if (prop == null)
                    // callbacks have no property
                    continue;
                kcpProperties.Add(prop);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(lobbyUrlProperty);
            if (GUILayout.Button("Create & Deploy Lobby"))
            {
                var input = CreateInstance<LobbyServiceCreateDialogue>();
                input.onLobby = url =>
                {
                    lobbyUrlProperty.stringValue = url;
                    serializedObject.ApplyModifiedProperties();
                };
                input.ShowUtility();
            }

            EditorGUILayout.PropertyField(lobbyWaitTimeoutProperty);
            EditorGUILayout.Separator();
            foreach (var prop in kcpProperties) EditorGUILayout.PropertyField(prop);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif