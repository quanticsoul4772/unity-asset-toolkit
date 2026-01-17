using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EasyPath;
using EasyPath.Demo;
using System.IO;
using System.Collections.Generic;

namespace EasyPath.Editor
{
    /// <summary>
    /// Editor utility to create demo scenes for EasyPath.
    /// </summary>
    public static class DemoSceneSetup
    {
        private static readonly Color[] AgentColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1f),   // Blue
            new Color(1f, 0.4f, 0.4f),   // Red
            new Color(0.4f, 1f, 0.4f),   // Green
            new Color(1f, 0.8f, 0.2f),   // Yellow
            new Color(0.8f, 0.4f, 1f),   // Purple
        };

        [MenuItem("EasyPath/Create Demo Scene/Basic Demo (1 Agent)", false, 100)]
        public static void CreateBasicDemoScene()
        {
            CreateDemoScene("EasyPath_BasicDemo", 1, 5);
        }

        [MenuItem("EasyPath/Create Demo Scene/Multi-Agent Demo (5 Agents)", false, 101)]
        public static void CreateMultiAgentDemoScene()
        {
            CreateDemoScene("EasyPath_MultiAgentDemo", 5, 10);
        }

        [MenuItem("EasyPath/Create Demo Scene/Stress Test (20 Agents)", false, 102)]
        public static void CreateStressTestScene()
        {
            CreateDemoScene("EasyPath_StressTest", 20, 20);
        }

        [MenuItem("EasyPath/Fix Existing Demo Scenes", false, 150)]
        public static void FixExistingDemoScenes()
        {
            // Check if the current scene has unsaved changes
            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "The current scene has unsaved changes. Do you want to save before fixing demo scenes?",
                    "Save and Continue",
                    "Cancel");
                
                if (!proceed)
                {
                    return;
                }
                
                EditorSceneManager.SaveScene(currentScene);
            }
            
            // Ensure the Obstacles layer exists
            int obstacleLayer = GetOrCreateLayer("Obstacles");
            if (obstacleLayer == 0)
            {
                Debug.LogError("[EasyPath] Cannot fix scenes - failed to create 'Obstacles' layer. " +
                    "Please manually create it in Edit > Project Settings > Tags and Layers.");
                return;
            }
            
            int obstacleLayerMask = 1 << obstacleLayer;
            
            // Dynamically find all EasyPath demo scenes using AssetDatabase
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene EasyPath_", new[] { "Assets/EasyPath/Demo/Scenes" });
            
            if (sceneGuids.Length == 0)
            {
                Debug.Log("[EasyPath] No EasyPath demo scenes found in Assets/EasyPath/Demo/Scenes.");
                return;
            }
            
            int fixedCount = 0;
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Open the scene
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Find the EasyPathGrid in the scene
                EasyPathGrid[] grids = Object.FindObjectsByType<EasyPathGrid>(FindObjectsSortMode.None);
                bool sceneModified = false;
                
                foreach (EasyPathGrid grid in grids)
                {
                    SerializedObject so = new SerializedObject(grid);
                    SerializedProperty obstacleLayerProp = so.FindProperty("_obstacleLayer");
                    
                    // Check if it's set to Everything (-1 or ~0)
                    if (obstacleLayerProp.intValue == -1 || obstacleLayerProp.intValue == ~0)
                    {
                        obstacleLayerProp.intValue = obstacleLayerMask;
                        so.ApplyModifiedProperties();
                        sceneModified = true;
                        Debug.Log($"[EasyPath] Fixed obstacle layer on grid in {scenePath}");
                    }
                }
                
                // Find ALL objects with colliders that should be obstacles
                // First check the standard Environment/Obstacles parent
                GameObject obstaclesParent = GameObject.Find("Environment/Obstacles");
                if (obstaclesParent != null)
                {
                    foreach (Transform child in obstaclesParent.transform)
                    {
                        if (child.gameObject.layer != obstacleLayer)
                        {
                            child.gameObject.layer = obstacleLayer;
                            sceneModified = true;
                        }
                    }
                }
                
                // Also check for any GameObject named "Obstacle_*" anywhere in the scene
                // This catches obstacles that may have been manually added elsewhere
                GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.StartsWith("Obstacle_") && obj.layer != obstacleLayer)
                    {
                        obj.layer = obstacleLayer;
                        sceneModified = true;
                        Debug.Log($"[EasyPath] Fixed layer on {obj.name}");
                    }
                }
                
                // Also check RuntimeObstacles parent (for dynamically spawned obstacles)
                GameObject runtimeObstacles = GameObject.Find("RuntimeObstacles");
                if (runtimeObstacles != null)
                {
                    foreach (Transform child in runtimeObstacles.transform)
                    {
                        if (child.gameObject.layer != obstacleLayer)
                        {
                            child.gameObject.layer = obstacleLayer;
                            sceneModified = true;
                        }
                    }
                }
                
                if (sceneModified)
                {
                    EditorSceneManager.SaveScene(scene);
                    fixedCount++;
                }
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"[EasyPath] Fixed {fixedCount} demo scene(s). Obstacle layer now set correctly.");
            }
            else
            {
                Debug.Log("[EasyPath] No demo scenes needed fixing (already configured correctly).");
            }
        }

        [MenuItem("EasyPath/Add Demo Scenes to Build Settings", false, 200)]
        public static void AddDemoScenesToBuildSettings()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/EasyPath/Demo/Scenes" });
            
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int addedCount = 0;
            
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Check if scene is already in build settings
                bool exists = false;
                foreach (var scene in scenes)
                {
                    if (scene.path == path)
                    {
                        exists = true;
                        break;
                    }
                }
                
                if (!exists)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    addedCount++;
                }
            }
            
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[EasyPath] Added {addedCount} demo scenes to Build Settings");
        }

        private static void CreateDemoScene(string sceneName, int agentCount, int obstacleCount)
        {
            // Ensure the Obstacles layer exists ONCE at the start
            int obstacleLayer = GetOrCreateLayer("Obstacles");
            bool layerCreationFailed = (obstacleLayer == 0);
            
            if (layerCreationFailed)
            {
                Debug.LogWarning("[EasyPath] WARNING: Could not create 'Obstacles' layer. " +
                    "Pathfinding will detect ALL colliders including the ground!\n" +
                    "To fix: Create an 'Obstacles' layer in Edit > Project Settings > Tags and Layers, " +
                    "then run EasyPath > Fix Existing Demo Scenes.");
            }
            
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create root game objects
            GameObject environmentRoot = new GameObject("Environment");
            GameObject pathfindingRoot = new GameObject("Pathfinding");
            GameObject agentsRoot = new GameObject("Agents");
            GameObject controllersRoot = new GameObject("Controllers");

            // Setup environment
            CreateGround(environmentRoot.transform);
            CreateObstacles(environmentRoot.transform, obstacleCount, obstacleLayer);
            CreateLighting();
            CreateCamera();

            // Setup pathfinding grid
            GameObject gridObject = CreateGrid(pathfindingRoot.transform, obstacleLayer);
            EasyPathGrid grid = gridObject.GetComponent<EasyPathGrid>();

            // Create agents
            for (int i = 0; i < agentCount; i++)
            {
                CreateAgent(agentsRoot.transform, i, agentCount);
            }

            // Setup controllers
            CreateDemoController(controllersRoot.transform);
            CreateClickToMoveController(controllersRoot.transform);
            CreateObstacleSpawner(controllersRoot.transform);
            
            if (agentCount > 1)
            {
                CreateMultiAgentDemo(controllersRoot.transform);
            }

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(newScene);

            // Ensure Demo/Scenes folder exists
            string scenesPath = "Assets/EasyPath/Demo/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/EasyPath/Demo"))
                {
                    AssetDatabase.CreateFolder("Assets/EasyPath", "Demo");
                }
                AssetDatabase.CreateFolder("Assets/EasyPath/Demo", "Scenes");
            }

            // Save scene
            string scenePath = $"{scenesPath}/{sceneName}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"[EasyPath] Demo scene created: {scenePath}");
            Debug.Log($"[EasyPath] Created {agentCount} agents and {obstacleCount} obstacles");
            Debug.Log("[EasyPath] Press Play to test the pathfinding!");
        }

        private static void CreateGround(Transform parent)
        {
            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(10f, 0f, 10f);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.layer = LayerMask.NameToLayer("Default");

            // Create ground material
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.4f, 0.5f, 0.4f);
            ground.GetComponent<Renderer>().material = groundMat;

            // Tag as ground for raycasting
            ground.tag = "Untagged";
        }

        private static void CreateObstacles(Transform parent, int count, int obstacleLayer)
        {
            GameObject obstaclesParent = new GameObject("Obstacles");
            obstaclesParent.transform.SetParent(parent);

            // Predefined obstacle positions for a nice layout
            Vector3[] obstaclePositions = GenerateObstaclePositions(count);

            for (int i = 0; i < count; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i}";
                obstacle.transform.SetParent(obstaclesParent.transform);
                
                // Assign to Obstacles layer so the grid can detect it
                obstacle.layer = obstacleLayer;
                
                // Random size variation
                float scaleX = Random.Range(1f, 3f);
                float scaleZ = Random.Range(1f, 3f);
                float scaleY = Random.Range(1f, 2.5f);
                obstacle.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                obstacle.transform.position = obstaclePositions[i] + Vector3.up * (scaleY / 2f);

                // Random rotation (only Y axis)
                obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 45f), 0);

                // Dark material
                Material obstacleMat = new Material(Shader.Find("Standard"));
                obstacleMat.color = new Color(0.3f, 0.3f, 0.35f);
                obstacle.GetComponent<Renderer>().material = obstacleMat;

                // Make sure it has a collider for obstacle detection
                // Cube primitive already has BoxCollider
            }
        }

        private static Vector3[] GenerateObstaclePositions(int count)
        {
            Vector3[] positions = new Vector3[count];
            float gridSize = 20f;
            float margin = 3f; // Keep obstacles away from edges

            for (int i = 0; i < count; i++)
            {
                // Generate positions avoiding the center spawn area
                Vector3 pos;
                int attempts = 0;
                do
                {
                    pos = new Vector3(
                        Random.Range(margin, gridSize - margin),
                        0f,
                        Random.Range(margin, gridSize - margin)
                    );
                    attempts++;
                } while (IsNearSpawnArea(pos) && attempts < 20);

                positions[i] = pos;
            }

            return positions;
        }

        private static bool IsNearSpawnArea(Vector3 pos)
        {
            // Keep a clear area around (10, 0, 2) where agents spawn
            Vector3 spawnCenter = new Vector3(10f, 0f, 2f);
            return Vector3.Distance(new Vector3(pos.x, 0, pos.z), spawnCenter) < 4f;
        }

        private static GameObject CreateGrid(Transform parent, int obstacleLayer)
        {
            GameObject gridObject = new GameObject("EasyPath Grid");
            gridObject.transform.SetParent(parent);
            gridObject.transform.position = Vector3.zero;

            EasyPathGrid grid = gridObject.AddComponent<EasyPathGrid>();
            
            // Convert layer index to layer mask
            int obstacleLayerMask = 1 << obstacleLayer;
            
            // Configure grid via serialized fields using SerializedObject
            SerializedObject so = new SerializedObject(grid);
            so.FindProperty("_width").intValue = 20;
            so.FindProperty("_height").intValue = 20;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_obstacleLayer").intValue = obstacleLayerMask; // Only detect Obstacles layer
            so.FindProperty("_obstacleCheckRadius").floatValue = 0.4f;
            so.FindProperty("_obstacleCheckHeight").floatValue = 0.5f; // Check above ground level
            so.FindProperty("_showDebugGizmos").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return gridObject;
        }

        private static void CreateAgent(Transform parent, int index, int totalAgents)
        {
            GameObject agentObject = new GameObject($"Agent_{index}");
            agentObject.transform.SetParent(parent);

            // Position agents in a row at the bottom of the grid
            float spacing = Mathf.Min(2f, 18f / totalAgents);
            float startX = 10f - (totalAgents - 1) * spacing / 2f;
            agentObject.transform.position = new Vector3(startX + index * spacing, 0f, 2f);

            // Add agent component
            EasyPathAgent agent = agentObject.AddComponent<EasyPathAgent>();
            
            // Configure agent
            SerializedObject so = new SerializedObject(agent);
            so.FindProperty("_speed").floatValue = Random.Range(4f, 6f);
            so.FindProperty("_rotationSpeed").floatValue = 360f;
            so.FindProperty("_showDebugPath").boolValue = true;
            so.FindProperty("_pathColor").colorValue = AgentColors[index % AgentColors.Length];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Create visual representation
            CreateAgentVisual(agentObject.transform, AgentColors[index % AgentColors.Length]);
        }

        private static void CreateAgentVisual(Transform parent, Color color)
        {
            // Body (capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            
            // Remove collider from visual
            Object.DestroyImmediate(body.GetComponent<Collider>());

            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = color;
            body.GetComponent<Renderer>().material = bodyMat;

            // Direction indicator (small sphere at front)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 0.5f, 0.4f);
            indicator.transform.localScale = Vector3.one * 0.2f;
            
            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.color = Color.white;
            indicator.GetComponent<Renderer>().material = indicatorMat;
        }

        private static void CreateLighting()
        {
            // Directional light
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            camera.fieldOfView = 60f;

            // Position camera to see the entire grid
            cameraObject.transform.position = new Vector3(10f, 18f, -5f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            // Add audio listener
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateDemoController(Transform parent)
        {
            GameObject controllerObject = new GameObject("Demo Controller");
            controllerObject.transform.SetParent(parent);
            controllerObject.AddComponent<DemoController>();
        }

        private static void CreateClickToMoveController(Transform parent)
        {
            GameObject clickToMove = new GameObject("Click To Move");
            clickToMove.transform.SetParent(parent);
            
            ClickToMove ctm = clickToMove.AddComponent<ClickToMove>();
            
            // Configure to use default layer for ground
            SerializedObject so = new SerializedObject(ctm);
            so.FindProperty("_groundLayer").intValue = ~0; // All layers
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateObstacleSpawner(Transform parent)
        {
            GameObject spawner = new GameObject("Obstacle Spawner");
            spawner.transform.SetParent(parent);
            spawner.AddComponent<ObstacleSpawner>();
        }

        private static void CreateMultiAgentDemo(Transform parent)
        {
            GameObject multiAgent = new GameObject("Multi Agent Demo");
            multiAgent.transform.SetParent(parent);
            multiAgent.AddComponent<MultiAgentDemo>();
        }
        
        /// <summary>
        /// Gets or creates a Unity layer by name.
        /// Returns the layer index.
        /// </summary>
        private static int GetOrCreateLayer(string layerName)
        {
            // Check if layer already exists
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer != -1)
            {
                return existingLayer;
            }
            
            // Layer doesn't exist - try to create it
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            
            // Layers 0-7 are built-in, user layers start at 8
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    // Found an empty slot - use it
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[EasyPath] Created '{layerName}' layer at index {i}");
                    return i;
                }
            }
            
            // No empty slots found
            Debug.LogWarning($"[EasyPath] Could not create '{layerName}' layer - no empty layer slots available. " +
                "Please manually create an 'Obstacles' layer in Edit > Project Settings > Tags and Layers.");
            return 0; // Return Default layer as fallback
        }
    }
}
