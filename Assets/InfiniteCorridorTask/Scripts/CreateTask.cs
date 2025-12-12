using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using SL.Config;

public class CreateTask : MonoBehaviour
{
    [MenuItem("CreateTask/New Task")]
    public static void createTask()
    {
        // Open file dialog for YAML configuration file
        string configPath = EditorUtility.OpenFilePanel(
            "Select Experiment Configuration YAML",
            Application.dataPath + "/InfiniteCorridorTask/Tasks/",
            "yaml,yml"
        ).Replace(Application.dataPath, "");

        if (string.IsNullOrEmpty(configPath))
        {
            Debug.LogError("No configuration YAML file selected.");
            return;
        }

        // Load the configuration
        MesoscopeExperimentConfiguration config = ConfigLoader.Load(Application.dataPath + configPath);
        if (config == null)
        {
            Debug.LogError("Failed to load configuration from YAML file.");
            return;
        }

        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";

        // Load padding prefab
        string paddingPath = prefabsPath + config.vr_environment.padding_prefab_name + ".prefab";
        GameObject padding = AssetDatabase.LoadAssetAtPath<GameObject>(paddingPath);

        if (padding == null)
        {
            Debug.LogError("No padding found at " + paddingPath);
            return;
        }

        int n_segments = config.segments.Count;

        // Load segment prefabs
        GameObject[] segment_prefabs = new GameObject[n_segments];
        for (int i = 0; i < n_segments; i++)
        {
            string segmentPath = prefabsPath + config.segments[i].name + ".prefab";
            segment_prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(segmentPath);

            if (segment_prefabs[i] == null)
            {
                Debug.LogError("No segment found at " + segmentPath);
                return;
            }
        }

        // Measure actual prefab lengths and compare with config
        float[] measured_segment_lengths = Utility.get_segment_lengths(segment_prefabs);
        float[] segment_lengths = config.GetSegmentLengthsUnity();

        float epsilon = 0.01f;
        for (int i = 0; i < n_segments; i++)
        {
            if (Mathf.Abs(measured_segment_lengths[i] - segment_lengths[i]) > epsilon)
            {
                Debug.Log($"Warning: For {config.segments[i].name}, there is a mismatch between the prefab length ({measured_segment_lengths[i]}) and the sum of all the cue lengths ({segment_lengths[i]}). Using {segment_lengths[i]} for the length of the segment.");
            }
        }

        int depth = config.vr_environment.segments_per_corridor;
        float padding_z_shift = depth * Mathf.Min(segment_lengths) - 1;

        // Create task GameObject hierarchy
        string new_task_name = "newTask";
        GameObject task = new GameObject(new_task_name);
        Task task_script = task.AddComponent<Task>();
        task_script.requireLick = true;
        task_script.configPath = configPath;

        int[] corridor_segments = new int[depth];
        int segment;
        float cur_corridor_x = 0;
        float corridor_x_shift = config.vr_environment.CorridorSpacingUnity;
        float z_shift;

        // Iterate through all possible corridor combinations
        for (int i = 0; i < Mathf.Pow(n_segments, depth); i++)
        {
            // Generate the combination for the current index
            for (int j = 0; j < depth; j++)
            {
                corridor_segments[j] = i / (int)Mathf.Pow(n_segments, depth - j - 1) % n_segments;
            }

            GameObject corridor = new GameObject($"Corridor{string.Join("", corridor_segments)}");
            corridor.transform.SetParent(task.transform);
            corridor.transform.localPosition = new Vector3(cur_corridor_x, 0, 0);

            z_shift = 0;
            for (int j = 0; j < depth; j++)
            {
                segment = corridor_segments[j];
                GameObject instance = PrefabUtility.InstantiatePrefab(segment_prefabs[segment]) as GameObject;

                // Only the first segment in each corridor should have a stimulus trigger zone and reset zone
                // since the later segments are just for visual illusion
                if (j > 0)
                {
                    StimulusTriggerZone stimulus_trigger_zone = instance.GetComponentInChildren<StimulusTriggerZone>();
                    if (stimulus_trigger_zone != null)
                    {
                        GameObject.DestroyImmediate(stimulus_trigger_zone.gameObject);
                    }
                    ResetZone reset_zone = instance.GetComponentInChildren<ResetZone>();
                    if (reset_zone != null)
                    {
                        GameObject.DestroyImmediate(reset_zone.gameObject);
                    }
                }
                else
                {
                    // For the first segment, set the showBoundary from config's trial visibility setting
                    StimulusTriggerZone stimulus_trigger_zone = instance.GetComponentInChildren<StimulusTriggerZone>();
                    if (stimulus_trigger_zone != null)
                    {
                        string segmentName = config.segments[segment].name;
                        stimulus_trigger_zone.showBoundary = config.GetSegmentMarkerVisibility(segmentName);
                    }
                }

                instance.transform.SetParent(corridor.transform, false);
                instance.transform.localPosition += new Vector3(0, 0, z_shift);
                z_shift += segment_lengths[segment];
            }

            GameObject padding_instance = PrefabUtility.InstantiatePrefab(padding) as GameObject;
            padding_instance.transform.SetParent(corridor.transform, false);
            padding_instance.transform.localPosition += new Vector3(0, 0, padding_z_shift);

            cur_corridor_x += corridor_x_shift;
        }

        // Open Save File Panel for user to specify location and name of prefab
        string savePath = EditorUtility.SaveFilePanel(
            "Save Task Prefab",
            Application.dataPath + "/InfiniteCorridorTask/Tasks/",
            new_task_name + ".prefab",
            "prefab"
        );

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("User did not select a save location.");
            DestroyImmediate(task);
            return;
        }

        savePath = FileUtil.GetProjectRelativePath(savePath);
        PrefabUtility.SaveAsPrefabAsset(task, savePath);
        DestroyImmediate(task);

        Debug.Log($"Task prefab saved to {savePath}");
    }
}
