using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    /// <summary>
    /// This tag is to label the stop of the ray-casting
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RayBasedGroundPlacerTag : MonoBehaviour
    {
        // Given a ray, find the first hit on the collider that has the tag attached
        public static bool Raycast(Ray ray, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(ray);
            var index = -1;
            for (var i = 0; i < hits.Length; i++)
            {
                var tag = hits[i].collider.GetComponent<RayBasedGroundPlacerTag>();
                if (tag != null && (index == -1 || hits[i].distance < hits[index].distance))
                {
                    index = i;
                }
            }
            hit = index == -1 ? new RaycastHit() : hits[index];
            return index != -1;
        }
    }
}
