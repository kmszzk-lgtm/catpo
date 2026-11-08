using System;
using System.Collections.Generic;

namespace Catpo.Core
{
    /// <summary>숙련도 종류 (docs/04-characters.md의 스킬 초안). 무기·제작 숙련도가 있다는 것은 [확정].</summary>
    public enum Skill
    {
        Melee,
        Shooting,
        Stealth,
        Search,
        Medical,
        Crafting,
    }

    /// <summary>캐릭터 한 명의 숙련도 레벨과 경험치.</summary>
    public sealed class SkillSet
    {
        private readonly Dictionary<Skill, int> levels = new Dictionary<Skill, int>();
        private readonly Dictionary<Skill, double> xp = new Dictionary<Skill, double>();

        public int Level(Skill skill) => levels.TryGetValue(skill, out var l) ? l : 1;

        public double Xp(Skill skill) => xp.TryGetValue(skill, out var x) ? x : 0;

        public void SetLevel(Skill skill, int level)
        {
            levels[skill] = Math.Max(1, Math.Min(Balance.MaxSkillLevel, level));
            xp[skill] = 0;
        }

        /// <summary>다음 레벨까지 필요한 경험치 [제안].</summary>
        public static double XpToNext(int level) => Balance.XpPerLevelStep * (level + 1);

        /// <summary>경험치를 더하고, 오른 레벨 수를 돌려준다.</summary>
        public int AddXp(Skill skill, double amount)
        {
            if (amount <= 0) return 0;
            int level = Level(skill);
            double current = Xp(skill) + amount;
            int gained = 0;
            while (level < Balance.MaxSkillLevel && current >= XpToNext(level))
            {
                current -= XpToNext(level);
                level++;
                gained++;
            }
            if (level >= Balance.MaxSkillLevel) current = 0;
            levels[skill] = level;
            xp[skill] = current;
            return gained;
        }
    }
}
