using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// 규칙 코드의 이름은 하나씩 지정해서, Unity 이름과 겹칠 일이 없게 한다
using Balance = Catpo.Core.Balance;
using Direction = Catpo.Core.Direction;
using ItemCatalog = Catpo.Core.ItemCatalog;
using ItemCategory = Catpo.Core.ItemCategory;
using ItemDefinition = Catpo.Core.ItemDefinition;
using Korean = Catpo.Core.Korean;
using PlaySession = Catpo.Core.PlaySession;
using HeatSource = Catpo.Core.HeatSource;
using RecipeCatalog = Catpo.Core.RecipeCatalog;
using Recipe = Catpo.Core.Recipe;
using Survivor = Catpo.Core.Survivor;
using SurvivorLocation = Catpo.Core.SurvivorLocation;

namespace Catpo.Game
{
    /// <summary>
    /// 옥탑방 화면 (첫 버전 3단계 시험판). Unity에 기본으로 들어 있는 IMGUI로 글자와 버튼만 그린다.
    /// 게임 진행은 모두 PlaySession(규칙 코드)이 맡고, 여기서는 그리고 버튼을 연결하기만 한다.
    /// 버튼을 누르면 바로 바꾸지 않고 다음 프레임 Update에서 처리한다 (IMGUI 레이아웃 오류 방지).
    /// </summary>
    public sealed class HomeScreen
    {
        private const float RefWidth = 1280f;
        private const float RefHeight = 720f;
        private const float TopHeight = 88f;
        private const float LogHeight = 150f;
        private const float LeftWidth = 330f;
        private const float RightWidth = 270f;
        private const float Pad = 8f;

        private enum Place { Rooftop, RooftopRoom, Floor3, Floor2, Floor1, Basement }

        private static readonly Place[] PlaceOrder =
            { Place.Rooftop, Place.RooftopRoom, Place.Floor3, Place.Floor2, Place.Floor1, Place.Basement };

        private enum Picking { None, BodyPurify, Hanafuda, PlayingCards }

        private static readonly Direction[] Directions = { Direction.North, Direction.East, Direction.South, Direction.West };

        private static readonly ItemCategory[] StorageOrder =
        {
            ItemCategory.Water, ItemCategory.Food, ItemCategory.Medical, ItemCategory.Fuel,
            ItemCategory.Material, ItemCategory.Tool, ItemCategory.Seasoning, ItemCategory.Hobby,
        };

        private PlaySession session;
        private Survivor selected;
        private Place place;
        private Picking picking;
        private readonly List<Action> pending = new List<Action>();
        private Vector2 peopleScroll, actionScroll, storageScroll, logScroll;
        private int shownLogCount;

        private bool stylesReady;
        private GUIStyle title, text, small, button, card, tab;

        public HomeScreen()
        {
            NewGame();
        }

        private void NewGame()
        {
            session = PlaySession.NewTutorial();
            selected = null;
            place = Place.RooftopRoom;
            picking = Picking.None;
            shownLogCount = -1;
        }

        // ── Unity에서 부르는 곳 ─────────────────────────────────────────

        public void Update(float deltaTime)
        {
            if (pending.Count > 0)
            {
                var todo = pending.ToArray();
                pending.Clear();
                foreach (var action in todo) action();
            }
            session.Tick(deltaTime);
        }

        public void OnGUI()
        {
            EnsureStyles();
            float scale = Math.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            if (scale <= 0f) return;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            if (!session.HasChosenPlayer) DrawChoose(w, h);
            else if (session.State.IsWorldOver) DrawWorldOver(w, h);
            else DrawHome(w, h);
        }

        private void Later(Action action) => pending.Add(action);

        // ── 캐릭터 고르기 ──────────────────────────────────────────────

