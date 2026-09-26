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
    /// Renders a random 1D data series as a 2D line plot using the OpenCV plot module.
    ///
    /// Demonstrates:
    /// - Creating Plot2d from a CV_64F column vector
    /// - Customizing plot background and line colors
    /// - Converting rendered BGR Mat to Unity Texture2D for display
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Core"/>: randu
    /// - <see cref="Plot2d"/>: create, setPlotBackgroundColor, setPlotLineColor, render
    /// - <see cref="Imgproc"/>: cvtColor
    /// - <see cref="OpenCVMatUnityUtils"/>
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
            // Plot data must be a 1xN or Nx1 matrix of type CV_64F (double).
            Mat data = new Mat(30, 1, CvType.CV_64F);
            Core.randu(data, 0, 500); // random values

            Mat plot_result = new Mat();

            Plot2d plot = Plot2d.create(data);
            plot.setPlotBackgroundColor(new Scalar(50, 50, 50));
            plot.setPlotLineColor(new Scalar(50, 50, 255));
            // render() draws the plot into an output BGR Mat.
            plot.render(plot_result);

            // Convert BGR to RGB for Unity Texture2D display.
            Imgproc.cvtColor(plot_result, plot_result, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(plot_result.cols(), plot_result.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(plot_result, texture);

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
