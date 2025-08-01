using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.PlotModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Plot Example
    /// An examples of creating 2D plot using the plot (Plot function for Mat data) module.
    /// </summary>
    public class PlotExample : MonoBehaviour
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
            // Plot data must be a 1xN or Nx1 matrix.
            // Plot data type must be double (CV_64F)
            Mat data = new Mat(30, 1, CvType.CV_64F);
            Core.randu(data, 0, 500); // random values

            Mat plot_result = new Mat();

            //Plot2d plot = Plot.createPlot2d (data);
            Plot2d plot = Plot2d.create(data);
            plot.setPlotBackgroundColor(new Scalar(50, 50, 50));
            plot.setPlotLineColor(new Scalar(50, 50, 255));
            plot.render(plot_result);

            Imgproc.cvtColor(plot_result, plot_result, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(plot_result.cols(), plot_result.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(plot_result, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void Update()
        {

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
