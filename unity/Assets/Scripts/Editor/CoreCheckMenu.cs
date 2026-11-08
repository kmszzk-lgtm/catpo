using System;
using System.Text;
using Catpo.Core;
using UnityEditor;
using UnityEngine;

namespace Catpo.EditorTools
{
    /// <summary>
    /// Unity 상단 메뉴 "좀비냥 > 규칙 코드 확인".
    /// 화면 없이 튜토리얼 반나절을 돌려 보고 결과를 Console 창에 찍는다.
    /// 규칙 코드가 Unity 안에서 제대로 컴파일되고 돌아가는지 확인하는 용도다.
    /// </summary>
    public static class CoreCheckMenu
    {
        [MenuItem("좀비냥/규칙 코드 확인")]
        public static void Run()
        {
            try
            {
                var state = GameState.NewTutorial();
                var jip = state.Get("jipnyang");
                var gun = state.Get("gunnyang");
                var ui = state.Get("uinyang");
                var gil = state.Get("gilnyang");
                LogState(state, "튜토리얼 시작");

                state.Advance(2 * 60); // 09:00, 외출 가능 시간
                gun.AutoDispatchDirection = Direction.North;
                state.Advance(0);
                Say($"군냥이 자동 파견: {(gun.IsDispatched ? "북쪽으로 출발" : "출발하지 못함")}");

                Say($"길냥이 빗물받이 만들기 (페트병): {Describe(HouseworkRules.BuildRainCollector(state, gil, ItemCatalog.PetBottle.Id))}");
                state.Storage.Add(ItemCatalog.RainWater.Id, 2); // 비 온 셈 치기
                Say($"집냥이 물 끓이기 (모닥불): {Describe(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire))}");
                Say($"의냥이 정화제 만들기 (소독약): {Describe(CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromDisinfectant))}");

                state.Advance(9 * 60); // 18:00
                LogState(state, "저녁 6시");

                Say($"의냥이가 군냥이 몸 정화: {Describe(HouseworkRules.PurifyBody(state, ui, gun))}");
                Say($"의냥이 집 정화: {Describe(HouseworkRules.PurifyHouse(state, ui))}");
                bool played = state.PlayHobby(new[] { jip, gil }, ItemCatalog.Hanafuda.Id, 1);
                if (played) state.Advance(60);
                Say($"집냥이와 길냥이 화투 1시간: {(played ? "스트레스 풀림" : "못 함")}");
                Say($"모두 잠자기: {(state.SleepUntilMorning() ? "다음 날 아침" : "밖에 누가 있어서 못 잠")}");
                LogState(state, "다음 날 아침");
                Say($"집냥이 현관 보수 (나무 막대): {Describe(HouseworkRules.RepairFrontDoor(state, jip, ItemCatalog.WoodStick.Id))}, 현관 {state.Home.FrontDoor:0}");

                Debug.Log("<b>[좀비냥] 규칙 코드 확인 끝.</b> 빨간 오류가 없으면 정상입니다.");
            }
            catch (Exception e)
            {
                Debug.LogError("[좀비냥] 규칙 코드 확인 중 오류가 났습니다. 아래 내용을 그대로 알려주세요.");
                Debug.LogException(e);
            }
        }

        private static void Say(string message) => Debug.Log("[좀비냥] " + message);

        private static string Describe(ActionResult r) =>
            r.Success ? $"성공 ({r.Hours:0.##}시간)" : $"실패 ({r.Failure})";

        private static void LogState(GameState state, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[좀비냥] <b>{title}</b> — {state.Time.Day}일째 {state.Time.Hour:00}:{state.Time.Minute:00}");
            foreach (var s in state.Survivors)
            {
                string where = s.Location == SurvivorLocation.Home ? "집" : $"밖({s.OutingDirection})";
                sb.AppendLine($"  {s.Name}: {where}, 오염도 {s.Contamination:0.#}, 감염도 {s.Infection:0.#}, " +
                              $"스트레스 {s.Stress:0.#}, 배고픔 {s.Hunger:0.#}, 목마름 {s.Thirst:0.#}");
            }
            sb.AppendLine($"  창고: 생수 {Count(state, ItemCatalog.BottledWater)}, 통조림 {Count(state, ItemCatalog.CannedFood)}, " +
                          $"라면 {Count(state, ItemCatalog.InstantNoodle)}, 정화제 {Count(state, ItemCatalog.Purifier)}, " +
                          $"끓인 물 {Count(state, ItemCatalog.BoiledWater)}, 나무 막대 {Count(state, ItemCatalog.WoodStick)}, " +
                          $"나무 판 {Count(state, ItemCatalog.WoodBoard)}, " +
                          $"화투 {Count(state, ItemCatalog.Hanafuda)}, 트럼프 카드 {Count(state, ItemCatalog.PlayingCards)}");
            sb.AppendLine($"  현관 {state.Home.FrontDoor:0}, 빗물받이 {state.Home.RainCollectorCapacity}, " +
                          $"부탄가스 {Count(state, ItemCatalog.Butane)}통, 집 정화 {(state.Home.IsHouseProtected(state.Time.TotalMinutes) ? "유지 중" : "끊김")}");
            Debug.Log(sb.ToString());
        }

        private static int Count(GameState state, ItemDefinition item) => state.Storage.Count(item.Id);
    }
}
