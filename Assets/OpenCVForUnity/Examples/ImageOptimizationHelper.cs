using OpenCVForUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Image optimization helper.
    /// v 1.0.0
    /// </summary>
    public class ImageOptimizationHelper : MonoBehaviour
    {
        /// <summary>
        /// The downscale ratio.
        /// </summary>
        [SerializeField, FormerlySerializedAs("downscaleRatio"), TooltipAttribute ("Set the ratio of down scaling.")]
        protected float _downscaleRatio = 2f;
        public float downscaleRatio {
            get { return _downscaleRatio; } 
            set { _downscaleRatio = Mathf.Clamp(value, 1f, float.MaxValue); }
        }
        
        /// <summary>
        /// The frame skipping ratio.
        /// </summary>
        [SerializeField, FormerlySerializedAs("frameSkippingRatio"), TooltipAttribute ("Set the ratio of frame skipping.")]
        protected int _frameSkippingRatio = 2;
        public int frameSkippingRatio {
            get { return _frameSkippingRatio; } 
            set { _frameSkippingRatio = (int)Mathf.Clamp(value, 1f, float.MaxValue); }
        }

        /// <summary>
        /// The frame count.
        /// </summary>
        protected int frameCount = 0;
        
        /// <summary>
        /// The downscale frame mat.
        /// </summary>
        protected Mat downScaleFrameMat;

        protected void OnValidate()
        {
            _downscaleRatio = Mathf.Clamp(_downscaleRatio, 1f, float.MaxValue);
            _frameSkippingRatio = (int)Mathf.Clamp(_frameSkippingRatio, 1f, float.MaxValue);
        }

        /// <summary>
        /// Indicates whether the current frame is skipped.
        /// </summary>
        /// <returns><c>true</c>, if the current frame is skipped, <c>false</c> otherwise.</returns>
        public virtual bool IsCurrentFrameSkipped ()
        {
            frameCount++;

            if (frameCount % frameSkippingRatio == 0) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the mat that downscaled the original mat.
        /// if downscaleRatio == 1 , return originalMat. 
        /// </summary>
        /// <returns>The downscale mat.</returns>
        /// <param name="originalMat">Original mat.</param>
        public virtual Mat GetDownScaleMat (Mat originalMat)
        {
            if (Mathf.Approximately(_downscaleRatio, 1f))
                return originalMat;

            if (downScaleFrameMat == null) {
                downScaleFrameMat = new Mat ();
            }

            Imgproc.resize (originalMat, downScaleFrameMat, new Size (), 1.0 / _downscaleRatio, 1.0 / _downscaleRatio, Imgproc.INTER_LINEAR);

            return downScaleFrameMat;
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public virtual void Dispose ()
        {
            frameCount = 0;

            if (downScaleFrameMat != null) {
                downScaleFrameMat.Dispose ();
                downScaleFrameMat = null;
            }
        }
    }
}