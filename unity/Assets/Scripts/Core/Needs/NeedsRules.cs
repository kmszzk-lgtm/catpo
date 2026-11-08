using System;

namespace Catpo.Core
{
    public enum ConsumeResult
    {
        Ok,
        Dead,
        NotEdible,
        NeedsBoiling,
        NotAvailable,
    }

    /// <summary>
    /// 배고픔·목마름. 감염도가 높을수록 더 빨리 온다 [확정]. 길냥이(생존본능)는 덜 온다 [확정].
    /// </summary>
    public static class NeedsRules
    {
        public static double InfectionMultiplier(Survivor s) => 1 + s.Infection * Balance.NeedsPerInfectionPoint;

        public static void OnTimePassed(Survivor s, double hours)
        {
            if (s.IsDead || hours <= 0) return;
            double m = InfectionMultiplier(s);
            s.Hunger = Math.Min(Balance.NeedMax, s.Hunger + Balance.HungerPerHour * s.Effects.HungerRate * m * hours);
            s.Thirst = Math.Min(Balance.NeedMax, s.Thirst + Balance.ThirstPerHour * s.Effects.ThirstRate * m * hours);
            if (s.Hunger >= Balance.NeedStressThreshold || s.Thirst >= Balance.NeedStressThreshold)
                StressRules.Add(s, Balance.NeedStressPerHour * hours);
        }

        /// <summary>
        /// 먹거나 마신다. 집에서는 공용 창고, 밖에서는 인벤토리를 source로 넘긴다.
        /// 음식은 불안(길냥이)이나 높은 스트레스 때문에 충동적으로 더 먹는다 [확정]. 그 몫을 쌓아 두었다가 1개가 차면 하나를 더 쓴다.
        /// </summary>
        public static ConsumeResult Consume(Survivor s, Inventory source, string itemId)
        {
            if (s.IsDead) return ConsumeResult.Dead;
            var item = ItemCatalog.Get(itemId);
            if (item == ItemCatalog.RainWater) return ConsumeResult.NeedsBoiling;
            if (!item.IsEdible) return ConsumeResult.NotEdible;
            if (!source.Remove(itemId)) return ConsumeResult.NotAvailable;

            s.Hunger = Math.Max(0, s.Hunger - item.HungerRelief);
            s.Thirst = Math.Max(0, s.Thirst - item.ThirstRelief);

            if (item.Category == ItemCategory.Food)
            {
                double extra = s.Effects.FoodConsumption * StressRules.FoodConsumptionMultiplier(s.Stress) - 1;
                if (extra > 0)
                {
                    s.ExtraFoodDebt += extra;
                    if (s.ExtraFoodDebt >= 1 - 1e-9 && source.Remove(itemId)) s.ExtraFoodDebt -= 1;
                }
            }
            return ConsumeResult.Ok;
        }
    }
}
