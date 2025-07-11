using Mirror.Examples.Common;
using Mirror.Examples.Common.Controllers.Tank;
using UnityEngine;

namespace Mirror.Examples.TankTheftAuto
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class TankAuthority : NetworkBehaviour
    {
        [Header("Components")] public GameObject triggerUI;

        public TankTurretBase tankTurret;
        public GameObject tankTrigger;

        [SyncVar(hook = nameof(OnIsControlledChanged))]
        public bool isControlled;

        private void Reset()
        {
            if (triggerUI == null)
                triggerUI = transform.Find("TriggerUI").gameObject;

            if (tankTrigger == null)
                tankTrigger = transform.Find("TankTrigger").gameObject;

            if (tankTurret == null)
                tankTurret = GetComponent<TankTurretBase>();

            triggerUI.SetActive(false);
        }

        [ClientCallback]
        private void Update()
        {
            if (triggerUI.activeSelf && Input.GetKeyDown(KeyCode.C))
                CmdTakeControl();

            if (isOwned && Input.GetKeyDown(KeyCode.X))
                CmdReleaseControl();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isClient || !other.gameObject.CompareTag("Player")) return;

            if (other.TryGetComponent(out NetworkIdentity networkIdentity))
                if (networkIdentity == NetworkClient.localPlayer)
                    triggerUI.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isClient || !other.gameObject.CompareTag("Player")) return;

            if (other.TryGetComponent(out NetworkIdentity networkIdentity))
                if (networkIdentity == NetworkClient.localPlayer)
                    triggerUI.SetActive(false);
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;

            base.OnValidate();
            Reset();
        }

        private void OnIsControlledChanged(bool _, bool newValue)
        {
            tankTrigger.SetActive(!newValue);
        }

        [Command(requiresAuthority = false)]
        private void CmdTakeControl(NetworkConnectionToClient conn = null)
        {
            // someone else is already controlling this tank
            if (connectionToClient != null)
            {
                Debug.LogWarning("Someone else is already controlling this tank");
                return;
            }

            // cache the regular player object
            conn.authenticationData = conn.identity.gameObject;

            // set the color to match the player
            if (conn.identity.TryGetComponent(out RandomColor randomColor))
                tankTurret.playerColor = randomColor.color;

            isControlled = true;

            NetworkServer.ReplacePlayerForConnection(conn, gameObject, ReplacePlayerOptions.Unspawn);
        }

        [Command]
        private void CmdReleaseControl()
        {
            // get the regular player object
            if (connectionToClient.authenticationData is GameObject player)
            {
                // Set pos and rot to match the tank, plus 3m offset to the right plus 1m up
                // because character controller pivot is at the center, not at the bottom.
                var pos = transform.position + transform.right * 3 + Vector3.up;
                player.transform.SetPositionAndRotation(pos, transform.rotation);

                // set the player object back to the player
                isControlled = false;
                tankTurret.playerColor = Color.black;

                // clear the player object
                connectionToClient.authenticationData = null;

                NetworkServer.ReplacePlayerForConnection(connectionToClient, player, ReplacePlayerOptions.KeepActive);
            }
        }

        public override void OnStartAuthority()
        {
            if (triggerUI.TryGetComponent(out TextMesh textMesh))
                textMesh.text = "Press 'X' to release control";
        }

        public override void OnStopAuthority()
        {
            if (triggerUI.TryGetComponent(out TextMesh textMesh))
                textMesh.text = "Press 'C' to take control";
        }

        public override void OnStartClient()
        {
            tankTrigger.SetActive(!isControlled);
        }

        public override void OnStopClient()
        {
            triggerUI.SetActive(false);
            tankTrigger.SetActive(true);
        }
    }
}