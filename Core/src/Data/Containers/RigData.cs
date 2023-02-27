﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;


using SLZ.VRMK;
using SLZ;
using SLZ.Rig;
using SLZ.Interaction;

using BoneLib;

using UnityEngine;

using LabFusion.Utilities;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.Extensions;

using SLZ.Combat;
using SLZ.Data;
using SLZ.Marrow.Data;
using SLZ.Marrow.Warehouse;
using SLZ.AI;
using SLZ.UI;

namespace LabFusion.Data
{
    /// <summary>
    /// A collection of basic rig information for use across PlayerReps and the Main RigManager.
    /// </summary>
    public class RigReferenceCollection {
        public RigManager RigManager { get; private set; }

        public Grip[] RigGrips { get; private set; }
        public Rigidbody[] RigRigidbodies { get; private set; }

        public InventorySlotReceiver[] RigSlots { get; private set; }

        public Hand LeftHand { get; private set; }
        public Hand RightHand { get; private set; }

        public TriggerRefProxy Proxy { get; private set; }

        public BaseController LeftController { get; private set; }
        public BaseController RightController { get; private set; }

        public byte? GetIndex(Grip grip, bool isAvatarGrip = false) {
            var gripArray = RigGrips;

            if (isAvatarGrip)
                gripArray = GetAvatarGrips();

            for (byte i = 0; i < gripArray.Length; i++) {
                if (gripArray[i] == grip)
                    return i;
            }
            return null;
        }

        public Grip GetGrip(byte index, bool isAvatarGrip = false) {
            var gripArray = RigGrips;

            if (isAvatarGrip)
                gripArray = GetAvatarGrips();

            if (gripArray != null && gripArray.Length > index)
                return gripArray[index];
            return null;
        }

        internal Grip[] GetAvatarGrips() {
            return RigManager._avatar.GetComponentsInChildren<Grip>();
        }

        internal InventorySlotReceiver[] GetAvatarSlots() {
            return RigManager._avatar.GetComponentsInChildren<InventorySlotReceiver>();
        }

        // Rigidbody order likes to randomly change on players
        // So we have to disgustingly update it every index call
        internal void GetRigidbodies() {
            if (RigManager.IsNOC())
                return;

            RigRigidbodies = RigManager.physicsRig.GetComponentsInChildren<Rigidbody>(true);
        }

        public byte? GetIndex(Rigidbody rb)
        {
            GetRigidbodies();

            for (byte i = 0; i < RigRigidbodies.Length; i++)
            {
                if (RigRigidbodies[i] == rb)
                    return i;
            }
            return null;
        }

        public Rigidbody GetRigidbody(byte index)
        {
            GetRigidbodies();

            if (RigRigidbodies != null && RigRigidbodies.Length > index)
                return RigRigidbodies[index];
            return null;
        }

        public byte? GetIndex(InventorySlotReceiver slot, bool isAvatarSlot = false)
        {
            var slotArray = RigSlots;

            if (isAvatarSlot)
                slotArray = GetAvatarSlots();

            for (byte i = 0; i < slotArray.Length; i++)
            {
                if (slotArray[i] == slot)
                    return i;
            }
            return null;
        }

        public InventorySlotReceiver GetSlot(byte index, bool isAvatarSlot = false) {
            var slotArray = RigSlots;

            if (isAvatarSlot)
                slotArray = GetAvatarSlots();

            if (slotArray != null && slotArray.Length > index)
                return slotArray[index];
            return null;
        }

        public Hand GetHand(Handedness handedness) {
            switch (handedness) {
                default:
                    return null;
                case Handedness.LEFT:
                    return LeftHand;
                case Handedness.RIGHT:
                    return RightHand;
            }
        }

        public RigReferenceCollection() { }

        public RigReferenceCollection(RigManager rigManager) {
            RigManager = rigManager;
            RigGrips = rigManager.physicsRig.GetComponentsInChildren<Grip>(true);

            RigSlots = rigManager.GetComponentsInChildren<InventorySlotReceiver>(true);

            LeftHand = rigManager.physicsRig.m_handLf.GetComponent<Hand>();
            RightHand = rigManager.physicsRig.m_handRt.GetComponent<Hand>();

            Proxy = rigManager.GetComponentInChildren<TriggerRefProxy>(true);

            LeftController = rigManager.openControllerRig.leftController;
            RightController = rigManager.openControllerRig.rightController;
        }
    }

