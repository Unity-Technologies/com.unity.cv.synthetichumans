using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    /// <summary>
    /// A component derived from this class can be put on the base prefab supplied to a HumanGenerator
    /// in order to have its methods called at certain points during the human generation lifecycle.
    /// </summary>
    public abstract class HumanGenerationLifecycleSubscriber : MonoBehaviour
    {
        /// <summary>
        /// This method is called when human generation is completed and the human is ready to be used in a scene.
        /// </summary>
        public virtual void OnGenerationComplete() { }
    }
}
