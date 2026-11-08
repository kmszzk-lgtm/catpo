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

        // 소지품
        public Inventory Inventory { get; } = new Inventory();
        public double CarryLimitKg { get; set; } = Balance.BaseCarryLimitKg;

        public Survivor(CharacterDefinition definition, IEnumerable<Trait> extraTraits = null)
        {
            Definition = definition;
            traits = definition.UniqueTraits.ToList();
            if (extraTraits != null) traits.AddRange(extraTraits);
        }

        /// <summary>무게 한도 안에서만 줍는다. 성공하면 true.</summary>
        public bool TryPickUp(string itemId, int amount = 1)
        {
            double added = ItemCatalog.Get(itemId).WeightKg * amount;
            if (Inventory.TotalWeightKg + added > CarryLimitKg + 1e-9) return false;
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
