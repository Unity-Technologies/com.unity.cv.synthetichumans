using System;
using System.Collections;
using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;


namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    public class PlacementRandomizerTag : RandomizerTag
    {
        public PlacementType solver;
        public int solverIndex;
    }
}