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

        // 오염도·감염도가 스트레스와 배고픔·목마름에 주는 영향 (Q87) ─────
        // 오염도의 영향은 스트레스뿐 [확정]. 감염도는 배고픔·목마름과 스트레스를 늘림 [확정].
        public const double StressPerContaminationPoint = 0.01; // [제안] 배율 = 1 + (오염도-1)/100
        public const double StressPerInfectionPoint = 0.01;     // [제안] 배율 = 1 + 감염도/100
        public const double NeedsPerInfectionPoint = 0.01;      // [제안] 배율 = 1 + 감염도/100

        // ── 소지품 (docs/10-controls-and-items.md) ─────────────────────
        public const double BaseCarryLimitKg = 12;           // [제안] 맨몸 기준

        // ── 배고픔·목마름 (0 = 괜찮음, 100 = 한계) ──────────────────────
        public const double NeedMax = 100;
        public const double HungerPerHour = 1.2;             // [제안] 약 3.5일이면 한계
        public const double ThirstPerHour = 1.8;             // [제안] 약 2.3일이면 한계
        public const double NeedStressThreshold = 80;        // [제안] 이 이상이면 스트레스가 쌓임
        public const double NeedStressPerHour = 1;           // [제안]

        // ── 숙련도 ─────────────────────────────────────────────────────
        public const int MaxSkillLevel = 10;                 // [제안]
        public const double XpPerLevelStep = 100;            // [제안] 다음 레벨까지 100 × 다음 레벨
        public const int PurifierMedicalLevel = 3;           // [제안] 의료 레벨이 높으면 정화제 제작 [확정]
        public const double MedicalEffectPerLevel = 0.1;     // [제안] 정화 효과 = 1 + (의료 레벨-1) × 0.1

        // ── 불과 요리 (docs/09-home-life.md) ───────────────────────────
        public const double FuelUnitsPerPaper = 1;           // [제안] 모닥불 연료
        public const double FuelUnitsPerWood = 3;            // [제안]
        public const double CampfireCookHours = 1.0;         // [제안]
        public const double GasBurnerCookHours = 0.5;        // [제안] 가스버너는 효율이 좋음 [확정]
        public const int ButaneChargesPerCan = 6;            // [제안] 부탄가스 1통으로 요리 6번
        public const int GasBurnerDurability = 30;           // [제안] 가스버너도 고장 남 [확정]

        // ── 빗물 ───────────────────────────────────────────────────────
        public static readonly double[] RainChanceBySeason = { 0.30, 0.45, 0.25, 0.10 }; // [제안] 봄, 여름, 가을, 겨울
        public const int RainWaterPerCollector = 4;          // [제안] 비 오는 날 빗물받이 하나당
        public const double BuildRainCollectorHours = 1;     // [제안]

        // ── 현관 ───────────────────────────────────────────────────────
        public const double FrontDoorMax = 100;
        public const double FrontDoorDailyDamage = 4;        // [제안] 바깥에서 계속 충격이 옴 [확정]
        public const double DoorRepairPerMaterial = 15;      // [제안]
        public const double DoorRepairPerTape = 5;           // [제안]
        public const double DoorRepairHours = 1;             // [제안]
        public const int HomeFurnitureMaterials = 10;        // [제안] 집 안 가구를 부숴 얻는 자재 총량
        public const double BreakFurnitureHours = 1;         // [제안]

        // ── 집 정화 ────────────────────────────────────────────────────
        public const double HousePurityMax = 100;
        public const double HousePurityDailyLoss = 10;       // [제안]
        public const double HousePurityPerPurifier = 40;     // [제안]
        public const double UnpurifiedHomeContaminationPerHour = 0.5; // [제안] 정화가 끊긴 집
        public const double BodyPurifyHours = 0.5;           // [제안]

        // ── AI 파견 (docs/02-day-expedition.md) ────────────────────────
        public const double DispatchLootShare = 0.4;         // [제안] 오브젝트 물자 중 가져오는 비율 (30~50%)
        public const double DispatchHoursPerSpot = 3;        // [제안] 바깥 3시간마다 오브젝트 1곳
        public const int DispatchReturnHoursBeforeEnd = 1;   // [제안] 해 지기 1시간 전에 돌아옴
        public const int FastDispatchHoursEarlier = 1;       // [제안] 이동이 빠른 캐릭터는 1시간 일찍
        public const int MinDispatchHours = 1;
    }
}
