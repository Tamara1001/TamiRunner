using UnityEngine;

/// <summary>
/// A Simple Singleton GameManager to handle the core gameplay loop, scoring, and vitals.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameOver = false;

    [Header("Player Vitals")]
    public int maxLives = 3;
    public int currentLives;

    [Header("Scoring")]
    public float score = 0f;
    [Tooltip("How much score is automatically added per second")]
    public float baseScoreRate = 10f;
    public float currentScoreMultiplier = 1f;

    [Header("Multiplier Powerup")]
    public float multiplierDuration = 5f;
    private float multiplierTimer = 0f;

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
        levelManager = FindObjectOfType<LevelManager>();
    }

    private void Update()
    {
        if (isGameOver) return;

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

    // --- API CALLED BY THE PLAYER CONTROLLER ---

    public void AddScore(float amount)
    {
        if (isGameOver) return;
        score += amount;
    }

    public void AddLife()
    {
        if (isGameOver) return;

        currentLives++;
        if (currentLives > maxLives)
        {
            currentLives = maxLives;
        }
    }

    public void ActivateMultiplier(float amount)
    {
        if (isGameOver) return;

        currentScoreMultiplier = amount;
        multiplierTimer = multiplierDuration;
    }

    public void TakeDamage()
    {
        if (isGameOver) return;

        currentLives--;
        Debug.Log("Ouch! Lives remaining: " + currentLives);

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;

        // Stop the world from moving
        if (levelManager != null)
        {
            levelManager.moveSpeed = 0f;
        }

        Debug.Log($"GAME OVER! The Royal Guard caught Ambu! Final Score: {Mathf.FloorToInt(score)}");
    }
}
