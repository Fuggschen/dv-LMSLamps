using UnityEngine;

namespace LMSLamps.Game
{
    public class GlareMaterialGrabber : MonoBehaviour
    {
        // Configuration from proxy
        public Transform? glare;

        // Runtime state
        private Renderer? _glareRenderer;
        private Material? _glareMaterial;
        private Lantern? _lantern;
        private int _lastColorIndex = -1;

        // Flickering state
        private float _flickerOffset;
        private const float FlickerSpeed = 2f;
        private const float FlickerIntensityMin = 0.8f;
        private const float FlickerIntensityMax = 1.0f;

        // Cached glare source
        private static Renderer? s_glareSource;
        private static Renderer GlareSource
        {
            get
            {
                if (s_glareSource == null)
                {
                    DV.Globals.G.Types.TryGetLivery("LocoDE2", out var de2);
                    s_glareSource = de2.interiorPrefab.transform
                        .Find("DashCluster/HeadlightsFront/L_Headlights/glare")
                        .GetComponent<Renderer>();
                }
                return s_glareSource;
            }
        }

        private static Material? s_glareMat;
        private static Material GlareMat
        {
            get
            {
                if (s_glareMat == null)
                {
                    s_glareMat = new Material(GlareSource.sharedMaterial);
                    s_glareMat.SetFloat("_FadeoutPower", 2.2f);
                    s_glareMat.SetFloat("_LightAtten", 0.7f);
                    s_glareMat.SetFloat("_MaxAtten", 0.8f);
                }
                return s_glareMat;
            }
        }

        public void Start()
        {
            try
            {
                // Find the Lantern component on this GameObject
                _lantern = GetComponent<Lantern>();
                if (_lantern == null)
                {
                    Main.Warning($"No Lantern component found on {gameObject.name}, glare will not update");
                }

                // Initialize flicker offset with a random value for variety
                _flickerOffset = Random.Range(0f, 100f);

                if (glare != null)
                {
                    CreateGlare();
                }
                else
                {
                    Main.Warning($"No glare transform assigned on {gameObject.name}");
                }
            }
            catch (System.Exception ex)
            {
                Main.Error($"Failed to apply glare on {gameObject.name}: {ex}");
            }
        }

        private void CreateGlare()
        {
            if (glare == null) return;

            // Instantiate the glare renderer from the DE2 headlight as a child of the glare transform
            var glareInstance = Instantiate(GlareSource, glare);
            glareInstance.transform.localPosition = Vector3.zero;
            glareInstance.transform.localRotation = Quaternion.identity;
            glareInstance.transform.localScale = Vector3.one;
            glareInstance.gameObject.SetActive(false);

            // Create our own material instance
            _glareMaterial = new Material(GlareMat);
            glareInstance.sharedMaterial = _glareMaterial;

            _glareRenderer = glareInstance;

            // Set initial tint color
            UpdateGlareTintColor();
        }

        public void Update()
        {
            if (_lantern == null || _glareRenderer == null || _glareMaterial == null)
                return;

            int currentColorIndex = _lantern.GetColorIndex();

            // Handle color change
            if (currentColorIndex != _lastColorIndex)
            {
                _lastColorIndex = currentColorIndex;

                if (currentColorIndex >= 1)
                {
                    // Turn on glare
                    _glareRenderer.gameObject.SetActive(true);
                    UpdateGlareTintColor();
                }
                else
                {
                    // Turn off glare
                    _glareRenderer.gameObject.SetActive(false);
                }
            }

            // Apply flickering when light is on
            if (currentColorIndex >= 1)
            {
                float flickerMultiplier = 1f;
                if (_lantern.useFlicker)
                {
                    flickerMultiplier = GetFlickerMultiplier();
                }

                Color baseColor = GetLanternEmissionColor();
                Color flickeredColor = baseColor * flickerMultiplier;

                if (_glareMaterial.HasProperty("_TintColor"))
                {
                    _glareMaterial.SetColor("_TintColor", flickeredColor);
                }
            }
        }

        private void UpdateGlareTintColor()
        {
            if (_glareMaterial == null || _lantern == null)
                return;

            Color emissionColor = _lantern.GetColorIndex() == 0
                ? Color.clear
                : GetLanternEmissionColor();

            if (_glareMaterial.HasProperty("_TintColor"))
            {
                _glareMaterial.SetColor("_TintColor", emissionColor);
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

            if (currentMaterial.HasProperty("_EmissionColor"))
                return currentMaterial.GetColor("_EmissionColor");
            else if (currentMaterial.HasProperty("_EmissiveColor"))
                return currentMaterial.GetColor("_EmissiveColor");
            else if (currentMaterial.HasProperty("_Emission"))
                return currentMaterial.GetColor("_Emission");

            return Color.white;
        }

        private float GetFlickerMultiplier()
        {
            float perlinValue = Mathf.PerlinNoise(Time.time * FlickerSpeed, _flickerOffset);
            return Mathf.Lerp(FlickerIntensityMin, FlickerIntensityMax, perlinValue);
        }
    }
}