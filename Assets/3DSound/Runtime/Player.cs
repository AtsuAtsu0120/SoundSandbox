using UnityEngine;
using UnityEngine.InputSystem;

namespace AtsuSoundProject
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class Player : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask groundLayer = ~0;

        private Rigidbody rb;
        private InputSystem_Actions inputActions;
        private Camera mainCamera;

        private Vector2 moveInput;
        private bool isSprinting;
        private bool jumpRequested;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

            inputActions = new InputSystem_Actions();
            inputActions.Player.AddCallbacks(this);
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            inputActions.Player.RemoveCallbacks(this);
            inputActions.Dispose();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
        }

        private void Move()
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return;
            }

            var camForward = mainCamera.transform.forward;
            var camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            var direction = camForward * moveInput.y + camRight * moveInput.x;
            var speed = isSprinting ? sprintSpeed : moveSpeed;

            var targetVelocity = direction * speed;
            var velocity = rb.linearVelocity;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            rb.linearVelocity = velocity;

            var targetRotation = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        private void Jump()
        {
            if (!jumpRequested)
            {
                return;
            }

            jumpRequested = false;

            if (!IsGrounded())
            {
                return;
            }

            var velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }

        private bool IsGrounded()
        {
            var origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
            return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out _, groundCheckDistance, groundLayer);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpRequested = true;
            }
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
        }

        public void OnNext(InputAction.CallbackContext context)
        {
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            isSprinting = context.performed;
        }
    }
}
