using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace Unity.CV.SyntheticHumans
{
    public class SyntheticHumanMaterialParameterModifier
    {
        delegate void ShaderRandomizer(Material mat, MaterialPropertyBlock propBlock);
        static readonly Dictionary<string, ShaderRandomizer> k_RandomizerFunctions = new Dictionary<string, ShaderRandomizer>()
        {
            {"HDRP/Lit", RandomizeHdrpLit},
            {"Shader Graphs/ClothColorize", RandomizeClothColorize},
            {"Shader Graphs/ClothingPatternRandomize", RandomizeClothPattern},
            {"Shader Graphs/Shoe.0000", RandomizeShoe},
            {"Shader Graphs/HairCVPR", RandomizeHair},
        };

        Material m_Mat;
        MaterialPropertyBlock m_PropBlock;
        static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int k_Hue = Shader.PropertyToID("Hue");
        static readonly int k_Saturation = Shader.PropertyToID("Saturation");
        static readonly int k_ColorOverlay = Shader.PropertyToID("Color_Overlay");
        static readonly int k_UVRotation = Shader.PropertyToID("UV_Rotation");
        static readonly int k_ShoeColor1 = Shader.PropertyToID("ShoeColor1");
        static readonly int k_ShoeColor2 = Shader.PropertyToID("ShoeColor2");
        static readonly int k_SoleColor = Shader.PropertyToID("SoleColor");
        static readonly int k_HairColor = Shader.PropertyToID("haircolor");
        static readonly int k_FabricColorA = Shader.PropertyToID("_FabricColorA");
        static readonly int k_FabricColorB = Shader.PropertyToID("_FabricColorB");
        static readonly int k_FabricColorC = Shader.PropertyToID("_FabricColorC");
        static readonly int k_FabricColorD = Shader.PropertyToID("_FabricColorD");
        static readonly int k_PatternSize = Shader.PropertyToID("_PatternSize");
        static readonly int k_RotatePattern = Shader.PropertyToID("_RotatePattern");

        public SyntheticHumanMaterialParameterModifier(Material mat, MaterialPropertyBlock propBlock)
        {
            m_Mat = mat;
            m_PropBlock = propBlock;
        }

        public void Randomize()
        {
            if (!k_RandomizerFunctions.ContainsKey(m_Mat.shader.name))
            {
                Debug.LogError(
                    $"Unsupported material shader {m_Mat.shader.name}. Skipping material parameter randomization.");
                return;
            }

            k_RandomizerFunctions[m_Mat.shader.name].Invoke(m_Mat, m_PropBlock);
        }

        static void RandomizeHdrpLit(Material mat, MaterialPropertyBlock propBlock)
        {
            // HDRP Lit shader only supports base color randomization
            float h, s, v;
            var floatParameter = new FloatParameter();

            var baseColor = mat.GetColor(k_BaseColor);
            Color.RGBToHSV(baseColor, out h, out s, out v);
            h = floatParameter.Sample();
            s = floatParameter.Sample();
            propBlock.SetColor(k_BaseColor, Color.HSVToRGB(h, s, v));
        }

        static void RandomizeClothColorize(Material mat, MaterialPropertyBlock propBlock)
        {
            var floatParameter = new FloatParameter();

            propBlock.SetFloat(k_Hue, floatParameter.Sample() * 360);
            propBlock.SetFloat(k_Saturation, floatParameter.Sample());
            propBlock.SetFloat(k_ColorOverlay, floatParameter.Sample());
            propBlock.SetFloat(k_UVRotation, floatParameter.Sample() * 360);
        }

        static void RandomizeClothPattern(Material mat, MaterialPropertyBlock propBlock)
        {
            var floatParameter = new FloatParameter();
            var colorParameter = new ColorHsvaParameter();
            var boolParameter = new BooleanParameter();

            if (boolParameter.Sample())
            {
                propBlock.SetInt(k_RotatePattern, 1);
            }
            else
            {
                propBlock.SetInt(k_RotatePattern, 0);
            }
            propBlock.SetColor(k_FabricColorA, colorParameter.Sample());
            propBlock.SetColor(k_FabricColorB, colorParameter.Sample());
            propBlock.SetColor(k_FabricColorC, colorParameter.Sample());
            propBlock.SetColor(k_FabricColorD, colorParameter.Sample());
            propBlock.SetFloat(k_PatternSize, floatParameter.Sample());
        }

        static void RandomizeShoe(Material mat, MaterialPropertyBlock propBlock)
        {
            var floatParameter = new FloatParameter();

            propBlock.SetFloat(k_ShoeColor1, floatParameter.Sample());
            propBlock.SetFloat(k_ShoeColor2, floatParameter.Sample());
            propBlock.SetFloat(k_SoleColor, floatParameter.Sample());
        }

        static void RandomizeHair(Material mat, MaterialPropertyBlock propBlock)
        {
            var floatParameter = new FloatParameter();

            propBlock.SetFloat(k_HairColor, floatParameter.Sample() * 10.0f);
        }
    }
}
