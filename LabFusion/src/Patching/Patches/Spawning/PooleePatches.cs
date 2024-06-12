﻿using System.Collections;

using HarmonyLib;

using LabFusion.Network;
using LabFusion.Syncables;
using LabFusion.Utilities;
using LabFusion.Data;
using LabFusion.Extensions;
using LabFusion.Senders;

using Il2CppSLZ.Marrow.Pool;

using MelonLoader;


namespace LabFusion.Patching
{
    [HarmonyPatch(typeof(Poolee), nameof(Poolee.OnSpawnEvent))]
    public class PooleeOnSpawnPatch
    {
        private static void CheckRemoveSyncable(Poolee __instance)
        {
            if (PropSyncable.Cache.TryGet(__instance.gameObject, out var syncable))
                SyncManager.RemoveSyncable(syncable);
        }

        public static void Postfix(Poolee __instance)
        {
            if (PooleeUtilities.IsPlayer(__instance))
                return;

            if (!NetworkInfo.HasServer || !__instance.SpawnableCrate)
            {
                return;
            }

            try
            {
                var barcode = __instance.SpawnableCrate.Barcode;

                if (!NetworkInfo.IsServer)
                {
                    // Check if we should prevent this object from spawning
                    if (barcode == CommonBarcodes.FADE_OUT_BARCODE)
                    {
                        __instance.gameObject.SetActive(false);
                    }
                    else if (!PooleeUtilities.ForceEnabled.Contains(__instance) && PooleeUtilities.CanForceDespawn(__instance))
                    {
                        CheckRemoveSyncable(__instance);

                        __instance.gameObject.SetActive(false);
                        MelonCoroutines.Start(CoForceDespawnRoutine(__instance));
                    }
                }
                else
                {
                    if (PooleeUtilities.CanSendSpawn(__instance))
                    {
                        CheckRemoveSyncable(__instance);

                        PooleeUtilities.CheckingForSpawn.Push(__instance);
                        FusionSceneManager.HookOnLevelLoad(() =>
                        {
                            DelayUtilities.Delay(() =>
                            {
                                OnVerifySpawned(__instance);
                            }, 4);
                        });
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                FusionLogger.LogException("to execute patch Poolee.OnSpawn", e);
#endif
            }
        }

        private static IEnumerator CoForceDespawnRoutine(Poolee __instance)
        {
            var go = __instance.gameObject;

            for (var i = 0; i < 3; i++)
            {
                yield return null;

                if (!PooleeUtilities.CanForceDespawn(__instance))
                {
                    go.SetActive(true);
                    yield break;
                }

                if (PooleeUtilities.CanSpawnList.Contains(__instance) || PooleeUtilities.ForceEnabled.Contains(__instance))
                    yield break;

                go.SetActive(false);
            }
        }

        private static void OnVerifySpawned(Poolee __instance)
        {
            PooleeUtilities.CheckingForSpawn.Pull(__instance);

            try
            {
                if (PooleeUtilities.CanSendSpawn(__instance) && !PooleeUtilities.ServerSpawnedList.Pull(__instance))
                {
                    var barcode = __instance.SpawnableCrate.Barcode;

                    var syncId = SyncManager.AllocateSyncID();
                    PooleeUtilities.OnServerLocalSpawn(syncId, __instance.gameObject, out PropSyncable newSyncable);

                    PooleeUtilities.SendSpawn(0, barcode, syncId, new SerializedTransform(__instance.transform), true);

                    // Insert catchup hook for future users
                    if (NetworkInfo.IsServer)
                        newSyncable.InsertCatchupDelegate((id) =>
                        {
                            SpawnSender.SendCatchupSpawn(0, barcode, syncId, new SerializedTransform(__instance.transform), id);
                        });
                }
            }
            catch (Exception e)
            {
#if DEBUG
                FusionLogger.LogException("to execute WaitForVerify", e);
#endif
            }
        }
    }

    [HarmonyPatch(typeof(Poolee), nameof(Poolee.OnDespawnEvent))]
    public class PooleeOnDespawnPatch
    {
        public static void Postfix(Poolee __instance)
        {
            if (PooleeUtilities.IsPlayer(__instance) || __instance.IsNOC())
                return;

            if (NetworkInfo.HasServer && PropSyncable.Cache.TryGet(__instance.gameObject, out var syncable))
            {
                SyncManager.RemoveSyncable(syncable);
            }
        }
    }

    [HarmonyPatch(typeof(Poolee), nameof(Poolee.Despawn))]
    public class PooleeDespawnPatch
    {
        public static bool IgnorePatch = false;

        public static bool Prefix(Poolee __instance)
        {
            if (PooleeUtilities.IsPlayer(__instance) || IgnorePatch || __instance.IsNOC())
                return true;

            try
            {
                if (NetworkInfo.HasServer)
                {
                    if (!NetworkInfo.IsServer && !PooleeUtilities.CanDespawn && PropSyncable.Cache.TryGet(__instance.gameObject, out var syncable))
                    {
                        return false;
                    }
                    else if (NetworkInfo.IsServer)
                    {
                        if (!CheckPropSyncable(__instance) && PooleeUtilities.CheckingForSpawn.Contains(__instance))
                            MelonCoroutines.Start(CoVerifyDespawnCoroutine(__instance));
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                FusionLogger.LogException("to execute patch Poolee.Despawn", e);
#endif
            }

            return true;
        }

        private static bool CheckPropSyncable(Poolee __instance)
        {
            if (PropSyncable.Cache.TryGet(__instance.gameObject, out var syncable))
            {
                PooleeUtilities.SendDespawn(syncable.Id);
                SyncManager.RemoveSyncable(syncable);
                return true;
            }
            return false;
        }

        private static IEnumerator CoVerifyDespawnCoroutine(Poolee __instance)
        {
            while (!__instance.IsNOC() && PooleeUtilities.CheckingForSpawn.Contains(__instance))
            {
                yield return null;
            }

            CheckPropSyncable(__instance);
        }
    }
}
