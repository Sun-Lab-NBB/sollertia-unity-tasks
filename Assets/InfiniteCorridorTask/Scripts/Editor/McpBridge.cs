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
        catch (Exception ex)
        {
            Debug.LogError($"McpBridge: Failed to start HTTP listener: {ex.Message}");
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

    /// <summary>Reads the request body, dispatches to the appropriate tool handler, and writes the response.</summary>
    /// <param name="context">The HTTP listener context containing the request and response objects.</param>
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
        catch (Exception ex)
        {
            responseJson = Error($"Bridge error: {ex.Message}");
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
            _ => Error($"Unknown tool: {tool}")
        };
    }

    // --------------------------------------------------------------------------------------------
    //  Asset creation tools
    // --------------------------------------------------------------------------------------------

    /// <summary>Generates a Task prefab from a YAML template by delegating to CreateTask.CreateFromTemplate.</summary>
    private static string GenerateTaskPrefab(Dictionary<string, object> args)
    {
        string templateName = GetString(args, "template_name");
        string savePath = GetString(args, "save_path", "");

        if (string.IsNullOrEmpty(templateName))
        {
            return Error("Missing required argument: template_name");
        }

        string configurationsDir = "Assets/InfiniteCorridorTask/Configurations/";
        string absoluteTemplatePath = Application.dataPath
            + "/InfiniteCorridorTask/Configurations/"
            + templateName
            + ".yaml";

        if (!File.Exists(absoluteTemplatePath))
        {
            return Error($"Template not found: {absoluteTemplatePath}");
        }

        string relativeConfigPath = "/InfiniteCorridorTask/Configurations/" + templateName + ".yaml";

        if (string.IsNullOrEmpty(savePath))
        {
            savePath = "Assets/InfiniteCorridorTask/Tasks/" + templateName + ".prefab";
        }

        // Ensures the Tasks output directory exists
        string tasksDir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(tasksDir) && !AssetDatabase.IsValidFolder(tasksDir))
        {
            string parent = Path.GetDirectoryName(tasksDir);
            string folder = Path.GetFileName(tasksDir);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        string result = CreateTask.CreateFromTemplate(absoluteTemplatePath, relativeConfigPath, savePath);

        if (result.StartsWith("error:"))
        {
            return Error(result.Substring(7).Trim());
        }

        return Ok(new Dictionary<string, object>
        {
            { "message", result },
            { "prefab_path", savePath },
            { "template_name", templateName }
        });
    }

    /// <summary>Reads a prefab and returns its hierarchy, components, and zone configuration.</summary>
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

        return Ok(new Dictionary<string, object>
        {
            { "prefab_path", prefabPath },
            { "hierarchy", hierarchy }
        });
    }

    /// <summary>Validates that a prefab's zone positions match the template's configured values.</summary>
    private static string ValidatePrefabAgainstTemplate(Dictionary<string, object> args)
    {
        string templateName = GetString(args, "template_name");

        if (string.IsNullOrEmpty(templateName))
        {
            return Error("Missing required argument: template_name");
        }

        string absoluteTemplatePath = Application.dataPath
            + "/InfiniteCorridorTask/Configurations/"
            + templateName
            + ".yaml";

        if (!File.Exists(absoluteTemplatePath))
        {
            return Error($"Template not found: {absoluteTemplatePath}");
        }

        TaskTemplate template = ConfigLoader.LoadTemplate(absoluteTemplatePath);
        if (template == null)
        {
            return Error("Failed to load template.");
        }

        string prefabsPath = "Assets/InfiniteCorridorTask/Prefabs/";
        float cmPerUnit = template.vr_environment.cm_per_unity_unit;
        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        foreach (Segment segment in template.segments)
        {
            string segmentPath = prefabsPath + segment.name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(segmentPath);

            Dictionary<string, object> segmentResult = new Dictionary<string, object>
            {
                { "segment", segment.name },
                { "prefab_exists", prefab != null }
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
                            (trial.stimulus_trigger_zone_start_cm + trial.stimulus_trigger_zone_end_cm)
                            / (2f * cmPerUnit);
                        float expectedSize =
                            (trial.stimulus_trigger_zone_end_cm - trial.stimulus_trigger_zone_start_cm) / cmPerUnit;

                        segmentResult["zone_z"] = actualZ;
                        segmentResult["expected_zone_z"] = expectedCenter;
                        segmentResult["zone_size"] = actualSize;
                        segmentResult["expected_zone_size"] = expectedSize;
                        segmentResult["zone_z_match"] =
                            Mathf.Abs(actualZ - expectedCenter) < 0.01f;
                        segmentResult["zone_size_match"] =
                            Mathf.Abs(actualSize - expectedSize) < 0.01f;
                    }
                }
            }

            results.Add(segmentResult);
        }

        return Ok(new Dictionary<string, object>
        {
            { "template_name", templateName },
            { "segments", results }
        });
    }

    /// <summary>Lists Unity assets of a given type filter (e.g., "Prefab", "Scene", "Material").</summary>
    private static string ListUnityAssets(Dictionary<string, object> args)
    {
        string assetType = GetString(args, "type", "Prefab");
        string searchPath = GetString(args, "path", "Assets/InfiniteCorridorTask");

        string[] guids = AssetDatabase.FindAssets($"t:{assetType}", new[] { searchPath });
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        return Ok(new Dictionary<string, object>
        {
            { "type", assetType },
            { "search_path", searchPath },
            { "assets", paths },
            { "count", paths.Count }
        });
    }

    // --------------------------------------------------------------------------------------------
    //  Scene tools
    // --------------------------------------------------------------------------------------------

    /// <summary>Lists all scene assets in the project.</summary>
    private static string ListScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        string activeScene = SceneManager.GetActiveScene().path;

        return Ok(new Dictionary<string, object>
        {
            { "scenes", paths },
            { "active_scene", activeScene },
            { "count", paths.Count }
        });
    }

    /// <summary>Opens a scene in the Editor.</summary>
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

        return Ok(new Dictionary<string, object>
        {
            { "message", $"Opened scene: {scenePath}" },
            { "scene_path", scenePath }
        });
    }

    /// <summary>
    /// Creates a new scene by copying ExperimentTemplate.unity, optionally adding a task prefab to it.
    /// </summary>
    private static string CreateScene(Dictionary<string, object> args)
    {
        string sceneName = GetString(args, "scene_name");
        string taskPrefabPath = GetString(args, "task_prefab_path", "");

        if (string.IsNullOrEmpty(sceneName))
        {
            return Error("Missing required argument: scene_name");
        }

        string templateScenePath = "Assets/Scenes/ExperimentTemplate.unity";
        string newScenePath = $"Assets/Scenes/{sceneName}.unity";

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
                return Ok(new Dictionary<string, object>
                {
                    { "message", $"Scene created but task prefab not found at: {taskPrefabPath}" },
                    { "scene_path", newScenePath },
                    { "warning", "task_prefab_not_found" }
                });
            }
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        return Ok(new Dictionary<string, object>
        {
            { "message", $"Scene created: {newScenePath}" },
            { "scene_path", newScenePath }
        });
    }

    // --------------------------------------------------------------------------------------------
    //  Play mode tools
    // --------------------------------------------------------------------------------------------

    /// <summary>Enters Play Mode in the Editor.</summary>
    private static string EnterPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            return Ok(new Dictionary<string, object>
            {
                { "message", "Already in Play Mode." },
                { "state", "playing" }
            });
        }

        EditorApplication.EnterPlaymode();

        return Ok(new Dictionary<string, object>
        {
            { "message", "Entering Play Mode." },
            { "state", "entering_play_mode" }
        });
    }

    /// <summary>Exits Play Mode in the Editor.</summary>
    private static string ExitPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            return Ok(new Dictionary<string, object>
            {
                { "message", "Not in Play Mode." },
                { "state", "edit" }
            });
        }

        EditorApplication.ExitPlaymode();

        return Ok(new Dictionary<string, object>
        {
            { "message", "Exiting Play Mode." },
            { "state", "exiting_play_mode" }
        });
    }

    /// <summary>Returns the current Editor play state.</summary>
    private static string GetPlayState()
    {
        string state = EditorApplication.isPlaying ? "playing"
            : EditorApplication.isCompiling ? "compiling"
            : "edit";

        return Ok(new Dictionary<string, object>
        {
            { "state", state },
            { "active_scene", SceneManager.GetActiveScene().name }
        });
    }

    // --------------------------------------------------------------------------------------------
    //  Helpers
    // --------------------------------------------------------------------------------------------

    /// <summary>Recursively inspects a GameObject and returns its hierarchy as a dictionary.</summary>
    /// <param name="go">The GameObject to inspect.</param>
    /// <returns>A dictionary describing the GameObject's transform, components, and children.</returns>
    private static Dictionary<string, object> InspectGameObject(GameObject go)
    {
        Dictionary<string, object> result = new Dictionary<string, object>
        {
            { "name", go.name },
            { "position", FormatVector3(go.transform.localPosition) },
            { "rotation", FormatVector3(go.transform.localEulerAngles) },
            { "scale", FormatVector3(go.transform.localScale) }
        };

        // Lists component types
        Component[] components = go.GetComponents<Component>();
        List<string> componentNames = components
            .Where(c => c != null)
            .Select(c => c.GetType().Name)
            .ToList();
        result["components"] = componentNames;

        // Includes BoxCollider details if present
        BoxCollider collider = go.GetComponent<BoxCollider>();
        if (collider != null)
        {
            result["collider_center"] = FormatVector3(collider.center);
            result["collider_size"] = FormatVector3(collider.size);
            result["collider_is_trigger"] = collider.isTrigger;
        }

        // Recurses into children
        List<Dictionary<string, object>> children = new List<Dictionary<string, object>>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            children.Add(InspectGameObject(go.transform.GetChild(i).gameObject));
        }

        if (children.Count > 0)
        {
            result["children"] = children;
        }

        return result;
    }

    /// <summary>Formats a Vector3 as a serializable dictionary.</summary>
    private static Dictionary<string, float> FormatVector3(Vector3 v)
    {
        return new Dictionary<string, float> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
    }

    /// <summary>Retrieves a string value from the arguments dictionary with an optional default.</summary>
    private static string GetString(Dictionary<string, object> args, string key, string defaultValue = null)
    {
        if (args.ContainsKey(key) && args[key] != null)
        {
            return args[key].ToString();
        }

        return defaultValue;
    }

    /// <summary>Constructs a success JSON response.</summary>
    private static string Ok(Dictionary<string, object> payload)
    {
        payload["success"] = true;
        return MiniJson.Serialize(payload);
    }

    /// <summary>Constructs an error JSON response.</summary>
    private static string Error(string message)
    {
        return MiniJson.Serialize(new Dictionary<string, object>
        {
            { "success", false },
            { "error", message }
        });
    }
}

