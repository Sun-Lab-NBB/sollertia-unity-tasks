using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidanceZone : MonoBehaviour
{

    [HideInInspector]
    public bool inZone = false;

    void Start()
    {

    }
    void OnTriggerEnter(Collider other)
    {
        inZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        inZone = false;
    }
}
