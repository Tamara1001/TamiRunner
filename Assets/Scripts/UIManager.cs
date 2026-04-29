using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Panels (CanvasGroups)")]
    [SerializeField] private CanvasGroup startMenuPanel;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private CanvasGroup gameOverPanel;

    [Header("Start Menu Elements")]
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private Button playButton;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("Game Over Elements")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalTimeText;
    [SerializeField] private Button restartButton;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine multiplierPulseCoroutine;
    private bool isGameOverTriggered = false;

    private void Start()
    {
        // 1. Initialize Panels
        SetPanelAlpha(startMenuPanel, 1f, true);
        SetPanelAlpha(hudPanel, 0f, false);
        SetPanelAlpha(gameOverPanel, 0f, false);

        // 2. Load and Display PlayerPrefs
        float bestScore = PlayerPrefs.GetFloat("HighScore", 0f);
        float bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        
        if (bestScoreText != null) bestScoreText.text = $"Puntaje más alto: {Mathf.FloorToInt(bestScore)}";
        if (bestTimeText != null) bestTimeText.text = $"Mejor tiempo: {FormatTime(bestTime)}";

        // 3. Hide Multiplier Text Initially
        if (multiplierText != null) multiplierText.gameObject.SetActive(false);

        // 4. Hook up button events
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // HUD Updates
        if (GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            UpdateHUD();
            HandleMultiplierVFX();
        }
        else if (GameManager.Instance.currentState == GameManager.GameState.GameOver && !isGameOverTriggered)
        {
            isGameOverTriggered = true;
            ShowGameOver();
        }
    }

    // --- BUTTON ACTIONS ---

    private void OnPlayClicked()
    {
        GameManager.Instance.StartGame();
        
        StartCoroutine(FadePanel(startMenuPanel, 0f, false));
        StartCoroutine(FadePanel(hudPanel, 1f, true));
    }

    private void OnRestartClicked()
    {
        // Reloads the currently active scene to reset the entire world seamlessly
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- HUD UPDATES ---

    private void UpdateHUD()
    {
        if (scoreText != null) scoreText.text = $"Puntaje: {Mathf.FloorToInt(GameManager.Instance.score)}";
        if (livesText != null) livesText.text = $"Vidas: {GameManager.Instance.currentLives}";
        if (timeText != null) timeText.text = $"Tiempo: {FormatTime(GameManager.Instance.currentTime)}";
    }

    private void HandleMultiplierVFX()
    {
        if (multiplierText == null) return;

        // If multiplier is active but the text is hidden, show it and start pulsing
        if (GameManager.Instance.currentScoreMultiplier > 1f && !multiplierText.gameObject.activeSelf)
        {
            multiplierText.gameObject.SetActive(true);
            if (multiplierPulseCoroutine != null) StopCoroutine(multiplierPulseCoroutine);
            multiplierPulseCoroutine = StartCoroutine(PulseTextLoop(multiplierText.transform));
        }
        // If multiplier wears off, shrink it away safely
        else if (GameManager.Instance.currentScoreMultiplier <= 1f && multiplierText.gameObject.activeSelf)
        {
            if (multiplierPulseCoroutine != null) StopCoroutine(multiplierPulseCoroutine);
            StartCoroutine(ShrinkTextAway(multiplierText.transform));
        }
    }

    // --- GAME OVER ---

    private void ShowGameOver()
    {
        StartCoroutine(FadePanel(hudPanel, 0f, false));
        StartCoroutine(FadePanel(gameOverPanel, 1f, true));

        if (finalScoreText != null) finalScoreText.text = $"Puntaje Final: {Mathf.FloorToInt(GameManager.Instance.score)}";
        if (finalTimeText != null) finalTimeText.text = $"Tiempo Final: {FormatTime(GameManager.Instance.currentTime)}";
    }

    // --- UTILITIES & COROUTINES ---

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void SetPanelAlpha(CanvasGroup panel, float alpha, bool interactable)
    {
        if (panel == null) return;

        // If we are making it visible, activate the GameObject first
        if (alpha > 0f) panel.gameObject.SetActive(true);

        panel.alpha = alpha;
        panel.interactable = interactable;
        panel.blocksRaycasts = interactable;

        // If we are hiding it completely, disable the GameObject
        if (alpha <= 0f && !interactable) panel.gameObject.SetActive(false);
    }

    private IEnumerator FadePanel(CanvasGroup panel, float targetAlpha, bool makeInteractable)
    {
        if (panel == null) yield break;

        // If we are fading IN, we MUST activate the GameObject first so the coroutine and rendering work!
        if (targetAlpha > 0f)
        {
            panel.gameObject.SetActive(true);
        }

        // Disable interaction immediately if fading out
        if (!makeInteractable)
        {
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }

        float startAlpha = panel.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            panel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panel.alpha = targetAlpha;

        // Enable interaction only after full fade-in
        if (makeInteractable)
        {
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }

        // If we faded OUT completely, disable the GameObject to save performance
        if (targetAlpha <= 0f)
        {
            panel.gameObject.SetActive(false);
        }
    }

    // Pulses the text endlessly using PingPong
    private IEnumerator PulseTextLoop(Transform textTransform)
    {
        Vector3 minScale = Vector3.one * 0.8f;
        Vector3 maxScale = Vector3.one * 1.2f;
        float pulseSpeed = 4f;
        
        while (true)
        {
            float scale = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            textTransform.localScale = Vector3.Lerp(minScale, maxScale, scale);
            yield return null;
        }
    }

    // Shrinks the text down to 0 before hiding it
    private IEnumerator ShrinkTextAway(Transform textTransform)
    {
        Vector3 startScale = textTransform.localScale;
        float elapsed = 0f;
        float shrinkDuration = 0.3f;

        while (elapsed < shrinkDuration)
        {
            textTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / shrinkDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        textTransform.localScale = Vector3.zero;
        textTransform.gameObject.SetActive(false);
    }
}
