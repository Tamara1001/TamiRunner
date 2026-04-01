using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages strict Object Pooling and mathematical translation for infinite street generation.
/// Player stays at Vector3.zero, the world moves towards them.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Tooltip("Array of street module prefabs (e.g., normal street, market street).")]
    public GameObject[] chunkPrefabs;
    [Tooltip("The exact Z-axis length of your chunk modules (e.g., 50).")]
    public float chunkLength = 50f;
    [Tooltip("Number of chunks to keep active. Must be enough to cover the horizon.")]
    public int chunksOnScreen = 7;

    [Header("Movement Settings")]
    [Tooltip("Speed the environment moves towards the camera.")]
    public float moveSpeed = 20f;
    [Tooltip("The Z-coordinate at which a chunk is fully behind the player and ready to recycle.")]
    public float despawnThreshold = -60f;

    // A strict FIFO queue to maintain the exact order of active chunks
    private Queue<GameObject> activeChunks;

    // Mathematical tracker for the tail-end position. This completely prevents gaps.
    private float nextSpawnZ = 0f;

    private void Start()
    {
        activeChunks = new Queue<GameObject>();

        // Pre-warm our Object Pool to create the initial path perfectly back-to-back.
        // No instantiation will occur after this Start() function!
        for (int i = 0; i < chunksOnScreen; i++)
        {
            SpawnInitialChunk();
        }
    }

    private void Update()
    {
        MoveWorld();
        CheckRecycle();
    }

    /// <summary>
    /// Initializer function. Used only in Start() to populate the screen.
    /// Spawns the first chunks directly underneath the player (Z=0).
    /// </summary>
    private void SpawnInitialChunk()
    {
        // Pick a random chunk for visual variety
        int randomIndex = Random.Range(0, chunkPrefabs.Length);

        // Spawn perfectly locked to the nextSpawnZ tracker
        GameObject chunk = Instantiate(chunkPrefabs[randomIndex], new Vector3(0, 0, nextSpawnZ), Quaternion.identity);

        // Parent to LevelManager for a clean hierarchy
        chunk.transform.SetParent(this.transform);

        activeChunks.Enqueue(chunk);

        // Advance tracker forward by exactly one chunk length
        nextSpawnZ += chunkLength;
    }

    /// <summary>
    /// Translates all active chunks towards the player mathematically.
    /// </summary>
    private void MoveWorld()
    {
        float step = moveSpeed * Time.deltaTime;

        foreach (GameObject chunk in activeChunks)
        {
            chunk.transform.Translate(Vector3.back * step, Space.World);
        }

        // CRITICAL BUG PREVENTION:
        // By pulling the 'nextSpawnZ' tracker backwards alongside the chunks,
        // we lock the chunk world-coordinates near zero (e.g., between -60 and 300).
        // This makes catastrophic infinite-distance floating-point jitter physically impossible!
        nextSpawnZ -= step;
    }

    /// <summary>
    /// Checks the oldest chunk and cleanly snaps it to the end of the line if it falls behind.
    /// </summary>
    private void CheckRecycle()
    {
        // We use 'while' instead of 'if' to safely handle massive lag spikes or blinding speeds.
        while (activeChunks.Peek().transform.position.z < despawnThreshold)
        {
            // Dequeue the old chunk
            GameObject oldestChunk = activeChunks.Dequeue();

            // Mathematically snap it exactly to the end of the line (Zero gaps!)
            oldestChunk.transform.position = new Vector3(0, 0, nextSpawnZ);

            // Send it to the back of the line
            activeChunks.Enqueue(oldestChunk);

            // Inform the tracker we just added a chunk to the tail
            nextSpawnZ += chunkLength;

            /*
             * NOTE FOR FUTURE POLISH: 
             * Because we are reusing the *exact* same GameObject, a 7-chunk pattern will repeat.
             * To randomize the endless track later: keep a larger pool of disabled chunks, 
             * deactivate 'oldestChunk' here, pull a random inactive chunk, position it at 
             * nextSpawnZ, activate it, and enqueue the new one instead.
             */
        }
    }
}
