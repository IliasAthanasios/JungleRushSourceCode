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
        SetupResolutionDropdown();
        SetupQualityDropdown();

        StartCoroutine(ApplySavedSettings());

        if (showCreditsOnLoad)
        {
            showCreditsOnLoad = false; 
            ShowCredits();
        }
        else
        {
            showCreditsOnLoad = false; 
            if (mainPanel != null) ShowMainPanel();
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
    }

    private IEnumerator ApplySavedSettings()
    {
        yield return null;

        float savedMaster = PlayerPrefs.GetFloat(VOLUME_KEY, 0.75f);
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);

        if (volumeSlider) volumeSlider.value = savedMaster;
        if (musicSlider) musicSlider.value = savedMusic;
        if (sfxSlider) sfxSlider.value = savedSFX;

        SetVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        float savedSens = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 0.2f);
        
        SetQuality(savedQuality);
        if (sensitivitySlider) sensitivitySlider.value = savedSens;
        SetSensitivity(savedSens);

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
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    #region Navigation & Camera
    public void PlayGame()
    {
        GameManager.ResetGameData(3);
        
        StartCoroutine(LoadSceneWithLoadingScreen("TutorialLevel"));
    }

    private IEnumerator LoadSceneWithLoadingScreen(string sceneName)
    {
        if (loadingScreen != null) 
        {
            loadingScreen.FadeIn();
        }
        
        yield return new WaitForSeconds(0.5f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        operation.allowSceneActivation = false;

        float timer = 0f;
        float minLoadTime = 1.5f; 

        while (timer < minLoadTime || operation.progress < 0.9f)
        {
            timer += Time.deltaTime;
            
            float progress = Mathf.Clamp01(timer / minLoadTime);
            if (loadingScreen != null) loadingScreen.SetProgress(progress);
            
            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetProgress(1f);
        
        yield return new WaitForSeconds(0.3f);
        
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
