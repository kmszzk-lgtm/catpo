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
        public const double BaseCarryLimitKg = 8;            // [제안] 무게 한도는 높은 편이 아님 [확정]
        public const double OverloadFactor = 1.5;            // [제안] 사람은 과적해서 더 들 수 있음 [확정]. AI는 과적하지 않음 [확정]
        // 과적하면 느려지고, 스태미나가 더 빨리 닳고, 공격 속도가 느려진다 [확정]. 과적 한도에 닿았을 때의 배율 [제안]
        public const double OverloadMaxMoveSlow = 0.4;       // 이동 속도 ×0.6
        public const double OverloadMaxStaminaExtra = 0.6;   // 스태미나 소모 ×1.6
        public const double OverloadMaxAttackSlow = 0.25;    // 공격 속도 ×0.75

        // ── 배고픔·목마름 (0 = 괜찮음, 100 = 한계) ──────────────────────
        public const double NeedMax = 100;
        public const double HungerPerHour = 1.2;             // [제안] 약 3.5일이면 한계
        public const double ThirstPerHour = 1.8;             // [제안] 약 2.3일이면 한계
        public const double NeedStressThreshold = 80;        // [제안] 이 이상이면 스트레스가 쌓임
        public const double NeedStressPerHour = 1;           // [제안]

        // ── 숙련도와 제작 ──────────────────────────────────────────────
        public const int MaxSkillLevel = 10;                 // [제안]
        public const double XpPerLevelStep = 100;            // [제안] 다음 레벨까지 100 × 다음 레벨
        public const int PurifierMedicalLevel = 3;           // [제안] 의료 레벨이 높으면 정화제 제작 [확정]
        // 만드는 사람의 특성과 기술 레벨은 제작 속도와 재료 절감에 영향을 준다 [확정]. 쓰는 사람에 따른 보너스는 없다 [확정].
        public const double CraftSpeedPerLevel = 0.1;        // [제안] 제작 속도 = 1 + (레벨-1) × 0.1
        public const double MaterialSavingPerLevel = 0.05;   // [제안] 재료 절감 = (레벨-1) × 0.05, 쌓여서 1이 되면 재료 1개를 아낌

        // ── 불과 요리 (docs/09-home-life.md) ───────────────────────────
        // 불은 플레이어가 고른다 [확정].
        public const double FuelUnitsPerPaper = 1;           // [제안] 모닥불 연료. 요리 1번 = 1
        public const double FuelUnitsPerWoodStick = 1;       // [제안]
        public const double FuelUnitsPerWoodBoard = 2;       // [제안]
        public const double CampfireCookHours = 1.0;         // [제안]
        public const double GasBurnerCookHours = 0.5;        // [제안] 가스버너는 효율이 좋음 [확정]
        public const int ButaneChargesPerCan = 6;            // [제안] 부탄가스 1통으로 요리 6번
        public const int GasBurnerCanLife = 100;             // [확정] 부탄가스 100통 정도 쓰면 고장

        // ── 빗물 ───────────────────────────────────────────────────────
        public static readonly double[] RainChanceBySeason = { 0.30, 0.45, 0.25, 0.10 }; // [제안] 봄, 여름, 가을, 겨울
        public const int RainWaterPerFunnelCollector = 4;    // [제안] 비닐봉투 + 나무 막대 + 용기
        public const int RainWaterPerPot = 2;                // [제안] 냄비째로 비 오는 곳에 두기
        public const double BuildRainCollectorHours = 1;     // [제안]

        // ── 현관 ───────────────────────────────────────────────────────
        public const double FrontDoorMax = 100;
        public const double FrontDoorDailyDamage = 4;        // [제안] 바깥에서 계속 충격이 옴 [확정]
        public const double DoorRepairPerBoard = 15;         // [제안] 나무 판
        public const double DoorRepairPerStick = 5;          // [제안] 나무 막대
        public const double DoorRepairPerTape = 5;           // [제안]
        public const double DoorRepairHours = 1;             // [제안]
        public const int HomeFurniturePieces = 24;           // [제안] 침대, 매트리스, 테이블 등은 집집마다 많음 [확정]
        public const int BoardsPerFurniture = 1;             // [제안] 가구 1개를 분해하면 나무 판 1개
        public const int SticksPerFurniture = 2;             // [제안] 나무 막대 2개
        public const double BreakFurnitureHours = 1;         // [제안]

        // ── 집 정화 ────────────────────────────────────────────────────
        public const int HouseProtectionHours = 24;          // 매일 써야 함 [확정]. 한 번에 24시간 [제안]
        public const double BodyPurifyHours = 0.5;           // [제안]
        // 정화가 끊긴 집은 바깥에 있는 것처럼 오염도가 오른다 [확정] → ContaminationPerOutsideHour 사용

        // ── AI 파견 (docs/02-day-expedition.md) ────────────────────────
        // 물자가 많은 곳은 가방이 빨리 차서 빨리 오고, 적은 곳은 찾는 데 오래 걸려 늦게 온다 [확정].
        public const double DispatchTravelMinutesPerStep = 20; // [제안] 오브젝트 사이 이동 시간
        public const double DispatchSearchMinutesPerItem = 6;  // [제안] 물건 1개를 찾는 시간
        public const double DispatchFullMarginKg = 0.3;      // [제안] 남은 여유가 이보다 적으면 가득 찬 것으로 봄
        public const int DispatchReturnHoursBeforeEnd = 1;   // [제안] 늦어도 해 지기 1시간 전에 돌아옴
        public const int MinDispatchHours = 1;
        public const double AutoDispatchContaminationLimit = 30; // [확정] 자동 파견은 오염도가 이 이상이면 쉬게 함
        // AI가 나갔다 돌아왔을 때 오염도가 50 이상이 되지 않게 파견 시간을 줄인다 [확정]. 여유를 둔 상한 [제안]
        public const double AiMaxContaminationOnReturn = 45;
        public const int MinDispatchExposureMinutes = 30;     // [제안] 이보다 짧게밖에 못 나가면 파견하지 않음
    }
}
