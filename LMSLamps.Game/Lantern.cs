using DV.CabControls;
using DV.CabControls.Spec;
using DV.Customization.Gadgets;
using UnityEngine;

namespace LMSLamps.Game
{
    public class Lantern : GadgetBase
    {
        // Configuration from proxy
        public Material? offMaterial;
        public Material[]? colorMaterials;
        public Renderer[]? lanternRenderers;
        public int materialIndex = 0;
        public GameObject? interactionCollider;
        public Light? sourceLight;

        // Behavior toggles
        public bool useFlicker = true;
        public bool useDelayedTurnOn = true;
        public bool useDelayedTurnOff = true;

        // Runtime state
        private int _currentColorIndex = 0;
        private float _lastInteractionTime = 0f;
        private const float InteractionCooldown = 0.1f;

        // Event for color changes
        public event System.Action<int>? OnColorChanged;
        
        // InfoText
        public const InteractionInfoType PowerON = (InteractionInfoType)10001;
        public const InteractionInfoType PowerOFF = (InteractionInfoType)10002;

        // Transition state
        private bool _isTransitioning = false;
        private float _transitionStartTime = 0f;
        private Color _startEmissionColor;
        private Color _targetEmissionColor;
        private Material? _targetMaterial;
        private const float TransitionDuration = 1f;

        // Flickering state
        private float _flickerOffset;
        private const float FlickerSpeed = 2f;
        private const float FlickerIntensityMin = 0.8f;
        private const float FlickerIntensityMax = 1.0f;

        // Public method for SaveGameManagerPatches to access the current color index
        public int GetColorIndex()
        {
            return _currentColorIndex;
        }

        public string GetUniqueKey()
        {
            // Use the GadgetBase UID
            return UID.ToString();
        }

        public void Start()
        {
            // Load persisted state
            LoadState();

            // Set initial material
            UpdateMaterial();

            // Set up interaction button event
            SetupButton();

            // Initialize flicker offset with a random value for variety
            _flickerOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // Calculate flicker multiplier
            float flickerMultiplier = 1f;
            if (_currentColorIndex >= 1 && useFlicker)
            {
                flickerMultiplier = GetFlickerMultiplier();
            }

            if (_isTransitioning)
            {
                // Calculate transition progress
                float elapsedTime = Time.time - _transitionStartTime;
                float t = Mathf.Clamp01(elapsedTime / TransitionDuration);

                // Lerp the emission color
                Color currentEmissionColor = Color.Lerp(_startEmissionColor, _targetEmissionColor, t);

                // Apply flickering to the emission color
                currentEmissionColor *= flickerMultiplier;

                // Apply the lerped emission color to all renderers
                if (lanternRenderers != null)
                {
                    foreach (Renderer renderer in lanternRenderers)
                    {
                        if (renderer == null) continue;
                        
                        Material[] materials = renderer.materials;
                        if (materialIndex < materials.Length)
                        {
                            Material currentMaterial = materials[materialIndex];
                            SetEmissionColor(currentMaterial, currentEmissionColor);
                        }
                    }
                }

                // Update light color
                if (sourceLight != null)
                {
                    sourceLight.color = currentEmissionColor;
                }

                // Check if transition is complete
                if (t >= 1f)
                {
                    CompleteTransition();
                }
            }
            else if (_currentColorIndex >= 1 && useFlicker)
            {
                // Apply flickering when not transitioning but light is on
                ApplyFlickeringToCurrentState(flickerMultiplier);
            }
        }

        private void SetupButton()
        {
            // Create Button component at runtime on the interaction collider
            GameObject targetObject = interactionCollider != null ? interactionCollider : gameObject;

            // Ensure the target has a collider
            var collider = targetObject.GetComponent<Collider>();

            // Ensure collider is a trigger
            collider.isTrigger = true;

            // Set layer to Interactable
            targetObject.layer = LayerMask.NameToLayer("Interactable");

            // Deactivate before adding components to defer Awake() until configuration is complete
            targetObject.SetActive(false);

            // Create Button component at runtime
            var buttonSpec = targetObject.AddComponent<Button>();
            buttonSpec.createRigidbody = false;
            buttonSpec.useJoints = false;
            buttonSpec.colliderGameObjects = new GameObject[] { targetObject };
            
            // Add InfoArea for interaction prompt
            var infoArea = targetObject.AddComponent<InfoArea>();
            infoArea.infoType = _currentColorIndex >= 1 ? PowerOFF : PowerON;

            // Reactivate to trigger Awake() with all components configured
            targetObject.SetActive(true);

            // Hook up the Used event after the GameObject is activated
            var buttonBase = targetObject.GetComponent<ButtonBase>();
            if (buttonBase != null)
            {
                buttonBase.Used += OnButtonPressed;
            }
        }

