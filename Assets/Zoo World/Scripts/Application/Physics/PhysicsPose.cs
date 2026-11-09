using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public readonly struct PhysicsPose
    {
        public readonly int Id;
        public readonly EntityPose Pose;

        public PhysicsPose(int id, EntityPose pose)
        {
            Id = id;
            Pose = pose;
        }
    }
}
