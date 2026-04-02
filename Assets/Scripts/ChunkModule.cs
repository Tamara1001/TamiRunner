using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to every street Chunk Prefab. Handles resetting its own state when recycled.
/// </summary>
public class ChunkModule : MonoBehaviour
{
    // A cached list to hold references to this specific chunk's collectibles
    private List<GameObject> resettablePickups = new List<GameObject>();

    private void Awake()
    {
        // By running this in Awake(), we only pay the performance cost of GetComponents 
        // once at startup, completely avoiding frame drops during active gameplay.
        // The 'true' parameter ensures we also find children that are currently inactive.
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            // Cache any object that can be "picked up" and hidden by the player
            if (child.CompareTag("Coin") || child.CompareTag("Life") || child.CompareTag("Multiplier"))
            {
                resettablePickups.Add(child.gameObject);
            }

            // Note: We DO NOT track objects tagged "Obstacle" because Ambu passes 
            // through them using I-Frames, meaning they never get disabled!
        }
    }

    /// <summary>
    /// Reactivates all cached pickups. Called by the LevelManager.
    /// </summary>
    public void ResetChunk()
    {
        foreach (GameObject pickup in resettablePickups)
        {
            if (pickup != null)
            {
                pickup.SetActive(true);
            }
        }
    }
}
