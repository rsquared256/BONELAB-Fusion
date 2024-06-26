﻿using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Extensions;
using LabFusion.Utilities;
using LabFusion.Representation;

namespace LabFusion.Syncables
{
    public static class SyncManager
    {
        /// <summary>
        /// The list of syncables currently active.
        /// </summary>
        public static readonly FusionDictionary<ushort, ISyncable> Syncables = new(new SyncableComparer());

        /// <summary>
        /// The list of syncables currently queued while waiting for an ID response from the server.
        /// <para>Make sure when adding or removing syncables from this list you are NOT using Syncable.GetId! That is for the permanent Syncables list!</para>
        /// </summary>
        public static readonly FusionDictionary<ushort, ISyncable> QueuedSyncables = new(new SyncableComparer());

        /// <summary>
        /// The last allocated id. Incremented server side.
        /// </summary>
        public static ushort LastId = 0;

        /// <summary>
        /// The last registered queue id. Only kept client side.
        /// </summary>
        public static ushort LastQueueId = 0;

        public static void RequestSyncableID(ushort queuedId)
        {
            if (NetworkInfo.HasServer)
            {
                if (NetworkInfo.IsServer)
                {
                    UnqueueSyncable(queuedId, AllocateSyncID(), out _);
                }
                else
                {
                    using var writer = FusionWriter.Create(SyncableIDRequestData.Size);
                    var data = SyncableIDRequestData.Create(PlayerIdManager.LocalSmallId, queuedId);
                    writer.Write(data);

                    using var message = FusionMessage.Create(NativeMessageTag.SyncableIDRequest, writer);
                    MessageSender.BroadcastMessageExceptSelf(NetworkChannel.Reliable, message);
                }
            }
        }

        internal static void OnInitializeMelon()
        {
            MultiplayerHooking.OnPlayerLeave += OnPlayerLeave;
        }

        internal static void OnPlayerLeave(PlayerId id)
        {
            // Loop through every syncable and see if we need to remove the owner
            foreach (var syncable in Syncables.Values)
            {
                var owner = syncable.GetOwner();

                if (owner.HasValue && PlayerIdManager.GetPlayerId(owner.Value) == null)
                    syncable.RemoveOwner();
            }
        }

        internal static void OnUpdate()
        {
            // Here we send over position information/etc of our syncables
            foreach (var syncable in Syncables.Values)
            {
                try
                {
                    syncable.OnUpdate();
                }
                catch (Exception e)
                {
#if DEBUG
                    FusionLogger.LogException("executing OnUpdate for syncable", e);
#endif
                }
            }

            // Now, run in parallel
            Parallel.ForEach(Syncables.Values, OnParallelUpdate);
        }

        private static void OnParallelUpdate(ISyncable syncable)
        {
            ThreadingUtilities.IL2PrepareThread();

            try
            {
                syncable.OnParallelUpdate();
            }
            catch (Exception e)
            {
#if DEBUG
                FusionLogger.LogException("executing OnParallelUpdate for syncable", e);
#endif
            }
        }

        internal static void OnFixedUpdate()
        {
            // Cache variables
            foreach (var syncable in Syncables)
            {
                try
                {
                    syncable.Value.OnPreFixedUpdate();
                }
                catch (Exception e)
                {
#if DEBUG
                    FusionLogger.LogException("executing OnPreFixedUpdate for syncable", e);
#endif
                }
            }

            // Run in parallel to calculate forces/etc
            Parallel.ForEach(Syncables.Values, OnParallelFixedUpdate);

            // Now apply forces
            foreach (var syncable in Syncables)
            {
                try
                {
                    syncable.Value.OnFixedUpdate();
                }
                catch (Exception e)
                {
#if DEBUG
                    FusionLogger.LogException("executing OnFixedUpdate for syncable", e);
#endif
                }
            }
        }

        private static void OnParallelFixedUpdate(ISyncable syncable)
        {
            ThreadingUtilities.IL2PrepareThread();

            try
            {
                syncable.OnParallelFixedUpdate();
            }
            catch (Exception e)
            {
#if DEBUG
                FusionLogger.LogException("executing OnParallelFixedUpdate for syncable", e);
#endif
            }
        }

