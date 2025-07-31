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
        private const int   BuffThresholdTicks = 2500 * 3;    // >= 3 in‑game hours
        private const float JoyGainPerTick     = 0.000024f;// ~6% recreation per hour

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

            // Sanity check
            if (toils.Count < 2) {
                // Log.Message("[FishingIsFun] Unexpected toil count < 2; skipping patch cycle");
                __result = toils;
                return;
            }

            // The second‑to‑last toil is the actual fishing loop
            var fishingToil = toils[toils.Count - 2];
            var pawn        = __instance.pawn;
            var joyNeed     = pawn.needs?.joy;
            // if (joyNeed == null) {
            //     // Log.Warning($"[FishingIsFun] Pawn {pawn} has no joy need; skipping recreation patch");
            //     __result = toils;
            //     return;
            // }

            // Counter for only active fishing ticks
            int ticksFished = 0;
            bool hasStartedFishing = false;

            // Reset counter when fishing starts
            fishingToil.initAction += () => {
                if (hasStartedFishing) {
                    return;
                }
                hasStartedFishing = true;
                ticksFished = 0;
                // Log.Message($"[FishingIsFun] {pawn.LabelShort} started fishing (counter reset)");
            };

            // Each tick of actual fishing
            fishingToil.tickAction += () => {
                ticksFished++;
                if (joyNeed != null) {
                    joyNeed.GainJoy(JoyGainPerTick, JoyKindDefOf.Meditative);
                }

                if (ticksFished >= BuffThresholdTicks && ticksFished % 500 == 0) {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
                    // Debug log every 500 ticks
                    // Log.Message($"[FishingIsFun] {pawn.LabelShort} fished for {ticksFished} ticks; recreation is now {joyNeed.CurLevel:P0}");
                }
            };

            // When the fishing loop ends, grant buff if threshold met
            fishingToil.AddFinishAction(() => {
                // Ensure reset for next fishing session
                ticksFished = 0;
                hasStartedFishing = false;
            });

            __result = toils;
        }
    }
}
