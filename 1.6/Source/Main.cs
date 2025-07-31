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
using UnityEngine;
using Verse;
using Verse.AI;


namespace FishingIsFun {
    // Mod entry point & definition
    public class ModEntry : Mod {
        // Static reference to settings for easy access in patches
        public static FishingIsFunSettings Settings;

        public ModEntry(ModContentPack content) : base(content) {
            // Will log on startup to confirm the DLL is loaded
            Settings = GetSettings<FishingIsFunSettings>();

            var harmony = new Harmony("jalapenolabs.rimworld.fishingisfun");
            harmony.PatchAll();

            Log.Message("⚓🐟 Fishing Is Fun mod was successfully loaded!");
        }

        // Provide a name for the mod settings category
        public override string SettingsCategory() => "Fishing Is Fun";

        // Draw the settings window contents
        public override void DoSettingsWindowContents(Rect inRect) {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Checkbox for mood buff on/off
            listing.CheckboxLabeled(
                "Enable mood buff", 
                ref Settings.enableMoodBuff
            );

            // Label and slider for the hours needed for the mood buff
            listing.Label($"Hours of fishing required for a mood buff: {Settings.hoursNeededForBuff} hour(s)");
            // Slider returns a float; round it to int for whole hours
            float hours = Settings.hoursNeededForBuff;

            // Range 0 to 12 hours
            hours = listing.Slider(hours, 0f, 12f);

            // Round the value to nearest 0.5 hour
            Settings.hoursNeededForBuff = Mathf.Round(hours * 2f) / 2f;

            // Checkbox for recreation gain on/off
            listing.CheckboxLabeled(
                "Enable recreation gain", 
                ref Settings.enableRecreationGain
            );

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    // /////////////////////////// //
    //       Mod Settings       //
    // /////////////////////////// //

    public class FishingIsFunSettings : ModSettings {
        // The configurable hours threshold (default 3 hours)
        public bool enableMoodBuff = true;
        public float hoursNeededForBuff = 3;
        public bool enableRecreationGain = true;

        // Save/load the setting value
        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref enableMoodBuff, "enableMoodBuff", true);
            Scribe_Values.Look(ref hoursNeededForBuff, "hoursNeededForBuff", 3);
            Scribe_Values.Look(ref enableRecreationGain, "enableRecreationGain", true);
        }
    }

    // /////////////////////////// //
    //       Harmony Patches       //
    // /////////////////////////// //

    [HarmonyPatch]
    public static class Patch_JobDriver_Fish_AddRecreation {
        // === Constants ===
        private const int TicksPerHour = 2500; // >= 1 in‑game hours
        private const int TicksPerHalfHour = TicksPerHour / 2; // 1250 ticks = 30 minutes
        private const float JoyGainPerTick = 0.000024f;// ~6% recreation per hour
        private static int BuffThresholdTicks => Mathf.RoundToInt(TicksPerHour * (ModEntry.Settings?.hoursNeededForBuff ?? 3f));

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

                // If the user set hours to 0 to have the buff be instant...
                if (ModEntry.Settings?.enableMoodBuff == true && BuffThresholdTicks == 0) {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
                }
                // Log.Message($"[FishingIsFun] {pawn.LabelShort} started fishing (counter reset)");
            };

            // Each tick of actual fishing
            fishingToil.tickAction += () => {
                ticksFished++;
                if (ModEntry.Settings?.enableRecreationGain == true && joyNeed != null) {
                    joyNeed.GainJoy(JoyGainPerTick, JoyKindDefOf.Meditative);
                }

                // Once every 30 rimworld minutes
                if (ModEntry.Settings?.enableMoodBuff == true && ticksFished >= BuffThresholdTicks && ticksFished % TicksPerHalfHour == 0) {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PleasantFishingTrip"));
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
