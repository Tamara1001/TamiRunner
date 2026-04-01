using UnityEngine;

/// <summary>
/// Controls the player's 5-lane movement, jumping, and sliding for an Infinite Runner.
/// Assumes the player object stays at Z=0 and the world moves towards them.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    [Tooltip("Distance in Unity units between each lane")]
    public float laneDistance = 2.5f;
    [Tooltip("How quickly the player snaps to the target lane")]
    public float laneSwitchSpeed = 15f;

    // We have 5 lanes: 0 (Far Left), 1 (Mid Left), 2 (Center), 3 (Mid Right), 4 (Far Right).
    private int currentLane = 2;

    [Header("Jump Settings")]
    [Tooltip("Initial upward velocity when jumping")]
    public float jumpForce = 8f;
    [Tooltip("Downward pull applied every frame while airborne")]
    public float gravity = -20f;

    private float verticalVelocity;
    private bool isGrounded = true;

    [Header("Slide Settings")]
    [Tooltip("How long the slide lasts in seconds")]
    public float slideDuration = 1.0f;
    [Tooltip("Collider height when sliding")]
    public float slideHeight = 1.0f;
    [Tooltip("Collider center when sliding")]
    public Vector3 slideCenter = new Vector3(0, 0.5f, 0);

    private bool isSliding = false;
    private float slideTimer;

    // Collider references for state management
    private CapsuleCollider capsuleCollider;
    private float originalHeight;
    private Vector3 originalCenter;

    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Store the original collider dimensions to restore them after sliding
        originalHeight = capsuleCollider.height;
        originalCenter = capsuleCollider.center;
    }

    private void Update()
    {
        HandleInput();
        HandleSlideTimer();
        ApplyMovement();
    }

    /// <summary>
    /// Listens for lane switching, jumping, and sliding inputs.
    /// </summary>
    private void HandleInput()
    {
        // Lane Switching
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwitchLane(-1); // Move Left
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwitchLane(1);  // Move Right
        }

        // Jumping
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded && !isSliding)
        {
            Jump();
        }

        // Sliding
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isGrounded && !isSliding)
        {
            StartSlide();
        }
        // Allows player to fast-fall / slide while in the air (Optional, common in runners)
        else if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isGrounded)
        {
            verticalVelocity -= jumpForce; // Slam down quickly
            StartSlide();
        }
    }

    /// <summary>
    /// Changes the target lane based on direction (-1 for left, 1 for right).
    /// </summary>
    private void SwitchLane(int direction)
    {
        currentLane += direction;

        // Clamp the lane to stay within our 5 lanes (0 to 4)
        currentLane = Mathf.Clamp(currentLane, 0, 4);
    }

    private void Jump()
    {
        isGrounded = false;
        verticalVelocity = jumpForce;
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        // Shrink the collider to slide under obstacles
        capsuleCollider.height = slideHeight;
        capsuleCollider.center = slideCenter;
    }

    private void StopSlide()
    {
        isSliding = false;

        // Restore the original collider dimensions
        capsuleCollider.height = originalHeight;
        capsuleCollider.center = originalCenter;
    }

    private void HandleSlideTimer()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                StopSlide();
            }
        }
    }

    /// <summary>
    /// Calculates and applies the mathematical translation. 
    /// No Rigidbody physics are used to ensure maximum responsiveness.
    /// </summary>
    private void ApplyMovement()
    {
        // 1. HORIZONTAL MOVEMENT (Lanes)
        // Calculate the target X position. Center lane (2) is X = 0.
        // Lanes: 0 -> -2*laneDist, 1 -> -1*laneDist, 2 -> 0, 3 -> 1*laneDist, 4 -> 2*laneDist
        float targetX = (currentLane - 2) * laneDistance;

        // Use Lerp for a smooth, snappy transition toward the target X position
        float newX = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * laneSwitchSpeed);

        // 2. VERTICAL MOVEMENT (Jump & Gravity)
        if (!isGrounded)
        {
            // Apply gravity over time
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Calculate the new Y position
        float newY = transform.position.y + (verticalVelocity * Time.deltaTime);

        // Simple Ground Check (Assuming the ground floor is exactly at Y = 0)
        // Note: For uneven terrain, you'd replace this with a Physics.SphereCast downwards.
        if (newY <= 0f)
        {
            newY = 0f;
            verticalVelocity = 0f;
            isGrounded = true;
        }

        // 3. APPLY POSITION
        // Keep Z strictly at 0 as defined in the GDD
        transform.position = new Vector3(newX, newY, 0f);
    }
}
