using System;
using System.Collections.Generic;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using R3;
using Zoo.Application;
using Zoo.Configuration;
using Zoo.Domain;

namespace Zoo.Headless.Physics
{
    public sealed class BepuPhysicsWorld : IPhysicsWorld
    {
        private sealed class Body
        {
            public BodyHandle Handle;
            public float Radius;
            public float GravityScale;
            public float? FlightAltitude;
            public float Remaining;
            public float Grace;
            public Vector3 Desired;
            public EntityPose Last;
        }

        private readonly BufferPool _buffers = new BufferPool();
        private readonly Simulation _simulation;
        private readonly Vector3 _gravity;

        private readonly Dictionary<int, Body> _bodies = new Dictionary<int, Body>();
        private readonly Dictionary<int, int> _entityIds = new Dictionary<int, int>();

        private readonly Dictionary<float, TypedIndex> _spheres = new Dictionary<float, TypedIndex>();
        private readonly HashSet<(int, int)> _currentContacts = new HashSet<(int, int)>();

        private readonly HashSet<(int, int)> _previousContacts = new HashSet<(int, int)>();
        private readonly List<(int, int)> _enteredContacts = new List<(int, int)>();

        private readonly Subject<PhysicsPose> _poses = new Subject<PhysicsPose>();
        private readonly Subject<Contact> _contacts = new Subject<Contact>();
        private bool _disposed;
        public Observable<PhysicsPose> Poses => _poses;
        public Observable<Contact> Contacts => _contacts;

        public BepuPhysicsWorld(WorldSettings settings)
        {
            settings.Validate();
            _gravity = settings.gravity.ToVector();
            _simulation = Simulation.Create(_buffers, new ContactCallbacks(this), new GravityIntegrator(_gravity, settings.linearDamping), new SolveDescription(8, 1));
            try
            {
                foreach (var box in settings.boxes)
                {
                    var shape = _simulation.Shapes.Add(new Box(box.size.x, box.size.y, box.size.z));
                    _simulation.Statics.Add(new StaticDescription(box.center.ToVector(), box.rotation.ToQuaternion(), shape));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Add(Animal animal, float? flightAltitude = null)
        {
            var radius = animal.Definition.Body.Radius;
            if (!_spheres.TryGetValue(radius, out var shape))
            {
                shape = _simulation.Shapes.Add(new Sphere(radius));
                _spheres.Add(radius, shape);
            }

            // Zero inverse inertia locks rotation, as on the Unity Rigidbody prefab.
            var inertia = new BodyInertia
            {
                InverseMass = 1 / animal.Definition.Body.Mass
            };
            var description = BodyDescription.CreateDynamic(animal.Pose.CurrentValue.Position + Vector3.UnitY * radius, inertia, new CollidableDescription(shape, 0.01f, ContinuousDetection.Continuous()), new BodyActivityDescription(-1));
            var handle = _simulation.Bodies.Add(description);
            _bodies.Add(animal.Id, new Body
            {
                Handle = handle,
                Radius = radius,
                GravityScale = animal.Definition.Body.GravityScale,
                FlightAltitude = flightAltitude,
                Last = animal.Pose.CurrentValue
            });
            _entityIds.Add(handle.Value, animal.Id);
        }

        public void Remove(int id)
        {
            if (!_bodies.TryGetValue(id, out var body))
            {
                return;
            }

            _simulation.Bodies.Remove(body.Handle);
            _entityIds.Remove(body.Handle.Value);
            _bodies.Remove(id);
            _previousContacts.RemoveWhere(pair => pair.Item1 == id || pair.Item2 == id);
        }

        public bool IsFree(Vector3 position, float radius)
        {
            var handler = new OverlapHandler();
            var pose = new RigidPose(position + Vector3.UnitY * (radius + 0.08f));
            _simulation.Sweep(new Sphere(radius), pose, new BodyVelocity(Vector3.Zero), 1, _buffers, ref handler);
            return !handler.Hit;
        }

        public void Move(int id, Vector3 direction, float speed, float duration)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = direction * speed;
                body.Remaining = duration;
            }
        }

        public void Steer(int id, Vector3 direction)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = direction * body.Desired.Length();
            }
        }

