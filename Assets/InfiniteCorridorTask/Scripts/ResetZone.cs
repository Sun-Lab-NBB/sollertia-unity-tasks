using UnityEngine;

public class ResetZone : MonoBehaviour
{
    private StimulusTriggerZone[] stimulusTriggerZones;
    private OccupancyZone[] occupancyZones;

    void Start()
    {
        // Find all instances of zones in the scene
        stimulusTriggerZones = FindObjectsByType<StimulusTriggerZone>(FindObjectsSortMode.None);
        occupancyZones = FindObjectsByType<OccupancyZone>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// Called when actor enters reset zone. Resets all stimulus trigger zones and occupancy zones
    /// to their initial state for the new lap.
    /// </summary>
    public void OnTriggerEnter(Collider collider)
    {
        // Reset all stimulus trigger zones
        foreach (StimulusTriggerZone zone in stimulusTriggerZones)
        {
            zone.ResetState();
        }

        // Reset all occupancy zones
        foreach (OccupancyZone zone in occupancyZones)
        {
            zone.ResetState();
        }
    }
}
