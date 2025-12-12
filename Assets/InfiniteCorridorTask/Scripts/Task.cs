using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Gimbl;
using UnityEngine.SceneManagement;
using SL.Config;

public class Task : MonoBehaviour
{
    // Some words for parts of the maze:
    //  Cue: A certain pattern on a wall
    //  Segment: A portion of the maze that cycles back to the start cue
    //  Corridor: A grouping of segments

    public Gimbl.ActorObject actor = null;
    public bool requireLick = false;

    // The track is infinite but need to specify how many random segments keep track of.
    // The track length should always be an overestimate to how far the animal is actually going to run.
    public float trackLength = 15000;

    // A seed for creation of random segments, a specific seed will always create the same pattern of cues.
    // If trackSeed is -1, then no seed will be used.
    public int trackSeed = -1;

    // Path to the YAML configuration file (relative to Application.dataPath)
    [System.NonSerialized]
    public string configPath;

    // For keeping track of where in the random sequence the animal is.
    private int current_segment_index;

    // Each time the animal completes a segment, it will go into a new random segment.
    // The segment sequence array holds the order of segments.
    private int[] segment_sequence_array;

    // Holds the order of cues
    private byte[] cue_sequence_array;

    // A wrapper class for sending cue_sequence_array over MQTT
    public class SequenceMsg
    {
        public byte[] cue_sequence;
    }
    private MQTTChannel cueSequenceTrigger;
    private MQTTChannel<SequenceMsg> cueSequenceChannel;

    private string sceneName;
    public class SceneNameMsg
    {
        public string name;
    }
    private MQTTChannel sceneNameTrigger;
    private MQTTChannel<SceneNameMsg> sceneNameChannel;

    private MQTTChannel requireLickTrue;
    private MQTTChannel requireLickFalse;

    private int depth;
    private int n_segments;
    private MesoscopeExperimentConfiguration config;

    private Dictionary<string, byte> cue_ids;
    private float[] segment_lengths;
    private float[] cue_lengths;

    private Dictionary<string, (float, float)> corridorMap;

    private List<int> cur_segment;
    private Vector3 pos;

