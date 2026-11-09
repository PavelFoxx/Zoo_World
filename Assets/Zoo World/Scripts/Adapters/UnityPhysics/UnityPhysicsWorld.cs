using System;
using System.Collections.Generic;
using R3;
using Unity.Collections;
using UnityEngine;
using Zoo.Application;
using Zoo.Domain;
using Zoo.Unity;
using NVector = System.Numerics.Vector3;
using NQuaternion = System.Numerics.Quaternion;

namespace Zoo.Physics.Unity
{
    public sealed class UnityPhysicsWorld : IPhysicsWorld, IContactResponseControl
    {
        private sealed class Body
        {
            public Rigidbody Rigidbody;
            public PhysicsBodyProxy Proxy;
            public float Radius;
            public float GravityScale;
            public float? FlightAltitude;
            public float Remaining;
            public float Grace;
            public Vector3 Desired;
            public EntityPose Last;
            public int ColliderId;
        }

        private readonly Dictionary<int, Body> _bodies = new Dictionary<int, Body>();

        private readonly Dictionary<int, Animal> _animalsByCollider = new Dictionary<int, Animal>();
        private readonly Subject<PhysicsPose> _poses = new Subject<PhysicsPose>();

        private readonly Subject<Contact> _contacts = new Subject<Contact>();
        private readonly HashSet<(int, int)> _pending = new HashSet<(int, int)>();

        private readonly object _contactLock = new object();
        private readonly List<(int, int)> _orderedContacts = new List<(int, int)>();
        private readonly IViewPool _pool;
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly SimulationMode _previousMode;
        private readonly PhysicsScene _physicsScene;
        private readonly PhysicsMaterial _material;
        private Func<Animal, Animal, bool> _shouldSuppressContact;
        private bool _disposed;
        public Observable<PhysicsPose> Poses => _poses;
        public Observable<Contact> Contacts => _contacts;

        public UnityPhysicsWorld(IViewPool pool, Transform root, GameObject bodyPrefab)
        {
            _pool = pool;
            _root = root;
            _prefab = bodyPrefab != null ? bodyPrefab : throw new ArgumentNullException(nameof(bodyPrefab));
            _previousMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            _physicsScene = UnityEngine.Physics.defaultPhysicsScene;
            _material = new PhysicsMaterial("Zoo bodies")
            {
                dynamicFriction = 0,
                staticFriction = 0,
                bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            UnityEngine.Physics.ContactModifyEvent += SuppressInteractionResponse;
            UnityEngine.Physics.ContactModifyEventCCD += SuppressInteractionResponse;
        }

        public static Vector3 U(NVector v) => new Vector3(v.X, v.Y, v.Z);

        public static NVector N(Vector3 v) => new NVector(v.x, v.y, v.z);

        public void SetContactFilter(Func<Animal, Animal, bool> shouldSuppress) => _shouldSuppressContact = shouldSuppress;

        public void Add(Animal animal, float? flightAltitude = null)
        {
            var go = _pool.Rent(_prefab, _root);
            go.SetActive(false);
            var rb = go.GetComponent<Rigidbody>();
            var radius = animal.Definition.Body.Radius;
            var collider = go.GetComponent<SphereCollider>();
            collider.radius = radius;
            collider.sharedMaterial = _material;
            collider.hasModifiableContacts = true;
            rb.mass = animal.Definition.Body.Mass;
            var gravityScale = animal.Definition.Body.GravityScale;
            rb.useGravity = gravityScale == 1;
            go.transform.SetPositionAndRotation(U(animal.Pose.CurrentValue.Position) + Vector3.up * radius, Quaternion.identity);
            rb.position = go.transform.position;
            rb.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            var proxy = go.GetComponent<PhysicsBodyProxy>();
            proxy.Owner = this;
            proxy.EntityId = animal.Id;
            var colliderId = collider.GetInstanceID();
            _bodies.Add(animal.Id, new Body
            {
                Rigidbody = rb,
                Proxy = proxy,
                Radius = radius,
                GravityScale = gravityScale,
                FlightAltitude = flightAltitude,
                Last = animal.Pose.CurrentValue,
                ColliderId = colliderId
            });
            _animalsByCollider.Add(colliderId, animal);
            go.SetActive(true);
            rb.WakeUp();
        }

        public void Remove(int id)
        {
            if (!_bodies.TryGetValue(id, out var body))
            {
                return;
            }

            _bodies.Remove(id);
            _animalsByCollider.Remove(body.ColliderId);
            body.Proxy.Owner = null;
            body.Proxy.EntityId = 0;
            body.Rigidbody.linearVelocity = Vector3.zero;
            body.Rigidbody.angularVelocity = Vector3.zero;
            body.Rigidbody.gameObject.SetActive(false);
            _pool.Return(body.Rigidbody.gameObject);
        }

        public bool IsFree(NVector position, float radius)
        {
            var p = U(position) + Vector3.up * (radius + 0.08f);
            return !UnityEngine.Physics.CheckSphere(p, radius, ~0, QueryTriggerInteraction.Ignore);
        }

        public void Move(int id, NVector direction, float speed, float duration)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = U(direction) * speed;
                body.Remaining = duration;
                body.Rigidbody.WakeUp();
            }
        }

