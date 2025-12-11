using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SL.Config
{
    /// <summary>
    /// Loads experiment configuration from YAML files.
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// Loads a MesoscopeExperimentConfiguration from a YAML file.
        /// </summary>
        /// <param name="filePath">The absolute path to the YAML configuration file.</param>
        /// <returns>The parsed configuration, or null if loading fails.</returns>
        public static MesoscopeExperimentConfiguration Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Configuration file not found: {filePath}");
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            string yaml = File.ReadAllText(filePath);
            var config = deserializer.Deserialize<MesoscopeExperimentConfiguration>(yaml);

            if (!Validate(config))
            {
                return null;
            }

            return config;
        }

        /// <summary>
        /// Validates the loaded configuration.
        /// </summary>
        private static bool Validate(MesoscopeExperimentConfiguration config)
        {
            if (config == null)
            {
                Debug.LogError("Failed to parse configuration file.");
                return false;
            }

            if (config.cues == null || config.cues.Count == 0)
            {
                Debug.LogError("No cues defined in configuration.");
                return false;
            }

            if (config.segments == null || config.segments.Count == 0)
            {
                Debug.LogError("No segments defined in configuration.");
                return false;
            }

            if (config.vr_environment == null)
            {
                Debug.LogError("No VR environment configuration defined.");
                return false;
            }

            // Validate cue codes are unique and within uint8 range
            var seenCodes = new System.Collections.Generic.HashSet<int>();
            var seenNames = new System.Collections.Generic.HashSet<string>();

            foreach (var cue in config.cues)
            {
                if (cue.code < 0 || cue.code > 255)
                {
                    Debug.LogError($"Cue '{cue.name}' has invalid code {cue.code}. Must be 0-255.");
                    return false;
                }

                if (!seenCodes.Add(cue.code))
                {
                    Debug.LogError($"Duplicate cue code {cue.code} found.");
                    return false;
                }

                if (!seenNames.Add(cue.name))
                {
                    Debug.LogError($"Duplicate cue name '{cue.name}' found.");
                    return false;
                }

                if (cue.length_cm <= 0)
                {
                    Debug.LogError($"Cue '{cue.name}' has invalid length {cue.length_cm}. Must be positive.");
                    return false;
                }
            }

            // Validate segment cue sequences reference valid cues
            foreach (var segment in config.segments)
            {
                if (segment.cue_sequence == null || segment.cue_sequence.Count == 0)
                {
                    Debug.LogError($"Segment '{segment.name}' has no cue sequence.");
                    return false;
                }

                foreach (var cueName in segment.cue_sequence)
                {
                    if (!seenNames.Contains(cueName))
                    {
                        Debug.LogError($"Segment '{segment.name}' references unknown cue '{cueName}'.");
                        return false;
                    }
                }

                // Validate transition probabilities if provided
                if (segment.transition_probabilities != null && segment.transition_probabilities.Count > 0)
                {
                    float sum = 0f;
                    foreach (var p in segment.transition_probabilities)
                    {
                        sum += p;
                    }

                    if (Mathf.Abs(sum - 1.0f) > 0.001f)
                    {
                        Debug.LogError($"Segment '{segment.name}' transition probabilities sum to {sum}, must be 1.0.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
