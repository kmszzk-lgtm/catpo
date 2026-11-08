using System;

namespace Catpo.Core
{
    /// <summary>
    /// 집안일과 치료 (docs/09-home-life.md). 모두 집에 있는 캐릭터가 공용 창고의 물건으로 한다.
    /// 걸리는 시간과 수치는 [제안].
    /// </summary>
    public static class HouseworkRules
    {
        /// <summary>빗물받이: 비닐이나 방수포를 플라스틱 용기와 결합한다 [확정].</summary>
        public static ActionResult BuildRainCollector(GameState state, Survivor s)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);

            string cover = state.Storage.Count(ItemCatalog.Vinyl.Id) > 0 ? ItemCatalog.Vinyl.Id
                : state.Storage.Count(ItemCatalog.Tarp.Id) > 0 ? ItemCatalog.Tarp.Id : null;
            if (cover == null || state.Storage.Count(ItemCatalog.PlasticContainer.Id) == 0)
                return ActionResult.Fail(ActionFailure.MissingInputs);

            state.Storage.Remove(cover);
            state.Storage.Remove(ItemCatalog.PlasticContainer.Id);
            state.Home.RainCollectors++;
            return Done(s, CraftKind.Housework, Skill.Crafting, 10, Balance.BuildRainCollectorHours);
        }

        /// <summary>
        /// 집 안 가구를 부숴 자재로 만든다 [확정]. 집 안 가구는 한정되어 있어서, 바닥나면 밖에서도 가져와야 한다는 메시지를 띄운다 [확정].
        /// </summary>
        public static ActionResult BreakFurniture(GameState state, Survivor s)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (state.Home.FurnitureMaterialsLeft <= 0)
                return ActionResult.Fail(ActionFailure.NothingLeft, needMaterialsFromOutside: true);

            state.Home.FurnitureMaterialsLeft--;
            state.Storage.Add(ItemCatalog.BuildingMaterial.Id);
            bool ranOut = state.Home.FurnitureMaterialsLeft == 0;
            var result = Done(s, CraftKind.Housework, Skill.Crafting, 5, Balance.BreakFurnitureHours);
            return new ActionResult(ActionFailure.None, result.Hours, needMaterialsFromOutside: ranOut);
        }

        /// <summary>현관 보수: 자재나 테이프로 막는다 [확정]. 현관이 뚫리면 게임 오버 [확정].</summary>
        public static ActionResult RepairFrontDoor(GameState state, Survivor s, string itemId)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);

            double amount;
            if (itemId == ItemCatalog.BuildingMaterial.Id) amount = Balance.DoorRepairPerMaterial;
            else if (itemId == ItemCatalog.Tape.Id) amount = Balance.DoorRepairPerTape;
            else return ActionResult.Fail(ActionFailure.MissingInputs);
            if (!state.Storage.Remove(itemId)) return ActionResult.Fail(ActionFailure.MissingInputs);

            state.Home.FrontDoor = Math.Min(Balance.FrontDoorMax, state.Home.FrontDoor + amount);
            return Done(s, CraftKind.Housework, Skill.Crafting, 5, Balance.DoorRepairHours);
        }

        /// <summary>집 정화: 정화제로 집 안 오염도가 오르지 않게 유지한다 [확정]. 수치는 [제안].</summary>
        public static ActionResult PurifyHouse(GameState state, Survivor s)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (!state.Storage.Remove(ItemCatalog.Purifier.Id)) return ActionResult.Fail(ActionFailure.MissingInputs);

            state.Home.HousePurity = Math.Min(Balance.HousePurityMax, state.Home.HousePurity + Balance.HousePurityPerPurifier);
            return Done(s, CraftKind.Medicine, Skill.Medical, 5, 1);
        }

        /// <summary>정화 효과 배율 = 의료 행위 특성(전문의) × 의료 레벨 [제안].</summary>
        public static double MedicalEffect(Survivor medic) =>
            medic.Effects.MedicalAction * (1 + (medic.Skills.Level(Skill.Medical) - 1) * Balance.MedicalEffectPerLevel);

        /// <summary>
        /// 몸 정화: 외출하고 돌아온 동료의 전신을 소독하고 항생제를 먹여 오염도를 낮춘다 [확정].
        /// 정화제는 오염도만 낮추고, 감염 상태는 그대로다 [확정].
        /// </summary>
        public static ActionResult PurifyBody(GameState state, Survivor medic, Survivor target)
        {
            var failure = CraftingRules.CheckCanActAtHome(medic);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (target.IsDead) return ActionResult.Fail(ActionFailure.Dead);
            if (target.Location != SurvivorLocation.Home) return ActionResult.Fail(ActionFailure.NotAtHome);
            if (!state.Storage.Remove(ItemCatalog.Purifier.Id)) return ActionResult.Fail(ActionFailure.MissingInputs);

            InfectionRules.ApplyPurifier(target, MedicalEffect(medic));
            return Done(medic, CraftKind.Medicine, Skill.Medical, 10, Balance.BodyPurifyHours);
        }

        private static ActionResult Done(Survivor s, CraftKind kind, Skill skill, double xp, double baseHours)
        {
            s.GainXp(skill, xp, atHome: true);
            StressRules.OnCraft(s, s.Definition.TasteFor(kind));
            return new ActionResult(ActionFailure.None, CraftingRules.HomeActionHours(s, baseHours));
        }
    }
}
