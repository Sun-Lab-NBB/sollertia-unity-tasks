using UnityEngine;
using Gimbl;

/// <summary>
/// Manages stimulus delivery based on animal behavior within the trigger zone.
/// Supports two trigger modes determined by the presence of child zones:
///
/// Lick Mode (WaterRewardTrial behavior):
/// - Requires GuidanceZone child for guidance mode support
/// - When guidance is disabled: Animal must lick in the zone to trigger stimulus
/// - When guidance is enabled: Stimulus delivered when animal reaches GuidanceZone
///
/// Occupancy Mode (GasPuffTrial behavior):
/// - Requires OccupancyZone child
/// - Animal must occupy zone for duration to DISARM the boundary
/// - Boundary collision triggers stimulus only when boundary is ARMED
/// </summary>
public class StimulusTriggerZone : MonoBehaviour
{
    /// <summary>
    /// Whether the stimulus boundary should be visible when this zone is active.
    /// Set at task creation time from the experiment config's show_stimulus_collision_boundary per trial type.
    /// </summary>
    public bool showBoundary = false;

    /// <summary>
    /// Whether this zone is active (only trigger once per lap). Reset by ResetZone.
    /// </summary>
    public bool isActive = true;

    // Internal state tracking
    private bool inZone = false;
    private bool lickDetectedInZone = false;

    // Child zones that determine behavior mode
    private GuidanceZone guidanceZone;
    private OccupancyZone occupancyZone;

    // Trigger mode is determined by presence of child zones
    private bool isOccupancyMode => occupancyZone != null;

    // Reference to Task for guidance mode state
    private Task task;

    // MQTT Channels
    private MQTTChannel stimulusTrigger;
    private MQTTChannel lickTrigger;

    void Start()
    {
        task = FindAnyObjectByType<Task>();

        // Find child zones that determine behavior mode
        guidanceZone = GetComponentInChildren<GuidanceZone>();
        occupancyZone = GetComponentInChildren<OccupancyZone>();

        // Setup MQTT channels
        stimulusTrigger = new MQTTChannel("Gimbl/Stimulus/");
        lickTrigger = new MQTTChannel("LickPort/", true);
        lickTrigger.Event.AddListener(OnLickDetected);
    }

    void Update()
    {
        if (!isActive)
            return;

        if (isOccupancyMode)
        {
            UpdateOccupancyMode();
        }
        else
        {
            UpdateLickMode();
        }
    }

    /// <summary>
    /// Lick mode behavior (for WaterRewardTrial and similar):
    /// - When guidance disabled: Animal must lick in zone
    /// - When guidance enabled with GuidanceZone: Animal can lick in zone OR reach guidance zone
    /// - When guidance enabled without GuidanceZone: Stimulus on zone entry
    /// </summary>
    private void UpdateLickMode()
    {
        if (task.requireLick) // Guidance mode disabled, animal must lick for stimulus
        {
            if (inZone && lickDetectedInZone)
            {
                TriggerStimulus();
            }
        }
        else if (guidanceZone != null) // Guidance mode with GuidanceZone
        {
            // Animal can receive stimulus by licking anywhere in the trigger zone
            if (inZone && lickDetectedInZone)
            {
                TriggerStimulus();
            }
            // Or if animal reaches the guidance zone, deliver the stimulus anyway
            else if (guidanceZone.inZone)
            {
                TriggerStimulus();
            }
        }
        else // Guidance mode but no GuidanceZone
        {
            // Animal gets stimulus as soon as it enters the stimulus zone
            if (inZone)
            {
                TriggerStimulus();
            }
        }
    }

    /// <summary>
    /// Occupancy mode behavior (for GasPuffTrial and similar):
    /// - Animal must occupy OccupancyZone for duration to disarm boundary
    /// - Boundary collision only triggers stimulus when boundary is ARMED
    /// - In guidance mode, OccupancyFailed triggers movement blocking via MQTT
    /// </summary>
    private void UpdateOccupancyMode()
    {
        // In occupancy mode, stimulus is only triggered by boundary collision
        // when the boundary is still armed (occupancy requirement not met)
        // The OccupancyZone handles the timer and MQTT messages

        // Boundary collision triggers stimulus only when boundary is ARMED
        if (inZone && !occupancyZone.boundaryDisarmed)
        {
            TriggerStimulus();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        inZone = true;
    }

    void OnTriggerExit(Collider collider)
    {
        inZone = false;
    }

    private void TriggerStimulus()
    {
        Debug.Log("Stimulus");
        GetComponent<MeshRenderer>().enabled = false; // Hide boundary
        stimulusTrigger.Send(); // Send stimulus message over MQTT
        // Prevent multiple triggers
        isActive = false;
        lickDetectedInZone = false;
    }

    /// <summary>
    /// Called on message from lickport - checks if animal is in zone for correct response.
    /// Only relevant in lick mode.
    /// </summary>
    private void OnLickDetected()
    {
        Debug.Log("Lick!");
        if (isActive && inZone && !isOccupancyMode)
        {
            lickDetectedInZone = true;
        }
    }

    /// <summary>
    /// Resets the zone state for a new lap.
    /// Called indirectly by ResetZone via isActive setter and mesh visibility.
    /// </summary>
    public void ResetState()
    {
        isActive = true;
        lickDetectedInZone = false;
        inZone = false;
        GetComponent<MeshRenderer>().enabled = showBoundary;
    }
}