/// <summary>
/// Minimal JSON serializer and deserializer for MCP bridge communication.
/// Handles dictionaries, lists, strings, numbers, booleans, and null values.
/// </summary>
public static class MiniJson
{
    /// <summary>Deserializes a JSON string into a dictionary.</summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A dictionary of string keys to object values.</returns>
    public static Dictionary<string, object> Deserialize(string json)
    {
        return Parse(json);
    }

    /// <summary>Serializes a dictionary to a JSON string.</summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    public static string Serialize(object obj)
    {
        if (obj == null)
        {
            return "null";
        }

        if (obj is bool b)
        {
            return b ? "true" : "false";
        }

        if (obj is string s)
        {
            return "\"" + EscapeString(s) + "\"";
        }

        if (obj is int || obj is long || obj is float || obj is double)
        {
            return obj.ToString();
        }

        if (obj is Dictionary<string, object> dict)
        {
            StringBuilder sb = new StringBuilder("{");
            bool first = true;
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append("\"").Append(EscapeString(kvp.Key)).Append("\":");
                sb.Append(Serialize(kvp.Value));
                first = false;
            }

            sb.Append("}");
            return sb.ToString();
        }

        if (obj is Dictionary<string, float> floatDict)
        {
            StringBuilder sb = new StringBuilder("{");
            bool first = true;
            foreach (KeyValuePair<string, float> kvp in floatDict)
            {
                if (!first) sb.Append(",");
                sb.Append("\"").Append(EscapeString(kvp.Key)).Append("\":").Append(kvp.Value);
                first = false;
            }

            sb.Append("}");
            return sb.ToString();
        }

