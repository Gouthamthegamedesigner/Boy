using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private PlayerControls _inputActions;
    private Transform _model;
    private bool _isJumpPressed;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _model = transform;
        _inputActions = new PlayerControls();
        _inputActions.Gameplay.Enable();
        
        // Subscribe to jump action
        _inputActions.Gameplay.Jump.performed += ctx => _isJumpPressed = true;
        _inputActions.Gameplay.Jump.canceled += ctx => _isJumpPressed = false;
    }

    void Update()
    {
        // Enhanced ground check with raycast
        bool isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        // Read movement input
        _moveInput = _inputActions.Gameplay.Move.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        Vector3 moveVelocity = moveDirection * moveSpeed;

        // Apply movement
        _controller.Move(moveVelocity * Time.deltaTime);

        // Handle jump
        if (_isJumpPressed && isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _isJumpPressed = false; // Reset jump input
        }

        // Rotation
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _model.rotation = Quaternion.Slerp(
                _model.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    void OnEnable()
    {
        _inputActions.Gameplay.Enable();
    }

    void OnDisable()
    {
        _inputActions.Gameplay.Disable();
        // Unsubscribe from jump action
        _inputActions.Gameplay.Jump.performed -= ctx => _isJumpPressed = true;
        _inputActions.Gameplay.Jump.canceled -= ctx => _isJumpPressed = false;
    }
}