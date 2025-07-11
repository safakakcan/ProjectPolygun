using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirror.Examples.Common.Controllers.Player
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(NetworkIdentity))]
    [DisallowMultipleComponent]
    public class PlayerControllerRBBase : NetworkBehaviour
    {
        [Flags]
        public enum ControlOptions : byte
        {
            None,
            MouseSteer = 1 << 0,
            AutoRun = 1 << 1,
            ShowUI = 1 << 2
        }

        public enum GroundState : byte
        {
            Grounded,
            Jumping,
            Falling
        }

        private const float BASE_DPI = 96f;

        [Header("Avatar Components")] public Rigidbody rigidBody;

        public CapsuleCollider capsuleCollider;

        [Header("User Interface")] public GameObject ControllerUIPrefab;

        [Header("Configuration")] [SerializeField]
        public MoveKeys moveKeys = new()
        {
            Forward = KeyCode.W,
            Back = KeyCode.S,
            StrafeLeft = KeyCode.A,
            StrafeRight = KeyCode.D,
            TurnLeft = KeyCode.Q,
            TurnRight = KeyCode.E,
            Jump = KeyCode.Space
        };

        [SerializeField] public OptionsKeys optionsKeys = new()
        {
            MouseSteer = KeyCode.M,
            AutoRun = KeyCode.R,
            ToggleUI = KeyCode.U
        };

        [Space(5)] public ControlOptions controlOptions = ControlOptions.ShowUI;

        [Header("Movement")] [Range(0, 20)] [FormerlySerializedAs("moveSpeedMultiplier")] [Tooltip("Speed in meters per second")]
        public float maxMoveSpeed = 8f;

        // Replacement for Sensitvity from Input Settings.
        [Range(0, 10f)] [Tooltip("Sensitivity factors into accelleration")]
        public float inputSensitivity = 2f;

        // Replacement for Gravity from Input Settings.
        [Range(0, 10f)] [Tooltip("Gravity factors into decelleration")]
        public float inputGravity = 2f;

        [Header("Turning")] [Range(0, 300f)] [Tooltip("Max Rotation in degrees per second")]
        public float maxTurnSpeed = 100f;

        [Range(0, 10f)] [FormerlySerializedAs("turnDelta")] [Tooltip("Rotation acceleration in degrees per second squared")]
        public float turnAcceleration = 3f;

        [Header("Jumping")] [Range(0, 10f)] [Tooltip("Initial jump speed in meters per second")]
        public float initialJumpSpeed = 2.5f;

        [Range(0, 10f)] [Tooltip("Maximum jump speed in meters per second")]
        public float maxJumpSpeed = 3.5f;

        [Range(0, 10f)] [FormerlySerializedAs("jumpDelta")] [Tooltip("Jump acceleration in meters per second squared")]
        public float jumpAcceleration = 4f;

        [Header("Diagnostics")] public RuntimeData runtimeData;

        private void Start()
        {
            Application.targetFrameRate = NetworkManager.singleton.sendRate;
            Time.fixedDeltaTime = 1f / NetworkManager.singleton.sendRate;
        }

        private void Update()
        {
            HandleOptions();

            var deltaTime = Time.deltaTime;

            if (controlOptions.HasFlag(ControlOptions.MouseSteer))
                HandleMouseSteer(deltaTime);
            else
                HandleTurning(deltaTime);

            HandleJumping(deltaTime);
            HandleMove(deltaTime);
        }

        private void FixedUpdate()
        {
            var fixedDeltaTime = Time.fixedDeltaTime;
            ApplyMove(fixedDeltaTime);

            // Update ground state
            var isGrounded = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.height / 2 + 0.1f);
            if (isGrounded)
                runtimeData.groundState = GroundState.Grounded;
            else if (runtimeData.groundState != GroundState.Jumping)
                runtimeData.groundState = GroundState.Falling;

            // Update velocity for diagnostics
#if UNITY_6000_0_OR_NEWER
            runtimeData.velocity = Vector3Int.FloorToInt(rigidBody.linearVelocity);
#else
            runtimeData.velocity = Vector3Int.FloorToInt(rigidBody.velocity);
#endif
        }

        private void HandleOptions()
        {
            if (optionsKeys.MouseSteer != KeyCode.None && Input.GetKeyUp(optionsKeys.MouseSteer))
            {
                controlOptions ^= ControlOptions.MouseSteer;
                SetCursor(controlOptions.HasFlag(ControlOptions.MouseSteer));
            }

            if (optionsKeys.AutoRun != KeyCode.None && Input.GetKeyUp(optionsKeys.AutoRun))
                controlOptions ^= ControlOptions.AutoRun;

            if (optionsKeys.ToggleUI != KeyCode.None && Input.GetKeyUp(optionsKeys.ToggleUI))
            {
                controlOptions ^= ControlOptions.ShowUI;

                if (runtimeData.controllerUI != null)
                    runtimeData.controllerUI.SetActive(controlOptions.HasFlag(ControlOptions.ShowUI));
            }
        }

        private void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Turning works while airborne...feature?
        private void HandleTurning(float deltaTime)
        {
            var targetTurnSpeed = 0f;

            // TurnLeft and TurnRight cancel each other out, reducing targetTurnSpeed to zero.
            if (moveKeys.TurnLeft != KeyCode.None && Input.GetKey(moveKeys.TurnLeft))
                targetTurnSpeed -= maxTurnSpeed;
            if (moveKeys.TurnRight != KeyCode.None && Input.GetKey(moveKeys.TurnRight))
                targetTurnSpeed += maxTurnSpeed;

            // If there's turn input or AutoRun is not enabled, adjust turn speed towards target
            // If no turn input and AutoRun is enabled, maintain the previous turn speed
            if (targetTurnSpeed != 0f || !controlOptions.HasFlag(ControlOptions.AutoRun))
                runtimeData.turnSpeed = Mathf.MoveTowards(runtimeData.turnSpeed, targetTurnSpeed, turnAcceleration * maxTurnSpeed * deltaTime);

            //transform.Rotate(0f, runtimeData.turnSpeed * fixedDeltaTime, 0f);
            transform.Rotate(transform.up, runtimeData.turnSpeed * deltaTime, Space.World);
        }

        private void HandleMouseSteer(float deltaTime)
        {
            // Accumulate mouse input over time
            runtimeData.mouseInputX += Input.GetAxisRaw("Mouse X") * runtimeData.mouseSensitivity;

            // Clamp the accumulator to simulate key press behavior
            runtimeData.mouseInputX = Mathf.Clamp(runtimeData.mouseInputX, -1f, 1f);

            // Calculate target turn speed
            var targetTurnSpeed = runtimeData.mouseInputX * maxTurnSpeed;

            // Use the same acceleration logic as HandleTurning
            runtimeData.turnSpeed = Mathf.MoveTowards(runtimeData.turnSpeed, targetTurnSpeed, runtimeData.mouseSensitivity * maxTurnSpeed * deltaTime);

            // Apply rotation
            //transform.Rotate(0f, runtimeData.turnSpeed * fixedDeltaTime, 0f);
            transform.Rotate(transform.up, runtimeData.turnSpeed * deltaTime, Space.World);

            runtimeData.mouseInputX = Mathf.MoveTowards(runtimeData.mouseInputX, 0f, runtimeData.mouseSensitivity * deltaTime);
        }

        private void HandleJumping(float deltaTime)
        {
            if (runtimeData.groundState != GroundState.Falling && moveKeys.Jump != KeyCode.None && Input.GetKey(moveKeys.Jump))
            {
                if (runtimeData.groundState != GroundState.Jumping)
                {
                    runtimeData.groundState = GroundState.Jumping;
                    runtimeData.jumpSpeed = initialJumpSpeed;
                }
                else if (runtimeData.jumpSpeed < maxJumpSpeed)
                {
                    // Increase jumpSpeed using a square root function for a fast start and slow finish
                    var jumpProgress = (runtimeData.jumpSpeed - initialJumpSpeed) / (maxJumpSpeed - initialJumpSpeed);
                    runtimeData.jumpSpeed += jumpAcceleration * Mathf.Sqrt(1 - jumpProgress) * deltaTime;
                }

                if (runtimeData.jumpSpeed >= maxJumpSpeed)
                {
                    runtimeData.jumpSpeed = maxJumpSpeed;
                    runtimeData.groundState = GroundState.Falling;
                }
            }
            else if (runtimeData.groundState != GroundState.Grounded)
            {
                runtimeData.groundState = GroundState.Falling;
                runtimeData.jumpSpeed = Mathf.Min(runtimeData.jumpSpeed, maxJumpSpeed);
                runtimeData.jumpSpeed += Physics.gravity.y * deltaTime;
            }
            else
                // maintain small downward speed for when falling off ledges
            {
                runtimeData.jumpSpeed = Physics.gravity.y * deltaTime;
            }
        }

        private void HandleMove(float deltaTime)
        {
            // Initialize target movement variables
            var targetMoveX = 0f;
            var targetMoveZ = 0f;

            // Check for WASD key presses and adjust target movement variables accordingly
            if (moveKeys.Forward != KeyCode.None && Input.GetKey(moveKeys.Forward)) targetMoveZ = 1f;
            if (moveKeys.Back != KeyCode.None && Input.GetKey(moveKeys.Back)) targetMoveZ = -1f;
            if (moveKeys.StrafeLeft != KeyCode.None && Input.GetKey(moveKeys.StrafeLeft)) targetMoveX = -1f;
            if (moveKeys.StrafeRight != KeyCode.None && Input.GetKey(moveKeys.StrafeRight)) targetMoveX = 1f;

            if (targetMoveX == 0f)
            {
                if (!controlOptions.HasFlag(ControlOptions.AutoRun))
                    runtimeData.horizontal = Mathf.MoveTowards(runtimeData.horizontal, targetMoveX, inputGravity * deltaTime);
            }
            else
            {
                runtimeData.horizontal = Mathf.MoveTowards(runtimeData.horizontal, targetMoveX, inputSensitivity * deltaTime);
            }

            if (targetMoveZ == 0f)
            {
                if (!controlOptions.HasFlag(ControlOptions.AutoRun))
                    runtimeData.vertical = Mathf.MoveTowards(runtimeData.vertical, targetMoveZ, inputGravity * deltaTime);
            }
            else
            {
                runtimeData.vertical = Mathf.MoveTowards(runtimeData.vertical, targetMoveZ, inputSensitivity * deltaTime);
            }
        }

        private void ApplyMove(float fixedDeltaTime)
        {
            // Handle horizontal movement
            runtimeData.direction = new Vector3(runtimeData.horizontal, 0f, runtimeData.vertical);
            runtimeData.direction = Vector3.ClampMagnitude(runtimeData.direction, 1f);
            runtimeData.direction = transform.TransformDirection(runtimeData.direction);
            runtimeData.direction *= maxMoveSpeed;

            // Apply horizontal movement
            rigidBody.MovePosition(rigidBody.position + runtimeData.direction * fixedDeltaTime);

            // Handle vertical movement (jumping and gravity)
#if UNITY_6000_0_OR_NEWER
            Vector3 verticalMovement = rigidBody.linearVelocity;
#else
            var verticalMovement = rigidBody.velocity;
#endif
            verticalMovement.y = runtimeData.jumpSpeed;

            // Apply gravity
            if (runtimeData.groundState != GroundState.Grounded)
                verticalMovement.y += Physics.gravity.y * fixedDeltaTime;

            // Apply vertical movement
#if UNITY_6000_0_OR_NEWER
            rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, verticalMovement.y, rigidBody.linearVelocity.z);
