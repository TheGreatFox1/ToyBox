﻿using System;
using HarmonyLib;
using UnityModManagerNet;
using Kingmaker;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Alignments;
using UnityEngine;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;

namespace ToyBox {
    public static class AlignmentPatches {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(UnitAlignment), nameof(UnitAlignment.GetDirection))]
        static class UnitAlignment_GetDirection_Patch {
            static void Postfix(UnitAlignment __instance, ref Vector2 __result, AlignmentShiftDirection direction) {
                if (settings.toggleAlignmentFix) {
                    if (direction == AlignmentShiftDirection.NeutralGood) __result = new Vector2(0, 1);
                    if (direction == AlignmentShiftDirection.NeutralEvil) __result = new Vector2(0, -1);
                    if (direction == AlignmentShiftDirection.LawfulNeutral) __result = new Vector2(-1, 0);
                    if (direction == AlignmentShiftDirection.ChaoticNeutral) __result = new Vector2(1, 0);
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), "Set", new Type[] { typeof(Alignment), typeof(bool) })]
        static class UnitAlignment_Set_Patch {
            static void Prefix(UnitAlignment __instance, ref Kingmaker.Enums.Alignment alignment) {
                if (settings.togglePreventAlignmentChanges) {
                    if (__instance.m_Value != null)
                        alignment = (Kingmaker.Enums.Alignment)__instance.m_Value;
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), "Shift", new Type[] { typeof(AlignmentShiftDirection), typeof(int), typeof(IAlignmentShiftProvider) })]
        static class UnitAlignment_Shift_Patch {
            static bool Prefix(UnitAlignment __instance, AlignmentShiftDirection direction, ref int value, IAlignmentShiftProvider provider) {
                try {
                    if ((settings.togglePreventAlignmentChanges)) {
                        value = 0;
                    }

                    if ((settings.toggleAlignmentFix)) {
                        if (value == 0) {
                            return false;
                        }
                        Vector2 vector = __instance.m_Vector;
                        float num = (float)value / 50f;
                        var directionVector = Traverse.Create(__instance).Method("GetDirection", new object[] { direction }).GetValue<Vector2>();
                        Vector2 newAlignment = __instance.m_Vector + directionVector * num;
                        if (newAlignment.magnitude > 1f) {
                            //Instead of normalizing towards true neutral, normalize opposite to the alignment vector
                            //to prevent sliding towards neutral
                            newAlignment -= (newAlignment.magnitude - newAlignment.normalized.magnitude) * directionVector;
                        }
                        if (direction == AlignmentShiftDirection.TrueNeutral && (Vector2.zero - __instance.m_Vector).magnitude < num) {
                            newAlignment = Vector2.zero;
                        }
                        Traverse.Create(__instance).Property<Vector2>("Vector").Value = newAlignment;
                        Traverse.Create(__instance).Method("UpdateValue").GetValue();
                        //Traverse requires the parameter types to find interface parameters
                        Traverse.Create(__instance).Method("OnChanged",
                            new Type[] { typeof(AlignmentShiftDirection), typeof(Vector2), typeof(IAlignmentShiftProvider), typeof(bool) },
                            new object[] { direction, vector, provider, true }).GetValue();
                        return false;
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ForbidSpellbookOnAlignmentDeviation), "CheckAlignment")]
        static class ForbidSpellbookOnAlignmentDeviation_CheckAlignment_Patch {
            static bool Prefix(ForbidSpellbookOnAlignmentDeviation __instance) {
                if ((settings.toggleSpellbookAbilityAlignmentChecks)) {
                    __instance.Alignment = __instance.Owner.Alignment.ValueRaw.ToMask();
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(AbilityCasterAlignment), nameof(AbilityCasterAlignment.IsCasterRestrictionPassed))]
        static class AbilityCasterAlignment_CheckAlignment_Patch {
            static void Postfix(ref bool __result) {
                if ((settings.toggleSpellbookAbilityAlignmentChecks)) {
                    __result = true;
                }
            }
        }
    }
}