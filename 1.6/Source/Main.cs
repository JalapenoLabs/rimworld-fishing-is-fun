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
            new Harmony("jalapenolabs.fishingisfun").PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patch_JobDriver_Fish_AddRecreation {
        // Target the MakeNewToils method of JobDriver_Fish directly
        static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("JobDriver_Fish");
            return type != null
                ? AccessTools.Method(type, "MakeNewToils")
                : throw new Exception("JobDriver_Fish not found. Ensure Odyssey is loaded.");
        }

        // Postfix to inject recreation gain and mood buff during the fishing toil
        static void Postfix(JobDriver_Fish __instance, ref IEnumerable<Toil> __result) {
            if (__result == null) return;

            var toils = __result.ToList();
            if (!toils.Any()) return;

            // Heuristic: last toil is active fishing
            var fishingToil = toils.Last();

            // Cache pawn and needs once to avoid reflection each tick
            var pawn = __instance.pawn;
            var joyNeed = pawn.needs?.joy;

            // If no joy need, skip all patches
            if (joyNeed == null) {
                __result = toils;
                return;
            }

            long startTick = -1;

            // Record start of fishing
            fishingToil.initAction += () => startTick = Find.TickManager.TicksGame;

            // Recreation gain per tick (captures joyNeed)
            fishingToil.tickAction += () => {
                joyNeed.GainJoy(0.001f, JoyKindDefOf.Meditative);
            };

            // On finish, grant mood buff if fished >= 1 hour
            fishingToil.AddFinishAction(() => {
                if (startTick < 0) return;
                if (Find.TickManager.TicksGame - startTick >= 2500) {
                    pawn.needs.mood.thoughts.memories
                        .TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
                }
            });

            __result = toils;
        }
    }
}