        private void OnButtonPressed()
        {
            // Check cooldown to prevent rapid cycling
            if (Time.time - _lastInteractionTime < InteractionCooldown)
            {
                return;
            }

            _lastInteractionTime = Time.time;
            CycleColor();
        }

        private void CycleColor()
        {
            if (colorMaterials == null || colorMaterials.Length == 0)
                return;

            // If already transitioning, complete the current transition first
            if (_isTransitioning)
            {
                CompleteTransition();
            }

            // Remember old index before cycling
            int oldColorIndex = _currentColorIndex;

            // Get current emission color before switching (from first renderer)
            if (lanternRenderers != null && lanternRenderers.Length > 0 && lanternRenderers[0] != null)
            {
                Material[] currentMaterials = lanternRenderers[0].materials;
                if (materialIndex < currentMaterials.Length)
                {
                    Material currentMaterial = currentMaterials[materialIndex];
                    _startEmissionColor = GetEmissionColor(currentMaterial);
                }
            }

            // Cycle to next color
            _currentColorIndex = (_currentColorIndex + 1) % colorMaterials.Length;
            
            // Set InfoArea text
            var infoArea = interactionCollider?.GetComponent<InfoArea>();
            if (infoArea != null)
                infoArea.infoType = _currentColorIndex == 0 ? PowerON : PowerOFF;

            // Get target emission color
            if (_currentColorIndex < 0 || _currentColorIndex >= colorMaterials.Length) return;
            _targetMaterial = colorMaterials[_currentColorIndex];
            _targetEmissionColor = GetEmissionColor(_targetMaterial);

            // Determine if we should use a transition based on the toggle settings
            bool wasOff = oldColorIndex == 0;
            bool isNowOff = _currentColorIndex == 0;

            bool shouldTransition = true;
            if (isNowOff && !useDelayedTurnOff)
                shouldTransition = false;
            else if (wasOff && !useDelayedTurnOn)
                shouldTransition = false;

            if (shouldTransition)
            {
                if (wasOff)
                {
                    // Turn-on: swap to the target material immediately so emission is visible,
                    // then lerp from dark to bright
                    SwapMaterialTo(_targetMaterial);
                    _startEmissionColor = Color.black;
                }
                else if (isNowOff)
                {
                    // Turn-off: keep current material, lerp from bright to dark,
                    // CompleteTransition will swap to off material
                }

                // Start transition
                _isTransitioning = true;
                _transitionStartTime = Time.time;
            }
            else
            {
                // Instant change: apply target material and emission color immediately
                CompleteTransition();
            }

            // Save state
            SaveState();

            // Notify listeners of color change
            OnColorChanged?.Invoke(_currentColorIndex);
        }

        private void SwapMaterialTo(Material? material)
        {
            if (material == null || lanternRenderers == null)
                return;

            foreach (Renderer renderer in lanternRenderers)
            {
                if (renderer == null) continue;

                Material[] materials = renderer.materials;
                if (materialIndex >= materials.Length) continue;

                materials[materialIndex] = material;
                renderer.materials = materials;
            }
        }

        private void UpdateMaterial()
        {
            if (lanternRenderers == null || colorMaterials == null)
                return;

            if (_currentColorIndex < 0 || _currentColorIndex >= colorMaterials.Length) return;

            // Update material on all renderers
            foreach (Renderer renderer in lanternRenderers)
            {
                if (renderer == null) continue;

                // Get the materials array, replace the material at the index, and set it back
                Material[] materials = renderer.materials;

                if (materialIndex >= materials.Length) continue;

                materials[materialIndex] = colorMaterials[_currentColorIndex];
                renderer.materials = materials;
            }

            // Update the source light color to match the material's emission color
            UpdateLightColor();
        }

