using Unity.CV.SyntheticHumans.Placement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Editor
{
    [CustomEditor(typeof(SittingPlacerTag))]
    class SittingPlacerTagEditor : UnityEditor.Editor
    {
        BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        bool m_ShowDirectionRangeGizmos = true;

        public override void OnInspectorGUI()
        {
            var tag = (SittingPlacerTag) target;

            EditMode.DoEditModeInspectorModeButton(EditMode.SceneViewEditMode.Collider, "Edit Volume",
                EditorGUIUtility.IconContent("EditCollider"), tag.volume, this);

            tag.volume = EditorGUILayout.BoundsField(new GUIContent("Effective Volume", "Local Position and Size of the Volume"), tag.volume);
            EditorGUILayout.LabelField(new GUIContent("Enabled Edges in Polar Coordinate", "Use the polar coordinate to define the part of the edges that a sitting human could be placed on"));
            EditorGUI.indentLevel++;
            tag.minimumDirectionAngle = EditorGUILayout.FloatField(new GUIContent("Min", "Minimum angle in degree"), tag.minimumDirectionAngle);
            tag.maximumDirectionAngle = EditorGUILayout.FloatField(new GUIContent("Max", "Maximum angle in degree"), tag.maximumDirectionAngle);
            while (tag.maximumDirectionAngle < tag.minimumDirectionAngle)
                tag.maximumDirectionAngle += 360;
            m_ShowDirectionRangeGizmos = EditorGUILayout.Toggle("Show Gizmos", m_ShowDirectionRangeGizmos);
            EditorGUI.indentLevel--;

            tag.area = (NavMeshPlacerTag.AreaType) EditorGUILayout.EnumPopup("NavMesh Area", tag.area);
            EditorUtility.SetDirty(target);
        }

        void OnSceneGUI()
        {
            var tag = (SittingPlacerTag) target;

            // Draw direction range of the tag
            if (m_ShowDirectionRangeGizmos)
            {
                var arcColor = Color.green;
                Handles.color = new Color(arcColor.r * 0.75f, arcColor.g * 0.75f, arcColor.b * 0.75f, arcColor.a * 0.15f);
                var center = tag.transform.TransformPoint(tag.volume.center);
                var direction = Vector3.ProjectOnPlane(tag.transform.forward, Vector3.up);
                direction = Quaternion.AngleAxis(tag.minimumDirectionAngle, Vector3.up) * direction;
                var angle = tag.maximumDirectionAngle - tag.minimumDirectionAngle;
                Handles.DrawSolidArc(center, Vector3.up, direction, angle, tag.GetRadius());
                EditorUtility.SetDirty(target);
            }

            // Draw effective volume of the tag
            if (EditMode.editMode != EditMode.SceneViewEditMode.Collider || !EditMode.IsOwner(this))
                return;

            var boxColor = Color.cyan;
            using (new Handles.DrawingScope(boxColor, tag.transform.localToWorldMatrix))
            {
                m_BoundsHandle.center = tag.volume.center;
                m_BoundsHandle.size = tag.volume.size;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tag, "Modified Sitting Placer Tag");
                    tag.volume.center = m_BoundsHandle.center;
                    tag.volume.size = m_BoundsHandle.size;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private void OnEnable()
        {
            var tag = (SittingPlacerTag) target;
            tag.UpdateVolumeByRenderingMesh();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void RenderBoxGizmo(SittingPlacerTag tag, GizmoType gizmoType)
        {
            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            if (tag.enabled)
            {
                Gizmos.matrix = tag.transform.localToWorldMatrix;
                var color = Color.cyan;
                var colorTrans = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a * 0.15f);

                Gizmos.color = colorTrans;
                Gizmos.DrawCube(tag.volume.center, tag.volume.size);

                Gizmos.color = color;
                Gizmos.DrawWireCube(tag.volume.center, tag.volume.size);

                Gizmos.matrix = oldMatrix;
                Gizmos.color = oldColor;
            }
        }
    }
}