        if (obj is IEnumerable<object> enumerable)
        {
            StringBuilder sb = new StringBuilder("[");
            bool first = true;
            foreach (object item in enumerable)
            {
                if (!first) sb.Append(",");
                sb.Append(Serialize(item));
                first = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        if (obj is IEnumerable<string> stringEnumerable)
        {
            StringBuilder sb = new StringBuilder("[");
            bool first = true;
            foreach (string item in stringEnumerable)
            {
                if (!first) sb.Append(",");
                sb.Append(Serialize(item));
                first = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        if (obj is IEnumerable<Dictionary<string, object>> dictEnumerable)
        {
            StringBuilder sb = new StringBuilder("[");
            bool first = true;
            foreach (Dictionary<string, object> item in dictEnumerable)
            {
                if (!first) sb.Append(",");
                sb.Append(Serialize(item));
                first = false;
            }

            sb.Append("]");
            return sb.ToString();
        }

        return "\"" + EscapeString(obj.ToString()) + "\"";
    }

    /// <summary>Escapes special characters in a string for JSON encoding.</summary>
    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // Unity's JsonUtility needs a wrapper class, but for MCP we need raw dictionary parsing.
    // This uses a simple recursive-descent parser instead.

    /// <summary>Wraps a JSON string for Unity's JsonUtility (unused, kept for fallback).</summary>
    private static string WrapForUnity(string json) => json;

    /// <summary>Wrapper class placeholder for JsonUtility compatibility.</summary>
    [Serializable]
    private class Wrapper
    {
        public Dictionary<string, object> ToDictionary() => new Dictionary<string, object>();
    }

    /// <summary>Parses a JSON string into a dictionary using a recursive-descent parser.</summary>
    /// <param name="json">The raw JSON string.</param>
    /// <returns>A parsed dictionary.</returns>
    public static Dictionary<string, object> Parse(string json)
    {
        int index = 0;
        return ParseObject(json, ref index);
    }

    /// <summary>Parses a JSON object.</summary>
    private static Dictionary<string, object> ParseObject(string json, ref int index)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        SkipWhitespace(json, ref index);

        if (index >= json.Length || json[index] != '{')
        {
            return result;
        }

        index++; // skip '{'
        SkipWhitespace(json, ref index);

        if (index < json.Length && json[index] == '}')
        {
            index++;
            return result;
        }

        while (index < json.Length)
        {
            SkipWhitespace(json, ref index);
            string key = ParseString(json, ref index);
            SkipWhitespace(json, ref index);

            if (index < json.Length && json[index] == ':')
            {
                index++;
            }

            SkipWhitespace(json, ref index);
            object value = ParseValue(json, ref index);
            result[key] = value;
            SkipWhitespace(json, ref index);

            if (index < json.Length && json[index] == ',')
            {
                index++;
            }
            else
            {
                break;
            }
        }

        if (index < json.Length && json[index] == '}')
        {
            index++;
        }

        return result;
    }

    /// <summary>Parses a JSON value (object, array, string, number, boolean, or null).</summary>
    private static object ParseValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);

        if (index >= json.Length)
        {
            return null;
        }

        char c = json[index];

        if (c == '"') return ParseString(json, ref index);
        if (c == '{') return ParseObject(json, ref index);
        if (c == '[') return ParseArray(json, ref index);
        if (c == 't' || c == 'f') return ParseBool(json, ref index);
        if (c == 'n') return ParseNull(json, ref index);
        return ParseNumber(json, ref index);
    }

