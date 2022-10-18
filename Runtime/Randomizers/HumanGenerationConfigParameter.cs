using System;
using UnityEngine.Perception.Randomization.Parameters;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    /// <summary>
    /// A categorical parameter for selecting a material group from a list of material groups
    /// </summary>
    [Serializable]
    public class HumanGenerationConfigParameter : CategoricalParameter<HumanGenerationConfig> {}
}
