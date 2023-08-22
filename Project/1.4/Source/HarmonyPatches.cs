using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using HarmonyLib;
using Verse.AI.Group;
using RimWorld.Planet;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmonyInstance = new Harmony("com.NewRatkin.rimworld.mod");
			harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		}

        [HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult))]
        [HarmonyPatch(nameof(LifeStageWorker_HumanlikeAdult.Notify_LifeStageStarted))]
        static class LifeStageWorker_HumanlikeAdult_Notify_LifeStageStarted_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(Pawn pawn, LifeStageDef previousLifeStage)
            {
                if (Current.ProgramState != ProgramState.Playing) return false;
                if (Faction.OfPlayer.def != RatkinFactionDefOf.RK_PlayerTribe) return true;

                if (pawn.Spawned && previousLifeStage != null && previousLifeStage.developmentalStage.Juvenile())
                {
                    EffecterDefOf.Birthday.SpawnAttached(pawn, pawn.Map);
                }
                if (pawn.story.bodyType == BodyTypeDefOf.Child || pawn.story.bodyType == BodyTypeDefOf.Baby)
                {
                    pawn.apparel?.DropAllOrMoveAllToInventory((Apparel apparel) => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));
                    BodyTypeDef bodyTypeFor = PawnGenerator.GetBodyTypeFor(pawn);
                    pawn.story.bodyType = bodyTypeFor;
                    pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                }
                if (!pawn.IsColonist)
                {
                    return false;
                }
                List<BackstoryCategoryFilter> backstoryCategories = new List<BackstoryCategoryFilter> { new BackstoryCategoryFilter { categories = new List<string> { "AdultTribal" }}};
                if (previousLifeStage.developmentalStage.Juvenile())
                {
                    if (pawn.ageTracker.vatGrowTicks >= 1200000)
                    {
                        PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, new List<BackstoryCategoryFilter> { new BackstoryCategoryFilter { categories = new List<string> { "VatGrown" }}}, pawn.Faction?.def);
                    }
                    else
                    {
                        BackstoryDef backstory = pawn.story.GetBackstory(BackstorySlot.Childhood);
                        if (backstory != null && backstory.IsPlayerColonyChildBackstory)
                        {
                            PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, backstoryCategories, pawn.Faction?.def);
                        }
                    }
                }
                if (pawn.story.GetBackstory(BackstorySlot.Adulthood) == null)
                {
                    PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Adulthood, backstoryCategories, pawn.Faction?.def);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(LifeStageWorker_HumanlikeChild))]
        [HarmonyPatch(nameof(LifeStageWorker_HumanlikeChild.Notify_LifeStageStarted))]
        static class LifeStageWorker_HumanlikeChild_Notify_LifeStageStarted_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(Pawn pawn, LifeStageDef previousLifeStage)
            {
                if (Current.ProgramState != ProgramState.Playing) return false;
                if (Faction.OfPlayer.def != RatkinFactionDefOf.RK_PlayerTribe) return true;

                if (previousLifeStage == null || !previousLifeStage.developmentalStage.Baby())
                {
                    return false;
                }
                if (pawn.story.bodyType != BodyTypeDefOf.Child)
                {
                    pawn.apparel?.DropAllOrMoveAllToInventory((Apparel apparel) => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child));
                    BodyTypeDef bodyTypeFor = PawnGenerator.GetBodyTypeFor(pawn);
                    pawn.story.bodyType = bodyTypeFor;
                    pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                }
                if (ModsConfig.IdeologyActive && pawn.Faction != null)
                {
                    Pawn pawn2 = pawn.GetMother();
                    if (pawn2?.Faction != pawn.Faction)
                    {
                        Pawn father;
                        pawn2 = (((father = pawn.GetFather()) == null || father.Faction != pawn.Faction) ? null : father);
                    }
                    if (pawn2 != null && pawn2.IsSlave)
                    {
                        pawn.guest.SetGuestStatus(pawn.Faction, GuestStatus.Slave);
                    }
                }
                bool flag = pawn.Ideo == null && pawn.ideo.TryJoinIdeoFromExposures();
                if (!ModsConfig.IdeologyActive)
                {
                    flag = false;
                }
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    List<WorkTypeDef> list = new List<WorkTypeDef>();
                    List<LifeStageWorkSettings> lifeStageWorkSettings = pawn.RaceProps.lifeStageWorkSettings;
                    for (int i = 0; i < lifeStageWorkSettings.Count; i++)
                    {
                        if (lifeStageWorkSettings[i].minAge <= pawn.ageTracker.AgeBiologicalYears)
                        {
                            list.Add(lifeStageWorkSettings[i].workType);
                        }
                    }
                    TaggedString text = "LetterBecameChild".Translate(pawn) + "\n\n" + list.Select((WorkTypeDef wt) => wt.labelShort.CapitalizeFirst()).ToLineList(" - ") + "\n\n" + "LetterBecameChildChanges".Translate();
                    if (ModsConfig.IdeologyActive)
                    {
                        text += "\n\n" + ((flag && !Find.IdeoManager.classicMode) ? ("LetterChildFollowIdeo".Translate(pawn, pawn.Ideo) + "\n\n") : TaggedString.Empty) + "LetterChildLegalStatus".Translate(pawn);
                        ChoiceLetter_BabyToChild choiceLetter_BabyToChild = (ChoiceLetter_BabyToChild)LetterMaker.MakeLetter("LetterLabelBecameChild".Translate(pawn), text, LetterDefOf.BabyToChild, pawn);
                        choiceLetter_BabyToChild.Start();
                        Find.LetterStack.ReceiveLetter(choiceLetter_BabyToChild);
                    }
                    else
                    {
                        ChoiceLetter let = LetterMaker.MakeLetter("LetterLabelBecameChild".Translate(pawn), text, LetterDefOf.PositiveEvent, pawn);
                        Find.LetterStack.ReceiveLetter(let);
                    }
                    if (pawn.Spawned)
                    {
                        EffecterDefOf.Birthday.SpawnAttached(pawn, pawn.Map);
                    }
                }
                List<BackstoryCategoryFilter> backstoryCategories = new List<BackstoryCategoryFilter> { new BackstoryCategoryFilter { categories = new List<string> { "Child" }}};
                if (pawn.IsColonist)
                {
                    backstoryCategories = LifeStageWorker_HumanlikeChild.ChildTribalBackstoryFilters;
                }
                PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, backstoryCategories, pawn.Faction?.def);

                return false;
            }
        }
    }
}