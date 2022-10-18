using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;


namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    public class PlacementVolumeProjectorTag : RandomizerTag
    {
        public bool useAbsoluteProjectionAngle = true;
        public Vector3 projectionVector = new Vector3(0, -1, 0);
    }
}