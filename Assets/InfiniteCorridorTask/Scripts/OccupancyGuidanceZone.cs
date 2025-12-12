/// <summary>
/// Provides the OccupancyGuidanceZone class that triggers brake activation in occupancy guidance mode.
///
/// Used as a child of OccupancyZone to define where guidance mode activates the brake.
/// When the animal enters this zone in guidance mode (!requireWait), sends a TriggerDelay message
/// to sl-experiment instructing it to lock the brake for the remaining occupancy duration.
/// </summary>
using Gimbl;
using UnityEngine;

/// <summary>
/// Secondary trigger zone for OccupancyZone that handles occupancy guidance mode.
/// When guidance mode is active and the animal enters, sends brake activation message with remaining duration.
/// </summary>
public class OccupancyGuidanceZone : MonoBehaviour
{
    /// <summary>Determines whether the animal is currently inside this guidance zone.</summary>
    [HideInInspector]
    public bool inZone = false;

    /// <summary>The reference to the Task for checking guidance mode state.</summary>
    private Task _task;

    /// <summary>The reference to the parent OccupancyZone to get remaining duration.</summary>
    private OccupancyZone _parentOccupancyZone;

    /// <summary>The MQTT channel for sending brake activation delay messages.</summary>
    private MQTTChannel<TriggerDelayMsg> _triggerDelayChannel;

    /// <summary>Determines whether the guidance trigger has already fired this lap.</summary>
    private bool _hasTriggered = false;

    /// <summary>Wrapper class for sending trigger delay duration over MQTT.</summary>
    public class TriggerDelayMsg
    {
        public uint delay_ms;
    }

    /// <summary>Initializes references and sets up MQTT channel.</summary>
    void Start()
    {
        _task = FindAnyObjectByType<Task>();
        _parentOccupancyZone = GetComponentInParent<OccupancyZone>();
        _triggerDelayChannel = new MQTTChannel<TriggerDelayMsg>("Gimbl/TriggerDelay/", false);
    }

    /// <summary>Called when the animal enters the guidance zone collider.</summary>
    void OnTriggerEnter(Collider other)
    {
        inZone = true;

        // Only triggers in guidance mode (!requireWait) and if not already triggered this lap
        if (!_task.requireWait && !_hasTriggered && _parentOccupancyZone != null)
        {
            TriggerBrakeActivation();
        }
    }

    /// <summary>Called when the animal exits the guidance zone collider.</summary>
    void OnTriggerExit(Collider other)
    {
        inZone = false;
    }

    /// <summary>Sends the TriggerDelay message with remaining occupancy duration to activate the brake.</summary>
    private void TriggerBrakeActivation()
    {
        // Calculates remaining duration based on how much time has already elapsed
        float elapsedMs = _parentOccupancyZone.GetElapsedMs();
        uint remainingMs = (uint)Mathf.Max(0, _parentOccupancyZone.occupancyDurationMs - elapsedMs);

        UnityEngine.Debug.Log($"OccupancyGuidanceZone: Triggering brake for {remainingMs}ms");

        _triggerDelayChannel.Send(new TriggerDelayMsg { delay_ms = remainingMs });
        _hasTriggered = true;
    }

    /// <summary>
    /// Resets the guidance zone state for a new lap.
    /// Called by ResetZone when the animal enters the reset zone.
    /// </summary>
    public void ResetState()
    {
        inZone = false;
        _hasTriggered = false;
    }
}
