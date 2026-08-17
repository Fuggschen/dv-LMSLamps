using custom_item_components;
using UnityEngine;

namespace LMSLamps.Unity
{
    public class LanternProxy : GadgetBase
    {
        [Header("Material Configuration")]
        [Tooltip("Material to use when lantern is off (no light)")]
        public Material? offMaterial;

        [Tooltip("Array of materials for different colors (e.g., red, yellow, green, blue)")]
        public Material[]? colorMaterials;

        [Header("Rendering")]
        [Tooltip("The renderer components that contain the lantern material")]
        public Renderer[]? lanternRenderers;

        [Tooltip("Index of the material to modify on each renderer (usually 0)")]
        public int materialIndex = 0;

        [Header("Interaction")]
        [Tooltip("GameObject with Collider for interaction (should have a MeshCollider or BoxCollider with isTrigger=true)")]
        public GameObject? interactionCollider;

        [Header("Light Source")]
        [Tooltip("The Light component (Source child object) to sync color with material emission")]
        public Light? sourceLight;

        [Header("Behavior Toggles")]
        [Tooltip("Enable flickering effect (e.g., for oil lanterns). Disable for steady light (e.g., LED).")]
        public bool useFlicker = true;

        [Tooltip("Enable delayed turn-on transition when switching from off to a color.")]
        public bool useDelayedTurnOn = true;

        [Tooltip("Enable delayed turn-off transition when switching from a color to off.")]
        public bool useDelayedTurnOff = true;
    }
}
