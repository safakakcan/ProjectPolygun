#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Edgegap.Editor
{
    public class CustomPopupContent : PopupWindowContent
    {
        private readonly List<string> _btnNames;
        private readonly string _defaultValue = "";
        private readonly float _maxHeight = 100;

        private readonly float _minHeight = 25;
        private readonly Action<string> _onBtnClick;
        private readonly float _width;
        private Vector2 scrollPos;

        public CustomPopupContent(
            List<string> btnNames,
            Action<string> btnCallback,
            string defaultValue,
            float width = 400
        )
        {
            _btnNames = btnNames;
            _onBtnClick = btnCallback;
            _width = width;
            _defaultValue = defaultValue;
        }

        public override Vector2 GetWindowSize()
        {
            var height = _minHeight;

            if (_btnNames.Count > 0) height *= _btnNames.Count;

            return new Vector2(_width, height <= _maxHeight ? height : _maxHeight);
        }

        public override void OnGUI(Rect rect)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var name in _btnNames)
                if (GUILayout.Button(name, GUILayout.Width(_width - 25)))
                {
                    if (name == "Create New Application")
                        _onBtnClick(_defaultValue);
                    else
                        _onBtnClick(name);

                    editorWindow.Close();
                }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif