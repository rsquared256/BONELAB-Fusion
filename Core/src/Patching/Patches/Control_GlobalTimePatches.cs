﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using LabFusion.Network;
using LabFusion.Preferences;
using LabFusion.Senders;
using UnityEngine;

namespace LabFusion.Patching {
    [HarmonyPatch(typeof(Control_GlobalTime))]
    public static class Control_GlobalTimePatches {
        public static bool IgnorePatches = false;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Control_GlobalTime.DECREASE_TIMESCALE))]
        public static void DECREASE_TIMESCALE(Control_GlobalTime __instance) {
            if (IgnorePatches)
                return;

            if (NetworkInfo.HasServer) {
                var mode = FusionPreferences.TimeScaleMode;

                switch (mode) {
                    case TimeScaleMode.EVERYONE:
                        TimeScaleSender.SendSlowMoButton(true);
                        break;
                    case TimeScaleMode.HOST_ONLY:
                        if (NetworkInfo.IsServer)
                            TimeScaleSender.SendSlowMoButton(true);
                        break;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Control_GlobalTime.TOGGLE_TIMESCALE))]
        public static void TOGGLE_TIMESCALE(Control_GlobalTime __instance) {
            if (IgnorePatches)
                return;

            if (NetworkInfo.HasServer)
            {
                var mode = FusionPreferences.TimeScaleMode;

                switch (mode)
                {
                    case TimeScaleMode.EVERYONE:
                        TimeScaleSender.SendSlowMoButton(false);
                        break;
                    case TimeScaleMode.HOST_ONLY:
                        if (NetworkInfo.IsServer)
                            TimeScaleSender.SendSlowMoButton(false);
                        break;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Control_GlobalTime.SET_TIMESCALE))]
        public static void SET_TIMESCALE(Control_GlobalTime __instance, ref float intensity)
        {
            if (IgnorePatches)
                return;

            if (NetworkInfo.HasServer)
            {
                var mode = FusionPreferences.TimeScaleMode;

                switch (mode)
                {
                    case TimeScaleMode.LOW_GRAVITY:
                    case TimeScaleMode.DISABLED:
                        intensity = 1f;
                        break;
                    case TimeScaleMode.HOST_ONLY:
                    case TimeScaleMode.EVERYONE:
                        if (!NetworkInfo.IsServer && TimeScaleSender.ReceivedTimeScale > 0f)
                            intensity = 1f / TimeScaleSender.ReceivedTimeScale;
                        break;
                }
            }
        }
    }
}
