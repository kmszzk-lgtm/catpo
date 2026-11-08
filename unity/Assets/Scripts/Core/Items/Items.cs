using System;
using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    public enum ItemCategory
    {
        Food,
        Water,
        Fuel,
        Material,
        Medical,
        Tool,
        Seasoning,
        /// <summary>둘 이상이 함께 노는 취미 물건 [확정].</summary>
        Hobby,
    }

    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public double WeightKg { get; }
        /// <summary>외출 전 유인이나 자동 귀환의 미끼로 던질 수 있는가 (docs/02-day-expedition.md).</summary>
        public bool IsLure { get; }
        /// <summary>먹거나 마셨을 때 줄어드는 배고픔·목마름 [제안].</summary>
        public double HungerRelief { get; }
        public double ThirstRelief { get; }

        public bool IsEdible => HungerRelief > 0 || ThirstRelief > 0;

        public ItemDefinition(string id, string name, ItemCategory category, double weightKg, bool isLure = false,
            double hungerRelief = 0, double thirstRelief = 0)
        {
            Id = id;
            Name = name;
            Category = category;
            WeightKg = weightKg;
            IsLure = isLure;
            HungerRelief = hungerRelief;
            ThirstRelief = thirstRelief;
        }
    }

    /// <summary>첫 버전에 쓰는 아이템. 무게는 모두 [제안].</summary>
    public static class ItemCatalog
    {
        private static readonly Dictionary<string, ItemDefinition> items = new Dictionary<string, ItemDefinition>();

        // 물과 음식
        public static readonly ItemDefinition BottledWater = Add("bottled_water", "생수", ItemCategory.Water, 1.5, thirstRelief: 50); // 1.5L 한 통 [확정]
        /// <summary>빗물은 끓여야 마실 수 있다 [제안].</summary>
        public static readonly ItemDefinition RainWater = Add("rain_water", "빗물", ItemCategory.Water, 1.0);
        public static readonly ItemDefinition BoiledWater = Add("boiled_water", "끓인 물", ItemCategory.Water, 1.0, thirstRelief: 30);
        public static readonly ItemDefinition CannedFood = Add("canned_food", "통조림", ItemCategory.Food, 0.4, hungerRelief: 35);
        public static readonly ItemDefinition InstantNoodle = Add("instant_noodle", "라면", ItemCategory.Food, 0.12, hungerRelief: 30);
        public static readonly ItemDefinition Chocolate = Add("chocolate", "초콜릿", ItemCategory.Food, 0.1, hungerRelief: 20);

        // 조미료: 한 번 쓰는 양이 적고 집집마다 한 통씩 있다 [확정]
        public static readonly ItemDefinition Salt = Add("salt", "소금", ItemCategory.Seasoning, 0.5);
        public static readonly ItemDefinition SoySauce = Add("soy_sauce", "간장", ItemCategory.Seasoning, 0.5);
        public static readonly ItemDefinition SesameOil = Add("sesame_oil", "참기름", ItemCategory.Seasoning, 0.3);
        public static readonly ItemDefinition CookingOil = Add("cooking_oil", "식용유", ItemCategory.Seasoning, 0.9);
        public static readonly ItemDefinition CanolaOil = Add("canola_oil", "카놀라유", ItemCategory.Seasoning, 0.9);

        // 취미 물건: 둘 이상이 함께해야 스트레스가 풀린다 [확정]. 프롤로그를 끝내면서 생긴다 [확정]
        public static readonly ItemDefinition Hanafuda = Add("hanafuda", "화투", ItemCategory.Hobby, 0.1);
        public static readonly ItemDefinition PlayingCards = Add("playing_cards", "트럼프 카드", ItemCategory.Hobby, 0.1);

        // 나무: 나무 막대와 나무 판 두 가지 [확정]. 가구를 분해해서 얻는다
        public static readonly ItemDefinition WoodStick = Add("wood_stick", "나무 막대", ItemCategory.Material, 0.5);
        public static readonly ItemDefinition WoodBoard = Add("wood_board", "나무 판", ItemCategory.Material, 1.5);

        // 연료와 불
        public static readonly ItemDefinition Paper = Add("paper", "종이", ItemCategory.Fuel, 0.3);
        public static readonly ItemDefinition Butane = Add("butane", "부탄가스", ItemCategory.Fuel, 0.3);
        public static readonly ItemDefinition GasBurner = Add("gas_burner", "가스버너", ItemCategory.Tool, 1.5);

        // 유인용으로 던질 수 있는 것: 소음이 큰 것 [확정]
        public static readonly ItemDefinition Stone = Add("stone", "돌", ItemCategory.Material, 0.5, isLure: true);
        public static readonly ItemDefinition LiquorBottle = Add("liquor_bottle", "술병", ItemCategory.Material, 0.8, isLure: true);

        // 의료와 정화
        public static readonly ItemDefinition Disinfectant = Add("disinfectant", "소독약", ItemCategory.Medical, 0.3);
        public static readonly ItemDefinition Bleach = Add("bleach", "락스", ItemCategory.Material, 1.0);
        public static readonly ItemDefinition Purifier = Add("purifier", "정화제", ItemCategory.Medical, 0.3);
        public static readonly ItemDefinition Suppressant = Add("suppressant", "억제제", ItemCategory.Medical, 0.05);
        public static readonly ItemDefinition Cure = Add("cure", "완치제", ItemCategory.Medical, 0.05);
        public static readonly ItemDefinition FirstAidKit = Add("first_aid_kit", "응급처치 도구", ItemCategory.Medical, 0.5);
        public static readonly ItemDefinition Painkiller = Add("painkiller", "진통제", ItemCategory.Medical, 0.05);
        public static readonly ItemDefinition HouseholdMedicine = Add("household_medicine", "상비약", ItemCategory.Medical, 0.1);

        // 생활용품과 재료
        public static readonly ItemDefinition Tarp = Add("tarp", "방수포", ItemCategory.Material, 1.5);
        public static readonly ItemDefinition Tape = Add("tape", "테이프", ItemCategory.Material, 0.2);
        /// <summary>어지간한 집에는 다 있다 [확정].</summary>
        public static readonly ItemDefinition VinylBag = Add("vinyl_bag", "비닐봉투", ItemCategory.Material, 0.02);
        /// <summary>빗물을 담을 수 있는 용기 [확정]: 1.5L 페트병, 냄비.</summary>
        public static readonly ItemDefinition PetBottle = Add("pet_bottle", "페트병(1.5L)", ItemCategory.Material, 0.05);
        public static readonly ItemDefinition Pot = Add("pot", "냄비", ItemCategory.Tool, 1.0);

        private static ItemDefinition Add(string id, string name, ItemCategory category, double weightKg, bool isLure = false,
            double hungerRelief = 0, double thirstRelief = 0)
        {
            var item = new ItemDefinition(id, name, category, weightKg, isLure, hungerRelief, thirstRelief);
            items.Add(id, item);
            return item;
        }

        public static ItemDefinition Get(string id)
        {
            if (!items.TryGetValue(id, out var item)) throw new KeyNotFoundException($"알 수 없는 아이템: {id}");
            return item;
        }

        public static bool TryGet(string id, out ItemDefinition item) => items.TryGetValue(id, out item);

        public static IEnumerable<ItemDefinition> All => items.Values;
    }

    /// <summary>아이템 묶음. 캐릭터 인벤토리와 공용 창고가 같은 구조를 쓴다.</summary>
    public sealed class Inventory
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public int Count(string itemId) => counts.TryGetValue(itemId, out var n) ? n : 0;

        public IReadOnlyDictionary<string, int> Items => counts;

        public bool IsEmpty => counts.Count == 0;

        public double TotalWeightKg => counts.Sum(kv => ItemCatalog.Get(kv.Key).WeightKg * kv.Value);

        public void Add(string itemId, int amount = 1)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            ItemCatalog.Get(itemId); // 없는 아이템이면 예외
            counts[itemId] = Count(itemId) + amount;
        }

        public bool Remove(string itemId, int amount = 1)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int have = Count(itemId);
            if (have < amount) return false;
            if (have == amount) counts.Remove(itemId);
            else counts[itemId] = have - amount;
            return true;
        }

        /// <summary>전부 다른 묶음으로 옮긴다. 귀환할 때 공용 창고에 자동 수납하는 데 쓴다 [확정].</summary>
        public void MoveAllTo(Inventory other)
        {
            foreach (var kv in counts.ToList()) other.Add(kv.Key, kv.Value);
            counts.Clear();
        }
    }
}