        public void Stop(int id)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = Vector3.Zero;
                body.Remaining = 0;
            }
        }

        public void Impulse(int id, Vector3 impulse)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                var reference = _simulation.Bodies.GetBodyReference(body.Handle);
                reference.ApplyLinearImpulse(impulse);
                body.Grace = 0.18f;
            }
        }

        internal void RecordContact(BodyHandle first, BodyHandle second)
        {
            var a = _entityIds[first.Value];
            var b = _entityIds[second.Value];
            _currentContacts.Add((Math.Min(a, b), Math.Max(a, b)));
        }

        public void Step(float delta)
        {
            Guard.Positive(delta, nameof(delta));
            foreach (var body in _bodies.Values)
            {
                var reference = _simulation.Bodies.GetBodyReference(body.Handle);
                if (body.Grace > 0)
                {
                    body.Grace = Math.Max(0, body.Grace - delta);
                }
                else
                {
                    var desired = body.Remaining > 0 ? body.Desired * Math.Min(1, body.Remaining / delta) : Vector3.Zero;
                    var current = reference.Velocity.Linear;
                    desired.Y = body.FlightAltitude is float altitude
                        ? FlightMovement.VerticalSpeed(altitude - (reference.Pose.Position.Y - body.Radius), delta, _gravity.Y * body.GravityScale)
                        : current.Y;
                    var change = desired - current;
                    var limit = 1000 * delta;
                    if (change.LengthSquared() > limit * limit)
                    {
                        change = Vector3.Normalize(change) * limit;
                    }

                    reference.Velocity.Linear += change;
                }

                // The integrator supplies world gravity. Apply the per-body difference before the step.
                if (body.GravityScale != 1)
                {
                    reference.Velocity.Linear += _gravity * ((body.GravityScale - 1) * delta);
                }

                body.Remaining = Math.Max(0, body.Remaining - delta);
            }

            _currentContacts.Clear();
            _simulation.Timestep(delta);
            foreach (var entry in _bodies)
            {
                var body = entry.Value;
                var reference = _simulation.Bodies.GetBodyReference(body.Handle);
                var velocity = reference.Velocity.Linear;
                velocity.Y = 0;
                var rotation = velocity.LengthSquared() > 0.001f ? Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.Atan2(velocity.X, velocity.Z)) : body.Last.Rotation;
                var pose = new EntityPose(reference.Pose.Position - Vector3.UnitY * body.Radius, rotation);
                if (!pose.Equals(body.Last))
                {
                    body.Last = pose;
                    _poses.OnNext(new PhysicsPose(entry.Key, pose));
                }
            }

            _enteredContacts.Clear();
            foreach (var pair in _currentContacts)
            {
                if (!_previousContacts.Contains(pair))
                {
                    _enteredContacts.Add(pair);
                }
            }

            _previousContacts.Clear();
            _previousContacts.UnionWith(_currentContacts);
            _enteredContacts.Sort();
            foreach (var pair in _enteredContacts)
            {
                _contacts.OnNext(new Contact(pair.Item1, pair.Item2));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _simulation.Dispose();
            _buffers.Clear();
            _bodies.Clear();
            _entityIds.Clear();
            _poses.Dispose();
            _contacts.Dispose();
        }

        private struct OverlapHandler : ISweepHitHandler
        {
            public bool Hit;

            public bool AllowTest(CollidableReference collidable) => !Hit;

            public bool AllowTest(CollidableReference collidable, int child) => !Hit;

            public void OnHit(ref float maximumT, float time, in Vector3 location, in Vector3 normal, CollidableReference collidable) => Hit = true;

            public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable) => Hit = true;
        }
    }
}