        public void Steer(int id, NVector direction)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = U(direction) * body.Desired.magnitude;
            }
        }

        public void Stop(int id)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Desired = Vector3.zero;
                body.Remaining = 0;
            }
        }

        public void Impulse(int id, NVector impulse)
        {
            if (_bodies.TryGetValue(id, out var body))
            {
                body.Rigidbody.AddForce(U(impulse), ForceMode.Impulse);
                body.Grace = 0.18f;
            }
        }

        internal void RecordContact(int a, int b)
        {
            if (a != b)
            {
                lock (_contactLock)
                {
                    _pending.Add((Math.Min(a, b), Math.Max(a, b)));
                }
            }
        }

        private void SuppressInteractionResponse(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            var shouldSuppress = _shouldSuppressContact;
            if (scene != _physicsScene || shouldSuppress == null)
            {
                return;
            }

            // PhysX may call this on worker threads. The simulation owns the lookup and
            // animal state and leaves them unchanged until Physics.Simulate returns.
            foreach (var pair in pairs)
            {
                if (pair.contactCount == 0 ||
                    !_animalsByCollider.TryGetValue(pair.colliderInstanceID, out var a) ||
                    !_animalsByCollider.TryGetValue(pair.otherColliderInstanceID, out var b) ||
                    !shouldSuppress(a, b))
                {
                    continue;
                }

                for (var i = 0; i < pair.contactCount; i++)
                {
                    pair.IgnoreContact(i);
                }

                // Ignored points need not produce OnCollisionEnter.
                RecordContact(a.Id, b.Id);
            }
        }

        public void Step(float delta)
        {
            var gravity = UnityEngine.Physics.gravity;
            foreach (var body in _bodies.Values)
            {
                var rb = body.Rigidbody;
                if (body.Grace > 0)
                {
                    body.Grace = Mathf.Max(0, body.Grace - delta);
                }
                else
                {
                    var desired = body.Remaining > 0 ? body.Desired * Mathf.Min(1, body.Remaining / delta) : Vector3.zero;
                    var current = rb.linearVelocity;
                    desired.y = body.FlightAltitude is float altitude
                        ? FlightMovement.VerticalSpeed(altitude - (rb.position.y - body.Radius), delta, gravity.y * body.GravityScale)
                        : current.y;
                    // A velocity motor; collision detection and resolution remain entirely PhysX.
                    rb.AddForce(Vector3.ClampMagnitude(desired - current, 1000 * delta), ForceMode.VelocityChange);
                }

                if (body.GravityScale != 1 && body.GravityScale != 0)
                {
                    rb.AddForce(gravity * body.GravityScale, ForceMode.Acceleration);
                }

                body.Remaining = Mathf.Max(0, body.Remaining - delta);
            }

            UnityEngine.Physics.Simulate(delta);
            foreach (var pair in _bodies)
            {
                var body = pair.Value;
                var rb = body.Rigidbody;
                var velocity = rb.linearVelocity;
                velocity.y = 0;
                var rotation = velocity.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(velocity)
                    : new Quaternion(body.Last.Rotation.X, body.Last.Rotation.Y, body.Last.Rotation.Z, body.Last.Rotation.W);
                var pose = new EntityPose(N(rb.position - Vector3.up * body.Radius), new NQuaternion(rotation.x, rotation.y, rotation.z, rotation.w));
                if (!pose.Equals(body.Last))
                {
                    body.Last = pose;
                    _poses.OnNext(new PhysicsPose(pair.Key, pose));
                }
            }

            _orderedContacts.Clear();
            _orderedContacts.AddRange(_pending);
            _pending.Clear();
            _orderedContacts.Sort();
            foreach (var pair in _orderedContacts)
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
            UnityEngine.Physics.ContactModifyEvent -= SuppressInteractionResponse;
            UnityEngine.Physics.ContactModifyEventCCD -= SuppressInteractionResponse;
            _shouldSuppressContact = null;
            foreach (var id in new List<int>(_bodies.Keys))
            {
                Remove(id);
            }

            _poses.Dispose();
            _contacts.Dispose();
            UnityEngine.Physics.simulationMode = _previousMode;
            UnityEngine.Object.Destroy(_material);
        }
    }
}
