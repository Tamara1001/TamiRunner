using UnityEngine;

/// <summary>
/// Dynamically updates global shader variables for the Curved World Shader Graph
/// using heavy smoothing, a Phase-based topology system, and slow Perlin Noise.
/// </summary>
public class CurvedWorldManager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;

    [Header("Shader Properties (Reference Strings)")]
    [Tooltip("Must match the exact 'Reference' string in the Shader Graph Blackboard")]
    public string sidewayStrengthRef = "_SidewayStrength";
    [Tooltip("Must match the exact 'Reference' string in the Shader Graph Blackboard")]
    public string backwardsStrengthRef = "_BackwardsStrength";

    [Header("Lane Influence (Sideways)")]
    [Tooltip("How much the world bends opposite to the player's lane")]
    public float laneInfluenceMultiplier = -0.001f; 
    [Tooltip("How quickly the lane-bending catches up to the player's current lane")]
    public float laneSmoothingSpeed = 2f;

    [Header("Speed Influence (Downhill)")]
    [Tooltip("How much the downhill slope intensifies at max speed compared to base speed")]
    public float speedInfluenceMultiplier = 0.002f;
    public float baseDownhillCurve = 0.001f;

    [Header("Phase System (Road Topology)")]
    [Tooltip("How often (in seconds) the world changes its target curve shape")]
    public float phaseChangeInterval = 10f;
    [Tooltip("How fast the world interpolates to a new Phase topology (Acts as a spring buffer)")]
    public float phaseSmoothingTime = 3f;
    [Tooltip("Min and Max ranges for the random Sideways bends during a Phase")]
    public Vector2 sidewaysPhaseRange = new Vector2(-0.003f, 0.003f);
    [Tooltip("Min and Max ranges for Downhill bends. Negative values create uphill sections!")]
    public Vector2 backwardsPhaseRange = new Vector2(-0.001f, 0.005f); 

    [Header("Winding Road Noise (Perlin)")]
    [Tooltip("A smaller number makes the noise slower and wider")]
    public float noiseFrequency = 0.2f;
    [Tooltip("X = Sideways Noise Amplitude, Y = Downhill Noise Amplitude (Increased for wider sweeps)")]
    public Vector2 noiseAmplitude = new Vector2(0.002f, 0.001f);

    private int sidewayID;
    private int backwardsID;

    // Smoothing Trackers
    private float currentLaneOffset = 0f;
    
    // Phase System Trackers
    private float phaseTimer = 0f;
    private float currentPhaseSideways = 0f;
    private float currentPhaseBackwards = 0f;
    private float targetPhaseSideways = 0f;
    private float targetPhaseBackwards = 0f;
    private float sidewayVelocity = 0f;
    private float backwardsVelocity = 0f;

    private void Awake()
    {
        // Cache the shader property IDs for performance
        sidewayID = Shader.PropertyToID(sidewayStrengthRef);
        backwardsID = Shader.PropertyToID(backwardsStrengthRef);
        
        // Initialize the first phase immediately
        PickNewPhase();
    }

    private void Update()
    {
        if (GameManager.Instance == null || playerController == null) return;

        // 1. PHASE SYSTEM (Road Topology)
        HandlePhaseSystem();

        // 2. LANE SMOOTHING (Bend opposite to player's lane, smoothed over time)
        // Lanes are 0 to 4. Center is 2.
        float targetLaneOffset = (playerController.CurrentLane - 2f);
        // Lerp creates a smooth, asymptotic curve toward the target lane
        currentLaneOffset = Mathf.Lerp(currentLaneOffset, targetLaneOffset, Time.deltaTime * laneSmoothingSpeed);
        float laneSideways = currentLaneOffset * laneInfluenceMultiplier;

        // 3. SPEED REACTION (Steeper slope as speed increases)
        float speedFactor = 0f;
        float baseSpeed = GameManager.Instance.baseSpeed;
        float maxSpeed = GameManager.Instance.maxSpeed;
        
        if (maxSpeed > baseSpeed)
        {
            speedFactor = Mathf.Clamp01((GameManager.Instance.currentSpeed - baseSpeed) / (maxSpeed - baseSpeed));
        }
        float speedBackwards = baseDownhillCurve + (speedFactor * speedInfluenceMultiplier);

        // 4. SMOOTH RANDOMNESS (Perlin Noise)
        // Decreased noiseFrequency makes it organically wide. Increased amplitude makes it noticeable.
        // Shifting the output from [0, 1] to [-1, 1] allows bidirectional swaying.
        float noiseX = (Mathf.PerlinNoise(Time.time * noiseFrequency, 0f) - 0.5f) * 2f * noiseAmplitude.x;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * noiseFrequency + 100f) - 0.5f) * 2f * noiseAmplitude.y;

        // 5. COMBINE ALL FORCES
        // The final shader value is a composite of the Phase System + Lane Bend + Speed Slope + Random Noise
        float finalSideways = currentPhaseSideways + laneSideways + noiseX;
        float finalBackwards = currentPhaseBackwards + speedBackwards + noiseY;

        // Push directly to the GPU globally. No material instantiation occurs!
        Shader.SetGlobalFloat(sidewayID, finalSideways);
        Shader.SetGlobalFloat(backwardsID, finalBackwards);
    }

    private void HandlePhaseSystem()
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
            PickNewPhase();
        }

        // Smoothly transition the current phase values towards the target phase values over several seconds.
        // SmoothDamp acts like a physics spring, preventing snappy, robotic linear transitions.
        currentPhaseSideways = Mathf.SmoothDamp(currentPhaseSideways, targetPhaseSideways, ref sidewayVelocity, phaseSmoothingTime);
        currentPhaseBackwards = Mathf.SmoothDamp(currentPhaseBackwards, targetPhaseBackwards, ref backwardsVelocity, phaseSmoothingTime);
    }

    private void PickNewPhase()
    {
        phaseTimer = phaseChangeInterval;

        // Pick a random target topology for the world to gradually morph into
        targetPhaseSideways = Random.Range(sidewaysPhaseRange.x, sidewaysPhaseRange.y);
        targetPhaseBackwards = Random.Range(backwardsPhaseRange.x, backwardsPhaseRange.y);
    }

    private void OnDisable()
    {
        // Safety reset when the scene unloads or object is disabled so the editor scene view isn't permanently warped
        Shader.SetGlobalFloat(sidewayID, 0f);
        Shader.SetGlobalFloat(backwardsID, 0f);
    }
}
