using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class MainMenuController : MonoBehaviour
{
    public static bool showCreditsOnLoad = false;
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public LoadingScreenUI loadingScreen;
    public GameObject creditsPanel; 
    

    [Header("Display Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;

    [Header("Audio & Sensitivity UI")]
    public Slider volumeSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider sensitivitySlider;
    public AudioMixer masterMixer;

    [Header("Camera Transition")]
    public Transform cameraTransform;
    public Transform mainViewAnchor;
    public Transform settingsViewAnchor;
    public float transitionSpeed = 3f;

    private List<Resolution> filteredResolutions = new List<Resolution>();
    private Coroutine transitionCoroutine;

    // Keys for saving/loading settings
    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string VOLUME_KEY = "MasterVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";
    private const string QUALITY_KEY = "QualityLevel";
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string RES_WIDTH_KEY = "ResolutionWidth";
    private const string RES_HEIGHT_KEY = "ResolutionHeight";

    void Start()
    {
        // 1. Initialize Settings UI and Lists (defaults to Native monitor resolution)
        SetupResolutionDropdown();
        SetupQualityDropdown();

        // 2. Load and Apply Settings
        StartCoroutine(ApplySavedSettings());

        // 3. UI Initial State
        if (showCreditsOnLoad)
        {
            showCreditsOnLoad = false; // Reset immediately so it doesn't repeat
            ShowCredits();
        }
        else
        {
            showCreditsOnLoad = false; // Force reset just in case
            if (mainPanel != null) ShowMainPanel();
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
    }

    private IEnumerator ApplySavedSettings()
    {
        // Wait one frame to allow display subsystem to initialize
        yield return null;

        // --- Audio ---
        float savedMaster = PlayerPrefs.GetFloat(VOLUME_KEY, 0.75f);
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);

        if (volumeSlider) volumeSlider.value = savedMaster;
        if (musicSlider) musicSlider.value = savedMusic;
        if (sfxSlider) sfxSlider.value = savedSFX;

        SetVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        // --- Quality & Sensitivity ---
        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        float savedSens = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 0.2f);
        
        SetQuality(savedQuality);
        if (sensitivitySlider) sensitivitySlider.value = savedSens;
        SetSensitivity(savedSens);

        // --- Display & Resolution ---
        int nativeW = Screen.mainWindowDisplayInfo.width > 0 ? Screen.mainWindowDisplayInfo.width : Screen.currentResolution.width;
        int nativeH = Screen.mainWindowDisplayInfo.height > 0 ? Screen.mainWindowDisplayInfo.height : Screen.currentResolution.height;
        int savedW = PlayerPrefs.GetInt(RES_WIDTH_KEY, nativeW);
        int savedH = PlayerPrefs.GetInt(RES_HEIGHT_KEY, nativeH);
        bool isFS = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(isFS);
        }

        ApplyDisplayMode(savedW, savedH, isFS);

        // --- Input Rebinds ---
        string savedRebinds = PlayerPrefs.GetString("InputRebinds");
        if (!string.IsNullOrEmpty(savedRebinds))
        {
            InputSystem.actions.LoadBindingOverridesFromJson(savedRebinds);
        }
    }

    private void ApplyDisplayMode(int width, int height, bool fullscreen)
    {
            if (fullscreen)
        {
            // Force borderless fullscreen at the CURRENT desktop resolution.
            // Do NOT call SetResolution here - it conflicts and cancels the switch.
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            // Apply the chosen windowed resolution, then force windowed mode.
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    #region Navigation & Camera
    public void PlayGame()
    {
        // Reset game data before starting
        GameManager.ResetGameData(3);
        
        // Use the loading screen routine instead of direct SceneManager.LoadScene
        StartCoroutine(LoadSceneWithLoadingScreen("TutorialLevel"));
    }

    private IEnumerator LoadSceneWithLoadingScreen(string sceneName)
    {
        // 1. Show the loading UI
        if (loadingScreen != null) 
        {
            loadingScreen.FadeIn();
        }
        
        // Give the fade animation a moment to become fully opaque
        yield return new WaitForSeconds(0.5f);

        // 2. Start loading the scene in the background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        // Don't let the scene switch automatically until we are ready
        operation.allowSceneActivation = false;

        float timer = 0f;
        float minLoadTime = 1.5f; // Ensures the screen is visible for at least 1.5 seconds

        while (timer < minLoadTime || operation.progress < 0.9f)
        {
            timer += Time.deltaTime;
            
            // Calculate progress (0 to 1) based on the timer or the operation progress
            float progress = Mathf.Clamp01(timer / minLoadTime);
            if (loadingScreen != null) loadingScreen.SetProgress(progress);
            
            yield return null;
        }

        // 3. Finalize loading
        if (loadingScreen != null) loadingScreen.SetProgress(1f);
        
        // Short pause at 100% for visual polish
        yield return new WaitForSeconds(0.3f);
        
        // Actually switch to the new scene
        operation.allowSceneActivation = true;
    }

    public void ShowSettings()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        if (settingsViewAnchor != null) StartTransition(settingsViewAnchor);
    }

    public void ShowMainPanel()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (mainViewAnchor != null) StartTransition(mainViewAnchor);
    }

    public void ShowCredits()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(true);
        
        // Optional: If you want the camera to move to a specific spot for credits
        // if (creditsViewAnchor != null) StartTransition(creditsViewAnchor);
    }

    public void HideCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(false);
        ShowMainPanel();
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void StartTransition(Transform target)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(target));
    }

    private IEnumerator TransitionCamera(Transform target)
    {
        float t = 0;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            cameraTransform.position = Vector3.Lerp(startPos, target.position, t);
            cameraTransform.rotation = Quaternion.Lerp(startRot, target.rotation, t);
            
            var sway = cameraTransform.GetComponent<MenuCameraMotion>();
            if (sway != null) sway.UpdateBaseTransform(cameraTransform.position, cameraTransform.rotation);
            
            yield return null;
        }
    }
    #endregion

    #region Settings Logic
    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        Resolution[] allResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        filteredResolutions.Clear();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        int nativeW = Screen.mainWindowDisplayInfo.width > 0 ? Screen.mainWindowDisplayInfo.width : Screen.currentResolution.width;
        int nativeH = Screen.mainWindowDisplayInfo.height > 0 ? Screen.mainWindowDisplayInfo.height : Screen.currentResolution.height;
        int targetW = PlayerPrefs.GetInt(RES_WIDTH_KEY, nativeW);
        int targetH = PlayerPrefs.GetInt(RES_HEIGHT_KEY, nativeH);

        int[] popularWidths = { 1280, 1366, 1600, 1920, 2560, 3840 };
        
        for (int i = 0; i < allResolutions.Length; i++)
        {
            bool isPopular = false;
            foreach (int w in popularWidths)
            {
                if (allResolutions[i].width == w) isPopular = true;
            }

            if (allResolutions[i].width == nativeW && allResolutions[i].height == nativeH)
            {
                isPopular = true;
            }

            if (isPopular)
            {
                bool exists = false;
                for (int j = 0; j < filteredResolutions.Count; j++)
                {
                    if (filteredResolutions[j].width == allResolutions[i].width && 
                        filteredResolutions[j].height == allResolutions[i].height)
                    {
                        if (allResolutions[i].refreshRateRatio.value > filteredResolutions[j].refreshRateRatio.value)
                            filteredResolutions[j] = allResolutions[i];
                        exists = true;
                        break;
                    }
                }
                if (!exists) filteredResolutions.Add(allResolutions[i]);
            }
        }

        filteredResolutions.Sort((a, b) => a.width.CompareTo(b.width));

        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            string option = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(option);

            if (filteredResolutions[i].width == targetW && filteredResolutions[i].height == targetH)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];
        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;

        PlayerPrefs.SetInt(RES_WIDTH_KEY, res.width);
        PlayerPrefs.SetInt(RES_HEIGHT_KEY, res.height);
        PlayerPrefs.Save();

        // In borderless fullscreen the OS keeps desktop resolution, so only
        // apply a custom resolution when in windowed mode.
        if (isFullscreen)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else
            Screen.SetResolution(res.width, res.height, FullScreenMode.Windowed);

    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        if (isFullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            int nativeW = Screen.mainWindowDisplayInfo.width > 0 ? Screen.mainWindowDisplayInfo.width : Screen.currentResolution.width;
            int nativeH = Screen.mainWindowDisplayInfo.height > 0 ? Screen.mainWindowDisplayInfo.height : Screen.currentResolution.height;
            int w = PlayerPrefs.GetInt(RES_WIDTH_KEY, nativeW);
            int h = PlayerPrefs.GetInt(RES_HEIGHT_KEY, nativeH);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        // Confirms the method actually fired in the build - check Player.log
        Debug.Log($"[Settings] SetFullscreen({isFullscreen}) applied. Mode now requested: {Screen.fullScreenMode}");
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(QUALITY_KEY, index);
        if (qualityDropdown) qualityDropdown.value = index;
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, value);
        var allControllers = Object.FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
        foreach (var controller in allControllers)
        {
            for (int i = 0; i < controller.Controllers.Count; i++)
            {
                var axis = controller.Controllers[i];
                if (axis.Name.Contains("Look") || axis.Name.Contains("Orbit") || axis.Name.Contains("POV"))
                {
                    float direction = (axis.Input.Gain < 0) ? -15f : 15f;
                    axis.Input.Gain = value * direction;
                    controller.Controllers[i] = axis;
                }
            }
        }
    }

    public void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
        if (masterMixer != null)
        {
            float db = value > 0 ? Mathf.Log10(value) * 20 : -80;
            masterMixer.SetFloat("MasterVolume", db);
        }
        else AudioListener.volume = value;
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
        if (masterMixer != null)
        {
            float db = value > 0 ? Mathf.Log10(value) * 20 : -80;
            masterMixer.SetFloat("MusicVolume", db);
        }
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(SFX_KEY, value);
        if (masterMixer != null)
        {
            float db = value > 0 ? Mathf.Log10(value) * 20 : -80;
            masterMixer.SetFloat("SFXVolume", db);
        }
    }

    public void ResetAllBindings()
    {
        InputSystem.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("InputRebinds");
        PlayerPrefs.Save();
        
        var allRebindUIs = Object.FindObjectsByType<InputRebindUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rebindUI in allRebindUIs)
        {
            rebindUI.UpdateUI();
        }
        
        Debug.Log("All controls reset to default.");
    }
    #endregion
}