using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gimbl;

public class LickStimulusSpawner : MonoBehaviour
{
    public GameObject lickPrefab;
    public GameObject stimulusPrefab;
    public Canvas canvas;
    // MQTT channels.
    private MQTTChannel lick;           // Subscribed to "LickPort/".
    private MQTTChannel stimulus;       // Subscribed to "Gimbl/Stimulus/"
    private bool showLick = false;      // Toggles lick msg creation.
    private bool showStimulus = false;  // Toggles stimulus msg creation.

    // Start is called before the first frame update
    void Start()
    {
        // Setup mqtt channel.
        lick = new MQTTChannel("LickPort/");
        lick.Event.AddListener(OnLick);
        stimulus = new MQTTChannel("Gimbl/Stimulus/");
        stimulus.Event.AddListener(OnStimulus);
    }

    void OnLick() { showLick = true; }
    void OnStimulus() { showStimulus = true; }
    private void CreateLickMsg()
    {
        showLick = false;
        Instantiate(lickPrefab, canvas.transform);
    }
    private void CreateStimulusMsg()
    {
        showStimulus = false;
        Instantiate(stimulusPrefab, canvas.transform);
    }
    // Update is called once per frame
    void Update()
    {
        if (showLick) { CreateLickMsg(); }
        if (showStimulus) { CreateStimulusMsg(); }
    }
}
