using UnityEngine;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     FPS movement system with typical first-person shooter mechanics
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSMovementSystem : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float crouchSpeed = 3f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundMask = 1;
        
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;
        
        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _currentMovement;
        private float _currentRotationX;
        private bool _isGrounded;
        private bool _isCrouching;
        
        // Input reference
        private PlayerInputHandler _inputHandler;

        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsMoving => _currentMovement.magnitude > 0.1f;
        public float CurrentSpeed => _currentMovement.magnitude;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _inputHandler = GetComponent<PlayerInputHandler>();
            
            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;
                
            // Lock cursor for FPS controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_inputHandler == null || !enabled) return;
            
            CheckGrounded();
            HandleLook();
            HandleMovement();
            HandleJump();
            HandleCrouch();
            ApplyMovement();
        }

        private void CheckGrounded()
        {
            // Check if grounded using a sphere cast slightly below the controller
            var groundCheckPosition = transform.position - Vector3.up * (_controller.height * 0.5f);
            _isGrounded = Physics.CheckSphere(groundCheckPosition, groundCheckDistance, groundMask);
            
            // Reset vertical velocity if grounded
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small negative value to keep grounded
            }
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;
            
            var lookInput = _inputHandler.LookInput;
            
            // Horizontal rotation (Y-axis)
            transform.Rotate(0, lookInput.x, 0);
            
            // Vertical rotation (X-axis) - applied to camera
            _currentRotationX -= lookInput.y;
            _currentRotationX = Mathf.Clamp(_currentRotationX, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(_currentRotationX, 0, 0);
        }

        private void HandleMovement()
        {
            var movementInput = _inputHandler.MovementInput;
            
            // Calculate target movement direction relative to player rotation
            var moveDirection = transform.right * movementInput.x + transform.forward * movementInput.y;
            
            // Determine target speed based on input states
            var targetSpeed = GetTargetSpeed();
            
            // Apply target movement
            var targetMovement = moveDirection * targetSpeed;
            
            // Smoothly interpolate to target movement
            var lerpSpeed = targetMovement.magnitude > _currentMovement.magnitude ? acceleration : deceleration;
            _currentMovement = Vector3.Lerp(_currentMovement, targetMovement, lerpSpeed * Time.deltaTime);
        }

        private float GetTargetSpeed()
        {
            if (_inputHandler.MovementInput.magnitude < 0.1f)
                return 0f;
                
            if (_isCrouching)
                return crouchSpeed;
                
            if (_inputHandler.SprintHeld)
                return sprintSpeed;
                
            return walkSpeed;
        }

        private void HandleJump()
        {
            if (_inputHandler.JumpPressed && _isGrounded && !_isCrouching)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void HandleCrouch()
        {
            if (_inputHandler.CrouchPressed)
            {
                _isCrouching = !_isCrouching;
            }
            
            // Adjust controller height for crouching
            var targetHeight = _isCrouching ? 1f : 2f;
            _controller.height = Mathf.Lerp(_controller.height, targetHeight, 10f * Time.deltaTime);
            
            // Adjust camera position
            if (cameraTransform != null)
            {
                var targetCameraY = _isCrouching ? 0.5f : 1.5f;
                var currentPos = cameraTransform.localPosition;
                currentPos.y = Mathf.Lerp(currentPos.y, targetCameraY, 10f * Time.deltaTime);
                cameraTransform.localPosition = currentPos;
            }
        }

        private void ApplyMovement()
        {
            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;
            
            // Combine horizontal movement with vertical velocity
            var finalMovement = _currentMovement + Vector3.up * _velocity.y;
            
            // Move the character controller
            _controller.Move(finalMovement * Time.deltaTime);
        }

        /// <summary>
        ///     Set movement enabled state
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled)
            {
                _currentMovement = Vector3.zero;
                _velocity = Vector3.zero;
            }
        }

        /// <summary>
        ///     Teleport player to position (useful for respawning)
        /// </summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            _velocity = Vector3.zero;
            _currentMovement = Vector3.zero;
            _currentRotationX = 0f;
            
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.identity;
            }
            
            _controller.enabled = true;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw ground check sphere
            var groundCheckPosition = transform.position - Vector3.up * (_controller?.height * 0.5f ?? 1f);
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPosition, groundCheckDistance);
        }
    }
} 