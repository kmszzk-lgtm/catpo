using System.Collections.Generic;

namespace Catpo.Core
{
    /// <summary>
    /// 튜토리얼 시작 살림 (docs/09-home-life.md).
    /// 3층 빌라와 4층 옥탑방에 집이 10가구 있다 [확정]. 1층 경비실의 도어락 마스터 키로 다 열어서 모았다 [확정].
    /// 생수·보존식·부탄가스는 몇몇 집에만 있던 것이라 건물 전체 합계로 정한다 [확정].
    /// </summary>
    public static class StartingSupplies
    {
        public const int Households = Balance.BuildingHouseholds;

        /// <summary>건물 전체를 다 뒤져서 나온 합계.</summary>
        public static readonly IReadOnlyDictionary<string, int> BuildingTotal = new Dictionary<string, int>
        {
            { ItemCatalog.BottledWater.Id, 11 },   // 1.5L 11통 [확정]. 생수를 많이 쌓아 두는 집은 흔치 않음
            { ItemCatalog.CannedFood.Id, 10 },     // 다 합쳐 보존식 1인 30일치 [확정] = 통조림 10 + 라면 12 + 초콜릿 8 [제안]
            { ItemCatalog.InstantNoodle.Id, 12 },
            { ItemCatalog.Chocolate.Id, 8 },
            { ItemCatalog.Butane.Id, 5 },          // 부탄가스를 쓰던 집은 하나뿐 [확정]
            { ItemCatalog.Tape.Id, 6 },            // 강력접착테이프, 청색테이프 같은 것이 몇몇 집에 몇 개 [확정], 개수 [제안]
            { ItemCatalog.VinylBag.Id, 20 },       // 어지간한 집에는 다 있음 [확정], 개수 [제안]
            { ItemCatalog.PetBottle.Id, 4 },       // [제안]
            { ItemCatalog.Pot.Id, 3 },             // [제안]
            { ItemCatalog.Paper.Id, 20 },          // [제안]
            { ItemCatalog.Bleach.Id, 4 },          // [제안]
            { ItemCatalog.LiquorBottle.Id, 3 },    // [제안]
        };

        /// <summary>조미료는 한 번 쓰는 양이 적어서 거의 줄지 않는다. 집집마다 한 통씩 [확정].</summary>
        public static readonly IReadOnlyList<ItemDefinition> Seasonings = new[]
        {
            ItemCatalog.Salt, ItemCatalog.SoySauce, ItemCatalog.SesameOil, ItemCatalog.CookingOil, ItemCatalog.CanolaOil,
        };

        /// <summary>집마다 있던 테이블은 다 분해했다 [확정]. 작업대는 대부분 3층에 있다 [확정].</summary>
        public const int TablesTakenApart = Households * Balance.TablesPerHousehold;

        public static void FillTutorial(Inventory st)
        {
            foreach (var kv in BuildingTotal) st.Add(kv.Key, kv.Value);
            foreach (var seasoning in Seasonings) st.Add(seasoning.Id, Households);

            st.Add(ItemCatalog.WoodStick.Id, TablesTakenApart * Balance.SticksPerFurniture);
            st.Add(ItemCatalog.WoodBoard.Id, TablesTakenApart * Balance.BoardsPerFurniture);
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
