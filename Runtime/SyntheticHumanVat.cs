using System;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    public class SyntheticHumanVat : MonoBehaviour
    {
        #region Inspector

        Mesh m_Mesh;
        Vector3[] m_Rest;
        [SerializeField]
        public VatData vatData;

        #endregion Inspector

        #region Structs

        [Serializable]
        public struct VatData
        {
            // The texture with the vertex differentials.
            public Texture2D vatTex;
            // The calculated fitted maximum and minimum range of the vat.
            public float vatMax;
            public float vatMin;
        }

        #endregion Structs

        #region Vat Utils

        /// <summary>
        /// Copies the rest geometry so that Delta data can be applied.
        /// Note the supplied GameObject has to be a VAT authored object at it's original rest position.
        /// </summary>
        static Vector3[] SyntheticHumanVatRest(Mesh vatMesh)
        {
            // Preserve the rest mesh on the first frame.
            var rest = vatMesh.vertices;
            return rest;
        }

        /// <summary>
        /// Calculates the index lookup from the VAT texture and applies to rest mesh.
        /// </summary>
        public static Mesh SyntheticHumanVatDelta(Mesh vatMesh, Vector3[] restVector, VATTag data, float deformationMultiplier = 1.0f, Texture2D vatTexture = null)
        {
            // Convert Tag data to variables.
            if (!vatTexture)
            {
                vatTexture = (Texture2D)data.linkedAsset;
            }

            var vatMax = data.vatmax;
            var vatMin = data.vatmin;

            // Calculate blend deformation of mesh.
            vatMesh.vertices = SyntheticHumanVatDecompress(vatMesh, restVector, vatTexture, vatMax, vatMin, deformationMultiplier);

            return vatMesh;
        }
        /// <summary>
        /// The Debug version is the same as the above for runtime testing with the Start() and Update() except data is passed from the struct instead of class to use in the inspector
        /// </summary>
        static Mesh SyntheticHumanVatDeltaDebug(Mesh vatMesh, Vector3[] restVector, VatData data)
        {
            // Convert Struct data to variables.
            var vatTexture = data.vatTex;
            var vatMax = data.vatMax;
            var vatMin = data.vatMin;

            // Calculate blend deformation of mesh.
            vatMesh.vertices = SyntheticHumanVatDecompress(vatMesh, restVector, vatTexture, vatMax, vatMin);

            return vatMesh;
        }

        /// <summary>
        /// The decompress inflates the encoded delta transform data stored in each texel to a vertex. This allows lossy high compression for blendshapes.
        /// </summary>
        static Vector3[] SyntheticHumanVatDecompress(Mesh vatMesh, Vector3[] restVector, Texture2D vatTexture, float vatMax, float vatMin, float deformationMultiplier = 1.0f)
        {

            // Get the mesh and uvs for this frame to update.
            var vertices = vatMesh.vertices;
            var uv2 = vatMesh.uv2;

            // Loop through each vertex
            for (var i = 0; i < vertices.Length; i++)
            {
                //Use the custom look up table, UV2, from the mesh on the U and V axis.
                var u = uv2[i].x;
                var v = uv2[i].y;
                // Texture size multiplier for Get Pixel.
                u *= vatTexture.width;
                v *= vatTexture.height;
                // Round down for the integer based GetPixel function.
                var uInt = (int)Mathf.Floor(u);
                var vInt = (int)Mathf.Floor(v);

                // Reads the pixel value corresponding to each vertex. Note: this is different than in shaders.
                Vector4 vat = vatTexture.GetPixel(uInt, vInt);
                // Convert from Vector4 to vector3 in C# from the Getpixel function.
                Vector3 posDelta = vat;

                //expand normalised position texture values to world space
                // Find the range for the Vat fit that was compressed to 0-1 to fit in texture space.
                var expandVat = vatMax - vatMin;
                // Expand the Vat texture by the fitted range
                posDelta *= expandVat;
                // Add the minimum to offset for the data i.e. 0-1 space.
                posDelta += new Vector3(vatMin, vatMin, vatMin);
                // Control the intensity of the deformation for artistic control.
                posDelta *= deformationMultiplier;
                // Apply the deformation to the rest frame. If you use the update geometry it expands perpetually.
                vertices[i] = restVector[i] + posDelta;

            }

            return vertices;
        }

        // TODO: implement this function to use a dictionary of VATs and blend percentages. This should keep us from generating extra meshes in memory
        // public static Mesh ApplyMultipleVats(Mesh startingMesh)
        // {
        //     return startingMesh;
        // }

        #endregion Vat Utils

        #region Example Behavior

        void Start()
        {
            // Creates the rest geometry
            // Get the rest mesh on the first frame.
            //m_Mesh = GetComponent<MeshFilter>().mesh;
            m_Mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;

            // Set initial variables on start, rest, mesh, normals, uv2
            m_Rest = SyntheticHumanVatRest(m_Mesh); // This is the unique static frame for this asset.
        }

        void Update()
        {
            // Get the mesh for this frame to update
            m_Mesh = GetComponent<MeshFilter>().mesh;

            // Calculate the Vat Delta position
            m_Mesh.vertices = SyntheticHumanVatDeltaDebug(m_Mesh, m_Rest, vatData).vertices;

            // Adjust bounds for perception package
            m_Mesh.RecalculateBounds();
            m_Mesh.RecalculateNormals();
        }

        #endregion Example Behavior
    }
}
