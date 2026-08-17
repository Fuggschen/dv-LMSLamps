using UnityEngine;

namespace LMSLamps.Unity
{
    public class GlareMaterialGrabberProxy : MonoBehaviour
    {
        [Header("Glare Configuration")]
        [Tooltip("Transform where the glare object will be instantiated as a child")]
        public Transform? glare;

        private void Awake()
        {
            // The actual glare creation will be handled by the Game-side GlareMaterialGrabber
            // This proxy just serves as a placeholder/marker for the Unity prefab
            // The Game-side component will replace this proxy at runtime
        }
    }
}
