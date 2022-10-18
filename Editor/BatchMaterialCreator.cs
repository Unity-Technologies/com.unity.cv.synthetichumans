using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.CV.SyntheticHumans.Editor
{
    class BatchMaterialCreator : EditorWindow
    {

        public string testFolderPath = "INVALID PATH";
        public TextAsset sourceCsv;
        static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");

        // Name of the window and location
        [MenuItem("Window/Escher/Batch Material Creator")]
        public static void OpenMainWindow()
        {
            GetWindow<BatchMaterialCreator>("Batch Material Creator");
        }

        // Basically the update function
        void OnGUI()
        {

            GUILayout.Label("Output Folder Path:" );
            GUILayout.Label( testFolderPath );
            if (GUILayout.Button("Select Output Folder"))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("Folder to Process", "", "");

                if (selectedPath.Contains(Application.dataPath))
                {
                    testFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length, (selectedPath.Length - Application.dataPath.Length));
                }
                else
                {
                    testFolderPath = "INVALID PATH";
                }
            }

            GUILayout.Label("Source CSV");
            sourceCsv = EditorGUILayout.ObjectField(sourceCsv, typeof(TextAsset), true) as TextAsset;

            //if (GUILayout.Button("Make Material File"))
            //{
            //    CreateMaterial("mulberry_silk", "#94766c", "Assets/SyntheticHumans/Models/Bodies/Adult/Male");
            //}

            //if (GUILayout.Button("Test CSV value"))
            //{
            //    importCSV();
            //}

            if (GUILayout.Button("Create Batch Materials"))
            {
                BatchCreateMaterials();
            }
        }

        static void CreateMaterial(string matName, string hexValue, string relativeFolderPath)
        {
            if (ColorUtility.TryParseHtmlString(hexValue, out var newCol))
            {
                var newAssetPath = Path.Combine(relativeFolderPath, matName) + ".asset";
                var newMaterial = new Material(Shader.Find("HDRP/Lit"));
                newMaterial.SetColor(k_BaseColor, newCol);
                AssetDatabase.CreateAsset(newMaterial, newAssetPath);
            }
        }

        // Only works for specific CSVs containing material name in column B and hex in column C
        void BatchCreateMaterials()
        {
            var parsedCsv = sourceCsv.text;
            var lines = parsedCsv.Split("\n"[0]);

            foreach (var line in lines)
            {
                var lineData = line.Trim().Split(","[0]);
                CreateMaterial( lineData[1], lineData[2], testFolderPath);
            }
        }

        // Debugging CSV Value Read
        public  void ImportCsv ()
        {
            var parsedCsv =  sourceCsv.text;
            var lines = parsedCsv.Split("\n"[0]);

            foreach (var line in lines)
            {
                var lineData = line.Trim().Split(","[0] );
                Debug.Log(lineData[0]);
                Debug.Log(lineData[1]);
                Debug.Log(lineData[2]);
            }
        }
    }
}
