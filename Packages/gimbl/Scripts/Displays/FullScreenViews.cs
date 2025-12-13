/// <summary>
/// Provides full-screen view management for multi-monitor VR displays.
///
/// Manages borderless full-screen game views that run with the Unity editor active,
/// enabling VR studies that use sets of adjacent monitors to display the world.
/// Camera-to-monitor assignments are persisted in per-scene asset files.
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gimbl
{
    /// <summary>
    /// Manages camera-to-monitor assignments and full-screen view creation.
    /// </summary>
    public class FullScreenViewManager
    {
        /// <summary>The list of detected monitors in the system.</summary>
        [SerializeField]
        public List<Monitor> _monitors;

        /// <summary>The saved camera assignments for the current scene.</summary>
        private FullScreenViewsSaved _savedFullScreenViews;

        /// <summary>Initializes the manager by detecting monitors and loading camera assignments.</summary>
        public FullScreenViewManager()
        {
            _monitors = Monitor.EnumeratedMonitors();
            LoadCameras();
        }

        /// <summary>Renders a button to refresh monitor positions.</summary>
        public void OnGUIRefreshMonitorPositions()
        {
            if (GUILayout.Button("Refresh Monitor Positions"))
            {
                List<Monitor> refreshedMonitors = Monitor.EnumeratedMonitors();
                for (int i = 0; i < refreshedMonitors.Count; i++)
                {
                    if (i < _monitors.Count)
                    {
                        refreshedMonitors[i].cameraEntityId = _monitors[i].cameraEntityId;
                    }
                }
                _monitors = refreshedMonitors;
            }
        }

        /// <summary>Renders camera assignment fields for each monitor.</summary>
        public void OnGUICameraObjectFields()
        {
            for (int monitorIndex = 0; monitorIndex < _monitors.Count; monitorIndex++)
            {
                Monitor monitor = _monitors[monitorIndex];
                EditorGUILayout.LabelField(
                    "Monitor " + (monitorIndex + 1).ToString() + " at (" + monitor.left + ", " + monitor.top + ")"
                );
                Camera oldCamera = (Camera)EditorUtility.EntityIdToObject(monitor.cameraEntityId);
                Camera newCamera = (Camera)EditorGUILayout.ObjectField("Camera", oldCamera, typeof(Camera), true);
                if (newCamera != null)
                {
                    EntityId entityId = newCamera.GetEntityId();
                    bool alreadyUsed = false;
                    foreach (Monitor otherMonitor in _monitors)
                    {
                        if (otherMonitor.cameraEntityId == entityId)
                        {
                            alreadyUsed = true;
                            break;
                        }
                    }
                    if (!alreadyUsed)
                    {
                        monitor.cameraEntityId = entityId;
                    }
                }
                else
                {
                    monitor.cameraEntityId = EntityId.None;
                }
                if (newCamera != oldCamera)
                {
                    SaveCameras();
                }
            }
        }

        /// <summary>Renders a button to show full-screen views.</summary>
        public void OnGUIShowFullScreenViews()
        {
            if (GUILayout.Button("Show Full-Screen Views"))
            {
                ShowFullScreenViews(true);
            }
        }

        /// <summary>Creates full-screen views for all monitors with assigned cameras.</summary>
        /// <param name="closeOldViews">If true, closes existing full-screen views before creating new ones.</param>
        public void ShowFullScreenViews(bool closeOldViews)
        {
            List<FullScreenView> existingViews = new List<FullScreenView>(FullScreenView.views);
            foreach (FullScreenView view in existingViews)
            {
                if (closeOldViews)
                {
                    view.Close();
                }
            }

            foreach (Monitor monitor in _monitors)
            {
                Camera camera = (Camera)EditorUtility.EntityIdToObject(monitor.cameraEntityId);
                if (camera != null)
                {
                    FullScreenView window = EditorWindow.CreateInstance<FullScreenView>();

                    float pixelsPerPointX = (monitor.left < 0) ? monitor.pixelsPerPoint : _monitors[0].pixelsPerPoint;
                    int windowX = (int)(monitor.left / pixelsPerPointX);
                    float pixelsPerPointY = (monitor.top < 0) ? monitor.pixelsPerPoint : _monitors[0].pixelsPerPoint;
                    int windowY = (int)(monitor.top / pixelsPerPointY);

                    int windowWidth = (int)(monitor.width / monitor.pixelsPerPoint);
                    int windowHeight = (int)(monitor.height / monitor.pixelsPerPoint);

                    window.position = new Rect(windowX, windowY, windowWidth, windowHeight);
                    window.cameraEntityId = camera.GetEntityId();

                    window.ShowPopup();
                }
            }
        }

        /// <summary>Saves camera assignments to the scene's asset file.</summary>
        public void SaveCameras()
        {
            _savedFullScreenViews.cameraNames.Clear();
            for (int monitorIndex = 0; monitorIndex < _monitors.Count; monitorIndex++)
            {
                Camera camera = (Camera)EditorUtility.EntityIdToObject(_monitors[monitorIndex].cameraEntityId);
                string path = (camera != null) ? PathName(camera.gameObject) : "";
                _savedFullScreenViews.cameraNames.Add(path);
            }
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(_savedFullScreenViews);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Loads camera assignments from the scene's asset file.</summary>
        public void LoadCameras()
        {
            string savedViewsPath =
                "Assets/VRSettings/Displays/"
                + EditorSceneManager.GetActiveScene().name
                + "-savedFullScreenViews.asset";
            _savedFullScreenViews = (FullScreenViewsSaved)
                AssetDatabase.LoadAssetAtPath(savedViewsPath, typeof(FullScreenViewsSaved));

            if (_savedFullScreenViews != null)
            {
                for (int savedIndex = 0; savedIndex < _savedFullScreenViews.cameraNames.Count; savedIndex++)
                {
                    if (savedIndex < _monitors.Count)
                    {
                        string cameraPath = _savedFullScreenViews.cameraNames[savedIndex];
                        GameObject cameraObject = GameObject.Find(cameraPath);
                        if (cameraObject != null)
                        {
                            Camera camera = cameraObject.GetComponent<Camera>();
                            if (camera != null)
                            {
                                _monitors[savedIndex].cameraEntityId = camera.GetEntityId();
                            }
                        }
                    }
                }
            }
            else
            {
                _savedFullScreenViews = ScriptableObject.CreateInstance<FullScreenViewsSaved>();
                AssetDatabase.CreateAsset(_savedFullScreenViews, savedViewsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Returns the full hierarchy path name for a GameObject.</summary>
        /// <param name="gameObject">The GameObject to get the path for.</param>
        /// <returns>The full hierarchy path from root to the GameObject.</returns>
        private string PathName(GameObject gameObject)
        {
            string path = gameObject.name;
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                path = gameObject.name + "/" + path;
            }
            return path;
        }
    }

    /// <summary>
    /// Renders a borderless full-screen game view in an editor window.
    /// </summary>
    public class FullScreenView : EditorWindow
    {
        /// <summary>The list of all active full-screen views.</summary>
        public static List<FullScreenView> views;

        /// <summary>The entity ID of the camera to render.</summary>
        public EntityId cameraEntityId;

        /// <summary>The camera component for rendering.</summary>
        private Camera _camera;

        /// <summary>Determines whether the view is currently rendering.</summary>
        private bool _rendering = false;

        /// <summary>Initializes the static views list.</summary>
        static FullScreenView()
        {
            if (views == null)
            {
                views = new List<FullScreenView>();
            }
        }

        /// <summary>Adds this view to the views list when created.</summary>
        private void Awake()
        {
            views.Add(this);
        }

        /// <summary>Registers the quit handler when enabled.</summary>
        private void OnEnable()
        {
            EditorApplication.wantsToQuit -= OnEditorWantsToQuit;
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;
        }

        /// <summary>Handles GUI events and renders the camera view.</summary>
        void OnGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent.isMouse && currentEvent.button == 0 && !EditorApplication.isPlaying)
            {
                Close();
            }
            else if (currentEvent.type == EventType.Repaint)
            {
                if (_camera == null)
                {
                    _camera = (Camera)EditorUtility.EntityIdToObject(cameraEntityId);
                    if (_camera)
                    {
                        _camera.enabled = false;
                        int renderWidth = (int)position.width;
                        int renderHeight = (int)position.height;
                        _camera.targetTexture = new RenderTexture(
                            renderWidth,
                            renderHeight,
                            24,
                            RenderTextureFormat.ARGB32
                        );
                        _rendering = true;
                    }
                }
                if (_rendering)
                {
                    if (_camera != null)
                    {
                        _camera.Render();
                        bool alphaBlend = false;
                        GUI.DrawTexture(
                            new Rect(0, 0, position.width, position.height),
                            _camera.targetTexture,
                            ScaleMode.ScaleToFit,
                            alphaBlend
                        );
                    }
                }
            }
        }

        /// <summary>Triggers repaint each frame when rendering.</summary>
        private void Update()
        {
            if ((_camera != null) && _rendering)
            {
                Repaint();
            }
        }

        /// <summary>Cleans up camera resources when destroyed.</summary>
        private void OnDestroy()
        {
            _rendering = false;
            _camera.targetTexture = null;
            _camera.enabled = true;
            views.Remove(this);
        }

        /// <summary>Closes this view when the editor is quitting.</summary>
        /// <returns>Always returns true to allow the editor to quit.</returns>
        bool OnEditorWantsToQuit()
        {
            this.Close();
            return true;
        }
    }

    /// <summary>
    /// Stores monitor display information and camera assignment.
    /// </summary>
    [Serializable]
    public class Monitor
    {
        /// <summary>The left position of the monitor in pixels.</summary>
        public int left;

        /// <summary>The top position of the monitor in pixels.</summary>
        public int top;

        /// <summary>The width of the monitor in pixels.</summary>
        public int width;

        /// <summary>The height of the monitor in pixels.</summary>
        public int height;

        /// <summary>The pixels per point scaling factor for this monitor.</summary>
        public float pixelsPerPoint;

        /// <summary>The entity ID of the camera assigned to this monitor.</summary>
        public EntityId cameraEntityId;

        /// <summary>Detects and returns a list of all system monitors.</summary>
        /// <returns>The list of detected monitors with their positions and dimensions.</returns>
        public static List<Monitor> EnumeratedMonitors()
        {
            List<Monitor> result = new List<Monitor>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    delegate(IntPtr hMonitor, IntPtr hdc, ref RectApi monitorRect, IntPtr dwData)
                    {
                        result.Add(
                            new Monitor(monitorRect.left, monitorRect.top, monitorRect.width, monitorRect.height)
                        );
                        return true;
                    },
                    0
                );
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                System.Diagnostics.Process xrandrProcess = new System.Diagnostics.Process();
                xrandrProcess.StartInfo.UseShellExecute = false;
                xrandrProcess.StartInfo.RedirectStandardOutput = true;
                xrandrProcess.StartInfo.FileName = "xrandr";
                xrandrProcess.Start();
                string xrandrOutput = xrandrProcess.StandardOutput.ReadToEnd();
                xrandrProcess.WaitForExit();
                foreach (Match match in Regex.Matches(xrandrOutput, @"(\d+)x(\d+)\+(\d+)\+(\d+)"))
                {
                    result.Add(
                        new Monitor(
                            int.Parse(match.Groups[3].Value),
                            int.Parse(match.Groups[4].Value),
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value)
                        )
                    );
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                System.Diagnostics.Process displayplacerProcess = new System.Diagnostics.Process();
                displayplacerProcess.StartInfo.UseShellExecute = false;
                displayplacerProcess.StartInfo.RedirectStandardOutput = true;
                displayplacerProcess.StartInfo.FileName = "/usr/local/bin/displayplacer";
                displayplacerProcess.StartInfo.Arguments = "list";
                displayplacerProcess.Start();
                string displayplacerOutput = displayplacerProcess.StandardOutput.ReadToEnd();
                displayplacerProcess.WaitForExit();
                foreach (
                    Match match in Regex.Matches(
                        displayplacerOutput,
                        @"Resolution: (\d+)x(\d+)(.|\n)*?Origin: [(](\d+),(\d+)[)]"
                    )
                )
                {
                    result.Add(
                        new Monitor(
                            int.Parse(match.Groups[4].Value),
                            int.Parse(match.Groups[5].Value),
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value)
                        )
                    );
                }
            }

            foreach (Monitor monitor in result)
            {
                MonitorTester tester = EditorWindow.CreateInstance<MonitorTester>();
                tester.position = new Rect(monitor.left, monitor.top, 20, 20);
                tester.monitor = monitor;
                tester.ShowPopup();
            }

            return result;
        }

        /// <summary>Creates a new monitor with the specified position and dimensions.</summary>
        /// <param name="leftPosition">The left position in pixels.</param>
        /// <param name="topPosition">The top position in pixels.</param>
        /// <param name="widthPixels">The width in pixels.</param>
        /// <param name="heightPixels">The height in pixels.</param>
        private Monitor(int leftPosition, int topPosition, int widthPixels, int heightPixels)
        {
            left = leftPosition;
            top = topPosition;
            width = widthPixels;
            height = heightPixels;
            pixelsPerPoint = 1.0f;
            cameraEntityId = EntityId.None;
        }

        /// <summary>The delegate for Windows monitor enumeration callback.</summary>
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RectApi pRect, IntPtr dwData);

        /// <summary>Windows API function to enumerate display monitors.</summary>
        [DllImport("user32")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

        /// <summary>
        /// Windows API rectangle structure for monitor bounds.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RectApi
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public int width
            {
                get { return right - left; }
            }
            public int height
            {
                get { return bottom - top; }
            }
        }

        /// <summary>
        /// Temporary editor window for detecting pixels per point on each monitor.
        /// </summary>
        private class MonitorTester : EditorWindow
        {
            /// <summary>The monitor to test.</summary>
            internal Monitor monitor;

            /// <summary>Records pixels per point and closes immediately.</summary>
            private void OnGUI()
            {
                monitor.pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
                Close();
            }
        }
    }
}
