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

        [MenuItem("SwarmAI/Create Demo Scene/Create All Demo Scenes", false, 50)]
        public static void CreateAllDemoScenes()
        {
            if (!CheckUnityVersionCompatibility())
            {
                return;
            }
            
            bool proceed = EditorUtility.DisplayDialog(
                "Create All Demo Scenes",
                "This will create 4 demo scenes:\n\n" +
                "- SwarmAI_FlockingDemo (30 agents)\n" +
                "- SwarmAI_FormationDemo (10 agents)\n" +
                "- SwarmAI_ResourceGatheringDemo (15 agents)\n" +
                "- SwarmAI_CombatFormationsDemo (10 agents)\n\n" +
                "Any existing scenes with the same names will be overwritten.\n\n" +
                "Continue?",
                "Create All",
                "Cancel"
            );
            
            if (!proceed) return;
            
            EditorUtility.DisplayProgressBar("Creating Demo Scenes", "Creating Flocking Demo...", 0.1f);
            CreateFlockingScene("SwarmAI_FlockingDemo", 30);
            
            EditorUtility.DisplayProgressBar("Creating Demo Scenes", "Creating Formation Demo...", 0.3f);
            CreateFormationScene("SwarmAI_FormationDemo", 10);
            
            EditorUtility.DisplayProgressBar("Creating Demo Scenes", "Creating Resource Gathering Demo...", 0.5f);
            CreateResourceGatheringScene("SwarmAI_ResourceGatheringDemo", 15);
            
            EditorUtility.DisplayProgressBar("Creating Demo Scenes", "Creating Combat Formations Demo...", 0.7f);
            CreateCombatFormationsScene("SwarmAI_CombatFormationsDemo", 10);
            
            EditorUtility.DisplayProgressBar("Creating Demo Scenes", "Refreshing assets...", 0.9f);
            AssetDatabase.Refresh();
            
            EditorUtility.ClearProgressBar();
            
            EditorUtility.DisplayDialog(
                "Demo Scenes Created",
                "All 4 demo scenes have been created successfully!\n\n" +
                "Location: Assets/EasyPath/Assets/SwarmAI/Demo/Scenes/\n\n" +
                "Next steps:\n" +
                "1. Open a demo scene\n" +
                "2. Press Play to test\n" +
                "3. Use SwarmAI > Add Demo Scenes to Build Settings",
                "OK"
            );
            
            Debug.Log("[SwarmAI] All demo scenes created successfully!");
        }

        [MenuItem("SwarmAI/Create Demo Scene/Flocking Demo (30 Agents)", false, 100)]
        public static void CreateFlockingDemoScene()
        {
            if (!CheckUnityVersionCompatibility()) return;
            CreateFlockingScene("SwarmAI_FlockingDemo", 30);
        }

        [MenuItem("SwarmAI/Create Demo Scene/Formation Demo (10 Agents)", false, 101)]
        public static void CreateFormationDemoScene()
        {
            if (!CheckUnityVersionCompatibility()) return;
            CreateFormationScene("SwarmAI_FormationDemo", 10);
        }

        [MenuItem("SwarmAI/Create Demo Scene/Resource Gathering Demo (15 Agents)", false, 102)]
        public static void CreateResourceGatheringDemoScene()
        {
            if (!CheckUnityVersionCompatibility()) return;
            CreateResourceGatheringScene("SwarmAI_ResourceGatheringDemo", 15);
        }

        [MenuItem("SwarmAI/Create Demo Scene/Combat Formations Demo (10 Agents)", false, 103)]
        public static void CreateCombatFormationsDemoScene()
        {
            if (!CheckUnityVersionCompatibility()) return;
            CreateCombatFormationsScene("SwarmAI_CombatFormationsDemo", 10);
        }

        [MenuItem("SwarmAI/Validate Package", false, 150)]
        public static void ValidatePackage()
        {
            ValidateSwarmAIPackage();
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

        #region Validation

        private static bool CheckUnityVersionCompatibility()
        {
            string unityVersion = Application.unityVersion;
            
            // Parse major version
            string[] versionParts = unityVersion.Split('.');
            if (versionParts.Length < 1) return true;
            
            // Check for known compatible versions
            bool isCompatible = true;
            string warning = null;
            
            if (unityVersion.StartsWith("2020"))
            {
                warning = "Unity 2020.x is below the minimum supported version (2021.3 LTS).\n" +
                         "Some features may not work correctly.";
            }
            else if (unityVersion.StartsWith("2019") || unityVersion.StartsWith("2018"))
            {
                isCompatible = false;
                warning = "Unity " + versionParts[0] + ".x is not supported.\n" +
                         "Please use Unity 2021.3 LTS or newer.";
            }
            
            if (!isCompatible)
            {
                EditorUtility.DisplayDialog(
                    "Unsupported Unity Version",
                    warning,
                    "OK"
                );
                return false;
            }
            
            if (warning != null)
            {
                return EditorUtility.DisplayDialog(
                    "Unity Version Warning",
                    warning + "\n\nContinue anyway?",
                    "Continue",
                    "Cancel"
                );
            }
            
            return true;
        }

        private static void ValidateSwarmAIPackage()
        {
            var results = new List<string>();
            var warnings = new List<string>();
            int passCount = 0;
            int failCount = 0;
            
            // Assembly validation - if this code runs, assemblies compiled successfully
            // The types are verified at compile time, not runtime
            results.Add("=== Assembly Validation ===");
            results.Add("  [OK] SwarmAI.Runtime - Compiled");
            results.Add("  [OK] SwarmAI.Editor - Compiled");
            results.Add("  [OK] SwarmAI.Demo - Compiled");
            passCount += 3;
            
            // Check demo scenes
            results.Add("");
            results.Add("=== Demo Scenes ===");
            string scenesPath = "Assets/EasyPath/Assets/SwarmAI/Demo/Scenes";
            string[] expectedScenes = new string[]
            {
                "SwarmAI_FlockingDemo.unity",
                "SwarmAI_FormationDemo.unity",
                "SwarmAI_ResourceGatheringDemo.unity",
                "SwarmAI_CombatFormationsDemo.unity"
            };
            
            foreach (string scene in expectedScenes)
            {
                string fullPath = $"{scenesPath}/{scene}";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(fullPath) != null)
                {
                    results.Add($"  [OK] {scene}");
                    passCount++;
                }
                else
                {
                    results.Add($"  [MISSING] {scene} - Use 'Create Demo Scene' menu");
                    warnings.Add($"Demo scene not found: {scene}");
                }
            }
            
            // Check documentation
            results.Add("");
            results.Add("=== Documentation ===");
            string docsPath = "Assets/EasyPath/Assets/SwarmAI/Documentation";
            string[] requiredDocs = new string[]
            {
                "README.md",
                "API-REFERENCE.md",
                "GETTING-STARTED.md",
                "CHANGELOG.md"
            };
            
            foreach (string doc in requiredDocs)
            {
                string fullPath = $"{docsPath}/{doc}";
                if (System.IO.File.Exists(fullPath))
                {
                    results.Add($"  [OK] {doc}");
                    passCount++;
                }
                else
                {
                    results.Add($"  [MISSING] {doc}");
                    failCount++;
                }
            }
            
            // Check package files
            results.Add("");
            results.Add("=== Package Files ===");
            string swarmAIPath = "Assets/EasyPath/Assets/SwarmAI";
            string[] packageFiles = new string[]
            {
                "package.json",
                "LICENSE.txt",
                "THIRD-PARTY-NOTICES.txt"
            };
            
            foreach (string file in packageFiles)
            {
                string fullPath = $"{swarmAIPath}/{file}";
                if (System.IO.File.Exists(fullPath))
                {
                    results.Add($"  [OK] {file}");
                    passCount++;
                }
                else
                {
                    results.Add($"  [MISSING] {file}");
                    failCount++;
                }
            }
            
            // Summary
            results.Add("");
            results.Add($"=== Summary ===");
            results.Add($"  Passed: {passCount}");
            results.Add($"  Failed: {failCount}");
            results.Add($"  Warnings: {warnings.Count}");
            results.Add($"  Unity Version: {Application.unityVersion}");
            
            // Display results
            string resultText = string.Join("\n", results);
            Debug.Log("[SwarmAI Package Validation]\n" + resultText);
            
            string dialogMessage = failCount == 0
                ? $"Validation passed!\n\nPassed: {passCount}\nWarnings: {warnings.Count}\n\nSee Console for details."
                : $"Validation found issues.\n\nPassed: {passCount}\nFailed: {failCount}\nWarnings: {warnings.Count}\n\nSee Console for details.";
            
            EditorUtility.DisplayDialog(
                "SwarmAI Package Validation",
                dialogMessage,
                "OK"
            );
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

        private static void CreateCombatFormationsScene(string sceneName, int agentCount)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject environmentRoot = new GameObject("Environment");
            GameObject agentsRoot = new GameObject("Agents");
            GameObject controllersRoot = new GameObject("Controllers");

            CreateGround(environmentRoot.transform, 60f);
            CreateLighting();
            // Position camera at origin so _boundsCenter will be (0,0,0)
            CreateCamera(new Vector3(0, 35f, 0f), 90f, true);

            CreateSwarmManager(controllersRoot.transform);

            // Match the script's _teamSpacing default of 15f
            // Teams are positioned at _boundsCenter ± (teamSpacing / 2)
            float teamSpacing = 15f;
            int halfCount = agentCount / 2;
            Color team1Color = new Color(0.2f, 0.4f, 1f); // Blue
            Color team2Color = new Color(1f, 0.3f, 0.2f); // Red

            // Team 1 - Left side (at -teamSpacing/2 = -7.5)
            Vector3 team1Center = new Vector3(-teamSpacing / 2f, 0, 0);
            for (int i = 0; i < halfCount; i++)
            {
                CreateSwarmAgent(agentsRoot.transform, i, agentCount, team1Center, 3f, team1Color);
            }

            // Team 2 - Right side (at +teamSpacing/2 = +7.5)
            Vector3 team2Center = new Vector3(teamSpacing / 2f, 0, 0);
            for (int i = halfCount; i < agentCount; i++)
            {
                CreateSwarmAgent(agentsRoot.transform, i, agentCount, team2Center, 3f, team2Color);
            }

            // Create demo controller
            GameObject demoController = new GameObject("Combat Formations Demo Controller");
            demoController.transform.SetParent(controllersRoot.transform);
            var combatDemo = demoController.AddComponent<Demo.CombatFormationsDemo>();
            
            SerializedObject so = new SerializedObject(combatDemo);
            so.FindProperty("_demoTitle").stringValue = "SwarmAI - Combat Formations Demo";
            so.FindProperty("_autoSpawnAgents").boolValue = false;
            so.FindProperty("_agentCount").intValue = agentCount;
            so.FindProperty("_teamSpacing").floatValue = teamSpacing;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveDemoScene(newScene, sceneName);

            Debug.Log($"[SwarmAI] Combat formations demo scene created with {agentCount} agents (2 teams of {halfCount})");
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

            Material groundMat = CreateMaterial(new Color(0.35f, 0.45f, 0.35f));
            ground.GetComponent<Renderer>().material = groundMat;
        }

        private static Material CreateMaterial(Color color)
        {
            // Try to find an appropriate shader based on render pipeline
            Shader shader = Shader.Find("Standard");
            
            // If Standard shader not found (URP/HDRP), try alternatives
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("HDRP/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            
            if (shader == null)
            {
                Debug.LogWarning("[SwarmAI] Could not find suitable shader. Materials may appear incorrect.");
                shader = Shader.Find("Hidden/InternalErrorShader");
            }
            
            Material mat = new Material(shader);
            mat.color = color;
            return mat;
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

                Material obstacleMat = CreateMaterial(new Color(0.25f, 0.25f, 0.3f));
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

            Material bodyMat = CreateMaterial(color);
            body.GetComponent<Renderer>().material = bodyMat;

            // Direction indicator (cone-like using scaled sphere)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 0.5f, 0.35f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.25f);

            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            Material indicatorMat = CreateMaterial(Color.white);
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
