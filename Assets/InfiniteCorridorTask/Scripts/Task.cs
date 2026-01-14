/// <summary>
/// Provides the Task class that manages the infinite corridor VR environment for mesoscope experiments.
///
/// Controls the generation and cycling of random maze segments, manages animal position
/// within the corridor system, and handles MQTT communication for cue sequences and scene information.
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gimbl;
using SL.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the infinite corridor VR task, managing segment generation, animal positioning, and MQTT communication.
/// </summary>
/// <remarks>
/// Terminology:
/// - Cue: A visual pattern displayed on the corridor walls.
/// - Segment: A portion of the maze composed of a sequence of cues.
/// - Corridor: A grouping of adjacent segments forming a visual unit.
/// </remarks>
public class Task : MonoBehaviour
{
    /// <summary>The actor (animal) being tracked in the VR environment.</summary>
    public ActorObject actor = null;

    /// <summary>Determines whether the animal must lick to receive a reward (lick guidance mode toggle).</summary>
    public bool requireLick = false;

    /// <summary>Determines whether the animal must wait in the occupancy zone (occupancy guidance mode toggle).</summary>
    public bool requireWait = false;

    /// <summary>
    /// The total length of the pre-generated random segment sequence.
    /// Should overestimate the distance the animal will actually travel.
    /// </summary>
    public float trackLength = 15000;

    /// <summary>
    /// The seed for random segment generation. A specific seed produces the same cue pattern.
    /// Set to -1 to use a random seed.
    /// </summary>
    public int trackSeed = -1;

    /// <summary>The path to the YAML configuration file, relative to Application.dataPath.</summary>
    public string configPath;

    /// <summary>The current index in the segment sequence array.</summary>
    private int _currentSegmentIndex;

    /// <summary>The array holding the order of randomly generated segments.</summary>
    private int[] _segmentSequenceArray;

    /// <summary>The array holding the flattened cue codes for the entire sequence.</summary>
    private byte[] _cueSequenceArray;

    /// <summary>Wrapper class for sending cue sequence over MQTT.</summary>
    public class SequenceMsg
    {
        public byte[] cue_sequence;
    }

    /// <summary>The MQTT channel that triggers sending the cue sequence.</summary>
    private MQTTChannel _cueSequenceTrigger;

    /// <summary>The MQTT channel for sending the cue sequence data.</summary>
    private MQTTChannel<SequenceMsg> _cueSequenceChannel;

    /// <summary>The name of the currently active Unity scene.</summary>
    private string _sceneName;

    /// <summary>Wrapper class for sending scene name over MQTT.</summary>
    public class SceneNameMsg
    {
        public string name;
    }

    /// <summary>The MQTT channel that triggers sending the scene name.</summary>
    private MQTTChannel _sceneNameTrigger;

    /// <summary>The MQTT channel for sending the scene name data.</summary>
    private MQTTChannel<SceneNameMsg> _sceneNameChannel;

    /// <summary>The MQTT channel for enabling lick requirement (lick guidance mode off).</summary>
    private MQTTChannel _requireLickTrue;

    /// <summary>The MQTT channel for disabling lick requirement (lick guidance mode on).</summary>
    private MQTTChannel _requireLickFalse;

    /// <summary>The MQTT channel for enabling wait requirement (occupancy guidance mode off).</summary>
    private MQTTChannel _requireWaitTrue;

    /// <summary>The MQTT channel for disabling wait requirement (occupancy guidance mode on).</summary>
    private MQTTChannel _requireWaitFalse;

    /// <summary>The number of segments visible in each corridor (corridor depth).</summary>
    private int _depth;

    /// <summary>The total number of unique segment types.</summary>
    private int _segmentCount;

    /// <summary>The loaded experiment configuration.</summary>
    private MesoscopeExperimentConfiguration _config;

    /// <summary>The mapping of cue names to their byte codes.</summary>
    private Dictionary<string, byte> _cueIds;

    /// <summary>The lengths of each segment type in Unity units.</summary>
    private float[] _segmentLengths;

    /// <summary>The lengths of each cue type in Unity units.</summary>
    private float[] _cueLengths;

    /// <summary>
    /// Maps corridor ID string to (x-position, first segment length).
    /// Used for teleporting the animal between corridors.
    /// </summary>
    private Dictionary<string, (float, float)> _corridorMap;

    /// <summary>The current corridor segment indices.</summary>
    private List<int> _curSegment;

    /// <summary>The cached actor position for updates.</summary>
    private Vector3 _position;

