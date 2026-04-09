/// <summary>
/// Provides the CreateTask class that generates Task prefabs from YAML configuration files via Unity Editor menu.
/// </summary>
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SL.Config;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor script that creates Task prefabs from task template files.
/// Generates all corridor combinations by instantiating segment prefabs and configuring zones.
/// </summary>
public class CreateTask : MonoBehaviour
{
    /// <summary>The tolerance for comparing measured prefab lengths against configured lengths.</summary>
    private const float LengthComparisonEpsilon = 0.01f;

    /// <summary>Creates a new Task prefab from a selected YAML configuration file via the Editor menu.</summary>
    [MenuItem("CreateTask/New Task")]
    public static void CreateNewTask()
    {
        // Opens file dialog for YAML task template file
        string configPath = EditorUtility
            .OpenFilePanel(
                "Select Task Template YAML",
                Application.dataPath + "/InfiniteCorridorTask/Configurations/",
                "yaml,yml"
            )
            .Replace(Application.dataPath, "");

        if (string.IsNullOrEmpty(configPath))
        {
            Debug.LogError("No configuration YAML file selected.");
            return;
        }

        // Opens save file panel for user to specify location and name of prefab
        string savePath = EditorUtility.SaveFilePanel(
            "Save Task Prefab",
            Application.dataPath + "/InfiniteCorridorTask/Tasks/",
            "newTask.prefab",
            "prefab"
        );

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("User did not select a save location.");
            return;
        }

