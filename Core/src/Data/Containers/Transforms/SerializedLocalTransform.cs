﻿using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using LabFusion.Extensions;
using LabFusion.Network;

using SystemVector3 = System.Numerics.Vector3;
using SystemQuaternion = System.Numerics.Quaternion;

namespace LabFusion.Data
{
    public class SerializedLocalTransform : IFusionSerializable
    {
        public const int Size = sizeof(float) * 3 + SerializedSmallQuaternion.Size;
        public static readonly SerializedLocalTransform Default = new(Vector3Extensions.zero, Quaternion.identity);

        public SystemVector3 position;
        public SystemQuaternion rotation;

        private SerializedSmallQuaternion _compressedRotation;

        public void Serialize(FusionWriter writer)
        {
            writer.Write(position);
            writer.Write(_compressedRotation);
        }

        public void Deserialize(FusionReader reader)
        {
            position = reader.ReadSystemVector3();

            _compressedRotation = reader.ReadFusionSerializable<SerializedSmallQuaternion>();
            rotation = _compressedRotation.Expand();
        }

        public SerializedLocalTransform() { }

        public SerializedLocalTransform(Vector3 localPosition, Quaternion localRotation)
            : this(localPosition.ToSystemVector3(), localRotation.ToSystemQuaternion()) { }

        public SerializedLocalTransform(Transform transform)
            : this(transform.localPosition.ToSystemVector3(), transform.localRotation.ToSystemQuaternion()) { }

        public SerializedLocalTransform(SystemVector3 localPosition, SystemQuaternion localRotation)
        {
            this.position = localPosition;
            this.rotation = localRotation;

            this._compressedRotation = SerializedSmallQuaternion.Compress(this.rotation);
        }
    }
}
