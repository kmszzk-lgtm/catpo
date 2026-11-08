using System;

namespace Catpo.Core
{
    /// <summary>
    /// 오염도와 감염도 규칙 (docs/03-infection.md).
    /// 오염도: 최저 1, 바깥에 있는 시간만큼 오름, 50 이상이면 감염, 정화제로만 낮춤.
    /// 감염도: 최저 0, 감염 상태에서 하루 (5 + 오염도 10마다 1)씩 오름, 100이면 완전 변이.
    /// </summary>
    public static class InfectionRules
    {
        public static void AddContamination(Survivor s, double amount)
        {
            if (s.IsDead || amount <= 0) return;
            s.Contamination = Clamp(s.Contamination + amount, Balance.ContaminationMin, Balance.ContaminationMax);
            if (s.Contamination >= Balance.InfectionThreshold) BecomeInfected(s);
        }

        /// <summary>바깥에 있던 시간만큼 오염도가 오른다. 밤낮은 상관없다 [확정].</summary>
        public static void ExposeOutside(Survivor s, double hours)
        {
            AddContamination(s, hours * Balance.ContaminationPerOutsideHour);
        }

        public static void Scratch(Survivor s) => AddContamination(s, Balance.ScratchContamination);

        /// <summary>좀비에게 물리면 바로 감염 상태 [확정].</summary>
        public static void Bite(Survivor s)
        {
            if (s.IsDead) return;
            BecomeInfected(s);
        }

        /// <summary>
        /// 정화제: 오염도만 낮춘다. 감염 상태에서도 쓸 수 있고, 50 아래로 내려도 감염 상태는 남는다 [확정].
        /// </summary>
        public static void ApplyPurifier(Survivor s, double effectMultiplier = 1)
        {
            if (s.IsDead) return;
            s.Contamination = Math.Max(Balance.ContaminationMin, s.Contamination - Balance.PurifierReduction * effectMultiplier);
        }

        /// <summary>
        /// 억제제: 감염도 -10, 3일 동안 감염도 상승을 막는다(오염도가 높아도). 0까지 내려도 감염 상태는 남는다 [확정].
        /// 다시 쓰면 막는 기간을 3일로 새로 맞춘다 [제안].
        /// </summary>
        public static void ApplySuppressant(Survivor s)
        {
            if (s.IsDead) return;
            s.Infection = Math.Max(Balance.InfectionMin, s.Infection - Balance.SuppressantReduction);
            s.InfectionBlockDays = Balance.SuppressantBlockDays;
        }

        /// <summary>완치제: 감염 상태를 없애고, 오염도가 10보다 높으면 10까지 낮춘다 [확정].</summary>
        public static void ApplyCure(Survivor s)
        {
            if (s.IsDead) return;
            s.IsInfected = false;
            s.Infection = Balance.InfectionMin;
            s.InfectionBlockDays = 0;
            if (s.Contamination > Balance.CureContaminationCap) s.Contamination = Balance.CureContaminationCap;
        }

        public static double DailyInfectionRise(double contamination)
        {
            return Balance.InfectionBaseDailyRise + Math.Floor(contamination / 10) * Balance.InfectionRisePer10Contamination;
        }

        /// <summary>
        /// 하루가 지날 때(자정) 한 번 부른다.
        /// regionExtraContamination: 지역 보스가 있으면 하루 +5 [확정]. 오늘 새로 감염된 캐릭터는 다음 날부터 감염도가 오른다.
        /// </summary>
        public static void DailyTick(Survivor s, double regionExtraContamination)
        {
            if (s.IsDead) return;
            bool wasInfected = s.IsInfected;
            AddContamination(s, regionExtraContamination);
            if (!wasInfected) return;

            if (s.InfectionBlockDays > 0)
            {
                s.InfectionBlockDays--;
                return;
            }

            s.Infection = Math.Min(Balance.InfectionMax, s.Infection + DailyInfectionRise(s.Contamination));
            if (s.Infection >= Balance.InfectionMax) s.Die("완전 변이");
        }

        /// <summary>귀환할 때 오염도가 기준을 넘었으면 다음 날 외출 불가 [제안 수치].</summary>
        public static bool NeedsRestAfterReturn(Survivor s) => s.Contamination > Balance.RestDayContamination;

        private static void BecomeInfected(Survivor s)
        {
            if (s.IsInfected) return;
            s.IsInfected = true;
            s.Infection = Balance.InfectionMin;
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
    }
}