        savePath = FileUtil.GetProjectRelativePath(savePath);
        string result = CreateFromTemplate(Application.dataPath + configPath, configPath, savePath);
        Debug.Log(result);
    }

    /// <summary>
    /// Creates a Task prefab from a YAML template file and saves it to the specified path.
    /// This is the parameterized entry point used by both the Editor menu and the MCP bridge.
    /// </summary>
    /// <param name="absoluteTemplatePath">The absolute path to the YAML template file.</param>
    /// <param name="relativeConfigPath">
    /// The config path relative to Application.dataPath, stored on the Task component for runtime loading.
    /// </param>
    /// <param name="savePath">The project-relative path where the prefab will be saved (e.g., "Assets/.../Task.prefab").</param>
    /// <returns>A status message describing success or the error encountered.</returns>
    public static string CreateFromTemplate(
        string absoluteTemplatePath,
        string relativeConfigPath,
        string savePath
    )
    {
        // Loads and validates task template
        TaskTemplate template = ConfigLoader.LoadTemplate(absoluteTemplatePath);
        if (template == null)
        {
            return "error: Failed to load task template from YAML file.";
        }

        // Builds cue and segment prefabs from template data when they do not already exist
        if (!BuildCuePrefabs(template))
        {
            return "error: Failed to build cue prefabs.";
        }

        if (!BuildSegmentPrefabs(template))
        {
            return "error: Failed to build segment prefabs.";
        }

        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";

        // Loads padding prefab
        string paddingPath = prefabsPath + template.vr_environment.padding_prefab_name + ".prefab";
        GameObject padding = AssetDatabase.LoadAssetAtPath<GameObject>(paddingPath);

        if (padding == null)
        {
            return "error: No padding found at " + paddingPath;
        }

        int nSegments = template.segments.Count;

        // Loads segment prefabs
        GameObject[] segmentPrefabs = new GameObject[nSegments];
        for (int i = 0; i < nSegments; i++)
        {
            string segmentPath = prefabsPath + template.segments[i].name + ".prefab";
            segmentPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(segmentPath);

            if (segmentPrefabs[i] == null)
            {
                return "error: No segment found at " + segmentPath;
            }
        }

        // Measures actual prefab lengths and compares with configuration
        float[] measuredSegmentLengths = Utility.GetSegmentLengths(segmentPrefabs);
        float[] segmentLengths = template.GetSegmentLengthsUnity();

        for (int i = 0; i < nSegments; i++)
        {
            if (Mathf.Abs(measuredSegmentLengths[i] - segmentLengths[i]) > LengthComparisonEpsilon)
            {
                Debug.Log(
                    $"Warning: For {template.segments[i].name}, there is a mismatch between the prefab length "
                        + $"({measuredSegmentLengths[i]}) and the sum of all the cue lengths ({segmentLengths[i]}). "
                        + $"Using {segmentLengths[i]} for the length of the segment."
                );
            }
        }

        int depth = template.vr_environment.segments_per_corridor;
        float paddingZShift = depth * Mathf.Min(segmentLengths) - 1;

        // Creates task GameObject hierarchy
        string taskName = Path.GetFileNameWithoutExtension(savePath);
        GameObject task = new GameObject(taskName);
        Task taskScript = task.AddComponent<Task>();
        taskScript.requireLick = true;
        taskScript.configPath = relativeConfigPath;

        int[] corridorSegments = new int[depth];
        int segment;
        float curCorridorX = 0;
        float corridorXShift = template.vr_environment.CorridorSpacingUnity;
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
                    StimulusTriggerZone stimulusTriggerZone =
                        instance.GetComponentInChildren<StimulusTriggerZone>();
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
                    StimulusTriggerZone stimulusTriggerZone =
                        instance.GetComponentInChildren<StimulusTriggerZone>();
                    if (stimulusTriggerZone != null)
                    {
                        string segmentName = template.segments[segment].name;
                        stimulusTriggerZone.showBoundary = template.GetSegmentMarkerVisibility(segmentName);
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

        PrefabUtility.SaveAsPrefabAsset(task, savePath);
        DestroyImmediate(task);

        return $"success: Task prefab saved to {savePath}";
    }

    /// <summary>
    /// Creates cue prefabs for cues that do not yet have a prefab in the Cues directory.
    /// Each cue prefab contains Left and Right Quad children with the cue material applied.
    /// </summary>
    /// <param name="template">The loaded task template.</param>
    /// <returns>True if all cue prefabs were built or already exist, false on error.</returns>
    private static bool BuildCuePrefabs(TaskTemplate template)
    {
        string cuesPath = "Assets/InfiniteCorridorTask/Cues/";
        string materialsPath = "Assets/InfiniteCorridorTask/Materials/";
        string texturesPath = "Assets/InfiniteCorridorTask/Textures/";
        float cmPerUnit = template.vr_environment.cm_per_unity_unit;

        // Ensures the Cues directory exists
        if (!AssetDatabase.IsValidFolder("Assets/InfiniteCorridorTask/Cues"))
        {
            AssetDatabase.CreateFolder("Assets/InfiniteCorridorTask", "Cues");
        }

        Mesh quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        foreach (Cue cue in template.cues)
        {
            string cuePrefabPath = cuesPath + "Cue_" + cue.name + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(cuePrefabPath) != null)
            {
                continue;
            }

            float lengthUnity = cue.LengthUnity(cmPerUnit);

            // Creates or loads the cue material
            string matPath = materialsPath + "Cue_" + cue.name + ".mat";
            Material cueMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (cueMat == null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturesPath + cue.texture);
                if (tex == null)
                {
                    Debug.LogError($"BuildCuePrefabs: Failed to load texture '{cue.texture}'.");
                    return false;
                }

                cueMat = new Material(Shader.Find("Standard"));
                cueMat.name = "Cue_" + cue.name;
                cueMat.SetTexture("_MainTex", tex);
                AssetDatabase.CreateAsset(cueMat, matPath);
            }

            // Creates cue GameObject with Left and Right Quad children
            GameObject cueGO = new GameObject("Cue_" + cue.name);

            GameObject right = new GameObject("Right");
            right.transform.SetParent(cueGO.transform);
            right.transform.localPosition = new Vector3(0.49f, 0.5f, lengthUnity / 2f);
            right.transform.localRotation = Quaternion.Euler(0, 90, 0);
            right.transform.localScale = new Vector3(-lengthUnity, 1, 1);
            right.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            right.AddComponent<MeshRenderer>().sharedMaterial = cueMat;

            GameObject left = new GameObject("Left");
            left.transform.SetParent(cueGO.transform);
            left.transform.localPosition = new Vector3(-0.49f, 0.5f, lengthUnity / 2f);
            left.transform.localRotation = Quaternion.Euler(0, -90, 0);
            left.transform.localScale = new Vector3(lengthUnity, 1, 1);
            left.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            left.AddComponent<MeshRenderer>().sharedMaterial = cueMat;

            PrefabUtility.SaveAsPrefabAsset(cueGO, cuePrefabPath);
            DestroyImmediate(cueGO);

            Debug.Log($"BuildCuePrefabs: Created {cuePrefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    /// <summary>
    /// Creates segment prefabs for segments that do not yet have a prefab in the Prefabs directory.
    /// Each segment prefab contains cue instances, floor, walls, and trigger/reset zones.
    /// </summary>
    /// <param name="template">The loaded task template.</param>
    /// <returns>True if all segment prefabs were built or already exist, false on error.</returns>
    private static bool BuildSegmentPrefabs(TaskTemplate template)
    {
        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";
        string cuesPath = "Assets/InfiniteCorridorTask/Cues/";
        string materialsPath = "Assets/InfiniteCorridorTask/Materials/";
        float cmPerUnit = template.vr_environment.cm_per_unity_unit;
        Dictionary<string, Cue> cueMap = template.GetCueByName();

        Mesh quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        Mesh planeMesh = Resources.GetBuiltinResource<Mesh>("New-Plane.fbx");

        // Loads shared materials
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(materialsPath + "Floor.mat");
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(materialsPath + "Wall.mat");

        if (floorMat == null || wallMat == null)
        {
            Debug.LogError("BuildSegmentPrefabs: Missing Floor.mat or Wall.mat.");
            return false;
        }

        // Loads zone template prefabs
        GameObject stimulusZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            prefabsPath + "StimulusTriggerZone.prefab"
        );
        GameObject occupancyZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            prefabsPath + "OccupancyTriggerZone.prefab"
        );
        GameObject resetZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            prefabsPath + "ResetZone.prefab"
        );

        foreach (Segment segment in template.segments)
        {
            string segmentPrefabPath = prefabsPath + segment.name + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(segmentPrefabPath) != null)
            {
                continue;
            }

            // Calculates total segment length in Unity units
            float totalLengthUnity = segment
                .cue_sequence.Sum(cn => cueMap[cn].LengthUnity(cmPerUnit));
            float cueOffsetUnity = template.cue_offset_cm / cmPerUnit;

            // Creates segment root with cue offset
            GameObject segmentGO = new GameObject(segment.name);
            segmentGO.transform.localPosition = new Vector3(0, 0, -cueOffsetUnity);

            // Places cue instances along the Z axis
            float cumulativeZ = 0f;
            foreach (string cueName in segment.cue_sequence)
            {
                Cue cue = cueMap[cueName];
                float cueLengthUnity = cue.LengthUnity(cmPerUnit);

                string cuePrefabPath = cuesPath + "Cue_" + cueName + ".prefab";
                GameObject cuePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cuePrefabPath);

                if (cuePrefab == null)
                {
                    Debug.LogError(
                        $"BuildSegmentPrefabs: Missing cue prefab at {cuePrefabPath}."
                    );
                    DestroyImmediate(segmentGO);
                    return false;
                }

                GameObject cueInstance = PrefabUtility.InstantiatePrefab(cuePrefab) as GameObject;
                cueInstance.name = "Cue" + cueName;
                cueInstance.transform.SetParent(segmentGO.transform);
                cueInstance.transform.localPosition = new Vector3(0, 0, cumulativeZ);

                cumulativeZ += cueLengthUnity;
            }

            // Creates Floor
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(segmentGO.transform);
            floor.transform.localPosition = new Vector3(0, 0, totalLengthUnity / 2f);
            floor.transform.localScale = new Vector3(0.1f, 1, totalLengthUnity / 10f);
            floor.AddComponent<MeshFilter>().sharedMesh = planeMesh;
            floor.AddComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Creates Walls group with LeftWall and RightWall
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(segmentGO.transform);
            walls.transform.localPosition = Vector3.zero;

            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(walls.transform);
            leftWall.transform.localPosition = new Vector3(
                -0.5f,
                0.5f,
                totalLengthUnity / 2f
            );
            leftWall.transform.localRotation = Quaternion.Euler(0, -90, 0);
            leftWall.transform.localScale = new Vector3(totalLengthUnity, 1, 1);
            leftWall.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            leftWall.AddComponent<MeshRenderer>().sharedMaterial = wallMat;

            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(walls.transform);
            rightWall.transform.localPosition = new Vector3(
                0.5f,
                0.5f,
                totalLengthUnity / 2f
            );
            rightWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
            rightWall.transform.localScale = new Vector3(totalLengthUnity, 1, 1);
            rightWall.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            rightWall.AddComponent<MeshRenderer>().sharedMaterial = wallMat;

            // Places zones from trial structure
            TrialStructure trial = template.GetTrialStructureForSegment(segment.name);

            if (trial != null)
            {
                float zoneStartUnity = trial.stimulus_trigger_zone_start_cm / cmPerUnit;
                float zoneEndUnity = trial.stimulus_trigger_zone_end_cm / cmPerUnit;
                float zoneCenterUnity = (zoneStartUnity + zoneEndUnity) / 2f;
                float zoneSizeUnity = zoneEndUnity - zoneStartUnity;
                float stimLocUnity = trial.stimulus_location_cm / cmPerUnit;

                if (trial.trigger_type == "lick" && stimulusZonePrefab != null)
                {
                    PlaceLickZone(
                        segmentGO,
                        stimulusZonePrefab,
                        zoneCenterUnity,
                        zoneSizeUnity,
                        stimLocUnity,
                        trial.show_stimulus_collision_boundary
                    );
                }
                else if (trial.trigger_type == "occupancy" && occupancyZonePrefab != null)
                {
                    PlaceOccupancyZone(
                        segmentGO,
                        occupancyZonePrefab,
                        zoneCenterUnity,
                        zoneSizeUnity,
                        stimLocUnity,
                        trial.show_stimulus_collision_boundary
                    );
                }

                // Places ResetZone at segment start
                if (resetZonePrefab != null)
                {
                    GameObject resetZone =
                        PrefabUtility.InstantiatePrefab(resetZonePrefab) as GameObject;
                    resetZone.transform.SetParent(segmentGO.transform);
                    resetZone.transform.localPosition = new Vector3(0, 0.5f, 1);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(segmentGO, segmentPrefabPath);
            DestroyImmediate(segmentGO);

            Debug.Log($"BuildSegmentPrefabs: Created {segmentPrefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    /// <summary>
    /// Instantiates and configures a StimulusTriggerZone (lick mode) within a segment.
    /// Positions the root collider to span the trigger zone and the GuidanceRegion at the stimulus location.
    /// </summary>
    private static void PlaceLickZone(
        GameObject parent,
        GameObject zonePrefab,
        float zoneCenterUnity,
        float zoneSizeUnity,
        float stimLocUnity,
        bool showBoundary
    )
    {
        GameObject zone = PrefabUtility.InstantiatePrefab(zonePrefab) as GameObject;
        zone.transform.SetParent(parent.transform);
        zone.transform.localPosition = new Vector3(0, 0.505f, zoneCenterUnity);

        // Configures root BoxCollider to span the trigger zone
        BoxCollider rootCollider = zone.GetComponent<BoxCollider>();
        if (rootCollider != null)
        {
            rootCollider.size = new Vector3(1, 1, zoneSizeUnity);
            rootCollider.center = Vector3.zero;
        }

        // Configures GuidanceRegion at the stimulus location
        GuidanceZone guidanceZone = zone.GetComponentInChildren<GuidanceZone>();
        if (guidanceZone != null)
        {
            BoxCollider guidanceCollider = guidanceZone.GetComponent<BoxCollider>();
            if (guidanceCollider != null)
            {
                guidanceCollider.size = new Vector3(1, 1, 0.4f);
                guidanceCollider.center = new Vector3(0, 0, stimLocUnity - zoneCenterUnity);
            }
        }

        // Sets boundary visibility
        StimulusTriggerZone stimZone = zone.GetComponent<StimulusTriggerZone>();
        if (stimZone != null)
        {
            stimZone.showBoundary = showBoundary;
        }
    }

    /// <summary>
    /// Instantiates and configures an OccupancyTriggerZone within a segment.
    /// The root is positioned at the stimulus boundary (past the occupancy zone).
    /// The OccupancyRegion child covers the start-to-end range where the animal must wait.
    /// </summary>
    private static void PlaceOccupancyZone(
        GameObject parent,
        GameObject zonePrefab,
        float zoneCenterUnity,
        float zoneSizeUnity,
        float stimLocUnity,
        bool showBoundary
    )
    {
        // Root position: stimulus boundary area, starting at stimulus_location and extending by zone size
        float rootZ = stimLocUnity + zoneSizeUnity / 2f;

        GameObject zone = PrefabUtility.InstantiatePrefab(zonePrefab) as GameObject;
        zone.transform.SetParent(parent.transform);
        zone.transform.localPosition = new Vector3(0, 0.505f, rootZ);

        // Configures root BoxCollider (stimulus boundary trigger area)
        BoxCollider rootCollider = zone.GetComponent<BoxCollider>();
        if (rootCollider != null)
        {
            rootCollider.size = new Vector3(1, 1, zoneSizeUnity);
            rootCollider.center = Vector3.zero;
        }

        // Configures OccupancyRegion to cover the occupancy zone range
        float occCenterOffset = zoneCenterUnity - rootZ;

        OccupancyZone occupancyZone = zone.GetComponentInChildren<OccupancyZone>();
        if (occupancyZone != null)
        {
            BoxCollider occCollider = occupancyZone.GetComponent<BoxCollider>();
            if (occCollider != null)
            {
                occCollider.size = new Vector3(1, 1, zoneSizeUnity);
                occCollider.center = new Vector3(0, 0, occCenterOffset);
            }
        }

        // Configures OccupancyGuidanceRegion at the downstream end of the occupancy zone
        OccupancyGuidanceZone occGuidanceZone =
            zone.GetComponentInChildren<OccupancyGuidanceZone>();
        if (occGuidanceZone != null)
        {
            BoxCollider occGuidanceCollider = occGuidanceZone.GetComponent<BoxCollider>();
            if (occGuidanceCollider != null)
            {
                occGuidanceCollider.size = new Vector3(1, 1, 0.4f);
                occGuidanceCollider.center = new Vector3(
                    0,
                    0,
                    occCenterOffset + zoneSizeUnity / 2f - 0.2f
                );
            }
        }

        // Sets boundary visibility
        StimulusTriggerZone stimZone = zone.GetComponent<StimulusTriggerZone>();
        if (stimZone != null)
        {
            stimZone.showBoundary = showBoundary;
        }
    }
}
