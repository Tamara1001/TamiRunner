using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A lightweight pooling system specifically for playing temporary Particle Effects (e.g. Pickup Sparks).
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs")]
    public ParticleSystem pickupEffectPrefab;
    
    [Header("Pool Settings")]
    public int poolSize = 5;
    
    private List<ParticleSystem> pool = new List<ParticleSystem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Pre-warm the pool
        if (pickupEffectPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                ParticleSystem ps = Instantiate(pickupEffectPrefab, transform);
                
                // CRITICAL FOR POOLING: Force the Particle System to disable itself when finished playing
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Disable;
                
                ps.gameObject.SetActive(false);
                pool.Add(ps);
            }
        }
    }

    /// <summary>
    /// Finds an inactive particle system, moves it to the target location, and plays it.
    /// </summary>
    public void PlayPickupEffect(Vector3 position)
    {
        foreach (ParticleSystem ps in pool)
        {
            if (!ps.gameObject.activeInHierarchy)
            {
                ps.transform.position = position;
                ps.gameObject.SetActive(true);
                ps.Play();
                return; // Play exactly one
            }
        }
    }
}
