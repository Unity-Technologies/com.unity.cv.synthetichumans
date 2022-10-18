using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Tags
{
    [Serializable]
    public abstract class MeshTag : SyntheticHumanTag
    {
        [JsonProperty("boneindex")]
        public List<string> boneOrder;
    }
}
