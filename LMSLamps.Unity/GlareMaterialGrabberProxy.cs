using UnityEngine;

namespace LMSLamps.Unity
{
    public enum GlareType
    {
        HeadlightsGlare,
        TaillightsGlare
    }

    public class GlareMaterialGrabberProxy : MonoBehaviour
    {
        [Header("Glare Configuration")]
        [Tooltip("The type of glare material to use")]
        public GlareType glareType = GlareType.HeadlightsGlare;

        [Tooltip("The GameObjects containing the glare renderers to apply the glare material to")]
        public GameObject[]? glareObjects;

        private void Awake()
        {
            // The actual material grabbing will be handled by the Game-side GlareMaterialGrabber
            // This proxy just serves as a placeholder/marker for the Unity prefab
            // The Game-side component will replace this proxy at runtime
        }

        // Optional: For debugging in Unity Editor
        private void OnValidate()
        {
            if (glareObjects == null || glareObjects.Length == 0)
            {
                Debug.LogWarning($"[GlareMaterialGrabberProxy] No glare objects are assigned on {gameObject.name}");
            }
        }
    }
}
