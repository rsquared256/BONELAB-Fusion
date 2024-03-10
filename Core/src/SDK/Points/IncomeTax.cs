﻿using LabFusion.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace LabFusion.SDK.Points
{
    public static class IncomeTax
    {
        // 10 million
        private const int _incomeTaxAmount = 10000000;

        public static bool CheckForIncomeTax(int amount)
        {
            return amount > _incomeTaxAmount;
        }

        public static void CollectTax()
        {
            FusionSceneManager.HookOnDelayedLevelLoad(() =>
            {
                string name = "Wacky Willy";

                FusionNotifier.Send(new FusionNotification()
                {
                    title = $"{name} Join",
                    message = $"{name} joined the server.",
                    isMenuItem = false,
                    isPopup = true,
                });

                FusionNotifier.Send(new FusionNotification()
                {
                    title = $"Income Tax",
                    message = $"{name} has collected your required tax.",
                    isMenuItem = false,
                    isPopup = true,
                });

                FusionContentLoader.BouncingStrong.Load((c) =>
                {
                    FusionAudio.Play2D(c, 1f);
                });

                FusionContentLoader.PurchaseFailure.Load((c) =>
                {
                    FusionAudio.Play2D(c, 1f);
                });

                Physics.gravity = new Vector3(0f, 0.1f, 0f);
            });
        }
    }
}
