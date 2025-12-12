/// <summary>
/// Provides the StimulusTriggerZone class that manages stimulus delivery based on animal behavior.
///
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
using Gimbl;
using UnityEngine;

/// <summary>
/// Manages stimulus delivery based on animal behavior within the trigger zone.
/// The trigger mode is determined by the presence of child GuidanceZone or OccupancyZone components.
/// </summary>
public class StimulusTriggerZone : MonoBehaviour
{
    /// <summary>
    /// Determines whether the stimulus boundary should be visible when this zone is active.
    /// Set at task creation time from the experiment config's show_stimulus_collision_boundary per trial type.
    /// </summary>
    public bool showBoundary = false;

    /// <summary>
    /// Determines whether this zone is active (only triggers once per lap). Reset by ResetZone.
    /// </summary>
    public bool isActive = true;

    /// <summary>Determines whether the animal is currently inside this trigger zone.</summary>
    private bool _inZone = false;

    /// <summary>Determines whether a lick was detected while the animal was in the zone.</summary>
    private bool _lickDetectedInZone = false;

    /// <summary>The child GuidanceZone that determines lick mode behavior, if present.</summary>
    private GuidanceZone _guidanceZone;

    /// <summary>The child OccupancyZone that determines occupancy mode behavior, if present.</summary>
    private OccupancyZone _occupancyZone;

    /// <summary>Determines whether this zone operates in occupancy mode based on presence of OccupancyZone child.</summary>
    private bool IsOccupancyMode => _occupancyZone != null;

    /// <summary>The reference to the Task for checking guidance mode state.</summary>
    private Task _task;

    /// <summary>The MQTT channel for sending stimulus trigger messages.</summary>
    private MQTTChannel _stimulusTrigger;

    /// <summary>The MQTT channel for receiving lick detection messages.</summary>
    private MQTTChannel _lickTrigger;

    /// <summary>Initializes the zone by finding child zones and setting up MQTT channels.</summary>
    void Start()
    {
        _task = FindAnyObjectByType<Task>();

        // Finds child zones that determine behavior mode
        _guidanceZone = GetComponentInChildren<GuidanceZone>();
        _occupancyZone = GetComponentInChildren<OccupancyZone>();

        // Sets up MQTT channels
        _stimulusTrigger = new MQTTChannel("Gimbl/Stimulus/");
        _lickTrigger = new MQTTChannel("LickPort/", true);
        _lickTrigger.Event.AddListener(OnLickDetected);
    }

    /// <summary>Updates the zone state each frame, handling stimulus trigger logic based on mode.</summary>
    void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (IsOccupancyMode)
        {
            UpdateOccupancyMode();
        }
        else
        {
            UpdateLickMode();
        }
    }

    /// <summary>
    /// Handles lick mode behavior (for WaterRewardTrial and similar):
    /// - When guidance disabled: Animal must lick in zone
    /// - When guidance enabled with GuidanceZone: Animal can lick in zone OR reach guidance zone
    /// - When guidance enabled without GuidanceZone: Stimulus on zone entry
    /// </summary>
    private void UpdateLickMode()
    {
        if (_task.requireLick) // Guidance mode disabled, animal must lick for stimulus
        {
            if (_inZone && _lickDetectedInZone)
            {
                TriggerStimulus();
            }
        }
        else if (_guidanceZone != null) // Guidance mode with GuidanceZone
        {
            // Animal can receive stimulus by licking anywhere in the trigger zone
            if (_inZone && _lickDetectedInZone)
            {
                TriggerStimulus();
            }
            // Or if animal reaches the guidance zone, delivers the stimulus anyway
            else if (_guidanceZone.inZone)
            {
                TriggerStimulus();
            }
        }
        else // Guidance mode but no GuidanceZone
        {
            // Animal gets stimulus as soon as it enters the stimulus zone
            if (_inZone)
            {
                TriggerStimulus();
            }
        }
    }

    /// <summary>
    /// Handles occupancy mode behavior (for GasPuffTrial and similar):
    /// - Animal must occupy OccupancyZone for duration to disarm boundary
    /// - Boundary collision only triggers stimulus when boundary is ARMED
    /// - In guidance mode, OccupancyFailed triggers movement blocking via MQTT
    /// </summary>
    private void UpdateOccupancyMode()
    {
        // In occupancy mode, stimulus is only triggered by boundary collision
        // when the boundary is still armed (occupancy requirement not met).
        // The OccupancyZone handles the timer and MQTT messages.

        // Boundary collision triggers stimulus only when boundary is ARMED
        if (_inZone && !_occupancyZone.boundaryDisarmed)
        {
            TriggerStimulus();
        }
    }

    /// <summary>Called when the animal enters the trigger zone collider.</summary>
    void OnTriggerEnter(Collider collider)
    {
        _inZone = true;
    }

    /// <summary>Called when the animal exits the trigger zone collider.</summary>
    void OnTriggerExit(Collider collider)
    {
        _inZone = false;
    }

    /// <summary>Triggers the stimulus, hides the boundary, and sends the MQTT message.</summary>
    private void TriggerStimulus()
    {
        Debug.Log("Stimulus");
        GetComponent<MeshRenderer>().enabled = false; // Hides boundary
        _stimulusTrigger.Send(); // Sends stimulus message over MQTT
        // Prevents multiple triggers
        isActive = false;
        _lickDetectedInZone = false;
    }

    /// <summary>
    /// MQTT callback that handles lick detection messages.
    /// Records that a lick occurred while in the zone (only relevant in lick mode).
    /// </summary>
    private void OnLickDetected()
    {
        Debug.Log("Lick!");
        if (isActive && _inZone && !IsOccupancyMode)
        {
            _lickDetectedInZone = true;
        }
    }

    /// <summary>
    /// Resets the zone state for a new lap.
    /// Called by ResetZone when the animal enters the reset zone.
    /// </summary>
    public void ResetState()
    {
        isActive = true;
        _lickDetectedInZone = false;
        _inZone = false;
        GetComponent<MeshRenderer>().enabled = showBoundary;
    }
}
