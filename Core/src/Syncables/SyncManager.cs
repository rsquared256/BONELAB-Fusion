﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Extensions;
using LabFusion.Utilities;
using LabFusion.Representation;
using System.IdentityModel.Tokens;

namespace LabFusion.Syncables {
    public static class SyncManager {
        public static readonly Dictionary<ushort, ISyncable> Syncables = new Dictionary<ushort, ISyncable>(new SyncableComparer());

        public static readonly Dictionary<ushort, ISyncable> QueuedSyncables = new Dictionary<ushort, ISyncable>(new SyncableComparer());

        /// <summary>
        /// The last allocated id. Incremented server side.
        /// </summary>
        public static ushort LastId = 0;

        /// <summary>
        /// The last registered queue id. Only kept client side.
        /// </summary>
        public static ushort LastQueueId = 0;

        public static void RequestSyncableID(ushort queuedId) {
            if (NetworkInfo.HasServer) {
                if (NetworkInfo.IsServer) {
                    UnqueueSyncable(queuedId, AllocateSyncID(), out var syncable);
                }
                else
                {
                    using (var writer = FusionWriter.Create(SyncableIDRequestData.Size))
                    {
                        using (var data = SyncableIDRequestData.Create(PlayerIdManager.LocalSmallId, queuedId))
                        {
                            writer.Write(data);

                            using (var message = FusionMessage.Create(NativeMessageTag.SyncableIDRequest, writer))
                            {
                                MessageSender.BroadcastMessageExceptSelf(NetworkChannel.Reliable, message);
                            }
                        }
                    }
                }
            }
        }

        internal static void OnUpdate() {
            // Here we send over position information/etc of our syncables
            foreach (var syncable in Syncables.Values) {
                try {
                    syncable.OnUpdate();
                }
                catch (Exception e) {
#if DEBUG
                    FusionLogger.LogException("executing OnUpdate for syncable", e);
#endif
                }
            }
        }

        internal static void OnFixedUpdate() {
            // Here we update the positions/etc of all of our synced objects
            foreach (var syncable in Syncables) {
                try {
                    syncable.Value.OnFixedUpdate();
                }
                catch (Exception e) {
#if DEBUG
                    FusionLogger.LogException("executing OnFixedUpdate for syncable", e);
#endif
                }
            }
        }

        public static void OnCleanup() {
            foreach (var syncable in Syncables.Values) {
                try {
                    syncable.Cleanup();
                }
                catch (Exception e) {
#if DEBUG
                    FusionLogger.LogException("cleaning up Syncable", e);
#endif
                }
            }

            Syncables.Clear();

            foreach (var syncable in QueuedSyncables) {
                try {
                    syncable.Value.Cleanup();
                }
                catch (Exception e) {
#if DEBUG
                    FusionLogger.LogException("cleaning up QueuedSyncable", e);
#endif
                }
            }

            QueuedSyncables.Clear();

            LastId = 0;
            LastQueueId = 0;
        }

        public static ushort AllocateSyncID() {
            LastId++;

            // Safety check incase the id is being used
            if (Syncables.ContainsKey(LastId)) {
                while (Syncables.ContainsKey(LastId) && LastId < ushort.MaxValue) {
                    LastId++;
                }
            }

            return LastId;
        }

        public static ushort AllocateQueueID() {
            LastQueueId++;

            // Safety check incase the id is being used
            if (QueuedSyncables.ContainsKey(LastQueueId)) {
                while (QueuedSyncables.ContainsKey(LastQueueId) && LastQueueId < ushort.MaxValue) {
                    LastQueueId++;
                }
            }

            return LastQueueId;
        }

        public static void RegisterSyncable(ISyncable syncable, ushort id) {
            RemoveSyncable(id);

            syncable.OnRegister(id);
            Syncables.Add(id, syncable);
            LastId = id;
        }

        public static void RemoveSyncable(ISyncable syncable) {
            if (Syncables.ContainsValue(syncable))
                Syncables.Remove(syncable.GetId());

            if (QueuedSyncables.ContainsValue(syncable))
                QueuedSyncables.Remove(syncable.GetId());

            syncable.Cleanup();
        }

        public static void RemoveSyncable(ushort id) {
            if (Syncables.ContainsKey(id)) {
                var syncToRemove = Syncables[id];
                Syncables.Remove(id);
                syncToRemove.Cleanup();
            }
        }

        public static ushort QueueSyncable(ISyncable syncable) {
            if (QueuedSyncables.ContainsValue(syncable)) {
                var pair = QueuedSyncables.First(o => o.Value == syncable);
                QueuedSyncables.Remove(pair.Key);
            }

            var id = AllocateQueueID();
            QueuedSyncables.Add(id, syncable);
            return id;
        }

        public static bool UnqueueSyncable(ushort queuedId, ushort newId, out ISyncable syncable) {
            syncable = null;

            if (QueuedSyncables.ContainsKey(queuedId)) {
                syncable = QueuedSyncables[queuedId];
                QueuedSyncables.Remove(queuedId);
                RegisterSyncable(syncable, newId);

                return true;
            }

            return false;
        }

        public static bool HasSyncable(ushort id) => Syncables.ContainsKey(id);

        public static bool TryGetSyncable(ushort id, out ISyncable syncable) => Syncables.TryGetValue(id, out syncable);
    }
}
