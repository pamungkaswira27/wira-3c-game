using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InputManager _inputManager;

    private void Start()
    {
        _inputManager.OnMainMenuInput += BackToMainMenu;
    }

    private void OnDestroy()
    {
        _inputManager.OnMainMenuInput -= BackToMainMenu;
    }

    private void BackToMainMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MainMenu");
    }
}
