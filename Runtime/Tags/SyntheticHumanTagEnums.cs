using System;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Tags
{
    public enum SyntheticHumanAgeRange
    {
        None,
        Toddler,
        Child1,
        Preteen,
        Teen,
        Adult,
        Elderly,
        Child2,
        Newborn
    }

    public enum SyntheticHumanHeightRange
    {
        None,
        Short,
        Average,
        Tall

    }
    public enum SyntheticHumanWeightRange
    {
        None,
        Small,
        Average,
        Large

    }

    public enum SyntheticHumanGender
    {
        None,
        Male,
        Female,
        Neutral
    }

    public enum SyntheticHumanEthnicity
    {
        None,
        Caucasian,
        Asian,
        LatinAmerican,
        African,
        MiddleEastern
    }

    public enum SyntheticHumanShapeLabel
    {
        None,
        Pregnant,
        MuscleBulk,
        FatOverlay

    }

    public enum SyntheticHumanFileExtension
    {
        None,
        fbx,
        exr,
        obj,
        png,
        mat,
        anim

    }

    public enum SyntheticHumanElement
    {
        None,
        Body,
        Head,
        Hair,
        Eye
    }

    public enum SyntheticHumanTextureType
    {
        None,
        Albedo,
        Mask,
        Normal,
        TranslucencyGloss,
        Identity,
        Tangent
    }

    public enum SyntheticHumanClothingElement
    {
        None,
        Shirt,
        Shoe,
        Pants,
        HeadCover,
        Glove,
        FullBody,
        Neutral
    }

    public enum SyntheticHumanClothingLabel
    {
        None,
        ActiveWear,
        Casual,
        Generic,
        ColdWeather,
    }

    public enum SyntheticHumanMaterialType
    {
        None,
        Fabric,
        Metal,
        Plastic,
        Head,
        Body,
        Hair,
        Prop,
        Shoe
    }

    public enum SyntheticHumanEyeColor
    {
        None,
        Amber,
        Blue,
        Brown,
        Gray,
        Green,
        Hazel,
        Special
    }

    public enum SyntheticHumanMuscleDefinitionRange
    {
        None,
        One,
        Two,
        Three,
        Four
    }

    public enum SyntheticHumanFacialFeatureLabel
    {
        None,
        Hair,
        LiverSpots,
        Wrinkles
    }

    public enum SyntheticHumanHeightWeightSolver
    {
        None,
        Discrete,
        BlendTarget,
        Additive
    }

    public enum PlacementType
    {
        None,
        Navmesh,
        VolumeProjection,
        CameraProjection,
        TransformRandomizer,
        PointList
    };

    // TODO (CS): Ideally we should not need this at all. Can probably get rid of it once we get rid of folder tags.
    public enum SyntheticHumanEnumBool
    {
        None,
        False,
        True
    }

    public enum SyntheticHumanClothingLayer
    {
        None,
        One,
        Two,
        Three,
        Four,
        Five
    }

}
