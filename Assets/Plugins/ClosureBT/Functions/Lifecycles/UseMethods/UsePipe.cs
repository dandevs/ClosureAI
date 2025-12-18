#if UNITASK_INSTALLED
using System;
using System.Runtime.CompilerServices;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Chains multiple variable transformations together in a pipeline.
        /// Each transformation function receives the output of the previous transformation.
        /// </summary>
        /// <typeparam name="T0">The type of the initial source variable.</typeparam>
        /// <typeparam name="T1">The type of the final output variable.</typeparam>
        /// <param name="var0">The source variable to start the pipeline.</param>
        /// <param name="var1">A function to transform the source variable.</param>
        /// <returns>A variable containing the final transformed output.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T1> UsePipe<T0, T1>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1)
        {
            return var1(var0);
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T2> UsePipe<T0, T1, T2>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2)
        {
            return var2(var1(var0));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T3> UsePipe<T0, T1, T2, T3>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3)
        {
            return var3(var2(var1(var0)));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T4> UsePipe<T0, T1, T2, T3, T4>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4)
        {
            return var4(var3(var2(var1(var0))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T5> UsePipe<T0, T1, T2, T3, T4, T5>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5)
        {
            return var5(var4(var3(var2(var1(var0)))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T6> UsePipe<T0, T1, T2, T3, T4, T5, T6>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6)
        {
            return var6(var5(var4(var3(var2(var1(var0))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T7> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7)
        {
            return var7(var6(var5(var4(var3(var2(var1(var0)))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T8> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8)
        {
            return var8(var7(var6(var5(var4(var3(var2(var1(var0))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T9> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9)
        {
            return var9(var8(var7(var6(var5(var4(var3(var2(var1(var0)))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T10> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10)
        {
            return var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0))))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T11> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10, Func<VariableType<T10>, VariableType<T11>> var11)
        {
            return var11(var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0)))))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T12> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10, Func<VariableType<T10>, VariableType<T11>> var11, Func<VariableType<T11>, VariableType<T12>> var12)
        {
            return var12(var11(var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0))))))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T13> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10, Func<VariableType<T10>, VariableType<T11>> var11, Func<VariableType<T11>, VariableType<T12>> var12, Func<VariableType<T12>, VariableType<T13>> var13)
        {
            return var13(var12(var11(var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0)))))))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T14> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10, Func<VariableType<T10>, VariableType<T11>> var11, Func<VariableType<T11>, VariableType<T12>> var12, Func<VariableType<T12>, VariableType<T13>> var13, Func<VariableType<T13>, VariableType<T14>> var14)
        {
            return var14(var13(var12(var11(var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0))))))))))))));
        }

        /// <inheritdoc cref="UsePipe{T0, T1}(VariableType{T0}, Func{VariableType{T0}, VariableType{T1}})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariableType<T15> UsePipe<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(VariableType<T0> var0, Func<VariableType<T0>, VariableType<T1>> var1, Func<VariableType<T1>, VariableType<T2>> var2, Func<VariableType<T2>, VariableType<T3>> var3, Func<VariableType<T3>, VariableType<T4>> var4, Func<VariableType<T4>, VariableType<T5>> var5, Func<VariableType<T5>, VariableType<T6>> var6, Func<VariableType<T6>, VariableType<T7>> var7, Func<VariableType<T7>, VariableType<T8>> var8, Func<VariableType<T8>, VariableType<T9>> var9, Func<VariableType<T9>, VariableType<T10>> var10, Func<VariableType<T10>, VariableType<T11>> var11, Func<VariableType<T11>, VariableType<T12>> var12, Func<VariableType<T12>, VariableType<T13>> var13, Func<VariableType<T13>, VariableType<T14>> var14, Func<VariableType<T14>, VariableType<T15>> var15)
        {
            return var15(var14(var13(var12(var11(var10(var9(var8(var7(var6(var5(var4(var3(var2(var1(var0)))))))))))))));
        }
    }
}

#endif
