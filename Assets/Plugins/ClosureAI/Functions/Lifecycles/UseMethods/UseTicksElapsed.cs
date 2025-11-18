#if UNITASK_INSTALLED
namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that tracks elapsed ticks since the node started.
        /// The elapsed ticks accumulate continuously while the node is active.
        /// </summary>
        /// <returns>A variable containing the elapsed ticks as an int.</returns>
        public static VariableType<int> UseTicksElapsed()
        {
            var ticks = Variable(static () => 0);

            OnPreTick(() =>
            {
                ticks.Value += 1;
            });

            return ticks;
        }
    }
}


#endif
