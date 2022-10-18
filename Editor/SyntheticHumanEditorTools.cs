using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AI.Navigation;
using Unity.CV.SyntheticHumans.Generators;
using Unity.CV.SyntheticHumans.Labelers;
using Unity.CV.SyntheticHumans.Placement;
using Unity.CV.SyntheticHumans.Randomizers;
using Unity.CV.SyntheticHumans.Tags;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.CV.SyntheticHumans.Editor
{
    // * A collection of functions for debugging Synthetic Humans in the editor
    // * It pulls settings from the scenario in the scene, which should have a working HumanAppearanceRandomizer
    // * Settings for placement are overwritten by internal logic
    // * Distribution settings are determined by the humanManager connected to the HumanAppearanceRandomizer
    class SyntheticHumanEditorTools : EditorWindow
    {
        [Tooltip("Object affected by debug functions that step through singular function of human creation")]
        public GameObject testObject;
        public AssetPackManifest assetPackManifest;
        public SyntheticHumanAssetPool targetAssetPool;
        [Tooltip("HumanGenerationConfig to use when creating people")]
        public HumanGenerationConfig generationConfig;
        [Tooltip("Number of humans to spawn when the Test Generation on Distribution button is pressed")]
        public int humanCount;
        [Tooltip("Number of humans that will appear before a new row is started when the Test Generation on Distribution button is pressed")]
        public int humansPerRow;
        [Tooltip("Spacing between humans when testing generation on distribution. X is applied between each human, and Z determines row spacing")]
        public Vector3 spacing;

        [Tooltip("In Scene GameObject with a SingleHumanSpecification and SingleHumanGenerationAssetRefs")]
        public GameObject humanSpecParentObject;
        public bool printDebugWithTagApplication;

        List<GameObject> m_GeneratedHumans = new List<GameObject>();
        List<Animator> m_GeneratedAnimators = new List<Animator>();
        AnimationClip m_HumanDebugAnimation;

        bool m_ShowAssetTools = true;
        bool m_ShowGenerationTools = true;
        bool m_ShowDebugTools;

        bool m_ShowTaginfoTools;
        string m_TaginfoEditKey;
        string m_TaginfoEditNewKey;
        string m_TaginfoEditValue;
        bool m_TaginfoEditAsInt;
        bool m_deleteIncompleteHumans;

        bool m_ShowAnimationTools;
        AnimationTag m_AnimationTag;

        bool m_ShowPlacementTools;
        AnimationPlacementGroup m_AnimationPlacementGroup;
        NavMeshSurface m_NavMeshSurface;
        Camera m_PlacementCamera;

        bool m_ShowCollisionTools;
        GameObject m_CollisionObject;

        Vector2 m_ScrollPosition = Vector2.zero;

        // Name of the window and location
        [MenuItem("Window/Synthetic Humans/Synthetic Humans Editor Tools")]
        public static void OpenMainWindow()
        {
            GetWindow<SyntheticHumanEditorTools>("Synthetic Humans Editor Tools");
        }

        public void BulkUpdateSelectedTaginfos()
        {
            var selectedTextFiles = Selection.GetFiltered<SyntheticHumanTag>(SelectionMode.Assets);

            foreach (var tag in selectedTextFiles)
            {
                var path = AssetDatabase.GetAssetPath(tag);
                if (Path.GetExtension(path) != ".taginfo")
                {
                    continue;
                }

                var asDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));

                object newValue;
                var useEditValue = !string.IsNullOrEmpty(m_TaginfoEditValue);
                if (useEditValue && m_TaginfoEditAsInt)
                {
                    newValue = Int32.Parse(m_TaginfoEditValue);
                }
                else if (useEditValue)
                {
                    newValue = m_TaginfoEditValue;
                }
                else
                {
                    try
                    {
                        // If this key doesn't already exist, just continue
                        newValue = asDictionary[m_TaginfoEditKey];
                    }
                    catch
                    {
                        continue;
                    }
                }

                asDictionary.Remove(m_TaginfoEditKey);
                asDictionary[m_TaginfoEditNewKey] = newValue;

                var serialized = JsonConvert.SerializeObject(asDictionary, Formatting.Indented);

                File.WriteAllText(path, serialized);
                EditorUtility.SetDirty(tag);
            }

            AssetDatabase.SaveAssets();
        }

        public void GenerateHumans()
        {
            var configCopy = Instantiate(generationConfig);
            configCopy.Init();

            for (var i = 0; i < humanCount; i++)
            {
                var human = HumanGenerator.GenerateHuman(configCopy, m_deleteIncompleteHumans);

                if (!human)
                {
                    continue;
                }

                human.name = $"{i}";

                human.transform.position = new Vector3(spacing.x * (i % humansPerRow), spacing.y * i, spacing.z * Mathf.Floor(i / (float)humansPerRow));
                var centerOffset = new Vector3(spacing.x * (humansPerRow / 2f), 0, spacing.z * (Mathf.Floor(humanCount / (float)humansPerRow) / 2f));
                human.transform.Translate(-centerOffset);

                if (m_HumanDebugAnimation)
                {
                    var animator = human.GetComponent<Animator>();
                    var controller = new AnimatorOverrideController(Resources.Load<RuntimeAnimatorController>("AnimationRandomizerController"));
                    controller["PlayerIdle"] = m_HumanDebugAnimation;
                    animator.runtimeAnimatorController = controller;
                    animator.Play("Base Layer.RandomState", 0, 0);

                    m_GeneratedAnimators.Add(animator);
                }

                m_GeneratedHumans.Add(human);
            }
        }

        void Update()
        {
            foreach (var animator in m_GeneratedAnimators)
            {
                animator.Update(Time.deltaTime/7);
            }
        }

        // Basically the update function
        void OnGUI()
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, false, false);

            // Group for Asset Management tools
            m_ShowAssetTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowAssetTools, "Asset Management");

            if (m_ShowAssetTools)
            {
                GUILayout.Label("Asset Pack");
                assetPackManifest = EditorGUILayout.ObjectField(assetPackManifest, typeof(AssetPackManifest), true) as AssetPackManifest;

                if (assetPackManifest)
                {
                    GUILayout.Label("Root Folder Path: " + AssetPackManager.GetPackRootFolder(assetPackManifest));
                    var updateButton = GUILayout.Button(new GUIContent("Update AssetPackManifest from Root Folder",
                        "Overwrites all tags in the root folder with metadata derived from folder tags and taginfo files, then rewrites the AssetPackManifest."));
                    if (updateButton)
                    {
                        AssetPackManager.RefreshPackFromRootFolder(assetPackManifest);
                    }
                }

                EditorGUILayout.Separator();

                GUILayout.Label("Asset Pool");
                targetAssetPool = EditorGUILayout.ObjectField(targetAssetPool, typeof(SyntheticHumanAssetPool), true) as SyntheticHumanAssetPool;

                if (GUILayout.Button(new GUIContent("Update Target Asset Pool", "Populates asset pool based on its settings. Only for testing.")))
                {
                    targetAssetPool.RefreshAssets();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for Batch Human Generation tools
            m_ShowGenerationTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowGenerationTools, "Human Generation");

            if (m_ShowGenerationTools)
            {
                GUILayout.Label("Generation Config");
                generationConfig = EditorGUILayout.ObjectField(generationConfig, typeof(HumanGenerationConfig), false) as HumanGenerationConfig;

                humanCount = EditorGUILayout.IntField("Human Count: ", humanCount);
                humansPerRow = EditorGUILayout.IntField("Humans per Row: ", humansPerRow);

                spacing = EditorGUILayout.Vector3Field("Spacing", spacing);
                GUILayout.Label("Debug Animation");
                m_HumanDebugAnimation = EditorGUILayout.ObjectField(m_HumanDebugAnimation, typeof(AnimationClip), false) as AnimationClip;

                GUILayout.Label("Delete Humans if Error Occurs");
                m_deleteIncompleteHumans = EditorGUILayout.Toggle(m_deleteIncompleteHumans);

                if (GUILayout.Button("Generate Humans") && generationConfig != null)
                {
                    GenerateHumans();
                }

                if (GUILayout.Button("Clear Generated Humans"))
                {
                    foreach (var human in m_GeneratedHumans.Where(human => human != null))
                    {
                        DestroyImmediate(human);
                    }

                    m_GeneratedHumans.Clear();
                    m_GeneratedAnimators.Clear();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for Debug tools
            m_ShowDebugTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowDebugTools, "Debug Tools");

            if (m_ShowDebugTools)
            {
                // Field for target GO
                GUILayout.Label("Target Object");
                testObject = EditorGUILayout.ObjectField(testObject, typeof(GameObject), true) as GameObject;

                if (GUILayout.Button("Dump fbx bone weights to json"))
                {
                    var weights = SkinWeights.LoadFromSkinnedMeshRenderer(testObject.GetComponent<SkinnedMeshRenderer>());
                    var json = JsonConvert.SerializeObject(weights);
                    var path = EditorUtility.SaveFilePanel("Skin Weights", "", "weights.json", "json");

                    File.WriteAllText(path, json);
                }

                if (GUILayout.Button("Verify vert orders"))
                {
                    //HumanGenerator.compareVertArrays(testObject.GetComponent<SkinnedMeshRenderer>().sharedMesh, sourceMesh);
                }

                if (GUILayout.Button("Print bind info to console"))
                {
                    if (testObject != null)
                    {
                        var myBones = testObject.GetComponent<Animator>().avatar.humanDescription.human;
                        for (var i = 0; i < myBones.Length; i++)
                        {
                            Debug.Log(myBones[i].boneName);
                            Debug.Log(myBones[i].limit.center);
                        }
                    }
                }

                if (GUILayout.Button("Save Human Mesh to File"))
                {
                    var objPath = EditorUtility.SaveFilePanel("Save Mesh to Obj File", "Assets", "human_mesh.obj", "obj");
                    var meshExporter = new MeshExporter(testObject.GetComponent<SkinnedMeshRenderer>(), exportTriangles: true);
                    var task = Task.Run(() => meshExporter.Export(objPath));
                    task.Wait();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for Debug tools
            m_ShowTaginfoTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTaginfoTools, "Taginfo Tools");

            if (m_ShowTaginfoTools)
            {
                GUILayout.Label("Key");
                m_TaginfoEditKey = EditorGUILayout.TextField(m_TaginfoEditKey);
                GUILayout.Label("New Key");
                m_TaginfoEditNewKey = EditorGUILayout.TextField(m_TaginfoEditNewKey);
                GUILayout.Label("Value (or blank to keep value)");
                m_TaginfoEditValue = EditorGUILayout.TextField(m_TaginfoEditValue);
                GUILayout.Label("Convert to int");
                m_TaginfoEditAsInt = EditorGUILayout.Toggle(m_TaginfoEditAsInt);

                if (GUILayout.Button("Bulk update selected taginfos"))
                {
                    BulkUpdateSelectedTaginfos();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for Animation Debug tools
            m_ShowAnimationTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowAnimationTools, "Animation Tools");
            if (m_ShowAnimationTools)
            {
                testObject = EditorGUILayout.ObjectField(new GUIContent("Target Object", "The human object to animate"),
                    testObject, typeof(GameObject), true) as GameObject;
                m_AnimationTag = EditorGUILayout.ObjectField(
                    new GUIContent("Animation Tag", "The animation tag asset whose linked animation clip will be used to animate the human object"),
                    m_AnimationTag, typeof(AnimationTag), true) as AnimationTag;
                if (GUILayout.Button("Animate Human"))
                {
                    var animator = testObject.GetComponent<Animator>();
                    var runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationRandomizerController");
                    var overrider = new AnimatorOverrideController(runtimeAnimatorController);
                    overrider["PlayerIdle"] = (AnimationClip)m_AnimationTag.linkedAsset;
                    animator.runtimeAnimatorController = overrider;
                    animator.Play("Base Layer.RandomState", 0, 0);
                    animator.Update(0.001f);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for Placement Debug tools
            m_ShowPlacementTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowPlacementTools, "Placement Tools");
            if (m_ShowPlacementTools)
            {
                testObject = EditorGUILayout.ObjectField(
                    new GUIContent("Target Object", "The human object that needs to be placed"),
                    testObject, typeof(GameObject), true) as GameObject;
                m_NavMeshSurface = EditorGUILayout.ObjectField(
                    new GUIContent("NavMesh Surface", "The navmesh surface object that the placer will place human on"),
                    m_NavMeshSurface, typeof(NavMeshSurface), true) as NavMeshSurface;
                m_PlacementCamera = EditorGUILayout.ObjectField(
                    new GUIContent("Camera", "The camera object and the placer will put the human object in the view of the camera"),
                    m_PlacementCamera, typeof(Camera), true) as Camera;
                m_AnimationPlacementGroup = EditorGUILayout.ObjectField(
                    new GUIContent("Animation Placement Group", "The AnimationPlacementGroup asset and the placer of this asset is going to be used to place the human object"),
                    m_AnimationPlacementGroup, typeof(AnimationPlacementGroup), false) as AnimationPlacementGroup;

                if (GUILayout.Button("Place Human"))
                {
                    m_NavMeshSurface.UpdateNavMesh();
                    var placer = m_AnimationPlacementGroup.placer;
                    var success = false;
                    placer.placementRandomizer = new NavMeshPlacementRandomizer()
                    {
                        cameras = new List<Camera>() { m_PlacementCamera }
                    };
                    success = placer.Place(testObject);
                    if (!success)
                    {
                        EditorUtility.DisplayDialog("Placement Result", "Failed to Place the Human", "OK");
                    }
                }

                if (GUILayout.Button("Print Active NavMeshSurfaces"))
                {
                    foreach (var surface in FindObjectsOfType<NavMeshSurface>())
                    {
                        Debug.Log($"Active NavMeshSurface: {surface.name}", surface);
                    }
                }

                if (GUILayout.Button("Enable Read/Write Property of FBX Files (Recursive)"))
                {
                    var folder = EditorUtility.OpenFolderPanel("Choose the root folder of FBX files", "Assets", "");
                    var files = Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories).ToList();
                    files.AddRange(Directory.GetFiles(folder, "*.FBX", SearchOption.AllDirectories));
                    for (var i = 0; i < files.Count; i++)
                    {
                        var file = files[i].Substring(Application.dataPath.Length - "Assets".Length);
                        EditorUtility.DisplayCancelableProgressBar("Enable read/write of FBX files", file, i / (float)files.Count);
                        var modelImporter = AssetImporter.GetAtPath(file) as ModelImporter;
                        if (!modelImporter.isReadable)
                        {
                            modelImporter.isReadable = true;
                            modelImporter.SaveAndReimport();
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Group for collision Debug tools
            m_ShowCollisionTools = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowCollisionTools, "Collision Tools");
            if (m_ShowCollisionTools)
            {
                testObject = EditorGUILayout.ObjectField(new GUIContent("Target Object", "The human object to detect collisions"),
                    testObject, typeof(GameObject), true) as GameObject;

                if (GUILayout.Button("Check Collisions"))
                {
                    var colliderManager = testObject.GetComponent<HumanColliderManager>();
                    var collisions = HumanColliderManager.GetCollisions(testObject);
                    foreach (var c in collisions)
                    {
                        Debug.Log($"Collision Detected: {c.HumanCollider.name} and {c.OtherCollider.name}; Penetration: {c.PenetrationDistance} meter");
                    }
                    EditorUtility.DisplayDialog("Collision Detection Result", $"Detected {collisions.Count} collisions", "OK");
                }

                m_CollisionObject = EditorGUILayout.ObjectField(
                    new GUIContent("Object To Add Colliders",
                        "Any object in the scene that has the MeshFilter component. If this object needs a non-convex mesh collider built from its mesh filter component, " +
                        "the following button will add the mesh collider to this object"),
                    m_CollisionObject, typeof(GameObject), true) as GameObject;

                if (GUILayout.Button("Add Non-Convex Mesh Colliders to GameObject (Recursive)"))
                {
                    var meshFilters = m_CollisionObject.GetComponentsInChildren<MeshFilter>();
                    foreach (var meshFilter in meshFilters)
                    {
                        var collider = meshFilter.GetComponent<Collider>();
                        if (collider == null)
                        {
                            var c = meshFilter.gameObject.AddComponent<MeshCollider>();
                            c.convex = false;
                            c.sharedMesh = meshFilter.sharedMesh;
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }

                if (GUILayout.Button("Add Non-Convex Mesh Colliders to Prefabs from folder (Recursive)"))
                {
                    var folder = EditorUtility.OpenFolderPanel("Choose the root folder of prefab files", "Assets", "");
                    var files = Directory.GetFiles(folder, "*.prefab", SearchOption.AllDirectories);
                    for (var i = 0; i < files.Length; i++)
                    {
                        var file = files[i].Substring(Application.dataPath.Length - "Assets".Length);
                        EditorUtility.DisplayCancelableProgressBar("Enable read/write of FBX files", file, i / (float)files.Length);
                        var prefab = PrefabUtility.LoadPrefabContents(file);
                        var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
                        foreach (var meshFilter in meshFilters)
                        {
                            var collider = meshFilter.GetComponent<Collider>();
                            if (collider == null)
                            {
                                var meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                                meshCollider.convex = false;
                                meshCollider.sharedMesh = meshFilter.sharedMesh;
                            }
                        }
                        PrefabUtility.SaveAsPrefabAsset(prefab, file);
                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.FlexibleSpace();

            GUILayout.EndScrollView();
        }
    }
}
