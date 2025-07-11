using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirror.Examples.Common.Controllers.Flyer
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkIdentity))]
    [DisallowMultipleComponent]
    public class FlyerControllerBase : NetworkBehaviour
    {
        [Flags]
        public enum ControlOptions : byte
        {
            None,
            MouseSteer = 1 << 0,
            AutoRun = 1 << 1,
            AutoLevel = 1 << 2,
            ShowUI = 1 << 3
        }

        public enum GroundState : byte
        {
            Grounded,
            Jumping,
            Falling
        }

        private const float BASE_DPI = 96f;

        [Header("Avatar Components")] public CapsuleCollider capsuleCollider;

        public CharacterController characterController;

        [Header("User Interface")] public GameObject ControllerUIPrefab;

        [Header("Configuration")] [SerializeField]
        public MoveKeys moveKeys = new()
        {
            Forward = KeyCode.W,
            Back = KeyCode.S,
            StrafeLeft = KeyCode.A,
            StrafeRight = KeyCode.D,
            TurnLeft = KeyCode.Q,
            TurnRight = KeyCode.E
        };

        [SerializeField] public FlightKeys flightKeys = new()
        {
            PitchDown = KeyCode.UpArrow,
            PitchUp = KeyCode.DownArrow,
            RollLeft = KeyCode.LeftArrow,
            RollRight = KeyCode.RightArrow,
            AutoLevel = KeyCode.L
        };

        [SerializeField] public OptionsKeys optionsKeys = new()
        {
            MouseSteer = KeyCode.M,
            AutoRun = KeyCode.R,
            ToggleUI = KeyCode.U
        };

        [Space(5)] public ControlOptions controlOptions = ControlOptions.AutoLevel | ControlOptions.ShowUI;

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

        [Header("Pitch")] [Range(0, 180f)] [Tooltip("Max Pitch in degrees per second")]
        public float maxPitchSpeed = 30f;

        [Range(0, 180f)] [Tooltip("Max Pitch in degrees")]
        public float maxPitchUpAngle = 20f;

        [Range(0, 180f)] [Tooltip("Max Pitch in degrees")]
        public float maxPitchDownAngle = 45f;

        [Range(0, 10f)] [Tooltip("Pitch acceleration in degrees per second squared")]
        public float pitchAcceleration = 3f;

        [Header("Roll")] [Range(0, 180f)] [Tooltip("Max Roll in degrees per second")]
        public float maxRollSpeed = 30f;

        [Range(0, 180f)] [Tooltip("Max Roll in degrees")]
        public float maxRollAngle = 45f;

        [Range(0, 10f)] [Tooltip("Roll acceleration in degrees per second squared")]
        public float rollAcceleration = 3f;

        [Header("Diagnostics")] public RuntimeData runtimeData;

        private void Update()
        {
            if (!Application.isFocused)
                return;

            if (!characterController.enabled)
                return;

            var deltaTime = Time.deltaTime;

            HandleOptions();

            if (controlOptions.HasFlag(ControlOptions.MouseSteer))
                HandleMouseSteer(deltaTime);
            else
                HandleTurning(deltaTime);

            HandlePitch(deltaTime);
            HandleRoll(deltaTime);
            HandleMove(deltaTime);
            ApplyMove(deltaTime);

            // Reset ground state
            if (characterController.isGrounded)
                runtimeData.groundState = GroundState.Grounded;
            else if (runtimeData.groundState != GroundState.Jumping)
                runtimeData.groundState = GroundState.Falling;

            // Diagnostic velocity...FloorToInt for display purposes
            runtimeData.velocity = Vector3Int.FloorToInt(characterController.velocity);
        }

        private void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
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

            if (flightKeys.AutoLevel != KeyCode.None && Input.GetKeyUp(flightKeys.AutoLevel))
                controlOptions ^= ControlOptions.AutoLevel;
        }

        // Turning works while airborne...feature?
        private void HandleTurning(float deltaTime)
        {
            var targetTurnSpeed = 0f;

            // Q and E cancel each other out, reducing targetTurnSpeed to zero.
            if (moveKeys.TurnLeft != KeyCode.None && Input.GetKey(moveKeys.TurnLeft))
                targetTurnSpeed -= maxTurnSpeed;
            if (moveKeys.TurnRight != KeyCode.None && Input.GetKey(moveKeys.TurnRight))
                targetTurnSpeed += maxTurnSpeed;

            runtimeData.turnSpeed = Mathf.MoveTowards(runtimeData.turnSpeed, targetTurnSpeed, turnAcceleration * maxTurnSpeed * deltaTime);
            transform.Rotate(0f, runtimeData.turnSpeed * deltaTime, 0f);
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
            transform.Rotate(0f, runtimeData.turnSpeed * deltaTime, 0f);

            runtimeData.mouseInputX = Mathf.MoveTowards(runtimeData.mouseInputX, 0f, runtimeData.mouseSensitivity * deltaTime);
        }

        private void HandlePitch(float deltaTime)
        {
            var targetPitchSpeed = 0f;
            var inputDetected = false;

            // Up and Down arrows for pitch
            if (flightKeys.PitchUp != KeyCode.None && Input.GetKey(flightKeys.PitchUp))
            {
                targetPitchSpeed -= maxPitchSpeed;
                inputDetected = true;
            }

            if (flightKeys.PitchDown != KeyCode.None && Input.GetKey(flightKeys.PitchDown))
            {
                targetPitchSpeed += maxPitchSpeed;
                inputDetected = true;
            }

            runtimeData.pitchSpeed = Mathf.MoveTowards(runtimeData.pitchSpeed, targetPitchSpeed, pitchAcceleration * maxPitchSpeed * deltaTime);

            // Apply pitch rotation
            runtimeData.pitchAngle += runtimeData.pitchSpeed * deltaTime;
            runtimeData.pitchAngle = Mathf.Clamp(runtimeData.pitchAngle, -maxPitchUpAngle, maxPitchDownAngle);

            // Return to zero when no input
            if (!inputDetected && controlOptions.HasFlag(ControlOptions.AutoLevel))
                runtimeData.pitchAngle = Mathf.MoveTowards(runtimeData.pitchAngle, 0f, maxPitchSpeed * deltaTime);

            ApplyRotation();
        }

        private void HandleRoll(float deltaTime)
        {
            var targetRollSpeed = 0f;
            var inputDetected = false;

            // Left and Right arrows for roll
            if (flightKeys.RollRight != KeyCode.None && Input.GetKey(flightKeys.RollRight))
            {
                targetRollSpeed -= maxRollSpeed;
                inputDetected = true;
            }

            if (flightKeys.RollLeft != KeyCode.None && Input.GetKey(flightKeys.RollLeft))
            {
                targetRollSpeed += maxRollSpeed;
                inputDetected = true;
            }

            runtimeData.rollSpeed = Mathf.MoveTowards(runtimeData.rollSpeed, targetRollSpeed, rollAcceleration * maxRollSpeed * deltaTime);

            // Apply roll rotation
            runtimeData.rollAngle += runtimeData.rollSpeed * deltaTime;
            runtimeData.rollAngle = Mathf.Clamp(runtimeData.rollAngle, -maxRollAngle, maxRollAngle);

            // Return to zero when no input
            if (!inputDetected && controlOptions.HasFlag(ControlOptions.AutoLevel))
                runtimeData.rollAngle = Mathf.MoveTowards(runtimeData.rollAngle, 0f, maxRollSpeed * deltaTime);

            ApplyRotation();
        }

        private void ApplyRotation()
        {
            // Get the current yaw (Y-axis rotation)
            var currentYaw = transform.localRotation.eulerAngles.y;

            // Apply all rotations
            transform.localRotation = Quaternion.Euler(runtimeData.pitchAngle, currentYaw, runtimeData.rollAngle);
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

        private void ApplyMove(float deltaTime)
        {
            // Create initial direction vector without jumpSpeed (y-axis).
            runtimeData.direction = new Vector3(runtimeData.horizontal, 0f, runtimeData.vertical);

            // Clamp so diagonal strafing isn't a speed advantage.
            runtimeData.direction = Vector3.ClampMagnitude(runtimeData.direction, 1f);

            // Transforms direction from local space to world space.
            runtimeData.direction = transform.TransformDirection(runtimeData.direction);

            // Multiply for desired ground speed.
            runtimeData.direction *= maxMoveSpeed;

            // Finally move the character.
            characterController.Move(runtimeData.direction * deltaTime);
        }

        [Serializable]
        public struct OptionsKeys
        {
            public KeyCode MouseSteer;
            public KeyCode AutoRun;
            public KeyCode ToggleUI;
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
        }

        [Serializable]
        public struct FlightKeys
        {
            public KeyCode PitchDown;
            public KeyCode PitchUp;
            public KeyCode RollLeft;
            public KeyCode RollRight;
            public KeyCode AutoLevel;
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

            [ReadOnly] [SerializeField] [Range(-180f, 180f)]
            private float _pitchAngle;

            [ReadOnly] [SerializeField] [Range(-180f, 180f)]
            private float _pitchSpeed;

            [ReadOnly] [SerializeField] [Range(-180f, 180f)]
            private float _rollAngle;

            [ReadOnly] [SerializeField] [Range(-180f, 180f)]
            private float _rollSpeed;

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

            public float pitchAngle
            {
                get => _pitchAngle;
                internal set => _pitchAngle = value;
            }

            public float pitchSpeed
            {
                get => _pitchSpeed;
                internal set => _pitchSpeed = value;
            }

            public float rollAngle
            {
                get => _rollAngle;
                internal set => _rollAngle = value;
            }

            public float rollSpeed
            {
                get => _rollSpeed;
                internal set => _rollSpeed = value;
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

        private void Reset()
        {
            if (capsuleCollider == null)
                capsuleCollider = GetComponent<CapsuleCollider>();

            // Enable by default...it will be disabled when characterController is enabled
            capsuleCollider.enabled = true;

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            // Override CharacterController default values
            characterController.enabled = false;
            characterController.skinWidth = 0.02f;
            characterController.minMoveDistance = 0f;

            GetComponent<Rigidbody>().isKinematic = true;

#if UNITY_EDITOR
            // For convenience in the examples, we use the GUID of the FlyerControllerUI
            // to find the correct prefab in the Mirror/Examples/_Common/Controllers folder.
            // This avoids conflicts with user-created prefabs that may have the same name
            // and avoids polluting the user's project with Resources.
            // This is not recommended for production code...use Resources.Load or AssetBundles instead.
            if (ControllerUIPrefab == null)
            {
                var path = AssetDatabase.GUIDToAssetPath("493615025d304c144bacfb91f6aac90e");
                ControllerUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
#endif

            enabled = false;
        }

        public override void OnStartAuthority()
        {
            // Calculate DPI-aware sensitivity
            var dpiScale = Screen.dpi > 0 ? Screen.dpi / BASE_DPI : 1f;
            runtimeData.mouseSensitivity = turnAcceleration * dpiScale;

            SetCursor(controlOptions.HasFlag(ControlOptions.MouseSteer));

            // capsuleCollider and characterController are mutually exclusive
            // Having both enabled would double fire triggers and other collisions
            capsuleCollider.enabled = false;
            characterController.enabled = true;
            enabled = true;
        }

        public override void OnStopAuthority()
        {
            enabled = false;

            // capsuleCollider and characterController are mutually exclusive
            // Having both enabled would double fire triggers and other collisions
            capsuleCollider.enabled = true;
            characterController.enabled = false;

            SetCursor(false);
        }

        public override void OnStartLocalPlayer()
        {
            if (ControllerUIPrefab != null)
                runtimeData.controllerUI = Instantiate(ControllerUIPrefab);

            if (runtimeData.controllerUI != null)
            {
                if (runtimeData.controllerUI.TryGetComponent(out FlyerControllerUI canvasControlPanel))
                    canvasControlPanel.Refresh(moveKeys, flightKeys, optionsKeys);

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