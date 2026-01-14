/// <summary>
/// Provides GUI layout configuration for Gimbl editor windows.
/// </summary>
using UnityEngine;

namespace Gimbl
{
    /// <summary>
    /// Defines shared GUI layout options and styles for editor windows.
    /// </summary>
    public class LayoutSettings
    {
        /// <summary>The layout option for edit field width.</summary>
        public static GUILayoutOption editWidth = GUILayout.Width(330);

        /// <summary>The layout option for standard edit fields.</summary>
        public static GUILayoutOption editFieldOp = GUILayout.Width(300);

        /// <summary>The layout option for tab text width.</summary>
        public static GUILayoutOption tabTextOp = GUILayout.Width(50);

        /// <summary>The layout option for button width.</summary>
        public static GUILayoutOption buttonOp = GUILayout.Width(100);

        /// <summary>The layout option for link object fields.</summary>
        public static GUILayoutOption linkObjectLayout = GUILayout.Width(147);

        /// <summary>The layout option for link label fields.</summary>
        public static GUILayoutOption linkFieldLayout = GUILayout.Width(150);

        /// <summary>The style for link field labels with rich text support.</summary>
        public static GUIStyle linkFieldStyle = new GUIStyle()
        {
            alignment = TextAnchor.MiddleLeft,
            normal = UnityEditor.EditorStyles.label.normal,
            fontStyle = FontStyle.Normal,
            richText = true,
            fixedWidth = 10,
        };

        /// <summary>The style for section header labels.</summary>
        public static GUIStyle sectionLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleLeft,
            normal = UnityEditor.EditorStyles.label.normal,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            richText = true,
        };

        /// <summary>The style for controller header labels.</summary>
        public static GUIStyle controllerLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleLeft,
            normal = UnityEditor.EditorStyles.label.normal,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            richText = true,
        };

        /// <summary>The sub-box style instance for nested content.</summary>
        public static SubBox subBox = new SubBox();

        /// <summary>The main box style instance for primary content.</summary>
        public static MainBox mainBox = new MainBox();
    }

    /// <summary>
    /// Defines the style for main content boxes in editor windows.
    /// </summary>
    public class MainBox
    {
        /// <summary>The GUI style for main boxes.</summary>
        public GUIStyle style;

        /// <summary>Creates a new main box style based on HelpBox.</summary>
        public MainBox()
        {
            style = new GUIStyle("HelpBox");
            style.margin = new RectOffset(10, 10, 10, 5);
            style.padding = new RectOffset(10, 5, 5, 15);
            style.fixedWidth = 350;
        }
    }

    /// <summary>
    /// Defines the style for nested content boxes in editor windows.
    /// </summary>
    public class SubBox
    {
        /// <summary>The GUI style for sub boxes.</summary>
        public GUIStyle style;

        /// <summary>Creates a new sub box style based on HelpBox.</summary>
        public SubBox()
        {
            style = new GUIStyle("HelpBox");
            style.margin = new RectOffset(15, 15, 10, 5);
            style.padding = new RectOffset(10, 5, 5, 15);
        }
    }
}
