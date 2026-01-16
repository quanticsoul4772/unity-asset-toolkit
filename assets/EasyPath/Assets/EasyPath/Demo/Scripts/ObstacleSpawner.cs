using UnityEngine;
using EasyPath;

namespace EasyPath.Demo
{
    /// <summary>
    /// Allows spawning and removing obstacles at runtime.
    /// Right-click to spawn obstacles, middle-click to remove.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EasyPathGrid _grid;
        [SerializeField] private Camera _camera;
        
        [Header("Obstacle Settings")]
        [SerializeField] private Vector3 _obstacleSize = new Vector3(1.5f, 1.5f, 1.5f);
        [SerializeField] private Color _obstacleColor = new Color(0.6f, 0.2f, 0.2f);
        
        [Header("Controls")]
        [SerializeField] private KeyCode _spawnKey = KeyCode.Mouse1; // Right click
        [SerializeField] private KeyCode _removeKey = KeyCode.Mouse2; // Middle click
        
        private Transform _obstaclesParent;
        
        private void Start()
        {
            EasyPathDemoInput.Initialize();
            
            if (_grid == null)
            {
                _grid = FindFirstObjectByType<EasyPathGrid>();
            }
            
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            // Find or create obstacles parent
            GameObject obstaclesObj = GameObject.Find("RuntimeObstacles");
            if (obstaclesObj == null)
            {
                obstaclesObj = new GameObject("RuntimeObstacles");
            }
            _obstaclesParent = obstaclesObj.transform;
        }
        
        private void Update()
        {
            // Spawn obstacle on right click
            if (EasyPathDemoInput.RightClickPressed)
            {
                TrySpawnObstacle();
            }
            
            // Remove obstacle on middle click
            if (EasyPathDemoInput.MiddleClickPressed)
            {
                TryRemoveObstacle();
            }
        }
        
        private void TrySpawnObstacle()
        {
            Ray ray = _camera.ScreenPointToRay(EasyPathDemoInput.MousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // Don't spawn on top of existing obstacles
                if (hit.collider.CompareTag("Obstacle"))
                {
                    return;
                }
                
                SpawnObstacle(hit.point);
            }
        }
        
        private void TryRemoveObstacle()
        {
            Ray ray = _camera.ScreenPointToRay(EasyPathDemoInput.MousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                if (hit.collider.CompareTag("Obstacle") || 
                    hit.collider.gameObject.name.StartsWith("RuntimeObstacle"))
                {
                    RemoveObstacle(hit.collider.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Spawn an obstacle at the given position.
        /// </summary>
        public void SpawnObstacle(Vector3 position)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"RuntimeObstacle_{Time.time:F2}";
            obstacle.transform.SetParent(_obstaclesParent);
            obstacle.transform.position = new Vector3(position.x, _obstacleSize.y / 2f, position.z);
            obstacle.transform.localScale = _obstacleSize;
            // Note: Don't set tag as "Obstacle" tag may not exist in project
            // We use name-based detection instead
            
            // Set material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = _obstacleColor;
            obstacle.GetComponent<Renderer>().material = mat;
            
            // Rebuild grid to account for new obstacle
            if (_grid != null)
            {
                _grid.BuildGrid();
            }
            
            Debug.Log($"[ObstacleSpawner] Spawned obstacle at {position}");
        }
        
        /// <summary>
        /// Remove an obstacle.
        /// </summary>
        public void RemoveObstacle(GameObject obstacle)
        {
            Destroy(obstacle);
            
            // Rebuild grid after removing obstacle
            if (_grid != null)
            {
                // Delay rebuild to next frame so the object is destroyed
                Invoke(nameof(RebuildGrid), 0.1f);
            }
            
            Debug.Log($"[ObstacleSpawner] Removed obstacle");
        }
        
        private void RebuildGrid()
        {
            if (_grid != null)
            {
                _grid.BuildGrid();
            }
        }
        
        private void OnGUI()
        {
            // Show obstacle spawner controls at bottom of screen
            GUILayout.BeginArea(new Rect(10, Screen.height - 60, 300, 50));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Obstacles: Right-click to spawn, Middle-click to remove");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
