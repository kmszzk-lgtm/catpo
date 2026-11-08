using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class CraftingAndHouseworkTests
    {
        private GameState state;
        private Survivor jip, gun, ui, gil;

        [SetUp]
        public void SetUp()
        {
            state = GameState.NewTutorial();
            jip = state.Get("jipnyang");
            gun = state.Get("gunnyang");
            ui = state.Get("uinyang");
            gil = state.Get("gilnyang");
        }

        private int Count(ItemDefinition item) => state.Storage.Count(item.Id);

        private void RemoveAll(ItemDefinition item)
        {
            int n = Count(item);
            if (n > 0) state.Storage.Remove(item.Id, n);
        }

        // ── 시작 살림 ──────────────────────────────────────────────────

        [Test]
        public void 튜토리얼은_경비실_마스터_키로_모은_살림으로_시작한다()
        {
            Assert.That(Count(ItemCatalog.BottledWater), Is.EqualTo(6));
            Assert.That(Count(ItemCatalog.Butane), Is.EqualTo(5));
            Assert.That(Count(ItemCatalog.SoySauce), Is.GreaterThan(0));
            Assert.That(Count(ItemCatalog.VinylBag), Is.GreaterThan(0));
            Assert.That(state.Home.HasWorkingGasBurner, Is.True);

            double hungerRelief = 0;
            foreach (var kv in state.Storage.Items)
            {
                var item = ItemCatalog.Get(kv.Key);
                if (item.Category == ItemCategory.Food) hungerRelief += item.HungerRelief * kv.Value;
            }
            double oneDay = Balance.HungerPerHour * 24;
            Assert.That(hungerRelief / oneDay, Is.InRange(28.0, 32.0), "보존식은 1인 기준 30일치");
        }

        // ── 정화제 ─────────────────────────────────────────────────────

        [Test]
        public void 의냥이는_소독약으로_정화제를_만든다()
        {
            var result = CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromDisinfectant);
            Assert.That(result.Success, Is.True);
            Assert.That(Count(ItemCatalog.Disinfectant), Is.EqualTo(1));
            Assert.That(Count(ItemCatalog.Purifier), Is.EqualTo(2));
            Assert.That(ui.Skills.Xp(Skill.Medical), Is.EqualTo(20 * 0.8).Within(1e-9), "태움: 집 안 경험치 조금 불이익");
        }

        [Test]
        public void 정화제_제작은_만드는_사람의_실력으로_빨라진다()
        {
            jip.Skills.SetLevel(Skill.Medical, Balance.PurifierMedicalLevel);
            double uiHours = CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromDisinfectant).Hours;
            double jipHours = CraftingRules.TryCraft(state, jip, RecipeCatalog.PurifierFromDisinfectant).Hours;
            Assert.That(uiHours, Is.EqualTo(1 / (1.4 * 1.5)).Within(1e-9), "의료 레벨 5, 전문의");
            Assert.That(jipHours, Is.EqualTo(1 / 1.25 / 1.2).Within(1e-9), "생활력, 의료 레벨 3");
        }

        [Test]
        public void 의냥이는_정화제를_만들_때_재료를_아낀다()
        {
            state.Storage.Add(ItemCatalog.Disinfectant.Id, 2); // 모두 4개
            ActionResult last = null;
            for (int i = 0; i < 4; i++) last = CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromDisinfectant);
            Assert.That(last.SavedInput, Is.EqualTo(ItemCatalog.Disinfectant.Id), "절감 0.3씩 쌓여 4번째에 1개를 아낌");
            Assert.That(Count(ItemCatalog.Disinfectant), Is.EqualTo(1));
            Assert.That(Count(ItemCatalog.Purifier), Is.EqualTo(8));
        }

        [Test]
        public void 의료_레벨이_낮으면_정화제를_못_만든다()
        {
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.PurifierFromDisinfectant).Failure,
                Is.EqualTo(ActionFailure.MissingSkill));
            jip.Skills.SetLevel(Skill.Medical, Balance.PurifierMedicalLevel);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.PurifierFromDisinfectant).Success, Is.True);
        }

        [Test]
        public void 재료가_없거나_집에_없으면_못_만든다()
        {
            Assert.That(CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromBleach).Failure,
                Is.EqualTo(ActionFailure.MissingInputs), "끓인 물이 없음");
            state.Advance(2 * 60);
            state.TryDepart(ui, Direction.South);
            Assert.That(CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromLiquor).Failure,
                Is.EqualTo(ActionFailure.NotAtHome));
        }

        [Test]
        public void 정화제는_누가_써_줘도_효과가_같고_감염은_그대로다()
        {
            InfectionRules.AddContamination(gun, 40); // 41
            InfectionRules.Bite(jip);
            InfectionRules.AddContamination(jip, 40); // 41
            state.Storage.Add(ItemCatalog.Purifier.Id, 2);

            Assert.That(HouseworkRules.PurifyBody(state, ui, gun).Success, Is.True);
            Assert.That(HouseworkRules.PurifyBody(state, gil, jip).Success, Is.True);
            Assert.That(gun.Contamination, Is.EqualTo(41 - Balance.PurifierReduction).Within(1e-9));
            Assert.That(jip.Contamination, Is.EqualTo(41 - Balance.PurifierReduction).Within(1e-9));
            Assert.That(jip.IsInfected, Is.True);
        }

        [Test]
        public void 집_정화가_끊기면_집에서도_바깥처럼_오염도가_오른다()
        {
            state.Home.HouseProtectedUntilMinute = state.Time.TotalMinutes;
            state.Advance(2 * 60);
            Assert.That(jip.Contamination, Is.EqualTo(1 + 2 * Balance.ContaminationPerOutsideHour).Within(1e-9));

            state.Storage.Add(ItemCatalog.Purifier.Id);
            Assert.That(HouseworkRules.PurifyHouse(state, ui).Success, Is.True);
            state.Advance(2 * 60);
            Assert.That(jip.Contamination, Is.EqualTo(1 + 2 * Balance.ContaminationPerOutsideHour).Within(1e-9));
        }

        [Test]
        public void 집_정화는_하루가_지나면_다시_해야_한다()
        {
            state.Advance(24 * 60 - 1);
            Assert.That(jip.Contamination, Is.EqualTo(1).Within(1e-9));
            state.Advance(60);
            Assert.That(jip.Contamination, Is.GreaterThan(1));
        }

        // ── 불과 요리 ──────────────────────────────────────────────────

        [Test]
        public void 불은_플레이어가_고른다()
        {
            state.Storage.Add(ItemCatalog.RainWater.Id, 4);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater).Failure, Is.EqualTo(ActionFailure.HeatNotChosen));
            Assert.That(state.Home.AvailableHeat(state.Storage), Is.EquivalentTo(new[] { HeatSource.Campfire, HeatSource.GasBurner }));

            var campfire = CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire);
            Assert.That(campfire.Heat, Is.EqualTo(HeatSource.Campfire));
            Assert.That(Count(ItemCatalog.Paper), Is.EqualTo(19));
            Assert.That(Count(ItemCatalog.Butane), Is.EqualTo(5), "모닥불을 골랐으니 부탄가스는 그대로");

            var burner = CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.GasBurner);
            Assert.That(burner.Heat, Is.EqualTo(HeatSource.GasBurner));
            Assert.That(Count(ItemCatalog.Butane), Is.EqualTo(4));
            Assert.That(state.Home.ButaneCharges, Is.EqualTo(Balance.ButaneChargesPerCan - 1));
            Assert.That(burner.Hours, Is.LessThan(campfire.Hours), "가스버너가 더 빠름");
            Assert.That(Count(ItemCatalog.BoiledWater), Is.EqualTo(4));
        }

        [Test]
        public void 가스버너는_부탄가스_100통쯤_쓰면_고장_난다()
        {
            Assert.That(state.Home.GasBurnerCansLeft, Is.EqualTo(100));
            state.Home.GasBurnerCansLeft = 1;
            for (int i = 0; i < Balance.ButaneChargesPerCan; i++)
                Assert.That(state.Home.UseHeat(state.Storage, HeatSource.GasBurner), Is.True);
            Assert.That(state.Home.HasWorkingGasBurner, Is.False);
            Assert.That(state.Home.CanUse(state.Storage, HeatSource.GasBurner), Is.False);
            Assert.That(Count(ItemCatalog.Butane), Is.EqualTo(4));
        }

        [Test]
        public void 탈_것이_없으면_모닥불을_쓸_수_없고_재료도_그대로다()
        {
            RemoveAll(ItemCatalog.Paper);
            RemoveAll(ItemCatalog.WoodStick);
            RemoveAll(ItemCatalog.WoodBoard);
            state.Storage.Add(ItemCatalog.RainWater.Id, 2);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire).Failure,
                Is.EqualTo(ActionFailure.NoHeat));
            Assert.That(Count(ItemCatalog.RainWater), Is.EqualTo(2));
        }

        [Test]
        public void 모닥불은_종이_나무_막대_나무_판_순으로_태운다()
        {
            RemoveAll(ItemCatalog.Paper);
            state.Storage.Add(ItemCatalog.RainWater.Id, 12);
            for (int i = 0; i < 4; i++) CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire);
            Assert.That(Count(ItemCatalog.WoodStick), Is.EqualTo(0));
            Assert.That(Count(ItemCatalog.WoodBoard), Is.EqualTo(2));
            CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire);
            CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater, HeatSource.Campfire);
            Assert.That(Count(ItemCatalog.WoodBoard), Is.EqualTo(1), "나무 판 1개는 요리 2번");
        }

        // ── 빗물 ───────────────────────────────────────────────────────

        [Test]
        public void 빗물받이는_비닐봉투_나무_막대_용기로_만들고_냄비는_그냥_둬도_된다()
        {
            Assert.That(HouseworkRules.BuildRainCollector(state, gil, ItemCatalog.PetBottle.Id).Success, Is.True);
            Assert.That(state.Home.RainCollectorCapacity, Is.EqualTo(Balance.RainWaterPerFunnelCollector));
            Assert.That(Count(ItemCatalog.PetBottle), Is.EqualTo(3));

            RemoveAll(ItemCatalog.WoodStick);
            Assert.That(HouseworkRules.BuildRainCollector(state, gil, ItemCatalog.PetBottle.Id).Failure,
                Is.EqualTo(ActionFailure.MissingInputs), "페트병만으로는 못 받음");
            Assert.That(HouseworkRules.BuildRainCollector(state, gil, ItemCatalog.Pot.Id).Success, Is.True);
            Assert.That(state.Home.RainCollectorCapacity,
                Is.EqualTo(Balance.RainWaterPerFunnelCollector + Balance.RainWaterPerPot));

            state.CollectRain();
            Assert.That(Count(ItemCatalog.RainWater), Is.EqualTo(state.Home.RainCollectorCapacity));
        }

        // ── 현관 ───────────────────────────────────────────────────────

        [Test]
        public void 현관은_매일_조금씩_상한다()
        {
            state.SleepUntilMorning();
            Assert.That(state.Home.FrontDoor, Is.EqualTo(Balance.FrontDoorMax - Balance.FrontDoorDailyDamage));
        }

        [Test]
        public void 가구를_분해해_나무_판과_막대로_현관을_보수한다()
        {
            int boards = Count(ItemCatalog.WoodBoard), sticks = Count(ItemCatalog.WoodStick);
            Assert.That(HouseworkRules.BreakFurniture(state, jip).Success, Is.True);
            Assert.That(Count(ItemCatalog.WoodBoard), Is.EqualTo(boards + Balance.BoardsPerFurniture));
            Assert.That(Count(ItemCatalog.WoodStick), Is.EqualTo(sticks + Balance.SticksPerFurniture));

            state.Home.FrontDoor = 50;
            HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.WoodBoard.Id);
            HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.WoodStick.Id);
            HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.Tape.Id);
            Assert.That(state.Home.FrontDoor,
                Is.EqualTo(50 + Balance.DoorRepairPerBoard + Balance.DoorRepairPerStick + Balance.DoorRepairPerTape));
            Assert.That(HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.Stone.Id).Failure, Is.EqualTo(ActionFailure.MissingInputs));
        }

        [Test]
        public void 집_안_가구가_바닥나면_밖에서_가져와야_한다()
        {
            ActionResult last = null;
            for (int i = 0; i < Balance.HomeFurniturePieces; i++)
            {
                last = HouseworkRules.BreakFurniture(state, jip);
                Assert.That(last.Success, Is.True);
            }
            Assert.That(last.NeedMaterialsFromOutside, Is.True);
            var more = HouseworkRules.BreakFurniture(state, jip);
            Assert.That(more.Failure, Is.EqualTo(ActionFailure.NothingLeft));
            Assert.That(more.NeedMaterialsFromOutside, Is.True);
        }

        [Test]
        public void 현관이_뚫리면_게임_오버다()
        {
            state.Home.FrontDoor = Balance.FrontDoorDailyDamage;
            state.SleepUntilMorning();
            Assert.That(state.IsWorldOver, Is.True);
            Assert.That(state.WorldOverReason, Is.EqualTo("무너진 현관"));
        }

        [Test]
        public void 집냥이는_생활력_덕분에_집안일이_빠르다()
        {
            double jipHours = HouseworkRules.BreakFurniture(state, jip).Hours;
            double gunHours = HouseworkRules.BreakFurniture(state, gun).Hours;
            Assert.That(jipHours, Is.LessThan(gunHours));
        }
    }
}
