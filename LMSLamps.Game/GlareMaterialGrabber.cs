using System.Linq;
using UnityEngine;

namespace LMSLamps.Game
{
    public enum GlareType
    {
        HeadlightsGlare,
        TaillightsGlare
    }

    public class GlareMaterialGrabber : MonoBehaviour
    {
        private const string TINT_COLOR_PROPERTY = "_TintColor";

        // Configuration from proxy
        public GameObject[]? glareObjects;
        public GlareType glareType = GlareType.HeadlightsGlare;

        // Runtime state
        private Material? _glareMaterial;
        private Lantern? _lantern;
        private int _lastColorIndex = -1; 

        // Transition state
        private bool _isGlareTransitioning = false;
        private float _glareTransitionStartTime = 0f;
        private Color _startGlareTintColor;
        private Color _targetGlareTintColor;
        private const float TransitionDuration = 1f;

        // Flickering state
        private float _flickerOffset;
        private const float FlickerSpeed = 2f;
        private const float FlickerIntensityMin = 0.8f;
        private const float FlickerIntensityMax = 1.0f;

        public void Start()
        {
            try
            {
                GrabAndApplyMaterial();

                // Find the Lantern component on this GameObject
                _lantern = GetComponent<Lantern>();
                if (_lantern == null)
                {
                    Main.Warning($"No Lantern component found on {gameObject.name}, glare tint color will not update");
                }

                // Initialize flicker offset with a random value for variety
                _flickerOffset = Random.Range(0f, 100f);
            }
            catch (System.Exception ex)
            {
                Main.Error($"Failed to apply glare material on {gameObject.name}: {ex}");
            }
        }

        public void Update()
        {
            // Calculate flicker multiplier
            float flickerMultiplier = 1f;
            if (_lantern != null && _lantern.GetColorIndex() >= 1)
            {
                flickerMultiplier = GetFlickerMultiplier();
            }

            // Handle glare tint color transition
            if (_isGlareTransitioning && _glareMaterial != null)
            {
                // Calculate transition progress
                float elapsedTime = Time.time - _glareTransitionStartTime;
                float t = Mathf.Clamp01(elapsedTime / TransitionDuration);

                // Lerp the tint color
                Color currentTintColor = Color.Lerp(_startGlareTintColor, _targetGlareTintColor, t);

                // Apply flickering to the tint color
                currentTintColor *= flickerMultiplier;

                // Apply the lerped tint color
                if (_glareMaterial.HasProperty(TINT_COLOR_PROPERTY))
                {
                    _glareMaterial.SetColor(TINT_COLOR_PROPERTY, currentTintColor);
                }

                // Check if transition is complete
                if (t >= 1f)
                {
                    _isGlareTransitioning = false;
                    
                    // If transitioning to OFF state, disable glare objects after transition completes
                    if (_lantern != null && _lantern.GetColorIndex() == 0)
                    {
                        EnableGlareObjects(false);
                    }
                }
            }
            else if (_lantern != null && _lantern.GetColorIndex() >= 1 && _glareMaterial != null)
            {
                // Apply flickering when not transitioning but light is on
                ApplyFlickeringToGlare(flickerMultiplier);
            }

            // Start new transition if the lantern color has changed
            if (_lantern != null && _glareMaterial != null)
            {
                int currentColorIndex = _lantern.GetColorIndex();
                if (currentColorIndex != _lastColorIndex)
                {
                    _lastColorIndex = currentColorIndex;
                    StartGlareTransition();
                }
            }
        }

        private void GrabAndApplyMaterial()
        {
            // Check if glare objects are assigned
            if (glareObjects == null || glareObjects.Length == 0)
            {
                Main.Warning($"No glare objects are assigned on {gameObject.name}");
                return;
            }

            // Determine material name based on glare type
            string materialName = glareType.ToString();

            // Find the material from the game's resource pool
            Material? baseMaterial = FindMaterial(materialName);
            if (baseMaterial == null)
            {
                Main.Error($"Could not find material '{materialName}' in material pool");
                return;
            }

            // Create an instance of the material so we can modify it without affecting other objects
            _glareMaterial = new Material(baseMaterial);

            // Apply the material to all glare objects and disable them initially
            foreach (var glareObject in glareObjects)
            {
                if (glareObject == null)
                {
                    Main.Warning($"Null glare object found in array on {gameObject.name}");
                    continue;
                }

                // Get the renderer component
                Renderer glareRenderer = glareObject.GetComponent<Renderer>();
                if (glareRenderer == null)
                {
                    Main.Warning($"Could not find Renderer component on glare object '{glareObject.name}'");
                    continue;
                }

                // Apply the material instance to the renderer
                glareRenderer.material = _glareMaterial;
                
                // Disable the glare object initially (no glare flash on placement)
                glareObject.SetActive(false);
            }

            // Set initial tint color
            UpdateGlareTintColor();
        }

