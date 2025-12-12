/// <summary>
/// Provides the StimulusMsg class that displays a temporary UI message when a stimulus is delivered.
/// </summary>
using UnityEngine;

/// <summary>
/// Self-destructing UI message component that displays briefly when a stimulus event occurs.
/// </summary>
public class StimulusMsg : MonoBehaviour
{
    /// <summary>The time in seconds before this message is destroyed.</summary>
    public float destroyTime = 4.0f;

    /// <summary>Schedules the destruction of this game object after the specified delay.</summary>
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
