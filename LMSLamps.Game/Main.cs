using System.Collections.Generic;
using System.Reflection;
using custom_item_components;
using custom_item_mod;
using HarmonyLib;
using UnityModManagerNet;
using LMSLamps.Unity;
using UnityEngine;

namespace LMSLamps.Game
{
    public static class Main
    {
        private static UnityModManager.ModEntry Instance { get; set; } = null!;
        public static Dictionary<string, int> LanternColorData { get; set; } = new Dictionary<string, int>();

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Instance = modEntry;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            RegisterGadgets();
            return true;
        }

        private static void RegisterGadgets()
        {
            // Register the lantern gadget
            try
            {
                CustomGadgetBaseMap.RegisterGadgetImplementation(
                    typeof(LanternProxy),
                    typeof(Lantern),
                    (GadgetBase source, ref DV.Customization.Gadgets.GadgetBase target) =>
                    {
                        var replacement = target as Lantern;
                        if (replacement != null)
                        {
                            CopyLanternProxyFields(source, replacement);
                        }
                    }
                );
            }
            catch (System.Exception ex)
            {
                Error($"Failed to register lantern gadget: {ex}");
            }
        }

        private static void CopyLanternProxyFields(GadgetBase proxy, Lantern replacement)
        {
            try
            {
                var proxyType = proxy.GetType();
                replacement.requirements.trainCarPresence = DV.Customization.TrainCarCustomization
                    .TrainCarCustomizerBase
                    .CustomizerTrainCarRequirements.RequireTrainCar;

                // Copy offMaterial
                var offMaterialField = proxyType.GetField("offMaterial");
                if (offMaterialField != null)
                {
                    replacement.offMaterial = offMaterialField.GetValue(proxy) as UnityEngine.Material;
                }

                // Copy colorMaterials array
                var colorMaterialsField = proxyType.GetField("colorMaterials");
                if (colorMaterialsField != null)
                {
                    replacement.colorMaterials = colorMaterialsField.GetValue(proxy) as UnityEngine.Material[];
                }

                // Copy lanternRenderers array
                var lanternRenderersField = proxyType.GetField("lanternRenderers");
                if (lanternRenderersField != null)
                {
                    replacement.lanternRenderers = lanternRenderersField.GetValue(proxy) as UnityEngine.Renderer[];
                }

                // Copy materialIndex
                var materialIndexField = proxyType.GetField("materialIndex");
                if (materialIndexField != null)
                {
                    replacement.materialIndex = (int)materialIndexField.GetValue(proxy);
                }

                // Copy interactionCollider
                var interactionColliderField = proxyType.GetField("interactionCollider");
                if (interactionColliderField != null)
                {
                    replacement.interactionCollider = interactionColliderField.GetValue(proxy) as UnityEngine.GameObject;
                }

                // Copy sourceLight
                var sourceLightField = proxyType.GetField("sourceLight");
                if (sourceLightField != null)
                {
                    replacement.sourceLight = sourceLightField.GetValue(proxy) as UnityEngine.Light;
                }

                // Copy behavior toggles
                var useFlickerField = proxyType.GetField("useFlicker");
                if (useFlickerField != null)
                {
                    replacement.useFlicker = (bool)useFlickerField.GetValue(proxy);
                }

                var useDelayedTurnOnField = proxyType.GetField("useDelayedTurnOn");
                if (useDelayedTurnOnField != null)
                {
                    replacement.useDelayedTurnOn = (bool)useDelayedTurnOnField.GetValue(proxy);
                }

                var useDelayedTurnOffField = proxyType.GetField("useDelayedTurnOff");
                if (useDelayedTurnOffField != null)
                {
                    replacement.useDelayedTurnOff = (bool)useDelayedTurnOffField.GetValue(proxy);
                }
            }
            catch (System.Exception ex)
            {
                Error($"Failed to copy lantern proxy fields: {ex}");
            }
        }

        internal static void Log(string message)
        {
            Instance.Logger.Log(message);
        }

        internal static void Warning(string message)
        {
            Instance.Logger.Warning(message);
        }

        internal static void Error(string message)
        {
            Instance.Logger.Error(message);
        }
    }

    /// <summary>
    /// Harmony patch to replace glare material grabber proxy components when MonoBehaviour awakens
    /// </summary>
    [HarmonyPatch(typeof(GlareMaterialGrabberProxy), "Awake")]
    public static class GlareMaterialGrabberProxyReplacementPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(GlareMaterialGrabberProxy __instance)
        {
            try
            {
                // Create the replacement component
                var replacement = __instance.gameObject.AddComponent<GlareMaterialGrabber>();

                // Copy glare transform from proxy to replacement
                replacement.glare = __instance.glare;

                // Destroy the proxy component
                Object.Destroy(__instance);

                // Prevent the original Awake from running
                return false;
            }
            catch (System.Exception ex)
            {
                Main.Error($"Failed to replace glare material grabber proxy: {ex}");
                return true; // Let original Awake run if we fail
            }
        }
    }
}