    /// <summary>Validates and auto-assigns the actor reference in the editor.</summary>
    void OnValidate()
    {
        if (actor == null)
        {
            ActorObject[] allActors = FindObjectsByType<ActorObject>(FindObjectsSortMode.None);
            if (allActors.Length > 0)
            {
                actor = allActors[0];
            }
        }
    }

    /// <summary>Initializes the task, loads configuration, and sets up MQTT channels.</summary>
    void Start()
    {
        // Warns if Task is not at origin
        if (transform.position != Vector3.zero)
        {
            Debug.LogWarning(
                $"Task is positioned at {transform.position}. Automatically Setting Task position to "
                    + "(0,0,0) for this runtime but it is recommended to permanently set the task position to "
                    + "(0,0,0) in Editor Mode."
            );
            transform.position = Vector3.zero;
        }

        string globalConfigPath = Application.dataPath + configPath;

        if (string.IsNullOrEmpty(configPath) || !File.Exists(globalConfigPath))
        {
            Debug.LogError("No configuration YAML file found at the specified path.");
            return;
        }

        // Loads and validates configuration
        _config = ConfigLoader.Load(globalConfigPath);
        if (_config == null)
        {
            Debug.LogError("Failed to load configuration from YAML file.");
            return;
        }

        // Extracts configuration values
        _segmentCount = _config.segments.Count;
        _cueIds = _config.GetCueNameToCode();
        _segmentLengths = _config.GetSegmentLengthsUnity();
        _cueLengths = _config.GetCueLengthsUnity();
        _depth = _config.vr_environment.segments_per_corridor;

        // Builds corridor map for teleportation.
        // Maps corridor segment combination to (x-position, first segment length).
        _corridorMap = new Dictionary<string, (float, float)>();

        int[] corridorSegments = new int[_depth];
        float curCorridorX = 0;
        float corridorXShift = _config.vr_environment.CorridorSpacingUnity;

        for (int i = 0; i < Mathf.Pow(_segmentCount, _depth); i++)
        {
            // Generates segment combination for current corridor index
            for (int j = 0; j < _depth; j++)
            {
                corridorSegments[j] = i / (int)Mathf.Pow(_segmentCount, _depth - j - 1) % _segmentCount;
            }

            _corridorMap[string.Join("-", corridorSegments)] = (curCorridorX, _segmentLengths[corridorSegments[0]]);
            curCorridorX += corridorXShift;
        }

        // Generates random maze sequence
        (_segmentSequenceArray, _cueSequenceArray) = GenerateRandomMaze(trackLength, trackSeed);

        // Initializes current segment tracking
        _currentSegmentIndex = 0;
        _curSegment = new List<int>(_segmentSequenceArray.Take(_depth));

        // Positions actor at the first corridor
        if (actor != null)
        {
            string corridorKey = string.Join("-", _curSegment);
            if (_corridorMap.TryGetValue(corridorKey, out var corridorData))
            {
                _position = actor.transform.position;
                _position.x = corridorData.Item1;
                actor.transform.position = _position;
            }
            else
            {
                Debug.LogError($"Task: Corridor key '{corridorKey}' not found in corridor map");
            }
        }

        // Sets up MQTT channels for cue sequence requests
        _cueSequenceTrigger = new MQTTChannel("CueSequenceTrigger/", true);
        _cueSequenceTrigger.Event.AddListener(OnCueSequenceTrigger);
        _cueSequenceChannel = new MQTTChannel<SequenceMsg>("CueSequence/", false);

        // Sets up MQTT channels for scene name requests
        _sceneName = SceneManager.GetActiveScene().name;
        _sceneNameTrigger = new MQTTChannel("SceneNameTrigger/", true);
        _sceneNameTrigger.Event.AddListener(OnSceneNameTrigger);
        _sceneNameChannel = new MQTTChannel<SceneNameMsg>("SceneName/", false);

        // Sets up MQTT channels for lick guidance mode control
        _requireLickTrue = new MQTTChannel("RequireLick/True/", true);
        _requireLickTrue.Event.AddListener(SetRequireLickTrue);

        _requireLickFalse = new MQTTChannel("RequireLick/False/", true);
        _requireLickFalse.Event.AddListener(SetRequireLickFalse);

        // Sets up MQTT channels for occupancy guidance mode control
        _requireWaitTrue = new MQTTChannel("RequireWait/True/", true);
        _requireWaitTrue.Event.AddListener(SetRequireWaitTrue);

        _requireWaitFalse = new MQTTChannel("RequireWait/False/", true);
        _requireWaitFalse.Event.AddListener(SetRequireWaitFalse);
    }

