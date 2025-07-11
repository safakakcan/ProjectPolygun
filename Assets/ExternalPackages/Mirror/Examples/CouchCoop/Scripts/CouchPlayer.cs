using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.CouchCoop
{
    public class CouchPlayer : NetworkBehaviour
    {
        // a list of players, is used for camera
        public static readonly List<GameObject> playersList = new();
        public Rigidbody rb;
        public float movementSpeed = 3;
        public float jumpSpeed = 6;

        public CouchPlayerManager couchPlayerManager;

        [SyncVar(hook = nameof(OnNumberChangedHook))]
        public int playerNumber;

        public Text textPlayerNumber;
        private bool isGrounded;
        private KeyCode jumpKey = KeyCode.Space; // Check CouchPlayerManager for controls
        private KeyCode leftKey = KeyCode.LeftArrow;
        private float movementVelocity;
        private KeyCode rightKey = KeyCode.RightArrow;

        public void Start()
        {
            playersList.Add(gameObject);
            // print("playersList: " + playersList.Count);

            SetPlayerUI();
        }

        private void Update()
        {
            if (!Application.isFocused) return;
            if (isOwned == false) return;

            // you can control all local players via arrow keys and space bar for fun testing
            // otherwise check and set individual controls in CouchPlayerManager script.
            if (isGrounded)
                if (Input.GetKey(KeyCode.Space) || Input.GetKeyDown(jumpKey))
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
#else
                    rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
#endif
                }

            movementVelocity = 0;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(leftKey)) movementVelocity = -movementSpeed;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(rightKey)) movementVelocity = movementSpeed;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(movementVelocity, rb.linearVelocity.y);
#else
            rb.velocity = new Vector2(movementVelocity, rb.velocity.y);
#endif
        }

        public void OnDestroy()
        {
            playersList.Remove(gameObject);
            // print("playersList: " + playersList.Count);
        }

        [ClientCallback]
        private void OnCollisionExit(Collision col)
        {
            if (isOwned == false) return;
            isGrounded = false;
        }

        [ClientCallback]
        private void OnCollisionStay(Collision col)
        {
            if (isOwned == false) return;
            isGrounded = true;
        }

        public override void OnStartAuthority()
        {
            enabled = true;

            if (isOwned)
            {
#if UNITY_2022_2_OR_NEWER
                couchPlayerManager = FindAnyObjectByType<CouchPlayerManager>();
#else
                // Deprecated in Unity 2023.1
                couchPlayerManager = GameObject.FindObjectOfType<CouchPlayerManager>();
#endif
                // setup controls according to the pre-sets on CouchPlayerManager
                jumpKey = couchPlayerManager.playerKeyJump[playerNumber];
                leftKey = couchPlayerManager.playerKeyLeft[playerNumber];
                rightKey = couchPlayerManager.playerKeyRight[playerNumber];
            }
        }

        private void OnNumberChangedHook(int _old, int _new)
        {
            //Debug.Log(name + " - OnNumberChangedHook: " + playerNumber);
            SetPlayerUI();
        }

        public void SetPlayerUI()
        {
            // called from hook and in start, to solve a race condition
            if (isOwned)
                textPlayerNumber.text = "Local: " + playerNumber;
            else
                textPlayerNumber.text = "Remote: " + playerNumber;
        }
    }
}