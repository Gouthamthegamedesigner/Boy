using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))] // New requirement
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    // Animation Parameters
    [Header("Animation")]
    public float animationBlendSpeed = 5f;
    
    private CharacterController _controller;
    private Animator _animator; // New reference
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private PlayerControls _inputActions;
    private Transform _model;
    private bool _isJumpPressed;
    private float _speedPercent; // For blending walk/run animations
    private bool _isGrounded;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>(); // Initialize animator
        _model = transform;
        _inputActions = new PlayerControls();
        _inputActions.Gameplay.Enable();
        
        _inputActions.Gameplay.Jump.performed += ctx => _isJumpPressed = true;
    }

    void Update()
    {
        // Enhanced ground check with raycast
        _isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        // Read movement input
        _moveInput = _inputActions.Gameplay.Move.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        Vector3 moveVelocity = moveDirection * moveSpeed;

        // Calculate speed percentage for animation blending
        float targetSpeedPercent = _moveInput.magnitude;
        _speedPercent = Mathf.Lerp(_speedPercent, targetSpeedPercent, animationBlendSpeed * Time.deltaTime);

        // Apply movement
        _controller.Move(moveVelocity * Time.deltaTime);

        // Handle jump
        if (_isJumpPressed && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _animator.SetTrigger("Jump"); // Trigger jump animation
            _isJumpPressed = false;
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

        // Update animator parameters
        _animator.SetFloat("Speed", _speedPercent);
        _animator.SetBool("IsGrounded", _isGrounded);
    }

    void OnEnable() => _inputActions.Gameplay.Enable();
    void OnDisable() => _inputActions.Gameplay.Disable();
}