using UnityEngine;

namespace Mirror.Examples.Basic
{
    public class CanvasUI : MonoBehaviour
    {
        // static instance that can be referenced from static methods below.
        private static CanvasUI instance;

        [Tooltip("Assign Main Panel so it can be turned on from Player:OnStartClient")]
        public RectTransform mainPanel;

        [Tooltip("Assign Players Panel for instantiating PlayerUI as child")]
        public RectTransform playersPanel;

        private void Awake()
        {
            instance = this;
        }

        public static void SetActive(bool active)
        {
            instance.mainPanel.gameObject.SetActive(active);
        }

        public static RectTransform GetPlayersPanel()
        {
            return instance.playersPanel;
        }
    }
}