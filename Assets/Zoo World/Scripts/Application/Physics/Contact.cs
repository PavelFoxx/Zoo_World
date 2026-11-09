using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public readonly struct Contact
    {
        public readonly int A;
        public readonly int B;

        public Contact(int a, int b)
        {
            A = Math.Min(a, b);
            B = Math.Max(a, b);
        }
    }
}