        private void UpdateLightColor()
        {
            if (sourceLight == null || colorMaterials == null)
                return;

            if (_currentColorIndex < 0 || _currentColorIndex >= colorMaterials.Length)
                return;

            Material currentMaterial = colorMaterials[_currentColorIndex];
            if (currentMaterial == null)
                return;

            // Extract emission color from the material
            Color emissionColor = GetEmissionColor(currentMaterial);

            // Set the light color to match the emission color
            sourceLight.color = emissionColor;
        }

        private Color GetEmissionColor(Material material)
        {
            // Try to get the emission color from common emission property names
            if (material.HasProperty("_EmissionColor"))
            {
                return material.GetColor("_EmissionColor");
            }
            else if (material.HasProperty("_EmissiveColor"))
            {
                return material.GetColor("_EmissiveColor");
            }
            else if (material.HasProperty("_Emission"))
            {
                return material.GetColor("_Emission");
            }

            // If no emission property found, return white as fallback
            return Color.white;
        }

        private void SetEmissionColor(Material material, Color color)
        {
            // Try to set the emission color using common emission property names
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color);
            }
            else if (material.HasProperty("_EmissiveColor"))
            {
                material.SetColor("_EmissiveColor", color);
            }
            else if (material.HasProperty("_Emission"))
            {
                material.SetColor("_Emission", color);
            }
        }

        private float GetFlickerMultiplier()
        {
            // Use Perlin noise for smooth, natural-looking flicker
            float perlinValue = Mathf.PerlinNoise(Time.time * FlickerSpeed, _flickerOffset);
            
            // Map Perlin noise (0-1) to flicker intensity range
            return Mathf.Lerp(FlickerIntensityMin, FlickerIntensityMax, perlinValue);
        }

        private void ApplyFlickeringToCurrentState(float flickerMultiplier)
        {
            if (colorMaterials == null || _currentColorIndex < 0 || _currentColorIndex >= colorMaterials.Length)
                return;

            Material baseMaterial = colorMaterials[_currentColorIndex];
            if (baseMaterial == null)
                return;

            // Get the base emission color
            Color baseEmissionColor = GetEmissionColor(baseMaterial);
            Color flickeredColor = baseEmissionColor * flickerMultiplier;

            // Apply flickered emission color to all renderers
            if (lanternRenderers != null)
            {
                foreach (Renderer renderer in lanternRenderers)
                {
                    if (renderer == null) continue;

                    Material[] materials = renderer.materials;
                    if (materialIndex < materials.Length)
                    {
                        Material currentMaterial = materials[materialIndex];
                        SetEmissionColor(currentMaterial, flickeredColor);
                    }
                }
            }

            // Apply flickering to light
            if (sourceLight != null)
            {
                sourceLight.color = flickeredColor;
            }
        }

        private void CompleteTransition()
        {
            // End transition state
            _isTransitioning = false;

            // Apply the target material to all renderers
            if (_targetMaterial != null && lanternRenderers != null)
            {
                foreach (Renderer renderer in lanternRenderers)
                {
                    if (renderer == null) continue;

                    Material[] materials = renderer.materials;
                    if (materialIndex < materials.Length)
                    {
                        materials[materialIndex] = _targetMaterial;
                        renderer.materials = materials;
                    }
                }
            }

            // Update light color to final target
            if (sourceLight != null)
            {
                sourceLight.color = _targetEmissionColor;
            }
        }

        private void LoadState()
        {
            string uniqueKey = GetUniqueKey();

            // Load the persisted color index from the static dictionary
            if (!Main.LanternColorData.TryGetValue(uniqueKey, out int savedColorIndex)) return;
            _currentColorIndex = savedColorIndex;

            // Validate the loaded index
            if (colorMaterials != null && _currentColorIndex >= colorMaterials.Length)
            {
                _currentColorIndex = 0;
            }
        }

        private void SaveState()
        {
            string uniqueKey = GetUniqueKey();

            // Update the static dictionary with the current color index
            Main.LanternColorData[uniqueKey] = _currentColorIndex;
        }
    }
}
