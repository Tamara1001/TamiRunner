using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles all visual "juice" for Ambu (Damage flashing, Heal flashing, Squash & Stretch).
/// Optimized for complex characters with multiple SkinnedMeshRenderers using MaterialPropertyBlock.
/// </summary>
public class PlayerVisualsNew : MonoBehaviour
{
    [Header("Mesh References")]
    [Tooltip("The parent object holding the Rig/Armature and all Mesh Renderers.")]
    public Transform playerMeshTransform; 
    
    // Cached renderers for performance
    private Renderer[] allRenderers;
    private MaterialPropertyBlock propBlock;
    private int colorPropertyID;

    [Header("Squash & Stretch")]
    public float squashDuration = 0.15f;
    public Vector3 jumpStretchScale = new Vector3(0.7f, 1.3f, 0.7f);
    public Vector3 landSquashScale = new Vector3(1.3f, 0.7f, 1.3f);
    private Vector3 originalScale = Vector3.one;

    [Header("Colors & Materials")]
    public Color damageColor = Color.red;
    [Tooltip("Celestial magic purple/gold for the shy healer")]
    public Color healColor = new Color(0.6f, 0.2f, 1f); 
    [Tooltip("Use '_Color' for Standard Pipeline, or '_BaseColor' for URP/HDRP")]
    public string colorPropertyName = "_BaseColor";

    private Coroutine squashCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        // 1. Performance Caching: Find all renderers ONLY ONCE at startup.
        allRenderers = GetComponentsInChildren<Renderer>(true);
        
        // 2. Initialize the MaterialPropertyBlock for zero-allocation color swapping
        propBlock = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID(colorPropertyName);

        if (playerMeshTransform != null) originalScale = playerMeshTransform.localScale;
    }

    private void OnEnable()
    {
        // Reset states in case the player is recycled via Object Pooling
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (playerMeshTransform != null) playerMeshTransform.localScale = originalScale;
        
        if (allRenderers != null)
        {
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                r.enabled = true; // Ensure visibility
                r.SetPropertyBlock(null); // Clear any color overrides
            }
        }
    }

    // ==========================================
    // SQUASH & STRETCH
    // ==========================================
    
    public void TriggerJumpSquash()
    {
        if (playerMeshTransform == null) return;
        if (squashCoroutine != null) StopCoroutine(squashCoroutine);
        squashCoroutine = StartCoroutine(AnimateScale(jumpStretchScale, originalScale, squashDuration));
    }

    public void TriggerLandSquash()
    {
        if (playerMeshTransform == null) return;
        if (squashCoroutine != null) StopCoroutine(squashCoroutine);
        squashCoroutine = StartCoroutine(AnimateScale(landSquashScale, originalScale, squashDuration));
    }

    private IEnumerator AnimateScale(Vector3 targetScale, Vector3 endScale, float duration)
    {
        float time = 0;
        float halfDur = duration / 2f;
        
        while (time < halfDur)
        {
            playerMeshTransform.localScale = Vector3.Lerp(originalScale, targetScale, time / halfDur);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < halfDur)
        {
            playerMeshTransform.localScale = Vector3.Lerp(targetScale, endScale, time / halfDur);
            time += Time.deltaTime;
            yield return null;
        }

        playerMeshTransform.localScale = endScale;
    }

    // ==========================================
    // FLASH & BLINK (DAMAGE / HEAL)
    // ==========================================

    public void TriggerDamageVisuals(float iframeDuration)
    {
        if (allRenderers == null || allRenderers.Length == 0) return;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColor(damageColor, 0.2f));

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkMesh(iframeDuration));
    }

    public void TriggerHealVisuals()
    {
        if (allRenderers == null || allRenderers.Length == 0) return;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColor(healColor, 0.4f)); 
    }

    private IEnumerator FlashColor(Color flashCol, float duration)
    {
        // Apply Color Override using MaterialPropertyBlock (Very Fast!)
        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropertyID, flashCol);
            r.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(duration);
        
        // Remove Override to restore original multi-materials seamlessly
        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(null);
        }
    }

    private IEnumerator BlinkMesh(float duration)
    {
        float elapsed = 0f;
        float blinkRate = 0.1f;
        bool isVisible = false; // Start by turning them off

        while (elapsed < duration)
        {
            foreach (var r in allRenderers)
            {
                if (r != null) r.enabled = isVisible;
            }
            isVisible = !isVisible;
            
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        // Safety: Ensure everything stays visible when I-Frames end!
        foreach (var r in allRenderers)
        {
            if (r != null) r.enabled = true;
        }
    }
}
