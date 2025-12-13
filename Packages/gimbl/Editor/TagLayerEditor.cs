/// <summary>
/// Provides utilities for programmatically managing Unity tags and layers.
///
/// Adapted from https://answers.unity.com/questions/33597/is-it-possible-to-create-a-tag-programmatically.html
/// </summary>
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace TagLayerEditor
{
    /// <summary>
    /// Manages Unity tags and layers through the TagManager asset.
    /// </summary>
    public class TagsAndLayers
    {
        /// <summary>The maximum number of tags allowed.</summary>
        private static int maxTags = 10000;

        /// <summary>The maximum number of layers allowed.</summary>
        private static int maxLayers = 31;

        /// <summary>Adds a new tag to the project if it doesn't already exist.</summary>
        /// <param name="tagName">The name of the tag to add.</param>
        /// <returns>True if the tag was added, false if it already exists or limit reached.</returns>
        public static bool AddTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp.arraySize >= maxTags)
            {
                Debug.Log("No more tags can be added to the Tags property. You have " + tagsProp.arraySize + " tags");
                return false;
            }
            if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName))
            {
                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(index);
                newTag.stringValue = tagName;
                tagManager.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        /// <summary>Adds a new layer to the project if it doesn't already exist.</summary>
        /// <param name="layerName">The name of the layer to add.</param>
        /// <returns>True if the layer was added, false if it already exists or no slots available.</returns>
        public static bool AddLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (!PropertyExists(layersProp, 0, maxLayers, layerName))
            {
                SerializedProperty layerSlot;
                for (int layerIndex = 8, lastIndex = maxLayers; layerIndex < lastIndex; layerIndex++)
                {
                    layerSlot = layersProp.GetArrayElementAtIndex(layerIndex);
                    if (layerSlot.stringValue == "")
                    {
                        layerSlot.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        return true;
                    }
                    if (layerIndex == lastIndex)
                        Debug.Log("All allowed layers have been filled");
                }
            }
            return false;
        }

        /// <summary>Removes a layer from the project.</summary>
        /// <param name="layerName">The name of the layer to remove.</param>
        /// <returns>True if the layer was removed, false if it doesn't exist.</returns>
        public static bool RemoveLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );

            SerializedProperty layersProp = tagManager.FindProperty("layers");

            if (PropertyExists(layersProp, 0, layersProp.arraySize, layerName))
            {
                SerializedProperty layerSlot;

                for (int layerIndex = 0, arraySize = layersProp.arraySize; layerIndex < arraySize; layerIndex++)
                {
                    layerSlot = layersProp.GetArrayElementAtIndex(layerIndex);

                    if (layerSlot.stringValue == layerName)
                    {
                        layerSlot.stringValue = "";
                        tagManager.ApplyModifiedProperties();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Checks if a value exists in a serialized array property.</summary>
        /// <param name="property">The serialized array property to search.</param>
        /// <param name="start">The starting index for the search.</param>
        /// <param name="end">The ending index for the search.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>True if the value exists in the property range.</returns>
        private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int elementIndex = start; elementIndex < end; elementIndex++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(elementIndex);
                if (element.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
