using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// Plot sample.
		/// </summary>
		public class PlotSample : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{
						// Plot data must be a 1xN or Nx1 matrix.
						// Plot data type must be double (CV_64F) 
						Mat data = new Mat (30, 1, CvType.CV_64F);
						Core.randu (data, 0, 500); // random values
			
						Mat plot_result = new Mat ();
			
						Plot2d plot = Plot.createPlot2d (data);
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

				public void OnBackButton ()
				{
						#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("OpenCVForUnitySample");
						#else
						Application.LoadLevel ("OpenCVForUnitySample");
						#endif
				}
		}
}
