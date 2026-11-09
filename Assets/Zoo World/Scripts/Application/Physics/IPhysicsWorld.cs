using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public interface IPhysicsWorld : IDisposable
    {
        Observable<PhysicsPose> Poses { get; }

        Observable<Contact> Contacts { get; }

        void Add(Animal animal, float? flightAltitude = null);
        void Remove(int id);
        bool IsFree(Vector3 position, float radius);
        void Move(int id, Vector3 direction, float speed, float duration);
        void Steer(int id, Vector3 direction);
        void Stop(int id);
        void Impulse(int id, Vector3 impulse);
        void Step(float delta);
    }
}
