using DV;
using HarmonyLib;

namespace LMSLamps.Game;

public class InteractionTextPatches
{
    [HarmonyPatch(typeof(InteractionText), nameof(InteractionText.GetText))]
    public static class GetTextPatch
    {
        public static bool Prefix(InteractionInfoType infoType, ref string __result)
        {
            if (infoType == Lantern.PowerON)
            {
                __result = $"Press {InteractionText.Instance.BtnUse} to ignite Lantern";
                return false;
            }

            if (infoType == Lantern.PowerOFF)
            {
                __result = $"Press {InteractionText.Instance.BtnUse} to extinguish Lantern";
                return false;           
            }

            return true;
        }
    }
}