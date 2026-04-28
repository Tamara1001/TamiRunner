using UnityEngine;

/// <summary>
/// Animates collectibles with a continuous rotation and sine-wave bob.
/// Safe for Object Pooling because it restores its original position in OnEnable.
/// </summary>
public class PickupAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float rotationSpeed = 100f;
    public float bobAmplitude = 0.5f;
    public float bobFrequency = 2f;

    private Vector3 startLocalPosition;
    private float timer;

    private void Awake()
    {
        // Cache the original local position as the anchor point for the bobbing
        startLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        // Reset timer and cleanly snap back to the start position when recycled
        timer = 0f;
        transform.localPosition = startLocalPosition;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 1. Bobbing (Mathf.Sin)
        float newY = startLocalPosition.y + Mathf.Sin(timer * bobFrequency) * bobAmplitude;
        transform.localPosition = new Vector3(startLocalPosition.x, newY, startLocalPosition.z);

        // 2. Rotating
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
