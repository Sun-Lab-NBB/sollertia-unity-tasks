/// <summary>
/// Provides the CreateTask class that generates Task prefabs from YAML configuration files via Unity Editor menu.
/// </summary>
using System.IO;
using SL.Config;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor script that creates Task prefabs from experiment configuration files.
/// Generates all corridor combinations by instantiating segment prefabs and configuring zones.
/// </summary>
public class CreateTask : MonoBehaviour
{
    /// <summary>The tolerance for comparing measured prefab lengths against configured lengths.</summary>
    private const float LengthComparisonEpsilon = 0.01f;

    /// <summary>Creates a new Task prefab from a selected YAML configuration file.</summary>
    [MenuItem("CreateTask/New Task")]
    public static void CreateNewTask()
    {
        // Opens file dialog for YAML configuration file
        string configPath = EditorUtility
            .OpenFilePanel(
                "Select Experiment Configuration YAML",
                Application.dataPath + "/InfiniteCorridorTask/Configurations/",
                "yaml,yml"
            )
            .Replace(Application.dataPath, "");

        if (string.IsNullOrEmpty(configPath))
        {
            Debug.LogError("No configuration YAML file selected.");
            return;
        }

        // Loads and validates configuration
        MesoscopeExperimentConfiguration config = ConfigLoader.Load(Application.dataPath + configPath);
        if (config == null)
        {
            Debug.LogError("Failed to load configuration from YAML file.");
            return;
        }

        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";

        // Loads padding prefab
        string paddingPath = prefabsPath + config.vr_environment.padding_prefab_name + ".prefab";
        GameObject padding = AssetDatabase.LoadAssetAtPath<GameObject>(paddingPath);

        if (padding == null)
        {
            Debug.LogError("No padding found at " + paddingPath);
            return;
        }

        int nSegments = config.segments.Count;

        // Loads segment prefabs
        GameObject[] segmentPrefabs = new GameObject[nSegments];
        for (int i = 0; i < nSegments; i++)
        {
            string segmentPath = prefabsPath + config.segments[i].name + ".prefab";
            segmentPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(segmentPath);

            if (segmentPrefabs[i] == null)
            {
                Debug.LogError("No segment found at " + segmentPath);
                return;
            }
        }

        // Measures actual prefab lengths and compares with configuration
        float[] measuredSegmentLengths = Utility.GetSegmentLengths(segmentPrefabs);
        float[] segmentLengths = config.GetSegmentLengthsUnity();

        for (int i = 0; i < nSegments; i++)
        {
            if (Mathf.Abs(measuredSegmentLengths[i] - segmentLengths[i]) > LengthComparisonEpsilon)
            {
                Debug.Log(
                    $"Warning: For {config.segments[i].name}, there is a mismatch between the prefab length "
                        + $"({measuredSegmentLengths[i]}) and the sum of all the cue lengths ({segmentLengths[i]}). "
                        + $"Using {segmentLengths[i]} for the length of the segment."
                );
            }
        }

        int depth = config.vr_environment.segments_per_corridor;
        float paddingZShift = depth * Mathf.Min(segmentLengths) - 1;

        // Creates task GameObject hierarchy
        string taskName = "newTask";
        GameObject task = new GameObject(taskName);
        Task taskScript = task.AddComponent<Task>();
        taskScript.requireLick = true;
        taskScript.configPath = configPath;

        int[] corridorSegments = new int[depth];
        int segment;
        float curCorridorX = 0;
        float corridorXShift = config.vr_environment.CorridorSpacingUnity;
        float zShift;

        // Iterates through all possible corridor combinations
        for (int i = 0; i < Mathf.Pow(nSegments, depth); i++)
        {
            // Generates the combination for the current index
            for (int j = 0; j < depth; j++)
            {
                corridorSegments[j] = i / (int)Mathf.Pow(nSegments, depth - j - 1) % nSegments;
            }

            GameObject corridor = new GameObject($"Corridor{string.Join("", corridorSegments)}");
            corridor.transform.SetParent(task.transform);
            corridor.transform.localPosition = new Vector3(curCorridorX, 0, 0);

            zShift = 0;
            for (int j = 0; j < depth; j++)
            {
                segment = corridorSegments[j];
                GameObject instance = PrefabUtility.InstantiatePrefab(segmentPrefabs[segment]) as GameObject;

                // Only the first segment in each corridor should have a stimulus trigger zone and reset zone
                // since the later segments are just for visual illusion
                if (j > 0)
                {
                    StimulusTriggerZone stimulusTriggerZone = instance.GetComponentInChildren<StimulusTriggerZone>();
                    if (stimulusTriggerZone != null)
                    {
                        DestroyImmediate(stimulusTriggerZone.gameObject);
                    }

                    ResetZone resetZone = instance.GetComponentInChildren<ResetZone>();
                    if (resetZone != null)
                    {
                        DestroyImmediate(resetZone.gameObject);
                    }
                }
                else
                {
                    // For the first segment, sets showBoundary from config's trial visibility setting
                    StimulusTriggerZone stimulusTriggerZone = instance.GetComponentInChildren<StimulusTriggerZone>();
                    if (stimulusTriggerZone != null)
                    {
                        string segmentName = config.segments[segment].name;
                        stimulusTriggerZone.showBoundary = config.GetSegmentMarkerVisibility(segmentName);
                    }
                }

                instance.transform.SetParent(corridor.transform, false);
                instance.transform.localPosition += new Vector3(0, 0, zShift);
                zShift += segmentLengths[segment];
            }

            GameObject paddingInstance = PrefabUtility.InstantiatePrefab(padding) as GameObject;
            paddingInstance.transform.SetParent(corridor.transform, false);
            paddingInstance.transform.localPosition += new Vector3(0, 0, paddingZShift);

            curCorridorX += corridorXShift;
        }

        // Opens save file panel for user to specify location and name of prefab
        string savePath = EditorUtility.SaveFilePanel(
            "Save Task Prefab",
            Application.dataPath + "/InfiniteCorridorTask/Tasks/",
            taskName + ".prefab",
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
