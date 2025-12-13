/// <summary>
/// Provides the ActorWindow class for actor and controller management in the editor.
///
/// Renders the editor window for creating, selecting, editing, and deleting actors
/// and controllers in the VR environment.
/// </summary>
using System.Collections.Generic;
using System.Linq;
using Gimbl;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Manages the editor window for actor and controller operations.
/// </summary>
public class ActorWindow : EditorWindow
{
    /// <summary>The scroll position for the window content.</summary>
    Vector2 scrollPosition = Vector2.zero;

    /// <summary>The delegate type for object creation functions.</summary>
    /// <typeparam name="T">The type of Unity Object to create.</typeparam>
    /// <param name="settings">The menu settings for the creation.</param>
    public delegate void CreateFunc<T>(MenuSettings<T> settings)
        where T : UnityEngine.Object;

    /// <summary>
    /// Stores menu state and selection for a generic Unity Object type.
    /// </summary>
    /// <typeparam name="T">The type of Unity Object this menu manages.</typeparam>
    public class MenuSettings<T>
        where T : UnityEngine.Object
    {
        /// <summary>The display name of the object type.</summary>
        public string typeName;

        /// <summary>The array of foldout visibility states.</summary>
        public bool[] show = { false, false, false, false, false };

        /// <summary>The name for creating new objects.</summary>
        public string name = "";

        /// <summary>The entity ID of the selected object for serialization.</summary>
        public EntityId selectedEntityId;

        /// <summary>The rectangle position for the editing window.</summary>
        public Rect editRect = new Rect();

        /// <summary>The backing field for the selected object.</summary>
        private T _selectedObj;

        /// <summary>The currently selected object.</summary>
        public T selectedObj
        {
            get { return _selectedObj; }
            set
            {
                if (!UnityEngine.Object.ReferenceEquals(value, _selectedObj))
                {
                    _selectedObj = value;
                    if (value != null)
                    {
                        selectedEntityId = value.GetEntityId();
                    }
                    else
                    {
                        selectedEntityId = EntityId.None;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Serializable menu settings for ActorObject selection.
    /// </summary>
    [System.Serializable]
    public class ActorMenuSettings : MenuSettings<ActorObject> { }

    /// <summary>
    /// Serializable menu settings for ControllerOutput selection.
    /// </summary>
    [System.Serializable]
    public class ControllerMenuSettings : MenuSettings<ControllerOutput> { }

    /// <summary>The menu settings for actor management.</summary>
    [SerializeField]
    private ActorMenuSettings actSettings = new ActorMenuSettings() { typeName = "Actor" };

    /// <summary>The menu settings for controller management.</summary>
    [SerializeField]
    private ControllerMenuSettings contSettings = new ControllerMenuSettings() { typeName = "Controller" };

    /// <summary>The available actor model names from Resources.</summary>
    private string[] actorModels;

    /// <summary>The index of the selected model in the dropdown.</summary>
    private int selectedModel = 0;

    /// <summary>Determines whether to add a tracking camera when creating actors.</summary>
    private bool trackCam = true;

    /// <summary>The selected controller type for creation.</summary>
    private ControllerTypes contType = ControllerTypes.LinearTreadmill;

    /// <summary>The current editor window instance.</summary>
    private static EditorWindow currentWindow;

    /// <summary>Shows the ActorWindow editor window.</summary>
    public static void ShowWindow()
    {
        currentWindow = GetWindow<ActorWindow>("Actors", true, typeof(MainWindow));
        currentWindow.Show();
    }

    /// <summary>Loads actor models from Resources when the window is enabled.</summary>
    private void OnEnable()
    {
        Resources.LoadAll<GameObject>("Actors/Mouse");
        UnityEngine.Object[] data = Resources.LoadAll<GameObject>("Actors/Prefabs");
        actorModels = data.Select(x => x.name).ToArray();
        actorModels = actorModels.Union(new string[] { "None" }).ToArray();
    }

    /// <summary>Renders the actor and controller management GUI.</summary>
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Height(position.height),
            GUILayout.Width(position.width)
        );

        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Actors", LayoutSettings.sectionLabel);

        EditorGUILayout.BeginHorizontal(LayoutSettings.editWidth);
        SelectMenu(actSettings);
        if (GUILayout.Button("Delete", LayoutSettings.buttonOp))
            actSettings.selectedObj.DeleteActor();
        EditorGUILayout.EndHorizontal();

        if (actSettings.selectedObj != null)
        {
            actSettings.selectedObj.EditMenu();
        }

        if (EditorApplication.isPlaying)
            GUI.enabled = false;
        actSettings.show[0] = EditorGUILayout.Foldout(actSettings.show[0], "Create");
        if (actSettings.show[0])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Actor", EditorStyles.boldLabel);
            actSettings.name = EditorGUILayout.TextField("Actor Name: ", actSettings.name, LayoutSettings.editFieldOp);
            selectedModel = EditorGUILayout.Popup("Model: ", selectedModel, actorModels, LayoutSettings.editFieldOp);
            trackCam = EditorGUILayout.Toggle("Add Tracking Cam: ", trackCam);
            CreateButton(actSettings);
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Controllers", LayoutSettings.sectionLabel);

        EditorGUILayout.BeginHorizontal(LayoutSettings.editWidth);
        SelectMenu(contSettings);
        if (GUILayout.Button("Delete", LayoutSettings.buttonOp))
            contSettings.selectedObj.master.DeleteController();
        EditorGUILayout.EndHorizontal();

        contSettings.show[0] = EditorGUILayout.Foldout(contSettings.show[0], "Edit");
        if (contSettings.show[0])
        {
            if (contSettings.selectedObj != null)
            {
                EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
                contSettings.selectedObj.master.EditMenu();
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Controller Settings", GUILayout.Width(250)))
                    contSettings.selectedObj.master.SaveController();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                if (EditorApplication.isPlaying)
                    GUI.enabled = false;
                EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Load Controller Settings", GUILayout.Width(250)))
                    contSettings.selectedObj.master.LoadController();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUILayout.EndVertical();
            }
        }

        if (EditorApplication.isPlaying)
            GUI.enabled = false;
        contSettings.show[1] = EditorGUILayout.Foldout(contSettings.show[1], "Create");
        if (contSettings.show[1])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Controller", EditorStyles.boldLabel);
            contSettings.name = EditorGUILayout.TextField(
                "Controller Name: ",
                contSettings.name,
                LayoutSettings.editFieldOp
            );
            contType = (ControllerTypes)EditorGUILayout.EnumPopup("Type: ", contType, LayoutSettings.editFieldOp);
            CreateButton(contSettings);
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();

        GUILayout.EndScrollView();
    }

    /// <summary>Renders the object selection field and handles selection recovery.</summary>
    /// <typeparam name="T">The type of Unity Object to select.</typeparam>
    /// <param name="settings">The menu settings containing the selection state.</param>
    private void SelectMenu<T>(MenuSettings<T> settings)
        where T : UnityEngine.Object
    {
        if (settings.selectedObj == null)
        {
            T obj = null;
            if (settings.selectedEntityId != EntityId.None)
            {
                try
                {
                    obj = (T)EditorUtility.EntityIdToObject(settings.selectedEntityId);
                }
                catch (System.InvalidCastException)
                {
                    obj = null;
                }
            }
            if (obj == null)
            {
                obj = FindAnyObjectByType<T>();
            }
            if (obj != null)
            {
                settings.selectedObj = obj;
            }
        }
        settings.selectedObj = (T)EditorGUILayout.ObjectField(settings.selectedObj, typeof(T), true);
    }

    /// <summary>Renders the create button with validation for duplicate and empty names.</summary>
    /// <typeparam name="T">The type of Unity Object to create.</typeparam>
    /// <param name="settings">The menu settings containing the new object name.</param>
    private void CreateButton<T>(MenuSettings<T> settings)
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
        {
            GameObject newObject = new GameObject(settings.name);

            if (typeof(T) == typeof(ControllerOutput))
            {
                ControllerObject controller = (ControllerObject)
                    newObject.AddComponent(System.Type.GetType(string.Format("Gimbl.{0}", contType.ToString())));
                controller.InitiateController();
                ControllerOutput controllerOutput = newObject.AddComponent<ControllerOutput>();
                controllerOutput.master = controller;
                settings.selectedObj = controllerOutput as T;
            }

            if (typeof(T) == typeof(ActorObject))
            {
                ActorObject actor = newObject.AddComponent<ActorObject>();
                actor.InitiateActor(actorModels[selectedModel], trackCam);
                settings.selectedObj = actor as T;
            }

            settings.name = "";
        }
        EditorGUILayout.EndHorizontal();
    }
}
