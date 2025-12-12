/// <summary>
/// Provides utility functions for measuring prefab dimensions.
/// </summary>
using UnityEngine;

/// <summary>
/// Static utility class for prefab measurements and other helper functions.
/// </summary>
public class Utility : MonoBehaviour
{
    /// <summary>Calculates the z-axis lengths of an array of segment prefabs.</summary>
    /// <param name="segmentPrefabs">The array of segment prefab GameObjects.</param>
    /// <returns>An array of lengths corresponding to each prefab's z-axis extent.</returns>
    public static float[] GetSegmentLengths(GameObject[] segmentPrefabs)
    {
        int nSegments = segmentPrefabs.Length;
        float[] segmentLengths = new float[nSegments];

        for (int i = 0; i < nSegments; i++)
        {
            segmentLengths[i] = GetPrefabLength(segmentPrefabs[i]);
        }

        return segmentLengths;
    }

    /// <summary>Calculates the z-axis length of a prefab by combining all child renderer bounds.</summary>
    /// <param name="prefab">The prefab GameObject to measure.</param>
    /// <returns>The z-axis size of the combined bounds.</returns>
    public static float GetPrefabLength(GameObject prefab)
    {
        // Gets all Renderers in the prefab
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();

        // Calculates the combined bounds
        Bounds combinedBounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // Returns the z-axis size of the prefab
        Vector3 size = combinedBounds.size;
        return size.z;
    }
}