        public static void OnCleanup()
        {
            foreach (var syncable in Syncables.Values)
            {
                try
                {
                    syncable.Cleanup();
                }
                catch (Exception e)
                {
#if DEBUG
                    FusionLogger.LogException("cleaning up Syncable", e);
#endif
                }
            }

            Syncables.Clear();

            foreach (var syncable in QueuedSyncables)
            {
                try
                {
                    syncable.Value.Cleanup();
                }
                catch (Exception e)
                {
#if DEBUG
                    FusionLogger.LogException("cleaning up QueuedSyncable", e);
#endif
                }
            }

            QueuedSyncables.Clear();

            LastId = 0;
            LastQueueId = 0;
        }

        public static ushort AllocateSyncID()
        {
            LastId++;

            // Safety check incase the id is being used
            if (Syncables.ContainsKey(LastId))
            {
                while (Syncables.ContainsKey(LastId) && LastId < ushort.MaxValue)
                {
                    LastId++;
                }
            }

            return LastId;
        }

        public static ushort AllocateQueueID()
        {
            LastQueueId++;

            // Safety check incase the id is being used
            if (QueuedSyncables.ContainsKey(LastQueueId))
            {
                while (QueuedSyncables.ContainsKey(LastQueueId) && LastQueueId < ushort.MaxValue)
                {
                    LastQueueId++;
                }
            }

            return LastQueueId;
        }

        public static void RegisterSyncable(ISyncable syncable, ushort id)
        {
            if (syncable.IsDestroyed())
            {
                FusionLogger.Warn("Tried registering a destroyed syncable!");
                return;
            }

            RemoveSyncable(id);

            syncable.OnRegister(id);
            Syncables.Add(id, syncable);
            LastId = id;
        }

        public static void RemoveSyncable(ISyncable syncable)
        {
            Internal_RemoveFromList(syncable);
            Internal_RemoveFromQueue(syncable);

            syncable.Cleanup();
        }

        private static void Internal_RemoveFromList(ISyncable syncable)
        {
            if (Syncables.ContainsValue(syncable))
            {
                var pair = Syncables.First(o => o.Value == syncable);
                Syncables.Remove(pair.Key);
            }
        }

        private static void Internal_RemoveFromQueue(ISyncable syncable)
        {
            if (QueuedSyncables.ContainsValue(syncable))
            {
                var pair = QueuedSyncables.First(o => o.Value == syncable);
                QueuedSyncables.Remove(pair.Key);
            }
        }

        public static void RemoveSyncable(ushort id)
        {
            if (Syncables.ContainsKey(id))
            {
                var syncToRemove = Syncables[id];
                Syncables.Remove(id);
                syncToRemove.Cleanup();
            }
        }

        public static ushort QueueSyncable(ISyncable syncable)
        {
            Internal_RemoveFromQueue(syncable);

            var id = AllocateQueueID();
            QueuedSyncables.Add(id, syncable);
            return id;
        }

        public static bool UnqueueSyncable(ushort queuedId, ushort newId, out ISyncable syncable)
        {
            syncable = null;

            if (HasQueuedSyncable(queuedId))
            {
                syncable = QueuedSyncables[queuedId];
                QueuedSyncables.Remove(queuedId);

                if (syncable.IsDestroyed())
                {
                    FusionLogger.Warn("Tried unqueuing a destroyed syncable!");
                    return false;
                }

                RegisterSyncable(syncable, newId);

                return true;
            }

            return false;
        }

        public static bool HasQueuedSyncable(ushort id)
        {
            return QueuedSyncables.ContainsKey(id);
        }

        public static bool HasSyncable(ushort id)
        {
            return Syncables.ContainsKey(id);
        }

        public static bool TryGetSyncable(ushort id, out ISyncable syncable) => Syncables.TryGetValue(id, out syncable);

        public static bool TryGetSyncable<TSyncable>(ushort id, out TSyncable syncable) where TSyncable : ISyncable
        {
            if (TryGetSyncable(id, out ISyncable result) && result is TSyncable generic)
            {
                syncable = generic;
                return true;
            }

            syncable = default;
            return false;
        }
    }
}
