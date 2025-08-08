using UnityEngine;
using System;
using BepInEx;

namespace TheSpiceOfLife;

public class Util
{
    // Resets the entire food consumption counter
    public static void ResetFoodConsumptionCounter()
    {
        FoodDiminishingReturnsPatch.FoodConsumptionCounter.Clear();
    }

    // Method to check if a food's benefits are diminished
    public static bool IsFoodDiminished(string foodName)
    {
        if (FoodDiminishingReturnsPatch.FoodConsumptionCounter.TryGetValue(foodName, out int count))
        {
            return count >= TheSpiceOfLifePlugin.ConsumptionThreshold.Value;
        }

        return false;
    }

    // Method to get the level of food benefit diminishment
    public static float GetFoodDiminishingLevel(string foodName, int offset = 0)
    {
        if (FoodDiminishingReturnsPatch.FoodConsumptionCounter.TryGetValue(foodName, out int count))
        {
            // Calculate how far beyond the threshold the item is
            int overThreshold = count + offset - TheSpiceOfLifePlugin.ConsumptionThreshold.Value;
            if (overThreshold > 0)
                return (float)Math.Pow(TheSpiceOfLifePlugin.DiminishingFactor.Value, overThreshold);
        }

        return 1f;
    }

    public static void ApplyDiminishedFoodBenefits(ItemDrop.ItemData item, int offset = 0)
    {
        string foodName = item.m_shared.m_name;

        // Store original values if not already stored
        if (!FoodDiminishingReturnsPatch.OriginalFoodValues.ContainsKey(foodName))
        {
            FoodDiminishingReturnsPatch.OriginalFoodValues[foodName] = (item.m_shared.m_food, item.m_shared.m_foodStamina, item.m_shared.m_foodEitr);
        }

        // Apply diminishing returns
        float dim_factor = GetFoodDiminishingLevel(foodName, offset);
        if (dim_factor < 1f)
        {
            var orig_values = FoodDiminishingReturnsPatch.OriginalFoodValues[foodName];
            item.m_shared.m_food = Math.Max((int)(orig_values.food * dim_factor), 1);
            item.m_shared.m_foodStamina = (int)(orig_values.stamina * dim_factor);
            item.m_shared.m_foodEitr = (int)(orig_values.eitr * dim_factor);
        }
    }

    public static void RevertFoodBenefitsToOriginal(ItemDrop.ItemData item)
    {
        string foodName = item.m_shared.m_name;
        if (!FoodDiminishingReturnsPatch.OriginalFoodValues.TryGetValue(foodName, out (float food, float stamina, float eitr) foodValue)) return;
        (float originalFood, float originalStamina, float originalEitr) = foodValue;
        item.m_shared.m_food = originalFood;
        item.m_shared.m_foodStamina = originalStamina;
        item.m_shared.m_foodEitr = originalEitr;
    }

    public static void UpdateFoodHistory(string foodName)
    {
        FoodDiminishingReturnsPatch.FoodHistory.Enqueue(foodName);

        while (FoodDiminishingReturnsPatch.FoodHistory.Count > TheSpiceOfLifePlugin.HistoryLength.Value)
        {
            string oldestFood = FoodDiminishingReturnsPatch.FoodHistory.Dequeue();
            if (!oldestFood.IsNullOrWhiteSpace() && !FoodDiminishingReturnsPatch.FoodHistory.Contains(oldestFood))
            {
                // Reset the counter if the oldest food is no longer in the history
                FoodDiminishingReturnsPatch.FoodConsumptionCounter.Remove(oldestFood);
            }
        }
    }
}