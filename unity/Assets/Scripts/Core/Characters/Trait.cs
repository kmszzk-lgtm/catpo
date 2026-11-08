using System.Collections.Generic;

namespace Catpo.Core
{
    /// <summary>
    /// 특성 하나가 주는 효과. 배율은 1이 기본이고, 여러 특성은 곱해서 합친다.
    /// </summary>
    public sealed class TraitEffects
    {
        public bool CanGoOutside = true;
        public double HomeActionSpeed = 1;      // 집 안 활동 속도
        public double HomeXp = 1;               // 집 안 행동 경험치
        public double OutsideXp = 1;            // 외부 활동 경험치
        public double MedicalAction = 1;        // 의료 행위 효과
        public double StressGeneration = 1;     // 모든 스트레스 생성량
        public double WakeStress = 1;           // 자고 일어날 때 스트레스
        public double HungerRate = 1;
        public double ThirstRate = 1;
        public double StaminaRegen = 1;
        public double StaminaUse = 1;
        public double MoveSpeed = 1;
        public double ActionSpeed = 1;
        public double FoodConsumption = 1;      // 음식을 먹을 때 소비량
        public double MedicalConsumptionWhenInjured = 1;
        public bool HazesStayHome;              // 태움: 집에 있으면 가끔 무작위 1명에게 스트레스
        public double HomeActivityReliefPerHour; // 집에서 하는 모든 활동으로 스트레스가 풀림 (더해서 합침)

        public static TraitEffects Combine(IEnumerable<TraitEffects> effects)
        {
            var r = new TraitEffects();
            foreach (var e in effects)
            {
                r.CanGoOutside &= e.CanGoOutside;
                r.HomeActionSpeed *= e.HomeActionSpeed;
                r.HomeXp *= e.HomeXp;
                r.OutsideXp *= e.OutsideXp;
                r.MedicalAction *= e.MedicalAction;
                r.StressGeneration *= e.StressGeneration;
                r.WakeStress *= e.WakeStress;
                r.HungerRate *= e.HungerRate;
                r.ThirstRate *= e.ThirstRate;
                r.StaminaRegen *= e.StaminaRegen;
                r.StaminaUse *= e.StaminaUse;
                r.MoveSpeed *= e.MoveSpeed;
                r.ActionSpeed *= e.ActionSpeed;
                r.FoodConsumption *= e.FoodConsumption;
                r.MedicalConsumptionWhenInjured *= e.MedicalConsumptionWhenInjured;
                r.HazesStayHome |= e.HazesStayHome;
                r.HomeActivityReliefPerHour += e.HomeActivityReliefPerHour;
            }
            return r;
        }
    }

    public sealed class Trait
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsPositive { get; }
        /// <summary>고유 특성은 조율할 수 없다 (docs/12-difficulty-and-progression.md).</summary>
        public bool IsUnique { get; }
        public TraitEffects Effects { get; }

        public Trait(string id, string name, bool isPositive, bool isUnique, TraitEffects effects)
        {
            Id = id;
            Name = name;
            IsPositive = isPositive;
            IsUnique = isUnique;
            Effects = effects;
        }
    }

    /// <summary>
    /// 캐릭터 고유 특성 (docs/04-characters.md). 효과의 존재는 [확정], 정확한 배율은 대부분 [제안].
    /// </summary>
    public static class TraitCatalog
    {
        public static readonly Trait HomeBody = new Trait("home_body", "외출불가", false, true,
            new TraitEffects { CanGoOutside = false });                                    // [확정]

        public static readonly Trait LifeSkill = new Trait("life_skill", "생활력", true, true,
            new TraitEffects
            {
                HomeActionSpeed = 1.25, HomeXp = 1.25,                                     // 보너스 [확정], 배율 [제안]
                HomeActivityReliefPerHour = Balance.LifeSkillReliefPerHour,                // 집안 모든 활동으로 풀림 [확정]
            });

        public static readonly Trait Drill = new Trait("drill", "얼차려", true, true,
            new TraitEffects { OutsideXp = 1.5, StressGeneration = 0.5 });                 // -50% [확정], 경험치 배율 [제안]

        public static readonly Trait Irregularity = new Trait("irregularity", "부조리", false, true,
            new TraitEffects { HomeXp = 0.5 });                                            // 불이익 [확정], 배율 [제안]

        public static readonly Trait Specialist = new Trait("specialist", "전문의", true, true,
            new TraitEffects { MedicalAction = 1.5 });                                     // 보너스 [확정], 배율 [제안]

        public static readonly Trait Hazing = new Trait("hazing", "태움", false, true,
            new TraitEffects { HomeXp = 0.8, HazesStayHome = true });                      // 조금 [확정], 배율 [제안]

        public static readonly Trait SurvivalInstinct = new Trait("survival_instinct", "생존본능", true, true,
            new TraitEffects
            {
                HungerRate = 0.75, ThirstRate = 0.75,
                StaminaRegen = 1.25, StaminaUse = 0.8,
                MoveSpeed = 1.1, ActionSpeed = 1.1,
            });                                                                            // 효과 [확정], 배율 [제안]

        public static readonly Trait Anxiety = new Trait("anxiety", "불안", false, true,
            new TraitEffects
            {
                WakeStress = 2,                    // 약 두 배 [확정]
                FoodConsumption = 1.2,             // 충동적으로 더 먹음 [확정], 배율 [제안]
                MedicalConsumptionWhenInjured = 1.5,
            });

        public static IReadOnlyList<Trait> All { get; } = new[]
        {
            HomeBody, LifeSkill, Drill, Irregularity, Specialist, Hazing, SurvivalInstinct, Anxiety,
        };
    }
}
