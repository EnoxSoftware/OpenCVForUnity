using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.Extensions.SourceToMat;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Host-loop yield helpers for OpenCVForUnity Example scenes using async/await instead of coroutines.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="SourceToMatHostWait.YieldHostLoopTickAsync"/> so WebGL and single-threaded hosts
    /// pump the captured Unity synchronization context correctly.
    /// <para>
    /// Intended for the same call sites as coroutines (for example <c>Start</c> and UI event handlers): invoke
    /// <see cref="YieldNextFrameAsync"/> from the Unity main thread. The first call captures
    /// <see cref="SynchronizationContext.Current"/>; if that first call is not on the main thread, later yields may
    /// not pump the Unity host loop correctly.
    /// </para>
    /// </remarks>
    internal static class ExampleMainLoopAwaitUtils
    {
        // Private Fields
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static SynchronizationContext _hostLoopContext;

        // Public Methods
        /// <summary>
        /// Advances one host-loop tick (similar to <c>yield return null</c> in a coroutine).
        /// </summary>
        /// <remarks>
        /// Call from the Unity main thread (same assumption as <c>yield return null</c> in a coroutine). On the first
        /// call when no context is stored yet, captures <see cref="SynchronizationContext.Current"/> for all later
        /// yields in the process.
        /// </remarks>
        /// <param name="cancellationToken">Cooperative cancellation token.</param>
        /// <returns>A task that completes after one host-loop continuation.</returns>
        public static Task YieldNextFrameAsync(CancellationToken cancellationToken = default)
        {
            EnsureHostLoopContextCaptured();
            return SourceToMatHostWait.YieldHostLoopTickAsync(_hostLoopContext, cancellationToken);
        }

        // Private Methods
        private static void EnsureHostLoopContextCaptured()
        {
            if (_hostLoopContext != null)
            {
                return;
            }

            _hostLoopContext = SynchronizationContext.Current;
        }
    }
}
