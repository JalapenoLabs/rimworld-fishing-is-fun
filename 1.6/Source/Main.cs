/*************************************************************************
*
 * JALAPENO LABS
 * __________________
 *
 * [2025] Jalapeno Labs LLC
 * MIT License
 */


/*************************************************************************
*
 * JALAPENO LABS
 * __________________
 *
 * [2025] Jalapeno Labs LLC
 * MIT License
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FishingIsFun {
    public class ModEntry : Mod {
        public ModEntry(ModContentPack content) : base(content) {
            var harmony = new Harmony("jalapenolabs.fishingisfun");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patch_JobDriver_Fish_AddRecreation {
        // Dynamically find Odyssey's JobDriver_Fish.MakeNewToils
        static MethodBase TargetMethod() {
            // Common type names to try; Odyssey may use one of these
            var candidates = new[] {
                "JobDriver_Fish",
                "RimWorld.JobDriver_Fish"
            };

            foreach (var name in candidates) {
                var target = AccessTools.TypeByName(name);
                if (target != null) {
                    var toil = AccessTools.Method(target, "MakeNewToils");
                    if (toil != null){
                        return toil;
                    };
                }
            }

            Log.Error("[FishingIsFun] Could not find JobDriver_Fish.MakeNewToils. Is Odyssey enabled and loaded before this mod?");
            return null;
        }

        // Postfix to inject recreation gain and mood buff during the active fishing toil
        static void Postfix(object __instance, ref IEnumerable<Toil> __result) {
            if (__result == null) {
                return;
            };

            var toils = __result.ToList();
            if (toils.Count == 0) {
                return;
            };

            // Heuristic: the final toil is the long, “do the fishing” toil
            var fishingToil = toils.Last();
            long startTick = -1L;

            // Record when fishing starts
            fishingToil.initAction += () => {
                startTick = Find.TickManager.TicksGame;
            };

            // Append recreation gain to its tickAction
            fishingToil.tickAction += () => {
                // __instance is a JobDriver; get its pawn via reflection if needed
                var pawnField = __instance.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) {
                    return;
                };

                var joy = pawn.needs?.joy;
                if (joy == null) {
                    return;
                };

                // Small, fixed per-tick recreation; Meditative suits quiet fishing
                joy.GainJoy(0.001f, JoyKindDefOf.Meditative);
            };

            // On finish, give the PleasantFishingTrip thought if fished >= 1 hour
            fishingToil.AddFinishAction(() => {
                var pawnField = __instance.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) {
                    return;
                };

                if (Find.TickManager.TicksGame - startTick >= 2500) {
                    pawn.needs?.mood?.thoughts?.memories
                        .TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
                }
            });

            __result = toils;
        }
    }
}
