/// <summary>
/// Provides the ConfigLoader class for loading and validating task templates from YAML files.
/// </summary>
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SL.Config
{
    /// <summary>
    /// Loads and validates task templates from YAML files.
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>Loads a TaskTemplate from a YAML file and derives the template name from the filename.</summary>
        /// <param name="filePath">The absolute path to the YAML template file.</param>
        /// <returns>The parsed template with template_name populated, or null if loading fails.</returns>
        public static TaskTemplate LoadTemplate(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Template file not found: {filePath}");
                return null;
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            string yaml = File.ReadAllText(filePath);
            TaskTemplate template = deserializer.Deserialize<TaskTemplate>(yaml);

            if (!ValidateTemplate(template))
            {
                return null;
            }

            // Derives template name from filename (without extension)
            template.template_name = Path.GetFileNameWithoutExtension(filePath);

            return template;
        }

        /// <summary>Validates the loaded template for required fields and data integrity.</summary>
        /// <param name="template">The template to validate.</param>
        /// <returns>True if the template is valid, false otherwise.</returns>
        private static bool ValidateTemplate(TaskTemplate template)
        {
            if (template == null)
            {
                Debug.LogError("Failed to parse template file.");
                return false;
            }

            if (template.cues == null || template.cues.Count == 0)
            {
                Debug.LogError("No cues defined in template.");
                return false;
            }

            if (template.segments == null || template.segments.Count == 0)
            {
                Debug.LogError("No segments defined in template.");
                return false;
            }

            if (template.vr_environment == null)
            {
                Debug.LogError("No VR environment configuration defined.");
                return false;
            }

            // Validates cue codes are unique and within uint8 range
            HashSet<int> seenCodes = new HashSet<int>();
            HashSet<string> seenNames = new HashSet<string>();

            foreach (Cue cue in template.cues)
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

            // Validates segment cue sequences reference valid cues
            HashSet<string> segmentNames = new HashSet<string>();
            foreach (Segment segment in template.segments)
            {
                segmentNames.Add(segment.name);

                if (segment.cue_sequence == null || segment.cue_sequence.Count == 0)
                {
                    Debug.LogError($"Segment '{segment.name}' has no cue sequence.");
                    return false;
                }

                foreach (string cueName in segment.cue_sequence)
                {
                    if (!seenNames.Contains(cueName))
                    {
                        Debug.LogError($"Segment '{segment.name}' references unknown cue '{cueName}'.");
                        return false;
                    }
                }

                // Validates transition probabilities sum to 1.0 if provided
                if (segment.transition_probabilities != null && segment.transition_probabilities.Count > 0)
                {
                    float sum = 0f;
                    foreach (float p in segment.transition_probabilities)
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

            // Validates trial structures reference valid segments
            if (template.trial_structures != null)
            {
                foreach (var kvp in template.trial_structures)
                {
                    string trialName = kvp.Key;
                    TrialStructure trial = kvp.Value;

                    if (!segmentNames.Contains(trial.segment_name))
                    {
                        Debug.LogError($"Trial '{trialName}' references unknown segment '{trial.segment_name}'.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
