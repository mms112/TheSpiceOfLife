using System.Collections.Generic;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TheSpiceOfLife;

[HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
static class FoodDiminishingReturnsPatch
{
    internal static Dictionary<string, int> foodConsumptionCounter = new();
    internal static Dictionary<string, (float food, float stamina, float eitr)> originalFoodValues = new();
    internal static Queue<string> foodHistory = new();

    public static void Prefix(Player __instance, ItemDrop.ItemData item)
    {
        string? foodName = item.m_shared.m_name;
        if (string.IsNullOrWhiteSpace(foodName)) return;

        Util.UpdateFoodHistory(foodName);

        // Restore original food benefits if the diminishing returns no longer apply
        if (Util.ShouldResetBenefits(foodName))
        {
            Util.RevertFoodBenefitsToOriginal(item);
        }

        if (foodConsumptionCounter.ContainsKey(foodName))
        {
            foodConsumptionCounter[foodName]++;
        }
        else
        {
            foodConsumptionCounter[foodName] = 1;
        }

        Util.ApplyDiminishedFoodBenefits(item);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.RemoveOneFood))]
static class PlayerRemoveOneFoodPatch
{
    static void Postfix(Player __instance)
    {
        Util.ResetFoodConsumptionCounter();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ClearFood))]
static class PlayerClearFoodPatch
{
    static void Postfix(Player __instance)
    {
        Util.ResetFoodConsumptionCounter();
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
static class HudUpdateFoodPatch
{
    public static Color defaultColor = new(0.0f, 0.0f, 0.0f, 0.5375f);
    public static Color redColor = new(1f, 0.0f, 0.0f, 1f);
    public static Image? parentImageTemp;
    public static Image? foodIconMinimalUITemp;

    public static void Prefix(Hud __instance, Player player)
    {
        List<Player.Food> foods = player.GetFoods();
        for (int index = 0; index < __instance.m_foodIcons.Length; ++index)
        {
            Image foodIcon = __instance.m_foodIcons[index];
            if (index < foods.Count)
            {
                Player.Food food = foods[index];
                foodIcon.transform.parent.TryGetComponent(out Image? parentImage);
                // Get the diminishing level (a value between 0 and 1, for example)
                float diminishingLevel = Util.GetFoodDiminishingLevel(food.m_item.m_shared.m_name);
                var colorLerped = Color.Lerp(defaultColor, redColor, diminishingLevel);
                // Apply a color gradient based on the diminishing level
                // Example: No color change at 0, full red tint at 1
                if (parentImage != null)
                {
                    parentImageTemp = parentImage;
                    parentImage.color = colorLerped;
                }

                if (!Chainloader.PluginInfos.ContainsKey("Azumatt.MinimalUI") || !Hud.instance || Hud.instance.m_foodBarRoot == null) continue;
                if (Utils.FindChild(Hud.instance.m_rootObject.transform.Find("MUI_FoodBar"), $"food{index}") == null) continue;
                Image? foodIconMinimalUI = Utils.FindChild(Hud.instance.m_rootObject.transform.Find("MUI_FoodBar"), $"food{index}")?.GetComponent<Image>();
                if (foodIconMinimalUI == null) continue;
                foodIconMinimalUITemp = foodIconMinimalUI;
                foodIconMinimalUI.color = colorLerped;
            }
            else
            {
                if (parentImageTemp != null)
                {
                    parentImageTemp.color = defaultColor;
                }

                if (foodIconMinimalUITemp != null)
                {
                    foodIconMinimalUITemp.color = defaultColor;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
static class ItemDropItemDataGetTooltipPatch
{
    static void Postfix(ItemDrop.ItemData item, ref string __result)
    {
        if (item.m_shared.m_food > 0) // Check if the item is food
        {
            string foodName = item.m_shared.m_name;
            int consumptionCount = FoodDiminishingReturnsPatch.foodConsumptionCounter.ContainsKey(foodName) ? FoodDiminishingReturnsPatch.foodConsumptionCounter[foodName] : 0;
            bool isDiminished = Util.IsFoodDiminished(foodName);

            StringBuilder sb = new("\n");
            sb.Append(Localization.instance.Localize($"\nConsumption Count: <color=orange>{consumptionCount}</color>\n"));
            sb.Append(Localization.instance.Localize($"Benefits Diminished: <color=orange>{(isDiminished ? "Yes" : "No")}</color>"));

            __result += sb.ToString();
        }
    }
}