﻿using LabFusion.Data;
using LabFusion.Representation;
using LabFusion.Syncables;
using LabFusion.Utilities;

using MelonLoader;

using System.Collections;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Audio;
using LabFusion.Marrow;

namespace LabFusion.Network
{
    public class DespawnResponseData : IFusionSerializable
    {
        public const int Size = sizeof(ushort) + sizeof(byte) * 2;

        public ushort syncId;
        public byte despawnerId;
        public bool isMag;

        public void Serialize(FusionWriter writer)
        {
            writer.Write(syncId);
            writer.Write(despawnerId);
            writer.Write(isMag);
        }

        public void Deserialize(FusionReader reader)
        {
            syncId = reader.ReadUInt16();
            despawnerId = reader.ReadByte();
            isMag = reader.ReadBoolean();
        }

        public static DespawnResponseData Create(ushort syncId, byte despawnerId, bool isMag = false)
        {
            return new DespawnResponseData()
            {
                syncId = syncId,
                despawnerId = despawnerId,
                isMag = isMag,
            };
        }
    }

    [Net.DelayWhileTargetLoading]
    public class DespawnResponseMessage : FusionMessageHandler
    {
        public override byte? Tag => NativeMessageTag.DespawnResponse;

        public override void HandleMessage(byte[] bytes, bool isServerHandled = false)
        {
            // Despawn the poolee if it exists
            using var reader = FusionReader.Create(bytes);
            var data = reader.ReadFusionSerializable<DespawnResponseData>();
            MelonCoroutines.Start(Internal_WaitForValidDespawn(data.syncId, data.despawnerId, data.isMag));
        }

        private static IEnumerator Internal_WaitForValidDespawn(ushort syncId, byte despawnerId, bool isMag)
        {
            // Delay at most 300 frames until this syncable exists
            int i = 0;
            while (!SyncManager.HasSyncable(syncId))
            {
                yield return null;

                i++;

                if (i >= 300)
                    break;
            }

            // Get the syncable from the valid id
            if (SyncManager.TryGetSyncable<PropSyncable>(syncId, out var syncable))
            {
                PooleeUtilities.CanDespawn = true;

                if (syncable.Poolee && syncable.Poolee.gameObject.activeInHierarchy)
                {
                    if (isMag)
                    {
                        AmmoInventory ammoInventory = AmmoInventory.Instance;

                        if (PlayerRepManager.TryGetPlayerRep(despawnerId, out var rep))
                        {
                            ammoInventory = rep.RigReferences.RigManager.GetComponentInChildren<AmmoInventory>(true);
                        }

                        SafeAudio3dPlayer.PlayAtPoint(ammoInventory.ammoReceiver.grabClips, ammoInventory.ammoReceiver.transform.position, Audio3dManager.softInteraction, 0.2f);

                        syncable.Poolee.gameObject.SetActive(false);
                    }
                    else
                    {
                        syncable.Poolee.Despawn();
                    }
                }

                SyncManager.RemoveSyncable(syncable);

                PooleeUtilities.CanDespawn = false;
            }
        }
    }
}
