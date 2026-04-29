using UnityEngine;

/// <summary>
/// A Simple Singleton GameManager to handle the core gameplay loop, scoring, and vitals.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    [Header("Game State")]
    public GameState currentState = GameState.Menu;

    [Header("Player Vitals")]
    public int maxLives = 3;
    public int currentLives;

    [Header("Scoring")]
    public float score = 0f;
    public float currentTime = 0f;
    [Tooltip("How much score is automatically added per second")]
    public float baseScoreRate = 10f;
    public float currentScoreMultiplier = 1f;

    [Header("Multiplier Powerup")]
    public float multiplierDuration = 5f;
    private float multiplierTimer = 0f;

    [Header("Animation References")]
    public Animator playerAnimator;

    // Reference to LevelManager to stop movement on Game Over
    private LevelManager levelManager;

    private void Awake()
    {
        // Enforce Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Important to not use gameObject.SetActive(false) for singleton enforcement!
        }
    }

    private void Start()
    {
        currentLives = maxLives;

        // Grab the LevelManager to stop the world upon Game Over
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        currentTime += Time.deltaTime;
        HandleScoring();
        HandleMultiplierTimer();
    }

    private void HandleScoring()
    {
        // Increase score passively over distance/time
        score += baseScoreRate * currentScoreMultiplier * Time.deltaTime;
    }

    private void HandleMultiplierTimer()
    {
        if (currentScoreMultiplier > 1f)
        {
            multiplierTimer -= Time.deltaTime;
            if (multiplierTimer <= 0f)
            {
                // Revert to normal scoring rate
                currentScoreMultiplier = 1f;
            }
        }
    }

    // --- API CALLED BY THE UI & PLAYER CONTROLLER ---

    public void StartGame()
    {
        currentState = GameState.Playing;
    }

    public void AddScore(float amount)
    {
        if (currentState != GameState.Playing) return;
        score += amount;
    }

    public void AddLife()
    {
        if (currentState != GameState.Playing) return;

        currentLives++;
        if (currentLives > maxLives)
        {
            currentLives = maxLives;
        }
    }

    public void ActivateMultiplier(float amount)
    {
        if (currentState != GameState.Playing) return;

        currentScoreMultiplier = amount;
        multiplierTimer = multiplierDuration;
    }

    public void TakeDamage()
    {
        if (currentState != GameState.Playing) return;

        currentLives--;
        Debug.Log("Ouch! Lives remaining: " + currentLives);

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        currentState = GameState.GameOver;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Death");
        }

        // Save records
        float bestScore = PlayerPrefs.GetFloat("HighScore", 0f);
        if (score > bestScore) PlayerPrefs.SetFloat("HighScore", score);

        float bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        if (currentTime > bestTime) PlayerPrefs.SetFloat("BestTime", currentTime);
        PlayerPrefs.Save();

        // Stop the world from moving
        if (levelManager != null)
        {
            levelManager.moveSpeed = 0f;
        }
    }
}
