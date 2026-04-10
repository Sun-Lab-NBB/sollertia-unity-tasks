/// <summary>
/// Provides the McpBridge editor plugin that exposes Unity Editor operations to external MCP relay servers.
///
/// Starts an HTTP listener on localhost when the Editor loads, accepting JSON tool call requests from the
/// sollertia-unity-tasks MCP relay. Each request specifies a tool name and arguments; the bridge dispatches
/// to the corresponding Unity Editor API and returns a JSON result.
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SL.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SL.Tasks;

/// <summary>
/// HTTP listener that bridges external MCP relay requests to Unity Editor API calls.
/// Initialized automatically when the Editor loads via <see cref="InitializeOnLoadAttribute"/>.
/// </summary>
[InitializeOnLoad]
public static class McpBridge
{
    /// <summary>The port on which the bridge listens for incoming HTTP requests.</summary>
    private const int Port = 8090;

    /// <summary>The HTTP listener instance.</summary>
    private static HttpListener _listener;

    /// <summary>Starts the HTTP listener and registers the editor update callback.</summary>
    static McpBridge()
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            EditorApplication.update += Poll;
            Debug.Log($"McpBridge: Listening on http://localhost:{Port}/");
        }
        catch (Exception exception)
        {
            Debug.LogError($"McpBridge: Failed to start HTTP listener: {exception.Message}");
        }
    }

    /// <summary>Checks for pending HTTP requests each editor frame and dispatches them.</summary>
    private static void Poll()
    {
        if (_listener == null || !_listener.IsListening)
        {
            return;
        }

        while (_listener.IsListening)
        {
            IAsyncResult asyncResult = _listener.BeginGetContext(null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne(0))
            {
                break;
            }

            HttpListenerContext context = _listener.EndGetContext(asyncResult);
            HandleRequest(context);
        }
    }

    /// <summary>
    /// Reads the request body, dispatches to the appropriate tool handler, and writes the response.
    /// </summary>
    /// <param name="context">
    /// The HTTP listener context containing the request and response objects.
    /// </param>
    private static void HandleRequest(HttpListenerContext context)
    {
        string responseJson;

        try
        {
            string body;
            using (StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            Dictionary<string, object> request = MiniJson.Deserialize(body);
            string tool = request.ContainsKey("tool") ? request["tool"].ToString() : "";
            Dictionary<string, object> args = request.ContainsKey("args")
                ? request["args"] as Dictionary<string, object> ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            responseJson = Dispatch(tool, args);
        }
        catch (Exception exception)
        {
            responseJson = Error($"Bridge error: {exception.Message}");
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }

    /// <summary>Routes a tool call to the appropriate handler method.</summary>
    /// <param name="tool">The tool name to dispatch.</param>
    /// <param name="args">The tool arguments as a string-keyed dictionary.</param>
    /// <returns>A JSON response string.</returns>
    private static string Dispatch(string tool, Dictionary<string, object> args)
    {
        return tool switch
        {
            "generate_task_prefab" => GenerateTaskPrefab(args),
            "inspect_prefab" => InspectPrefab(args),
            "validate_prefab_against_template" => ValidatePrefabAgainstTemplate(args),
            "list_unity_assets" => ListUnityAssets(args),
            "list_scenes" => ListScenes(),
            "open_scene" => OpenScene(args),
            "create_scene" => CreateScene(args),
            "enter_play_mode" => EnterPlayMode(),
            "exit_play_mode" => ExitPlayMode(),
            "get_play_state" => GetPlayState(),
            _ => Error($"Unknown tool: {tool}"),
        };
    }

    /// <summary>
    /// Generates a Task prefab from a YAML template by delegating to CreateTask.CreateFromTemplate.
    /// </summary>
    /// <param name="args">The tool arguments containing template_name and optional save_path.</param>
    /// <returns>A JSON response with the generated prefab path or an error message.</returns>
    private static string GenerateTaskPrefab(Dictionary<string, object> args)
    {
        string templateName = GetString(args, "template_name");
        string savePath = GetString(args, "save_path", defaultValue: "");

        if (string.IsNullOrEmpty(templateName))
        {
            return Error("Missing required argument: template_name");
        }

        string absoluteTemplatePath = Path.Combine(
            Application.dataPath,
            "InfiniteCorridorTask",
            "Configurations",
            $"{templateName}.yaml"
        );

        if (!File.Exists(absoluteTemplatePath))
        {
            return Error($"Template not found: {absoluteTemplatePath}");
        }

        string relativeConfigPath = Path.Combine("/InfiniteCorridorTask", "Configurations", $"{templateName}.yaml");

        if (string.IsNullOrEmpty(savePath))
        {
            savePath = Path.Combine("Assets", "InfiniteCorridorTask", "Tasks", $"{templateName}.prefab");
        }

        // Ensures the Tasks output directory exists
        string tasksDirectory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(tasksDirectory) && !AssetDatabase.IsValidFolder(tasksDirectory))
        {
            string parent = Path.GetDirectoryName(tasksDirectory);
            string folder = Path.GetFileName(tasksDirectory);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        string result = CreateTask.CreateFromTemplate(absoluteTemplatePath, relativeConfigPath, savePath);

        if (result.StartsWith("error:", StringComparison.Ordinal))
        {
            return Error(result.Substring(7).Trim());
        }

        return Ok(
            new Dictionary<string, object>
            {
                { "message", result },
                { "prefab_path", savePath },
                { "template_name", templateName },
            }
        );
    }

    /// <summary>Reads a prefab and returns its hierarchy, components, and zone configuration.</summary>
    /// <param name="args">The tool arguments containing prefab_path.</param>
    /// <returns>A JSON response with the prefab hierarchy or an error message.</returns>
    private static string InspectPrefab(Dictionary<string, object> args)
    {
        string prefabPath = GetString(args, "prefab_path");

        if (string.IsNullOrEmpty(prefabPath))
        {
            return Error("Missing required argument: prefab_path");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return Error($"Prefab not found at: {prefabPath}");
        }

        Dictionary<string, object> hierarchy = InspectGameObject(prefab);

        return Ok(new Dictionary<string, object> { { "prefab_path", prefabPath }, { "hierarchy", hierarchy } });
    }

    /// <summary>
    /// Validates that a prefab's zone positions match the template's configured values.
    /// </summary>
    /// <param name="args">The tool arguments containing template_name.</param>
    /// <returns>A JSON response with validation results for each segment.</returns>
    private static string ValidatePrefabAgainstTemplate(Dictionary<string, object> args)
    {
        string templateName = GetString(args, "template_name");

        if (string.IsNullOrEmpty(templateName))
        {
            return Error("Missing required argument: template_name");
        }

        string absoluteTemplatePath = Path.Combine(
            Application.dataPath,
            "InfiniteCorridorTask",
            "Configurations",
            $"{templateName}.yaml"
        );

        if (!File.Exists(absoluteTemplatePath))
        {
            return Error($"Template not found: {absoluteTemplatePath}");
        }

        TaskTemplate template;
        try
        {
            template = ConfigLoader.LoadTemplate(absoluteTemplatePath);
        }
        catch (Exception exception)
        {
            return Error($"Failed to load template: {exception.Message}");
        }

        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";
        float cmPerUnit = template.vrEnvironment.cmPerUnityUnit;
        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        foreach (Segment segment in template.segments)
        {
            string segmentPath = Path.Combine(prefabsPath, $"{segment.name}.prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(segmentPath);

            Dictionary<string, object> segmentResult = new Dictionary<string, object>
            {
                { "segment", segment.name },
                { "prefab_exists", prefab != null },
            };

            if (prefab != null)
            {
                TrialStructure trial = template.GetTrialStructureForSegment(segment.name);
                if (trial != null)
                {
                    StimulusTriggerZone zone = prefab.GetComponentInChildren<StimulusTriggerZone>();
                    segmentResult["has_zone"] = zone != null;

                    if (zone != null)
                    {
                        float actualZ = zone.transform.localPosition.z;
                        BoxCollider collider = zone.GetComponent<BoxCollider>();
                        float actualSize = collider != null ? collider.size.z : 0f;

                        float expectedCenter =
                            (trial.stimulusTriggerZoneStartCm + trial.stimulusTriggerZoneEndCm) / (2f * cmPerUnit);
                        float expectedSize =
                            (trial.stimulusTriggerZoneEndCm - trial.stimulusTriggerZoneStartCm) / cmPerUnit;

                        segmentResult["zone_z"] = actualZ;
                        segmentResult["expected_zone_z"] = expectedCenter;
                        segmentResult["zone_size"] = actualSize;
                        segmentResult["expected_zone_size"] = expectedSize;
                        segmentResult["zone_z_match"] = Mathf.Abs(actualZ - expectedCenter) < 0.01f;
                        segmentResult["zone_size_match"] = Mathf.Abs(actualSize - expectedSize) < 0.01f;
                    }
                }
            }

            results.Add(segmentResult);
        }

        return Ok(new Dictionary<string, object> { { "template_name", templateName }, { "segments", results } });
    }

    /// <summary>
    /// Lists Unity assets of a given type filter (e.g., "Prefab", "Scene", "Material").
    /// </summary>
    /// <param name="args">The tool arguments containing optional type and path filters.</param>
    /// <returns>A JSON response with matching asset paths.</returns>
    private static string ListUnityAssets(Dictionary<string, object> args)
    {
        string assetType = GetString(args, "type", defaultValue: "Prefab");
        string searchPath = GetString(args, "path", defaultValue: "Assets/InfiniteCorridorTask");

        string[] guids = AssetDatabase.FindAssets($"t:{assetType}", new[] { searchPath });
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToList();

        return Ok(
            new Dictionary<string, object>
            {
                { "type", assetType },
                { "search_path", searchPath },
                { "assets", paths },
                { "count", paths.Count },
            }
        );
    }

    /// <summary>Lists all scene assets in the project.</summary>
    /// <returns>A JSON response with all scene paths and the active scene.</returns>
    private static string ListScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToList();

        string activeScene = SceneManager.GetActiveScene().path;

        return Ok(
            new Dictionary<string, object>
            {
                { "scenes", paths },
                { "active_scene", activeScene },
                { "count", paths.Count },
            }
        );
    }

    /// <summary>Opens a scene in the Editor.</summary>
    /// <param name="args">The tool arguments containing scene_path.</param>
    /// <returns>A JSON response confirming the scene was opened or an error message.</returns>
    private static string OpenScene(Dictionary<string, object> args)
    {
        string scenePath = GetString(args, "scene_path");

        if (string.IsNullOrEmpty(scenePath))
        {
            return Error("Missing required argument: scene_path");
        }

        if (!File.Exists(scenePath))
        {
            return Error($"Scene not found at: {scenePath}");
        }

        // Saves the current scene before switching
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(scenePath);

        return Ok(
            new Dictionary<string, object> { { "message", $"Opened scene: {scenePath}" }, { "scene_path", scenePath } }
        );
    }

    /// <summary>
    /// Creates a new scene by copying ExperimentTemplate.unity, optionally adding a task prefab to it.
    /// </summary>
    /// <param name="args">The tool arguments containing scene_name and optional task_prefab_path.</param>
    /// <returns>A JSON response with the created scene path or an error message.</returns>
    private static string CreateScene(Dictionary<string, object> args)
    {
        string sceneName = GetString(args, "scene_name");
        string taskPrefabPath = GetString(args, "task_prefab_path", defaultValue: "");

        if (string.IsNullOrEmpty(sceneName))
        {
            return Error("Missing required argument: scene_name");
        }

        string templateScenePath = Path.Combine("Assets", "Scenes", "ExperimentTemplate.unity");
        string newScenePath = Path.Combine("Assets", "Scenes", $"{sceneName}.unity");

        if (!File.Exists(templateScenePath))
        {
            return Error($"Template scene not found at: {templateScenePath}");
        }

        if (File.Exists(newScenePath))
        {
            return Error($"Scene already exists at: {newScenePath}");
        }

        // Copies the template scene file
        AssetDatabase.CopyAsset(templateScenePath, newScenePath);
        AssetDatabase.Refresh();

        // Opens the new scene and adds the task prefab if specified
        EditorSceneManager.OpenScene(newScenePath);

        if (!string.IsNullOrEmpty(taskPrefabPath))
        {
            GameObject taskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(taskPrefabPath);
            if (taskPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(taskPrefab);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            else
            {
                return Ok(
                    new Dictionary<string, object>
                    {
                        { "message", $"Scene created but task prefab not found at: {taskPrefabPath}" },
                        { "scene_path", newScenePath },
                        { "warning", "task_prefab_not_found" },
                    }
                );
            }
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        return Ok(
            new Dictionary<string, object>
            {
                { "message", $"Scene created: {newScenePath}" },
                { "scene_path", newScenePath },
            }
        );
    }

    /// <summary>Enters Play Mode in the Editor.</summary>
    /// <returns>A JSON response with the current play state.</returns>
    private static string EnterPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            return Ok(
                new Dictionary<string, object> { { "message", "Already in Play Mode." }, { "state", "playing" } }
            );
        }

        EditorApplication.EnterPlaymode();

        return Ok(
            new Dictionary<string, object> { { "message", "Entering Play Mode." }, { "state", "entering_play_mode" } }
        );
    }

    /// <summary>Exits Play Mode in the Editor.</summary>
    /// <returns>A JSON response with the current play state.</returns>
    private static string ExitPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            return Ok(new Dictionary<string, object> { { "message", "Not in Play Mode." }, { "state", "edit" } });
        }

        EditorApplication.ExitPlaymode();

        return Ok(
            new Dictionary<string, object> { { "message", "Exiting Play Mode." }, { "state", "exiting_play_mode" } }
        );
    }

    /// <summary>Returns the current Editor play state.</summary>
    /// <returns>A JSON response with the current state and active scene name.</returns>
    private static string GetPlayState()
    {
        string state =
            EditorApplication.isPlaying ? "playing"
            : EditorApplication.isCompiling ? "compiling"
            : "edit";

        return Ok(
            new Dictionary<string, object>
            {
                { "state", state },
                { "active_scene", SceneManager.GetActiveScene().name },
            }
        );
    }

    /// <summary>Recursively inspects a GameObject and returns its hierarchy as a dictionary.</summary>
    /// <param name="gameObject">The GameObject to inspect.</param>
    /// <returns>A dictionary describing the GameObject's transform, components, and children.</returns>
    private static Dictionary<string, object> InspectGameObject(GameObject gameObject)
    {
        Dictionary<string, object> result = new Dictionary<string, object>
        {
            { "name", gameObject.name },
            { "position", FormatVector3(gameObject.transform.localPosition) },
            { "rotation", FormatVector3(gameObject.transform.localEulerAngles) },
            { "scale", FormatVector3(gameObject.transform.localScale) },
        };

        // Lists component types
        Component[] components = gameObject.GetComponents<Component>();
        List<string> componentNames = components
            .Where(component => component != null)
            .Select(component => component.GetType().Name)
            .ToList();
        result["components"] = componentNames;

        // Includes BoxCollider details if present
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        if (collider != null)
        {
            result["collider_center"] = FormatVector3(collider.center);
            result["collider_size"] = FormatVector3(collider.size);
            result["collider_is_trigger"] = collider.isTrigger;
        }

        // Recurses into children
        List<Dictionary<string, object>> children = new List<Dictionary<string, object>>();
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            children.Add(InspectGameObject(gameObject.transform.GetChild(i).gameObject));
        }

        if (children.Count > 0)
        {
            result["children"] = children;
        }

        return result;
    }

    /// <summary>Formats a Vector3 as a serializable dictionary.</summary>
    /// <param name="vector">The Vector3 to format.</param>
    /// <returns>A dictionary with x, y, and z keys.</returns>
    private static Dictionary<string, float> FormatVector3(Vector3 vector)
    {
        return new Dictionary<string, float>
        {
            { "x", vector.x },
            { "y", vector.y },
            { "z", vector.z },
        };
    }

    /// <summary>Retrieves a string value from the arguments dictionary with an optional default.</summary>
    /// <param name="args">The arguments dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The default value if the key is not found.</param>
    /// <returns>The string value, or the default if not found.</returns>
    private static string GetString(Dictionary<string, object> args, string key, string defaultValue = null)
    {
        if (args.ContainsKey(key) && args[key] != null)
        {
            return args[key].ToString();
        }

        return defaultValue;
    }

    /// <summary>Constructs a success JSON response.</summary>
    /// <param name="payload">The response payload dictionary.</param>
    /// <returns>A JSON string with success set to true.</returns>
    private static string Ok(Dictionary<string, object> payload)
    {
        payload["success"] = true;
        return MiniJson.Serialize(payload);
    }

    /// <summary>Constructs an error JSON response.</summary>
    /// <param name="message">The error message.</param>
    /// <returns>A JSON string with success set to false and the error message.</returns>
    private static string Error(string message)
    {
        return MiniJson.Serialize(new Dictionary<string, object> { { "success", false }, { "error", message } });
    }
}
