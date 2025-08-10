using System.Collections.Generic;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static TheSpiceOfLife.Util;
using MaikelMod.Managers;
using Newtonsoft.Json;

namespace TheSpiceOfLife;

internal class ModData
{
    public int Version;
    public string FoodConsumptionCounter = "";
    public string FoodHistory = "";
}

[HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
static class FoodDiminishingReturnsPatch
{
    internal static Dictionary<string, int> FoodConsumptionCounter = new();
    internal static Dictionary<string, (float food, float stamina, float eitr)> OriginalFoodValues = new();
    internal static Queue<string> FoodHistory = new();

    private static readonly string[] DiminishingMessages =
    [
        "$spiceoflife_sickofit_1",
        "$spiceoflife_sickofit_2",
        "$spiceoflife_sickofit_3"
    ];


    public static void Prefix(Player __instance, ItemDrop.ItemData item)
    {
        string? foodName = item.m_shared.m_name;
        if (string.IsNullOrWhiteSpace(foodName)) return;

        UpdateFoodHistory(foodName);

        if (FoodConsumptionCounter.ContainsKey(foodName))
        {
            FoodConsumptionCounter[foodName]++;
        }
        else
        {
            FoodConsumptionCounter[foodName] = 1;
        }

        ApplyDiminishedFoodBenefits(item);


        int newCount = FoodConsumptionCounter[foodName];
        int threshold = TheSpiceOfLifePlugin.ConsumptionThreshold.Value;

        if (newCount != threshold) return;
        if (__instance != Player.m_localPlayer) return;

        string message = DiminishingMessages[Random.Range(0, DiminishingMessages.Length)];
        // This displays a bubble above your head, but only for you
        Chat.instance.AddInworldText(
            Player.m_localPlayer.gameObject,
            12345L, // "talker ID" (any unique long; local only, so no conflict)
            Player.m_localPlayer.GetHeadPoint(),
            Talker.Type.Normal,
            UserInfo.GetLocalUser(),
            Localization.instance.Localize(message)
        );
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.RemoveOneFood))]
static class PlayerRemoveOneFoodPatch
{
    static void Prefix(Player __instance)
    {
        if (__instance.m_foods.Count == 0) return;
        UpdateFoodHistory("");
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ClearFood))]
static class PlayerClearFoodPatch
{
    static void Postfix(Player __instance)
    {
        ResetFoodConsumptionCounter();
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
static class HudUpdateFoodPatch
{
    public static Color DefaultColor = new(0.0f, 0.0f, 0.0f, 0.5375f);
    public static Color RedColor = new(1f, 0.0f, 0.0f, 1f);
    //public static Image? ParentImageTemp;
    public static Image? FoodIconMinimalUITemp;

    public static void Prefix(Hud __instance, Player player)
    {
        List<Player.Food> foods = player.GetFoods();
        for (int index = 0; index < __instance.m_foodIcons.Length; ++index)
        {
            Image foodIcon = __instance.m_foodIcons[index];
            foodIcon.transform.parent.TryGetComponent(out Image? parentImage);
            if (index < foods.Count)
            {
                Player.Food food = foods[index];          
                // Get the diminishing level (a value between 0 and 1, for example)
                float diminishingLevel = GetFoodDiminishingLevel(food.m_item.m_shared.m_name);
                Color colorLerped = Color.Lerp(RedColor, DefaultColor, diminishingLevel < 1 ? diminishingLevel - 0.35f : 1f);
                // Apply a color gradient based on the diminishing level
                // Example: No color change at 0, full red tint at 1
                if (parentImage != null)
                {
                    parentImage.color = colorLerped;
                }

                if (!Chainloader.PluginInfos.ContainsKey("Azumatt.MinimalUI") || !Hud.instance || Hud.instance.m_foodBarRoot == null) continue;
                if (Utils.FindChild(Hud.instance.m_rootObject.transform.Find("MUI_FoodBar"), $"food{index}") == null) continue;
                Image? foodIconMinimalUI = Utils.FindChild(Hud.instance.m_rootObject.transform.Find("MUI_FoodBar"), $"food{index}")?.GetComponent<Image>();
                if (foodIconMinimalUI == null) continue;
                FoodIconMinimalUITemp = foodIconMinimalUI;
                foodIconMinimalUI.color = colorLerped;
            }
            else
            {
                if (parentImage != null)
                {
                    parentImage.color = DefaultColor;
                }

                if (FoodIconMinimalUITemp != null)
                {
                    FoodIconMinimalUITemp.color = DefaultColor;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
static class ItemDropItemDataGetTooltipPatch
{
    static void Prefix(ItemDrop.ItemData item, ref string __result)
    {
        if (item.m_shared.m_food > 0 && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) // Check if the item is food
        {
            string foodName = item.m_shared.m_name;
            int consumptionCount = FoodDiminishingReturnsPatch.FoodConsumptionCounter.ContainsKey(foodName) ? FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName] : 0;
            
            if (consumptionCount >= TheSpiceOfLifePlugin.ConsumptionThreshold.Value)
                ApplyDiminishedFoodBenefits(item, 1);
        }
    }

    static void Postfix(ItemDrop.ItemData item, ref string __result)
    {
        if (item.m_shared.m_food > 0 && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) // Check if the item is food
        {
            string foodName = item.m_shared.m_name;
            int consumptionCount = FoodDiminishingReturnsPatch.FoodConsumptionCounter.ContainsKey(foodName) ? FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName] : 0;
            bool isDiminished = IsFoodDiminished(foodName);

            StringBuilder sb = new("\n");
            sb.Append(Localization.instance.Localize($"\n$spiceoflife_consumptioncount: <color=orange>{consumptionCount}</color>\n"));
            sb.Append(Localization.instance.Localize($"$spiceoflife_diminished: <color=orange>{(isDiminished ? "$menu_yes" : "$spiceoflife_no")}</color>"));

            if (isDiminished)
                ApplyDiminishedFoodBenefits(item);

            __result += sb.ToString();
        }
    }
}

[HarmonyPatch]
static class CustomModDataPatch
{
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    [HarmonyPostfix]
    static void ClearModDataOnLogout()
    {
        FoodDiminishingReturnsPatch.FoodConsumptionCounter.Clear();
        FoodDiminishingReturnsPatch.FoodHistory.Clear();
    }

    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
    [HarmonyPostfix]
    static void LoadModData(Player __result)
    {
        if (FoodDiminishingReturnsPatch.FoodConsumptionCounter.Count > 0 || FoodDiminishingReturnsPatch.FoodHistory.Count > 0)
            return;

        string? json = __result.GetComponent<SaveManager>()?.GetModData(TheSpiceOfLifePlugin.ModGUID);

        if (json != null)
        {
            ModData modData = JsonUtility.FromJson<ModData>(json);
            if (modData.Version == 1)
            {
                FoodDiminishingReturnsPatch.FoodConsumptionCounter = JsonConvert.DeserializeObject<Dictionary<string, int>>(modData.FoodConsumptionCounter) ?? FoodDiminishingReturnsPatch.FoodConsumptionCounter;
                FoodDiminishingReturnsPatch.FoodHistory = JsonConvert.DeserializeObject<Queue<string>>(modData.FoodHistory) ?? FoodDiminishingReturnsPatch.FoodHistory;

                foreach (var food in __result.m_foods)
                {
                    string foodName = food.m_item.m_shared.m_name;
                    int consumptionCount = FoodDiminishingReturnsPatch.FoodConsumptionCounter.ContainsKey(foodName) ? FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName] : 0;

                    if (consumptionCount > TheSpiceOfLifePlugin.ConsumptionThreshold.Value)
                        ApplyDiminishedFoodBenefits(food.m_item);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    [HarmonyPrefix]
    static void SaveModData(Player __instance)
    {
        List<string> to_remove = new List<string>();

        foreach (var key in FoodDiminishingReturnsPatch.FoodConsumptionCounter.Keys)
        {
            if (FoodDiminishingReturnsPatch.FoodConsumptionCounter[key] <= 0)
                to_remove.Add(key);
        }

        foreach (var key in to_remove)
        {
            FoodDiminishingReturnsPatch.FoodConsumptionCounter.Remove(key);
        }

        ModData modData = new ModData();
        modData.Version = 1;
        modData.FoodConsumptionCounter = JsonConvert.SerializeObject(FoodDiminishingReturnsPatch.FoodConsumptionCounter, Formatting.None);
        modData.FoodHistory = JsonConvert.SerializeObject(FoodDiminishingReturnsPatch.FoodHistory);
        __instance.GetComponent<SaveManager>()?.RegisterModData(TheSpiceOfLifePlugin.ModGUID, JsonUtility.ToJson(modData, false));
    }
}