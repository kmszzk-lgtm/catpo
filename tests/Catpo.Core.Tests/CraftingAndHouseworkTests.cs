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

        // ── 정화제 ─────────────────────────────────────────────────────

        [Test]
        public void 의냥이는_소독약으로_정화제를_만든다()
        {
            state.Storage.Add(ItemCatalog.Disinfectant.Id);
            var result = CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromDisinfectant);
            Assert.That(result.Success, Is.True);
            Assert.That(state.Storage.Count(ItemCatalog.Disinfectant.Id), Is.EqualTo(0));
            Assert.That(state.Storage.Count(ItemCatalog.Purifier.Id), Is.EqualTo(2));
            Assert.That(ui.Skills.Xp(Skill.Medical), Is.EqualTo(20 * 0.8).Within(1e-9), "태움: 집 안 경험치 조금 불이익");
        }

        [Test]
        public void 의료_레벨이_낮으면_정화제를_못_만들고_레벨이_오르면_만든다()
        {
            state.Storage.Add(ItemCatalog.Disinfectant.Id);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.PurifierFromDisinfectant).Failure,
                Is.EqualTo(ActionFailure.MissingSkill));
            jip.Skills.SetLevel(Skill.Medical, Balance.PurifierMedicalLevel);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.PurifierFromDisinfectant).Success, Is.True);
        }

        [Test]
        public void 재료가_없거나_집에_없으면_못_만든다()
        {
            Assert.That(CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromLiquor).Failure,
                Is.EqualTo(ActionFailure.MissingInputs));
            state.Advance(2 * 60);
            state.TryDepart(ui, Direction.South);
            state.Storage.Add(ItemCatalog.LiquorBottle.Id);
            Assert.That(CraftingRules.TryCraft(state, ui, RecipeCatalog.PurifierFromLiquor).Failure,
                Is.EqualTo(ActionFailure.NotAtHome));
        }

        [Test]
        public void 의냥이의_몸_정화는_오염도를_크게_낮추고_감염은_그대로_둔다()
        {
            InfectionRules.AddContamination(gun, 40); // 41
            InfectionRules.Bite(jip);
            state.Storage.Add(ItemCatalog.Purifier.Id, 2);

            Assert.That(HouseworkRules.PurifyBody(state, ui, gun).Success, Is.True);
            Assert.That(gun.Contamination, Is.EqualTo(1), "전문의 1.5 × 의료 레벨 5 → 20 × 2.1 = 42");

            InfectionRules.AddContamination(jip, 40); // 41
            Assert.That(HouseworkRules.PurifyBody(state, gil, jip).Success, Is.True);
            Assert.That(jip.Contamination, Is.EqualTo(21).Within(1e-9));
            Assert.That(jip.IsInfected, Is.True);
        }

        [Test]
        public void 집_정화가_끊기면_집에서도_오염도가_오르고_정화제로_되살린다()
        {
            state.Home.HousePurity = 0;
            state.Advance(2 * 60);
            Assert.That(jip.Contamination, Is.EqualTo(2).Within(1e-9));

            state.Storage.Add(ItemCatalog.Purifier.Id);
            Assert.That(HouseworkRules.PurifyHouse(state, ui).Success, Is.True);
            Assert.That(state.Home.HousePurity, Is.EqualTo(Balance.HousePurityPerPurifier));
            state.Advance(2 * 60);
            Assert.That(jip.Contamination, Is.EqualTo(2).Within(1e-9));
        }

        // ── 불과 요리 ──────────────────────────────────────────────────

        [Test]
        public void 물_끓이기는_가스버너와_부탄가스를_먼저_쓴다()
        {
            state.Storage.Add(ItemCatalog.RainWater.Id, 2);
            var result = CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Heat, Is.EqualTo(HeatSource.GasBurner));
            Assert.That(result.Hours, Is.EqualTo(Balance.GasBurnerCookHours / 1.25).Within(1e-9), "집냥이 생활력");
            Assert.That(state.Storage.Count(ItemCatalog.BoiledWater.Id), Is.EqualTo(2));
            Assert.That(state.Storage.Count(ItemCatalog.Butane.Id), Is.EqualTo(0), "부탄가스 1통을 뜯었다");
            Assert.That(state.Home.ButaneCharges, Is.EqualTo(Balance.ButaneChargesPerCan - 1));
            Assert.That(state.Home.GasBurnerDurability, Is.EqualTo(Balance.GasBurnerDurability - 1));
        }

        [Test]
        public void 가스버너가_고장_나면_모닥불에_종이를_태운다()
        {
            state.Home.GasBurnerDurability = 1;
            state.Storage.Add(ItemCatalog.RainWater.Id, 4);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater).Heat, Is.EqualTo(HeatSource.GasBurner));
            Assert.That(state.Home.HasWorkingGasBurner, Is.False);

            int paper = state.Storage.Count(ItemCatalog.Paper.Id);
            var result = CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater);
            Assert.That(result.Heat, Is.EqualTo(HeatSource.Campfire));
            Assert.That(result.Hours, Is.EqualTo(Balance.CampfireCookHours / 1.25).Within(1e-9));
            Assert.That(state.Storage.Count(ItemCatalog.Paper.Id), Is.EqualTo(paper - 1));
        }

        [Test]
        public void 탈_것이_없으면_물을_끓일_수_없고_재료도_그대로다()
        {
            state.Home.GasBurnerDurability = 0;
            state.Storage.Remove(ItemCatalog.Paper.Id, state.Storage.Count(ItemCatalog.Paper.Id));
            state.Storage.Remove(ItemCatalog.Wood.Id, state.Storage.Count(ItemCatalog.Wood.Id));
            state.Storage.Add(ItemCatalog.RainWater.Id, 2);
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater).Failure, Is.EqualTo(ActionFailure.NoHeat));
            Assert.That(state.Storage.Count(ItemCatalog.RainWater.Id), Is.EqualTo(2));
        }

        [Test]
        public void 나무_1개는_모닥불_요리_3번이다()
        {
            state.Home.GasBurnerDurability = 0;
            state.Storage.Remove(ItemCatalog.Paper.Id, state.Storage.Count(ItemCatalog.Paper.Id));
            state.Storage.Add(ItemCatalog.RainWater.Id, 8);
            for (int i = 0; i < 3; i++)
                Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater).Success, Is.True);
            Assert.That(state.Storage.Count(ItemCatalog.Wood.Id), Is.EqualTo(0));
            Assert.That(CraftingRules.TryCraft(state, jip, RecipeCatalog.BoilWater).Failure, Is.EqualTo(ActionFailure.NoHeat));
        }

        // ── 빗물 ───────────────────────────────────────────────────────

        [Test]
        public void 빗물받이를_만들면_비_오는_날_빗물이_모인다()
        {
            Assert.That(HouseworkRules.BuildRainCollector(state, gil).Success, Is.True);
            Assert.That(state.Home.RainCollectors, Is.EqualTo(1));
            Assert.That(state.Storage.Count(ItemCatalog.PlasticContainer.Id), Is.EqualTo(0));
            Assert.That(HouseworkRules.BuildRainCollector(state, gil).Failure, Is.EqualTo(ActionFailure.MissingInputs));

            state.CollectRain();
            Assert.That(state.Storage.Count(ItemCatalog.RainWater.Id), Is.EqualTo(Balance.RainWaterPerCollector));
        }

        // ── 현관 ───────────────────────────────────────────────────────

        [Test]
        public void 현관은_매일_조금씩_상한다()
        {
            state.SleepUntilMorning();
            Assert.That(state.Home.FrontDoor, Is.EqualTo(Balance.FrontDoorMax - Balance.FrontDoorDailyDamage));
        }

        [Test]
        public void 가구를_부숴_자재로_현관을_보수한다()
        {
            state.Home.FrontDoor = 50;
            Assert.That(HouseworkRules.BreakFurniture(state, jip).Success, Is.True);
            Assert.That(HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.BuildingMaterial.Id).Success, Is.True);
            Assert.That(state.Home.FrontDoor, Is.EqualTo(50 + Balance.DoorRepairPerMaterial));
            Assert.That(HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.Tape.Id).Success, Is.True);
            Assert.That(state.Home.FrontDoor, Is.EqualTo(50 + Balance.DoorRepairPerMaterial + Balance.DoorRepairPerTape));
            Assert.That(HouseworkRules.RepairFrontDoor(state, gun, ItemCatalog.BuildingMaterial.Id).Failure,
                Is.EqualTo(ActionFailure.MissingInputs));
        }

        [Test]
        public void 집_안_가구가_바닥나면_밖에서_가져와야_한다()
        {
            ActionResult last = null;
            for (int i = 0; i < Balance.HomeFurnitureMaterials; i++)
            {
                last = HouseworkRules.BreakFurniture(state, jip);
                Assert.That(last.Success, Is.True);
            }
            Assert.That(last.NeedMaterialsFromOutside, Is.True);
            var more = HouseworkRules.BreakFurniture(state, jip);
            Assert.That(more.Failure, Is.EqualTo(ActionFailure.NothingLeft));
            Assert.That(more.NeedMaterialsFromOutside, Is.True);
            Assert.That(state.Storage.Count(ItemCatalog.BuildingMaterial.Id), Is.EqualTo(Balance.HomeFurnitureMaterials));
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