    void OnValidate()
    {
        if (actor == null)
        {
            Gimbl.ActorObject[] all_actors = FindObjectsByType<Gimbl.ActorObject>(FindObjectsSortMode.None);
            if (all_actors.Length > 0)
            {
                actor = all_actors[0];
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (transform.position != Vector3.zero)
        {
            Debug.LogWarning($"Task is positioned at {transform.position}. Automatically Setting Task position to (0,0,0) for this runtime but it is recommended to permanently set the task position to (0,0,0) in Editor Mode.");
            transform.position = Vector3.zero;
        }

        string globalConfigPath = Application.dataPath + configPath;

        if (string.IsNullOrEmpty(configPath) || !File.Exists(globalConfigPath))
        {
            Debug.LogError("No configuration YAML file found at the specified path.");
            return;
        }

        // Load configuration from YAML
        config = ConfigLoader.Load(globalConfigPath);
        if (config == null)
        {
            Debug.LogError("Failed to load configuration from YAML file.");
            return;
        }

        n_segments = config.segments.Count;
        cue_ids = config.GetCueNameToCode();
        segment_lengths = config.GetSegmentLengthsUnity();
        cue_lengths = config.GetCueLengthsUnity();
        depth = config.vr_environment.segments_per_corridor;

        // To teleport the animal correctly between corridors, you need to know when to teleport
        // (ie when the first segment of the current corridor ends) and where to teleport
        // (ie where the first segment of the next corridor starts).
        // Corridor map holds this info, the first float is the position of the corridor
        // and the second float is the length of the first segment in the corridor.
        corridorMap = new Dictionary<string, (float, float)>();

        int[] corridor_segments = new int[depth];
        float cur_corridor_x = 0;
        float corridor_x_shift = config.vr_environment.CorridorSpacingUnity;

        for (int i = 0; i < Mathf.Pow(n_segments, depth); i++)
        {
            // Generate the combination for the current index
            for (int j = 0; j < depth; j++)
            {
                corridor_segments[j] = i / (int)Mathf.Pow(n_segments, depth - j - 1) % n_segments;
            }

            corridorMap[string.Join("-", corridor_segments)] = (cur_corridor_x, segment_lengths[corridor_segments[0]]);
            cur_corridor_x += corridor_x_shift;
        }

        // Create random sequence of segments
        (segment_sequence_array, cue_sequence_array) = GenerateRandomMaze(trackLength, trackSeed);

        // Figure out what the first corridor is from the first segments
        current_segment_index = 0;
        cur_segment = new List<int>(segment_sequence_array.Take(depth));

        if (actor != null)
        {
            pos = actor.transform.position;
            pos.x = corridorMap[string.Join("-", cur_segment)].Item1;
            actor.transform.position = pos;
        }

        // Create MQTT channels for sending cue sequence
        cueSequenceTrigger = new MQTTChannel("CueSequenceTrigger/", true);
        cueSequenceTrigger.Event.AddListener(OnCueSequenceTrigger);
        cueSequenceChannel = new MQTTChannel<SequenceMsg>("CueSequence/", false);

        // Create MQTT channels for sending the name of the active scene
        sceneName = SceneManager.GetActiveScene().name;
        sceneNameTrigger = new MQTTChannel("SceneNameTrigger/", true);
        sceneNameTrigger.Event.AddListener(OnSceneNameTrigger);
        sceneNameChannel = new MQTTChannel<SceneNameMsg>("SceneName/", false);

        // Create MQTT channel for toggling requireLick
        requireLickTrue = new MQTTChannel("RequireLick/True/", true);
        requireLickTrue.Event.AddListener(SetRequireLickTrue);

        requireLickFalse = new MQTTChannel("RequireLick/False/", true);
        requireLickFalse.Event.AddListener(SetRequireLickFalse);
    }

    // Update is called once per frame
    void Update()
    {
        if (actor != null)
        {
            pos = actor.transform.position;
            // Check if the animal has traveled through the entire segment
            if (pos.z > corridorMap[string.Join("-", cur_segment)].Item2)
            {
                // Teleport the animal back to the start of the corridors
                pos.z -= corridorMap[string.Join("-", cur_segment)].Item2;

                // Switch to a different corridor according to the future segments
                current_segment_index++;
                if (current_segment_index <= segment_sequence_array.Length - depth)
                {
                    cur_segment.RemoveAt(0);
                    cur_segment.Add(segment_sequence_array[current_segment_index + depth - 1]);
                }
                else
                {
                    throw new System.Exception("Animal ran through all generated segments.");
                }

                // Teleport the animal to the new corridor
                pos.x = corridorMap[string.Join("-", cur_segment)].Item1;
                actor.transform.position = pos;
            }
        }
        else
        {
            Debug.LogError("Actor is null.");
        }
    }

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

    /// <summary>
    /// Generates a random sequence of maze segments based on the specified length and optional seed.
    /// </summary>
    /// <param name="length">The total desired length of the maze sequence.</param>
    /// <param name="seed">An optional seed value for the random number generator. If -1, a new random generator is used.</param>
    /// <returns>
    /// A tuple containing two arrays:
    /// - An integer array representing the sequence of segments in the maze.
    /// - A byte array representing the cues associated with the maze sequence.
    /// </returns>
    private (int[], byte[]) GenerateRandomMaze(float length, int? seed = null)
    {
        float sequence_length = 0;

        System.Random random = seed.HasValue && seed != -1 ? new System.Random(seed.Value) : new System.Random();

        List<int> segment_sequence = new List<int>();
        List<byte> cue_sequence = new List<byte>();

        int choice = random.Next(n_segments);

        while (sequence_length < length)
        {
            segment_sequence.Add(choice);

            var segment = config.segments[choice];
            foreach (string cue in segment.cue_sequence)
            {
                cue_sequence.Add(cue_ids[cue]);
            }

            sequence_length += segment_lengths[choice];

            if (segment.HasTransitionProbabilities)
            {
                choice = SampleFromDistribution(segment.transition_probabilities.ToArray(), random);
            }
            else
            {
                choice = random.Next(n_segments);
            }
        }

        return (segment_sequence.ToArray(), cue_sequence.ToArray());
    }

    private void OnCueSequenceTrigger()
    {
        Debug.Log("received request for cue sequence");
        cueSequenceChannel.Send(new SequenceMsg() { cue_sequence = cue_sequence_array });
    }

    private void OnSceneNameTrigger()
    {
        sceneNameChannel.Send(new SceneNameMsg() { name = sceneName });
    }

    private void SetRequireLickTrue()
    {
        requireLick = true;
    }

    private void SetRequireLickFalse()
    {
        requireLick = false;
    }
}