#else
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, verticalMovement.y, rigidBody.velocity.z);
#endif
        }

        [Serializable]
        public struct MoveKeys
        {
            public KeyCode Forward;
            public KeyCode Back;
            public KeyCode StrafeLeft;
            public KeyCode StrafeRight;
            public KeyCode TurnLeft;
            public KeyCode TurnRight;
            public KeyCode Jump;
        }

        [Serializable]
        public struct OptionsKeys
        {
            public KeyCode MouseSteer;
            public KeyCode AutoRun;
            public KeyCode ToggleUI;
        }

        // Runtime data in a struct so it can be folded up in inspector
        [Serializable]
        public struct RuntimeData
        {
            [ReadOnly] [SerializeField] [Range(-1f, 1f)]
            private float _horizontal;

            [ReadOnly] [SerializeField] [Range(-1f, 1f)]
            private float _vertical;

            [ReadOnly] [SerializeField] [Range(-300f, 300f)]
            private float _turnSpeed;

            [ReadOnly] [SerializeField] [Range(-10f, 10f)]
            private float _jumpSpeed;

            [ReadOnly] [SerializeField] [Range(-1.5f, 1.5f)]
            private float _animVelocity;

            [ReadOnly] [SerializeField] [Range(-1.5f, 1.5f)]
            private float _animRotation;

            [ReadOnly] [SerializeField] [Range(-1f, 1f)]
            private float _mouseInputX;

            [ReadOnly] [SerializeField] [Range(0, 30f)]
            private float _mouseSensitivity;

            [ReadOnly] [SerializeField] private GroundState _groundState;
            [ReadOnly] [SerializeField] private Vector3 _direction;
            [ReadOnly] [SerializeField] private Vector3Int _velocity;
            [ReadOnly] [SerializeField] private GameObject _controllerUI;

            #region Properties

            public float horizontal
            {
                get => _horizontal;
                internal set => _horizontal = value;
            }

            public float vertical
            {
                get => _vertical;
                internal set => _vertical = value;
            }

            public float turnSpeed
            {
                get => _turnSpeed;
                internal set => _turnSpeed = value;
            }

            public float jumpSpeed
            {
                get => _jumpSpeed;
                internal set => _jumpSpeed = value;
            }

            public float animVelocity
            {
                get => _animVelocity;
                internal set => _animVelocity = value;
            }

            public float animRotation
            {
                get => _animRotation;
                internal set => _animRotation = value;
            }

            public float mouseInputX
            {
                get => _mouseInputX;
                internal set => _mouseInputX = value;
            }

            public float mouseSensitivity
            {
                get => _mouseSensitivity;
                internal set => _mouseSensitivity = value;
            }

            public GroundState groundState
            {
                get => _groundState;
                internal set => _groundState = value;
            }

            public Vector3 direction
            {
                get => _direction;
                internal set => _direction = value;
            }

            public Vector3Int velocity
            {
                get => _velocity;
                internal set => _velocity = value;
            }

            public GameObject controllerUI
            {
                get => _controllerUI;
                internal set => _controllerUI = value;
            }

            #endregion
        }

        #region Network Setup

        protected override void OnValidate()
        {
            // Skip if Editor is in Play mode
            if (Application.isPlaying) return;

            base.OnValidate();
            Reset();
        }

        public virtual void Reset()
        {
            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody>();
            if (capsuleCollider == null)
                capsuleCollider = GetComponent<CapsuleCollider>();

            // Configure Rigidbody
            rigidBody.useGravity = true;
            rigidBody.interpolation = RigidbodyInterpolation.None;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidBody.isKinematic = true;

            // Freeze rotation on X and Z axes, but allow rotation on Y axis
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

#if UNITY_EDITOR
            // For convenience in the examples, we use the GUID of the PlayerControllerUI
            // to find the correct prefab in the Mirror/Examples/_Common/Controllers folder.
            // This avoids conflicts with user-created prefabs that may have the same name
            // and avoids polluting the user's project with Resources.
            // This is not recommended for production code...use Resources.Load or AssetBundles instead.
            if (ControllerUIPrefab == null)
            {
                var path = AssetDatabase.GUIDToAssetPath("5caaf0d5754a64f4080f0c8b55c0b03d");
                ControllerUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
#endif

            enabled = false;
        }

        private void OnDisable()
        {
            runtimeData.horizontal = 0f;
            runtimeData.vertical = 0f;
            runtimeData.turnSpeed = 0f;
        }

        public override void OnStartAuthority()
        {
            // Calculate DPI-aware sensitivity
            var dpiScale = Screen.dpi > 0 ? Screen.dpi / BASE_DPI : 1f;
            runtimeData.mouseSensitivity = turnAcceleration * dpiScale;

            SetCursor(controlOptions.HasFlag(ControlOptions.MouseSteer));

            rigidBody.isKinematic = false;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            enabled = true;
        }

        public override void OnStopAuthority()
        {
            enabled = false;

            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidBody.isKinematic = true;

            SetCursor(false);
        }

        public override void OnStartLocalPlayer()
        {
            if (ControllerUIPrefab != null)
                runtimeData.controllerUI = Instantiate(ControllerUIPrefab);

            if (runtimeData.controllerUI != null)
            {
                if (runtimeData.controllerUI.TryGetComponent(out PlayerControllerRBUI canvasControlPanel))
                    canvasControlPanel.Refresh(moveKeys, optionsKeys);

                runtimeData.controllerUI.SetActive(controlOptions.HasFlag(ControlOptions.ShowUI));
            }
        }

        public override void OnStopLocalPlayer()
        {
            if (runtimeData.controllerUI != null)
                Destroy(runtimeData.controllerUI);

            runtimeData.controllerUI = null;
        }

        #endregion
    }
}