using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// GrabCut Example
    /// An example of background removal using the Imgproc.grabCut function.
    /// http://docs.opencv.org/3.1.0/d8/d83/tutorial_py_grabcut.html
    /// </summary>
    public class GrabCutExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            Texture2D imageTexture = Resources.Load ("lena") as Texture2D;

            Mat image = new Mat (imageTexture.height, imageTexture.width, CvType.CV_8UC3);

            Utils.texture2DToMat (imageTexture, image);
            Debug.Log ("image.ToString() " + image.ToString ());

            Texture2D maskTexture = Resources.Load ("lena_grabcut_mask") as Texture2D;

            Mat mask = new Mat (imageTexture.height, imageTexture.width, CvType.CV_8UC1);

            Utils.texture2DToMat (maskTexture, mask);
            Debug.Log ("mask.ToString() " + mask.ToString ());


            OpenCVForUnity.Rect rectangle = new OpenCVForUnity.Rect (10, 10, image.cols () - 20, image.rows () - 20);

            Mat bgdModel = new Mat (); // extracted features for background
            Mat fgdModel = new Mat (); // extracted features for foreground
                        
            convertToGrabCutValues (mask); // from grayscale values to grabcut values 

            int iterCount = 5;
//          Imgproc.grabCut (image, mask, rectangle, bgdModel, fgdModel, iterCount, Imgproc.GC_INIT_WITH_RECT);
            Imgproc.grabCut (image, mask, rectangle, bgdModel, fgdModel, iterCount, Imgproc.GC_INIT_WITH_MASK);

            convertToGrayScaleValues (mask); // back to grayscale values
            Imgproc.threshold (mask, mask, 128, 255, Imgproc.THRESH_TOZERO);

            Mat foreground = new Mat (image.size (), CvType.CV_8UC3, new Scalar (0, 0, 0));
            image.copyTo (foreground, mask);


            Texture2D texture = new Texture2D (image.cols (), image.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (foreground, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        }

        private void convertToGrayScaleValues (Mat mask)
        {
            int width = mask.rows ();
            int height = mask.cols ();
            byte[] buffer = new byte[width * height];
            mask.get (0, 0, buffer);
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    int value = buffer [y * width + x];

                    if (value == Imgproc.GC_BGD) {
                        buffer [y * width + x] = 0; // for sure background
                    } else if (value == Imgproc.GC_PR_BGD) {
                        buffer [y * width + x] = 85; // probably background
                    } else if (value == Imgproc.GC_PR_FGD) {
                        buffer [y * width + x] = (byte)170; // probably foreground
                    } else {
                        buffer [y * width + x] = (byte)255; // for sure foreground
                    }
                }
            }
            mask.put (0, 0, buffer);
        }

        private void convertToGrabCutValues (Mat mask)
        {
            int width = mask.rows ();
            int height = mask.cols ();
            byte[] buffer = new byte[width * height];
            mask.get (0, 0, buffer);
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    int value = buffer [y * width + x];
                    if (value >= 0 && value < 64) {
                        buffer [y * width + x] = Imgproc.GC_BGD; // for sure background
                    } else if (value >= 64 && value < 128) {
                        buffer [y * width + x] = Imgproc.GC_PR_BGD; // probably background
                    } else if (value >= 128 && value < 192) {
                        buffer [y * width + x] = Imgproc.GC_PR_FGD; // probably foreground
                    } else {
                        buffer [y * width + x] = Imgproc.GC_FGD; // for sure foreground
                    }
                }
            }
            mask.put (0, 0, buffer);
        }

        // Update is called once per frame
        void Update ()
        {
    
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}