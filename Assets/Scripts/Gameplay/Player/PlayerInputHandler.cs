using UnityEngine;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     Handles player input for FPS controls
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        
        // Current input state
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool FirePressed { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool CrouchHeld { get; private set; }

        private void Update()
        {
            if (!enabled) return;
            
            UpdateMovementInput();
            UpdateLookInput();
            UpdateActionInput();
        }

        private void UpdateMovementInput()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            MovementInput = new Vector2(horizontal, vertical).normalized;
        }

        private void UpdateLookInput()
        {
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            if (invertY)
                mouseY = -mouseY;
                
            LookInput = new Vector2(mouseX, mouseY);
        }

        private void UpdateActionInput()
        {
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            
            FirePressed = Input.GetMouseButtonDown(0);
            FireHeld = Input.GetMouseButton(0);
            
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            
            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            
            CrouchPressed = Input.GetKeyDown(KeyCode.LeftControl);
            CrouchHeld = Input.GetKey(KeyCode.LeftControl);
        }

        /// <summary>
        ///     Enable or disable input handling
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled)
            {
                // Clear all inputs when disabled
                MovementInput = Vector2.zero;
                LookInput = Vector2.zero;
                JumpPressed = false;
                FirePressed = false;
                FireHeld = false;
                ReloadPressed = false;
                SprintHeld = false;
                CrouchPressed = false;
                CrouchHeld = false;
            }
        }

        /// <summary>
        ///     Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        /// <summary>
        ///     Toggle Y-axis inversion
        /// </summary>
        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }
    }
} 