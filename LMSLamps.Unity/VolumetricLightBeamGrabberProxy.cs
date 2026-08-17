using UnityEngine;

namespace LMSLamps.Unity
{
    public class VolumetricLightBeamGrabberProxy : MonoBehaviour
    {
        [Header("VolumetricLightBeam Configuration")]
        [Tooltip("The GameObjects to apply the VolumetricLightBeam script to at runtime")]
        public GameObject[]? targetObjects;

        private void Awake()
        {
            // The actual script application will be handled by the Game-side VolumetricLightBeamGrabber
            // This proxy just serves as a placeholder/marker for the Unity prefab
            // The Game-side component will replace this proxy at runtime
        }

        // Optional: For debugging in Unity Editor
        private void OnValidate()
        {
            if (targetObjects == null || targetObjects.Length == 0)
            {
                Debug.LogWarning($"[VolumetricLightBeamGrabberProxy] No target objects are assigned on {gameObject.name}");
            }
        }
    }
}
