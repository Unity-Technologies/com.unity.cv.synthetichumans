using Unity.CV.SyntheticHumans;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HumanColliderManager))]
class HumanColliderManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var colliderManager = (HumanColliderManager) target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Colliders"))
        {
            if (colliderManager.SkeletonOrderedBones == null)
            {
                Debug.LogError("Failed to add colliders because the skeleton information is missing");
                return;
            }
            colliderManager.Initialize();
        }

        if (GUILayout.Button("Remove Colliders"))
        {
            colliderManager.RemoveColliders();
        }

        if (GUILayout.Button("Check Collision"))
        {
            var collisions = HumanColliderManager.GetCollisions(colliderManager.gameObject);
            var message = $"{collisions.Count} collisions are detected";
            Debug.Log(message);
            foreach (var collision in collisions)
            {
                Debug.Log($"Collision Pair: {collision.HumanCollider.name} and {collision.OtherCollider.name}");
            }
            EditorUtility.DisplayDialog("Collisions", message, "OK");
        }
        EditorGUILayout.EndHorizontal();
    }
}
