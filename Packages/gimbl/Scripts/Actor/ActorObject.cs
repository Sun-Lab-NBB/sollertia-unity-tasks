/// <summary>
/// Provides the ActorObject class representing an animal in the VR environment.
///
/// Manages the actor's display, controller, and settings references with validation
/// to ensure proper linkage between components.
/// </summary>
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gimbl
{
    /// <summary>
    /// Represents an animal actor in the VR environment with linked display and controller.
    /// </summary>
    [System.Serializable]
    public partial class ActorObject : MonoBehaviour
    {
        /// <summary>Determines whether actor movement is enabled.</summary>
        public bool isActive = true;

        /// <summary>The serialized backing field for the display property.</summary>
        [SerializeField]
        private DisplayObject _display;

        /// <summary>The display object rendering the VR view for this actor.</summary>
        public DisplayObject display
        {
            get { return _display; }
            set
            {
                if (value != _display)
                {
                    // Parents new display to this actor
                    if (value != null)
                    {
                        value.ParentToActor(this);
                    }

                    // Unparents previous display if it existed
                    if (_display != null)
                    {
                        _display.Unparent();
                    }

                    _display = value;
                }
            }
        }

        /// <summary>The actor's configuration settings asset.</summary>
        [SerializeField]
        public ActorSettings settings;

        /// <summary>The serialized backing field for the controller property.</summary>
        [SerializeField]
        private ControllerOutput _controller;

        /// <summary>
        /// The controller providing input for this actor. Only one controller can be linked at a time.
        /// </summary>
        public ControllerOutput controller
        {
            get { return _controller; }
            set
            {
                if (_controller != value)
                {
                    // Abandons previous controller
                    if (_controller != null)
                    {
                        _controller.master.Actor = null;
                    }

                    _controller = value;

                    if (value != null)
                    {
                        value.master.Actor = this;

                        // Ensures other actors are no longer coupled to this controller
                        foreach (ActorObject act in FindObjectsByType<ActorObject>(FindObjectsSortMode.None))
                        {
                            if (act.controller == value && act != this)
                            {
                                Debug.LogWarning(
                                    $"Switched Controller {value.gameObject.name} "
                                        + $"from {act.gameObject.name} to {this.gameObject.name}"
                                );
                                act._controller = null;
                            }
                        }
                    }

                    if (!EditorApplication.isPlaying)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                        );
                    }
                }
            }
        }

        /// <summary>Called when the actor starts. Currently empty.</summary>
        public void Start() { }

        /// <summary>Called after all Update methods. Currently empty.</summary>
        public void LateUpdate() { }

        /// <summary>Initializes a new actor with the specified model and optional tracking camera.</summary>
        /// <param name="modelStr">The name of the model prefab to load, or "None" for no model.</param>
        /// <param name="trackCam">If true, creates a tracking camera for this actor.</param>
        public void InitiateActor(string modelStr, bool trackCam)
        {
            gameObject.transform.SetParent(GameObject.Find("Actors").transform);

            ActorSettings asset = ScriptableObject.CreateInstance<ActorSettings>();
            AssetDatabase.CreateAsset(asset, $"Assets/VRSettings/Actors/{gameObject.name}.asset");
            settings = asset;

            // Adds character controller for collision detection
            CharacterController charObj = gameObject.AddComponent<CharacterController>();
            charObj.slopeLimit = 45;
            charObj.stepOffset = 0.000001f;
            charObj.skinWidth = 0.05f;
            charObj.minMoveDistance = 0.001f;
            charObj.center = new Vector3(0, 0.55f, 0);
            charObj.radius = 0.5f;
            charObj.height = 0.1f;

            // Creates render layer for this actor
            TagLayerEditor.TagsAndLayers.AddLayer(gameObject.name);

            // Instantiates the model if specified
            if (modelStr != "None")
            {
                Object modelObj = Resources.Load($"Actors/Prefabs/{modelStr}");
                GameObject model = Instantiate(modelObj) as GameObject;
                model.name = $"Model {modelStr}";
                model.transform.SetParent(gameObject.transform);
                model.layer = LayerMask.NameToLayer(gameObject.name);
            }

            // Creates tracking camera if requested
            if (trackCam)
            {
                // Finds currently used displays to avoid conflicts
                List<int> usedDisplays = new List<int>();
                List<int> availableDisplays = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };
                TagLayerEditor.TagsAndLayers.AddTag("TrackCam");

                foreach (GameObject trackObj in GameObject.FindGameObjectsWithTag("TrackCam"))
                {
                    usedDisplays.Add(trackObj.GetComponent<Camera>().targetDisplay);
                }

                int[] displays = availableDisplays.Except(usedDisplays).ToArray();
                int nextDisp = displays.Length > 0 ? displays[0] : 7;

                // Creates the tracking camera
                GameObject cam = new GameObject($"Track Cam: {settings.name}");
                Camera camComp = cam.AddComponent<Camera>();
                cam.transform.parent = gameObject.transform;
                cam.transform.localPosition = new Vector3(0, 1, -1.3f);
                cam.transform.eulerAngles = new Vector3(20, 0, 0);
                camComp.clearFlags = CameraClearFlags.Skybox;
                camComp.backgroundColor = Color.black;
                cam.tag = "TrackCam";
                camComp.targetDisplay = nextDisp;
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Actor");
        }

        /// <summary>Deletes this actor after user confirmation.</summary>
        public void DeleteActor()
        {
            bool accept = EditorUtility.DisplayDialog(
                $"Remove Actor {name}?",
                $"Are you sure you want to delete Actor {name}?",
                "Delete",
                "Cancel"
            );

            if (accept)
            {
                TagLayerEditor.TagsAndLayers.RemoveLayer(name);

                // Unparents attached displays before deletion
                PerspectiveProjection cam = GetComponentInChildren<PerspectiveProjection>();
                if (cam != null)
                {
                    cam.transform.parent.transform.SetParent(null);
                }

                Undo.DestroyObjectImmediate(gameObject);
            }
        }

        /// <summary>Renders the editor GUI for editing actor properties.</summary>
        public void EditMenu()
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);

            // Controller field
            EditorGUILayout.BeginHorizontal();
            if (controller != null)
            {
                EditorGUILayout.LabelField(
                    "<color=#66CC00>Controller: </color>",
                    LayoutSettings.linkFieldStyle,
                    LayoutSettings.linkFieldLayout
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    "<color=#EE0000>Controller: </color>",
                    LayoutSettings.linkFieldStyle,
                    LayoutSettings.linkFieldLayout
                );
            }

            controller = (ControllerOutput)
                EditorGUILayout.ObjectField(
                    controller,
                    typeof(ControllerOutput),
                    true,
                    LayoutSettings.linkObjectLayout
                );
            EditorGUILayout.EndHorizontal();

            // Display field
            EditorGUILayout.BeginHorizontal();
            if (display != null)
            {
                EditorGUILayout.LabelField(
                    "<color=#66CC00>Display: </color>",
                    LayoutSettings.linkFieldStyle,
                    LayoutSettings.linkFieldLayout
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    "<color=#EE0000>Display: </color>",
                    LayoutSettings.linkFieldStyle,
                    LayoutSettings.linkFieldLayout
                );
            }

            display = (DisplayObject)
                EditorGUILayout.ObjectField(display, typeof(DisplayObject), true, LayoutSettings.linkObjectLayout);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
