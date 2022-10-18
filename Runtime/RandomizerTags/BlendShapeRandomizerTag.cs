using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [AddComponentMenu("Perception/RandomizerTags/Blend Shape Randomizer Tag")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]

    public class BlendShapeRandomizerTag : RandomizerTag
    {
        [FormerlySerializedAs("CustomizedWeightRandomizationRange")]
        public bool customizedWeightRandomizationRange;
        public float minWeight;
        public float maxWeight = 100.0f;

        /// <summary>
        /// Adds a new blend shape m_newBlendShape to the tagged object if it doesn't already exist in its SkinnedMesh Renderer
        /// </summary>
        /// <param name="newBlendShape"></param>
        public void AddNewBlendShape(GameObject newBlendShape)
        {
            var skinnedMesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;

            //Doesn't add blend shape if a blend shape with same name already exists
            if (skinnedMesh.GetBlendShapeIndex(newBlendShape.name) == -1)
            {
                Vector3[] deltaVertices;

                //Object added already has a SkinnedMeshRenderer and so at least one blend shape: copy it over
                if (newBlendShape.GetComponentInChildren<SkinnedMeshRenderer>())
                {
                    var newBlendShapeMesh = newBlendShape.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

                    if (newBlendShapeMesh.vertexCount == skinnedMesh.vertexCount)
                    {
                        var sourceBlendShapeCount = newBlendShapeMesh.blendShapeCount;

                        deltaVertices = new Vector3[newBlendShapeMesh.vertexCount];

                        for (var i = 0; i < sourceBlendShapeCount; i++)
                        {
                            newBlendShapeMesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, null, null);
                            skinnedMesh.AddBlendShapeFrame(newBlendShape.name, 100, deltaVertices, null, null);
                            skinnedMesh.RecalculateNormals();
                            skinnedMesh.RecalculateTangents();
                        }
                    }
                    else
                    {
                        Debug.LogWarning(newBlendShape.name + " doesn't have the same number of vertices as " + skinnedMesh.name + ": cannot assign new Blend Shape");
                    }

                }
                //Object added doesn't have a SkinnedMeshRenderer but a MeshFilter, so no blend shape: take its mesh data to add it as blend shape
                else if (newBlendShape.GetComponentInChildren<MeshFilter>())
                {
                    var newBlendShapeMesh = newBlendShape.GetComponentInChildren<MeshFilter>().sharedMesh;

                    if (newBlendShapeMesh.vertexCount == skinnedMesh.vertexCount)
                    {
                        deltaVertices = new Vector3[newBlendShapeMesh.vertexCount];

                        var tagVertices = skinnedMesh.vertices;
                        var sourceVertices = newBlendShapeMesh.vertices;

                        for (var i = 0; i < newBlendShapeMesh.vertexCount; i++)
                        {
                            deltaVertices[i] = sourceVertices[i] - tagVertices[i];
                        }

                        skinnedMesh.AddBlendShapeFrame(newBlendShape.name, 100, deltaVertices, null, null);
                        skinnedMesh.RecalculateNormals();
                        skinnedMesh.RecalculateTangents();
                    }
                    else
                    {
                        Debug.LogWarning(newBlendShape.name + " doesn't have the same number of vertices as " + skinnedMesh.name + ": cannot assign new Blend Shape");
                    }
                }
                else
                {
                    Debug.LogWarning("Game Object " + newBlendShape.name + " doesn't have a MeshFilter or SkinnedMeshRenderer component: cannot assign new Blend Shape");
                }
            }
        }


        /// <summary>
        /// Sets randomized weight to tagged object's blendshape at index m_blendShapeIndex, from FloatParameter m_rawWeight set in randomizer
        /// Can choose to apply a customized range of randomization to tagged object's blendshapes or not
        /// </summary>
        /// <param name="blendShapeIndex"></param>
        /// <param name="rawWeight"></param>
        public void SetWeight(int blendShapeIndex, float rawWeight)
        {
            if (customizedWeightRandomizationRange)
            {
                //might need to add some kind of condition to apply this to only propotions blend shapes and not characteristical ones

                var skinnedMeshRend = GetComponent<SkinnedMeshRenderer>();
                var scaledWeight = rawWeight * 0.01f * (maxWeight - minWeight) + minWeight;
                skinnedMeshRend.SetBlendShapeWeight(blendShapeIndex, scaledWeight);
            }
            else
            {
                var skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, rawWeight);
            }
        }
    }
}
