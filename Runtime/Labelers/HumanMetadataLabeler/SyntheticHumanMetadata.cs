using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Labelers
{
    [Serializable]
    struct SyntheticHumanMetadata : IMessageProducer
    {
        public uint instanceId;

        public string age;
        public string height;
        public string weight;
        [FormerlySerializedAs("Sex")]
        public string gender;
        public string ethnicity;

        public string bodyMeshTag;
        public string hairMeshTag;
        public string faceVatTag;
        public string primaryBlendVatTag;
        public string secondaryBlendVatTag;
        public string bodyMaterialTag;
        public string faceMaterialTag;
        public string eyeMaterialTag;
        public string hairMaterialTag;
        public string templateSkeleton;
        public string[] clothingTags;
        public string[] clothingMaterialTags;

        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddUInt("instanceId", instanceId);
            builder.AddString("age", age);
            builder.AddString("height", height);
            builder.AddString("weight", weight);
            builder.AddString("sex", gender);
            builder.AddString("ethnicity", ethnicity);
            builder.AddString("bodyMeshTag", bodyMeshTag);
            builder.AddString("hairMeshTag", hairMeshTag);
            builder.AddString("faceVatTag", faceVatTag);
            builder.AddString("primaryBlendVatTag", primaryBlendVatTag);
            builder.AddString("secondaryBlendVatTag", secondaryBlendVatTag);
            builder.AddString("bodyMaterialTag", bodyMaterialTag);
            builder.AddString("faceMaterialTag", faceMaterialTag);
            builder.AddString("eyeMaterialTag", eyeMaterialTag);
            builder.AddString("hairMaterialTag", hairMaterialTag);
            builder.AddString("templateSkeleton", templateSkeleton);
            builder.AddStringArray("clothingTags", clothingTags);
            builder.AddStringArray("clothingMaterialTags", clothingMaterialTags);
        }
    }
}
