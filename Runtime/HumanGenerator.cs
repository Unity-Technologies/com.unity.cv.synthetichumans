using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Generators
{
    public static class HumanGenerator
    {
        static Mathematics.Random s_RandomGenerator;
        static readonly int k_Rotation = Shader.PropertyToID("rotation");
        static readonly int k_Subtone = Shader.PropertyToID("subtone");
        static readonly int k_Tone = Shader.PropertyToID("tone");

        public static GameObject GenerateHuman(HumanGenerationConfig generationConfig, bool discardPartialGenerations = true)
        {
            s_RandomGenerator.state = SamplerState.NextRandomState();

            var human = generationConfig.basePrefab ? Object.Instantiate(generationConfig.basePrefab) : new GameObject();

            var assetRefsForGeneration = human.AddComponent<SingleHumanGenerationAssetRefs>();

            if (generationConfig.preselectedGenerationAssetRefs)
            {
                //A non-null preselectedGenerationAssetRefs from the config should be strictly for debugging a specific list of one or more assets. Not expected in normal operation.
                assetRefsForGeneration.CopyGenerationAttributesFrom(generationConfig.preselectedGenerationAssetRefs);
            }

            var humanSpecs = human.AddComponent<SingleHumanSpecification>();

            AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
            //if a SingleHumanGenerationAssetRefs was already attached for debugging purposes, humanSpecs will only affect the null SyntheticHuman tags from the ref list

            var assetTagPool = generationConfig.assetTagPool;
            if (!assetTagPool)
            {
                Debug.LogError($"The provided {nameof(HumanGenerationConfig)} asset named {generationConfig.name} does not have a {nameof(SyntheticHumanAssetPool)} assigned. Will not generate a human with this config");
                if (discardPartialGenerations)
                {
                    Object.DestroyImmediate(human);
                    return null;
                }
                else
                    return human;
            }

            PopulateGenerationAssetRefListBasedOnSpecs(assetTagPool, assetRefsForGeneration, humanSpecs, generationConfig);

            if (!LoadAndAssignFinalAssets(assetRefsForGeneration))
            {
                Debug.LogError($"Unable to load and assign final assets from selected tags");
                if (discardPartialGenerations)
                {
                    Object.DestroyImmediate(human);
                    return null;
                }
                else
                    return human;

            }

            ApplyMeshBase(assetRefsForGeneration.bodyMesh, human);

            ApplyBodyVats(assetRefsForGeneration, humanSpecs);

            //Rig application
            var newHumanRenderer = human.GetComponent<SkinnedMeshRenderer>();
            var skeletonInfo = generationConfig.CreateSkeleton(newHumanRenderer);

            // TODO (CS): we want to choose the weight file / blend it based on the blending mode.
            BindSkinnedRendererToSkeleton(newHumanRenderer, skeletonInfo, assetRefsForGeneration.bodyMeshTag, assetRefsForGeneration.primaryBlendVATTag);

            //var customHumanDataLabeler = human.AddComponent<CustomHumanMetadataAnnotation>();

            GenerateClothes(assetRefsForGeneration, skeletonInfo);
            ApplyClothingVats(assetRefsForGeneration, humanSpecs);
            BindClothesToSkeleton(assetRefsForGeneration, skeletonInfo);

            AssignFaceBodyEyeMaterials(newHumanRenderer, assetRefsForGeneration, humanSpecs);

            GenerateHair(assetRefsForGeneration, skeletonInfo, newHumanRenderer);

            // Add Mesh Collider to the body
            var colliderManager = human.AddComponent<HumanColliderManager>();
            colliderManager.SkeletonOrderedBones = skeletonInfo.OrderedBones;
            if (generationConfig.enableColliderGeneration)
            {
                colliderManager.Initialize();
            }

            // Let any user-added Lifecycle Subscribers know that human generation is complete so that they can run custom code.
            var lifecycleSubscribers = human.GetComponents<HumanGenerationLifecycleSubscriber>();
            foreach (var subscriber in lifecycleSubscribers)
            {
                subscriber.OnGenerationComplete();
            }

            return human;
        }



        /// <summary>
        /// Randomly generates a single set of final properties for a single human to be generated based on the provided <see cref="generationConfig"/>, and stores them in <see cref="humanSpecs"/>
        /// </summary>
        /// <param name="humanSpecs"></param>
        /// <param name="generationConfig"></param>
        static void AssignRandomHumanSpecsBasedOnConfig(SingleHumanSpecification humanSpecs, HumanGenerationConfig generationConfig)
        {
            var success = false;
            while (!success)
            {
                //TODO (MK): We should probably make sure all of the specs that we choose here have at least one matching asset of each kind that matters. E.g. if age is chosen to be adult, make sure
                //there are adult body tags, body vats, etc. Note that this does not ensure that the combination of the specs we choose will also have matching assets, but it
                //is a first filtering step that weeds out many problematic spec combinations.

                //Get random values based on generationConfig
                humanSpecs.age = SyntheticHumanAgeRangeExtension.GetSyntheticHumanAgeRange(generationConfig.ageRange.Sample());
                //Debug.Log($"Selected age range: {humanSpecs.age}");
                humanSpecs.gender = generationConfig.genders[s_RandomGenerator.NextInt(generationConfig.genders.Count)];
                humanSpecs.ethnicity = generationConfig.ethnicities[s_RandomGenerator.NextInt(generationConfig.ethnicities.Count)];
                humanSpecs.heightWeightSolver = generationConfig.heightWeightSolver;
                humanSpecs.normalizedHeight = generationConfig.heightRange.Sample();
                humanSpecs.heightRange = humanSpecs.normalizedHeight > 0.5f ? SyntheticHumanHeightRange.Tall : SyntheticHumanHeightRange.Short;
                humanSpecs.normalizedWeight = generationConfig.weightRange.Sample();
                humanSpecs.weightRange = humanSpecs.normalizedWeight > 0.5f ? SyntheticHumanWeightRange.Large : SyntheticHumanWeightRange.Small;
                humanSpecs.requiredClothing = generationConfig.requiredClothingParameters;
                //------------------

                success = true;
            }
            //TODO: to be implemented
        }

        static void PopulateGenerationAssetRefListBasedOnSpecs(SyntheticHumanAssetPool assetTagPool, SingleHumanGenerationAssetRefs generationAssetRefs, SingleHumanSpecification humanSpecs, HumanGenerationConfig generationConfig)
        {
            if (generationAssetRefs.randomizationSeed != 0)
            {
                SamplerState.randomState = generationAssetRefs.randomizationSeed;
                Debug.LogWarning($"Found a non-zero randomization seed in the {nameof(SingleHumanGenerationAssetRefs)} attached to a provided {nameof(HumanGenerationConfig)}. This overrides the global seed in the simulation and should only be used for debugging.");

                //This should be strictly for debugging a specific SingleHumanGenerationAssetRefs with a specific randomization seed.
                //Should not be involved in normal operation
            }

            const int maxRetries = 100;
            var retryCount = 0;
            var allRequiredTagsFound = false;
            while (!allRequiredTagsFound && retryCount <= maxRetries)
            {
                //if (retryCount != 0)
                //    Debug.Log("Retrying compatible asset finding loop.");

                retryCount++;

                if (!generationAssetRefs.bodyMeshTag)
                {
                    generationAssetRefs.bodyMeshTag = FindCompatibleBodyMeshTag(assetTagPool, humanSpecs);
                    if (!generationAssetRefs.bodyMeshTag)
                    {
                        //Debug.LogError("Could not find a compatible body mesh tag in the provided asset tag pool.");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                FindAndAssignCompatibleHeightWeightVatTags(assetTagPool, generationAssetRefs, humanSpecs);
                //we don't check whether the primary or secondary body VATs are null because they are not critical

                if (!generationAssetRefs.faceVATTag)
                {
                    generationAssetRefs.faceVATTag = FindCompatibleFaceVatTag(assetTagPool, humanSpecs);
                    if (!generationAssetRefs.faceVATTag)
                    {
                        //Debug.LogError("Could not find a compatible face VAT tag in the provided asset tag pool.");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                if (!generationAssetRefs.faceMatTag)
                {
                    generationAssetRefs.faceMatTag = FindCompatibleFaceMaterialTag(assetTagPool, humanSpecs);
                    if (!generationAssetRefs.faceMatTag)
                    {
                        //Debug.LogError("Could not find a compatible face material tag in the provided asset tag pool.");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                if (!generationAssetRefs.bodyMatTag)
                {
                    generationAssetRefs.bodyMatTag = FindCompatibleBodyMaterialTag(assetTagPool, generationAssetRefs, humanSpecs);
                    if (!generationAssetRefs.bodyMatTag)
                    {
                        //Debug.LogError("Could not find a compatible body material tag in the provided asset tag pool. ");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                if (!generationAssetRefs.eyeMatTag)
                {
                    generationAssetRefs.eyeMatTag = FindCompatibleEyeMaterialTag(assetTagPool);
                    if (!generationAssetRefs.eyeMatTag)
                    {
                        //Debug.LogError("Could not find a compatible eye material tag in the provided asset tag pool. ");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                if (!generationAssetRefs.hairMeshTag)
                {
                    generationAssetRefs.hairMeshTag = FindCompatibleHairMeshTag(assetTagPool, humanSpecs);
                    if (!generationAssetRefs.hairMeshTag)
                    {
                        //Debug.LogError("Could not find a compatible hair mesh tag in the provided asset tag pool.");
                        AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                        continue;
                    }
                }

                if (generationAssetRefs.hairMeshTag)
                {
                    if (!generationAssetRefs.hairMatTag)
                    {
                        generationAssetRefs.hairMatTag = FindCompatibleHairMaterialTag(assetTagPool, generationAssetRefs.hairMeshTag.name);
                        if (!generationAssetRefs.hairMatTag)
                        {
                            //Debug.LogError("Could not find a compatible hair material tag in the provided asset tag pool. ");
                            AssignRandomHumanSpecsBasedOnConfig(humanSpecs, generationConfig);
                            continue;
                        }
                    }
                }

                ReviewAndAssignClothingTags(humanSpecs, generationAssetRefs, assetTagPool);

                FindAndAssignCompatibleClothingVatTags(assetTagPool, generationAssetRefs, humanSpecs);

                allRequiredTagsFound = generationAssetRefs.bodyMeshTag && generationAssetRefs.bodyMatTag
                    && generationAssetRefs.faceVATTag && generationAssetRefs.faceMatTag
                    && generationAssetRefs.eyeMatTag && generationAssetRefs.hairMeshTag && generationAssetRefs.hairMatTag;
            }
            //Debug.Log($"Retry count = {retryCount}");
        }


        /// <summary>
        /// Compare the given human specs and the candidate tag in regard with the provided list of human trait types (age, gender, etc.). If all requested traits
        /// match the candidate tag is acceptable.
        /// </summary>
        /// <param name="syntheticHumanPropertiesToMatch"></param>
        /// <param name="humanSpecs"></param>
        /// <param name="syntheticHumanTag"></param>
        /// <param name="ignoreNonExistentFieldsInTag"></param>
        /// <param name="ignoreNoneValueFieldsInSpec"></param>
        /// <returns></returns>
        static bool DoSpecsAndTagExactlyMatch(List<Type> syntheticHumanPropertiesToMatch, SingleHumanSpecification humanSpecs, SyntheticHumanTag syntheticHumanTag, bool ignoreNonExistentFieldsInTag = true, bool ignoreNoneValueFieldsInSpec = true)
        {
            var humanSpecsFields = humanSpecs.GetType().GetFields();
            var syntheticHumanTagFields = syntheticHumanTag.GetType().GetFields();

            var fieldTypesInTag = syntheticHumanTagFields.Select(info => info.FieldType);
            var notExistingFieldTypes = syntheticHumanPropertiesToMatch.Where(type => !fieldTypesInTag.Contains(type)).ToList();

            //if we have requested matching on fields that do not even exist in this tag and our matching policy disallows this, return false
            if (notExistingFieldTypes.Count > 0 && !ignoreNonExistentFieldsInTag)
                return false;

            foreach (var type in syntheticHumanPropertiesToMatch)
            {
                var referenceValue = humanSpecsFields.FirstOrDefault(fieldInfo => fieldInfo.FieldType == type)?.GetValue(humanSpecs);
                var checkedValue = syntheticHumanTagFields.FirstOrDefault(fieldInfo => fieldInfo.FieldType == type)?.GetValue(syntheticHumanTag);
                if (referenceValue != null && !referenceValue.Equals(checkedValue) &&
                    !(referenceValue.ToString() == "None" & ignoreNoneValueFieldsInSpec))
                    return false;
            }

            return true;
        }

        #region TagFinding

        static BodyTag FindCompatibleBodyMeshTag(SyntheticHumanAssetPool assetTagPool, SingleHumanSpecification humanSpecs)
        {
            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                typeof(SyntheticHumanGender)
            };
            var compatibleTags = assetTagPool.filteredBodyTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
            return compatibleTags.Count > 0 ? compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        static VATTag FindCompatibleFaceVatTag(SyntheticHumanAssetPool assetTagPool, SingleHumanSpecification humanSpecs)
        {
            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                //Males and Females share the same face VATs.  It is no longer necessary to filter by gender. (Aug 26, 2022)
            };

            var compatibleTags = assetTagPool.filteredFaceVatTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
            return compatibleTags.Count > 0 ?  compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        static MaterialTag FindCompatibleFaceMaterialTag(SyntheticHumanAssetPool assetTagPool, SingleHumanSpecification humanSpecs)
        {
            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                // Ethnicity is handled in material parameters for now (Jun 7, 2022)
            };

            var compatibleTags = assetTagPool.filteredFaceMatTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
            compatibleTags = compatibleTags.Where(tag => tag.humanElement == SyntheticHumanElement.Head && (tag.gender == humanSpecs.gender || tag.gender == SyntheticHumanGender.Neutral)).ToList();
            return compatibleTags.Count > 0 ? compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        static MaterialTag FindCompatibleBodyMaterialTag(SyntheticHumanAssetPool assetTagPool, SingleHumanGenerationAssetRefs assetRefs, SingleHumanSpecification humanSpecs)
        {
            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                // Ethnicity is handled in material parameters for now (Jun 7, 2022)
            };

            var compatibleTags = assetTagPool.filteredBodyMatTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
            compatibleTags = compatibleTags.Where(tag => tag.humanElement == SyntheticHumanElement.Body && (tag.gender == humanSpecs.gender || tag.gender == SyntheticHumanGender.Neutral)).ToList();
            return compatibleTags.Count > 0 ? compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        static MaterialTag FindCompatibleEyeMaterialTag(SyntheticHumanAssetPool assetTagPool)
        {
            return assetTagPool.filteredEyeMatTags[s_RandomGenerator.NextInt(0, assetTagPool.filteredEyeMatTags.Count)];
        }

        static HairTag FindCompatibleHairMeshTag(SyntheticHumanAssetPool assetTagPool, SingleHumanSpecification humanSpecs)
        {
            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
            };

            var compatibleTags = assetTagPool.filteredHairTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
            return compatibleTags.Count > 0 ? compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        static MaterialTag FindCompatibleHairMaterialTag(SyntheticHumanAssetPool assetTagPool, string hairMeshName)
        {
            return assetTagPool.filteredHairMatTags[s_RandomGenerator.NextInt(0, assetTagPool.filteredHairMatTags.Count)];

            //JW - Hair materials can be shared across all hair meshes and ages (Aug 26, 2022)
            //return assetTagPool.filteredHairMatTags.FirstOrDefault(tag => tag.name == hairMeshName);
        }

        static void FindAndAssignCompatibleHeightWeightVatTags(SyntheticHumanAssetPool assetTagPool, SingleHumanGenerationAssetRefs generationAssetRefs, SingleHumanSpecification humanSpecs)
        {
            if (generationAssetRefs.primaryBlendVATTag)
                return;

            if (humanSpecs.heightWeightSolver == SyntheticHumanHeightWeightSolver.None)
                return;

            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                typeof(SyntheticHumanGender)
            };

            var initialCompatibleTags = assetTagPool.filteredBodyVatTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();

            switch (humanSpecs.heightWeightSolver)
            {
                case SyntheticHumanHeightWeightSolver.Discrete:
                {
                    // Skip if it's already defined in the tag
                    FindDiscreteHeightWeightVats(humanSpecs, generationAssetRefs, initialCompatibleTags);
                    break;
                }

                case SyntheticHumanHeightWeightSolver.BlendTarget:
                {
                    FindBlendTargetHeightWeightVats(humanSpecs,generationAssetRefs, initialCompatibleTags);
                    break;
                }

                case SyntheticHumanHeightWeightSolver.Additive:
                {
                    if (generationAssetRefs.secondaryBlendVATTag)
                        return;

                    FindAdditiveHeightWeightVats(humanSpecs, generationAssetRefs, initialCompatibleTags);
                    break;
                }
            }
        }

        static void FindAndAssignCompatibleClothingVatTags(SyntheticHumanAssetPool assetTagPool, SingleHumanGenerationAssetRefs generationAssetRefs, SingleHumanSpecification humanSpecs)
        {
            //TODO (MK): Currently we do not support testing of specific clothing VATs through adding them to a preselected generationAssetRefs. Add functionality here to check generationAssetRefs for any already assigned clothing VATs and use them if they match the types of the previously selected clothing elements

            if (humanSpecs.heightWeightSolver == SyntheticHumanHeightWeightSolver.None)
                return;

            var propertiesToMatch = new List<Type>
            {
                typeof(SyntheticHumanAgeRange),
                typeof(SyntheticHumanGender)
            };

            var initialCompatibleTags = assetTagPool.filteredClothingVatTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();

            //generationAssetRefs.selectedClothingVATTags = new List<ClothingVATList>(new ClothingVATList[generationAssetRefs.clothingTags.Count]);

            generationAssetRefs.clothingPrimaryBlendVATTags = new List<VATTag>( new VATTag[generationAssetRefs.clothingTags.Count()] );
            generationAssetRefs.clothingSecondaryBlendVATTags = new List<VATTag>( new VATTag[generationAssetRefs.clothingTags.Count()]);

            switch (humanSpecs.heightWeightSolver)
            {
                case SyntheticHumanHeightWeightSolver.Discrete:
                {
                    // Skip if it's already defined in the tag
                    FindDiscreteHeightWeightClothingVats(humanSpecs, generationAssetRefs, initialCompatibleTags);
                    break;
                }

                case SyntheticHumanHeightWeightSolver.BlendTarget:
                {
                    FindBlendTargetHeightWeightClothingVats(humanSpecs,generationAssetRefs, initialCompatibleTags);
                    break;
                }

                case SyntheticHumanHeightWeightSolver.Additive:
                {
                    FindAdditiveHeightWeightClothingVats(humanSpecs, generationAssetRefs, assetTagPool.filteredClothingVatTags);
                    break;
                }
            }
        }

        /// <summary>
        /// Find a single body VAT in the primary blend slot of a <see cref="SingleHumanGenerationAssetRefs"/>.
        /// In this case, normalized height is used as a filter only, and normalized weight will be disregarded.
        /// This is intended to select a singular VAT shape that will be applied 100% to determine both height and weight
        /// </summary>
        static void FindDiscreteHeightWeightVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            var enumMatch = Mathf.CeilToInt(humanSpecs.normalizedHeight * (Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1));
            // Filter against the normalized height for the character
            var compatibleTags = assetTagList.Where(tag => (int) tag.height == enumMatch).ToList();
            if (compatibleTags.Count > 0) generationAssetRefs.primaryBlendVATTag = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)];
        }

        static void FindDiscreteHeightWeightClothingVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            var enumMatch = Mathf.CeilToInt(humanSpecs.normalizedHeight * (Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1));

            for (var i = 0; i < generationAssetRefs.clothingTags.Count(); i++)
            {
                // Filter against the normalized height for the character
                var compatibleTags = assetTagList.Where(tag => (int)tag.height == enumMatch && tag.parentMesh == generationAssetRefs.clothingTags[i].name).ToList();
                if (compatibleTags.Count > 0) generationAssetRefs.clothingPrimaryBlendVATTags[i] = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] ;
            }
        }

        /// <summary>
        /// Find a single body VAT in the primary blend slot of a <see cref="SingleHumanGenerationAssetRefs"/>.
        /// In this case, normalized height is used as a filter and a blend percentage to this one shape. Normalized weight will be disregarded.
        /// This is intended to select a singular VAT shape that will be applied to determine both height and weight at a percentage
        /// Since blending the average/average against the default mesh would result in no change, it is not a valid selection
        /// </summary>
        static void FindBlendTargetHeightWeightVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            var enumMatch = Mathf.CeilToInt(humanSpecs.normalizedHeight * (Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1));
            // Filter against the normalized height for the character and disregard average/average
            var compatibleTags = assetTagList.Where(tag => (int) tag.height == enumMatch && !(tag.weight == SyntheticHumanWeightRange.Average && tag.height == SyntheticHumanHeightRange.Average)).ToList();
            if (compatibleTags.Count > 0) generationAssetRefs.primaryBlendVATTag = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)];
        }

        static void FindBlendTargetHeightWeightClothingVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            var enumMatch = Mathf.CeilToInt(humanSpecs.normalizedHeight * (Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1));
            for (var i = 0; i < generationAssetRefs.clothingTags.Count(); i++)
            {
                // Filter against the normalized height for the character and disregard average/average
                var compatibleTags = assetTagList.Where(tag => (int)tag.height == enumMatch && !(tag.weight == SyntheticHumanWeightRange.Average && tag.height == SyntheticHumanHeightRange.Average && tag.parentMesh == generationAssetRefs.clothingTags[i].name)).ToList();
                if (compatibleTags.Count > 0) generationAssetRefs.clothingPrimaryBlendVATTags[i] = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] ;
            }
        }

        /// <summary>
        /// Find a body VAT in the primary and secondary blend slots of a <see cref="SingleHumanGenerationAssetRefs"/>.
        /// In this case, normalized height is used as a filter for the first shape. Normalized weight is used for the second.
        /// Normalized height and weight blend percentages will be applied independently
        /// Since blending the average/average against the default mesh would result in no change, it is not a valid selection
        /// </summary>
        static void FindAdditiveHeightWeightVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            //PRIMARY VAT BLEND

            var enumMatch = humanSpecs.normalizedHeight > 0.5f ? Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1 : 1;
            // Filter against normalized height, ensure the weight is average since this shape will be used for height only (no double weight applications)
            var compatibleTags = assetTagList.Where(tag => (int) tag.height == enumMatch && tag.weight == SyntheticHumanWeightRange.Average).ToList();
            if (compatibleTags.Count > 0) generationAssetRefs.primaryBlendVATTag = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)];

            //SECONDARY VAT BLEND
            enumMatch = humanSpecs.normalizedWeight > 0.5f ? Enum.GetValues(typeof(SyntheticHumanWeightRange)).Length - 1 : 1;
            // Filter against normalized height, ensure the weight is average since this shape will be used for height only (no double weight applications)
            compatibleTags = assetTagList.Where(tag => (int) tag.weight == enumMatch && tag.height == SyntheticHumanHeightRange.Average).ToList();
            if (compatibleTags.Count > 0) generationAssetRefs.secondaryBlendVATTag = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)];
        }

        static void FindAdditiveHeightWeightClothingVats(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, List<VATTag> assetTagList)
        {
            for (var i = 0;i< generationAssetRefs.clothingTags.Count();i++)
            {

                //generationAssetRefs.selectedClothingVATTags[i] = new ClothingVATList();



                //PRIMARY VAT BLEND
                var enumMatch = humanSpecs.normalizedHeight > 0.5f ? Enum.GetValues(typeof(SyntheticHumanHeightRange)).Length - 1 : 1;

                // Filter against normalized height, ensure the weight is average since this shape will be used for height only (no double weight applications)
                var compatibleTags = assetTagList.Where(tag => (int)tag.height == enumMatch && tag.weight == SyntheticHumanWeightRange.Average && tag.parentMesh == generationAssetRefs.clothingTags[i].name).ToList();
                if (compatibleTags.Count > 0) generationAssetRefs.clothingPrimaryBlendVATTags[i] = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] ;

                //SECONDARY VAT BLEND
                enumMatch = humanSpecs.normalizedWeight > 0.5f ? Enum.GetValues(typeof(SyntheticHumanWeightRange)).Length - 1 : 1;

                // Filter against normalized height, ensure the weight is average since this shape will be used for height only (no double weight applications)
                compatibleTags = assetTagList.Where(tag => (int)tag.weight == enumMatch && tag.height == SyntheticHumanHeightRange.Average && tag.parentMesh ==generationAssetRefs.clothingTags[i].name).ToList();
                if (compatibleTags.Count > 0) generationAssetRefs.clothingSecondaryBlendVATTags[i] = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] ;

            }
        }



        // TODO (CS): This whole function probably belongs in a more focused place. We really need to rethink how we handle
        // runtime material randomization stuff. Right now we're only really set up for sampling using premade materials
        // and tags. I think we're gonna want more systems like this where we have single materials and randomize parameters.
        static void AssignFaceBodyEyeMaterials(
            Renderer humanRenderer, SingleHumanGenerationAssetRefs generationAssetRefs, SingleHumanSpecification humanSpecs)
        {

            var newHumanMats = new Material[3];
            newHumanMats[0] = generationAssetRefs.faceMaterial;
            newHumanMats[1] = generationAssetRefs.bodyMaterial;
            newHumanMats[2] = generationAssetRefs.eyeMaterial;
            humanRenderer.materials = newHumanMats;

            var faceProps = new MaterialPropertyBlock();
            var bodyProps = new MaterialPropertyBlock();
            var eyeProps = new MaterialPropertyBlock();
            humanRenderer.GetPropertyBlock(faceProps, 0);
            humanRenderer.GetPropertyBlock(bodyProps, 1);
            humanRenderer.GetPropertyBlock(eyeProps, 2);

            // TODO(CS): this is fragile. We want this to be configurable outside of code. We should tackle when we handle the
            // more general stuff mentioned above
            var skinToneMin = 0.0f;
            var skinToneMax = 10.0f;
            switch (humanSpecs.ethnicity)
            {
                case SyntheticHumanEthnicity.African:
                    skinToneMin = 5.0f;
                    break;
                case SyntheticHumanEthnicity.Asian:
                    skinToneMax = 5.0f;
                    break;
                case SyntheticHumanEthnicity.Caucasian:
                    skinToneMax = 5.0f;
                    break;
                case SyntheticHumanEthnicity.LatinAmerican:
                    skinToneMin = 4.0f;
                    skinToneMax = 7.0f;
                    break;
                case SyntheticHumanEthnicity.MiddleEastern:
                    skinToneMin = 4.0f;
                    skinToneMax = 7.0f;
                    break;
            }
            var skinTone = s_RandomGenerator.NextFloat() * (skinToneMax - skinToneMin) + skinToneMin;
            var subTone = s_RandomGenerator.NextFloat() * 10.0f;

            bodyProps.SetFloat(k_Tone, skinTone);
            bodyProps.SetFloat(k_Subtone, subTone);
            faceProps.SetFloat(k_Tone, skinTone);
            faceProps.SetFloat(k_Subtone, subTone);
            eyeProps.SetFloat(k_Rotation, s_RandomGenerator.NextFloat() * 360);

            humanRenderer.SetPropertyBlock(faceProps, 0);
            humanRenderer.SetPropertyBlock(bodyProps, 1);
            humanRenderer.SetPropertyBlock(eyeProps, 2);
        }

        static void GenerateHair(SingleHumanGenerationAssetRefs generationAssetRefs, GeneratedSkeletonInfo skeletonInfo, SkinnedMeshRenderer humanRenderer)
        {
            // Add Hair
            var sourceHairRenderer = generationAssetRefs.hairMesh.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh duplicatedHairMesh;
            if (sourceHairRenderer != null)
            {
                duplicatedHairMesh = DuplicateBaseMesh(sourceHairRenderer.sharedMesh);
            }
            else
            {
                duplicatedHairMesh = DuplicateBaseMesh(generationAssetRefs.hairMesh.GetComponentInChildren<MeshFilter>().sharedMesh);
            }

            var newHairObject = new GameObject("Hair");
            ApplyMeshBase(duplicatedHairMesh, newHairObject);

            var newHairRenderer = newHairObject.GetComponent<SkinnedMeshRenderer>();
            newHairObject.transform.parent = humanRenderer.transform;
            newHairRenderer.material = generationAssetRefs.hairMaterial;

            // TODO (CS): This should be replaced by a normal bind from a weight file once we have those for hair.
            // It's interesting that this is seemingly enough to get the hair bound to the head end. If you try
            // actually setting the bone weights, it causes the hair to position itself at the root of the object.
            // Presumably, this has something to do with the bindposes?
            newHairRenderer.rootBone = skeletonInfo.OrderedBones[skeletonInfo.BoneNameIndices["head_end"]];

            if (generationAssetRefs.hairMatTag.allowPropertyRandomization == SyntheticHumanEnumBool.True)
            {
                RandomizeMaterialParameters(newHairRenderer);
            }
        }
        static void ReviewAndAssignClothingTags(SingleHumanSpecification humanSpecs, SingleHumanGenerationAssetRefs generationAssetRefs, SyntheticHumanAssetPool assetTagPool)
        {
            // TODO: (LP) Build out compatibility matrix for clothing that doesn't recycle filtering logic from human profile

            //RemoveDuplicateClothingElementsFromAlreadyAssignedOnes(generationAssetRefs);
            //TODO (MK): Do we really want this? AFAI can tell it is only for when we are debugging and we assign more than on clothing item of the same element.

            foreach (var element in humanSpecs.requiredClothing)
            {
                // var assigned = generationAssetRefs.clothingTags.Exists(tag => tag.clothingElement == element.baseMeshTag && tag.layer == element.layer);
                //
                // if (assigned)
                //     continue;

                var availableTags = assetTagPool.filteredClothingTags.Where(tag => tag.clothingElement == element.clothingElement && tag.layer == element.layer);

                var propertiesToMatch = new List<Type>
                {
                    typeof(SyntheticHumanAgeRange),
                    typeof(SyntheticHumanGender)
                };

                var compatibleTags = availableTags.Where(tag => DoSpecsAndTagExactlyMatch(propertiesToMatch, humanSpecs, tag)).ToList();
                if (compatibleTags.Count > 0)
                {
                    var randomTag = compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)];
                    generationAssetRefs.clothingTags.Add(randomTag);
                }
            }

            var fault = false;
            foreach (var clothingTag in generationAssetRefs.clothingTags)
            {
                List<MaterialTag> compatibleClothingMats;
                compatibleClothingMats = string.IsNullOrEmpty(clothingTag.materialId) ?
                    assetTagPool.filteredClothingMatTags.Where(tag => tag.materialType == clothingTag.materialType).ToList() :
                    assetTagPool.filteredClothingMatTags.Where(tag => tag.materialId == clothingTag.materialId).ToList();

                if (compatibleClothingMats.Count > 0)
                    generationAssetRefs.clothingMatTags.Add(compatibleClothingMats[s_RandomGenerator.NextInt(0, compatibleClothingMats.Count)]);
                else
                {
                    Debug.LogError($"Could not find compatible material tags for a clothing item. Requested clothing material type: {clothingTag.materialType}. Requested clothing materialId: {clothingTag.materialId}. Clothing will not be generated.");
                    fault = true;
                    break;
                }
            }

            if (fault)
            {
                generationAssetRefs.clothingTags.Clear();
                generationAssetRefs.clothingMats.Clear();
                //clear the clothing mesh and material tag lists so that no clothing is generated, otherwise there will be exceptions later on.
            }
        }

        #endregion

        #region AssetLoading

        static bool LoadAndAssignFinalAssets(SingleHumanGenerationAssetRefs generationAssetRefs)
        {
            //TODO (MK): Make synthetic human tag a generic type on the type of asset loaded. Then this function can be significantly shrunk down.

            try
            {
                //Body mesh -----------------------------------
                if (!generationAssetRefs.bodyMeshTag)
                {
                    Debug.LogError($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a body mesh tag after asset population. Skipping human creation.");
                    return false;
                }

                var bodyObj = LoadFromLinkedAsset<GameObject>(generationAssetRefs.bodyMeshTag);
                generationAssetRefs.bodyMesh = DuplicateMeshFromGameObject(bodyObj);
                //--------------------------------------------


                //Body material -------------------------------
                if (!generationAssetRefs.bodyMatTag)
                {
                    Debug.LogError($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a body material tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.bodyMaterial = LoadFromLinkedAsset<Material>(generationAssetRefs.bodyMatTag);
                //---------------------------------------------


                //Body height/weight vats ---------------------
                if (generationAssetRefs.primaryBlendVATTag)
                {
                    generationAssetRefs.primaryBlendVAT = LoadFromLinkedAsset<Texture2D>(generationAssetRefs.primaryBlendVATTag);
                }

                if (generationAssetRefs.secondaryBlendVATTag)
                {
                    generationAssetRefs.secondaryBlendVAT = LoadFromLinkedAsset<Texture2D>(generationAssetRefs.secondaryBlendVATTag);
                }

                //---------------------------------------------

                //Face VAT -------------------------------
                if (!generationAssetRefs.faceVATTag)
                {
                    Debug.LogWarning($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a face VAT tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.faceVAT = LoadFromLinkedAsset<Texture2D>(generationAssetRefs.faceVATTag);
                //---------------------------------------------

                //Face material -------------------------------
                if (!generationAssetRefs.faceMatTag)
                {
                    Debug.LogWarning($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a face material tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.faceMaterial = LoadFromLinkedAsset<Material>(generationAssetRefs.faceMatTag);
                //---------------------------------------------

                //Eye material -------------------------------
                if (!generationAssetRefs.eyeMatTag)
                {
                    Debug.LogWarning($"A {nameof(SingleHumanGenerationAssetRefs)} is missing an eye material tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.eyeMaterial = LoadFromLinkedAsset<Material>(generationAssetRefs.eyeMatTag);
                //---------------------------------------------

                //Hair mesh -------------------------------
                if (!generationAssetRefs.hairMeshTag)
                {
                    Debug.LogWarning($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a hair mesh tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.hairMesh = LoadFromLinkedAsset<GameObject>(generationAssetRefs.hairMeshTag);
                //---------------------------------------------

                //Hair material -------------------------------
                if (!generationAssetRefs.hairMatTag)
                {
                    Debug.LogWarning($"A {nameof(SingleHumanGenerationAssetRefs)} is missing a hair material tag after asset population. Skipping human creation.");
                    return false;
                }

                generationAssetRefs.hairMaterial = LoadFromLinkedAsset<Material>(generationAssetRefs.hairMatTag);
                //---------------------------------------------

                //Clothing meshes------------------------------
                foreach (var clothingTag in generationAssetRefs.clothingTags)
                {
                    var clothingObj = LoadFromLinkedAsset<GameObject>(clothingTag);
                    generationAssetRefs.selectedClothing.Add(DuplicateMeshFromGameObject(clothingObj));
                }

                //---------------------------------------------

                //Clothing VATs------------------------------

                foreach (var primaryClothingVatTag in generationAssetRefs.clothingPrimaryBlendVATTags)
                {
                    var clothingBlendVatTag = LoadFromLinkedAsset<Texture2D>(primaryClothingVatTag);
                    generationAssetRefs.clothingPrimaryBlendVAT.Add(clothingBlendVatTag);
                }

                foreach (var secondaryClothingVatTag in generationAssetRefs.clothingSecondaryBlendVATTags)
                {
                    var clothingBlendVatTag = LoadFromLinkedAsset<Texture2D>(secondaryClothingVatTag);
                    generationAssetRefs.clothingSecondaryBlendVAT.Add(clothingBlendVatTag);
                }
                //---------------------------------------------
            }
            catch (Exception e)
            {
                Debug.LogError($"Human asset loading failed: {e}");
                return false;
            }

            return true;
        }

        static T LoadFromLinkedAsset<T>(SyntheticHumanTag tag) where T : Object
        {
            try
            {
                return (T)tag.linkedAsset;
            }
            catch
            {
                throw new Exception($"Could not load tag linked asset of type {typeof(T).Name}. Check asset tag named {tag.name}");
            }
        }

        static Mesh ExtractMeshFromGameObject(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
            var skinnedRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();

            Mesh mesh = null;
            if (meshFilter && skinnedRenderer)
            {
                throw new Exception($"Error while extracting mesh from {gameObject.name}. GameObject contains both a MeshFilter and a SkinnedMeshRenderer.");
            }
            else if (meshFilter)
            {
                mesh = meshFilter.sharedMesh;
            }
            else if (skinnedRenderer)
            {
                mesh = skinnedRenderer.sharedMesh;
            }

            if (!mesh)
            {
                throw new Exception($"Error while extracting mesh from {gameObject.name}. Cannot find mesh on GameObject.");
            }

            return mesh;
        }

        static Mesh DuplicateMeshFromGameObject(GameObject gameObject)
        {
            return DuplicateBaseMesh(ExtractMeshFromGameObject(gameObject));
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Adds all nested children of parentGameObject to the list listOfChildren
        /// </summary>
        // static void GetChildRecursive(GameObject parentGameObject, List<GameObject> listOfChildren ){
        //     if (null == parentGameObject)
        //         return;
        //
        //     foreach (Transform child in parentGameObject.transform){
        //         if (null == child)
        //             continue;
        //
        //         listOfChildren.Add(child.gameObject);
        //         GetChildRecursive(child.gameObject, listOfChildren);
        //     }
        // }


        static void RandomizeMaterialParameters(Renderer renderer, int materialIndex = 0)
        {
            var propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock, materialIndex);
            var randomizer = new SyntheticHumanMaterialParameterModifier(renderer.sharedMaterials[materialIndex], propBlock);
            randomizer.Randomize();
            renderer.SetPropertyBlock(propBlock);
        }

        /// <summary>
        /// Returns a newly constructed mesh that mirrors several parameters from originalMesh
        /// </summary>
        /// ///
        static Mesh DuplicateBaseMesh(Mesh originalMesh)
        {
            var returnableMesh = new Mesh
            {
                name = originalMesh.name,
                vertices = originalMesh.vertices,
                uv = originalMesh.uv,
                uv2 = originalMesh.uv2,
                triangles = originalMesh.triangles,
                subMeshCount = originalMesh.subMeshCount
            };

            for (var i = 0; i < originalMesh.subMeshCount; i++)
            {
                returnableMesh.SetSubMesh(i, originalMesh.GetSubMesh(i));
            }

            returnableMesh.RecalculateNormals();

            return returnableMesh;
        }

        /// <summary>
        /// Adds a skinned mesh renderer to starterObject
        /// </summary>
        static void ApplyMeshBase(Mesh inputMesh, GameObject starterObject)
        {
            var newMeshRenderer = starterObject.AddComponent<SkinnedMeshRenderer>();
            newMeshRenderer.updateWhenOffscreen = true;
            newMeshRenderer.sharedMesh = inputMesh;
        }

        #endregion

        #region ClothingWork
        /// <summary>
        /// Creates clothing on a human using assets defined in a <see cref="SingleHumanGenerationAssetRefs"/> that should already be set up
        /// </summary>
        static void GenerateClothes(SingleHumanGenerationAssetRefs targetAssetRefs, GeneratedSkeletonInfo skeletonInfo)
        {
            var targetHuman = targetAssetRefs.gameObject;

            // Loop through sampled clothing tags
            for (var i = 0; i < targetAssetRefs.selectedClothing.Count; i++)
            {
                // Make a mesh
                var newClothingItem = new GameObject("ClothingItem_" + i);
                ApplyMeshBase(targetAssetRefs.selectedClothing[i], newClothingItem);

                // Apply materials. Sub-meshes require a loop.
                var targetRenderer = newClothingItem.GetComponent<SkinnedMeshRenderer>();
                var clothingMats = new Material[targetRenderer.sharedMesh.subMeshCount];

                for (var j = 0; j < targetRenderer.sharedMesh.subMeshCount; j++)
                {
                    var clothingMatResource = (Material)targetAssetRefs.clothingMatTags[i].linkedAsset;
                    clothingMats[j] = clothingMatResource;
                }
                targetRenderer.materials = clothingMats;

                //Need to do this after all the materials are set, otherwise it won't be able to access parameters for randomizing
                if (targetAssetRefs.clothingMatTags[i].allowPropertyRandomization == SyntheticHumanEnumBool.True)
                {
                    for (var j = 0; j < targetRenderer.sharedMesh.subMeshCount; j++)
                        RandomizeMaterialParameters(targetRenderer, j);
                }

                // Parent to human
                newClothingItem.transform.parent = targetHuman.transform;
                targetAssetRefs.createdClothing.Add(newClothingItem);
                //
                // var clothingLabel = newClothingItem.AddComponent<Labeling>();
                // clothingLabel.labels.Add("clothing");

            }
        }

        static void BindClothesToSkeleton(SingleHumanGenerationAssetRefs targetAssetRefs, GeneratedSkeletonInfo skeletonInfo)
        {
            for (int i = 0; i < targetAssetRefs.createdClothing.Count; i++)
            {

                var targetRenderer = targetAssetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>();


                // If attachment and rotation are needed, set root and rotate
                if (targetAssetRefs.clothingTags[i].attachedToJoint == SyntheticHumanEnumBool.True)
                {
                    targetRenderer.rootBone = skeletonInfo.OrderedBones[skeletonInfo.BoneNameIndices[targetAssetRefs.clothingTags[i].jointName]];
                    // var clothingTransform = targetRenderer.transform;
                    // clothingTransform.parent = skeletonInfo.OrderedBones[skeletonInfo.BoneNameIndices[targetAssetRefs.clothingTags[i].jointName]];
                    // clothingTransform.localPosition = Vector3.zero;
                    // clothingTransform.localEulerAngles = targetAssetRefs.clothingTags[i].rotationOffset;

                }
                else
                    BindSkinnedRendererToSkeleton(
                        targetRenderer,
                        skeletonInfo,
                        targetAssetRefs.clothingTags[i],
                        targetAssetRefs.clothingPrimaryBlendVATTags[i]);
            }
        }
        #endregion

        #region AnimationAndRigging

        /// <summary>
        /// Binds a given SkinnedMeshRenderer to an existing skeleton, copying bone weights and bind poses from
        /// a provided weight file.
        /// </summary>
        /// <param name="targetRenderer">The target renderer to be bound. to copy the boneWeights and bindPoses to</param>
        /// <param name="skeletonInfo">The GeneratedSkeletonInfo of the skeleton we're binding to</param>
        static void BindSkinnedRendererToSkeleton(SkinnedMeshRenderer targetRenderer, GeneratedSkeletonInfo skeletonInfo, MeshTag meshTag, VATTag vatTag)
        {
            // TODO (CS): these are big enough branches that we probably want to factor these out to separate methods
            if (vatTag.loadWeightsFromMesh)
            {
                var sourceRenderer = LoadFromLinkedAsset<GameObject>(meshTag).GetComponentInChildren<SkinnedMeshRenderer>();
                var sourceWeights = sourceRenderer.sharedMesh.boneWeights;

                var boneIndexNames = new Dictionary<int, string>();
                for (int i = 0; i < sourceRenderer.bones.Length; i++)
                {
                    boneIndexNames[i] = sourceRenderer.bones[i].gameObject.name;
                }

                var newWeights = new BoneWeight[sourceWeights.Length];
                for (var i = 0; i < newWeights.Length; i++)
                {
                    newWeights[i] = new BoneWeight()
                    {
                        weight0 = sourceWeights[i].weight0,
                        weight1 = sourceWeights[i].weight1,
                        weight2 = sourceWeights[i].weight2,
                        weight3 = sourceWeights[i].weight3,
                        boneIndex0 = skeletonInfo.BoneNameIndices[boneIndexNames[sourceWeights[i].boneIndex0]],
                        boneIndex1 = skeletonInfo.BoneNameIndices[boneIndexNames[sourceWeights[i].boneIndex1]],
                        boneIndex2 = skeletonInfo.BoneNameIndices[boneIndexNames[sourceWeights[i].boneIndex2]],
                        boneIndex3 = skeletonInfo.BoneNameIndices[boneIndexNames[sourceWeights[i].boneIndex3]],
                    };
                }

                targetRenderer.sharedMesh.boneWeights = newWeights;
                targetRenderer.sharedMesh.bindposes = skeletonInfo.InitialPoses;
                targetRenderer.rootBone = skeletonInfo.SkeletonRoot.transform;
                targetRenderer.bones = skeletonInfo.OrderedBones;
            }
            else
            {
                Debug.LogWarning($"Not loading weights for {vatTag.name} because non-fbx weight loader not implemented.");
            }
        }
        #endregion

        #region VATwork

        // TODO (CS) : VAT application creates new vertices even though we've already created a copy of the vertices in a previous step. Should probably modify in place instead
        static void ApplyBodyVats(SingleHumanGenerationAssetRefs assetRefs, SingleHumanSpecification humanSpecs)
        {
            // Apply facial VATs
            assetRefs.bodyMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.bodyMesh, assetRefs.bodyMesh.vertices, assetRefs.faceVATTag, 1).vertices;

            // Apply height and weight VATs

            switch (humanSpecs.heightWeightSolver)
            {
                // Discrete solver uses one shape for height and weight. It does not apply a blend percentage
                case SyntheticHumanHeightWeightSolver.Discrete:
                {
                    if (assetRefs.primaryBlendVAT)
                    {
                        assetRefs.bodyMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.bodyMesh, assetRefs.bodyMesh.vertices, assetRefs.primaryBlendVATTag,  1, assetRefs.primaryBlendVAT).vertices;
                    }

                    break;
                }

                // BlendTarget and Additive solvers use blend percentages. A BlendTarget Mesh should not have any secondary VATs applied, so it can live in this if statement with the assumption slot 2 is null
                case SyntheticHumanHeightWeightSolver.Additive:
                case SyntheticHumanHeightWeightSolver.BlendTarget:
                {
                    if (assetRefs.primaryBlendVAT)
                    {
                        var heightBlendValue = Mathf.Abs((humanSpecs.normalizedHeight - .5f) * 2f);
                        assetRefs.bodyMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.bodyMesh, assetRefs.bodyMesh.vertices, assetRefs.primaryBlendVATTag, heightBlendValue, assetRefs.primaryBlendVAT).vertices;
                    }

                    if (assetRefs.secondaryBlendVAT)
                    {
                        var weightBlendValue = Mathf.Abs((humanSpecs.normalizedWeight - .5f) * 2f);
                        assetRefs.bodyMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.bodyMesh, assetRefs.bodyMesh.vertices, assetRefs.secondaryBlendVATTag, weightBlendValue, assetRefs.secondaryBlendVAT).vertices;
                    }
                        assetRefs.bodyMesh.RecalculateNormals();
                    break;
                }
            }
        }

        // TODO (CS) : VAT application creates new vertices even though we've already created a copy of the vertices in a previous step. Should probably modify in place instead
        static void ApplyClothingVats(SingleHumanGenerationAssetRefs assetRefs, SingleHumanSpecification humanSpecs)
        {
            for (var i = 0; i < assetRefs.selectedClothing.Count(); i++)
            {
                // Apply height and weight VATs
                switch (humanSpecs.heightWeightSolver)
                {
                    // Discrete solver uses one shape for height and weight. It does not apply a blend percentage
                    case SyntheticHumanHeightWeightSolver.Discrete:
                    {
                        if (assetRefs.primaryBlendVAT)
                        {
                            assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh, assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices, assetRefs.clothingPrimaryBlendVATTags[i], 1, assetRefs.clothingSecondaryBlendVAT[i]).vertices;
                        }

                        break;
                    }

                    // BlendTarget and Additive solvers use blend percentages. A BlendTarget Mesh should not have any secondary VATs applied, so it can live in this if statement with the assumption slot 2 is null
                    case SyntheticHumanHeightWeightSolver.Additive:
                    case SyntheticHumanHeightWeightSolver.BlendTarget:
                    {
                        if (assetRefs.clothingPrimaryBlendVATTags[i])
                        {
                            var heightBlendValue = Mathf.Abs((humanSpecs.normalizedHeight - .5f) * 2f);
                            assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh, assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices, assetRefs.clothingPrimaryBlendVATTags[i], heightBlendValue, assetRefs.clothingPrimaryBlendVAT[i]).vertices;
                        }

                        if (assetRefs.clothingSecondaryBlendVATTags[i])
                        {
                            var weightBlendValue = Mathf.Abs((humanSpecs.normalizedWeight - .5f) * 2f);
                            assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices = SyntheticHumanVat.SyntheticHumanVatDelta(assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh, assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices, assetRefs.clothingSecondaryBlendVATTags[i], weightBlendValue, assetRefs.clothingSecondaryBlendVAT[i]).vertices;
                        }
                            assetRefs.createdClothing[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.RecalculateNormals();
                        break;
                    }
                }
            }
        }

        #endregion
    }
}
