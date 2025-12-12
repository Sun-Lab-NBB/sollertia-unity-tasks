/// <summary>
/// Provides the LickStimulusSpawner class that spawns UI messages in response to MQTT events.
/// </summary>
using Gimbl;
using UnityEngine;

/// <summary>
/// Listens for lick and stimulus MQTT messages and spawns corresponding UI message prefabs.
/// </summary>
public class LickStimulusSpawner : MonoBehaviour
{
    /// <summary>The prefab to instantiate when a lick is detected.</summary>
    public GameObject lickPrefab;

    /// <summary>The prefab to instantiate when a stimulus is delivered.</summary>
    public GameObject stimulusPrefab;

    /// <summary>The canvas where UI message prefabs will be spawned.</summary>
    public Canvas canvas;

    /// <summary>The MQTT channel for receiving lick detection messages.</summary>
    private MQTTChannel _lick;

    /// <summary>The MQTT channel for receiving stimulus delivery messages.</summary>
    private MQTTChannel _stimulus;

    /// <summary>Determines whether a lick message should be shown on the next Update.</summary>
    private bool _showLick = false;

    /// <summary>Determines whether a stimulus message should be shown on the next Update.</summary>
    private bool _showStimulus = false;

    /// <summary>Sets up MQTT channels and registers event listeners.</summary>
    void Start()
    {
        // Sets up MQTT channels
        _lick = new MQTTChannel("LickPort/");
        _lick.Event.AddListener(OnLick);
        _stimulus = new MQTTChannel("Gimbl/Stimulus/");
        _stimulus.Event.AddListener(OnStimulus);
    }

    /// <summary>Checks for pending messages and spawns UI prefabs on the main thread.</summary>
    void Update()
    {
        if (_showLick)
        {
            CreateLickMsg();
        }

        if (_showStimulus)
        {
            CreateStimulusMsg();
        }
    }

    /// <summary>MQTT callback that flags a lick message to be shown.</summary>
    private void OnLick()
    {
        _showLick = true;
    }

    /// <summary>MQTT callback that flags a stimulus message to be shown.</summary>
    private void OnStimulus()
    {
        _showStimulus = true;
    }

    /// <summary>Instantiates the lick message prefab on the canvas.</summary>
    private void CreateLickMsg()
    {
        _showLick = false;
        Instantiate(lickPrefab, canvas.transform);
    }

    /// <summary>Instantiates the stimulus message prefab on the canvas.</summary>
    private void CreateStimulusMsg()
    {
        _showStimulus = false;
        Instantiate(stimulusPrefab, canvas.transform);
    }
}
