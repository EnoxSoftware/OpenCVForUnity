using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// GrabCut Example
    /// Segments foreground from background using an initial trimap mask and GrabCut refinement.
    ///
    /// Demonstrates:
    /// - Converting a grayscale trimap to GrabCut label values
    /// - Foreground extraction with <see cref="Imgproc.grabCut"/> (GC_INIT_WITH_MASK)
    /// - Copying foreground pixels via a binary mask
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="OpenCVForUnity.CoreModule.Rect"/>
    /// - <see cref="Imgproc"/>: grabCut, threshold, GC_BGD, GC_PR_BGD, GC_PR_FGD, GC_FGD
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.1.0/d8/d83/tutorial_py_grabcut.html
    /// </para>
    /// </remarks>
    public class GrabCutExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Unity Lifecycle Methods
        private void Start()
        {
            Texture2D imageTexture = Resources.Load("face") as Texture2D;

            Mat image = new Mat(imageTexture.height, imageTexture.width, CvType.CV_8UC3);

            OpenCVMatUnityUtils.Texture2DToMat(imageTexture, image);
            Debug.Log("image.ToString() " + image.ToString(), this);

            Texture2D maskTexture = Resources.Load("face_grabcut_mask") as Texture2D;

            Mat mask = new Mat(imageTexture.height, imageTexture.width, CvType.CV_8UC1);

            OpenCVMatUnityUtils.Texture2DToMat(maskTexture, mask);
            Debug.Log("mask.ToString() " + mask.ToString(), this);

            OpenCVForUnity.CoreModule.Rect rectangle = new OpenCVForUnity.CoreModule.Rect(10, 10, image.cols() - 20, image.rows() - 20);

            // GMM models updated iteratively by GrabCut.
            Mat bgdModel = new Mat(); // extracted features for background
            Mat fgdModel = new Mat(); // extracted features for foreground

            // Map grayscale trimap values (0-255) to GrabCut label constants.
            ConvertToGrabCutValues(mask);

            int iterCount = 5;
            //Imgproc.grabCut (image, mask, rectangle, bgdModel, fgdModel, iterCount, Imgproc.GC_INIT_WITH_RECT);
            // Refine segmentation using the user-provided mask as initialization.
            Imgproc.grabCut(image, mask, rectangle, bgdModel, fgdModel, iterCount, Imgproc.GC_INIT_WITH_MASK);

            // Convert GrabCut labels back to grayscale for display and masking.
            ConvertToGrayScaleValues(mask);
            // Keep only definite/probable foreground (values >= 128).
            Imgproc.threshold(mask, mask, 128, 255, Imgproc.THRESH_TOZERO);

            // Copy source pixels where the foreground mask is non-zero.
            Mat foreground = new Mat(image.size(), CvType.CV_8UC3, new Scalar(0, 0, 0));
            image.copyTo(foreground, mask);

            Texture2D texture = new Texture2D(image.cols(), image.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(foreground, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void Update()
        {

        }

        // Private Methods
        /// <summary>
        /// Maps GrabCut label constants back to grayscale visualization values.
        /// </summary>
        private void ConvertToGrayScaleValues(Mat mask)
        {
            int width = mask.rows();
            int height = mask.cols();
            byte[] buffer = new byte[width * height];
            mask.get(0, 0, buffer);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int value = buffer[y * width + x];

                    if (value == Imgproc.GC_BGD)
                    {
                        buffer[y * width + x] = 0; // for sure background
                    }
                    else if (value == Imgproc.GC_PR_BGD)
                    {
                        buffer[y * width + x] = 85; // probably background
                    }
                    else if (value == Imgproc.GC_PR_FGD)
                    {
                        buffer[y * width + x] = (byte)170; // probably foreground
                    }
                    else
                    {
                        buffer[y * width + x] = (byte)255; // for sure foreground
                    }
                }
            }
            mask.put(0, 0, buffer);
        }

        /// <summary>
        /// Maps grayscale trimap bands to GrabCut foreground/background labels.
        /// </summary>
        private void ConvertToGrabCutValues(Mat mask)
        {
            int width = mask.rows();
            int height = mask.cols();
            byte[] buffer = new byte[width * height];
            mask.get(0, 0, buffer);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int value = buffer[y * width + x];
                    if (value >= 0 && value < 64)
                    {
                        buffer[y * width + x] = Imgproc.GC_BGD; // for sure background
                    }
                    else if (value >= 64 && value < 128)
                    {
                        buffer[y * width + x] = Imgproc.GC_PR_BGD; // probably background
                    }
                    else if (value >= 128 && value < 192)
                    {
                        buffer[y * width + x] = Imgproc.GC_PR_FGD; // probably foreground
                    }
                    else
                    {
                        buffer[y * width + x] = Imgproc.GC_FGD; // for sure foreground
                    }
                }
            }
            mask.put(0, 0, buffer);
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
