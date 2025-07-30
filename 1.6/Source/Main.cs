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

// >= 1 hour (2500 ticks)
private const int   BuffThresholdTicks = 2500;
// Joy gain per tick (0.000144f) to match ~36% recreation per in-game hour
// This is based on the vanilla meditative joy gain rate
private const float JoyGainPerTick     = 0.000144f;

namespace FishingIsFun {
    public class ModEntry : Mod {
        public ModEntry(ModContentPack content) : base(content) {
            // <<<— this will show up in your RimWorld log at startup
            Log.Message("[FishingIsFun] ⚓ ModEntry constructor running - Fishing Is Fun loaded!");

            var harmony = new Harmony("jalapenolabs.rimworld.fishingisfun");
            harmony.PatchAll(); 
        }
    }
}

[HarmonyPatch]
public static class Patch_JobDriver_Fish_AddRecreation {
    // Target the MakeNewToils method of JobDriver_Fish
    static MethodBase TargetMethod() {
        var type = AccessTools.TypeByName("JobDriver_Fish");
        if (type == null) throw new Exception("JobDriver_Fish not found. Ensure Odyssey is loaded.");
        return AccessTools.Method(type, "MakeNewToils");
    }

    // Postfix to inject recreation gain and mood buff
    static void Postfix(JobDriver_Fish __instance, ref IEnumerable<Toil> __result) {
        if (__result == null) return;
        var toils = __result.ToList();
        if (!toils.Any()) return;

        // **Logging**: confirm patch execution and number of toils
        Log.Message($"[FishingIsFun] Patching JobDriver_Fish.MakeNewToils – found {toils.Count} toils. Injecting recreation gain.");

        // Heuristic: assume last toil is the active fishing action
        for (int i = 0; i < toils.Count; i++) {
            Log.Message($"[FishingIsFun] Toil #{i} – CompleteMode: {toils[i].defaultCompleteMode}");
        }
        
        // The fishing toil return should always be 3 toils, however if there is any chance that there is less this is a simple crash guard protection
        if (toils.Count < 2) {
            Log.Error("[FishingIsFun] Unexpected toil count < 2, skipping recreation patch");
            __result = toils;
            return;
        }

        var fishingToil = toils[toils.Count - 2];
        var pawn = __instance.pawn;
        var joyNeed = pawn.needs?.joy;
        if (joyNeed == null) {
            // **Logging**: pawn has no joy need (e.g. animals); skip patch
            Log.Warning($"[FishingIsFun] Pawn {pawn} has no joy/recreation need. Skipping recreation patch.");
            __result = toils;
            return;
        }

        long startTick = -1;

        // Record start time of fishing
        fishingToil.initAction += () => {
            startTick = Find.TickManager.TicksGame;
            Log.Message($"[FishingIsFun] {pawn.LabelShort} started fishing at tick {startTick}.");
        };

        // Recreation gain each tick
        fishingToil.tickAction += () => {
            // Small joy gain per tick (0.000144f); scaled as Meditative joy
            // This is to match the ~36 % recreation per in‑game hour based on vanilla meditative joy gain
            joyNeed.GainJoy(JoyGainPerTick, JoyKindDefOf.Meditative);
            // **Logging**: periodically log joy to see it increasing
            if (Find.TickManager.TicksGame % 500 == 0) {
                Log.Message($"[FishingIsFun] {pawn.LabelShort} is fishing... Recreation now {joyNeed.CurLevel:P0}.");
            }
        };

        // On finish, grant mood buff if fished
        fishingToil.AddFinishAction(() => {
            if (startTick < 0) {
                Log.Message("[FishingIsFun] FinishAction called but startTick was not set.");
                return;
            }
            long ticksFishing = Find.TickManager.TicksGame - startTick;
            if (ticksFishing >= BuffThresholdTicks) {
                Log.Message($"[FishingIsFun] {pawn.LabelShort} fished for {ticksFishing} ticks (>=2500). Adding PleasantFishingTrip memory.");
                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
            } else {
                Log.Message($"[FishingIsFun] {pawn.LabelShort} fished for {ticksFishing} ticks (<2500). No mood buff given.");
            }
        });

        __result = toils;
    }
}
