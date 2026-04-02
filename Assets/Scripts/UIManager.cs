using UnityEngine;
using TMPro; // Required for modern Unity UI

/// <summary>
/// Basic UI Manager to read and display data from the GameManager.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    [Header("Game Over Screen")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    // Cache the GameManager reference
    private GameManager gameManager;

    private void Start()
    {
        // Grab the Singleton instance
        gameManager = GameManager.Instance;

        // Ensure Game Over plane starts deactivated
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Graceful fail-safe if the GameManager isn't initialized yet
        if (gameManager == null) return;

        UpdateHUD();

        // Trigger the Game Over visual exactly once when death occurs
        if (gameManager.isGameOver && gameOverPanel != null && !gameOverPanel.activeSelf)
        {
            DisplayGameOver();
        }
    }

    /// <summary>
    /// Continuously updates text values while playing.
    /// Notice the null checks prevent the game crashing if you forget to assign text fields in the Inspector.
    /// </summary>
    private void UpdateHUD()
    {
        if (scoreText != null)
        {
            // Round down the float to show a clean integer score
            scoreText.text = "Score: " + Mathf.FloorToInt(gameManager.score).ToString();
        }

        if (livesText != null)
        {
            livesText.text = "Lives: " + gameManager.currentLives.ToString();
        }
    }

    /// <summary>
    /// Halts standard UI updates and overlays the death screen.
    /// </summary>
    private void DisplayGameOver()
    {
        gameOverPanel.SetActive(true);

        if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score: " + Mathf.FloorToInt(gameManager.score).ToString();
        }

        // Hide the active HUD to clean up the screen
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (livesText != null) livesText.gameObject.SetActive(false);
    }
}
