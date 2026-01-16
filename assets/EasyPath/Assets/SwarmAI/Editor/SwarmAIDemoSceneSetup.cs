using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace SwarmAI.Editor
{
    /// <summary>
    /// Editor utility to create demo scenes for SwarmAI.
    /// Creates Flocking, Formation, and Resource Gathering demo scenes.
    /// </summary>
    public static class SwarmAIDemoSceneSetup
    {
        private static readonly Color[] AgentColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1f),   // Blue
            new Color(1f, 0.4f, 0.4f),   // Red
            new Color(0.4f, 1f, 0.4f),   // Green
            new Color(1f, 0.8f, 0.2f),   // Yellow
            new Color(0.8f, 0.4f, 1f),   // Purple
            new Color(1f, 0.6f, 0.4f),   // Orange
            new Color(0.4f, 0.8f, 0.8f), // Cyan
            new Color(0.8f, 0.8f, 0.4f), // Lime
        };

        #region Menu Items

        [MenuItem("SwarmAI/Create Demo Scene/Flocking Demo (30 Agents)", false, 100)]
        public static void CreateFlockingDemoScene()
        {
            CreateFlockingScene("SwarmAI_FlockingDemo", 30);
        }

        [MenuItem("SwarmAI/Create Demo Scene/Formation Demo (10 Agents)", false, 101)]
        public static void CreateFormationDemoScene()
        {
            CreateFormationScene("SwarmAI_FormationDemo", 10);
        }

        [MenuItem("SwarmAI/Create Demo Scene/Resource Gathering Demo (15 Agents)", false, 102)]
        public static void CreateResourceGatheringDemoScene()
        {
            CreateResourceGatheringScene("SwarmAI_ResourceGatheringDemo", 15);
        }

        [MenuItem("SwarmAI/Add Demo Scenes to Build Settings", false, 200)]
        public static void AddDemoScenesToBuildSettings()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/EasyPath/Assets/SwarmAI/Demo/Scenes" });
            
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int addedCount = 0;
            
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
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
            Debug.Log($"[SwarmAI] Added {addedCount} demo scenes to Build Settings");
        }

        #endregion

        #region Scene Creation

        private static void CreateFlockingScene(string sceneName, int agentCount)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create hierarchy
            GameObject environmentRoot = new GameObject("Environment");
            GameObject agentsRoot = new GameObject("Agents");
            GameObject controllersRoot = new GameObject("Controllers");

            // Setup environment
            CreateGround(environmentRoot.transform, 40f);
            CreateObstacles(environmentRoot.transform, 8);
            CreateLighting();
            CreateCamera(new Vector3(0, 25f, -15f), 55f, true);

            // Create SwarmManager
            CreateSwarmManager(controllersRoot.transform);

            // Create agent prefab holder
            GameObject prefabHolder = new GameObject("AgentPrefab");
            prefabHolder.transform.SetParent(agentsRoot.transform);
            prefabHolder.SetActive(false);
            CreateAgentVisual(prefabHolder.transform, AgentColors[0]);
            SwarmAgent prefabAgent = prefabHolder.AddComponent<SwarmAgent>();

            // Create agents
            for (int i = 0; i < agentCount; i++)
            {
                CreateSwarmAgent(agentsRoot.transform, i, agentCount, new Vector3(0, 0, 0), 8f);
            }

            // Create demo controller
            GameObject demoController = new GameObject("Flocking Demo Controller");
            demoController.transform.SetParent(controllersRoot.transform);
            var flockingDemo = demoController.AddComponent<Demo.FlockingDemo>();
            
            // Configure via SerializedObject
            SerializedObject so = new SerializedObject(flockingDemo);
            so.FindProperty("_demoTitle").stringValue = "SwarmAI - Flocking Demo";
            so.FindProperty("_autoSpawnAgents").boolValue = false; // We already created them
            so.FindProperty("_agentCount").intValue = agentCount;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save scene
            SaveDemoScene(newScene, sceneName);

            Debug.Log($"[SwarmAI] Flocking demo scene created with {agentCount} agents");
        }

        private static void CreateFormationScene(string sceneName, int agentCount)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject environmentRoot = new GameObject("Environment");
            GameObject agentsRoot = new GameObject("Agents");
            GameObject controllersRoot = new GameObject("Controllers");

            CreateGround(environmentRoot.transform, 50f);
            CreateLighting();
            CreateCamera(new Vector3(0, 30f, -20f), 50f, true);

            CreateSwarmManager(controllersRoot.transform);

            // Create agents in a cluster
            for (int i = 0; i < agentCount; i++)
            {
                Color color = i == 0 ? Color.yellow : AgentColors[(i - 1) % AgentColors.Length];
                CreateSwarmAgent(agentsRoot.transform, i, agentCount, new Vector3(0, 0, -5), 3f, color);
            }

            // Create demo controller
            GameObject demoController = new GameObject("Formation Demo Controller");
            demoController.transform.SetParent(controllersRoot.transform);
            var formationDemo = demoController.AddComponent<Demo.FormationDemo>();
            
            SerializedObject so = new SerializedObject(formationDemo);
            so.FindProperty("_demoTitle").stringValue = "SwarmAI - Formation Demo";
            so.FindProperty("_autoSpawnAgents").boolValue = false;
            so.FindProperty("_agentCount").intValue = agentCount;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveDemoScene(newScene, sceneName);

            Debug.Log($"[SwarmAI] Formation demo scene created with {agentCount} agents");
        }

        private static void CreateResourceGatheringScene(string sceneName, int agentCount)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject environmentRoot = new GameObject("Environment");
            GameObject agentsRoot = new GameObject("Agents");
            GameObject controllersRoot = new GameObject("Controllers");

            CreateGround(environmentRoot.transform, 50f);
            CreateLighting();
            CreateCamera(new Vector3(0, 35f, -25f), 50f, true);

            CreateSwarmManager(controllersRoot.transform);

            // Create agents near base position
            Vector3 basePos = new Vector3(0, 0, -10);
            for (int i = 0; i < agentCount; i++)
            {
                CreateSwarmAgent(agentsRoot.transform, i, agentCount, basePos + Vector3.forward * 3f, 4f);
            }

            // Create demo controller
            GameObject demoController = new GameObject("Resource Gathering Demo Controller");
            demoController.transform.SetParent(controllersRoot.transform);
            var gatheringDemo = demoController.AddComponent<Demo.ResourceGatheringDemo>();
            
            SerializedObject so = new SerializedObject(gatheringDemo);
            so.FindProperty("_demoTitle").stringValue = "SwarmAI - Resource Gathering Demo";
            so.FindProperty("_autoSpawnAgents").boolValue = false;
            so.FindProperty("_agentCount").intValue = agentCount;
            so.FindProperty("_basePosition").vector3Value = basePos;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveDemoScene(newScene, sceneName);

            Debug.Log($"[SwarmAI] Resource gathering demo scene created with {agentCount} agents");
        }

        #endregion

        #region Helper Methods

        private static void CreateGround(Transform parent, float size)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);

            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.35f, 0.45f, 0.35f);
            ground.GetComponent<Renderer>().material = groundMat;
        }

        private static void CreateObstacles(Transform parent, int count)
        {
            GameObject obstaclesParent = new GameObject("Obstacles");
            obstaclesParent.transform.SetParent(parent);

            for (int i = 0; i < count; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i}";
                obstacle.transform.SetParent(obstaclesParent.transform);

                float scaleX = Random.Range(1.5f, 4f);
                float scaleZ = Random.Range(1.5f, 4f);
                float scaleY = Random.Range(1.5f, 3f);
                obstacle.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Random position avoiding center spawn area
                Vector3 pos;
                int attempts = 0;
                do
                {
                    pos = new Vector3(
                        Random.Range(-15f, 15f),
                        scaleY / 2f,
                        Random.Range(-15f, 15f)
                    );
                    attempts++;
                } while (pos.magnitude < 5f && attempts < 20);
                
                obstacle.transform.position = pos;
                obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                Material obstacleMat = new Material(Shader.Find("Standard"));
                obstacleMat.color = new Color(0.25f, 0.25f, 0.3f);
                obstacle.GetComponent<Renderer>().material = obstacleMat;

                // Add to obstacle layer for avoidance
                obstacle.layer = LayerMask.NameToLayer("Default");
            }
        }

        private static void CreateSwarmManager(Transform parent)
        {
            GameObject managerObj = new GameObject("SwarmManager");
            managerObj.transform.SetParent(parent);
            managerObj.AddComponent<SwarmManager>();
        }

        private static GameObject CreateSwarmAgent(Transform parent, int index, int total, Vector3 center, float radius, Color? colorOverride = null)
        {
            GameObject agentObj = new GameObject($"Agent_{index}");
            agentObj.transform.SetParent(parent);

            // Random position within radius
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = 0;
            agentObj.transform.position = center + randomOffset;

            // Add SwarmAgent component
            SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();

            // Configure agent
            SerializedObject so = new SerializedObject(agent);
            so.FindProperty("_maxSpeed").floatValue = Random.Range(4f, 6f);
            so.FindProperty("_rotationSpeed").floatValue = 360f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Create visual
            Color color = colorOverride ?? AgentColors[index % AgentColors.Length];
            CreateAgentVisual(agentObj.transform, color);

            return agentObj;
        }

        private static void CreateAgentVisual(Transform parent, Color color)
        {
            // Body (capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);

            Object.DestroyImmediate(body.GetComponent<Collider>());

            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = color;
            body.GetComponent<Renderer>().material = bodyMat;

            // Direction indicator (cone-like using scaled sphere)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 0.5f, 0.35f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.25f);

            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.color = Color.white;
            indicator.GetComponent<Renderer>().material = indicatorMat;
        }

        private static void CreateLighting()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.9f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static void CreateCamera(Vector3 position, float pitch, bool addController = false)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";

            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.2f, 0.3f);
            camera.fieldOfView = 60f;

            cameraObj.transform.position = position;
            cameraObj.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            cameraObj.AddComponent<AudioListener>();
            
            if (addController)
            {
                cameraObj.AddComponent<Demo.CameraController>();
            }
        }

        private static void SaveDemoScene(Scene scene, string sceneName)
        {
            EditorSceneManager.MarkSceneDirty(scene);

            // Ensure folder exists
            string scenesPath = "Assets/EasyPath/Assets/SwarmAI/Demo/Scenes";
            EnsureFolderExists(scenesPath);

            string scenePath = $"{scenesPath}/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[SwarmAI] Demo scene saved: {scenePath}");
            Debug.Log("[SwarmAI] Press Play to test the demo!");
        }

        private static void EnsureFolderExists(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        #endregion
    }
}
