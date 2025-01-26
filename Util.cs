using UnityEngine;

namespace TheSpiceOfLife;

public class Util
{
    // Resets the entire food consumption counter
    public static void ResetFoodConsumptionCounter()
    {
        FoodDiminishingReturnsPatch.FoodConsumptionCounter.Clear();
    }

    // Resets the consumption counter for a specific food
    public static void ResetFoodCounter(string foodName)
    {
        if (FoodDiminishingReturnsPatch.FoodConsumptionCounter.ContainsKey(foodName))
        {
            FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName] = 0;
        }
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
    public static float GetFoodDiminishingLevel(string foodName)
    {
        if (FoodDiminishingReturnsPatch.FoodConsumptionCounter.TryGetValue(foodName, out int count))
        {
            // Calculate how far beyond the threshold the item is
            int overThreshold = count - TheSpiceOfLifePlugin.ConsumptionThreshold.Value;

            // Use the diminishingFactor to scale the diminishing level
            float scale = 1f / TheSpiceOfLifePlugin.DiminishingFactor.Value;

            // For instance, 0 means no diminishment, 1 means fully diminished
            // Apply the scale and clamp the result
            return Mathf.Clamp01(overThreshold / scale);
        }

        return 0f;
    }

    public static void ApplyDiminishedFoodBenefits(ItemDrop.ItemData item)
    {
        string foodName = item.m_shared.m_name;

        // Store original values if not already stored
        if (!FoodDiminishingReturnsPatch.OriginalFoodValues.ContainsKey(foodName))
        {
            FoodDiminishingReturnsPatch.OriginalFoodValues[foodName] = (item.m_shared.m_food, item.m_shared.m_foodStamina, item.m_shared.m_foodEitr);
        }

        // Apply diminishing returns
        int consumptionCount = FoodDiminishingReturnsPatch.FoodConsumptionCounter[foodName];
        if (consumptionCount > TheSpiceOfLifePlugin.ConsumptionThreshold.Value)
        {
            item.m_shared.m_food *= TheSpiceOfLifePlugin.DiminishingFactor.Value;
            item.m_shared.m_foodStamina *= TheSpiceOfLifePlugin.DiminishingFactor.Value;
            item.m_shared.m_foodEitr *= TheSpiceOfLifePlugin.DiminishingFactor.Value;
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
        if (FoodDiminishingReturnsPatch.FoodHistory.Count >= TheSpiceOfLifePlugin.HistoryLength.Value)
        {
            string oldestFood = FoodDiminishingReturnsPatch.FoodHistory.Dequeue();
            if (!FoodDiminishingReturnsPatch.FoodHistory.Contains(oldestFood))
            {
                // Reset the counter if the oldest food is no longer in the history
                FoodDiminishingReturnsPatch.FoodConsumptionCounter[oldestFood] = 0;
            }
        }

        FoodDiminishingReturnsPatch.FoodHistory.Enqueue(foodName);
    }

    public static bool ShouldResetBenefits(string foodName)
    {
        // Reset benefits if the food is not in recent history, regardless of its current counter value
        return !FoodDiminishingReturnsPatch.FoodHistory.Contains(foodName);
    }
}