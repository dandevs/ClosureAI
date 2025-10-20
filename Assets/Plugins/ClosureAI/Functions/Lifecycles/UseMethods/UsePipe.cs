#if UNITASK_INSTALLED
using System;
using System.Runtime.CompilerServices;

namespace ClosureAI
{
    public static partial class AI
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T1> UsePipe<T0, T1>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1)
        {
            return var1(var0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T2> UsePipe<T0, T1, T2>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2)
        {
            return var2(var1(var0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T3> UsePipe<T0, T1, T2, T3>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3)
        {
            return var3(var2(var1(var0)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T4> UsePipe<T0, T1, T2, T3, T4>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4)
        {
            return var4(var3(var2(var1(var0))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T5> UsePipe<T0, T1, T2, T3, T4, T5>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5)
        {
            return var5(var4(var3(var2(var1(var0)))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T6> UsePipe<T0, T1, T2, T3, T4, T5, T6>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6)
        {
            return var6(var5(var4(var3(var2(var1(var0))))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T7> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7)
        {
            return var7(var6(var5(var4(var3(var2(var1(var0)))))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T8> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8)
        {
            return var8(var7(var6(var5(var4(var3(var2(var1(var0))))))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T9> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9)
        {
            return var9(var8(var7(var6(var5(var4(var3(var2(var1(var0)))))))));
        }
    }
}

#endif