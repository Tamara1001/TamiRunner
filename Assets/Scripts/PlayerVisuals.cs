using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all visual "juice" for Ambu (Damage flashing, Heal flashing, Squash & Stretch).
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [Header("Mesh References")]
    [Tooltip("The child object containing the Mesh Filter/Renderer, NOT the parent capsule!")]
    public Transform playerMeshTransform; 
    public MeshRenderer meshRenderer;

    [Header("Squash & Stretch")]
    public float squashDuration = 0.15f;
    public Vector3 jumpStretchScale = new Vector3(0.7f, 1.3f, 0.7f);
    public Vector3 landSquashScale = new Vector3(1.3f, 0.7f, 1.3f);
    private Vector3 originalScale;

    [Header("Colors & Materials")]
    public Color damageColor = Color.red;
    [Tooltip("Celestial magic purple/gold for the shy healer")]
    public Color healColor = new Color(0.6f, 0.2f, 1f); 
    private Color originalColor;

    private Coroutine squashCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (playerMeshTransform != null) originalScale = playerMeshTransform.localScale;
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
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
        
        // 1. Scale towards the distorted squash/stretch target
        while (time < halfDur)
        {
            playerMeshTransform.localScale = Vector3.Lerp(originalScale, targetScale, time / halfDur);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        // 2. Snap back to original scale smoothly
        while (time < halfDur)
        {
            playerMeshTransform.localScale = Vector3.Lerp(targetScale, endScale, time / halfDur);
            time += Time.deltaTime;
            yield return null;
        }

        // Safety: ensure it strictly returns to normal
        playerMeshTransform.localScale = endScale;
    }

    // ==========================================
    // FLASH & BLINK (DAMAGE / HEAL)
    // ==========================================

    public void TriggerDamageVisuals(float iframeDuration)
    {
        if (meshRenderer == null) return;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColor(damageColor, 0.2f));

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkMesh(iframeDuration));
    }

    public void TriggerHealVisuals()
    {
        if (meshRenderer == null) return;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashColor(healColor, 0.4f)); // A warm celestial glow
    }

    private IEnumerator FlashColor(Color flashCol, float duration)
    {
        meshRenderer.material.color = flashCol;
        yield return new WaitForSeconds(duration);
        
        // Safety: Ensure it resets so pooled elements don't get stuck red!
        meshRenderer.material.color = originalColor; 
    }

    private IEnumerator BlinkMesh(float duration)
    {
        float elapsed = 0f;
        float blinkRate = 0.1f;

        while (elapsed < duration)
        {
            meshRenderer.enabled = !meshRenderer.enabled; // Toggle visibility
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        meshRenderer.enabled = true; // Ensure it stays visible when I-Frames end!
    }
}
