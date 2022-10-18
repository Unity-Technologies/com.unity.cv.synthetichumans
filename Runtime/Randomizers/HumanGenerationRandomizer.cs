using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CV.SyntheticHumans.Generators;
using UnityEngine.Perception.Randomization.Randomizers;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Human Generation Randomizer")]
    public class HumanGenerationRandomizer : Randomizer
    {
        public IntegerParameter activeHumansInEachIteration = new IntegerParameter {value = new UniformSampler(0, 10)};
        public int humanPoolSize = 50;
        public int poolRefreshIntervalIterations = 50;
        public HumanGenerationConfigParameter humanGenerationConfigs = new HumanGenerationConfigParameter();

        GameObject m_PoolParent;
        List<GameObject> m_HumansInPool;
        string m_PoolObjectNamePrefix = "HumanPool_";
        Mathematics.Random m_RandomGenerator;

        int m_DetectedStartingIteration;
        //on USim, when using N instances, the starting iteration can be between 0 and N-1,
        int m_IterationInterval = -1;
        //on USim, scenario.currentIteration starts from the Instance's index number and is always incremented by the number of instances. E.g. with 10 USim instances, the iteration numbers for Instance number 4 start from 3 (0-indexed) and continue to 13, 23, 33, ....

        Dictionary<HumanGenerationConfig, HumanGenerationConfig> m_RunTimeCopiesOfConfigs;
        //the configs are scriptable objects. work on copies of them at runtime to make sure we don't change the assets.

        protected override void OnScenarioStart()
        {
            base.OnScenarioStart();
            m_RunTimeCopiesOfConfigs = new Dictionary<HumanGenerationConfig, HumanGenerationConfig>();
            foreach (var config in humanGenerationConfigs.categories.Select(cat => cat.Item1))
            {
                if (m_RunTimeCopiesOfConfigs.ContainsKey(config))
                {
                    Debug.LogError($"Duplicate {nameof(HumanGenerationConfig)} assets have been added to the {nameof(HumanGenerationRandomizer)}. This will cause an incorrect distribution of configs. Please make sure each added config is a unique asset.");
                }
                else
                {
                    m_RunTimeCopiesOfConfigs.Add(config, Object.Instantiate(config));
                }
            }

            m_HumansInPool = new List<GameObject>();
            m_RandomGenerator = SamplerState.CreateGenerator();
            m_PoolParent = new GameObject($"{m_PoolObjectNamePrefix}_{GetType().Name}");
            m_DetectedStartingIteration = scenario.currentIteration;

            foreach (var config in m_RunTimeCopiesOfConfigs.Values)
            {
                config.Init();
            }

            RefreshHumanPool();
        }

        protected override void OnIterationStart()
        {
            if (scenario.currentIteration != m_DetectedStartingIteration && m_IterationInterval == -1)
            {
                //figure out the iteration increment interval
                m_IterationInterval = scenario.currentIteration - m_DetectedStartingIteration;
            }

            var actualCurrentIteration = (scenario.currentIteration - m_DetectedStartingIteration) / m_IterationInterval;
            if (actualCurrentIteration != 0 && actualCurrentIteration % poolRefreshIntervalIterations == 0)
            {
                RefreshHumanPool();
            }

            ActivateRandomSubsetOfHumans();
        }

        protected override void OnIterationEnd()
        {
            DeactivateAllHumans();
        }

        void RefreshHumanPool()
        {
            DestroyUnusedHumans();

            var tryCount = 0;
            while(m_HumansInPool.Count < humanPoolSize && tryCount < 500)
            {
                tryCount++;
                var configToUse = humanGenerationConfigs.Sample();
                configToUse = m_RunTimeCopiesOfConfigs[configToUse];
                var human = HumanGenerator.GenerateHuman(configToUse);
                if (!human)
                    continue;
                human.name = $"GeneratedHuman_{m_HumansInPool.Count} - based on {configToUse.name}";
                m_HumansInPool.Add(human);
                human.transform.SetParent(m_PoolParent.transform);

                human.SetActive(false);
            }
        }

        void ActivateRandomSubsetOfHumans()
        {
            var activeHumans = activeHumansInEachIteration.Sample();

            var tmpList = m_HumansInPool.OrderBy(x => m_RandomGenerator.NextFloat(0, 1)).Take(activeHumans);
            foreach (var human in tmpList)
            {
                human.SetActive(true);
            }
        }

        void DeactivateAllHumans()
        {
            foreach (var human in m_HumansInPool)
            {
                human.SetActive(false);
            }
        }

        void DestroyUnusedHumans()
        {
            foreach (var human in m_HumansInPool)
            {
                Object.Destroy(human);
            }
            m_HumansInPool.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
