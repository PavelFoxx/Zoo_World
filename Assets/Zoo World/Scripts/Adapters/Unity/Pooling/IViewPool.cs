using System;
using UnityEngine;

namespace Zoo.Unity
{
    public interface IViewPool : IDisposable
    {
        GameObject Rent(GameObject prefab, Transform parent);
        T Rent<T>(GameObject prefab, Transform parent) where T : class;
        void Return(GameObject instance);
    }
}
