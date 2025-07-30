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
            // <<<— will log on startup to confirm the DLL is loaded
            Log.Message("[FishingIsFun] ⚓ ModEntry constructor running - Fishing Is Fun loaded!");

            var harmony = new Harmony("jalapenolabs.rimworld.fishingisfun");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patch_JobDriver_Fish_AddRecreation {
        // === Constants ===
        private const int   BuffThresholdTicks = 2500;    // >= 1 in‑game hour
        private const float JoyGainPerTick     = 0.000024f;// ~6% recreation per hour
        private const int   LogIntervalTicks   = 500;     // debug log every X fishing ticks

        // Target the Odyssey fishing job’s MakeNewToils() method
        static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("JobDriver_Fish");
            if (type == null) {
                throw new Exception("JobDriver_Fish not found. Ensure Odyssey is loaded.");
            }
            return AccessTools.Method(type, "MakeNewToils");
        }

        // Postfix: add rec gain and mood buff
        static void Postfix(JobDriver_Fish __instance, ref IEnumerable<Toil> __result) {
            if (__result == null) {
                return;
            };
            var toils = __result.ToList();

            // Log & safety check
            Log.Message($"[FishingIsFun] Patching JobDriver_Fish.MakeNewToils - found {toils.Count} toils");
            if (toils.Count < 2) {
                Log.Error("[FishingIsFun] Unexpected toil count < 2; skipping patch");
                __result = toils;
                return;
            }
            for (int i = 0; i < toils.Count; i++) {
                Log.Message($"[FishingIsFun] Toil #{i} - CompleteMode: {toils[i].defaultCompleteMode}");
            }

            // The second‑to‑last toil is the actual fishing loop
            var fishingToil = toils[toils.Count - 2];
            var pawn        = __instance.pawn;
            var joyNeed     = pawn.needs?.joy;
            if (joyNeed == null) {
                // Log.Warning($"[FishingIsFun] Pawn {pawn} has no joy need; skipping recreation patch");
                __result = toils;
                return;
            }

            // Counter for only active fishing ticks
            int ticksFished = 0;
            bool hasStartedFishing = false;

            // Reset counter when fishing starts
            fishingToil.initAction += () => {
                // only reset on the very first entry into the fishing loop
                if (!hasStartedFishing) {
                    hasStartedFishing = true;
                    ticksFished = 0;
                    Log.Message($"[FishingIsFun] {pawn.LabelShort} started fishing (counter reset)");
                }
            };
            // Each tick of actual fishing
            fishingToil.tickAction += () => {
                ticksFished++;
                joyNeed.GainJoy(JoyGainPerTick, JoyKindDefOf.Meditative);

                // Debug log periodically
                if (ticksFished % LogIntervalTicks == 0) {
                    Log.Message($"[FishingIsFun] {pawn.LabelShort} fished {ticksFished} ticks; recreation now {joyNeed.CurLevel:P0}");
                }
            };

            // When the fishing loop ends, grant buff if threshold met
            fishingToil.AddFinishAction(() => {
                if (ticksFished >= BuffThresholdTicks) {
                    Log.Message($"[FishingIsFun] {pawn.LabelShort} fished {ticksFished} ticks (>= {BuffThresholdTicks}); granting PleasantFishingTrip");
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
                } else {
                    Log.Message($"[FishingIsFun] {pawn.LabelShort} fished {ticksFished} ticks (< {BuffThresholdTicks}); no buff");
                }
            });

            __result = toils;
        }
    }
}
