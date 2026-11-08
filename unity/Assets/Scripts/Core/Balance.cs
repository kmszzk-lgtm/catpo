namespace Catpo.Core
{
    /// <summary>
    /// 밸런스 수치를 한곳에 모은다.
    /// [확정]은 기획자가 정한 값, [제안]은 기획 문서(docs/)의 제안 값이다. 제안 값은 플레이 테스트로 조정한다.
    /// </summary>
    public static class Balance
    {
        // ── 시간 (docs/07-time-and-seasons.md) ─────────────────────────
        public const int MinutesPerDay = 24 * 60;
        public const int DaysPerSeason = 30;                 // [확정]
        public const int SeasonsPerYear = 4;                 // [확정]
        public const double RealSecondsPerGameHour = 180;    // [제안] 게임 1시간 = 실제 약 3분
        public const double ZombieSpeedAtNoon = 0.6;         // [제안] 12:00에 가장 느림 [확정]
        public const double ZombieSpeedAtMidnight = 1.4;     // [제안] 00:00에 가장 빠름 [확정]

        // ── 오염도 (docs/03-infection.md) ──────────────────────────────
        public const double ContaminationMin = 1;            // [확정] 최저 1
        public const double ContaminationMax = 100;
        public const double InfectionThreshold = 50;         // [확정] 50 이상이면 감염 상태
        public const double ContaminationPerOutsideHour = 4; // [제안]
        public const double RestDayContamination = 41;       // [제안] 귀환 때 이 값을 넘으면 다음 날 외출 불가
        public const double ScratchContamination = 4;        // [제안]
        public const double PurifierReduction = 20;          // [제안]
        public const double RegionBossContaminationPerDay = 5; // [확정] 지역 보스가 있는 지역

        // ── 감염도 ─────────────────────────────────────────────────────
        public const double InfectionMin = 0;                // [확정] 최저 0
        public const double InfectionMax = 100;              // [확정] 100이면 완전 변이
        public const double InfectionBaseDailyRise = 5;      // [확정]
        public const double InfectionRisePer10Contamination = 1; // [확정] 오염도 10마다 하루 +1
        public const double SuppressantReduction = 10;       // [확정]
        public const int SuppressantBlockDays = 3;           // [확정]
        public const double CureContaminationCap = 10;       // [확정]

        // ── 스트레스 (docs/08-stress.md) ───────────────────────────────
        public const double StressMin = 0;
        public const double StressMax = 100;
        public const double StressOnWake = 5;                // [제안] 자고 일어날 때마다 쌓임 [확정]
        public const double StressPerHomeHour = 0.25;        // [제안]
        public const double OutsideStressMultiplier = 2;     // [확정] 집 밖은 두 배
        public const double StressPerInjurySeverity = 10;    // [제안]
        public const double StressOnCompanionDeath = 40;     // [제안] 아주 많이 [확정]
        public const double HazingStress = 2;                // [제안] 태움 1회
        public const double HazingIntervalHours = 3;         // [제안] 태움 주기
        public const double ReliefHighValueItem = 10;        // [제안]
        public const double ReliefScarceResource = 15;       // [제안]
        public const double ReliefHobbyPerHour = 8;          // [제안] 둘 이상 함께 [확정]
        public const double ReliefLikedCraft = 5;            // [제안]
        public const double StressDislikedCraft = 5;         // [제안]
        public const double StressSensitiveThreshold = 40;   // [제안] 예민
        public const double StressLimitThreshold = 70;       // [제안] 한계

        // ── 소지품 (docs/10-controls-and-items.md) ─────────────────────
        public const double BaseCarryLimitKg = 12;           // [제안] 맨몸 기준
    }
}
