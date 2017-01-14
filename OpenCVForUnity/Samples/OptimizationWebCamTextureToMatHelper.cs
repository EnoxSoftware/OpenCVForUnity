using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
    /// <summary>
    /// Optimization web cam texture to mat helper.
    /// </summary>
    public class OptimizationWebCamTextureToMatHelper : WebCamTextureToMatHelper
    {
        /// <summary>
        /// The DOWNSAMPL e_ RATI.
        /// </summary>
        public int DOWNSCALE_RATIO = 2;
        
        /// <summary>
        /// The SKI p_ FRAME.
        /// </summary>
        public int SKIP_FRAMES = 2;
        
        /// <summary>
        /// The frame count.
        /// </summary>
        int frameCount;
        
        /// <summary>
        /// The rgba mat_downscale.
        /// </summary>
        Mat downScaleRgbaMat;

        /// <summary>
        /// Whether this frame is SkipFrame.
        /// </summary>
        /// <returns><c>true</c> if this instance is skip frame; otherwise, <c>false</c>.</returns>
        public bool IsSkipFrame ()
        {
            frameCount++;

            if (SKIP_FRAMES <= 0)
                return true;

            if (frameCount % SKIP_FRAMES == 0) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get the Mat that downscaled the original Mat.
        /// if DOWNSCALE <= 1 , return originalMat. 
        /// </summary>
        /// <returns>The down scale mat.</returns>
        /// <param name="originalMat">Original mat.</param>
        public Mat GetDownScaleMat (Mat originalMat)
        {
            if (DOWNSCALE_RATIO <= 1)
                return originalMat;

            if (downScaleRgbaMat == null) {
                downScaleRgbaMat = new Mat ();
            }

            Imgproc.resize (originalMat, downScaleRgbaMat, new Size (), 1.0 / DOWNSCALE_RATIO, 1.0 / DOWNSCALE_RATIO, Imgproc.INTER_LINEAR);

            return downScaleRgbaMat;
        }

        public new void Dispose ()
        {
            if (downScaleRgbaMat != null) {
                downScaleRgbaMat.Dispose ();
                downScaleRgbaMat = null;
            }

            base.Dispose ();
        }
    }
}