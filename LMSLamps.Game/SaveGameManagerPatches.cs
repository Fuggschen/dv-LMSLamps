using System.Collections.Generic;
using System.Linq;
using DV.JObjectExtstensions;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace LMSLamps.Game
{
    [HarmonyPatch(typeof(SaveGameManager))]
    internal class SaveGameManagerPatches
    {
        [HarmonyPatch("DoSaveIO"), HarmonyPrefix]
        public static void InjectSaveData(SaveGameData data)
        {
            var loadedData = data.GetJObject("LMSLamps");
            loadedData ??= new JObject();

            Main.LanternColorData.Clear();

            // Collect all lantern color states from active train cars
            // Need to search through all GameObjects to find Lantern components
            var allLanterns = UnityEngine.Object.FindObjectsOfType<Lantern>();
            foreach (var lantern in allLanterns)
            {
                string uniqueKey = lantern.GetUniqueKey();
                if (!string.IsNullOrEmpty(uniqueKey))
                {
                    // Store the color index using unique key for each lantern
                    Main.LanternColorData[uniqueKey] = lantern.GetColorIndex();
                }
            }

            loadedData.SetObjectViaJSON("data", new DataHolder(Main.LanternColorData));
            data.SetJObject("LMSLamps", loadedData);

            Main.Log($"Saved lantern data for {Main.LanternColorData.Count} lantern(s), found {allLanterns.Length} lantern(s) total");
        }

        [HarmonyPatch(nameof(SaveGameManager.FindStartGameData)), HarmonyPostfix]
        public static void ExtractSaveData(SaveGameManager __instance)
        {
            if (__instance.data == null) return;

            var data = __instance.data.GetJObject("LMSLamps");
            if (data == null) return;

            var holder = data.GetObjectViaJSON<DataHolder>("data");
            if (holder == null) return;

            Main.LanternColorData = holder.ToData();

            Main.Log($"Loaded lantern data for {Main.LanternColorData.Count} car(s)");
        }

        private class DataHolder
        {
            public string[] Keys;
            public int[] Values;

            public DataHolder()
            {
                Keys = new string[0];
                Values = new int[0];
            }

            public DataHolder(Dictionary<string, int> data)
            {
                Keys = data.Keys.ToArray();
                Values = data.Values.ToArray();
            }

            public Dictionary<string, int> ToData()
            {
                Dictionary<string, int> data = new Dictionary<string, int>();
                int length = Keys.Length;
                for (int i = 0; i < length; i++)
                {
                    data.Add(Keys[i], Values[i]);
                }

                return data;
            }
        }
    }
}
