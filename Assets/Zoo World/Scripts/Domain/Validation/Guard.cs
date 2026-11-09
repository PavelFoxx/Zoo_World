using System;

namespace Zoo.Domain
{
    public static class Guard
    {
        public static void Positive(float value, string name)
        {
            if (!(value > 0) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public static void NonNegative(float value, string name)
        {
            if (!(value >= 0) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
