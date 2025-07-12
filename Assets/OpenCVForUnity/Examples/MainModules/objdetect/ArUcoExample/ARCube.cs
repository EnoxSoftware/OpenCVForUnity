using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// A class that manages a 3D cube object displayed in AR space.
    /// This class provides functionality for generating info plate textures,
    /// handling show/hide animations, and more.
    /// </summary>
    /// <remarks>
    /// - Generates and applies an info plate texture based on specified text.
    /// - Updates the info plate content using the AR marker name.
    /// - Controls appearance and disappearance animations using coroutines for smooth transitions.
    /// - Utilizes OpenCV for texture generation, enabling customizable info display.
    /// </remarks>
    public class ARCube : MonoBehaviour
    {
        // Public Fields
        public GameObject InfoPlate;

        // Private Fields
        private Coroutine _enterAnimationCoroutine = null;
        private Coroutine _exitAnimationCoroutine = null;

        // Public Methods
        /// <summary>
        /// Sets the texture of the info plate based on the given AR marker name.
        /// </summary>
        /// <param name="arUcoMarkerName">The name of the AR marker</param>
        public void SetInfoPlateTexture(string arUcoMarkerName)
        {
            if (InfoPlate == null) return;

            Texture newTexture = ARCube.CreateInfoPlateTexture(arUcoMarkerName, 200, 200);

            // Get the Renderer component
            Renderer renderer = InfoPlate.transform.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError("ARCube does not have a Renderer component.");
                return;
            }

            // Change the texture
            renderer.material.mainTexture = newTexture;
            Debug.Log("Updated ARCube texture.");
        }

        /// <summary>
        /// Draws the specified text on a Mat of size width x height and converts it to a Texture2D.
        /// </summary>
        /// <param name="text">Text to be rendered</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <returns>The generated Texture2D</returns>
        public static Texture2D CreateInfoPlateTexture(string text, int width, int height)
        {
            // Create an OpenCV Mat (4 channels, transparent background)
            Mat mat = new Mat(height, width, CvType.CV_8UC4, new Scalar(0, 0, 0, 0));

            // Split text by spaces to insert line breaks
            string[] lines = text.Split(' ');

            // OpenCV font settings
            int fontFace = Imgproc.FONT_HERSHEY_SIMPLEX;
            double fontScale = 1.0;
            int thickness = 2;

            // Determine the maximum width among text lines and calculate total height
            int[] baseLine = new int[1];
            int maxTextWidth = 0;
            int totalTextHeight = 0;
            Size[] textSizes = new Size[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                textSizes[i] = Imgproc.getTextSize(lines[i], fontFace, fontScale, thickness, baseLine);
                maxTextWidth = Mathf.Max(maxTextWidth, (int)textSizes[i].width);
                totalTextHeight += (int)textSizes[i].height + baseLine[0] + 5;
            }

            // If text width exceeds the image width, adjust font scale
            if (maxTextWidth > width * 0.9)
            {
                fontScale *= (width * 0.9) / maxTextWidth;
                maxTextWidth = (int)(maxTextWidth * fontScale);
                totalTextHeight = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    textSizes[i] = Imgproc.getTextSize(lines[i], fontFace, fontScale, thickness, baseLine);
                    totalTextHeight += (int)textSizes[i].height + baseLine[0] + 5;
                }
            }

            // Calculate margins
            int marginX = (width - maxTextWidth) / 2;
            int marginY = (height - totalTextHeight) / 2;

            // Draw a black rectangle for the text background
            int rectX = marginX - 10;
            int rectY = marginY - 10;
            Imgproc.rectangle(mat, new Point(rectX, rectY), new Point(rectX + maxTextWidth + 20, rectY + totalTextHeight + 10), new Scalar(0, 0, 0, 255), -1);

            // Draw each line of text (centered)
            int y = marginY;
            for (int i = 0; i < lines.Length; i++)
            {
                int textX = (width - (int)textSizes[i].width) / 2;
                y += (int)textSizes[i].height;
                Imgproc.putText(mat, lines[i], new Point(textX, y), fontFace, fontScale, new Scalar(255, 255, 255, 255), thickness);
                y += baseLine[0] + 5;
            }

            // Convert Mat to Texture2D
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(mat, texture);

            // Release Mat resources
            mat.Dispose();

            return texture;
        }

        /// <summary>
        /// Starts the enter animation, making the object appear smoothly by scaling it along the Z-axis.
        /// If an exit animation is running, it is stopped before starting the enter animation.
        /// </summary>
        /// <param name="obj">The GameObject to animate</param>
        /// <param name="startScaleZ">Initial scale value along the Z-axis</param>
        /// <param name="endScaleZ">Final scale value along the Z-axis</param>
        /// <param name="duration">Duration of the animation in seconds</param>
        public IEnumerator EnterAnimation(GameObject obj, float startScaleZ, float endScaleZ, float duration)
        {
            if (_exitAnimationCoroutine != null)
            {
                StopCoroutine(_exitAnimationCoroutine);
                _exitAnimationCoroutine = null;
            }

            if (_enterAnimationCoroutine != null)
            {
                StopCoroutine(_enterAnimationCoroutine);
                _enterAnimationCoroutine = null;
            }

            obj.SetActive(true);
            _enterAnimationCoroutine = StartCoroutine(AnimateScale(obj, startScaleZ, endScaleZ, duration));
            yield return _enterAnimationCoroutine;
            _enterAnimationCoroutine = null;
        }

        /// <summary>
        /// Starts the exit animation, making the object disappear smoothly by scaling it along the Z-axis.
        /// If an enter animation is running, it is stopped before starting the exit animation.
        /// </summary>
        /// <param name="obj">The GameObject to animate</param>
        /// <param name="startScaleZ">Initial scale value along the Z-axis</param>
        /// <param name="endScaleZ">Final scale value along the Z-axis</param>
        /// <param name="duration">Duration of the animation in seconds</param>
        public IEnumerator ExitAnimation(GameObject obj, float startScaleZ, float endScaleZ, float duration)
        {
            if (_enterAnimationCoroutine != null)
            {
                StopCoroutine(_enterAnimationCoroutine);
                _enterAnimationCoroutine = null;
            }

            if (_exitAnimationCoroutine != null)
            {
                StopCoroutine(_exitAnimationCoroutine);
                _exitAnimationCoroutine = null;
            }

            _exitAnimationCoroutine = StartCoroutine(AnimateScale(obj, startScaleZ, endScaleZ, duration));
            yield return _exitAnimationCoroutine;

            if (_exitAnimationCoroutine == null) yield return null;
            obj.SetActive(false);
            _exitAnimationCoroutine = null;
        }

        // Private Methods
        /// <summary>
        /// Performs a scaling animation along the Z-axis over a specified duration.
        /// </summary>
        /// <param name="obj">The GameObject to animate</param>
        /// <param name="startScaleZ">Initial scale value along the Z-axis</param>
        /// <param name="endScaleZ">Final scale value along the Z-axis</param>
        /// <param name="duration">Duration of the animation in seconds</param>
        private IEnumerator AnimateScale(GameObject obj, float startScaleZ, float endScaleZ, float duration)
        {
            float elapsedTime = 0f;
            Vector3 initialScale = obj.transform.localScale;
            Vector3 targetScale = new Vector3(initialScale.x, initialScale.y, endScaleZ);

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                obj.transform.localScale = new Vector3(initialScale.x, initialScale.y, Mathf.Lerp(startScaleZ, endScaleZ, t));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            obj.transform.localScale = targetScale;
        }
    }
}
