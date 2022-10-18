using UnityEditor;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    class DefaultSyntheticHumanAssetProcessor : ISyntheticHumanAssetProcessor
    {
        public void OnPreprocessTexture(AssetImporter assetImporter)
        {
            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null)
                return;

            var name = textureImporter.assetPath.ToLower();
            if (!name.Contains("vat."))
                return;

            var extension = name.Substring(name.LastIndexOf(".")).ToLower();

            switch (extension)
            {
                case ".exr":
                    // note: Global settings for all the platforms
                    textureImporter.textureType = TextureImporterType.Default;
                    textureImporter.textureShape = TextureImporterShape.Texture2D;
                    textureImporter.sRGBTexture = false;
                    textureImporter.alphaSource = TextureImporterAlphaSource.None;
                    textureImporter.alphaIsTransparency = false;
                    textureImporter.npotScale = TextureImporterNPOTScale.None;
                    textureImporter.isReadable = true;
                    textureImporter.streamingMipmaps = false;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                    textureImporter.filterMode = FilterMode.Point;

                    // Standalone
                    // Construct the class that contains our importer settings, we'll re-use this class per platform by changing any fields we need changed
                    // Let's start with standalone build target, "name" field determines the target platform
                    var tips = new TextureImporterPlatformSettings()
                    {
                        allowsAlphaSplitting = false,
                        androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality16Bit,
                        compressionQuality = 100,
                        crunchedCompression = false,
                        format = textureImporter.DoesSourceTextureHaveAlpha() ? TextureImporterFormat.RGBAHalf : TextureImporterFormat.RGBA32,
                        maxTextureSize = 256,
                        name = "Standalone",
                        resizeAlgorithm = TextureResizeAlgorithm.Bilinear,
                        overridden = true,
                        textureCompression = TextureImporterCompression.Uncompressed,
                    };
                    textureImporter.SetPlatformTextureSettings(tips);

                    // At this point we don't need to declare and define a settings class, just change the fields we want changed and re-use it!
                    // iPhone
                    tips.name = "iPhone";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // Web
                    // tips.name           = "Web";
                    // importer.SetPlatformTextureSettings(tips);
                    // Android
                    tips.name = "Android";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // WebGL - Does not handle RGBAHalf
                    // tips.name           = "WebGL";
                    // importer.SetPlatformTextureSettings(tips);
                    // Windows Store Apps
                    tips.name = "Windows Store Apps";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // PS4
                    tips.name = "PS4";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // PSM
                    tips.name = "PSM";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // XboxOne
                    tips.name = "XboxOne";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // Nintendo 3DS
                    tips.name = "Nintendo 3DS";
                    textureImporter.SetPlatformTextureSettings(tips);

                    // tvOS
                    tips.name = "tvOS";
                    textureImporter.SetPlatformTextureSettings(tips);
                    break;
            }
        }

        public void OnPreprocessModel(AssetImporter assetImporter)
        {
            var modelImporter = assetImporter as ModelImporter;
            if (modelImporter == null)
                return;

            var name = modelImporter.assetPath.ToLower();

            if (name.Contains("mesh."))
            {
                var extension = name.Substring(name.LastIndexOf(".")).ToLower();
                switch (extension)
                {
                    case ".fbx":
                        // Model - Scene
                        modelImporter.globalScale = 1.0F;
                        modelImporter.useFileUnits = true;
                        modelImporter.importBlendShapes = false;
                        modelImporter.importVisibility = false;
                        modelImporter.importCameras = false;
                        modelImporter.importLights = false;
                        modelImporter.preserveHierarchy = false;

                        // Model - Meshes
                        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                        modelImporter.isReadable = true;

                        //importer.optimizeMesh       = false;
                        modelImporter.optimizeMeshPolygons = false;
                        modelImporter.optimizeMeshVertices = false;
                        modelImporter.addCollider = false;

                        // Model - Geometry
                        modelImporter.keepQuads = false;
                        modelImporter.weldVertices = false;
                        modelImporter.indexFormat = ModelImporterIndexFormat.Auto;
                        modelImporter.importNormals = ModelImporterNormals.Import;
                        modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                        modelImporter.swapUVChannels = false;
                        modelImporter.generateSecondaryUV = false;

                        // Rig
                        modelImporter.animationType = ModelImporterAnimationType.Generic;

                        // Animation
                        modelImporter.importAnimation = false;
                        modelImporter.importConstraints = false;

                        // Materials
                        // importer.importMaterials    = true;
                        modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                        modelImporter.useSRGBMaterialColor = false;

                        // importer.materialLocation   = ModelImporterMaterialLocation.InPrefab;
                        // importer.materialName       = ModelImporterMaterialName.BasedOnMaterialName;
                        // importer.materialSearch     = ModelImporterMaterialSearch.Everywhere;
                        // importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                        break;
                }
            }
        }

        public void OnPostprocessMaterial(Material material)
        {
        }
    }
}
