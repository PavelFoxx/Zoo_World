using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using R3;
using Zoo.Application;
using Zoo.Domain;

namespace Zoo.Tests
{
    // Models application physics commands for deterministic simulation tests.
    internal sealed class TestPhysics : IPhysicsWorld, IContactResponseControl
    {
        public bool SpaceAvailable = true;
        public int DisposeCount;

        public readonly Dictionary<int, Vector3> Bodies = new Dictionary<int, Vector3>();
        public readonly List<Vector3> Impulses = new List<Vector3>();

        private readonly Dictionary<int, (Vector3 velocity, float remaining)> _motion = new Dictionary<int, (Vector3, float)>();
        private readonly Subject<PhysicsPose> _poses = new Subject<PhysicsPose>();

        private readonly Subject<Contact> _contacts = new Subject<Contact>();
        public Observable<PhysicsPose> Poses => _poses;
        public Observable<Contact> Contacts => _contacts;
        public Func<Animal, Animal, bool> ContactFilter { get; private set; }

        public void SetContactFilter(Func<Animal, Animal, bool> shouldSuppress) => ContactFilter = shouldSuppress;

        public void PublishContact(Contact contact) => _contacts.OnNext(contact);

        public void Add(Animal animal, float? flightAltitude = null) => Bodies.Add(animal.Id, animal.Pose.CurrentValue.Position);

        public void Remove(int id)
        {
            Bodies.Remove(id);
            _motion.Remove(id);
        }

        public bool IsFree(Vector3 point, float radius) => SpaceAvailable;

        public void Move(int id, Vector3 direction, float speed, float duration) => _motion[id] = (direction * speed, duration);

        public void Steer(int id, Vector3 direction)
        {
            if (_motion.TryGetValue(id, out var motion))
            {
                _motion[id] = (direction * motion.velocity.Length(), motion.remaining);
            }
        }

        public void Stop(int id) => _motion.Remove(id);

        public void Impulse(int id, Vector3 impulse) => Impulses.Add(impulse);

        public void Step(float delta)
        {
            foreach (var id in Bodies.Keys.ToArray())
            {
                if (!_motion.TryGetValue(id, out var motion))
                {
                    continue;
                }

                var elapsed = Math.Min(delta, motion.remaining);
                _motion[id] = (motion.velocity, Math.Max(0, motion.remaining - delta));
                Bodies[id] += motion.velocity * elapsed;
                _poses.OnNext(new PhysicsPose(id, new EntityPose(Bodies[id], Quaternion.Identity)));
            }
        }

        public void Dispose()
        {
            DisposeCount++;
            _poses.Dispose();
            _contacts.Dispose();
            Bodies.Clear();
        }
    }
}
