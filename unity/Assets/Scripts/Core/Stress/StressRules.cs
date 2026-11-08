using System;
using System.Collections.Generic;

namespace Catpo.Core
{
    public enum StressLevel
    {
        Normal,
        Sensitive,
        Limit,
        Breakdown,
    }

    public enum CraftTaste
    {
        Neutral,
        Liked,
        Disliked,
    }

    /// <summary>
    /// 스트레스 규칙 (docs/08-stress.md). 생성량에는 특성 배율(군냥이 얼차려 -50% 등)이 곱해진다.
    /// </summary>
    public static class StressRules
    {
        public static void Add(Survivor s, double amount)
        {
            if (s.IsDead || amount <= 0) return;
            s.Stress = Math.Min(Balance.StressMax, s.Stress + amount * s.Effects.StressGeneration);
        }

        public static void Relieve(Survivor s, double amount)
        {
            if (s.IsDead || amount <= 0) return;
            s.Stress = Math.Max(Balance.StressMin, s.Stress - amount);
        }

        /// <summary>자고 일어날 때마다 쌓인다 [확정]. 길냥이(불안)는 약 두 배 [확정].</summary>
        public static void OnWake(Survivor s) => Add(s, Balance.StressOnWake * s.Effects.WakeStress);

        /// <summary>시간이 지나며 쌓인다. 집 밖은 집보다 두 배 [확정].</summary>
        public static void OnTimePassed(Survivor s, double hours)
        {
            double multiplier = s.Location == SurvivorLocation.Outside ? Balance.OutsideStressMultiplier : 1;
            Add(s, Balance.StressPerHomeHour * multiplier * hours);
        }

        public static void OnInjury(Survivor s, double severity) => Add(s, Balance.StressPerInjurySeverity * severity);

        public static void OnCompanionDeath(Survivor s) => Add(s, Balance.StressOnCompanionDeath);

        public static void OnHazed(Survivor s) => Add(s, Balance.HazingStress);

        /// <summary>취향에 맞는 제작은 풀리고, 안 맞는 제작은 쌓인다 [확정].</summary>
        public static void OnCraft(Survivor s, CraftTaste taste)
        {
            if (taste == CraftTaste.Liked) Relieve(s, Balance.ReliefLikedCraft);
            else if (taste == CraftTaste.Disliked) Add(s, Balance.StressDislikedCraft);
        }

        /// <summary>화투·트럼프 카드 같은 취미 물건은 둘 이상이 함께해야 한다 [확정]. 혼자면 아무 일도 없다.</summary>
        public static bool PlayHobby(IReadOnlyCollection<Survivor> participants, double hours)
        {
            int alive = 0;
            foreach (var p in participants) if (!p.IsDead) alive++;
            if (alive < 2) return false;
            foreach (var p in participants) Relieve(p, Balance.ReliefHobbyPerHour * hours);
            return true;
        }

        /// <summary>동료가 고가치 아이템이나 부족한 자원을 가져오면 집에 있는 모두의 스트레스가 풀린다 [확정].</summary>
        public static void OnDelivery(IEnumerable<Survivor> atHome, bool highValueItem, bool scarceResource)
        {
            double amount = (highValueItem ? Balance.ReliefHighValueItem : 0) + (scarceResource ? Balance.ReliefScarceResource : 0);
            foreach (var s in atHome) Relieve(s, amount);
        }

        public static StressLevel LevelOf(double stress)
        {
            if (stress >= Balance.StressMax) return StressLevel.Breakdown;
            if (stress >= Balance.StressLimitThreshold) return StressLevel.Limit;
            if (stress >= Balance.StressSensitiveThreshold) return StressLevel.Sensitive;
            return StressLevel.Normal;
        }

        // 높을 때 효과 [확정]: 식량 소모 증가, 행동·제작 속도 감소, 더 빨리 피로. 배율은 [제안].

        public static double ActionSpeedMultiplier(double stress)
        {
            switch (LevelOf(stress))
            {
                case StressLevel.Sensitive: return 0.9;
                case StressLevel.Limit: return 0.75;
                case StressLevel.Breakdown: return 0;
                default: return 1;
            }
        }

        public static double FoodConsumptionMultiplier(double stress)
        {
            switch (LevelOf(stress))
            {
                case StressLevel.Sensitive: return 1.1;
                case StressLevel.Limit:
                case StressLevel.Breakdown: return 1.25;
                default: return 1;
            }
        }

        public static double FatigueMultiplier(double stress)
        {
            return LevelOf(stress) >= StressLevel.Limit ? 1.25 : 1;
        }
    }
}