        private void StartGlareTransition()
        {
            if (_glareMaterial == null || _lantern == null)
                return;

            // Get current tint color as start color
            if (_glareMaterial.HasProperty(TINT_COLOR_PROPERTY))
            {
                _startGlareTintColor = _glareMaterial.GetColor(TINT_COLOR_PROPERTY);
            }
            else
            {
                _startGlareTintColor = Color.white;
            }

            // Get target color from lantern
            _targetGlareTintColor = GetLanternEmissionColor();

            // If transitioning to ON state (color index >= 1), enable glare objects
            if (_lantern.GetColorIndex() >= 1)
            {
                EnableGlareObjects(true);
            }

            // Start transition
            _isGlareTransitioning = true;
            _glareTransitionStartTime = Time.time;
        }

        private void UpdateGlareTintColor()
        {
            if (_glareMaterial == null || _lantern == null)
                return;

            // Get the current color from the lantern
            Color emissionColor = GetLanternEmissionColor();
            
            // If lantern is off, use transparent black for glare
            if (_lantern.GetColorIndex() == 0)
            {
                emissionColor = Color.clear; // Fully transparent
            }

            // Check if the material has the tint color property
            if (_glareMaterial.HasProperty(TINT_COLOR_PROPERTY))
            {
                // Set the tint color immediately (used for initial setup)
                _glareMaterial.SetColor(TINT_COLOR_PROPERTY, emissionColor);
            }
            else
            {
                Main.Warning($"Material '{glareType}' does not have property '{TINT_COLOR_PROPERTY}'");
            }
        }

        private float GetFlickerMultiplier()
        {
            // Use Perlin noise for smooth, natural-looking flicker
            float perlinValue = Mathf.PerlinNoise(Time.time * FlickerSpeed, _flickerOffset);
            
            // Map Perlin noise (0-1) to flicker intensity range
            return Mathf.Lerp(FlickerIntensityMin, FlickerIntensityMax, perlinValue);
        }

        private void ApplyFlickeringToGlare(float flickerMultiplier)
        {
            if (_glareMaterial == null || _lantern == null)
                return;

            // Get the base tint color from the lantern emission
            Color baseTintColor = GetLanternEmissionColor();
            Color flickeredColor = baseTintColor * flickerMultiplier;

            // Apply the flickered tint color
            if (_glareMaterial.HasProperty(TINT_COLOR_PROPERTY))
            {
                _glareMaterial.SetColor(TINT_COLOR_PROPERTY, flickeredColor);
            }
        }

        private Color GetLanternEmissionColor()
        {
            if (_lantern == null || _lantern.colorMaterials == null)
                return Color.white;

            int colorIndex = _lantern.GetColorIndex();
            if (colorIndex < 0 || colorIndex >= _lantern.colorMaterials.Length)
                return Color.white;

            Material? currentMaterial = _lantern.colorMaterials[colorIndex];
            if (currentMaterial == null)
                return Color.white;

            // Try to get the emission color from common emission property names
            if (currentMaterial.HasProperty("_EmissionColor"))
            {
                return currentMaterial.GetColor("_EmissionColor");
            }
            else if (currentMaterial.HasProperty("_EmissiveColor"))
            {
                return currentMaterial.GetColor("_EmissiveColor");
            }
            else if (currentMaterial.HasProperty("_Emission"))
            {
                return currentMaterial.GetColor("_Emission");
            }

            // If no emission property found, return white as fallback
            return Color.white;
        }

        private void EnableGlareObjects(bool enabled)
        {
            if (glareObjects == null)
                return;

            foreach (var glareObject in glareObjects)
            {
                if (glareObject != null)
                {
                    glareObject.SetActive(enabled);
                }
            }
        }

        private Material? FindMaterial(string materialName)
        {
            var allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            var material = allMaterials.FirstOrDefault(m => m.name == materialName);

            if (material == null)
            {
                return null;
            }
            return material;
        }
    }
}