        private void DrawChoose(float w, float h)
        {
            GUI.Box(new Rect(0, 0, w, h), "");
            GUILayout.BeginArea(new Rect(40, 30, w - 80, h - 60));
            GUILayout.Label("좀비냥 · 튜토리얼 모드 (봄 1일째 아침)", title);
            GUILayout.Label("누구로 할까요? 고른 캐릭터는 직접 맡고, 나머지 셋은 AI가 맡습니다.", text);
            GUILayout.Space(12);

            float cardWidth = (w - 80 - 3 * 12) / 4;
            GUILayout.BeginHorizontal();
            foreach (var s in session.State.Survivors)
            {
                var d = s.Definition;
                string traits = string.Join(", ", s.Traits.Select(t => t.Name));
                string body = $"{d.Name}\n\n{d.Job} · {d.StartFloor}\n특성: {traits}" + (s.CanGoOutside ? "" : "\n밖에 나갈 수 없음");
                if (GUILayout.Button(body, card, GUILayout.Width(cardWidth), GUILayout.Height(220)))
                {
                    var pick = s;
                    Later(() =>
                    {
                        session.ChoosePlayer(pick);
                        selected = pick;
                    });
                }
                GUILayout.Space(12);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            GUILayout.Label("시험판입니다. 그림 없이 글자와 버튼만 있습니다. 직접 밖에 나가는 것은 다음 단계에서 만듭니다.", small);
            GUILayout.EndArea();
        }

        // ── 월드가 끝남 ────────────────────────────────────────────────

        private void DrawWorldOver(float w, float h)
        {
            GUI.Box(new Rect(0, 0, w, h), "");
            GUILayout.BeginArea(new Rect(60, 60, w - 120, h - 120));
            GUILayout.Label("월드가 끝났습니다", title);
            GUILayout.Label($"{session.State.WorldOverReason} · {session.ClockText}", text);
            GUILayout.Space(12);
            foreach (var line in session.Log.Skip(Math.Max(0, session.Log.Count - 8)))
                GUILayout.Label(line, small);
            GUILayout.Space(12);
            if (GUILayout.Button("처음부터 다시", button, GUILayout.Width(240), GUILayout.Height(48)))
                Later(NewGame);
            GUILayout.EndArea();
        }

        // ── 옥탑방 화면 ────────────────────────────────────────────────

        private void DrawHome(float w, float h)
        {
            float bodyHeight = h - TopHeight - LogHeight;
            DrawTopBar(new Rect(0, 0, w, TopHeight));
            DrawPeople(new Rect(0, TopHeight, LeftWidth, bodyHeight));
            DrawActions(new Rect(LeftWidth, TopHeight, w - LeftWidth - RightWidth, bodyHeight));
            DrawStorage(new Rect(w - RightWidth, TopHeight, RightWidth, bodyHeight));
            DrawLog(new Rect(0, h - LogHeight, w, LogHeight));
        }

        private static Rect Inset(Rect r) => new Rect(r.x + Pad, r.y + Pad, r.width - 2 * Pad, r.height - 2 * Pad);

        private void DrawTopBar(Rect r)
        {
            var st = session.State;
            GUI.Box(r, "");
            GUILayout.BeginArea(Inset(r));

            GUILayout.BeginHorizontal();
            GUILayout.Label(session.ClockText, title, GUILayout.Width(260));
            GUILayout.FlexibleSpace();
            GUILayout.Label("시간", small, GUILayout.Width(40));
            foreach (var speed in PlaySession.Speeds)
            {
                bool on = Math.Abs(session.Speed - speed) < 1e-9;
                var keep = GUI.backgroundColor;
                if (on) GUI.backgroundColor = Highlight;
                if (GUILayout.Button(SpeedName(speed), button, GUILayout.Width(64), GUILayout.Height(30)))
                {
                    double pick = speed;
                    Later(() => session.Speed = pick);
                }
                GUI.backgroundColor = keep;
            }
            GUILayout.Space(16);
            if (GUILayout.Button("모두 잠자기", button, GUILayout.Width(120), GUILayout.Height(30)))
                Later(() => session.Sleep());
            GUILayout.EndHorizontal();

            string weather = st.Home.RainedToday ? "비" : "맑음";
            GUILayout.Label(
                $"날씨 {weather}   ·   {session.OutingWindowText}   ·   {session.HouseProtectionText}   ·   " +
                $"현관 {st.Home.FrontDoor:0}/{Balance.FrontDoorMax:0}   ·   빗물받이 {st.Home.RainCollectorCapacity}",
                small);

            GUILayout.EndArea();
        }

        private void DrawPeople(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(Inset(r));
            GUILayout.Label("사람 (눌러서 고르기)", small);
            peopleScroll = GUILayout.BeginScrollView(peopleScroll);
            foreach (var s in session.State.Survivors)
            {
                string star = s == session.Player ? "★ " : "";
                string warn = s.IsInfected ? "  감염!" : "";
                string body =
                    $"{star}{s.Name} · {session.LocationText(s)}{warn}\n" +
                    $"오염 {s.Contamination:0}  감염 {s.Infection:0}  스트레스 {s.Stress:0}\n" +
                    $"배고픔 {s.Hunger:0}  목마름 {s.Thirst:0}\n" +
                    session.ActivityText(s);
                var keep = GUI.backgroundColor;
                if (s == selected) GUI.backgroundColor = Highlight;
                if (GUILayout.Button(body, card, GUILayout.Height(96)))
                {
                    var pick = s;
                    Later(() =>
                    {
                        selected = pick;
                        picking = Picking.None;
                    });
                }
                GUI.backgroundColor = keep;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawActions(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(Inset(r));

            GUILayout.BeginHorizontal();
            foreach (var p in PlaceOrder)
            {
                var keep = GUI.backgroundColor;
                if (p == place) GUI.backgroundColor = Highlight;
                if (GUILayout.Button(PlaceName(p), tab, GUILayout.Height(34)))
                {
                    var pick = p;
                    Later(() =>
                    {
                        place = pick;
                        picking = Picking.None;
                    });
                }
                GUI.backgroundColor = keep;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(PlaceDescription(place), small);
            GUILayout.Space(4);

            actionScroll = GUILayout.BeginScrollView(actionScroll);
            if (selected == null || selected.IsDead)
            {
                GUILayout.Label("왼쪽에서 일을 시킬 사람을 고르세요.", text);
            }
            else if (picking != Picking.None)
            {
                DrawPicker(selected);
            }
            else
            {
                GUILayout.Label($"{Korean.EunNeun(selected.Name)} 무엇을 할까?", text);
                switch (place)
                {
                    case Place.Rooftop: DrawRooftop(selected); break;
                    case Place.RooftopRoom: DrawRooftopRoom(selected); break;
                    case Place.Floor3: DrawFloor3(selected); break;
                    case Place.Floor2: GUILayout.Label("작업대 일부 (무기 손질). 첫 버전에는 아직 만들 것이 없다.", small); break;
                    case Place.Floor1: DrawFloor1(selected); break;
                    case Place.Basement: GUILayout.Label("바이오쿨러는 그림으로만 보여 준다 (첫 버전에는 없음).", small); break;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawRooftop(Survivor s)
        {
            var st = session.State;
            GUILayout.Label("빗물받이", text);
            GUILayout.BeginHorizontal();
            if (ActionButton($"빗물받이 만들기 (비닐봉투 + 나무 막대 + 페트병, 페트병 {Count(ItemCatalog.PetBottle)})"))
                Later(() => session.BuildRainCollector(s, ItemCatalog.PetBottle));
            if (ActionButton($"냄비 내놓기 (냄비 {Count(ItemCatalog.Pot)})"))
                Later(() => session.BuildRainCollector(s, ItemCatalog.Pot));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("도르래로 파견 보내기 (AI가 나가서 흔한 물건을 주워 온다)", text);
            GUILayout.BeginHorizontal();
            foreach (var d in Directions)
            {
                var occupant = st.Outings.OccupantOf(d);
                string label = Korean.DirectionName(d) + (occupant != null ? $" ({occupant.Name} 나가 있음)" : "");
                if (ActionButton(label))
                {
                    var dir = d;
                    Later(() => session.Dispatch(s, dir));
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            string auto = s.AutoDispatchDirection.HasValue ? Korean.DirectionName(s.AutoDispatchDirection.Value) : "끔";
            GUILayout.Label($"자동 파견: {auto} (외출 시간이면 알아서 나가고, 오염도가 높으면 쉰다)", text);
            GUILayout.BeginHorizontal();
            if (ActionButton("자동: 끄기")) Later(() => session.SetAutoDispatch(s, null));
            foreach (var d in Directions)
            {
                if (ActionButton("자동: " + Korean.DirectionName(d)))
                {
                    var dir = d;
                    Later(() => session.SetAutoDispatch(s, dir));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRooftopRoom(Survivor s)
        {
            var home = session.State.Home;
            GUILayout.Label("물 끓이기 (빗물 2개 → 끓인 물 2개)", text);
            string burner = home.HasWorkingGasBurner
                ? $"부탄가스 {Count(ItemCatalog.Butane)}통, 버너 수명 {home.GasBurnerCansLeft}통 남음"
                : "가스버너 고장";
            GUILayout.Label(
                $"빗물 {Count(ItemCatalog.RainWater)}  ·  모닥불 연료: 종이 {Count(ItemCatalog.Paper)}, " +
                $"나무 막대 {Count(ItemCatalog.WoodStick)}, 나무 판 {Count(ItemCatalog.WoodBoard)}  ·  {burner}",
                small);
            GUILayout.BeginHorizontal();
            if (ActionButton("물 끓이기: 모닥불 (느림)"))
                Later(() => session.Craft(s, RecipeCatalog.BoilWater, HeatSource.Campfire));
            if (ActionButton("물 끓이기: 가스버너 (빠름)"))
                Later(() => session.Craft(s, RecipeCatalog.BoilWater, HeatSource.GasBurner));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("먹기 · 마시기", text);
            GUILayout.BeginHorizontal();
            int inRow = 0;
            foreach (var item in ItemCatalog.All.Where(i => i.IsEdible && i != ItemCatalog.RainWater && Count(i) > 0))
            {
                if (inRow == 4)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    inRow = 0;
                }
                if (ActionButton($"{item.Name} ({Count(item)})"))
                {
                    var food = item;
                    Later(() => session.Eat(s, food));
                }
                inRow++;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("함께 놀기 (둘 이상, 1시간)", text);
            GUILayout.BeginHorizontal();
            if (Count(ItemCatalog.Hanafuda) > 0 && ActionButton("화투 치기")) Later(() => picking = Picking.Hanafuda);
            if (Count(ItemCatalog.PlayingCards) > 0 && ActionButton("트럼프 카드")) Later(() => picking = Picking.PlayingCards);
            GUILayout.EndHorizontal();
        }

        private void DrawFloor3(Survivor s)
        {
            GUILayout.Label($"정화제 만들기 (의료 3 이상이나 의냥이) · 정화제 {Count(ItemCatalog.Purifier)}개", text);
            GUILayout.BeginHorizontal();
            RecipeButton(s, RecipeCatalog.PurifierFromDisinfectant, $"소독약 → 2개 (소독약 {Count(ItemCatalog.Disinfectant)})");
            RecipeButton(s, RecipeCatalog.PurifierFromBleach, $"락스 + 끓인 물 → 2개 (락스 {Count(ItemCatalog.Bleach)})");
            RecipeButton(s, RecipeCatalog.PurifierFromLiquor, $"술 → 1개 (술병 {Count(ItemCatalog.LiquorBottle)})");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("정화 (정화제 1개씩)", text);
            GUILayout.BeginHorizontal();
            if (ActionButton("몸 정화해 주기 (오염도 -20)")) Later(() => picking = Picking.BodyPurify);
            if (ActionButton("집 정화 (24시간)")) Later(() => session.PurifyHouse(s));
            GUILayout.EndHorizontal();
        }

        private void RecipeButton(Survivor s, Recipe recipe, string label)
        {
            if (ActionButton(label)) Later(() => session.Craft(s, recipe));
        }

        private void DrawFloor1(Survivor s)
        {
            var home = session.State.Home;
            GUILayout.Label(
                $"현관 {home.FrontDoor:0}/{Balance.FrontDoorMax:0}. 매일 {Balance.FrontDoorDailyDamage:0}씩 상하고, 0이 되면 게임 오버.",
                text);
            GUILayout.BeginHorizontal();
            DoorButton(s, ItemCatalog.WoodBoard, Balance.DoorRepairPerBoard);
            DoorButton(s, ItemCatalog.WoodStick, Balance.DoorRepairPerStick);
            DoorButton(s, ItemCatalog.Tape, Balance.DoorRepairPerTape);
            GUILayout.EndHorizontal();
        }

        private void DoorButton(Survivor s, ItemDefinition material, double amount)
        {
            if (ActionButton($"{material.Name} +{amount:0} ({Count(material)})"))
                Later(() => session.RepairFrontDoor(s, material));
        }

        private void DrawPicker(Survivor s)
        {
            bool purify = picking == Picking.BodyPurify;
            GUILayout.Label(purify ? $"{Korean.EunNeun(s.Name)} 누구 몸을 정화할까?" : $"{Korean.EunNeun(s.Name)} 누구와 함께할까?", text);
            GUILayout.BeginHorizontal();
            foreach (var other in session.State.Survivors.Where(x => !x.IsDead && x.Location == SurvivorLocation.Home))
            {
                if (!purify && other == s) continue;
                if (ActionButton("→ " + other.Name))
                {
                    var target = other;
                    var mode = picking;
                    Later(() =>
                    {
                        picking = Picking.None;
                        if (mode == Picking.BodyPurify) session.PurifyBody(s, target);
                        else session.PlayHobby(s, target, mode == Picking.Hanafuda ? ItemCatalog.Hanafuda : ItemCatalog.PlayingCards);
                    });
                }
            }
            GUILayout.EndHorizontal();
            if (ActionButton("취소")) Later(() => picking = Picking.None);
        }

        private void DrawStorage(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(Inset(r));
            GUILayout.Label("공용 창고", text);
            storageScroll = GUILayout.BeginScrollView(storageScroll);
            foreach (var category in StorageOrder)
            {
                var items = ItemCatalog.All.Where(i => i.Category == category && Count(i) > 0).ToList();
                if (items.Count == 0) continue;
                GUILayout.Label($"[{CategoryName(category)}]", small);
                foreach (var item in items) GUILayout.Label($"  {item.Name}  {Count(item)}", small);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLog(Rect r)
        {
            GUI.Box(r, "");
            GUILayout.BeginArea(Inset(r));
            if (session.Log.Count != shownLogCount)
            {
                shownLogCount = session.Log.Count;
                logScroll = new Vector2(0, 1000000f); // 새 글이 오면 맨 아래로
            }
            logScroll = GUILayout.BeginScrollView(logScroll);
            foreach (var line in session.Log) GUILayout.Label(line, small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ── 도우미 ─────────────────────────────────────────────────────

        private bool ActionButton(string label) => GUILayout.Button(label, button, GUILayout.MinHeight(40));

        private int Count(ItemDefinition item) => session.State.Storage.Count(item.Id);

        private static readonly Color Highlight = new Color(1f, 0.8f, 0.35f);

        private static string SpeedName(double speed) =>
            speed <= 0 ? "멈춤" : Math.Abs(speed - 1) < 1e-9 ? "보통" : $"×{speed:0}";

        private static string PlaceName(Place p)
        {
            switch (p)
            {
                case Place.Rooftop: return "옥상";
                case Place.RooftopRoom: return "옥탑방";
                case Place.Floor3: return "3층";
                case Place.Floor2: return "2층";
                case Place.Floor1: return "1층";
                default: return "지하";
            }
        }

        private static string PlaceDescription(Place p)
        {
            switch (p)
            {
                case Place.Rooftop: return "옥상: 빗물받이, 도르래(외출과 파견)";
                case Place.RooftopRoom: return "옥탑방: 모닥불과 가스버너, 먹고 마시기, 함께 놀기";
                case Place.Floor3: return "3층 (아이콘): 작업대 대부분, 정화제 만들기, 정화";
                case Place.Floor2: return "2층 (아이콘): 작업대 일부";
                case Place.Floor1: return "1층 (아이콘): 현관 바리케이드";
                default: return "지하 (이미지): 바이오쿨러";
            }
        }

        private static string CategoryName(ItemCategory c)
        {
            switch (c)
            {
                case ItemCategory.Water: return "물";
                case ItemCategory.Food: return "음식";
                case ItemCategory.Medical: return "의료";
                case ItemCategory.Fuel: return "연료";
                case ItemCategory.Material: return "재료";
                case ItemCategory.Tool: return "도구";
                case ItemCategory.Seasoning: return "조미료";
                default: return "취미";
            }
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            // 한글이 들어 있는 운영체제 글꼴을 쓴다. 없으면 Unity 기본 글꼴로 그린다.
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial" }, 16);

            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { font = font, fontSize = 14, wordWrap = true };
            small.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 15, wordWrap = true };
            tab = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, fontStyle = FontStyle.Bold };
            card = new GUIStyle(GUI.skin.button) { font = font, fontSize = 15, wordWrap = true, alignment = TextAnchor.UpperLeft };
            card.padding = new RectOffset(10, 10, 8, 8);
        }
    }
}
