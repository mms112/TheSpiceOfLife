using System.Collections.Generic;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static TheSpiceOfLife.Util;

namespace TheSpiceOfLife;

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

        // Restore original food benefits if the diminishing returns no longer apply
        if (ShouldResetBenefits(foodName))
        {
            RevertFoodBenefitsToOriginal(item);
        }

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
    static void Postfix(Player __instance)
    {
        ResetFoodConsumptionCounter();
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
    public static Image? ParentImageTemp;
    public static Image? FoodIconMinimalUITemp;

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
                float diminishingLevel = GetFoodDiminishingLevel(food.m_item.m_shared.m_name);
                Color colorLerped = Color.Lerp(DefaultColor, RedColor, diminishingLevel);
                // Apply a color gradient based on the diminishing level
                // Example: No color change at 0, full red tint at 1
                if (parentImage != null)
                {
                    ParentImageTemp = parentImage;
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
                if (ParentImageTemp != null)
                {
                    ParentImageTemp.color = DefaultColor;
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
    static void Postfix(ItemDrop.ItemData item, ref string __result)
    {
        if (item.m_shared.m_food > 0) // Check if the item is food
        {
            string foodName = item.m_shared.m_name;
            int consumptionCount = FoodDiminishingReturnsPatch.FoodConsumptionCounter.ContainsKey(foodName) ? FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName] : 0;
            bool isDiminished = IsFoodDiminished(foodName);

            StringBuilder sb = new("\n");
            sb.Append(Localization.instance.Localize($"\n$spiceoflife_consumptioncount: <color=orange>{consumptionCount}</color>\n"));
            sb.Append(Localization.instance.Localize($"$spiceoflife_diminished: <color=orange>{(isDiminished ? "$menu_yes" : "$spiceoflife_no")}</color>"));

            __result += sb.ToString();
        }
    }
}