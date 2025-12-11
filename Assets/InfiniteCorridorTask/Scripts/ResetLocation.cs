using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetLocation : MonoBehaviour
{
    private RewardLocation[] rewardLocations; // Array to hold all instances of RewardLocation

    // Start is called before the first frame update
    void Start()
    {
        // Find all instances of RewardLocation in the scene
        rewardLocations = FindObjectsByType<RewardLocation>(FindObjectsSortMode.None);
    }

    // Called when actor enters reset location.
    public void OnTriggerEnter(Collider collider)
    {
        // Loop through all reward locations and update their isActive state
        foreach (RewardLocation rewardLocation in rewardLocations)
        {
            // Set marker visible/invisible based on per-location showMarker setting from config
            rewardLocation.GetComponent<MeshRenderer>().enabled = rewardLocation.showMarker;
            rewardLocation.isActive = true; // Activate each reward location
        }
    }
}
