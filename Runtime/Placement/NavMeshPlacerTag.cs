using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.CV.SyntheticHumans.Placement
{
    public abstract class NavMeshPlacerTag : MonoBehaviour
    {
        public enum AreaType
        {
            All,
            [InspectorName("NavMesh Surface")]
            NavMeshSurface,
            [InspectorName("NavMesh Modifier")]
            NavMeshModifier,
            [InspectorName("NavMesh Volume")]
            NavMeshModifierVolume,
        }

        [Tooltip("This type defines which component on the same game object the tag will use to fetch the NavMesh area type.")]
        public AreaType area;

        // Check if the tag and the corresponding modifier is active and valid
        public bool IsActive()
        {
            if (!isActiveAndEnabled)
                return false;
            switch (area)
            {
                case AreaType.All:
                    return true;
                case AreaType.NavMeshSurface:
                    var surface = GetComponent<NavMeshSurface>();
                    return surface.isActiveAndEnabled && surface.defaultArea != 1;
                case AreaType.NavMeshModifier:
                    var modifier = GetComponent<NavMeshModifier>();
                    return modifier.isActiveAndEnabled && !modifier.ignoreFromBuild && modifier.overrideArea &&
                           modifier.area != 1;
                case AreaType.NavMeshModifierVolume:
                    var modifierVolume = GetComponent<NavMeshModifierVolume>();
                    return modifierVolume.isActiveAndEnabled && modifierVolume.area != 1;
            }

            return false;
        }

        public int GetArea()
        {
            switch (area)
            {
                case AreaType.All:
                    return NavMesh.AllAreas;
                case AreaType.NavMeshSurface:
                    var surface = GetComponent<NavMeshSurface>();
                    return surface.defaultArea;
                case AreaType.NavMeshModifier:
                    var modifier = GetComponent<NavMeshModifier>();
                    return modifier.area;
                case AreaType.NavMeshModifierVolume:
                    var modifierVolume = GetComponent<NavMeshModifierVolume>();
                    return modifierVolume.area;
            }
            // Default to be Not Walkable area
            return 1;
        }

        // Get the list of active and valid tags from the scene
        public static List<T> GetActivePlacerTags<T>() where T : NavMeshPlacerTag
        {
            var tags = FindObjectsOfType<T>();
            return tags.Where(t => t.IsActive()).ToList();
        }

        public static int GetAreaMask<T>() where T : NavMeshPlacerTag => GetAreaMask(GetActivePlacerTags<T>());

        // Get the area masks for all the given tags
        public static int GetAreaMask<T>(IEnumerable<T> tags) where T : NavMeshPlacerTag
        {
            var areaMask = 0;
            foreach (var tag in tags)
            {
                var area = tag.GetArea();
                if (area == NavMesh.AllAreas)
                    return area;
                areaMask |= 1 << tag.GetArea();
            }
            return areaMask;
        }

        void OnEnable()
        {
            area = FindAreaType();
        }

        private void OnValidate()
        {
            area = FindAreaType();
        }

        // Auto-select area type from the parent game object
        AreaType FindAreaType()
        {
            var success = gameObject.GetComponent<NavMeshSurface>() != null;
            if (success)
            {
                return AreaType.NavMeshSurface;
            }

            success = gameObject.GetComponent<NavMeshModifier>() != null;
            if (success)
            {
                return AreaType.NavMeshModifier;
            }

            success = gameObject.GetComponent<NavMeshModifierVolume>() != null;
            if (success)
            {
                return AreaType.NavMeshModifierVolume;
            }
            return AreaType.All;
        }
    }
}
