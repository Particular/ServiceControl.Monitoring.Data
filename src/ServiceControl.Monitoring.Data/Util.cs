﻿namespace ServiceControl.Monitoring.Data
{
    using System.Runtime.CompilerServices;

    static class Util
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this int n)
        {
            return (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this long n)
        {
            return (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(this int i)
        {
            var r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(this long i)
        {
            var r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }
    }
}