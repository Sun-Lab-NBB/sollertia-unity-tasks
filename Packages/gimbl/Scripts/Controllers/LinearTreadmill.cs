/// <summary>
/// Provides the LinearTreadmill class for handling physical treadmill input via MQTT.
///
/// Receives movement data from an external treadmill device and translates it
/// to actor position updates in the VR environment.
/// </summary>
using UnityEditor;
using UnityEngine;

namespace Gimbl
{
    /// <summary>
    /// Handles linear treadmill input from MQTT and updates actor position.
    /// </summary>
    public class LinearTreadmill : ControllerObject
    {
        /// <summary>The settings for this treadmill controller.</summary>
        public LinearTreadmillSettings settings;

        /// <summary>The MQTT message class containing movement data.</summary>
        public class MSG
        {
            public float movement;
        }

        /// <summary>The accumulated movement since last frame.</summary>
        private float _moved;

        /// <summary>The cached actor position for updates.</summary>
        private Vector3 _pos;

        /// <summary>The cached actor rotation for updates.</summary>
        private Quaternion _newRot;

        /// <summary>Sets up the MQTT listener for this treadmill on start.</summary>
        void Start()
        {
            if (this.GetType() == typeof(LinearTreadmill))
            {
                MQTTChannel<MSG> channel = new MQTTChannel<MSG>($"{settings.deviceName}/Data");
                channel.Event.AddListener(OnMessage);
            }
        }

        /// <summary>Processes accumulated movement each frame.</summary>
        public void Update()
        {
            ProcessMovement();
        }

        /// <summary>Applies accumulated movement to the actor's position.</summary>
        public void ProcessMovement()
        {
            lock (movement)
            {
                if (Actor != null && settings.isActive)
                {
                    _moved = movement.Sum();

                    _pos = Actor.transform.position;
                    _newRot = Actor.transform.rotation;

                    _pos.z = _pos.z + _moved;

                    if (Actor.isActive)
                    {
                        Actor.transform.position = _pos;
                        Actor.transform.rotation = _newRot;
                    }
                }

                movement.Clear();
            }
        }

        /// <summary>MQTT callback that receives movement data from the treadmill.</summary>
        /// <param name="msg">The message containing the movement value.</param>
        public void OnMessage(MSG msg)
        {
            lock (movement)
            {
                movement.Add(msg.movement);
            }
        }

        /// <summary>Creates or links the settings ScriptableObject for this controller.</summary>
        /// <param name="assetPath">The path to an existing settings asset, or empty to create new.</param>
        public override void LinkSettings(string assetPath = "")
        {
            LinearTreadmillSettings asset;

            if (assetPath == "")
            {
                asset = ScriptableObject.CreateInstance<LinearTreadmillSettings>();
                AssetDatabase.CreateAsset(asset, $"Assets/VRSettings/Controllers/{this.gameObject.name}.asset");
            }
            else
            {
                asset = (LinearTreadmillSettings)
                    AssetDatabase.LoadAssetAtPath(assetPath, typeof(LinearTreadmillSettings));
            }

            settings = asset;
        }

        /// <summary>Renders the editor GUI for this controller.</summary>
        public override void EditMenu()
        {
            SerializedObject serializedObject = new SerializedObject(settings);

            if (this.GetType() == typeof(SimulatedLinearTreadmill))
            {
                ControllerMenuTitle(settings.isActive, "Simulated Linear Treadmill");
                EditorGUILayout.LabelField("Device", EditorStyles.boldLabel);

                if (EditorApplication.isPlaying)
                {
                    GUI.enabled = false;
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("isActive"),
                    new GUIContent("Active"),
                    LayoutSettings.editFieldOp
                );
                EditorGUI.indentLevel--;
                GUI.enabled = true;
            }
            else
            {
                ControllerMenuTitle(settings.isActive, "Linear Treadmill");
                EditorGUILayout.LabelField("Device", EditorStyles.boldLabel);

                if (EditorApplication.isPlaying)
                {
                    GUI.enabled = false;
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("isActive"),
                    new GUIContent("Active"),
                    LayoutSettings.editFieldOp
                );
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("deviceName"),
                    new GUIContent("MQTT Name"),
                    LayoutSettings.editFieldOp
                );
                EditorGUI.indentLevel--;
                GUI.enabled = true;
            }
        }
    }
}
