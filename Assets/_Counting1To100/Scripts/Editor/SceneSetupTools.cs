using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace Counting1To100.Editor
{
    public class SceneSetupTools : EditorWindow
    {
        private string _sceneName = "NewScene";
        private string _folderPath = "Assets/_Counting1To100/Scenes";
        
        [MenuItem("Counting1to100/Scene Setup Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupTools>("Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scene Configuration", EditorStyles.boldLabel);
            _sceneName = EditorGUILayout.TextField("Scene Name", _sceneName);
            _folderPath = EditorGUILayout.TextField("Folder Path", _folderPath);

            GUILayout.Space(10);
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Start Scene"))
            {
                CreateScene(SetupStartSceneContent);
            }

            if (GUILayout.Button("Create Game Scene"))
            {
                CreateScene(SetupGameSceneContent);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Add Game Systems (Current Scene)"))
            {
                SetupGameSceneContent(SceneManager.GetActiveScene());
            }
        }

        private void CreateScene(System.Action<Scene> setupAction)
        {
            // 1. Create/Open Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Run specific setup logic
            setupAction(scene);

            // 3. Save Scene
            if (!AssetDatabase.IsValidFolder(_folderPath))
            {
                // Simple recursive folder creation check or just ensure path exists
                Directory.CreateDirectory(_folderPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(_folderPath, _sceneName + ".unity");
            // Ensure Unity path separators
            fullPath = fullPath.Replace("\\", "/"); 
            
            EditorSceneManager.SaveScene(scene, fullPath);
            Debug.Log($"[SceneSetupTools] Scene '{_sceneName}' created at {fullPath}");
            AssetDatabase.Refresh();
        }

        private void SetupStartSceneContent(Scene scene)
        {
            // Create GameManager (Non-persisting)
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // Setup UI
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Wooden Plank (Panel/Image)
            GameObject plankObj = new GameObject("WoodenPlank");
            plankObj.transform.SetParent(canvasObj.transform, false);
            Image plankImage = plankObj.AddComponent<Image>();
            plankImage.color = new Color(0.6f, 0.4f, 0.2f); 
            RectTransform plankRT = plankObj.GetComponent<RectTransform>();
            plankRT.sizeDelta = new Vector2(600, 300);

            // Start Button
            GameObject btnObj = new GameObject("StartButton");
            btnObj.transform.SetParent(plankObj.transform, false);
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = Color.green;
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(200, 60);

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = "START";
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.black;
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Attach UIManager
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>();

            // Assign References via SerializedObject to modify private fields
            SerializedObject so = new SerializedObject(uiManager);
            so.Update();
            
            SerializedProperty playBtnProp = so.FindProperty("_playButton");
            playBtnProp.objectReferenceValue = btn;

            SerializedProperty startPanelProp = so.FindProperty("_startPanel");
            startPanelProp.objectReferenceValue = plankObj;

            so.ApplyModifiedProperties();
        }

        private void SetupGameSceneContent(Scene scene)
        {
            // 1. Ensure Canvas Exists
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = Camera.main;
                c.planeDistance = 10f; 
                
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create GamePanel A(Stretch/Stretch)
            GameObject gamePanel = new GameObject("GamePanel");
            gamePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRT = gamePanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero; 

            // Find UIManager and assign GamePanel
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>(); // Add if missing
            
            SerializedObject so = new SerializedObject(uiManager);
            so.Update();
            SerializedProperty gamePanelProp = so.FindProperty("_gamePanel");
            gamePanelProp.objectReferenceValue = gamePanel;
            so.ApplyModifiedProperties();

            // 3. Setup Bee Spawners (Top Area)
            GameObject beeArea = new GameObject("BeeSpawners");
            beeArea.transform.SetParent(gamePanel.transform, false);
            RectTransform beeRT = beeArea.AddComponent<RectTransform>();
            beeRT.anchorMin = new Vector2(0, 1);
            beeRT.anchorMax = new Vector2(1, 1);
            beeRT.pivot = new Vector2(0.5f, 1);
            beeRT.anchoredPosition = new Vector2(0, -50); // Just below top edge
            
            beeArea.AddComponent<BeeSpawner>();
            
            // Create SpawnPoint child
            GameObject spawnPointObj = new GameObject("SpawnPoint");
            spawnPointObj.transform.SetParent(beeArea.transform, false);
            spawnPointObj.transform.localPosition = Vector3.zero;

            // Assign SpawnPoint to Spawner
            BeeSpawner spawner = beeArea.GetComponent<BeeSpawner>();
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.Update();
            
            SerializedProperty spawnPointProp = spawnerSO.FindProperty("_spawnPoint");
            spawnPointProp.objectReferenceValue = spawnPointObj.transform;

            // Assign Bee Prefab (BeeController)
            string prefabPath = "Assets/_Counting1To100/Prefabs/Bee_Prefab.prefab";
            GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (loadedPrefab != null)
            {
                BeeController beeCtrlPrefab = loadedPrefab.GetComponent<BeeController>();
                if (beeCtrlPrefab != null)
                {
                    SerializedProperty beePrefabProp = spawnerSO.FindProperty("_beePrefab");
                    beePrefabProp.objectReferenceValue = beeCtrlPrefab;
                }
                else
                {
                    Debug.LogError($"[SceneSetupTools] Bee_Prefab at {prefabPath} does not have a BeeController component!");
                }
            }

            spawnerSO.ApplyModifiedProperties();

            // 4. Setup Jars Holder (Bottom Area)
            GameObject jarsArea = new GameObject("JarsHolder");
            jarsArea.transform.SetParent(gamePanel.transform, false);
            RectTransform jarRT = jarsArea.AddComponent<RectTransform>();
            jarRT.anchorMin = new Vector2(0, 0);
            jarRT.anchorMax = new Vector2(1, 0);
            jarRT.pivot = new Vector2(0.5f, 0);
            jarRT.anchoredPosition = new Vector2(0, 50); // Just above bottom edge

            // 5. Create Placeholder Bee (UI)
            // Reuse existing prefabPath variable if it was declared above, or declare if not.
            // Since it was declared above, we just use it.
            if (File.Exists(prefabPath)) 
            {   
                 // We already have the path in 'prefabPath' variable from earlier lines
            }
            
            // To be safe and clean, I will just re-assign or use the variable.
            // But wait, the previous block might be inside an 'if' or not? 
            // Looking at the provided file content:
            // Line 190: string prefabPath = ...
            // Line 218: string prefabPath = ... (This scopes are the same function block)
            
            // So I will just remove the type declaration 'string' 
            
            // prefabPath is already "Assets/_Counting1To100/Prefabs/Bee_Prefab.prefab"
            
            //beePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            //if (beePrefab != null)
            //{
            //    // Instantiate as child of GamePanel for checking
            //    GameObject beeObj = (GameObject)PrefabUtility.InstantiatePrefab(beePrefab, gamePanel.transform);
            //    beeObj.transform.localPosition = Vector3.zero;
            //    beeObj.name = "Bee_Placeholder";
            //}
            //else
            //{
            //    Debug.LogError($"[SceneSetupTools] Bee_Prefab not found at {prefabPath}");
            //}
        }
    }
}
