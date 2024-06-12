﻿using UnityEngine;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;

using LabFusion.Marrow;

namespace LabFusion.UI
{
    public static class InfoBoxHelper
    {
        public static void CompleteInfoBoard(GameObject gameObject)
        {
            // Currently just needs to add the InfoBox script
            gameObject.AddComponent<InfoBox>();
        }

        public static void SpawnInfoBoard(Vector3 position, Quaternion rotation)
        {
            var spawnable = new Spawnable()
            {
                crateRef = FusionSpawnableReferences.InfoBoardReference,
                policyData = null,
            };

            AssetSpawner.Register(spawnable);

            AssetSpawner.Spawn(spawnable, position, rotation, new Il2CppSystem.Nullable<Vector3>(Vector3.one), true, new Il2CppSystem.Nullable<int>(0), null, null);
        }
    }
}