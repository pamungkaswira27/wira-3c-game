using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InputManager _inputManager;
    [SerializeField]
    private CameraManager _cameraManager;

    [Header("Walk Movement")]
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private Transform _cameraTransform;

    [Header("Sprint Movement")]
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkToSprintTransition;

    [Header("Crouch Movement")]
    [SerializeField]
    private float _crouchSpeed;

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

    [Header("Glide Movement")]
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _minGlideRotationX;
    [SerializeField]
    private float _maxGlideRotationX;

    [Header("Combat")]
    [SerializeField]
    private float _resetComboInterval;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _destroyableLayerMask;

    [Header("Rotation Movement")]
    [SerializeField]
    private float _rotationSmoothTime;

    private Animator _animator;
    private CapsuleCollider _capsuleCollider;
    private Coroutine _resetComboCoroutine;
    private Rigidbody _rigidbody;

    private PlayerStance _playerStance;

    private float _rotationSmoothVelocity;
    private float _movementSpeed;
    private bool _isGrounded;
    private bool _isPunching;
    private int _comboIndex;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        HideAndLockCursor();

        _inputManager.OnMoveInput += Move;
        _inputManager.OnSprintInput += Sprint;
        _inputManager.OnJumpInput += Jump;
        _inputManager.OnClimbInput += StartClimb;
        _inputManager.OnCancelClimb += CancelClimb;
        _inputManager.OnCrouchInput += Crouch;
        _inputManager.OnGlideInput += StartGlide;
        _inputManager.OnCancelGlide += CancelGlide;
        _inputManager.OnPunchInput += Punch;
        _cameraManager.OnChangePerspective += ChangePerspective;

        _movementSpeed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
    }

    private void FixedUpdate()
    {
        CheckIsGrounded();
        CheckStep();
        Glide();
    }

    private void OnDestroy()
    {
        _inputManager.OnMoveInput -= Move;
        _inputManager.OnSprintInput -= Sprint;
        _inputManager.OnJumpInput -= Jump;
        _inputManager.OnClimbInput -= StartClimb;
        _inputManager.OnCancelClimb -= CancelClimb;
        _inputManager.OnCrouchInput -= Crouch;
        _inputManager.OnGlideInput -= StartGlide;
        _inputManager.OnCancelGlide -= CancelGlide;
        _inputManager.OnPunchInput -= Punch;
        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

    private void HideAndLockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Move(Vector2 axisDirection)
    {
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouch = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;

        Vector3 movementDirection;
        Vector3 velocity;

        if ((isPlayerStanding || isPlayerCrouch) && !_isPunching)
        {
            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1f)
                    {
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(_movementSpeed * Time.deltaTime * movementDirection);
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    movementDirection = horizontalDirection + verticalDirection;
                    _rigidbody.AddForce(_movementSpeed * Time.deltaTime * movementDirection);
                    break;
            }

            velocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            _animator.SetFloat("velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("velocityX", velocity.magnitude * axisDirection.x);
            _animator.SetFloat("velocityZ", velocity.magnitude * axisDirection.y);
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(_movementSpeed * Time.deltaTime * movementDirection);

            velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y, 0f);
            _animator.SetFloat("climbVelocityX", velocity.magnitude * axisDirection.x);
            _animator.SetFloat("climbVelocityY", velocity.magnitude * axisDirection.y);
        }
        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;

            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance != PlayerStance.Stand)
        {
            return;
        }

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
        _animator.SetTrigger("jump");
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayerMask);
        _animator.SetBool("isGrounded", _isGrounded);

        if (_isGrounded)
        {
            CancelGlide();
        }
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
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(70f);
            _animator.SetBool("isClimbing", true);
            _capsuleCollider.center = Vector3.up * 1.3f;
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
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(40f);
            _animator.SetBool("isClimbing", false);
            _capsuleCollider.center = Vector3.up * 0.9f;
        }
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("changePerspective");
    }

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("isCrouch", true);
            _movementSpeed = _crouchSpeed;
            _capsuleCollider.height = 1.3f;
            _capsuleCollider.center = Vector3.up * 0.66f;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("isCrouch", false);
            _movementSpeed = _walkSpeed;
            _capsuleCollider.height = 1.8f;
            _capsuleCollider.center = Vector3.up * 0.9f;
        }
    }

    private void StartGlide()
    {
        if (_playerStance != PlayerStance.Glide && !_isGrounded)
        {
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("isGliding", true);
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        }
    }

    private void Glide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(Time.deltaTime * totalForce);
        }
    }

    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("isGliding", false);
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    private void Punch()
    {
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;

            if (_comboIndex < 3)
            {
                _comboIndex++;
            }
            else
            {
                _comboIndex = 1;
            }

            _animator.SetInteger("combo", _comboIndex);
            _animator.SetTrigger("punch");
        }
    }

    private void EndPunch()
    {
        _isPunching = false;

        if (_resetComboCoroutine != null)
        {
            StopCoroutine(_resetComboCoroutine);
        }

        _resetComboCoroutine = StartCoroutine(ResetCombo_Coroutine());
    }

    private IEnumerator ResetCombo_Coroutine()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _comboIndex = 0;
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _destroyableLayerMask);

        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i] != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }
}
