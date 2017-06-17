using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Optimization webcam texture to mat helper.
    /// v 1.0.1
    /// </summary>
    public class OptimizationWebCamTextureToMatHelper : WebCamTextureToMatHelper
    {
        /// <summary>
        /// The downscale ratio.
        /// </summary>
        public float downscaleRatio = 2;
        
        /// <summary>
        /// The frame skipping ratio.
        /// </summary>
        public int frameSkippingRatio = 2;
        
        /// <summary>
        /// The frame count.
        /// </summary>
        protected int frameCount = 0;
        
        /// <summary>
        /// The rgba downscale mat.
        /// </summary>
        protected Mat downScaleRgbaMat;

        /// <summary>
        /// Indicates whether the current frame is skipped.
        /// </summary>
        /// <returns><c>true</c>, if the current frame is skipped, <c>false</c> otherwise.</returns>
        public virtual bool IsCurrentFrameSkipped ()
        {
            frameCount++;

            if (frameSkippingRatio <= 0)
                return false;

            if (frameCount % frameSkippingRatio == 0) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the Mat that downscaled the original Mat.
        /// if downscaleRatio <= 1 , return originalMat. 
        /// </summary>
        /// <returns>The downscale mat.</returns>
        /// <param name="originalMat">Original mat.</param>
        public virtual Mat GetDownScaleMat (Mat originalMat)
        {
            if (downscaleRatio <= 1)
                return originalMat;

            if (downScaleRgbaMat == null) {
                downScaleRgbaMat = new Mat ();
            }

            Imgproc.resize (originalMat, downScaleRgbaMat, new Size (), 1.0 / downscaleRatio, 1.0 / downscaleRatio, Imgproc.INTER_LINEAR);

            return downScaleRgbaMat;
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        protected override void _Dispose ()
        {
            frameCount = 0;

            if (downScaleRgbaMat != null) {
                downScaleRgbaMat.Dispose ();
                downScaleRgbaMat = null;
            }

            base._Dispose ();
        }
    }
}