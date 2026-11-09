using UnityEngine;

namespace Zoo.Physics.Unity
{
    public sealed class PhysicsBodyProxy : MonoBehaviour
    {
        internal UnityPhysicsWorld Owner;
        internal int EntityId;

        private void OnCollisionEnter(Collision collision)
        {
            if (Owner == null)
            {
                return;
            }

            var other = collision.rigidbody != null ? collision.rigidbody.GetComponent<PhysicsBodyProxy>() : null;
            if (other != null && other.Owner == Owner)
            {
                Owner.RecordContact(EntityId, other.EntityId);
            }
        }
    }
}
