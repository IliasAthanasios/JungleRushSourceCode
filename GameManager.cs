using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public CanvasGroup gameOverScreen; 
    public LoadingScreenUI loadingScreen;

    [Header("Game State")]
    public int startingLives = 3;
    
    private static int score = 0;
    private static int currentLives = -1;
    private static bool isProtectedStatic = false; 

    [Header("Score Logic")]
    public int scoreToGainLife = 50;
    private static bool isCountingDown = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lifeGainSound;
    public AudioClip gameOverSound; 

    [Header("Protection System")]
    public GameObject shieldVisual;

    [Header("Level Transition")]
    public string nextSceneName = "Level1";

    [Header("Checkpoint Settings")]
    public float deathAnimDuration = 1.8f; 
    public float fadeOutDuration = 0.4f;  
    public float fadeInDuration = 0.4f;   
    public float gameOverFadeDuration = 0.8f; 
    private static Vector3 lastCheckpointPosition;
    private static bool hasCheckpoint = false;

    public bool IsGameOver => isGameOver;
    private bool isGameOver = false;
    private bool isDying = false;

    public bool isProtected 
    { 
        get => isProtectedStatic; 
        set => isProtectedStatic = value; 
    }

    void Awake()
    {
        Instance = this;
        if (currentLives == -1) currentLives = startingLives;
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        isProtected = isProtectedStatic;

        if (gameObject.GetComponent<CheckpointStateManager>() == null)
            gameObject.AddComponent<CheckpointStateManager>();
    }

    void Start()
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.SetAlpha(1f);
            StartCoroutine(InitialFadeIn());
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.alpha = 0f;
            gameOverScreen.interactable = false;
            gameOverScreen.blocksRaycasts = false;
            gameOverScreen.gameObject.SetActive(false);
        }

        if (loadingScreen != null) loadingScreen.gameObject.SetActive(false);

        var playerObj = GameObject.FindWithTag("Player");
        if (!hasCheckpoint && playerObj != null) SetCheckpoint(playerObj.transform.position);
        
        UpdateScoreUI();
        UpdateLivesUI();
        if (shieldVisual != null) shieldVisual.SetActive(isProtected);
    }

    private IEnumerator InitialFadeIn()
    {
        yield return new WaitForSeconds(0.2f); 
        yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));
    }

    public static int GetScore() => score;
    public static void SetScore(int val) { score = val; if(Instance != null) Instance.UpdateScoreUI(); }

    public void SetCheckpoint(Vector3 position)
    {
        lastCheckpointPosition = position;
        hasCheckpoint = true;
        if (CheckpointStateManager.Instance != null) CheckpointStateManager.Instance.CaptureSnapshot();
    }

    public void SetProtection(bool state)
    {
        isProtected = state;
        if (shieldVisual != null) shieldVisual.SetActive(state);
    }

    public void LoseLife()
    {
        if (isGameOver || isDying) return;
        
        if (isProtected)
        {
            SetProtection(false);
            return;
        }

        isDying = true;
        currentLives--;
        UpdateLivesUI();

        var player = Object.FindAnyObjectByType<CrashPlayerController>();
        if (player != null) player.Die();
        
        if (currentLives <= 0) TriggerGameOver();
        else StartCoroutine(DeathAndRespawnRoutine(player));
    }

    private IEnumerator DeathAndRespawnRoutine(CrashPlayerController player)
    {
        yield return new WaitForSeconds(deathAnimDuration);

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(fadeOutDuration));
        else
            yield return new WaitForSeconds(fadeOutDuration);


        if (CheckpointStateManager.Instance != null)
            CheckpointStateManager.Instance.RestoreSnapshot();

        if (player != null)
        {
            player.Teleport(lastCheckpointPosition);
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null) { anim.Rebind(); anim.Play("Idle 0", 0, 0); }
        }

        yield return new WaitForSeconds(0.3f);

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));

        if (player != null) player.enabled = true;
        isDying = false;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        hasCheckpoint = false;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(deathAnimDuration);
        Time.timeScale = 0f;

        var player = Object.FindAnyObjectByType<CrashPlayerController>();
        if (player != null)
        {
            foreach (var s in player.GetComponents<AudioSource>())
                if (s.loop) s.Stop();
        }

        if (audioSource != null && gameOverSound != null)
            audioSource.PlayOneShot(gameOverSound);

        if (gameOverScreen != null)
        {
            gameOverScreen.gameObject.SetActive(true);
            gameOverScreen.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < gameOverFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                gameOverScreen.alpha = Mathf.Clamp01(elapsed / gameOverFadeDuration);
                yield return null;
            }
            gameOverScreen.alpha = 1f;
            gameOverScreen.interactable = true;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        ResetGameData(startingLives);
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        MainMenuController.showCreditsOnLoad = false;
        ResetGameData(startingLives);
        StartCoroutine(LoadSceneRoutine("MainMenu"));
    }

    public void WinGame()
    {   
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(LoadSceneRoutine(nextSceneName));
    }
    public void FinishGame()
    {
    MainMenuController.showCreditsOnLoad = true;
    
    SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        hasCheckpoint = false;
        if (string.IsNullOrEmpty(sceneName)) sceneName = SceneManager.GetActiveScene().name;
        if (loadingScreen != null) loadingScreen.FadeIn();
        
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(0.5f));
        
        yield return new WaitForSeconds(0.8f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        float timer = 0f;
        while (timer < 2.0f || op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;
            if (loadingScreen != null) loadingScreen.SetProgress(Mathf.Clamp01(timer / 2.0f));
            yield return null;
        }
        
        if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlpha(1f);
        op.allowSceneActivation = true;
    }

    public void AddScore(int amount, Vector3? collectionPoint = null, AudioClip collectSound = null)
    {
        if (isGameOver || isCountingDown) return;
        score += amount;
        if (collectionPoint.HasValue && ScoreCollectorUI.Instance != null)
            ScoreCollectorUI.Instance.SpawnBananas(amount, collectionPoint.Value, collectSound);
        if (score >= scoreToGainLife) StartCoroutine(ScoreToLifeRoutine());
        UpdateScoreUI();
    }

    IEnumerator ScoreToLifeRoutine()
    {
        isCountingDown = true;
        currentLives++;
        UpdateLivesUI();
        if (audioSource != null && lifeGainSound != null) audioSource.PlayOneShot(lifeGainSound);
        while (score > 0)
        {
            score--;
            UpdateScoreUI();
            yield return new WaitForSeconds(0.02f);
        }
        isCountingDown = false;
    }

    void UpdateScoreUI() { if (scoreText != null) scoreText.text = score.ToString(); }
    void UpdateLivesUI() { if (livesText != null) livesText.text = currentLives.ToString(); }

    public static void ResetGameData(int defaultLives = 3)
    {
        score = 0;
        currentLives = defaultLives;
        isProtectedStatic = false;
        isCountingDown = false;
        hasCheckpoint = false;
    }
}
