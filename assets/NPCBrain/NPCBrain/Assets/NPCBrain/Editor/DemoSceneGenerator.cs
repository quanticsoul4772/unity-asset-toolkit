using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NPCBrain.Editor
{
    /// <summary>
    /// Editor utility to generate polished demo scenes for GuardNPC and PatrolNPC.
    /// </summary>
    public static class DemoSceneGenerator
    {
        private const string DemoScenesPath = "Assets/NPCBrain/Demo/Scenes";
        
        [MenuItem("NPCBrain/Create Guard Demo Scene", false, 200)]
        public static void CreateGuardDemoScene()
        {
            if (!ConfirmSceneCreation("GuardDemo")) return;
            
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            SetupCamera(new Vector3(0f, 25f, -15f), 55f);
            SetupLighting();
            
            // Create demo controller
            var controller = new GameObject("GuardDemoSetup");
            controller.AddComponent<Demo.GuardDemoSetup>();
            
            // Create info display
            CreateSceneInfo("Guard Demo", "Chase, investigate, and patrol behaviors");
            
            // Save scene
            string scenePath = $"{DemoScenesPath}/GuardDemo.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"<color=green>Guard Demo Scene created at: {scenePath}</color>");
            Debug.Log("Press Play to test. Use WASD to move the player and trigger guard reactions!");
        }
        
        [MenuItem("NPCBrain/Create Patrol Demo Scene", false, 201)]
        public static void CreatePatrolDemoScene()
        {
            if (!ConfirmSceneCreation("PatrolDemo")) return;
            
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            SetupCamera(new Vector3(0f, 30f, -20f), 50f);
            SetupLighting();
            
            // Create demo controller
            var controller = new GameObject("PatrolDemoSetup");
            controller.AddComponent<Demo.PatrolDemoSetup>();
            
            // Create info display
            CreateSceneInfo("Patrol Demo", "Simple waypoint patrol with timing variation");
            
            // Save scene
            string scenePath = $"{DemoScenesPath}/PatrolDemo.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"<color=green>Patrol Demo Scene created at: {scenePath}</color>");
            Debug.Log("Press Play to watch NPCs patrol their color-coded routes!");
        }
        
        [MenuItem("NPCBrain/Create All Demo Scenes", false, 202)]
        public static void CreateAllDemoScenes()
        {
            if (!EditorUtility.DisplayDialog("Create All Demo Scenes",
                "This will create GuardDemo.unity and PatrolDemo.unity in the Demo/Scenes folder.\n\nContinue?",
                "Create All", "Cancel"))
            {
                return;
            }
            
            CreateGuardDemoSceneInternal();
            CreatePatrolDemoSceneInternal();
            
            Debug.Log("<color=green>All demo scenes created successfully!</color>");
        }
        
        [MenuItem("NPCBrain/Open Guard Demo", false, 210)]
        public static void OpenGuardDemo()
        {
            OpenDemoScene("GuardDemo");
        }
        
        [MenuItem("NPCBrain/Open Patrol Demo", false, 211)]
        public static void OpenPatrolDemo()
        {
            OpenDemoScene("PatrolDemo");
        }
        
        private static void CreateGuardDemoSceneInternal()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            SetupCamera(new Vector3(0f, 25f, -15f), 55f);
            SetupLighting();
            
            var controller = new GameObject("GuardDemoSetup");
            controller.AddComponent<Demo.GuardDemoSetup>();
            
            CreateSceneInfo("Guard Demo", "Chase, investigate, and patrol behaviors");
            
            string scenePath = $"{DemoScenesPath}/GuardDemo.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        
        private static void CreatePatrolDemoSceneInternal()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            SetupCamera(new Vector3(0f, 30f, -20f), 50f);
            SetupLighting();
            
            var controller = new GameObject("PatrolDemoSetup");
            controller.AddComponent<Demo.PatrolDemoSetup>();
            
            CreateSceneInfo("Patrol Demo", "Simple waypoint patrol with timing variation");
            
            string scenePath = $"{DemoScenesPath}/PatrolDemo.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        
        private static bool ConfirmSceneCreation(string sceneName)
        {
            string scenePath = $"{DemoScenesPath}/{sceneName}.unity";
            
            if (System.IO.File.Exists(scenePath))
            {
                return EditorUtility.DisplayDialog($"Replace {sceneName}?",
                    $"{sceneName}.unity already exists. Replace it?",
                    "Replace", "Cancel");
            }
            
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
        
        private static void OpenDemoScene(string sceneName)
        {
            string scenePath = $"{DemoScenesPath}/{sceneName}.unity";
            
            if (!System.IO.File.Exists(scenePath))
            {
                if (EditorUtility.DisplayDialog($"{sceneName} Not Found",
                    $"{sceneName}.unity doesn't exist. Create it now?",
                    "Create", "Cancel"))
                {
                    if (sceneName == "GuardDemo")
                        CreateGuardDemoScene();
                    else if (sceneName == "PatrolDemo")
                        CreatePatrolDemoScene();
                }
                return;
            }
            
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
        
        private static void SetupCamera(Vector3 position, float angle)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.Euler(angle, 0f, 0f);
                camera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }
        
        private static void SetupLighting()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    light.intensity = 1.2f;
                    light.shadows = LightShadows.Soft;
                }
            }
            
            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);
        }
        
        private static void CreateSceneInfo(string title, string subtitle)
        {
            var infoObject = new GameObject("SceneInfo");
            infoObject.transform.position = new Vector3(-15f, 0.1f, 15f);
            
            var textMesh = infoObject.AddComponent<TextMesh>();
            textMesh.text = $"NPCBrain - {title}\n{subtitle}\n\nOpen Window > NPCBrain > Debug Window for inspection.";
            textMesh.fontSize = 18;
            textMesh.characterSize = 0.15f;
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.color = new Color(0.3f, 0.3f, 0.3f);
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
        }
    }
}
