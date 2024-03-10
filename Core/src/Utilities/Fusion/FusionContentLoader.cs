﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BoneLib;

using LabFusion.Data;

using UnityEngine;

namespace LabFusion.Utilities
{
    public static class FusionContentLoader
    {
        public static WeakAssetReference<AssetBundle> ContentBundle { get; private set; } = new();

        public static WeakAssetReference<GameObject> PointShopPrefab { get; private set; } = new();
        public static WeakAssetReference<GameObject> InfoBoxPrefab { get; private set; } = new();
        public static WeakAssetReference<GameObject> CupBoardPrefab { get; private set; } = new();

        public static WeakAssetReference<GameObject> EntangledLinePrefab { get; private set; } = new();

        public static WeakAssetReference<GameObject> AchievementPopupPrefab { get; private set; } = new();
        public static WeakAssetReference<GameObject> BitPopupPrefab { get; private set; } = new();
        public static WeakAssetReference<GameObject> MutePopupPrefab { get; private set; } = new();

        public static WeakAssetReference<Texture2D> SabrelakeLogo { get; private set; } = new();
        public static WeakAssetReference<Texture2D> LavaGangLogo { get; private set; } = new();

        public static WeakAssetReference<AudioClip> GeoGrpFellDownTheStairs { get; private set; } = new();
        public static WeakAssetReference<AudioClip> BouncingStrong { get; private set; } = new();

        public static WeakAssetReference<AudioClip> LavaGangVictory { get; private set; } = new();
        public static WeakAssetReference<AudioClip> SabrelakeVictory { get; private set; } = new();

        public static WeakAssetReference<AudioClip> LavaGangFailure { get; private set; } = new();
        public static WeakAssetReference<AudioClip> SabrelakeFailure { get; private set; } = new();

        public static WeakAssetReference<AudioClip> DMTie { get; private set; } = new();

        public static WeakAssetReference<AudioClip> BitGet { get; private set; } = new();

        public static WeakAssetReference<AudioClip> UISelect { get; private set; } = new();
        public static WeakAssetReference<AudioClip> UIDeny { get; private set; } = new();
        public static WeakAssetReference<AudioClip> UIConfirm { get; private set; } = new();
        public static WeakAssetReference<AudioClip> UITurnOff { get; private set; } = new();
        public static WeakAssetReference<AudioClip> UITurnOn { get; private set; } = new();

        public static WeakAssetReference<AudioClip> PurchaseFailure { get; private set; } = new();
        public static WeakAssetReference<AudioClip> PurchaseSuccess { get; private set; } = new();

        public static WeakAssetReference<AudioClip> EquipItem { get; private set; } = new();
        public static WeakAssetReference<AudioClip> UnequipItem { get; private set; } = new();

        public static WeakAssetReference<Texture2D> NotificationInformation { get; private set; } = new();
        public static WeakAssetReference<Texture2D> NotificationWarning { get; private set; } = new();
        public static WeakAssetReference<Texture2D> NotificationError { get; private set; } = new();
        public static WeakAssetReference<Texture2D> NotificationSuccess { get; private set; } = new();

        // Laser cursor
        public static WeakAssetReference<GameObject> LaserCursor { get; private set; } = new();
        public static WeakAssetReference<AudioClip> LaserPulseSound { get; private set; } = new();
        public static WeakAssetReference<AudioClip> LaserRaySpawn { get; private set; } = new();
        public static WeakAssetReference<AudioClip> LaserRayDespawn { get; private set; } = new();
        public static WeakAssetReference<AudioClip> LaserPrismaticSFX { get; private set; } = new();

        private static readonly string[] _combatSongNames = new string[6] {
            "music_FreqCreepInModulationBuggyPhysics",
            "music_SicklyBugInitiative",
            "music_SyntheticCavernsRemix",
            "music_WWWonderlan",
            "music_SmigglesInDespair",
            "music_AppenBeyuge",
        };

        private static readonly List<AudioClip> _combatPlaylist = new();
        public static AudioClip[] CombatPlaylist => _combatPlaylist.ToArray();

        private static AssetBundleCreateRequest _contentBundleRequest = null;

        private static void OnBundleCompleted(AsyncOperation operation)
        {
            var bundle = _contentBundleRequest.assetBundle;
            ContentBundle.SetAsset(bundle);

            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.PointShopPrefab, (v) => { PointShopPrefab.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.InfoBoxPrefab, (v) => { InfoBoxPrefab.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.CupBoardPrefab, (v) => { CupBoardPrefab.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.EntangledLinePrefab, (v) => { EntangledLinePrefab.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.AchievementPopupPrefab, (v) => { AchievementPopupPrefab.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.BitPopupPrefab, (v) => { BitPopupPrefab.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.MutePopupPrefab, (v) => { MutePopupPrefab.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.SabrelakeLogo, (v) => { SabrelakeLogo.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.LavaGangLogo, (v) => { LavaGangLogo.SetAsset(v); });

            foreach (var song in _combatSongNames)
            {
                bundle.LoadPersistentAssetAsync<AudioClip>(song, (v) => { _combatPlaylist.Add(v); });
            }

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.GeoGrpFellDownTheStairs, (v) => { GeoGrpFellDownTheStairs.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.BouncingStrong, (v) => { BouncingStrong.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LavaGangVictory, (v) => { LavaGangVictory.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.SabrelakeVictory, (v) => { SabrelakeVictory.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LavaGangFailure, (v) => { LavaGangFailure.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.SabrelakeFailure, (v) => { SabrelakeFailure.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.DMTie, (v) => { DMTie.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.BitGet, (v) => { BitGet.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UISelect, (v) => { UISelect.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UIDeny, (v) => { UIDeny.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UIConfirm, (v) => { UIConfirm.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UITurnOff, (v) => { UITurnOff.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UITurnOn, (v) => { UITurnOn.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.PurchaseFailure, (v) => { PurchaseFailure.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.PurchaseSuccess, (v) => { PurchaseSuccess.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.EquipItem, (v) => { EquipItem.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.UnequipItem, (v) => { UnequipItem.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.NotificationInformation, (v) => { NotificationInformation.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.NotificationWarning, (v) => { NotificationWarning.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.NotificationError, (v) => { NotificationError.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<Texture2D>(ResourcePaths.NotificationSuccess, (v) => { NotificationSuccess.SetAsset(v); });

            bundle.LoadPersistentAssetAsync<GameObject>(ResourcePaths.LaserCursor, (v) => { LaserCursor.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LaserPulseSound, (v) => { LaserPulseSound.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LaserRaySpawn, (v) => { LaserRaySpawn.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LaserRayDespawn, (v) => { LaserRayDespawn.SetAsset(v); });
            bundle.LoadPersistentAssetAsync<AudioClip>(ResourcePaths.LaserPrismaticSFX, (v) => { LaserPrismaticSFX.SetAsset(v); });
        }

        public static void OnBundleLoad()
        {
            _contentBundleRequest = FusionBundleLoader.LoadAssetBundleAsync(ResourcePaths.ContentBundle);

            if (_contentBundleRequest != null)
            {
                _contentBundleRequest.add_completed((Il2CppSystem.Action<AsyncOperation>)OnBundleCompleted);
            }
            else
                FusionLogger.Error("Content Bundle failed to load!");
        }

        public static void OnBundleUnloaded()
        {
            // Unload content bundle
            if (ContentBundle.HasAsset)
            {
                ContentBundle.Asset.Unload(true);
                ContentBundle.UnloadAsset();
            }
        }
    }
}