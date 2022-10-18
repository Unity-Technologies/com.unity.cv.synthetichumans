using System.Collections.Generic;
using System;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    /// <summary>
    /// Holds information required for the <see cref="Unity.CV.SyntheticHumans.Generators.HumanGenerator"/> to populate a human with specific game object clones
    /// It primarily accomplishes this through variables that inherit from
    /// A companion <see cref="SingleHumanSpecification"/>  contains high level filters that will determine what assets are populated during the HumanGenerator CreateHuman function
    /// </summary>
    public class SingleHumanGenerationAssetRefs : MonoBehaviour
    {
        /// <summary>
        /// Copy attributes that are important for generating a human from a source component to this component. This function can be used when we need to debug specific assets. The values specified in the editor are copied to runtime objects.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public void CopyGenerationAttributesFrom(SingleHumanGenerationAssetRefs source)
        {
            randomizationSeed = source.randomizationSeed;
            bodyMeshTag = source.bodyMeshTag;
            hairMeshTag = source.hairMeshTag;
            faceVATTag = source.faceVATTag;
            primaryBlendVATTag = source.primaryBlendVATTag;
            bodyMatTag = source.bodyMatTag;
            faceMatTag = source.faceMatTag;
            eyeMatTag = source.eyeMatTag;
            hairMatTag = source.hairMatTag;
            clothingTags = new List<ClothingTag>(source.clothingTags);
            clothingPrimaryBlendVAT = new List<Texture2D>(source.clothingPrimaryBlendVAT);
            clothingPrimaryBlendVATTags = new List<VATTag>(source.clothingPrimaryBlendVATTags);
            clothingSecondaryBlendVAT = new List<Texture2D>(source.clothingSecondaryBlendVAT);
            clothingSecondaryBlendVATTags = new List<VATTag>(source.clothingSecondaryBlendVATTags);

            clothingMatTags = new List<MaterialTag>(source.clothingMatTags);
        }

        [Tooltip("Random seed used to generate this human. If 0, will store current seed from the scenario at time of generation")]
        public uint randomizationSeed;

        [HideInInspector]
        public Mesh bodyMesh;
        [Tooltip("Tag that determines which base mesh will be selected for the body")]
        public BodyTag bodyMeshTag;

        [HideInInspector]
        public GameObject hairMesh;
        [Tooltip("Tag that determines which base mesh will be selected for the hair")]
        public HairTag hairMeshTag;

        [HideInInspector]
        public Texture2D faceVAT;
        [Tooltip("Tag that determines VAT will determine facial shape blends")]
        public VATTag faceVATTag;

        [HideInInspector]
        public Texture2D primaryBlendVAT;
        [Tooltip("Tag that determines VAT will be used for the height and weight in discrete + target blend solvers. Height only in additive solvers")]
        public VATTag primaryBlendVATTag;

        [HideInInspector]
        public Texture2D secondaryBlendVAT;
        [Tooltip("Tag that determines VAT will be used for the weight blend on additive solvers")]
        public VATTag secondaryBlendVATTag;

        [HideInInspector]
        public Material bodyMaterial;
        [Tooltip("Tag that determines material is applied to the body")]
        public MaterialTag bodyMatTag;

        [HideInInspector]
        public Material faceMaterial;
        [Tooltip("Tag that determines material is applied to the face")]
        public MaterialTag faceMatTag;

        [HideInInspector]
        public Material eyeMaterial;
        [Tooltip("Tag that determines material is applied to the eyes")]
        public MaterialTag eyeMatTag;

        [HideInInspector]
        public Material hairMaterial;
        [Tooltip("Tag that determines material is applied to the hair")]
        public MaterialTag hairMatTag;

        [Tooltip("List of tags that determine what clothing models to layer on top of the body mesh")]
        public List<ClothingTag> clothingTags = new List<ClothingTag>();

        [Tooltip("List of tags that determine what materials are applied to clothing items in selectedClothing")]
        public List<MaterialTag> clothingMatTags = new List<MaterialTag>();

        [HideInInspector]
        public List<Texture2D> clothingPrimaryBlendVAT = new List<Texture2D>();
        [Tooltip("List of tags that determine what VATs are used for height blending on selectedClothing objects")]
        public List<VATTag> clothingPrimaryBlendVATTags = new List<VATTag>();

        [HideInInspector]
        public List<Texture2D> clothingSecondaryBlendVAT = new List<Texture2D>();
        [Tooltip("List of tags that determine what VATs are used for weight blending on selectedClothing objects")]
        public List<VATTag> clothingSecondaryBlendVATTags = new List<VATTag>();

        [HideInInspector]
        public string animControllerName = "AnimationRandomizerController";

        [HideInInspector]
        public List<Mesh> selectedClothing = new List<Mesh>();

        [HideInInspector]
        public List<GameObject> createdClothing = new List<GameObject>();

        [HideInInspector]
        public List<Material> clothingMats = new List<Material>();

    }
}
