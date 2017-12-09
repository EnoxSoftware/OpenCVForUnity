using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Plot Example
    /// An examples of creating 2D plot using the plot (Plot function for Mat data) module.
    /// </summary>
    public class PlotExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            // Plot data must be a 1xN or Nx1 matrix.
            // Plot data type must be double (CV_64F) 
            Mat data = new Mat (30, 1, CvType.CV_64F);
            Core.randu (data, 0, 500); // random values
            
            Mat plot_result = new Mat ();
            
//            Plot2d plot = Plot.createPlot2d (data);
            Plot2d plot = Plot2d.create (data);
            plot.setPlotBackgroundColor (new Scalar (50, 50, 50));
            plot.setPlotLineColor (new Scalar (50, 50, 255));
            plot.render (plot_result);

            Imgproc.cvtColor (plot_result, plot_result, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D (plot_result.cols (), plot_result.rows (), TextureFormat.RGBA32, false);
            Utils.matToTexture2D (plot_result, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
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