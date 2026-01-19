using UnityEngine;
using UnityEditor;
using EasyPath;

namespace EasyPath.Editor
{
    /// <summary>
    /// Custom inspector for EasyPathGrid component providing enhanced editing capabilities.
    /// </summary>
    /// <remarks>
    /// Features:
    /// - Organized property sections (Grid Settings, Obstacle Detection, Debug Visualization)
    /// - Real-time grid statistics (total cells, walkable count, grid dimensions)
    /// - One-click grid rebuilding
    /// - Interactive edit mode for toggling cell walkability in Scene view
    /// - Visual feedback with colored wire cubes highlighting cells under cursor
    /// </remarks>
    [CustomEditor(typeof(EasyPathGrid))]
    public class EasyPathGridEditor : UnityEditor.Editor
    {
        private SerializedProperty _width;
        private SerializedProperty _height;
        private SerializedProperty _cellSize;
        private SerializedProperty _obstacleLayer;
        private SerializedProperty _obstacleCheckRadius;
        private SerializedProperty _showDebugGizmos;
        private SerializedProperty _walkableColor;
        private SerializedProperty _blockedColor;
        
        /// <summary>
        /// Tracks whether edit mode is active for cell walkability editing.
        /// </summary>
        private bool _editMode = false;

        /// <summary>
        /// Caches serialized properties on inspector enable.
        /// </summary>
        private void OnEnable()
        {
            _width = serializedObject.FindProperty("_width");
            _height = serializedObject.FindProperty("_height");
            _cellSize = serializedObject.FindProperty("_cellSize");
            _obstacleLayer = serializedObject.FindProperty("_obstacleLayer");
            _obstacleCheckRadius = serializedObject.FindProperty("_obstacleCheckRadius");
            _showDebugGizmos = serializedObject.FindProperty("_showDebugGizmos");
            _walkableColor = serializedObject.FindProperty("_walkableColor");
            _blockedColor = serializedObject.FindProperty("_blockedColor");
        }

        /// <summary>
        /// Renders the custom inspector GUI with organized sections and runtime controls.
        /// </summary>
        /// <remarks>
        /// Layout:
        /// 1. Grid Settings - Width, height, cell size configuration
        /// 2. Obstacle Detection - Layer mask and check radius
        /// 3. Debug Visualization - Gizmo toggles and color settings
        /// 4. Info - Read-only statistics (total/walkable cells, grid dimensions)
        /// 5. Buttons - Rebuild Grid and Edit Mode toggle
        ///
        /// Edit Mode allows Ctrl+Click in Scene view to toggle individual cell walkability.
        /// </remarks>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EasyPathGrid grid = (EasyPathGrid)target;
            
            // Header
            EditorGUILayout.LabelField("EasyPath Grid", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Grid Settings
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_width, new GUIContent("Width", "Number of cells horizontally"));
            EditorGUILayout.PropertyField(_height, new GUIContent("Height", "Number of cells vertically"));
            EditorGUILayout.PropertyField(_cellSize, new GUIContent("Cell Size", "Size of each cell in world units"));
            EditorGUILayout.Space();
            
            // Obstacle Detection
            EditorGUILayout.LabelField("Obstacle Detection", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_obstacleLayer, new GUIContent("Obstacle Layers", "Layers that block pathfinding"));
            EditorGUILayout.PropertyField(_obstacleCheckRadius, new GUIContent("Check Radius", "Radius for obstacle sphere checks"));
            EditorGUILayout.Space();
            
            // Debug Visualization
            EditorGUILayout.LabelField("Debug Visualization", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_showDebugGizmos, new GUIContent("Show Gizmos"));
            
            if (_showDebugGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_walkableColor, new GUIContent("Walkable Color"));
                EditorGUILayout.PropertyField(_blockedColor, new GUIContent("Blocked Color"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Info
            EditorGUILayout.LabelField("Info", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Total Cells", grid.Width * grid.Height);
            EditorGUILayout.IntField("Walkable Cells", grid.WalkableCount);
            EditorGUILayout.FloatField("Grid Size (X)", grid.Width * grid.CellSize);
            EditorGUILayout.FloatField("Grid Size (Z)", grid.Height * grid.CellSize);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Rebuild Grid", GUILayout.Height(30)))
            {
                grid.BuildGrid();
                SceneView.RepaintAll();
            }
            
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _editMode ? Color.yellow : Color.white;
            if (GUILayout.Button(_editMode ? "Exit Edit Mode" : "Edit Mode", GUILayout.Height(30)))
            {
                _editMode = !_editMode;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = prevColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (_editMode)
            {
                EditorGUILayout.HelpBox("Ctrl+Click on cells in Scene view to toggle walkability.", MessageType.Info);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Handles Scene view rendering and input for edit mode.
        /// </summary>
        /// <remarks>
        /// When edit mode is active, draws a yellow wire cube under the cursor
        /// and processes Ctrl+Click events to toggle cell walkability.
        /// </remarks>
        private void OnSceneGUI()
        {
            EasyPathGrid grid = (EasyPathGrid)target;
            
            if (!_editMode)
            {
                return;
            }
            
            HandleCellEditing(grid);
        }

        /// <summary>
        /// Processes mouse input for cell walkability editing in Scene view.
        /// </summary>
        /// <param name="grid">The EasyPathGrid component being edited.</param>
        /// <remarks>
        /// Input:
        /// - Ctrl+Click: Toggles walkability of the clicked cell
        ///
        /// Raycasts against a ground plane at grid position to determine which cell
        /// was clicked. Changes are recorded with Undo support.
        ///
        /// Draws a yellow wire cube under the cursor for visual feedback.
        /// </remarks>
        private void HandleCellEditing(EasyPathGrid grid)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, grid.transform.position);
                
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    Vector2Int cell = grid.WorldToGrid(hitPoint);
                    
                    Undo.RecordObject(grid, "Toggle Cell Walkability");
                    grid.ToggleWalkable(cell.x, cell.y);
                    
                    e.Use();
                    SceneView.RepaintAll();
                }
            }
            
            // Draw handles for visual feedback
            Handles.color = new Color(1f, 1f, 0f, 0.3f);
            
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane plane = new Plane(Vector3.up, grid.transform.position);
            
            if (plane.Raycast(mouseRay, out float dist))
            {
                Vector3 point = mouseRay.GetPoint(dist);
                Vector2Int gridPos = grid.WorldToGrid(point);
                Vector3 cellCenter = grid.GridToWorld(gridPos.x, gridPos.y);
                
                Handles.DrawWireCube(cellCenter, new Vector3(grid.CellSize, 0.2f, grid.CellSize));
            }
            
            HandleUtility.Repaint();
        }
    }
}
