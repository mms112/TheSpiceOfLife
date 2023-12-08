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
    internal static Dictionary<string, int> foodConsumptionCounter = new Dictionary<string, int>();
    internal static Dictionary<string, (float food, float stamina, float eitr)> originalFoodValues = new Dictionary<string, (float, float, float)>();
    internal static Queue<string> foodHistory = new Queue<string>();

    public static void Prefix(Player __instance, ItemDrop.ItemData item)
    {
        // Logic to modify the food item before it's processed by EatFood
        string? foodName = item.m_shared.m_name;
        if (string.IsNullOrWhiteSpace(foodName)) return;
        // Update food history
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
    private static readonly Color DefaultColor = new Color(0.0f, 0.0f, 0.0f, 0.5375f);
    private static readonly Color RedColor = new Color(1f, 0.0f, 0.0f, 0.5375f);
    private static bool isMinimalUILoaded = false;

    static HudUpdateFoodPatch()
    {
        isMinimalUILoaded = Chainloader.PluginInfos.ContainsKey("Azumatt.MinimalUI");
    }

    public static void Prefix(Hud __instance, Player player)
    {
        List<Player.Food> foods = player.GetFoods();
        Transform? muiFoodBarTransform = isMinimalUILoaded ? Hud.instance.m_rootObject.transform.Find("MUI_FoodBar") : null;

        for (int index = 0; index < __instance.m_foodIcons.Length; ++index)
        {
            Image foodIcon = __instance.m_foodIcons[index];
            Image parentImage = foodIcon.transform.parent.GetComponent<Image>();

            if (index < foods.Count)
            {
                Player.Food food = foods[index];
                float diminishingLevel = Util.GetFoodDiminishingLevel(food.m_item.m_shared.m_name);
                Color colorLerped = Color.Lerp(DefaultColor, RedColor, diminishingLevel);

                parentImage.color = colorLerped;

                if (isMinimalUILoaded && Hud.instance.m_foodBarRoot != null)
                {
                    Transform? foodIconTransform = muiFoodBarTransform?.Find($"food{index}");
                    if (foodIconTransform != null)
                    {
                        Image foodIconMinimalUI = foodIconTransform.GetComponent<Image>();
                        foodIconMinimalUI.color = colorLerped;
                    }
                }
            }
            else
            {
                parentImage.color = DefaultColor;
                if (isMinimalUILoaded && Hud.instance.m_foodBarRoot != null)
                {
                    Transform? foodIconTransform = muiFoodBarTransform?.Find($"food{index}");
                    if (foodIconTransform != null)
                    {
                        Image foodIconMinimalUI = foodIconTransform.GetComponent<Image>();
                        foodIconMinimalUI.color = DefaultColor;
                    }
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