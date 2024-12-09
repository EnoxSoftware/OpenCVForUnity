using UnityEngine;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// A class for measuring FPS. Simply call `MeasureFPS()` periodically to measure FPS.
    /// </summary>
    public class FPSCounter
    {
        // Variable for counting frames
        private int frameCount = 0;

        // The time when the last measurement was taken
        private float lastMeasureTime = 0.0f;

        // The measured FPS
        private float currentFPS = 0.0f;

        // Measurement interval (in seconds)
        private float measureInterval = 1.0f;

        /// <summary>
        /// Constructor to set the measurement interval.
        /// </summary>
        /// <param name="interval">FPS measurement interval (in seconds)</param>
        public FPSCounter(float interval = 1.0f)
        {
            measureInterval = interval;
            lastMeasureTime = Time.time;
        }

        /// <summary>
        /// Measures FPS. Call this method periodically.
        /// </summary>
        public void MeasureFPS()
        {
            frameCount++;

            float currentTime = Time.time;
            float elapsedTime = currentTime - lastMeasureTime;

            if (elapsedTime >= measureInterval)
            {
                currentFPS = frameCount / elapsedTime;
                frameCount = 0;
                lastMeasureTime = currentTime;
            }
        }

        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        /// <returns>Current FPS</returns>
        public float GetCurrentFPS()
        {
            return currentFPS;
        }
    }
}

