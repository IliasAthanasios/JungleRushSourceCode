using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    private bool isPaused = false;
    private InputAction pauseAction;

    void Awake()
    {
        pauseAction = InputSystem.actions?.FindAction("Pause");
    }

    void Start()
    {
        // Ensure the menu is hidden and the game is running when we start
        Resume();
    }

    void OnEnable()
    {
        if (pauseAction == null) pauseAction = InputSystem.actions?.FindAction("Pause");
        
        if (pauseAction != null)
        {
            pauseAction.performed += OnPauseAction;
            pauseAction.Enable();
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPauseAction;
        }
    }

    private void OnPauseAction(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        // If we are in the settings panel, ESC should probably take us back to the pause menu first
        if (settingsPanel.activeSelf)
        {
            BackToPauseMenu();
            return;
        }

        isPaused = !isPaused;
        if (isPaused) Pause();
        else Resume();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze time
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
        
        // Unlock cursor so we can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreeze time
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        // Relock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // Reset time!
        MainMenuController.showCreditsOnLoad = false;
        SceneManager.LoadScene("MainMenu");
    }
}