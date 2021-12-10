using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Alteruna.Trinity.Development
{
    public class Tools : EditorWindow
    {
        [System.Serializable]
        private struct DevClient
        {
            public Scene Scene;
            public Camera MainCamera;
            public Trinity Client;
        }

        [System.Serializable]
        private struct ClientStatistics
        {
            public string Name;
            public Alteruna.Trinity.NetworkStatistics Statistics;
        }

        private const float CONTROLS_HEIGHT = 35.0f;
        private const float GRAPH_HEIGHT = 150.0f;
        private const int STATS_DATA_POINTS = 30;
        private const float GRAPH_DOWNSCALE_RATE = 0.9f;
        private const float GRAPH_LABEL_XOFFSET = -25f;
        private const float GRAPH_LABEL_YOFFSET = -20f;
        private const float GRAPH_LABEL_HEIGHT = 25;
        private const float GRAPH_PADDING_TOP = 20;
        private const float GRAPH_PADDING_BOTTOM = 20;
        private const float GRAPH_PADDING_LEFT = 50;
        private const int GRAPH_LABEL_FONT_SIZE = 10;

        private const int SERVER_CLIENT_INDEX = 0;
        private const int MAX_NUM_CLIENTS = 3;
        private const int DEFAULT_PORT = 20000;
        private const string TEMP_DIR = "Assets/TempDev/";
        private const string DEV_SCENE_NAME = "DevScene";

        private const string DEV_MATERIAL_PATH = "Assets/Trinity/Scripts/DevTools/Materials/Wireframe.mat";
        private const string ARROWL_PATH = "Assets/Trinity/Scripts/DevTools/Textures/arrowL.png";
        private const string ARROWR_PATH = "Assets/Trinity/Scripts/DevTools/Textures/arrowR.png";
        private const string LINE_PATH = "Assets/Trinity/Scripts/DevTools/Textures/arrowLine.png";
        private const string SQUISH_PATH = "Assets/Trinity/Scripts/DevTools/Textures/timeSquish.png";
        private const string DOT_PATH = "Assets/Trinity/Scripts/DevTools/Textures/dot.png";

        public static Material DevMaterial = null;
        public static Texture arrowR = null;
        public static Texture arrowL = null;
        public static Texture lineTexture = null;
        public static Texture squishTexture = null;
        public static Texture dotTexture = null;

        private DevClient mServerClient;
        private List<DevClient> DevClients = new List<DevClient>();
        private List<ClientStatistics> mDevClientsStatistics = new List<ClientStatistics>();
        private HistoryBuffer<float> mIncomingData = new HistoryBuffer<float>(STATS_DATA_POINTS);
        private HistoryBuffer<float> mOutgoingData = new HistoryBuffer<float>(STATS_DATA_POINTS);
        private float mMaxValue = 0.0f;
        private float mStatTimer = 0.0f;
        private float mLastMaxTraffic = 0.0f;
        private Vector2 mScrollPos;

        private SceneTemplateAsset mCurrentSceneTemplate;

        private bool mFinishedSetup = false;

        [MenuItem("Trinity/Tools")]
        static void ShowWindow()
        {
            var window = GetWindow<Tools>();
            window.titleContent = new GUIContent("Trinity Tools");
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(300, 300);
            window.Show();
        }

        private void CreateDevClient()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneAsset currentSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);

            if (currentSceneAsset != null && FindObjectsOfType<Trinity>().Length > 0)
            {
                mServerClient =
                    new DevClient
                    {
                        Scene = currentScene,
                        MainCamera = FindObjectsOfType<Camera>().FirstOrDefault(c => c.tag == "MainCamera"),
                        Client = FindObjectsOfType<Trinity>().FirstOrDefault(c => c.gameObject.scene == currentScene),
                    };

                mServerClient.Client.DevClientIndex = SERVER_CLIENT_INDEX;
                mServerClient.Client.ClientName = "Client0";

                // Unpack all our prefabs
                Undo.RecordObjects(currentScene.GetRootGameObjects(), "prefabs");
                foreach (GameObject root in currentScene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>())
                    {
                        if (PrefabUtility.IsPartOfAnyPrefab(child.gameObject))
                        {
                            PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                        }
                    }
                }

                // Ugly solution to get around template not having any dependencies
                MeshRenderer serverMeshRenderer = mServerClient.Client.gameObject.GetComponent<MeshRenderer>();
                bool hadMeshRenderer = true;
                if (serverMeshRenderer == null)
                {
                    hadMeshRenderer = false;
                    serverMeshRenderer = mServerClient.Client.gameObject.AddComponent<MeshRenderer>();
                }
                if (DevMaterial == null)
                {
                    DevMaterial = (Material)AssetDatabase.LoadAssetAtPath(DEV_MATERIAL_PATH, typeof(Material));
                }
                serverMeshRenderer.material = DevMaterial;
                EditorSceneManager.SaveScene(currentScene);

                // Create a SceneTemplate from the current active scene
                string templatePath = TEMP_DIR + "Template" + DevClients.Count + ".scenetemplate";
                mCurrentSceneTemplate = SceneTemplateService.CreateTemplateFromScene(currentSceneAsset, templatePath);

                foreach (var dep in mCurrentSceneTemplate.dependencies)
                {
                    dep.instantiationMode = TemplateInstantiationMode.Clone;
                }

                // Undo prefab unpacks
                Undo.PerformUndo();

                // Create our new dev scene based on the template we made
                string scenePath = TEMP_DIR + DEV_SCENE_NAME + DevClients.Count + ".unity";
                InstantiationResult newScene = SceneTemplateService.Instantiate(mCurrentSceneTemplate, true, scenePath);

                if (newScene.scene.isLoaded)
                {
                    foreach (GameObject root in newScene.scene.GetRootGameObjects())
                    {
                        if (root.GetComponentsInChildren<Trinity>().Length < 1 &&
                            root.GetComponentsInChildren<Synchronizable>().Length < 1)
                        {
                            DestroyImmediate(root);
                            continue;
                        }

                        foreach (Collider col in root.GetComponentsInChildren<Collider>())
                        {
                            col.enabled = false;
                        }
                    }
                }

                // Disable scene picking
                SceneVisibilityManager.instance.DisablePicking(newScene.scene);

                // Store our newly created DevClient
                DevClients.Add(new DevClient
                {
                    Scene = newScene.scene,
                    MainCamera = FindObjectsOfType<Camera>().FirstOrDefault(c => c.tag == "MainCamera"),
                    Client = FindObjectsOfType<Trinity>().FirstOrDefault(c => c.gameObject.scene == newScene.scene),
                });

                DevClients[DevClients.Count - 1].Client.DevClientIndex = DevClients.Count;
                DevClients[DevClients.Count - 1].Client.ClientName = "Client" + DevClients.Count;
                DevClients[DevClients.Count - 1].Client.IsDevClient = true;

                // Destroy MeshRenderer used to get around scene not having dependencies
                if (!hadMeshRenderer)
                {
                    DestroyImmediate(serverMeshRenderer);

                    MeshRenderer devMeshRenderer = DevClients[DevClients.Count - 1].Client.gameObject.GetComponent<MeshRenderer>();
                    DestroyImmediate(devMeshRenderer);
                }

                EditorSceneManager.SaveScene(currentScene);
                EditorSceneManager.SaveScene(newScene.scene);
            }
        }

        private void DestroyDevClients()
        {
            int loadedScenes = EditorSceneManager.sceneCount;
            for (int i = 0; i < loadedScenes; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                if (scene.name.Contains(DEV_SCENE_NAME))
                {
                    EditorSceneManager.UnloadSceneAsync(scene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            DevClients.Clear();

            if (mServerClient.Client == null)
            {
                mServerClient.Client = FindObjectsOfType<Trinity>().FirstOrDefault(c => c.gameObject.scene == SceneManager.GetActiveScene());
            }

            if (mServerClient.Client != null)
            {
                mServerClient.Client.AutoJoinFirstSession = false;
                mServerClient.Client.AutoJoinOwnSession = false;
                mServerClient.Client.IsDevClient = false;
                mServerClient.Client.BroadcastEnabled = true;
                mServerClient.Client.UseKnownDevDevice = false;
            }
        }

        private void DeleteTempAssets()
        {
            AssetDatabase.DeleteAsset(TEMP_DIR);
            AssetDatabase.Refresh();
        }

        private void SeperateLastDevClient()
        {
            if (DevClients.Count < 1)
                return;

            DevClient client = DevClients[DevClients.Count - 1];

            // Setup client CodecManager and Synchronizables
            GameObject[] clientRootObjects = client.Scene.GetRootGameObjects();
            SynchronizableManager clientCM = null;
            foreach (GameObject root in clientRootObjects)
            {
                clientCM = root.GetComponentInChildren<SynchronizableManager>();
                if (clientCM != null)
                {
                    break;
                }
            }

            foreach (GameObject root in clientRootObjects)
            {
                if (clientCM != null)
                {
                    clientCM.ClearCodecs();

                    foreach (Synchronizable sync in root.GetComponentsInChildren<Synchronizable>())
                    {
                        sync.OverrideSynchronizableManager(clientCM);
                    }
                }
            }

            if (clientCM == null)
            {
                Debug.LogError("Couldn't find a CodecManager in the DevScene.");
                return;
            }

            foreach (GameObject root in clientRootObjects)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.material = DevMaterial;
                }
            }

            // Setup AlterunaConnect objects to avoid collisions
            mServerClient.Client.BroadcastEnabled = false;
            mServerClient.Client.UseKnownDevDevice = false;
            mServerClient.Client.AutoJoinOwnSession = true;

            client.Client.BroadcastEnabled = false;
            client.Client.UseKnownDevDevice = true;
            client.Client.ServerIP = "localhost";
            client.Client.ServerPort = mServerClient.Client.LANServerPort;
            client.Client.LANServerPort = mServerClient.Client.LANServerPort + DevClients.Count;
            client.Client.PublishPort = mServerClient.Client.PublishPort + DevClients.Count;
            client.Client.AutoJoinFirstSession = true;
            client.Client.AutoJoinOwnSession = false;
        }

        private void OnEnable()
        {
            // Find assets
            if (DevMaterial == null ||
                arrowL == null ||
                arrowR == null ||
                lineTexture == null ||
                squishTexture == null ||
                dotTexture == null)
            {
                DevMaterial = (Material)AssetDatabase.LoadAssetAtPath(DEV_MATERIAL_PATH, typeof(Material));
                arrowL = (Texture)AssetDatabase.LoadAssetAtPath(ARROWL_PATH, typeof(Texture));
                arrowR = (Texture)AssetDatabase.LoadAssetAtPath(ARROWR_PATH, typeof(Texture));
                lineTexture = (Texture)AssetDatabase.LoadAssetAtPath(LINE_PATH, typeof(Texture));
                squishTexture = (Texture)AssetDatabase.LoadAssetAtPath(SQUISH_PATH, typeof(Texture));
                dotTexture = (Texture)AssetDatabase.LoadAssetAtPath(DOT_PATH, typeof(Texture));
            }

            EditorApplication.quitting += DestroyDevClients;
            EditorApplication.quitting += DeleteTempAssets;
        }

        private void CreateTempFolders()
        {
            string projectPath = System.IO.Directory.GetCurrentDirectory() + '/';
            System.IO.Directory.CreateDirectory(projectPath + TEMP_DIR);

            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            GUILayout.Box("Developer Controls", GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create DevClient", GUILayout.Height(CONTROLS_HEIGHT)))
            {
                if (EditorApplication.isPlaying == false)
                {
                    if (DevClients.Count < MAX_NUM_CLIENTS - 1)
                    {
                        CreateTempFolders();

                        CreateDevClient();
                        SeperateLastDevClient();
                    }
                }
            }

            if (GUILayout.Button("Destroy DevClients", GUILayout.Height(CONTROLS_HEIGHT)))
            {
                if (EditorApplication.isPlaying == false)
                {
                    DestroyDevClients();
                    DeleteTempAssets();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("Statistics", GUILayout.ExpandWidth(true));
            DrawStatistics();

            CreateGraph();
        }

        private void DrawStatistics()
        {
            // Title bar
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box("Client", GUILayout.Width(position.width * 0.3f));
            GUILayout.Box("Sent (KB)", GUILayout.Width(position.width * 0.3f));
            GUILayout.Box("Recieved (KB)", GUILayout.Width(position.width * 0.3f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            foreach (ClientStatistics stats in mDevClientsStatistics)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(stats.Name, GUILayout.Width(position.width * 0.3f));
                GUILayout.Box(stats.Statistics?.TotalKilobytesSent.ToString(), GUILayout.Width(position.width * 0.3f));
                GUILayout.Box(stats.Statistics?.TotalKilobytesReceived.ToString(), GUILayout.Width(position.width * 0.3f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void CreateGraph()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(GRAPH_HEIGHT));
            EditorGUI.DrawRect(rect, Color.black);

            UnityEditor.Handles.color = new Color(1, 1, 1, 0.1f);
            DrawScaleLines(rect, mIncomingData.ToArray(), mOutgoingData.ToArray());

            UnityEditor.Handles.color = Color.red;
            DrawGraph(rect, mIncomingData.ToArray());

            UnityEditor.Handles.color = Color.green;
            DrawGraph(rect, mOutgoingData.ToArray());
        }

        private void DrawScaleLines(Rect rect, float[] incoming, float[] outgoing)
        {
            if (incoming.Length < 1 || outgoing.Length < 1)
                return;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            labelStyle.wordWrap = true;
            labelStyle.fontSize = GRAPH_LABEL_FONT_SIZE;
            labelStyle.normal.textColor = Color.white;

            float graphBottom = (rect.yMax - GRAPH_PADDING_BOTTOM);
            float graphTop = rect.height - (GRAPH_PADDING_BOTTOM + GRAPH_PADDING_TOP);

            float average = (incoming.Sum() + outgoing.Sum()) / (incoming.Length + outgoing.Length);
            float max = Mathf.Max(incoming.Max(), outgoing.Max());

            Vector3 from;
            Vector3 to;

            // max value line
            from = new Vector3(rect.xMin + GRAPH_PADDING_LEFT, graphBottom - (graphTop / mMaxValue) * max, 0);
            to = new Vector3(rect.xMax, from.y, 0);
            GUI.Label(new Rect(rect.xMin, to.y - (GRAPH_LABEL_HEIGHT * 0.5f), GRAPH_PADDING_LEFT, GRAPH_LABEL_HEIGHT), (max / 1000).ToString("0.00") + " KB", labelStyle);
            UnityEditor.Handles.DrawLine(from, to);

            // half-max value line
            from = new Vector3(rect.xMin + GRAPH_PADDING_LEFT, graphBottom - (graphTop / mMaxValue) * average, 0);
            to = new Vector3(rect.xMax, from.y, 0);
            GUI.Label(new Rect(rect.xMin, to.y - (GRAPH_LABEL_HEIGHT * 0.5f), GRAPH_PADDING_LEFT, GRAPH_LABEL_HEIGHT), (average / 1000).ToString("0.00") + " KB", labelStyle);
            UnityEditor.Handles.DrawLine(from, to);

            // 0 value line
            from = new Vector3(rect.xMin + GRAPH_PADDING_LEFT, graphBottom, 0);
            to = new Vector3(rect.xMax, graphBottom, 0);
            UnityEditor.Handles.DrawLine(from, to);
        }

        private void DrawGraph(Rect rect, float[] values)
        {
            float step = (rect.width - GRAPH_PADDING_LEFT) / (values.Length - 1);
            Vector3 prevPos = new Vector3(0, 0, 0);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > mMaxValue)
                    mMaxValue = values[i];

                Vector3 pos = new Vector3(step * i, values[i], 0);

                if (i == 0)
                    prevPos.y = values[i];

                // Draw line
                float graphBottom = (rect.yMax - GRAPH_PADDING_BOTTOM);
                float graphTop = rect.height - (GRAPH_PADDING_BOTTOM + GRAPH_PADDING_TOP);
                Vector3 from = new Vector3(rect.xMin + GRAPH_PADDING_LEFT + prevPos.x, graphBottom - (graphTop / mMaxValue) * prevPos.y, 0);
                Vector3 to = new Vector3(rect.xMin + GRAPH_PADDING_LEFT + pos.x, graphBottom - (graphTop / mMaxValue) * pos.y, 0);
                UnityEditor.Handles.DrawLine(from, to);
                prevPos = pos;
            }
        }

        private void Update()
        {
            // Update statistics data
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                mStatTimer += Time.deltaTime;
                if (mServerClient.Client != null)
                {
                    if (mStatTimer > Trinity.STATISITCS_INTERVAL)
                    {
                        mStatTimer = 0.0f;

                        mIncomingData.PushFront(mServerClient.Client.Statistics.BytesPerSecondReceived);
                        mOutgoingData.PushFront(mServerClient.Client.Statistics.BytesPerSecondSent);
                        mLastMaxTraffic = Mathf.Max(mServerClient.Client.Statistics.BytesPerSecondReceived, mServerClient.Client.Statistics.BytesPerSecondSent);
                    }

                    if (mMaxValue > mLastMaxTraffic)
                    {
                        mMaxValue -= (mMaxValue * GRAPH_DOWNSCALE_RATE) * Time.deltaTime;
                    }
                }
            }

            if (EditorApplication.isPlaying)
            {
                if (mFinishedSetup)
                    return;

                mDevClientsStatistics.Clear();
                foreach (Trinity client in FindObjectsOfType<Trinity>())
                {
                    if (client.DevClientIndex == SERVER_CLIENT_INDEX)
                    {
                        mServerClient.Client = client;
                    }

                    mDevClientsStatistics.Add(
                        new ClientStatistics
                        {
                            Name = client.ClientName,
                            Statistics = client.Statistics,
                        }
                    );
                }

                mFinishedSetup = true;
            }
            else
            {
                mFinishedSetup = false;
            }
        }
    }
}