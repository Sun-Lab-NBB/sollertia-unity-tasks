/// <summary>
/// Provides data classes for parsing and accessing task templates from YAML files.
///
/// These classes mirror the Python task template classes from sl-shared-assets, containing
/// the data needed by Unity for VR corridor system prefab generation and runtime.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;

namespace SL.Config
{
    /// <summary>
    /// Defines a single visual cue used in the VR environment.
    /// Each cue has a unique name (used in segment definitions) and a unique uint8 code (for MQTT communication).
    /// Cues are not loaded as individual prefabs - they are baked into segment prefabs.
    /// </summary>
    [Serializable]
    public class Cue
    {
        /// <summary>The visual identifier for the cue (e.g., 'A', 'B', 'Gray'). Used in segment cue sequences.</summary>
        public string name;

        /// <summary>The unique uint8 code (0-255) used for MQTT communication and data analysis.</summary>
        public int code;

        /// <summary>The length of the cue in centimeters.</summary>
        public float length_cm;

        /// <summary>
        /// The texture filename (e.g., "Cue 001 - 2x1 repeat.png") located in
        /// Assets/InfiniteCorridorTask/Textures/. Applied 1:1 to the cue wall panels.
        /// </summary>
        public string texture;

        /// <summary>Returns the length in Unity units given a cm-per-unit conversion factor.</summary>
        public float LengthUnity(float cmPerUnit) => length_cm / cmPerUnit;
    }

    /// <summary>
    /// Defines a visual segment composed of a sequence of cues for the Unity corridor system.
    /// Segments are the building blocks of the infinite corridor, each containing a sequence of visual cues
    /// and optional transition probabilities for segment-to-segment transitions.
    /// </summary>
    [Serializable]
    public class Segment
    {
        /// <summary>The segment identifier, must match the Unity prefab name.</summary>
        public string name;

        /// <summary>The ordered sequence of cue names that comprise this segment.</summary>
        public List<string> cue_sequence;

        /// <summary>The optional transition probabilities to other segments. If provided, must sum to 1.0.</summary>
        public List<float> transition_probabilities;

        /// <summary>Determines whether transition probabilities are defined for this segment.</summary>
        public bool HasTransitionProbabilities =>
            transition_probabilities != null && transition_probabilities.Count > 0;
    }

    /// <summary>
    /// Defines the spatial configuration of a trial structure for Unity prefabs.
    /// Contains segment mapping, zone positions, and visibility settings.
    /// This mirrors the TrialStructure class from sl-shared-assets task_template_data module.
    /// </summary>
    [Serializable]
    public class TrialStructure
    {
        /// <summary>The name of the Unity Segment this trial structure is based on.</summary>
        public string segment_name;

        /// <summary>The position of the trial stimulus trigger zone starting boundary, in centimeters.</summary>
        public float stimulus_trigger_zone_start_cm;

        /// <summary>The position of the trial stimulus trigger zone ending boundary, in centimeters.</summary>
        public float stimulus_trigger_zone_end_cm;

        /// <summary>The location of the invisible boundary with which the animal must collide to elicit the stimulus.</summary>
        public float stimulus_location_cm;

        /// <summary>
        /// Determines whether the stimulus collision boundary is visible to the animal during this trial type.
        /// When True, the boundary marker is displayed in the VR environment at the stimulus location.
        /// </summary>
        public bool show_stimulus_collision_boundary = false;

        /// <summary>
        /// The trigger mode for the stimulus zone. "lick" uses the StimulusTriggerZone prefab with
        /// a GuidanceZone child. "occupancy" uses the OccupancyTriggerZone prefab with OccupancyZone
        /// and OccupancyGuidanceZone children.
        /// </summary>
        public string trigger_type;
    }

    /// <summary>
    /// Defines the Unity VR corridor system configuration.
    /// </summary>
    [Serializable]
    public class VREnvironment
    {
        /// <summary>The horizontal spacing between corridor instances in centimeters.</summary>
        public float corridor_spacing_cm = 20.0f;

        /// <summary>The number of segments visible in each corridor instance (corridor depth).</summary>
        public int segments_per_corridor = 3;

        /// <summary>The name of the Unity prefab used for corridor padding.</summary>
        public string padding_prefab_name = "Padding";

