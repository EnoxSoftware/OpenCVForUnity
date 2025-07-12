using UnityEngine;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// A class for measuring FPS. Simply call `MeasureFPS()` periodically to measure FPS.
    /// </summary>
    public class FPSCounter
    {
        // Private Fields
        // Variable for counting frames
        private int _frameCount = 0;

        // The time when the last measurement was taken
        private float _lastMeasureTime = 0.0f;

        // The measured FPS
        private float _currentFPS = 0.0f;

        // Measurement interval (in seconds)
        private float _measureInterval = 1.0f;

        /// <summary>
        /// Constructor to set the measurement interval.
        /// </summary>
        /// <param name="interval">FPS measurement interval (in seconds)</param>
        public FPSCounter(float interval = 1.0f)
        {
            _measureInterval = interval;
            _lastMeasureTime = Time.time;
        }

        /// <summary>
        /// Measures FPS. Call this method periodically.
        /// </summary>
        public void MeasureFPS()
        {
            _frameCount++;

            float currentTime = Time.time;
            float elapsedTime = currentTime - _lastMeasureTime;

            if (elapsedTime >= _measureInterval)
            {
                _currentFPS = _frameCount / elapsedTime;
                _frameCount = 0;
                _lastMeasureTime = currentTime;
            }
        }

        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        /// <returns>Current FPS</returns>
        public float GetCurrentFPS()
        {
            return _currentFPS;
        }
    }
}
