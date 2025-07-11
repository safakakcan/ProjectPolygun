using UnityEngine;

namespace Mirror.Examples.AdditiveLevels
{
    // This script is attached to portal labels to keep them facing the camera
    public class LookAtMainCamera : MonoBehaviour
    {
        // LateUpdate so that all camera updates are finished.
        [ClientCallback]
        private void LateUpdate()
        {
            transform.forward = Camera.main.transform.forward;
        }

        // This will be enabled by Portal script in OnStartClient
        private void OnValidate()
        {
            enabled = false;
        }
    }
}