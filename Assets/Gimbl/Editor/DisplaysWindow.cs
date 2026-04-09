/// <summary>
/// Provides the DisplaysWindow class for VR display management in the editor.
///
/// Renders the editor window for creating, editing, and managing VR displays,
/// and handles camera-to-monitor mapping for full-screen views.
/// </summary>
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gimbl
{
    /// <summary>
    /// Manages the editor window for display configuration and camera mapping.
    /// </summary>
    public class DisplaysWindow : EditorWindow
    {
        /// <summary>The scroll position for the window content.</summary>
        Vector2 scrollPosition = Vector2.zero;

        /// <summary>The delegate type for display creation functions.</summary>
        /// <typeparam name="T">The type of Unity Object to create.</typeparam>
        /// <param name="settings">The menu settings for the creation.</param>
        public delegate void CreateFunc<T>(MenuSettings<T> settings)
            where T : UnityEngine.Object;

        /// <summary>Tracks pending scene changes when exiting play mode.</summary>
        private bool exitPlayModeSceneChangeComing = false;

        /// <summary>
        /// Stores menu state for a generic Unity Object type.
        /// </summary>
        /// <typeparam name="T">The type of Unity Object this menu manages.</typeparam>
        [System.Serializable]
        public class MenuSettings<T>
        {
            /// <summary>The display name of the object type.</summary>
            public string typeName;

            /// <summary>The array of foldout visibility states.</summary>
            public bool[] show = { false, false, false, false, false };

            /// <summary>The name for creating new objects.</summary>
            public string name = "";

            /// <summary>The currently selected object.</summary>
            public T selected;
        }

        /// <summary>
        /// Serializable menu settings for DisplayObject selection.
        /// </summary>
        [System.Serializable]
        public class DisplayMenu : MenuSettings<DisplayObject> { }

        /// <summary>The available display model names from Resources.</summary>
        string[] displayModels;

        /// <summary>The index of the selected model in the dropdown.</summary>
        private int selectedModel = 0;

        /// <summary>The selected display type for creation.</summary>
        Gimbl.DisplayType dispType = Gimbl.DisplayType.Monitor;

        /// <summary>The serialized object for property editing.</summary>
        SerializedObject serializedObject;

        /// <summary>The menu settings for display management.</summary>
        private DisplayMenu dispSettings = new DisplayMenu() { typeName = "Display" };

        /// <summary>The full-screen view manager for camera mapping.</summary>
        [SerializeField]
        public FullScreenViewManager fullScreenManager;

        /// <summary>The current editor window instance.</summary>
        private static EditorWindow currentWindow;

        /// <summary>Shows the DisplaysWindow editor window.</summary>
        public static void ShowWindow()
        {
            if (currentWindow == null)
                currentWindow = GetWindow<DisplaysWindow>("Displays", true, typeof(MainWindow));
        }

        /// <summary>Initializes display models and scene change handlers when enabled.</summary>
        private void OnEnable()
        {
            TagLayerEditor.TagsAndLayers.AddTag("VRDisplay");
            UnityEngine.Object[] data = Resources.LoadAll<GameObject>("Displays");
            displayModels = data.Select(x => x.name).ToArray();
            fullScreenManager = new FullScreenViewManager();

            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>Removes scene change handlers when disabled.</summary>
        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>Reloads camera assignments when the active scene changes.</summary>
        /// <param name="oldScene">The previous active scene.</param>
        /// <param name="newScene">The new active scene.</param>
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (exitPlayModeSceneChangeComing == true)
            {
                exitPlayModeSceneChangeComing = false;
            }
            else
            {
                fullScreenManager.LoadCameras();
            }
        }

        /// <summary>Handles play mode transitions for full-screen view management.</summary>
        /// <param name="state">The play mode state change.</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                fullScreenManager.ShowFullScreenViews(false);
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                exitPlayModeSceneChangeComing = true;
            }
        }

        /// <summary>Renders the display management and camera mapping GUI.</summary>
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Height(position.height),
                GUILayout.Width(position.width)
            );

            EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
            EditorGUILayout.LabelField("Displays", LayoutSettings.sectionLabel);

            EditorGUILayout.BeginHorizontal();
            SelectMenu(dispSettings);
            if (GUILayout.Button("Delete", LayoutSettings.buttonOp))
                DeleteDisplay();
            EditorGUILayout.EndHorizontal();

            if (dispSettings.selected != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (dispSettings.selected.currentBrightness > 0)
                {
                    if (GUILayout.Button("Blank Display"))
                    {
                        dispSettings.selected.currentBrightness = 0;
                    }
                }
                else
                {
                    if (GUILayout.Button("Show Display"))
                    {
                        dispSettings.selected.currentBrightness = dispSettings.selected.settings.brightness;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            dispSettings.show[0] = EditorGUILayout.Foldout(dispSettings.show[0], "Edit");
            if (dispSettings.show[0])
            {
                if (dispSettings.selected != null)
                {
                    EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
                    serializedObject = new SerializedObject(dispSettings.selected.settings);
                    float prevHeight = dispSettings.selected.settings.heightInVR;
                    float prevBrightness = dispSettings.selected.settings.brightness;
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("isActive"),
                        true,
                        LayoutSettings.editFieldOp
                    );
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("brightness"),
                        true,
                        LayoutSettings.editFieldOp
                    );
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("heightInVR"),
                        true,
                        LayoutSettings.editFieldOp
                    );
                    serializedObject.ApplyModifiedProperties();
                    if (prevHeight != dispSettings.selected.settings.heightInVR)
                    {
                        dispSettings.selected.transform.localPosition = new Vector3(
                            0,
                            dispSettings.selected.settings.heightInVR,
                            0
                        );
                    }
                    if (prevBrightness != dispSettings.selected.settings.brightness)
                    {
                        dispSettings.selected.currentBrightness = dispSettings.selected.settings.brightness;
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            if (EditorApplication.isPlaying)
                GUI.enabled = false;
            dispSettings.show[1] = EditorGUILayout.Foldout(dispSettings.show[1], "Create");
            if (dispSettings.show[1])
            {
                EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
                EditorGUILayout.LabelField("Create Display", EditorStyles.boldLabel);
                dispSettings.name = EditorGUILayout.TextField(
                    "Display Name: ",
                    dispSettings.name,
                    LayoutSettings.editFieldOp
                );
                selectedModel = EditorGUILayout.Popup(
                    "Model: ",
                    selectedModel,
                    displayModels,
                    LayoutSettings.editFieldOp
                );
                dispType = (Gimbl.DisplayType)EditorGUILayout.EnumPopup("Type: ", dispType, LayoutSettings.editFieldOp);
                CreateButton(dispSettings, new CreateFunc<DisplayObject>(CreateDisplay));
                EditorGUILayout.EndVertical();
            }
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
            EditorGUILayout.LabelField("Camera Mapping", LayoutSettings.sectionLabel);

            fullScreenManager.OnGUIRefreshMonitorPositions();
            fullScreenManager.OnGUICameraObjectFields();
            if (EditorApplication.isPlaying)
                GUI.enabled = false;
            fullScreenManager.OnGUIShowFullScreenViews();
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Renders the object selection field.</summary>
        /// <typeparam name="T">The type of Unity Object to select.</typeparam>
        /// <param name="settings">The menu settings containing the selection state.</param>
        private void SelectMenu<T>(MenuSettings<T> settings)
            where T : UnityEngine.Object
        {
            T existingObject = FindAnyObjectByType<T>();
            if (settings.selected == null && existingObject != null)
                settings.selected = existingObject;
            settings.selected = (T)EditorGUILayout.ObjectField(settings.selected, typeof(T), true);
        }

        /// <summary>Renders the create button with validation for duplicate and empty names.</summary>
        /// <typeparam name="T">The type of Unity Object to create.</typeparam>
        /// <param name="settings">The menu settings containing the new object name.</param>
        /// <param name="createFunction">The function to call when creating the object.</param>
        private void CreateButton<T>(MenuSettings<T> settings, CreateFunc<T> createFunction)
            where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            T[] existingObjects = FindObjectsByType<T>(FindObjectsSortMode.None);
            string[] existingNames = existingObjects.Select(x => x.name).ToArray();
            string validationMessage = "";
            if (ArrayUtility.Contains(existingNames, settings.name))
            {
                validationMessage = "Duplicate name";
                GUI.enabled = false;
            }
            if (settings.name == "")
            {
                validationMessage = "Empty Name";
                GUI.enabled = false;
            }
            EditorGUILayout.LabelField(validationMessage, GUILayout.Width(197));
            if (GUILayout.Button("Create", LayoutSettings.buttonOp))
                createFunction(settings);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Deletes the currently selected display after confirmation.</summary>
        private void DeleteDisplay()
        {
            GameObject displayObject = dispSettings.selected.gameObject;
            bool confirmDelete = EditorUtility.DisplayDialog(
                string.Format("Remove Display {0}?", displayObject.name),
                string.Format("Are you sure you want to delete Display {0}?", displayObject.name),
                "Delete",
                "Cancel"
            );
            if (confirmDelete)
            {
                Undo.DestroyObjectImmediate(displayObject);
            }
        }

        /// <summary>Creates a new display with the specified settings.</summary>
        /// <typeparam name="T">The type of component for the menu settings.</typeparam>
        /// <param name="settings">The menu settings containing the display name.</param>
        private void CreateDisplay<T>(MenuSettings<T> settings)
            where T : UnityEngine.Component
        {
            UnityEngine.Object modelPrefab = Resources.Load(
                String.Format("Displays/{0}", displayModels[selectedModel])
            );
            GameObject displayObject = Instantiate(modelPrefab) as GameObject;
            displayObject.name = settings.name;
            DisplayObject display = displayObject.AddComponent<DisplayObject>();
            displayObject.tag = "VRDisplay";

            DisplaySettings displaySettings = CreateInstance<DisplaySettings>();
            AssetDatabase.CreateAsset(
                displaySettings,
                string.Format("Assets/VRSettings/Displays/{0}.asset", displayObject.name)
            );
            display.settings = displaySettings;

            switch (dispType)
            {
                case Gimbl.DisplayType.Monitor:
                    MeshRenderer[] meshRenderers = displayObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mesh in meshRenderers)
                    {
                        mesh.GetComponent<MeshCollider>().enabled = false;
                        GameObject cameraObject = new GameObject(String.Format("Camera: {0}", mesh.name));
                        cameraObject.transform.SetParent(mesh.transform.parent);
                        cameraObject.transform.localPosition = new Vector3(0, 0, 0);
                        Camera cameraComponent = cameraObject.AddComponent<Camera>();
                        cameraComponent.nearClipPlane = 0.3f;
                        cameraComponent.targetDisplay = 8;
                        cameraComponent.clearFlags = CameraClearFlags.Skybox;
                        cameraComponent.backgroundColor = Color.black;
                        PerspectiveProjection projection = cameraObject.AddComponent<PerspectiveProjection>();
                        projection.projectionScreen = mesh.gameObject;
                        projection.setNearClipPlane = false;
                        mesh.enabled = false;
                    }
                    break;
            }
            Undo.RegisterCreatedObjectUndo(displayObject, "Create Display");
            settings.selected = display as T;
            settings.name = "";
        }
    }
}
