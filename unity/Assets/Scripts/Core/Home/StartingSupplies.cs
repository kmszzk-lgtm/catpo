using System;
using System.Collections.Generic;

namespace Catpo.Core
{
    /// <summary>
    /// 튜토리얼 시작 살림 (docs/09-home-life.md).
    /// 3층 빌라와 4층 옥탑방에 집이 10가구 있다 [확정]. 1층 경비실의 도어락 마스터 키로 다 열어서 모았다 [확정].
    /// 건물에서 버틴 사람은 4명이라, 6가구 살림은 아무도 쓰지 않았다 [확정].
    /// </summary>
    public static class StartingSupplies
    {
        public const int Households = Balance.BuildingHouseholds;
        public const int UntouchedHouseholds = 6;              // [확정]
        public const double UsedHouseholdShareLeft = 0.5;      // [제안] 4명이 버티면서 쓴 집은 절반쯤 남음

        /// <summary>살림이 온전히 남은 가구 수로 환산한 값. 6 + 4 × 0.5 = 8가구.</summary>
        public static double HouseholdEquivalents =>
            UntouchedHouseholds + (Households - UntouchedHouseholds) * UsedHouseholdShareLeft;

        /// <summary>
        /// 한 가구에 있던 소모품 [제안]. 앞서 기획자가 준 수치(생수 6통, 보존식 1인 30일치, 부탄가스 5개)를 한 가구 기준으로 봤다.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, int> PerHousehold = new Dictionary<string, int>
        {
            { ItemCatalog.BottledWater.Id, 6 },    // 생수는 집에 많이 두지 않음 [확정]
            { ItemCatalog.CannedFood.Id, 10 },     // 보존식 1인 30일치 [확정] = 통조림 10 + 라면 12 + 초콜릿 8
            { ItemCatalog.InstantNoodle.Id, 12 },
            { ItemCatalog.Chocolate.Id, 8 },
            { ItemCatalog.Butane.Id, 5 },
            { ItemCatalog.VinylBag.Id, 10 },       // 어지간한 집에는 다 있음 [확정]
            { ItemCatalog.PetBottle.Id, 2 },
            { ItemCatalog.Pot.Id, 2 },
            { ItemCatalog.Paper.Id, 10 },
            { ItemCatalog.Tape.Id, 1 },
            { ItemCatalog.Bleach.Id, 1 },
            { ItemCatalog.LiquorBottle.Id, 1 },
            { ItemCatalog.HouseholdMedicine.Id, 1 },
        };

        /// <summary>조미료는 한 번 쓰는 양이 적어서 거의 줄지 않는다. 집집마다 한 통씩 [확정].</summary>
        public static readonly IReadOnlyList<ItemDefinition> Seasonings = new[]
        {
            ItemCatalog.Salt, ItemCatalog.SoySauce, ItemCatalog.SesameOil, ItemCatalog.CookingOil, ItemCatalog.CanolaOil,
        };

        public static void FillTutorial(Inventory st)
        {
            foreach (var kv in PerHousehold)
                st.Add(kv.Key, (int)Math.Round(kv.Value * HouseholdEquivalents));
            foreach (var seasoning in Seasonings) st.Add(seasoning.Id, Households);

            // 그동안 부서진 가구 조각 [제안]
            st.Add(ItemCatalog.WoodStick.Id, 4);
            st.Add(ItemCatalog.WoodBoard.Id, 2);
            // 의냥이 시작 아이템 [확정]: 응급처치 도구 여러 개, 상비약, 진통제. 개수와 소독약은 [제안]
            st.Add(ItemCatalog.FirstAidKit.Id, 3);
            st.Add(ItemCatalog.HouseholdMedicine.Id, 5);
            st.Add(ItemCatalog.Painkiller.Id, 5);
            st.Add(ItemCatalog.Disinfectant.Id, 2);
            // 프롤로그를 끝내면서 함께 노는 취미 물건이 생긴다 [확정]
            st.Add(ItemCatalog.Hanafuda.Id, 1);
            st.Add(ItemCatalog.PlayingCards.Id, 1);
        }
    }
}
