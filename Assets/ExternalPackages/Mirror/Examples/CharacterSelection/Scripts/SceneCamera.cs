using UnityEngine;

namespace Mirror.Examples.CharacterSelection
{
    public class SceneCamera : NetworkBehaviour
    {
        [Header("Components")] [SerializeField]
        private CharacterSelection characterSelection;

        [SerializeField] private Transform cameraTarget;

        [Header("Diagnostics")] [ReadOnly] [SerializeField]
        private SceneReferencer sceneReferencer;

        [ReadOnly] [SerializeField] private Transform cameraObj;

        private void Reset()
        {
            characterSelection = GetComponent<CharacterSelection>();
            cameraTarget = transform.Find("CameraTarget");
            enabled = false;
        }

        private void Update()
        {
            if (!Application.isFocused)
                return;

            if (cameraObj && characterSelection)
                characterSelection.floatingInfo.forward = cameraObj.transform.forward;

            if (cameraObj && cameraTarget)
                cameraObj.SetPositionAndRotation(cameraTarget.position, cameraTarget.rotation);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Reset();
        }

        public override void OnStartAuthority()
        {
#if UNITY_2022_2_OR_NEWER
            sceneReferencer = FindAnyObjectByType<SceneReferencer>();
#else
            // Deprecated in Unity 2023.1
            sceneReferencer = GameObject.FindObjectOfType<SceneReferencer>();
#endif

            cameraObj = sceneReferencer.cameraObject.transform;

            enabled = true;
        }

        public override void OnStopAuthority()
        {
            enabled = false;
        }
    }
}