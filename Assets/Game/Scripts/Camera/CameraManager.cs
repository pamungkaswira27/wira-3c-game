using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InputManager _inputManager;

    [Header("Camera States")]
    [SerializeField]
    private CameraState _cameraState;

    [Header("Virtual Cameras")]
    [SerializeField]
    private CinemachineFreeLook _thirdPersonCamera;
    [SerializeField]
    private CinemachineVirtualCamera _firstPersonCamera;

    public CameraState CameraState => _cameraState;

    private void Start()
    {
        _inputManager.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOV -= SwitchCamera;
    }

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        CinemachinePOV pov = _firstPersonCamera.GetCinemachineComponent<CinemachinePOV>();

        if (isClamped)
        {
            pov.m_HorizontalAxis.m_Wrap = false;
            pov.m_HorizontalAxis.m_MinValue = playerRotation.y - 45f;
            pov.m_HorizontalAxis.m_MaxValue = playerRotation.y + 45f;
        }
        else
        {
            pov.m_HorizontalAxis.m_Wrap = true;
            pov.m_HorizontalAxis.m_MinValue = -180f;
            pov.m_HorizontalAxis.m_MaxValue = 180f;
        }
    }

    public void SetTPSFieldOfView(float fieldOfView)
    {
        _thirdPersonCamera.m_Lens.FieldOfView = fieldOfView;
    }

    private void SwitchCamera()
    {
        if (_cameraState == CameraState.ThirdPerson)
        {
            _cameraState = CameraState.FirstPerson;
            _thirdPersonCamera.gameObject.SetActive(false);
            _firstPersonCamera.gameObject.SetActive(true);
        }
        else
        {
            _cameraState = CameraState.ThirdPerson;
            _thirdPersonCamera.gameObject.SetActive(true);
            _firstPersonCamera.gameObject.SetActive(false);
        }
    }
}
