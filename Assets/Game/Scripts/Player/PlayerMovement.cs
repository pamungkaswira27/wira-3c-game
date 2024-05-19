using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InputManager _inputManager;

    [Header("Walk Movement")]
    [SerializeField]
    private float _walkSpeed;

    [Header("Sprint Movement")]
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkToSprintTransition;

    [Header("Jump Movement")]
    [SerializeField]
    private float _jumpForce;

    [Header("Ground Detector")]
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private LayerMask _groundLayerMask;

    [Header("Step Movement")]
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;

    [Header("Climb Movement")]
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbDetectorDistance;
    [SerializeField]
    private LayerMask _climbableLayerMask;
    [SerializeField]
    private Vector3 _climbOffset;
    [SerializeField]
    private float _climbSpeed;

    [Header("Rotation Movement")]
    [SerializeField]
    private float _rotationSmoothTime;

    private Rigidbody _rigidbody;

    private PlayerStance _playerStance;

    private float _rotationSmoothVelocity;
    private float _movementSpeed;
    private bool _isGrounded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _inputManager.OnMoveInput += Move;
        _inputManager.OnSprintInput += Sprint;
        _inputManager.OnJumpInput += Jump;
        _inputManager.OnClimbInput += StartClimb;
        _inputManager.OnCancelClimb += CancelClimb;

        _movementSpeed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
    }

    private void FixedUpdate()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void OnDestroy()
    {
        _inputManager.OnMoveInput -= Move;
        _inputManager.OnSprintInput -= Sprint;
        _inputManager.OnJumpInput -= Jump;
        _inputManager.OnClimbInput -= StartClimb;
        _inputManager.OnCancelClimb -= CancelClimb;
    }

    private void Move(Vector2 axisDirection)
    {
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;

        Vector3 movementDirection;

        if (isPlayerStanding)
        {
            if (axisDirection.magnitude >= 0.1f)
            {
                float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                _rigidbody.AddForce(_movementSpeed * Time.deltaTime * movementDirection);
            }
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(_movementSpeed * Time.deltaTime * movementDirection);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (_movementSpeed < _sprintSpeed)
            {
                _movementSpeed += _walkToSprintTransition * Time.deltaTime;
            }
        }
        else
        {
            if (_movementSpeed > _walkSpeed)
            {
                _movementSpeed -= _walkToSprintTransition * Time.deltaTime;
            }
        }
    }

    private void Jump()
    {
        if (!_isGrounded)
        {
            return;
        }

        Vector3 jumpDirection = Vector3.up;
        _rigidbody.AddForce(_jumpForce * Time.deltaTime * jumpDirection);
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayerMask);
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position, transform.forward, _stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset, transform.forward, _stepCheckerDistance);

        if (isHitLowerStep && !isHitUpperStep)
        {
            _rigidbody.AddForce(0f, _stepForce * Time.deltaTime, 0f);
        }
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit raycastHit, _climbDetectorDistance, _climbableLayerMask);
        bool isNotClimbing = _playerStance != PlayerStance.Climb;

        if (_isGrounded && isNotClimbing && isInFrontOfClimbingWall)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = raycastHit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
            _movementSpeed = _climbSpeed;
        }
    }

    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _playerStance = PlayerStance.Stand;
            _rigidbody.useGravity = true;
            _movementSpeed = _walkSpeed;
            transform.position -= transform.forward;
        }
    }
}
