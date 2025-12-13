/// <summary>
/// Provides the LickMsg class that displays a temporary UI message when a lick is detected.
/// </summary>
using UnityEngine;

/// <summary>
/// Self-destructing UI message component that displays briefly when a lick event occurs.
/// </summary>
public class LickMsg : MonoBehaviour
{
    /// <summary>The time in seconds before this message is destroyed.</summary>
    public float destroyTime = 1.0f;

    /// <summary>Schedules the destruction of this game object after the specified delay.</summary>
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
