using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    public enum SurvivorLocation
    {
        Home,
        Outside,
    }

    /// <summary>
    /// 게임 속에서 살아 움직이는 캐릭터 한 명의 상태.
    /// </summary>
    public sealed class Survivor
    {
        public CharacterDefinition Definition { get; }
        public string Id => Definition.Id;
        public string Name => Definition.Name;

        private readonly List<Trait> traits;
        public IReadOnlyList<Trait> Traits => traits;
        public TraitEffects Effects => TraitEffects.Combine(traits.Select(t => t.Effects));
        public bool CanGoOutside => Effects.CanGoOutside;

        // 몸 상태 (docs/03-infection.md)
        public double Contamination { get; internal set; } = Balance.ContaminationMin;
        public double Infection { get; internal set; } = Balance.InfectionMin;
        public bool IsInfected { get; internal set; }
        public int InfectionBlockDays { get; internal set; }

        // 스트레스 (docs/08-stress.md)
        public double Stress { get; internal set; }

        // 배고픔·목마름 (0 = 괜찮음, 100 = 한계)
        public double Hunger { get; internal set; }
        public double Thirst { get; internal set; }
        /// <summary>충동적으로 더 먹는 양이 쌓인 값. 1이 넘으면 음식 1개를 더 먹는다.</summary>
        internal double ExtraFoodDebt { get; set; }

        // 숙련도
        public SkillSet Skills { get; } = new SkillSet();
        /// <summary>제작 재료 절감이 쌓인 값. 1이 넘으면 재료 1개를 아낀다.</summary>
        internal double MaterialSavingCredit { get; set; }

        /// <summary>사람이 직접 맡은 캐릭터. AI 파견은 사람이 맡지 않은 캐릭터만 할 수 있다 [확정].</summary>
        public bool ControlledByPlayer { get; set; }

        public bool IsDead { get; private set; }
        public string DeathCause { get; private set; }

        // 외출 (docs/02-day-expedition.md)
        public SurvivorLocation Location { get; internal set; } = SurvivorLocation.Home;
        public Direction? OutingDirection { get; internal set; }
        public bool WentOutToday { get; internal set; }
        /// <summary>오늘 늦게 돌아와서, 다음 날 아침에 외출할 수 없게 됨.</summary>
        public bool RestDayPending { get; internal set; }
        /// <summary>오늘은 집에서 쉬어야 함.</summary>
        public bool RestingToday { get; internal set; }

        // AI 파견 중이면 정해진 시각에 저절로 돌아온다
        public bool IsDispatched { get; internal set; }
        internal long DispatchStartMinute { get; set; }
        public long DispatchReturnAtMinute { get; internal set; }
        internal List<DispatchPick> DispatchPlan { get; set; } = new List<DispatchPick>();
        /// <summary>자동 파견 방향. 정해 두면 외출 가능 시간에 알아서 나갔다 온다 [확정]. null이면 자동 파견 안 함.</summary>
        public Direction? AutoDispatchDirection { get; set; }

        // 소지품
        public Inventory Inventory { get; } = new Inventory();
        public double CarryLimitKg { get; set; } = Balance.BaseCarryLimitKg;

        public Survivor(CharacterDefinition definition, IEnumerable<Trait> extraTraits = null)
        {
            Definition = definition;
            traits = definition.UniqueTraits.ToList();
            if (extraTraits != null) traits.AddRange(extraTraits);
            foreach (var kv in definition.StartingSkills) Skills.SetLevel(kv.Key, kv.Value);
        }

        /// <summary>
        /// 경험치를 얻는다. 집 안 행동과 외부 활동에 특성 배율이 따로 붙는다 [확정].
        /// </summary>
        public int GainXp(Skill skill, double amount, bool atHome)
        {
            double multiplier = atHome ? Effects.HomeXp : Effects.OutsideXp;
            return Skills.AddXp(skill, amount * multiplier);
        }

        /// <summary>과적했을 때 들 수 있는 최대 무게. 사람만 과적할 수 있다 [확정].</summary>
        public double OverloadLimitKg => CarryLimitKg * Balance.OverloadFactor;

        public bool IsOverloaded => Inventory.TotalWeightKg > CarryLimitKg + 1e-9;

        /// <summary>
        /// 무게 한도 안에서 줍는다. allowOverload면 과적 한도까지 줍는다 (사람이 직접 수색할 때).
        /// AI는 과적하지 않는다 [확정].
        /// </summary>
        public bool TryPickUp(string itemId, int amount = 1, bool allowOverload = false)
        {
            double added = ItemCatalog.Get(itemId).WeightKg * amount;
            double limit = allowOverload ? OverloadLimitKg : CarryLimitKg;
            if (Inventory.TotalWeightKg + added > limit + 1e-9) return false;
            Inventory.Add(itemId, amount);
            return true;
        }

        internal void Die(string cause)
        {
            if (IsDead) return;
            IsDead = true;
            DeathCause = cause;
        }

        public override string ToString() => $"{Name} (오염 {Contamination:0.#}, 감염 {Infection:0.#}, 스트레스 {Stress:0.#})";
    }
}
