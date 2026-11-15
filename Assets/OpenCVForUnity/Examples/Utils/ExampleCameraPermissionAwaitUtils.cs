#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat.Grabbers;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Camera permission wait helpers for OpenCVForUnity Example scenes using async/await instead of coroutines.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="WebCamTextureGrabberUtils.WaitForCameraPermissionAsync"/> with
    /// <see cref="SourceToMatHostMarshaling.ResolveCapturedSynchronizationContext"/> and
    /// <see cref="SourceToMatSynchronizationContextScope"/> so mobile and WebGL hosts pump the captured Unity
    /// synchronization context correctly during permission dialogs.
    /// <para>
    /// Intended for the same call sites as coroutines (for example <c>Start</c> and UI event handlers): invoke
    /// <see cref="WaitForCameraPermissionAsync"/> from the Unity main thread. The first call captures the host main
    /// thread id and <see cref="SynchronizationContext.Current"/>; if that first call is not on the main thread,
    /// later waits may not marshal or yield correctly.
    /// </para>
    /// </remarks>
    internal static class ExampleCameraPermissionAwaitUtils
    {
        // Private Fields
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static int _mainThreadId;

#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static SynchronizationContext _mainThreadContext;

#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static bool _hostAffinityCaptured;

        // Public Methods
        /// <summary>
        /// Waits for camera permission on platforms that require it.
        /// </summary>
        /// <remarks>
        /// Call from the Unity main thread (same assumption as legacy permission coroutines in Examples). Editor and
        /// unsupported platforms return <see langword="true"/> immediately via the product helper.
        /// </remarks>
        /// <param name="cancellationToken">Cooperative cancellation token.</param>
        /// <returns><see langword="true"/> when camera access is authorized; otherwise <see langword="false"/>.</returns>
        public static async Task<bool> WaitForCameraPermissionAsync(CancellationToken cancellationToken = default)
        {
            EnsureHostAffinityCaptured();

            SynchronizationContext hostLoopContext =
                SourceToMatHostMarshaling.ResolveCapturedSynchronizationContext(
                    _mainThreadId,
                    _mainThreadContext);

            using (new SourceToMatSynchronizationContextScope(hostLoopContext))
            {
                return await WebCamTextureGrabberUtils.WaitForCameraPermissionAsync(
                    hostLoopContext,
                    cancellationToken);
            }
        }

        // Private Methods
        private static void EnsureHostAffinityCaptured()
        {
            if (_hostAffinityCaptured)
            {
                return;
            }

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _mainThreadContext = SynchronizationContext.Current;
            _hostAffinityCaptured = true;
        }
    }
}

#endif
