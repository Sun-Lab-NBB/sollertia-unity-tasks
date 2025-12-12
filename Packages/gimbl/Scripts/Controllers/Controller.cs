using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gimbl
{
    // Avialable types.
    public enum ControllerTypes
    {
        LinearTreadmill,
        SimulatedLinearTreadmill,
    }

    public abstract class ControllerObject : MonoBehaviour
    {
        public ActorObject Actor;
        public abstract void EditMenu(); // Custom edit menu.
        public abstract void LinkSettings(string assetPath = ""); // Creates or links a settings file (ScriptableObject).

        // Buffer for accumulating linear treadmill input between frames.
        public class ValueBuffer
        {
            private float[] values;
            private int bufferSize;
            private int counter;
            private bool isCircular;

            public ValueBuffer(int size, bool circular)
            {
                bufferSize = size;
                values = new float[bufferSize];
                counter = 0;
                isCircular = circular;
            }

            public void Add(float value)
            {
                values[counter] = value;
                counter++;
                if (counter == bufferSize)
                {
                    counter = isCircular ? 0 : bufferSize - 1;
                }
            }

            public float Sum()
            {
                float result = 0;
                int limit = isCircular ? bufferSize : counter;
                for (int i = 0; i < limit; i++)
                {
                    result += values[i];
                }
                return result;
            }

            public void Clear()
            {
                int limit = isCircular ? bufferSize : counter;
                for (int i = 0; i < limit; i++)
                {
                    values[i] = 0;
                }
                counter = 0;
            }
        }

        public ValueBuffer movement = new ValueBuffer(100, false);

        public void InitiateController()
        {
            gameObject.transform.SetParent(GameObject.Find("Controllers").transform);
            // Create settings file.
            LinkSettings();
            //update main controller parent.
            UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create Controller");
        }

        public void SaveController()
        {
            GameObject controller = this.gameObject;
            // get controller type and file extension.
            string sourceType = UnityEditor
                .AssetDatabase.GetMainAssetTypeAtPath(
                    string.Format("Assets/VRSettings/Controllers/{0}.asset", this.name)
                )
                .ToString();
            string[] s = sourceType.Split('.');
            string extension = s[1];
            // File dialogue.
            string outputFile = UnityEditor.EditorUtility.SaveFilePanel(
                "Save Controller settings as..",
                "",
                "",
                extension
            );
            if (outputFile.Length == 0)
                return;
            UnityEditor.AssetDatabase.SaveAssets();
            string sourcePath = System.IO.Path.Combine(
                Application.dataPath,
                string.Format("VRSettings/Controllers/{0}.asset", controller.name)
            );
            UnityEditor.FileUtil.ReplaceFile(sourcePath, outputFile);
        }

        public void LoadController()
        {
            GameObject controller = this.gameObject;
            // get controller type and file extension.
            string sourceFile = string.Format("Assets/VRSettings/Controllers/{0}.asset", this.gameObject.name);
            string sourceType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(sourceFile).ToString();
            string[] s = sourceType.Split('.');
            string extension = s[1];
            // File Dialogue.
            string inputFile = UnityEditor.EditorUtility.OpenFilePanel("Import Setup", Application.dataPath, extension);
            if (inputFile.Length == 0)
                return;
            // Remove current settings file.
            string settingsFileAssetPath = string.Format("Assets/VRSettings/Controllers/{0}.asset", controller.name);
            UnityEditor.AssetDatabase.DeleteAsset(settingsFileAssetPath);
            // Copy new file to location.
            string newLoc = System.IO.Path.Combine(
                Application.dataPath,
                string.Format("VRSettings/Controllers/{0}.asset", controller.name)
            );
            UnityEditor.FileUtil.CopyFileOrDirectory(inputFile, newLoc);
            UnityEditor.AssetDatabase.ImportAsset(settingsFileAssetPath);
            // Link to controller.
            controller.GetComponent<ControllerObject>().LinkSettings(settingsFileAssetPath);
        }

        public void DeleteController()
        {
            GameObject controller = this.gameObject;
            bool accept = UnityEditor.EditorUtility.DisplayDialog(
                string.Format("Remove Controller {0}?", controller.name),
                string.Format("Are you sure you want to delete Controller {0}?", controller.name),
                "Delete",
                "Cancel"
            );
            if (accept)
            {
                // Not deleting scriptable object asset so delete it can be undone.
                UnityEditor.Undo.DestroyObjectImmediate(controller);
            }
        }

        public void ControllerMenuTitle(bool isActive, string type)
        {
            EditorGUILayout.BeginHorizontal();
            if (isActive && Actor != null)
            {
                EditorGUILayout.LabelField(
                    string.Format("<color=#66CC00>{0}</color> - {1}", name, type),
                    LayoutSettings.controllerLabel
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    string.Format("<color=#EE0000>{0}</color> - {1}", name, type),
                    LayoutSettings.controllerLabel
                );
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
