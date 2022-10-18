using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.CV.SyntheticHumans.Placement;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Placement - NavMesh Placement Randomizer")]
    public class NavMeshPlacementRandomizer : Randomizer
    {
        public int maxRetries = 5;
        public List<Camera> cameras;

        static Mathematics.Random s_RandomGenerator;

        protected override void OnScenarioStart()
        {
            s_RandomGenerator.state = SamplerState.NextRandomState();
        }

        protected override void OnIterationStart()
        {
            // Initialize NavMesh from all NavMeshSurface components
            foreach (var surface in NavMeshSurface.activeSurfaces)
            {
                surface.UpdateNavMesh();
            }

            // Place all objects that has Human Placement Randomizer Tag
            var tags = tagManager.Query<NavMeshPlacementRandomizerTag>();
            var failures = new List<GameObject>();
            foreach (var tag in tags)
            {
                // Find the placer to place humans
                var placer = tag.SamplePlacer();
                if (placer == null)
                {
                    Debug.LogWarning("Placer is null. Skipping the placement on NavMesh surface.");
                    continue;
                }
                placer.placementRandomizer = this;

                // Place human in the scene
                var retry = 0;
                var success = false;
                while (retry < maxRetries && !success)
                {
                    success = placer.Place(tag.gameObject);
                    retry++;
                }

                if (!success)
                {
                    failures.Add(tag.gameObject);
                }
            }

            // Inactivate the game objects in the scene if they are failed to be placed
            Debug.Log($"In Frame {Time.frameCount}, the randomizer successfully placed {tags.Count() - failures.Count} " +
                      $"and failed to place {failures.Count} humans.");
            if (failures.Count > 0)
            {
                foreach (var failure in failures)
                {
                    failure.SetActive(false);
                }
            }
        }
    }
}