    public static class RigData
    {
        public static RigReferenceCollection RigReferences { get; private set; } = new RigReferenceCollection();

        public static string RigAvatarId { get; private set; } = AvatarWarehouseUtilities.INVALID_AVATAR_BARCODE;
        public static SerializedAvatarStats RigAvatarStats { get; private set; } = null;

        public static Vector3 RigSpawn { get; private set; }
        public static Quaternion RigSpawnRot { get; private set; }

        private static bool _wasPaused = false;

        public static void OnCacheRigInfo() {
            var manager = Player.rigManager;

            if (!manager) {
                return;
            }
            
            // Add player additions
            PlayerAdditionsHelper.OnCreatedRig(manager);

            if (NetworkInfo.HasServer) {
                PlayerAdditionsHelper.OnEnterServer(manager);
            }

            // Store spawn values
            RigSpawn = manager.transform.position;
            RigSpawnRot = manager.transform.rotation;

            // Store the references
            RigReferences = new RigReferenceCollection(manager);
            RigReferences.RigManager.bodyVitals.rescaleEvent += (BodyVitals.RescaleUI)OnSendVitals;

            // Notify hooks
            MultiplayerHooking.Internal_OnLocalPlayerCreated(manager);
        }

        public static void OnSendVitals() {
            // Send body vitals to network
            if (NetworkInfo.HasServer) {
                using (FusionWriter writer = FusionWriter.Create(PlayerRepVitalsData.Size)) {
                    using (PlayerRepVitalsData data = PlayerRepVitalsData.Create(PlayerIdManager.LocalSmallId, RigReferences.RigManager.bodyVitals)) {
                        writer.Write(data);

                        using (var message = FusionMessage.Create(NativeMessageTag.PlayerRepVitals, writer)) {
                            MessageSender.BroadcastMessageExceptSelf(NetworkChannel.Reliable, message);
                        }
                    }
                }
            }
        }

        public static void OnRigUpdate() {
            var rm = RigReferences.RigManager;

            if (!rm.IsNOC()) {
                var barcode = rm.AvatarCrate.Barcode;
                if (barcode != RigAvatarId) {
                    // Save the stats
                    RigAvatarStats = new SerializedAvatarStats(rm.avatar);

                    // Send switch message to notify the server
                    if (NetworkInfo.HasServer) {
                        using (FusionWriter writer = FusionWriter.Create(PlayerRepAvatarData.DefaultSize)) {
                            using (PlayerRepAvatarData data = PlayerRepAvatarData.Create(PlayerIdManager.LocalSmallId, RigAvatarStats, barcode)) {
                                writer.Write(data);

                                using (var message = FusionMessage.Create(NativeMessageTag.PlayerRepAvatar, writer)) {
                                    MessageSender.BroadcastMessageExceptSelf(NetworkChannel.Reliable, message);
                                }
                            }
                        }
                    }

                    RigAvatarId = barcode;
                }

                // Pause check incase the rigs decide to behave strangely
                if (!rm.openControllerRig.IsPaused && _wasPaused) {
                    rm.bodyVitals.CalibratePlayerBodyScale();
                }

                _wasPaused = rm.openControllerRig.IsPaused;

                // Update hands
                OnHandUpdate(RigReferences.LeftHand);
                OnHandUpdate(RigReferences.RightHand);
            }
        }

        public static void OnHandUpdate(Hand hand) {
            // Try fixing UI every once in a while
            if (NetworkInfo.HasServer && Time.frameCount % 720 == 0) {
                var uiInput = UIControllerInput.Cache.Get(hand.Controller.gameObject);

                // If the cursor target is disabled or doesn't exist then clear the list
                if (uiInput != null) {
                    var target = uiInput.CursorTarget;

                    if (target == null || !target.gameObject.activeInHierarchy)
                        uiInput._cursorTargetOverrides.Clear();
                }
            }
        }

        public static string GetAvatarBarcode() {
            var rm = RigReferences.RigManager;

            if (rm)
                return rm.AvatarCrate.Barcode;
            return AvatarWarehouseUtilities.INVALID_AVATAR_BARCODE;
        }
    }
}
