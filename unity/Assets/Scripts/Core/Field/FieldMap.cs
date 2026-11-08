using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    /// <summary>
    /// 바깥 필드의 수색 대상 하나 (편의점, 공구점, 길가 차량, 주택 등).
    /// 건물 구조는 고정, 물자는 무작위 [확정]. 첫 버전은 물자를 정해 두고 시작한다.
    /// </summary>
    public sealed class LootSpot
    {
        public string Id { get; }
        public string Name { get; }
        public Direction Direction { get; }
        /// <summary>집에서 가까운 순서. 0이 가장 가깝다.</summary>
        public int Order { get; }
        public Inventory Contents { get; } = new Inventory();
        /// <summary>AI 파견이 상호작용해서 필드에서 제거된 오브젝트 [확정].</summary>
        public bool IsRemoved { get; internal set; }

        public LootSpot(string id, string name, Direction direction, int order)
        {
            Id = id;
            Name = name;
            Direction = direction;
            Order = order;
        }
    }

    /// <summary>
    /// 집 앞 사거리에서 4방향으로 나뉜 필드. 수거한 물자는 다시 생기지 않는다 [확정].
    /// </summary>
    public sealed class FieldMap
    {
        private readonly List<LootSpot> spots = new List<LootSpot>();

        public IReadOnlyList<LootSpot> All => spots;

        public LootSpot Get(string id) => spots.First(s => s.Id == id);

        /// <summary>아직 남아 있는 오브젝트를 가까운 순서로.</summary>
        public IEnumerable<LootSpot> Remaining(Direction d) =>
            spots.Where(s => s.Direction == d && !s.IsRemoved).OrderBy(s => s.Order);

        public LootSpot AddSpot(string id, string name, Direction direction, int order, params (string itemId, int count)[] contents)
        {
            var spot = new LootSpot(id, name, direction, order);
            foreach (var (itemId, count) in contents) spot.Contents.Add(itemId, count);
            spots.Add(spot);
            return spot;
        }

        /// <summary>
        /// 직접 수색해서 줍는다. 그 방향으로 나가 있어야 하고, 무게 한도 안에서만 줍는다.
        /// 실제로 주운 개수를 돌려준다.
        /// </summary>
        public int Take(Survivor s, LootSpot spot, string itemId, int amount)
        {
            if (s.IsDead || spot.IsRemoved) return 0;
            if (s.Location != SurvivorLocation.Outside || s.OutingDirection != spot.Direction) return 0;
            int taken = 0;
            while (taken < amount && spot.Contents.Count(itemId) > 0 && s.TryPickUp(itemId))
            {
                spot.Contents.Remove(itemId);
                taken++;
            }
            return taken;
        }

        /// <summary>
        /// 튜토리얼 필드 [제안]. 첫 버전은 북쪽(편의점 두 곳과 공구점)부터 만든다 [확정].
        /// 다른 방향에는 AI 파견이 갈 수 있도록 주택 몇 채만 둔다.
        /// </summary>
        public static FieldMap CreateTutorial()
        {
            var f = new FieldMap();
            var C = ItemCatalog.CannedFood.Id; var N = ItemCatalog.InstantNoodle.Id; var W = ItemCatalog.BottledWater.Id;
            var P = ItemCatalog.Paper.Id; var L = ItemCatalog.LiquorBottle.Id; var B = ItemCatalog.Butane.Id;
            var D = ItemCatalog.Disinfectant.Id; var T = ItemCatalog.Tape.Id; var S = ItemCatalog.Stone.Id;

            f.AddSpot("north_conv_a", "편의점 A", Direction.North, 0, (C, 6), (N, 8), (W, 6), (P, 4), (L, 3));
            f.AddSpot("north_car", "길가 차량", Direction.North, 1, (P, 2), (T, 1), (S, 4));
            f.AddSpot("north_conv_b", "편의점 B", Direction.North, 2, (C, 5), (W, 5), (B, 3), (D, 1), (L, 2));
            f.AddSpot("north_hardware", "공구점", Direction.North, 3,
                (T, 4), (ItemCatalog.Tarp.Id, 2), (ItemCatalog.Vinyl.Id, 3), (ItemCatalog.PlasticContainer.Id, 3),
                (ItemCatalog.BuildingMaterial.Id, 4), (ItemCatalog.GasBurner.Id, 1), (B, 2));
            f.AddSpot("north_house_1", "주택", Direction.North, 4,
                (ItemCatalog.Wood.Id, 3), (P, 3), (ItemCatalog.Bleach.Id, 1), (ItemCatalog.HouseholdMedicine.Id, 2));
            f.AddSpot("north_house_2", "주택", Direction.North, 5, (C, 2), (ItemCatalog.Bleach.Id, 1), (ItemCatalog.Wood.Id, 2));

            foreach (var d in new[] { Direction.East, Direction.South, Direction.West })
            {
                string key = d.ToString().ToLowerInvariant();
                f.AddSpot(key + "_house_1", "주택", d, 0, (C, 2), (W, 2), (P, 3));
                f.AddSpot(key + "_house_2", "주택", d, 1, (ItemCatalog.Wood.Id, 3), (L, 1), (S, 3));
            }
            return f;
        }
    }
}
