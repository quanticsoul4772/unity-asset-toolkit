using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NPCBrain.Editor
{
    /// <summary>
    /// Editor utility to generate TestScene for Week 3 validation.
    /// </summary>
    public static class TestSceneGenerator
    {
        [MenuItem("NPCBrain/Create Test Scene", false, 100)]
        public static void CreateTestScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Setup ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            
            // Setup lighting (use existing directional light from default scene setup)
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (lights.Length > 0)
            {
                lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            
            // Setup camera
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 15f, -10f);
                camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            }
            
            // Create test scene controller
            var controller = new GameObject("TestSceneController");
            controller.AddComponent<Demo.TestSceneController>();
            
            // Create info text object
            CreateInfoCanvas();
            
            // Save scene
            string scenePath = "Assets/NPCBrain/Demo/Scenes/TestScene.unity";
            EnsureDirectoryExists(scenePath);
            
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            Debug.Log($"TestScene created at: {scenePath}");
            Debug.Log("Press Play to see NPCs with Utility AI + Criticality behavior variation.");
        }
        
        private static void CreateInfoCanvas()
        {
            // Create a simple 3D text for scene labeling
            var infoObject = new GameObject("SceneInfo");
            infoObject.transform.position = new Vector3(-12f, 0.1f, 12f);
            
            var textMesh = infoObject.AddComponent<TextMesh>();
            textMesh.text = "NPCBrain Test Scene\nWeek 3: Utility AI + Criticality\n\nWatch NPCs choose actions based on utility scores.\nCriticality adjusts randomness over time.";
            textMesh.fontSize = 20;
            textMesh.characterSize = 0.15f;
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.color = Color.black;
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
        }
        
        [MenuItem("NPCBrain/Open Test Scene", false, 101)]
        public static void OpenTestScene()
        {
            string scenePath = "Assets/NPCBrain/Demo/Scenes/TestScene.unity";
            
            if (!System.IO.File.Exists(scenePath))
            {
                if (EditorUtility.DisplayDialog("TestScene Not Found", 
                    "TestScene doesn't exist. Create it now?", "Create", "Cancel"))
                {
                    CreateTestScene();
                }
                return;
            }
            
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}