    /// <summary>Parses a JSON string.</summary>
    private static string ParseString(string json, ref int index)
    {
        if (index >= json.Length || json[index] != '"')
        {
            return "";
        }

        index++; // skip opening quote
        StringBuilder sb = new StringBuilder();

        while (index < json.Length && json[index] != '"')
        {
            if (json[index] == '\\' && index + 1 < json.Length)
            {
                index++;
                char escaped = json[index];
                switch (escaped)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    default: sb.Append(escaped); break;
                }
            }
            else
            {
                sb.Append(json[index]);
            }

            index++;
        }

        if (index < json.Length)
        {
            index++; // skip closing quote
        }

        return sb.ToString();
    }

    /// <summary>Parses a JSON number.</summary>
    private static object ParseNumber(string json, ref int index)
    {
        int start = index;
        while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-'
            || json[index] == 'e' || json[index] == 'E' || json[index] == '+'))
        {
            index++;
        }

        string numStr = json.Substring(start, index - start);

        if (numStr.Contains('.') || numStr.Contains('e') || numStr.Contains('E'))
        {
            double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d);
            return d;
        }

        long.TryParse(numStr, out long l);
        return l;
    }

    /// <summary>Parses a JSON boolean.</summary>
    private static object ParseBool(string json, ref int index)
    {
        if (json.Substring(index).StartsWith("true"))
        {
            index += 4;
            return true;
        }

        index += 5;
        return false;
    }

    /// <summary>Parses a JSON null value.</summary>
    private static object ParseNull(string json, ref int index)
    {
        index += 4;
        return null;
    }

    /// <summary>Parses a JSON array.</summary>
    private static List<object> ParseArray(string json, ref int index)
    {
        List<object> result = new List<object>();
        index++; // skip '['
        SkipWhitespace(json, ref index);

        if (index < json.Length && json[index] == ']')
        {
            index++;
            return result;
        }

        while (index < json.Length)
        {
            SkipWhitespace(json, ref index);
            result.Add(ParseValue(json, ref index));
            SkipWhitespace(json, ref index);

            if (index < json.Length && json[index] == ',')
            {
                index++;
            }
            else
            {
                break;
            }
        }

        if (index < json.Length && json[index] == ']')
        {
            index++;
        }

        return result;
    }

    /// <summary>Advances the index past whitespace characters.</summary>
    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }
}
