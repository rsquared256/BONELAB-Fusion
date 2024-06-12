﻿using UnityEngine;

using LabFusion.Network;
using LabFusion.Data;
using LabFusion.Syncables;
using LabFusion.Utilities;
using Il2CppSLZ.Marrow.Warehouse;

namespace LabFusion.Senders
{
    public static class SpawnSender
    {
        /// <summary>
        /// Sends a catchup for the OnPlaceEvent for a CrateSpawner.
        /// </summary>
        /// <param name="placer"></param>
        /// <param name="syncable"></param>
        /// <param name="userId"></param>
        public static void SendCratePlacerCatchup(CrateSpawner placer, PropSyncable syncable, ulong userId)
        {
            if (NetworkInfo.IsServer)
            {
                using var writer = FusionWriter.Create(SpawnableCratePlacerData.Size);
                var data = SpawnableCratePlacerData.Create(syncable.GetId(), placer.gameObject);
                writer.Write(data);

                using var message = FusionMessage.Create(NativeMessageTag.CrateSpawner, writer);
                MessageSender.SendFromServer(userId, NetworkChannel.Reliable, message);
            }
        }

        /// <summary>
        /// Sends the OnPlaceEvent for a CrateSpawner.
        /// </summary>
        /// <param name="placer"></param>
        /// <param name="go"></param>
        public static void SendCratePlacerEvent(CrateSpawner placer, GameObject go)
        {
            if (NetworkInfo.IsServer)
            {
                // Wait for the level to load and for 5 frames before sending messages
                FusionSceneManager.HookOnLevelLoad(() =>
                {
                    DelayUtilities.Delay(() =>
                    {
                        Internal_OnSendCratePlacer(placer, go);
                    }, 5);
                });
            }
        }

        private static void Internal_OnSendCratePlacer(CrateSpawner placer, GameObject go)
        {
            if (PropSyncable.Cache.TryGet(go, out var syncable))
            {
                using (var writer = FusionWriter.Create(SpawnableCratePlacerData.Size))
                {
                    var data = SpawnableCratePlacerData.Create(syncable.GetId(), placer.gameObject);
                    writer.Write(data);

                    using var message = FusionMessage.Create(NativeMessageTag.CrateSpawner, writer);
                    MessageSender.BroadcastMessageExceptSelf(NetworkChannel.Reliable, message);
                }

                // Insert the catchup hook for future users
                syncable.InsertCatchupDelegate((id) =>
                {
                    SendCratePlacerCatchup(placer, syncable, id);
                });
            }
        }


        /// <summary>
        /// Sends a catchup sync message for a pool spawned object.
        /// </summary>
        /// <param name="syncable"></param>
        public static void SendCatchupSpawn(byte owner, string barcode, ushort syncId, SerializedTransform serializedTransform, ulong userId)
        {
            if (NetworkInfo.IsServer)
            {
                using var writer = FusionWriter.Create(SpawnResponseData.GetSize(barcode));
                var data = SpawnResponseData.Create(owner, barcode, syncId, serializedTransform);
                writer.Write(data);

                using var message = FusionMessage.Create(NativeMessageTag.SpawnResponse, writer);
                MessageSender.SendFromServer(userId, NetworkChannel.Reliable, message);
            }
        }
    }
}