        /// <summary>The conversion factor from centimeters to Unity units.</summary>
        public float cm_per_unity_unit = 10.0f;

        /// <summary>Returns the corridor spacing in Unity units.</summary>
        public float CorridorSpacingUnity => corridor_spacing_cm / cm_per_unity_unit;
    }

    /// <summary>
    /// Defines a VR task template used by Unity for prefab generation and runtime configuration.
    /// This mirrors the TaskTemplate class from sl-shared-assets task_template_data module.
    /// The template name is derived from the YAML filename during loading.
    /// </summary>
    [Serializable]
    public class TaskTemplate
    {
        /// <summary>The list of visual cues used in the task.</summary>
        public List<Cue> cues;

        /// <summary>The list of visual segments for the Unity corridor system.</summary>
        public List<Segment> segments;

        /// <summary>
        /// The dictionary of trial structures mapping trial names to their spatial configurations.
        /// Keys are trial names (e.g., 'ABC'), values contain zone positions and visibility settings.
        /// </summary>
        public Dictionary<string, TrialStructure> trial_structures;

        /// <summary>The configuration for the Unity VR corridor system.</summary>
        public VREnvironment vr_environment;

        /// <summary>
        /// The offset of the animal's starting position relative to the VR environment's
        /// cue sequence origin, in centimeters.
        /// </summary>
        public float cue_offset_cm;

        /// <summary>
        /// The template name, derived from the YAML filename during loading.
        /// Also corresponds to the Unity scene name.
        /// </summary>
        public string template_name;

        /// <summary>Returns a map of cue name to byte code for MQTT encoding.</summary>
        public Dictionary<string, byte> GetCueNameToCode()
        {
            return cues.ToDictionary(c => c.name, c => (byte)c.code);
        }

        /// <summary>Returns a map of cue name to Cue.</summary>
        public Dictionary<string, Cue> GetCueByName()
        {
            return cues.ToDictionary(c => c.name, c => c);
        }

        /// <summary>Returns segment lengths in Unity units as an array.</summary>
        public float[] GetSegmentLengthsUnity()
        {
            Dictionary<string, Cue> cueMap = GetCueByName();
            float cmPerUnit = vr_environment.cm_per_unity_unit;
            return segments
                .Select(s => s.cue_sequence.Sum(cueName => cueMap[cueName].LengthUnity(cmPerUnit)))
                .ToArray();
        }

        /// <summary>Returns cue lengths in Unity units as an array.</summary>
        public float[] GetCueLengthsUnity()
        {
            float cmPerUnit = vr_environment.cm_per_unity_unit;
            return cues.Select(c => c.LengthUnity(cmPerUnit)).ToArray();
        }

        /// <summary>Returns a segment by name lookup dictionary.</summary>
        public Dictionary<string, Segment> GetSegmentByName()
        {
            return segments.ToDictionary(s => s.name, s => s);
        }

        /// <summary>Calculates the total length of a segment in Unity units.</summary>
        public float GetSegmentLengthUnity(string segmentName)
        {
            Segment segment = GetSegmentByName()[segmentName];
            Dictionary<string, Cue> cueMap = GetCueByName();
            float cmPerUnit = vr_environment.cm_per_unity_unit;
            return segment.cue_sequence.Sum(cueName => cueMap[cueName].LengthUnity(cmPerUnit));
        }

        /// <summary>
        /// Returns the trial structure associated with the given segment name, or null if none exists.
        /// </summary>
        public TrialStructure GetTrialStructureForSegment(string segmentName)
        {
            if (trial_structures == null)
            {
                return null;
            }

            foreach (TrialStructure trial in trial_structures.Values)
            {
                if (trial.segment_name == segmentName)
                {
                    return trial;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns whether the stimulus collision boundary should be visible for a given segment.
        /// Looks up the trial that references this segment and returns its visibility setting.
        /// Returns false if no trial references this segment.
        /// </summary>
        public bool GetSegmentMarkerVisibility(string segmentName)
        {
            if (trial_structures == null)
            {
                return false;
            }

            foreach (TrialStructure trial in trial_structures.Values)
            {
                if (trial.segment_name == segmentName)
                {
                    return trial.show_stimulus_collision_boundary;
                }
            }

            return false;
        }
    }
}
