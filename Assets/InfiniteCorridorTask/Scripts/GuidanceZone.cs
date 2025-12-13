/// <summary>
/// Provides the GuidanceZone class that tracks whether an animal has entered a guidance trigger area.
///
/// Used as a child of StimulusTriggerZone to define where guidance mode delivers automatic stimulus.
/// When the animal reaches this zone in guidance mode, the parent StimulusTriggerZone delivers the stimulus.
/// </summary>
using UnityEngine;

/// <summary>
/// Tracks whether the animal is inside the guidance zone collider.
/// Used by parent StimulusTriggerZone to determine when to deliver automatic stimulus in guidance mode.
/// </summary>
public class GuidanceZone : MonoBehaviour
{
    /// <summary>Determines whether the animal is currently inside this guidance zone.</summary>
    [HideInInspector]
    public bool inZone = false;

    /// <summary>Called when the animal enters the guidance zone collider.</summary>
    void OnTriggerEnter(Collider other)
    {
        inZone = true;
    }

    /// <summary>Called when the animal exits the guidance zone collider.</summary>
    void OnTriggerExit(Collider other)
    {
        inZone = false;
    }
}
