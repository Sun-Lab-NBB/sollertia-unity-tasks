using UnityEngine;
using Gimbl;
using System.Diagnostics;

/// <summary>
/// Tracks whether an animal has occupied a zone for a required duration.
/// Used for trial types that require occupancy-based stimulus disarming.
///
/// Behavior:
/// - When the animal enters the zone, a high-precision timer starts
/// - If the animal stays for occupancy_duration_ms, emits OccupancyMet and disarms the boundary
/// - If the animal leaves early, emits OccupancyFailed and the boundary remains armed
/// - The parent StimulusTriggerZone reads the boundaryDisarmed state to determine collision behavior
/// </summary>
public class OccupancyZone : MonoBehaviour
{
    /// <summary>
    /// The duration in milliseconds that the animal must occupy the zone to disarm the boundary.
    /// Set at task creation time from the experiment config.
    /// </summary>
    public float occupancyDurationMs = 1000f;

    /// <summary>
    /// Whether the animal is currently inside this zone.
    /// </summary>
    [HideInInspector]
    public bool inZone = false;

    /// <summary>
    /// Whether the boundary has been disarmed by meeting the occupancy requirement.
    /// Reset to false by ResetZone at lap start.
    /// </summary>
    [HideInInspector]
    public bool boundaryDisarmed = false;

    /// <summary>
    /// Whether this zone is active (only check once per lap). Reset by ResetZone.
    /// </summary>
    public bool isActive = true;

    // High-precision stopwatch for accurate ms timing
    private Stopwatch occupancyTimer;

    // MQTT Channels
    private MQTTChannel occupancyMetChannel;
    private MQTTChannel occupancyFailedChannel;

    void Start()
    {
        occupancyTimer = new Stopwatch();
        occupancyMetChannel = new MQTTChannel("Gimbl/OccupancyMet/");
        occupancyFailedChannel = new MQTTChannel("Gimbl/OccupancyFailed/");
    }

    void Update()
    {
        if (!isActive || boundaryDisarmed)
            return;

        if (occupancyTimer.IsRunning && inZone)
        {
            if (occupancyTimer.ElapsedMilliseconds >= occupancyDurationMs)
            {
                OnOccupancyMet();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || boundaryDisarmed)
            return;

        inZone = true;
        occupancyTimer.Restart();
        UnityEngine.Debug.Log("OccupancyZone: Animal entered, timer started");
    }

    void OnTriggerExit(Collider other)
    {
        if (!isActive)
            return;

        inZone = false;
        occupancyTimer.Stop();

        if (!boundaryDisarmed)
        {
            OnOccupancyFailed();
        }
    }

    private void OnOccupancyMet()
    {
        UnityEngine.Debug.Log("OccupancyZone: Occupancy met - boundary disarmed");
        boundaryDisarmed = true;
        occupancyTimer.Stop();
        occupancyMetChannel.Send();
    }

    private void OnOccupancyFailed()
    {
        UnityEngine.Debug.Log("OccupancyZone: Occupancy failed - animal left early");
        occupancyFailedChannel.Send();
    }

    /// <summary>
    /// Resets the occupancy zone state for a new lap.
    /// Called by ResetZone when the animal enters the reset zone.
    /// </summary>
    public void ResetState()
    {
        isActive = true;
        boundaryDisarmed = false;
        inZone = false;
        occupancyTimer.Reset();
    }
}