    /// <summary>Checks animal position and handles corridor transitions each frame.</summary>
    void Update()
    {
        if (actor == null)
            return;

        string corridorKey = string.Join("-", _curSegment);
        if (!_corridorMap.TryGetValue(corridorKey, out var corridorData))
        {
            Debug.LogError($"Task: Corridor key '{corridorKey}' not found in corridor map");
            return;
        }

        _position = actor.transform.position;

        // Checks if animal has traveled through the current segment
        if (_position.z > corridorData.Item2)
        {
            // Teleports animal back to start of corridor
            _position.z -= corridorData.Item2;

            // Advances to next corridor based on future segments
            _currentSegmentIndex++;
            if (_currentSegmentIndex <= _segmentSequenceArray.Length - _depth)
            {
                _curSegment.RemoveAt(0);
                _curSegment.Add(_segmentSequenceArray[_currentSegmentIndex + _depth - 1]);
            }
            else
            {
                throw new Exception("Animal ran through all generated segments.");
            }

            // Teleports to new corridor
            string newCorridorKey = string.Join("-", _curSegment);
            if (_corridorMap.TryGetValue(newCorridorKey, out var newCorridorData))
            {
                _position.x = newCorridorData.Item1;
                actor.transform.position = _position;
            }
            else
            {
                Debug.LogError($"Task: New corridor key '{newCorridorKey}' not found in corridor map");
            }
        }
    }

    /// <summary>Samples an index from a probability distribution.</summary>
    /// <param name="probabilities">The array of probabilities that must sum to 1.0.</param>
    /// <param name="random">The random number generator instance.</param>
    /// <returns>The sampled index.</returns>
    private int SampleFromDistribution(float[] probabilities, System.Random random)
    {
        float r = (float)random.NextDouble();
        float cumulative = 0f;

        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (r < cumulative)
                return i;
        }

        return probabilities.Length - 1;
    }

    /// <summary>Generates a random sequence of maze segments based on the specified length and optional seed.</summary>
    /// <param name="length">The total desired length of the maze sequence in Unity units.</param>
    /// <param name="seed">The optional seed for random number generator. Use -1 for random seed.</param>
    /// <returns>A tuple containing (segment indices array, flattened cue codes array).</returns>
    private (int[], byte[]) GenerateRandomMaze(float length, int? seed = null)
    {
        float sequenceLength = 0;

        System.Random random = seed.HasValue && seed != -1 ? new System.Random(seed.Value) : new System.Random();

        List<int> segmentSequence = new List<int>();
        List<byte> cueSequence = new List<byte>();

        int choice = random.Next(_segmentCount);

        while (sequenceLength < length)
        {
            segmentSequence.Add(choice);

            Segment segment = _config.segments[choice];
            foreach (string cue in segment.cue_sequence)
            {
                cueSequence.Add(_cueIds[cue]);
            }

            sequenceLength += _segmentLengths[choice];

            // Uses transition probabilities if defined, otherwise uniform random
            if (segment.HasTransitionProbabilities)
            {
                choice = SampleFromDistribution(segment.transition_probabilities.ToArray(), random);
            }
            else
            {
                choice = random.Next(_segmentCount);
            }
        }

        return (segmentSequence.ToArray(), cueSequence.ToArray());
    }

    /// <summary>MQTT callback that sends the cue sequence when requested.</summary>
    private void OnCueSequenceTrigger()
    {
        Debug.Log("Task: Received request for cue sequence");
        _cueSequenceChannel.Send(new SequenceMsg() { cue_sequence = _cueSequenceArray });
    }

    /// <summary>MQTT callback that sends the scene name when requested.</summary>
    private void OnSceneNameTrigger()
    {
        _sceneNameChannel.Send(new SceneNameMsg() { name = _sceneName });
    }

    /// <summary>MQTT callback that enables lick requirement (disables lick guidance mode).</summary>
    private void SetRequireLickTrue()
    {
        requireLick = true;
    }

    /// <summary>MQTT callback that disables lick requirement (enables lick guidance mode).</summary>
    private void SetRequireLickFalse()
    {
        requireLick = false;
    }

    /// <summary>MQTT callback that enables wait requirement (disables occupancy guidance mode).</summary>
    private void SetRequireWaitTrue()
    {
        requireWait = true;
    }

    /// <summary>MQTT callback that disables wait requirement (enables occupancy guidance mode).</summary>
    private void SetRequireWaitFalse()
    {
        requireWait = false;
    }
}
