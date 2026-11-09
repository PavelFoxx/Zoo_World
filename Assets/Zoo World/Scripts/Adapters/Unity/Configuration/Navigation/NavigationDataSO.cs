using System;
using UnityEngine;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Navigation/Level data")]
    public sealed class NavigationDataSO : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string ProfileId;
            public byte[] Mesh;
        }

        public string GeometryHash;
        public Entry[] Profiles;
    }
}
