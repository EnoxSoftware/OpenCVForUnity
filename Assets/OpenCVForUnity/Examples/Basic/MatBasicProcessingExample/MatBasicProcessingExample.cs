using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mat Basic Processing Example
    /// </summary>
    public class MatBasicProcessingExample : MonoBehaviour
    {
        // Public Fields
        public ScrollRect ExampleCodeScrollRect;
        public UnityEngine.UI.Text ExampleCodeText;
        public ScrollRect ExecutionResultScrollRect;
        public UnityEngine.UI.Text ExecutionResultText;

        // Unity Lifecycle Methods
        private IEnumerator Start()
        {
            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        // Private Methods
        private void UpdateScrollRect()
        {
            ExampleCodeScrollRect.verticalNormalizedPosition = ExecutionResultScrollRect.verticalNormalizedPosition = 1f;
        }

        // Public Methods
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        public void OnInitializationExampleButtonClick()
        {
            //
            // initialization example
            //
            // Showcase initialization methods for different matrix types and sizes.
            //

            // 3x3 matrix (set array value)
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            mat1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log("mat1=" + mat1.dump());

            // 2x2 rotation matrix
            double angle = 30, a = Math.Cos(angle * Math.PI / 180), b = Math.Sin(angle * Math.PI / 180);
            Mat mat2 = new Mat(2, 2, CvType.CV_64FC1);
            mat2.put(0, 0, a, -b, b, a);
            Debug.Log("mat2=" + mat2.dump());

            // 5x5 all 1's matrix
            Mat mat3 = Mat.ones(5, 5, CvType.CV_64FC1);
            Debug.Log("mat3=" + mat3.dump());

            // 5x5 all zero's matrix
            Mat mat4 = Mat.zeros(5, 5, CvType.CV_64FC1);
            Debug.Log("mat4=" + mat4.dump());

            // 5x5 identity matrix
            Mat mat5 = Mat.eye(5, 5, CvType.CV_64FC1);
            Debug.Log("mat5=" + mat5.dump());

            // 3x3 initialize with a constant
            Mat mat6 = new Mat(3, 3, CvType.CV_64FC1, new Scalar(5));
            Debug.Log("mat6=" + mat6.dump());

            // 3x2 initialize with a uniform distribution random number
            Mat mat7 = new Mat(3, 2, CvType.CV_8UC1);
            Core.randu(mat7, 0, 256);
            Debug.Log("mat7=" + mat7.dump());

            // 3x2 initialize with a normal distribution random number
            Mat mat8 = new Mat(3, 2, CvType.CV_8UC1);
            Core.randn(mat8, 128, 10);
            Debug.Log("mat8=" + mat8.dump());

            // 2x2x3x4 matrix (4 dimensional array)
            int[] sizes = new int[] { 2, 2, 3, 4 };
            Mat mat9 = new Mat(sizes, CvType.CV_8UC1, Scalar.all(0));
            Debug.Log("mat9.dims=" + mat9.dims());
            Debug.Log("mat9.rows=" + mat9.rows() + " //When the matrix is more than 2-dimensional, the returned size is (-1, -1).");
            Debug.Log("mat9.cols=" + mat9.cols());

            ExampleCodeText.text = @"
            //
            // initialization example
            //
            // Showcase initialization methods for different matrix types and sizes.
            //

            // 3x3 matrix (set array value)
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            mat1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log (""mat1="" + mat1.dump());

            // 2x2 rotation matrix
            double angle = 30, a = Math.Cos(angle*Math.PI/180), b = Math.Sin(angle*Math.PI/180);
            Mat mat2 = new Mat (2, 2, CvType.CV_64FC1);
            mat2.put (0, 0, a, -b, b, a);
            Debug.Log (""mat2="" + mat2.dump());

            // 5x5 all 1's matrix
            Mat mat3 = Mat.ones(5, 5, CvType.CV_64FC1);
            Debug.Log (""mat3="" + mat3.dump());

            // 5x5 all zero's matrix
            Mat mat4 = Mat.zeros(5, 5, CvType.CV_64FC1);
            Debug.Log (""mat4="" + mat4.dump());

            // 5x5 identity matrix
            Mat mat5 = Mat.eye(5, 5, CvType.CV_64FC1);
            Debug.Log (""mat5="" + mat5.dump());

            // 3x3 initialize with a constant
            Mat mat6 = new Mat (3, 3, CvType.CV_64FC1, new Scalar(5));
            Debug.Log (""mat6="" + mat6.dump());

            // 3x2 initialize with a uniform distribution random number
            Mat mat7 = new Mat (3, 2, CvType.CV_8UC1);
            Core.randu (mat7, 0, 256);
            Debug.Log (""mat7="" + mat7.dump());

            // 3x2 initialize with a normal distribution random number
            Mat mat8 = new Mat (3, 2, CvType.CV_8UC1);
            Core.randn (mat8, 128, 10);
            Debug.Log (""mat8="" + mat8.dump());

            // 2x2x3x4 matrix (4 dimensional array)
            int[] sizes = new int[]{ 2, 2, 3, 4 };
            Mat mat9 = new Mat (sizes, CvType.CV_8UC1, Scalar.all (0));
            Debug.Log (""mat9.dims="" + mat9.dims());
            Debug.Log (""mat9.rows="" + mat9.rows () + "" //When the matrix is more than 2-dimensional, the returned size is (-1, -1)."");
            Debug.Log (""mat9.cols="" + mat9.cols ());
            ";

            ExecutionResultText.text = "mat1=" + mat1.dump() + "\n";
            ExecutionResultText.text += "mat2=" + mat2.dump() + "\n";
            ExecutionResultText.text += "mat3=" + mat3.dump() + "\n";
            ExecutionResultText.text += "mat4=" + mat4.dump() + "\n";
            ExecutionResultText.text += "mat5=" + mat5.dump() + "\n";
            ExecutionResultText.text += "mat6=" + mat6.dump() + "\n";
            ExecutionResultText.text += "mat7=" + mat7.dump() + "\n";
            ExecutionResultText.text += "mat8=" + mat8.dump() + "\n";
            ExecutionResultText.text += "mat9.dims=" + mat9.dims() + "\n";
            ExecutionResultText.text += "mat9.rows=" + mat9.rows() + " //When the matrix is more than 2-dimensional, the returned size is (-1, -1)." + "\n";
            ExecutionResultText.text += "mat9.cols=" + mat9.cols() + "\n";

            UpdateScrollRect();
        }

        public void OnMultiChannelExampleButtonClick()
        {
            //
            // multi channel example
            //
            // Initialization of matrices with various numbers of channels, including those with four or more channels.
            //

            // 64F, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            Debug.Log("mat1");
            Debug.Log("   dim:" + mat1.dims() + " elemSize1:" + mat1.elemSize1() + " channel:" + mat1.channels());

            // 64F, channels=10, 3x3
            Debug.Log("mat2");
            Mat mat2 = new Mat(3, 3, CvType.CV_64FC(10));
            Debug.Log("   dim:" + mat2.dims() + " elemSize1:" + mat2.elemSize1() + " channels:" + mat2.channels());

            // 64F, channles=1, 2x2x3x4 (4 dimensional array)
            Debug.Log("mat3");
            int[] sizes = new int[] { 2, 2, 3, 4 };
            Mat mat3 = new Mat(sizes, CvType.CV_64FC1);
            Debug.Log("   dim:" + mat3.dims() + " elemSize1:" + mat3.elemSize1() + " channels:" + mat3.channels());

            ExampleCodeText.text = @"
            //
            // multi channel example
            //
            // Initialization of matrices with various numbers of channels, including those with four or more channels.
            //

            // 64F, channels=1, 3x3
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            Debug.Log (""mat1"");
            Debug.Log (""   dim:"" + mat1.dims() + "" elemSize1:"" + mat1.elemSize1() + "" channel:"" + mat1.channels());

            // 64F, channels=10, 3x3
            Debug.Log (""mat2"");
            Mat mat2 = new Mat (3, 3, CvType.CV_64FC(10));
            Debug.Log (""   dim:"" + mat2.dims() + "" elemSize1:"" + mat2.elemSize1() + "" channels:"" + mat2.channels());

            // 64F, channles=1, 2x2x3x4 (4 dimensional array)
            Debug.Log (""mat3"");
            int[] sizes = new int[]{ 2, 2, 3, 4 };
            Mat mat3 = new Mat (sizes, CvType.CV_64FC1);
            Debug.Log (""   dim:"" + mat3.dims() + "" elemSize1:"" + mat3.elemSize1() + "" channels:"" + mat3.channels());
            ";

            ExecutionResultText.text = "mat1" + "\n";
            ExecutionResultText.text += "   dim:" + mat1.dims() + " elemSize1:" + mat1.elemSize1() + " channels:" + mat1.channels() + "\n";
            ExecutionResultText.text += "mat2" + "\n";
            ExecutionResultText.text += "   dim:" + mat2.dims() + " elemSize1:" + mat2.elemSize1() + " channels:" + mat2.channels() + "\n";
            ExecutionResultText.text += "mat3" + "\n";
            ExecutionResultText.text += "   dim:" + mat3.dims() + " elemSize1:" + mat3.elemSize1() + " channels:" + mat3.channels() + "\n";

            UpdateScrollRect();
        }

        public void OnDumpExampleButtonClick()
        {
            //
            // dump example
            //
            // Output the elements of the matrix as a string.
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // 32F, channels=1, 1x3x4x3
            Mat mat3 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            mat3.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            // dump
            Debug.Log("mat1=" + mat1);
            Debug.Log("mat1.dump()=" + mat1.dump());
            Debug.Log("mat1=" + mat2);
            Debug.Log("mat2.dump()=" + mat2.dump());
            Debug.Log("mat3=" + mat3);
            Debug.Log("mat3.reshape(3, new int[] { 3, 4 }).dump() =" + mat3.reshape(3, new int[] { 3, 4 }).dump() + " // If the matrix is more than 2 dimensional, the dump method is not supported, so the contents can be pseudo-output by reshape it into a 2 dimensional matrix.");

            ExampleCodeText.text = @"
            //
            // dump example
            //
            // Output the elements of the matrix as a string.
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // 32F, channels=1, 1x3x4x3
            Mat mat3 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            mat3.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            // dump
            Debug.Log(""mat1 = "" + mat1);
            Debug.Log(""mat1.dump()="" + mat1.dump());
            Debug.Log(""mat1="" + mat2);
            Debug.Log(""mat2.dump()="" + mat2.dump());
            Debug.Log(""mat3="" + mat3);
            Debug.Log(""mat3.reshape(3, new int[] { 3, 4 }).dump() ="" + mat3.reshape(3, new int[] { 3, 4 }).dump() + "" // If the matrix is more than 2 dimensional, the dump method is not supported, so the contents can be pseudo-output by reshape it into a 2 dimensional matrix."");
            ";

            ExecutionResultText.text = "mat1=" + mat1 + "\n";
            ExecutionResultText.text += "mat1.dump()=" + mat1.dump() + "\n";
            ExecutionResultText.text += "mat2=" + mat2 + "\n";
            ExecutionResultText.text += "mat2.dump()=" + mat2.dump() + "\n";
            ExecutionResultText.text += "mat3=" + mat3 + "\n";
            ExecutionResultText.text += "mat3.reshape(3, new int[] { 3, 4 }).dump()=" + mat3.reshape(3, new int[] { 3, 4 }).dump() + " // If the matrix is more than 2 dimensional, the dump method is not supported, so the contents can be pseudo-output by reshape it into a 2 dimensional matrix.";

            UpdateScrollRect();
        }

        public void OnCVExceptionHandlingExampleButtonClick()
        {
            //
            // CVException handling example
            //
            // How to display Native-side OpenCV error logs in the Unity Editor Console.
            //

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // dump
            Debug.Log("m1=" + m1);
            Debug.Log("m1.dump()=" + m1.dump());
            Debug.Log("m2=" + m2);
            Debug.Log("m2.dump()=" + m2.dump());

            ExecutionResultText.text = "m1=" + m1 + "\n";
            ExecutionResultText.text += "m1.dump()=" + m1.dump() + "\n";
            ExecutionResultText.text += "m2=" + m2 + "\n";
            ExecutionResultText.text += "m2.dump()=" + m2.dump() + "\n";

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false);

            Mat m3 = new Mat();
            Core.divide(m1, m2, m3); // element type is different.
            Debug.Log("m3=" + m3);
            ExecutionResultText.text += "m3=" + m3 + "\n";

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true);
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4); // element type is different.
                Debug.Log("m4=" + m4);
                ExecutionResultText.text += "m4=" + m4 + "\n";
            }
            catch (Exception e)
            {
                Debug.Log("CVException: " + e);
                ExecutionResultText.text += "CVException: " + e + "\n";
            }
            OpenCVDebug.SetDebugMode(false);


            ExampleCodeText.text = @"
            // CVException handling example
            //
            // How to display Native-side OpenCV error logs in the Unity Editor Console.
            //

            // 32F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_32FC1);
            m1.put (0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat (3, 3, CvType.CV_8UC1);
            m2.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // dump
            Debug.Log (""m1="" + m1);
            Debug.Log (""m1.dump()="" + m1.dump ());
            Debug.Log (""m2="" + m2);
            Debug.Log (""m2.dump()="" + m2.dump ());

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false);

            Mat m3 = new Mat();
            Core.divide(m1, m2, m3);
            Debug.Log(""m3="" + m3);

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true);
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4);
                Debug.Log(""m4="" + m4);
            }
            catch (Exception e)
            {
                Debug.Log (""CVException: "" + e);
            }
            OpenCVDebug.SetDebugMode (false);
            ";

            UpdateScrollRect();
        }

        public void OnPropertyExampleButtonClick()
        {
            //
            // property example
            //
            // List the properties of an OpenCV matrix.
            //

            // 64F, channels=1, 3x4
            Mat mat1 = new Mat(3, 4, CvType.CV_64FC1);

            // number of rows
            Debug.Log("rows:" + mat1.rows());
            // number of columns
            Debug.Log("cols:" + mat1.cols());
            // number of dimensions
            Debug.Log("dims:" + mat1.dims());
            // size
            Debug.Log("size[]:" + mat1.size().width + ", " + mat1.size().height);
            // bit depth ID
            Debug.Log("depth (ID):" + mat1.depth() + "(=" + CvType.CV_64F + ")");
            // number of channels
            Debug.Log("channels:" + mat1.channels());
            // size of one element
            Debug.Log("elemSize:" + mat1.elemSize() + "[byte]");
            // size for one channel in one element
            Debug.Log("elemSize1 (elemSize/channels):" + mat1.elemSize1() + "[byte]");
            // total number of elements
            Debug.Log("total:" + mat1.total());
            // size of step
            Debug.Log("step (step1*elemSize1):" + mat1.step1() * mat1.elemSize1() + "[byte]");
            // total number of channels within one step
            Debug.Log("step1 (step/elemSize1):" + mat1.step1());
            // is the data continuous?
            Debug.Log("isContinuous:" + mat1.isContinuous());
            // is it a submatrix?
            Debug.Log("isSubmatrix:" + mat1.isSubmatrix());
            // is the data empty?
            Debug.Log("empty:" + mat1.empty());

            Debug.Log("==============================");


            // 32FC, channels=5, 4x5, 3x4 Submatrix
            Mat mat2 = new Mat(4, 5, CvType.CV_32FC(5));
            OpenCVForUnity.CoreModule.Rect roi_rect = new OpenCVForUnity.CoreModule.Rect(0, 0, 3, 4);
            Mat r1 = new Mat(mat2, roi_rect);

            // number of rows
            Debug.Log("rows:" + r1.rows());
            // number of columns
            Debug.Log("cols:" + r1.cols());
            // number of dimensions
            Debug.Log("dims:" + r1.dims());
            // size
            Debug.Log("size[]:" + r1.size().width + ", " + r1.size().height);
            // bit depth ID
            Debug.Log("depth (ID):" + r1.depth() + "(=" + CvType.CV_32F + ")");
            // number of channels
            Debug.Log("channels:" + r1.channels());
            // size of one element
            Debug.Log("elemSize:" + r1.elemSize() + "[byte]");
            // size for one channel in one element
            Debug.Log("elemSize1 (elemSize/channels):" + r1.elemSize1() + "[byte]");
            // total number of elements
            Debug.Log("total:" + r1.total());
            // size of step
            Debug.Log("step (step1*elemSize1):" + r1.step1() * r1.elemSize1() + "[byte]");
            // total number of channels within one step
            Debug.Log("step1 (step/elemSize1):" + r1.step1());
            // is the data continuous?
            Debug.Log("isContinuous:" + r1.isContinuous());
            // is it a submatrix?
            Debug.Log("isSubmatrix:" + r1.isSubmatrix());
            // is the data empty?
            Debug.Log("empty:" + r1.empty());

            Debug.Log("==============================");


            // 32S, channles=2, 2x3x3x4x6 (5 dimensional array)
            int[] sizes = new int[] { 2, 3, 3, 4, 6 };
            Mat mat3 = new Mat(sizes, CvType.CV_32SC2);

            // number of rows
            Debug.Log("rows:" + mat3.rows());
            // number of columns
            Debug.Log("cols:" + mat3.cols());
            // number of dimensions
            Debug.Log("dims:" + mat3.dims());
            // size
            string size = "";
            for (int i = 0; i < mat3.dims(); ++i)
            {
                size += mat3.size(i) + ", ";
            }
            Debug.Log("size[]:" + size);
            // bit depth ID
            Debug.Log("depth (ID):" + mat3.depth() + "(=" + CvType.CV_32S + ")");
            // number of channels
            Debug.Log("channels:" + mat3.channels());
            // size of one element
            Debug.Log("elemSize:" + mat3.elemSize() + "[byte]");
            // size for one channel in one element
            Debug.Log("elemSize1 (elemSize/channels):" + mat3.elemSize1() + "[byte]");
            // total number of elements
            Debug.Log("total:" + mat3.total());
            // size of step
            string step = "";
            for (int i = 0; i < mat3.dims(); ++i)
            {
                step += mat3.step1(i) * mat3.elemSize1() + ", ";
            }
            Debug.Log("step (step1*elemSize1):" + step + "[byte]");
            // total number of channels within one step
            Debug.Log("step1 (step/elemSize1):" + mat3.step1());
            // is the data continuous?
            Debug.Log("isContinuous:" + mat3.isContinuous());
            // is it a submatrix?
            Debug.Log("isSubmatrix:" + mat3.isSubmatrix());
            // is the data empty?
            Debug.Log("empty:" + mat3.empty());


            ExampleCodeText.text = @"
            //
            // property example
            //
            // List the properties of an OpenCV matrix.
            //

            // 64F, channels=1, 3x4
            Mat mat1 = new Mat (3, 4, CvType.CV_64FC1);

            // number of rows
            Debug.Log (""rows:"" + mat1.rows ());
            // number of columns
            Debug.Log (""cols:"" + mat1.cols ());
            // number of dimensions
            Debug.Log (""dims:"" + mat1.dims ());
            // size
            Debug.Log (""size[]:"" + mat1.size ().width + "", "" + mat1.size ().height);
            // bit depth ID
            Debug.Log (""depth (ID):"" + mat1.depth () + ""(="" + CvType.CV_64F + "")"");
            // number of channels
            Debug.Log (""channels:"" + mat1.channels ());
            // size of one element
            Debug.Log (""elemSize:"" + mat1.elemSize () + ""[byte]"");
            // size for one channel in one element
            Debug.Log (""elemSize1 (elemSize/channels):"" + mat1.elemSize1 () + ""[byte]"");
            // total number of elements
            Debug.Log (""total:"" + mat1.total ());
            // size of step
            Debug.Log (""step (step1*elemSize1):"" + mat1.step1 () * mat1.elemSize1 () + ""[byte]"");
            // total number of channels within one step
            Debug.Log (""step1 (step/elemSize1):"" + mat1.step1 ());
            // is the data continuous?
            Debug.Log (""isContinuous:"" + mat1.isContinuous ());
            // is it a submatrix?
            Debug.Log (""isSubmatrix:"" + mat1.isSubmatrix ());
            // is the data empty?
            Debug.Log (""empty:"" + mat1.empty ());

            Debug.Log (""=============================="");


            // 32FC, channels=5, 4x5, 3x4 Submatrix
            Mat mat2 = new Mat (4, 5, CvType.CV_32FC (5));
            OpenCVForUnity.CoreModule.Rect roi_rect = new OpenCVForUnity.CoreModule.Rect (0, 0, 3, 4);
            Mat r1 = new Mat (mat2, roi_rect);

            // number of rows
            Debug.Log (""rows:"" + r1.rows ());
            // number of columns
            Debug.Log (""cols:"" + r1.cols ());
            // number of dimensions
            Debug.Log (""dims:"" + r1.dims ());
            // size
            Debug.Log (""size[]:"" + r1.size ().width + "", "" + r1.size ().height);
            // bit depth ID
            Debug.Log (""depth (ID):"" + r1.depth () + ""(="" + CvType.CV_32F + "")"");
            // number of channels
            Debug.Log (""channels:"" + r1.channels ());
            // size of one element
            Debug.Log (""elemSize:"" + r1.elemSize () + ""[byte]"");
            // size for one channel in one element
            Debug.Log (""elemSize1 (elemSize/channels):"" + r1.elemSize1 () + ""[byte]"");
            // total number of elements
            Debug.Log (""total:"" + r1.total ());
            // size of step
            Debug.Log (""step (step1*elemSize1):"" + r1.step1 () * r1.elemSize1 () + ""[byte]"");
            // total number of channels within one step
            Debug.Log (""step1 (step/elemSize1):"" + r1.step1 ());
            // is the data continuous?
            Debug.Log (""isContinuous:"" + r1.isContinuous ());
            // is it a submatrix?
            Debug.Log (""isSubmatrix:"" + r1.isSubmatrix ());
            // is the data empty?
            Debug.Log (""empty:"" + r1.empty ());

            Debug.Log (""=============================="");


            // 32S, channles=2, 2x3x3x4x6 (5 dimensional array)
            int[] sizes = new int[]{ 2, 3, 3, 4, 6 };
            Mat mat3 = new Mat (sizes, CvType.CV_32SC2);

            // number of rows
            Debug.Log (""rows:"" + mat3.rows ());
            // number of columns
            Debug.Log (""cols:"" + mat3.cols ());
            // number of dimensions
            Debug.Log (""dims:"" + mat3.dims ());
            // size
            string size = """";
            for (int i = 0; i < mat3.dims (); ++i) {
                size += mat3.size (i) + "", "";
            }
            Debug.Log (""size[]:"" + size);
            // bit depth ID
            Debug.Log (""depth (ID):"" + mat3.depth () + ""(="" + CvType.CV_32S + "")"");
            // number of channels
            Debug.Log (""channels:"" + mat3.channels ());
            // size of one element
            Debug.Log (""elemSize:"" + mat3.elemSize () + ""[byte]"");
            // size for one channel in one element
            Debug.Log (""elemSize1 (elemSize/channels):"" + mat3.elemSize1 () + ""[byte]"");
            // total number of elements
            Debug.Log (""total:"" + mat3.total ());
            // size of step
            string step = """";
            for (int i = 0; i < mat3.dims (); ++i) {
                step += mat3.step1 (i) * mat3.elemSize1 () + "", "";
            }
            Debug.Log (""step (step1*elemSize1):"" + step + ""[byte]"");
            // total number of channels within one step
            Debug.Log (""step1 (step/elemSize1):"" + mat3.step1 ());
            // is the data continuous?
            Debug.Log (""isContinuous:"" + mat3.isContinuous ());
            // is it a submatrix?
            Debug.Log (""isSubmatrix:"" + mat3.isSubmatrix ());
            // is the data empty?
            Debug.Log (""empty:"" + mat3.empty ());
            ";

            ExecutionResultText.text = "rows:" + mat1.rows() + "\n";
            ExecutionResultText.text += "cols:" + mat1.cols() + "\n";
            ExecutionResultText.text += "dims:" + mat1.dims() + "\n";
            ExecutionResultText.text += "size[]:" + mat1.size().width + ", " + mat1.size().height + "\n";
            ExecutionResultText.text += "depth (ID):" + mat1.depth() + "(=" + CvType.CV_64F + ")" + "\n";
            ExecutionResultText.text += "channels:" + mat1.channels() + "\n";
            ExecutionResultText.text += "elemSize:" + mat1.elemSize() + "[byte]" + "\n";
            ExecutionResultText.text += "elemSize1 (elemSize/channels):" + mat1.elemSize1() + "[byte]" + "\n";
            ExecutionResultText.text += "total:" + mat1.total() + "\n";
            ExecutionResultText.text += "step (step1*elemSize1):" + mat1.step1() * mat1.elemSize1() + "[byte]" + "\n";
            ExecutionResultText.text += "step1 (step/elemSize1):" + mat1.step1() + "\n";
            ExecutionResultText.text += "isContinuous:" + mat1.isContinuous() + "\n";
            ExecutionResultText.text += "isSubmatrix:" + mat1.isSubmatrix() + "\n";
            ExecutionResultText.text += "empty:" + mat1.empty() + "\n";

            ExecutionResultText.text += "==============================" + "\n";

            ExecutionResultText.text += "rows:" + r1.rows() + "\n";
            ExecutionResultText.text += "cols:" + r1.cols() + "\n";
            ExecutionResultText.text += "dims:" + r1.dims() + "\n";
            ExecutionResultText.text += "size[]:" + r1.size().width + ", " + r1.size().height + "\n";
            ExecutionResultText.text += "depth (ID):" + r1.depth() + "(=" + CvType.CV_32F + ")" + "\n";
            ExecutionResultText.text += "channels:" + r1.channels() + "\n";
            ExecutionResultText.text += "elemSize:" + r1.elemSize() + "[byte]" + "\n";
            ExecutionResultText.text += "elemSize1 (elemSize/channels):" + r1.elemSize1() + "[byte]" + "\n";
            ExecutionResultText.text += "total:" + r1.total() + "\n";
            ExecutionResultText.text += "step (step1*elemSize1):" + r1.step1() * r1.elemSize1() + "[byte]" + "\n";
            ExecutionResultText.text += "step1 (step/elemSize1):" + r1.step1() + "\n";
            ExecutionResultText.text += "isContinuous:" + r1.isContinuous() + "\n";
            ExecutionResultText.text += "isSubmatrix:" + r1.isSubmatrix() + "\n";
            ExecutionResultText.text += "empty:" + r1.empty() + "\n";

            ExecutionResultText.text += "==============================" + "\n";

            ExecutionResultText.text += "rows:" + mat3.rows() + "\n";
            ExecutionResultText.text += "cols:" + mat3.cols() + "\n";
            ExecutionResultText.text += "dims:" + mat3.dims() + "\n";
            ExecutionResultText.text += "size[]:" + size + "\n";
            ExecutionResultText.text += "depth (ID):" + mat3.depth() + "(=" + CvType.CV_32S + ")" + "\n";
            ExecutionResultText.text += "channels:" + mat3.channels() + "\n";
            ExecutionResultText.text += "elemSize:" + mat3.elemSize() + "[byte]" + "\n";
            ExecutionResultText.text += "elemSize1 (elemSize/channels):" + mat3.elemSize1() + "[byte]" + "\n";
            ExecutionResultText.text += "total:" + mat3.total() + "\n";
            ExecutionResultText.text += "step (step1*elemSize1):" + step + "[byte]" + "\n";
            ExecutionResultText.text += "step1 (step/elemSize1):" + mat3.step1() + "\n";
            ExecutionResultText.text += "isContinuous:" + mat3.isContinuous() + "\n";
            ExecutionResultText.text += "isSubmatrix:" + mat3.isSubmatrix() + "\n";
            ExecutionResultText.text += "empty:" + mat3.empty() + "\n";

            UpdateScrollRect();
        }

        public void OnFourArithmeticOperationExampleButtonClick()
        {
            //
            // four arithmetic operation example
            //
            // Performs four arithmetic methods on matrices.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            // Scalar
            Scalar s = new Scalar(5);
            // alpha
            double alpha = 3;

            Debug.Log("m1=" + m1.dump());
            Debug.Log("m2=" + m2.dump());
            Debug.Log("s=" + s);
            Debug.Log("alpha=" + alpha);

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";
            ExecutionResultText.text += "s=" + s + "\n";
            ExecutionResultText.text += "alpha=" + alpha + "\n";

            Mat m_dst = new Mat();

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            Core.add(m1, m2, m_dst);
            Debug.Log("m1+m2=" + m_dst.dump());
            ExecutionResultText.text += "m1+m2=" + m_dst.dump() + "\n";
            Core.add(m1, s, m_dst);
            Debug.Log("m1+s=" + m_dst.dump());
            ExecutionResultText.text += "m1+s=" + m_dst.dump() + "\n";

            Core.subtract(m1, m2, m_dst);
            Debug.Log("m1-m2=" + m_dst.dump());
            ExecutionResultText.text += "m1-m2=" + m_dst.dump() + "\n";
            Core.subtract(m1, s, m_dst);
            Debug.Log("m1-s=" + m_dst.dump());
            ExecutionResultText.text += "m1-s=" + m_dst.dump() + "\n";

            Core.multiply(m1, Scalar.all(-1), m_dst);
            Debug.Log("-m1=" + m_dst.dump());
            ExecutionResultText.text += "-m1=" + m_dst.dump() + "\n";


            // Scaling: A*alpha A/alpha
            Core.multiply(m1, Scalar.all(3), m_dst);
            Debug.Log("m1*alpha=" + m_dst.dump());
            ExecutionResultText.text += "m1*alpha=" + m_dst.dump() + "\n";
            Core.divide(m1, Scalar.all(3), m_dst);
            Debug.Log("m1/alpha=" + m_dst.dump());
            ExecutionResultText.text += "m1/alpha=" + m_dst.dump() + "\n";


            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            Debug.Log("m1.mul(m2)=" + (m1.mul(m2)).dump());
            ExecutionResultText.text += "m1.mul(m2)=" + (m1.mul(m2)).dump() + "\n";

            Core.divide(m1, m2, m_dst);
            Debug.Log("m1/m2=" + m_dst.dump());
            ExecutionResultText.text += "m1/m2=" + m_dst.dump() + "\n";

            Core.divide(new Mat(m1.size(), m1.type(), Scalar.all(3)), m1, m_dst);
            Debug.Log("alpha/m2=" + m_dst.dump());
            ExecutionResultText.text += "alpha/m2=" + m_dst.dump() + "\n";


            // Matrix multiplication: A*B
            Core.gemm(m1, m2, 1, new Mat(), 0, m_dst);
            Debug.Log("m1*m2=" + m_dst.dump());
            ExecutionResultText.text += "m1*m2=" + m_dst.dump() + "\n";


            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            Core.bitwise_and(m1, m2, m_dst);
            Debug.Log("m1&m2=" + m_dst.dump());
            ExecutionResultText.text += "m1&m2=" + m_dst.dump() + "\n";

            Core.bitwise_or(m1, m2, m_dst);
            Debug.Log("m1|m2=" + m_dst.dump());
            ExecutionResultText.text += "m1|m2=" + m_dst.dump() + "\n";

            Core.bitwise_xor(m1, m2, m_dst);
            Debug.Log("m1^m2=" + m_dst.dump());
            ExecutionResultText.text += "m1^m2=" + m_dst.dump() + "\n";

            Core.bitwise_not(m1, m_dst);
            Debug.Log("~m1=" + m_dst.dump());
            ExecutionResultText.text += "~m1=" + m_dst.dump() + "\n";


            ExampleCodeText.text = @"
            //
            // four arithmetic operation example
            //
            // Performs four arithmetic methods on matrices.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            // Scalar
            Scalar s = new Scalar(5);
            // alpha
            double alpha = 3;

            Debug.Log(""m1 = "" + m1.dump());
            Debug.Log(""m2="" + m2.dump());
            Debug.Log(""s="" + s);
            Debug.Log(""alpha="" + alpha);

            Mat m_dst = new Mat();

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            Core.add(m1, m2, m_dst);
            Debug.Log(""m1+m2="" + m_dst.dump());
            Core.add(m1, s, m_dst);
            Debug.Log(""m1+s="" + m_dst.dump());

            Core.subtract(m1, m2, m_dst);
            Debug.Log(""m1-m2="" + m_dst.dump());
            Core.subtract(m1, s, m_dst);
            Debug.Log(""m1-s="" + m_dst.dump());

            Core.multiply(m1, Scalar.all(-1), m_dst);
            Debug.Log(""-m1="" + m_dst.dump());


            // Scaling: A*alpha A/alpha
            Core.multiply(m1, Scalar.all(3), m_dst);
            Debug.Log(""m1*alpha="" + m_dst.dump());
            Core.divide(m1, Scalar.all(3), m_dst);
            Debug.Log(""m1/alpha="" + m_dst.dump());


            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            Debug.Log(""m1.mul(m2)="" + (m1.mul(m2)).dump());

            Core.divide(m1, m2, m_dst);
            Debug.Log(""m1/m2="" + m_dst.dump());

            Core.divide(new Mat(m1.size(), m1.type(), Scalar.all(3)), m1, m_dst);
            Debug.Log(""alpha/m2="" + m_dst.dump());


            // Matrix multiplication: A*B
            Core.gemm(m1, m2, 1, new Mat(), 0, m_dst);
            Debug.Log(""m1*m2="" + m_dst.dump());


            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            Core.bitwise_and(m1, m2, m_dst);
            Debug.Log(""m1&m2="" + m_dst.dump());

            Core.bitwise_or(m1, m2, m_dst);
            Debug.Log(""m1|m2="" + m_dst.dump());

            Core.bitwise_xor(m1, m2, m_dst);
            Debug.Log(""m1^m2="" + m_dst.dump());

            Core.bitwise_not(m1, m_dst);
            Debug.Log(""~m1="" + m_dst.dump());
            ";

            UpdateScrollRect();
        }

        public void OnConvertToExampleButtonClick()
        {
            //
            // convertTo example
            //
            // The Core.convertTo function changes the data type or scale of a Mat object.
            // It is used in various situations in image processing, such as converting between different data types or adjusting the brightness of an image.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Debug.Log("m1=" + m1.dump());

            // 64F -> 8U (dst mat, type)
            Mat m2 = new Mat();
            m1.convertTo(m2, CvType.CV_8U);
            Debug.Log("m2=" + m2.dump());

            // 64F -> 8U (dst mat, type, scale factor, added to the scaled value)
            Mat m3 = new Mat();
            m1.convertTo(m3, CvType.CV_8U, 2, 10);
            Debug.Log("m3=" + m3.dump());

            ExampleCodeText.text = @"
            //
            // convertTo example
            //
            // The Core.convertTo function changes the data type or scale of a Mat object.
            // It is used in various situations in image processing, such as converting between different data types or adjusting the brightness of an image.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Debug.Log (""m1="" + m1.dump());

            // 64F -> 8U (dst mat, type)
            Mat m2 = new Mat ();
            m1.convertTo (m2, CvType.CV_8U);
            Debug.Log (""m2="" + m2.dump());

            // 64F -> 8U (dst mat, type, scale factor, added to the scaled value)
            Mat m3 = new Mat ();
            m1.convertTo (m3, CvType.CV_8U, 2, 10);
            Debug.Log (""m3="" + m3.dump());
            ";

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";
            ExecutionResultText.text += "m3=" + m3.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnReshapeExampleButtonClick()
        {
            //
            // reshape example
            //
            // Changes the shape and/or the number of channels of a  matrix without copying the data.
            // The method makes a new matrix header for this elements.The new matrix may have a different size and / or different number of channels.Any combination is possible if:
            // - No extra elements are included into the new matrix and no elements are excluded.Consequently, the product rows* cols*channels() must stay the same after the transformation.
            // - No data is copied.That is, this is an O(1) operation.Consequently, if you change the number of rows, or the operation changes the indices of elements row in some other way, the matrix must be continuous.See "Mat.isContinuous".
            //

            // 64F, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Debug.Log("m1=" + m1.dump());
            Debug.Log("ch=" + m1.channels());

            // channels=1, 3x4 -> channels=2, 3x2
            Mat m2 = m1.reshape(2);
            Debug.Log("m2=" + m2.dump());
            Debug.Log("ch=" + m2.channels());

            // channels=1, 3x4 -> channels=1, 2x6
            Mat m3 = m1.reshape(1, 2);
            Debug.Log("m3=" + m3.dump());
            Debug.Log("ch=" + m3.channels());

            // 2D -> 4D
            Mat src = new Mat(6, 5, CvType.CV_8UC3, new Scalar(0));
            Mat m4 = src.reshape(1, new int[] { 1, src.channels() * src.cols(), 1, src.rows() });
            Debug.Log("m4.dims=" + m4.dims());
            string size = "";
            for (int i = 0; i < m4.dims(); ++i)
            {
                size += m4.size(i) + ", ";
            }
            Debug.Log("size[]=" + size);
            Debug.Log("ch=" + m4.channels());

            // 3D -> 2D
            src = new Mat(new int[] { 4, 6, 7 }, CvType.CV_8UC3, new Scalar(0));
            Mat m5 = src.reshape(1, new int[] { src.channels() * src.size(2), src.size(0) * src.size(1) });
            Debug.Log("m5=" + m5);
            Debug.Log("ch=" + m5.channels());

            ExampleCodeText.text = @"
            //
            // reshape example
            //
            // Changes the shape and/or the number of channels of a  matrix without copying the data.
            // The method makes a new matrix header for this elements.The new matrix may have a different size and / or different number of channels.Any combination is possible if:
            // - No extra elements are included into the new matrix and no elements are excluded.Consequently, the product rows* cols*channels() must stay the same after the transformation.
            // - No data is copied.That is, this is an O(1) operation.Consequently, if you change the number of rows, or the operation changes the indices of elements row in some other way, the matrix must be continuous.See ""Mat.isContinuous"".
            //

            // 64F, channels=1, 3x4
            Mat m1 = new Mat (3, 4, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12);
            Debug.Log (""m1="" + m1.dump());
            Debug.Log (""ch="" + m1.channels());

            // channels=1, 3x4 -> channels=2, 3x2
            Mat m2 = m1.reshape (2);
            Debug.Log (""m2="" + m2.dump ());
            Debug.Log (""ch="" + m2.channels ());

            // channels=1, 3x4 -> channels=1, 2x6
            Mat m3 = m1.reshape (1, 2);
            Debug.Log (""m3="" + m3.dump ());
            Debug.Log (""ch="" + m3.channels ());

            // 2D -> 4D
            Mat src = new Mat (6, 5, CvType.CV_8UC3, new Scalar (0));
            Mat m4 = src.reshape (1, new int[]{ 1, src.channels () * src.cols (), 1, src.rows () });
            Debug.Log (""m4.dims="" + m4.dims ());
            string size = """";
            for (int i = 0; i < m4.dims (); ++i) {
                size += m4.size (i) + "", "";
            }
            Debug.Log (""size[]="" + size);
            Debug.Log (""ch="" + m4.channels ());

            // 3D -> 2D
            src = new Mat (new int[]{ 4, 6, 7 }, CvType.CV_8UC3, new Scalar (0));
            Mat m5 = src.reshape (1, new int[]{ src.channels () * src.size (2), src.size (0) * src.size (1) });
            Debug.Log (""m5="" + m5);
            Debug.Log (""ch="" + m5.channels ());
            ";

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "ch=" + m1.channels() + "\n";
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";
            ExecutionResultText.text += "ch=" + m2.channels() + "\n";
            ExecutionResultText.text += "m3=" + m3.dump() + "\n";
            ExecutionResultText.text += "ch=" + m3.channels() + "\n";
            ExecutionResultText.text += "m4.dims=" + m4.dims() + "\n";
            ExecutionResultText.text += "m4.size[]=" + size + "\n";
            ExecutionResultText.text += "ch=" + m4.channels() + "\n";
            ExecutionResultText.text += "m5=" + m5 + "\n";
            ExecutionResultText.text += "ch=" + m5.channels() + "\n";

            UpdateScrollRect();
        }

        public void OnTransposeExampleButtonClick()
        {
            //
            // transpose example
            //
            // The Core.transpose function can be used for various image processing tasks such as rotating images by 90 degrees and changing the shape of matrices by swapping rows and columns of Mat.
            // - The Core.transpose function is a function that performs a transposition operation on a two-dimensional matrix.
            // - The Core.transposeND function is a function that performs a transposition operation on a tensor of arbitrary dimensions.For example, it can be used to swap specific dimensions of a 3D tensor(such as video data).
            //

            // Transposes a matrix.
            // 8U, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_8UC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Debug.Log("m1=" + m1.dump());

            // [3x4] -> [4x3]
            Mat m1_t = new Mat();
            Core.transpose(m1, m1_t);
            Debug.Log("Core.transpose(m1, m1_t)=" + m1_t.dump());

            // Transpose for n-dimensional matrices.
            // 32F, channels=1, 1x3x4x3
            Mat m2 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            m2.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            string m2_size = "";
            for (int i = 0; i < m2.dims(); ++i)
            {
                m2_size += m2.size(i) + ", ";
            }
            Debug.Log("m2=" + m2.reshape(3, new int[] { 3, 4 }).dump());
            Debug.Log("m2 size[]=" + m2_size);

            // [1x3x4x3] -> [1x4x3x3]
            Mat m2_t = new Mat();
            MatOfInt order = new MatOfInt(0, 2, 1, 3); // Transpose order
            Core.transposeND(m2, order, m2_t);
            string m2_t_size = "";
            for (int i = 0; i < m2_t.dims(); ++i)
            {
                m2_t_size += m2_t.size(i) + ", ";
            }
            Debug.Log("Core.transposeND(m2, m2_t)=" + m2_t.reshape(3, new int[] { 4, 3 }).dump());
            Debug.Log("m2_t size[]=" + m2_t_size);

            ExampleCodeText.text = @"
            //
            // transpose example
            //
            // The Core.transpose function can be used for various image processing tasks such as rotating images by 90 degrees and changing the shape of matrices by swapping rows and columns of Mat.
            // - The Core.transpose function is a function that performs a transposition operation on a two-dimensional matrix.
            // - The Core.transposeND function is a function that performs a transposition operation on a tensor of arbitrary dimensions.For example, it can be used to swap specific dimensions of a 3D tensor(such as video data).
            //

            // Transposes a matrix.
            // 8U, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_8UC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Debug.Log(""m1 = "" + m1.dump());

            // [3x4] -> [4x3]
            Mat m1_t = new Mat();
            Core.transpose(m1, m1_t);
            Debug.Log(""Core.transpose(m1, m1_t)="" + m1_t.dump());

            // Transpose for n-dimensional matrices.
            // 32F, channels=1, 1x3x4x3
            Mat m2 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            m2.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            string m2_size = "";
            for (int i = 0; i < m2.dims(); ++i)
            {
                m2_size += m2.size(i) + "", "";
            }
            Debug.Log(""m2="" + m2.reshape(3, new int[] { 3, 4 }).dump());
            Debug.Log(""m2 size[]="" + m2_size);

            // [1x3x4x3] -> [1x4x3x3]
            Mat m2_t = new Mat();
            MatOfInt order = new MatOfInt(0, 2, 1, 3); // Transpose order
            Core.transposeND(m2, order, m2_t);
            string m2_t_size = "";
            for (int i = 0; i < m2_t.dims(); ++i)
            {
                m2_t_size += m2_t.size(i) + "", "";
            }
            Debug.Log(""Core.transposeND(m2, m2_t)="" + m2_t.reshape(3, new int[] { 4, 3 }).dump());
            Debug.Log(""m2_t size[]="" + m2_t_size);
            ";

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "Core.transpose(m1, m1_t)=" + m1_t.dump() + "\n";
            ExecutionResultText.text += "m2=" + m2.reshape(3, new int[] { 3, 4 }).dump() + "\n";
            ExecutionResultText.text += "m2 size[]=" + m2_size + "\n";
            ExecutionResultText.text += "Core.transposeND(m2, m2_t)=" + m2_t.reshape(3, new int[] { 4, 3 }).dump() + "\n";
            ExecutionResultText.text += "m2_t size[]=" + m2_t_size + "\n";

            UpdateScrollRect();
        }

        public void OnRangeExampleButtonClick()
        {
            //
            // range example
            //
            // Mat.rowRange and Mat.colRange efficiently extract submatrices from a Mat by creating new Mat headers that point to specified row or column ranges of the original data, without copying the underlying data.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log("m1=" + m1.dump());

            // all rows
            Debug.Log("m1.rowRange(Range.all())=" + m1.rowRange(Range.all()).dump());

            // rowRange(0,2)
            Debug.Log("m1.rowRange(new Range(0,2))=" + m1.rowRange(new Range(0, 2)).dump());

            // row(0)
            Debug.Log("m1.row(0)=" + m1.row(0).dump());

            // all cols
            Debug.Log("m1.colRange(Range.all())=" + m1.colRange(Range.all()).dump());

            // colRange(0,2)
            Debug.Log("m1.colRange(new Range(0,2))=" + m1.colRange(new Range(0, 2)).dump());

            // col(0)
            Debug.Log("m1.col(0)=" + m1.col(0).dump());

            ExampleCodeText.text = @"
            //
            // range example
            //
            // Mat.rowRange and Mat.colRange efficiently extract submatrices from a Mat by creating new Mat headers that point to specified row or column ranges of the original data, without copying the underlying data.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log (""m1="" + m1.dump());

            // all rows
            Debug.Log (""m1.rowRange(Range.all())="" + m1.rowRange(Range.all()).dump());

            // rowRange(0,2)
            Debug.Log (""m1.rowRange(new Range(0,2))="" + m1.rowRange(new Range(0,2)).dump());

            // row(0)
            Debug.Log (""m1.row(0)="" + m1.row(0).dump());

            // all cols
            Debug.Log (""m1.colRange(Range.all())="" + m1.colRange(Range.all()).dump());

            // colRange(0,2)
            Debug.Log (""m1.colRange(new Range(0,2))="" + m1.colRange(new Range(0,2)).dump());

            // col(0)
            Debug.Log (""m1.col(0)="" + m1.col(0).dump());
            ";

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "m1.rowRange(Range.all())=" + m1.rowRange(Range.all()).dump() + "\n";
            ExecutionResultText.text += "m1.rowRange(new Range(0,2))=" + m1.rowRange(new Range(0, 2)).dump() + "\n";
            ExecutionResultText.text += "m1.row(0)=" + m1.row(0).dump() + "\n";
            ExecutionResultText.text += "m1.colRange(Range.all())=" + m1.colRange(Range.all()).dump() + "\n";
            ExecutionResultText.text += "m1.colRange(new Range(0,2))=" + m1.colRange(new Range(0, 2)).dump() + "\n";
            ExecutionResultText.text += "m1.col(0)=" + m1.col(0).dump() + "\n";

            UpdateScrollRect();
        }

        public void OnSubmatrixExampleButtonClick()
        {
            //
            // submatrix (ROI) example
            //
            // A submatrix (Region of Interest, ROI) is a region cut out of an image or matrix. OpenCV allows you to create a submatrix that manipulates only that region without copying the original data.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log("m1=" + m1.dump());

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";

            // get submatrix (ROI) of range (row[0_2] col[0_2])
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(0, 0, 2, 2));
            Debug.Log("m2=" + m2.dump());
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";
            Debug.Log("m2.submat()=" + m2.submat(0, 2, 0, 2).dump());
            ExecutionResultText.text += "m2.submat()=" + m2.submat(0, 2, 0, 2).dump() + "\n";

            // find the parent matrix size of the submatrix (ROI) m2 and its position in it
            Size wholeSize = new Size();
            Point ofs = new Point();
            m2.locateROI(wholeSize, ofs);
            Debug.Log("wholeSize:" + wholeSize.width + "x" + wholeSize.height);
            Debug.Log("offset:" + ofs.x + ", " + ofs.y);

            ExecutionResultText.text += "wholeSize:" + wholeSize.width + "x" + wholeSize.height + "\n";
            ExecutionResultText.text += "offset:" + ofs.x + ", " + ofs.y + "\n";

            // expand the range of submatrix (ROI)
            m2.adjustROI(0, 1, 0, 1);
            Debug.Log("rows=" + m2.rows() + ", " + "cols=" + m2.cols());
            Debug.Log("m2=" + m2.dump());

            ExecutionResultText.text += "rows=" + m2.rows() + ", " + "cols=" + m2.cols() + "\n";
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // submatrix (ROI) example
            //
            // A submatrix (Region of Interest, ROI) is a region cut out of an image or matrix. OpenCV allows you to create a submatrix that manipulates only that region without copying the original data.
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log (""m1="" + m1.dump ());

            // get submatrix (ROI) of range (row[0_2] col[0_2])
            Mat m2 = new Mat (m1, new OpenCVForUnity.CoreModule.Rect(0,0,2,2));
            Debug.Log (""m2="" + m2.dump());
            Debug.Log (""m2.submat()="" + m2.submat(0,2,0,2).dump());

            // find the parent matrix size of the submatrix (ROI) m2 and its position in it
            Size wholeSize = new Size ();
            Point ofs = new Point ();
            m2.locateROI (wholeSize, ofs);
            Debug.Log (""wholeSize:"" + wholeSize.width + ""x"" + wholeSize.height);
            Debug.Log (""offset:"" + ofs.x + "", "" + ofs.y);

            // expand the range of submatrix (ROI)
            m2.adjustROI(0, 1, 0, 1);
            Debug.Log (""rows="" + m2.rows() + "", "" + ""cols="" + m2.cols());
            Debug.Log (""m2="" + m2.dump());
            ";

            UpdateScrollRect();
        }

        public void OnShallowCopyAndDeepCopyExampleButtonClick()
        {
            //
            // shallow copy and deep copy example
            //
            // When working with image and matrix data in OpenCVForUnity, the concepts of shallow copy and deep copy are important. These two methods differ in how they duplicate data, and can significantly affect the behavior of your program.
            // - Shallow copy: Creates a new Mat object that references the same memory region as the original data.
            // - Deep copy: Creates a new Mat object by copying the data into a new memory region, independent of the original data.
            //

            // 3x3 matrix
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            mat1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // shallow copy
            Mat m_shallow = mat1;

            // deep copy (clone, copyTo)
            Mat m_deep1 = mat1.clone();
            Mat m_deep2 = new Mat();
            mat1.copyTo(m_deep2);

            Debug.Log("mat1=" + mat1.dump());
            Debug.Log("m_shallow=" + m_shallow.dump());
            Debug.Log("m_deep1=" + m_deep1.dump());
            Debug.Log("m_deep2=" + m_deep2.dump());

            ExecutionResultText.text = "mat1=" + mat1.dump() + "\n";
            ExecutionResultText.text += "m_shallow=" + m_shallow.dump() + "\n";
            ExecutionResultText.text += "m_deep1=" + m_deep1.dump() + "\n";
            ExecutionResultText.text += "m_deep2=" + m_deep2.dump() + "\n";

            // rewrite (0, 0) element of matrix mat1
            mat1.put(0, 0, 100);

            Debug.Log("mat1=" + mat1.dump());
            Debug.Log("m_shallow=" + m_shallow.dump());
            Debug.Log("m_deep1=" + m_deep1.dump());
            Debug.Log("m_deep2=" + m_deep2.dump());

            ExecutionResultText.text += "mat1=" + mat1.dump() + "\n";
            ExecutionResultText.text += "m_shallow=" + m_shallow.dump() + "\n";
            ExecutionResultText.text += "m_deep1=" + m_deep1.dump() + "\n";
            ExecutionResultText.text += "m_deep2=" + m_deep2.dump() + "\n";

            Debug.Log("mat1.Equals(m_shallow)=" + mat1.Equals(m_shallow));
            Debug.Log("mat1.Equals(m_deep1)=" + mat1.Equals(m_deep1));
            Debug.Log("mat1.Equals(m_deep2)=" + mat1.Equals(m_deep2));

            ExecutionResultText.text += "mat1.Equals(m_shallow)=" + mat1.Equals(m_shallow) + "\n";
            ExecutionResultText.text += "mat1.Equals(m_deep1)=" + mat1.Equals(m_deep1) + "\n";
            ExecutionResultText.text += "mat1.Equals(m_deep2)=" + mat1.Equals(m_deep2) + "\n";

            ExampleCodeText.text = @"
            //
            // shallow copy and deep copy example
            //
            // When working with image and matrix data in OpenCVForUnity, the concepts of shallow copy and deep copy are important. These two methods differ in how they duplicate data, and can significantly affect the behavior of your program.
            // - Shallow copy: Creates a new Mat object that references the same memory region as the original data.
            // - Deep copy: Creates a new Mat object by copying the data into a new memory region, independent of the original data.
            //

            // 3x3 matrix
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            mat1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // shallow copy
            Mat m_shallow = mat1;

            // deep copy (clone, copyTo)
            Mat m_deep1 = mat1.clone();
            Mat m_deep2 = new Mat();
            mat1.copyTo (m_deep2);

            Debug.Log (""mat1="" + mat1.dump());
            Debug.Log (""m_shallow="" + m_shallow.dump());
            Debug.Log (""m_deep1="" + m_deep1.dump());
            Debug.Log (""m_deep2="" + m_deep2.dump());

            // rewrite (0, 0) element of matrix mat1
            mat1.put(0, 0, 100);

            Debug.Log (""mat1="" + mat1.dump());
            Debug.Log (""m_shallow="" + m_shallow.dump());
            Debug.Log (""m_deep1="" + m_deep1.dump());
            Debug.Log (""m_deep2="" + m_deep2.dump());

            Debug.Log (""mat1.Equals(m_shallow)="" + mat1.Equals(m_shallow));
            Debug.Log (""mat1.Equals(m_deep1)="" + mat1.Equals(m_deep1));
            Debug.Log (""mat1.Equals(m_deep2)="" + mat1.Equals(m_deep2));
            ";

            UpdateScrollRect();
        }

        public void OnMergeExampleButtonClick()
        {
            //
            // merge example
            //
            // The Core.merge function merges multiple Mat objects into a single Mat object.
            // - Number of channels: The number of Mat objects to merge is the number of channels in the output Mat object.
            // - Size: The size(number of rows and columns) of all Mat objects to be combined must match.
            // - Data Type: The data types of all Mat objects to be combined must match.
            //

            // 2x2 matrix
            Mat m1 = new Mat(2, 2, CvType.CV_64FC1);
            m1.put(0, 0, 1.0, 2.0, 3.0, 4.0);
            Mat m2 = new Mat(2, 2, CvType.CV_64FC1);
            m2.put(0, 0, 1.1, 2.1, 3.1, 4.1);
            Mat m3 = new Mat(2, 2, CvType.CV_64FC1);
            m3.put(0, 0, 1.2, 2.2, 3.2, 4.2);

            List<Mat> mv = new List<Mat>();
            mv.Add(m1);
            mv.Add(m2);
            mv.Add(m3);

            // merge
            Mat m_merged = new Mat();
            Core.merge(mv, m_merged);

            // dump
            Debug.Log("m_merged=" + m_merged.dump());

            ExampleCodeText.text = @"
            //
            // merge example
            //
            // The Core.merge function merges multiple Mat objects into a single Mat object.
            // - Number of channels: The number of Mat objects to merge is the number of channels in the output Mat object.
            // - Size: The size(number of rows and columns) of all Mat objects to be combined must match.
            // - Data Type: The data types of all Mat objects to be combined must match.
            //

            // 2x2 matrix
            Mat m1 = new Mat (2, 2, CvType.CV_64FC1);
            m1.put (0, 0, 1.0, 2.0, 3.0, 4.0);
            Mat m2 = new Mat (2, 2, CvType.CV_64FC1);
            m2.put (0, 0, 1.1, 2.1, 3.1, 4.1);
            Mat m3 = new Mat (2, 2, CvType.CV_64FC1);
            m3.put (0, 0, 1.2, 2.2, 3.2, 4.2);

            List<Mat> mv = new List<Mat>();
            mv.Add (m1);
            mv.Add (m2);
            mv.Add (m3);

            // merge
            Mat m_merged = new Mat();
            Core.merge (mv, m_merged);

            // dump
            Debug.Log (""m_merged="" + m_merged.dump());
            ";

            ExecutionResultText.text = "m_merged=" + m_merged.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnMixChannelsExampleButtonClick()
        {
            //
            // mixChannels example
            //
            // The Core.mixChannels function allows you to freely manipulate the channels of a Mat object.
            // It is used to reorder channels or to create a new Mat object from multiple Mat objects.
            //

            // 2x2 matrix
            Mat m1 = new Mat(2, 2, CvType.CV_64FC1);
            m1.put(0, 0, 1.0, 2.0, 3.0, 4.0);
            Mat m2 = new Mat(2, 2, CvType.CV_64FC1);
            m2.put(0, 0, 1.1, 2.1, 3.1, 4.1);
            Mat m3 = new Mat(2, 2, CvType.CV_64FC1);
            m3.put(0, 0, 1.2, 2.2, 3.2, 4.2);

            List<Mat> mv = new List<Mat>();
            mv.Add(m1);
            mv.Add(m2);
            mv.Add(m3);

            // mat for output must be allocated.
            Mat m_mixed1 = new Mat(2, 2, CvType.CV_64FC2);
            Mat m_mixed2 = new Mat(2, 2, CvType.CV_64FC2);
            MatOfInt fromTo = new MatOfInt(0, 0, 1, 1, 1, 3, 2, 2);

            List<Mat> mixv = new List<Mat>();
            mixv.Add(m_mixed1);
            mixv.Add(m_mixed2);

            // mix
            Core.mixChannels(mv, mixv, fromTo);

            // dump
            Debug.Log("m_mixed1=" + m_mixed1.dump());
            Debug.Log("m_mixed2=" + m_mixed2.dump());

            ExampleCodeText.text = @"
            //
            // mixChannels example
            //
            // The Core.mixChannels function allows you to freely manipulate the channels of a Mat object.
            // It is used to reorder channels or to create a new Mat object from multiple Mat objects.
            //

            // 2x2 matrix
            Mat m1 = new Mat (2, 2, CvType.CV_64FC1);
            m1.put (0, 0, 1.0, 2.0, 3.0, 4.0);
            Mat m2 = new Mat (2, 2, CvType.CV_64FC1);
            m2.put (0, 0, 1.1, 2.1, 3.1, 4.1);
            Mat m3 = new Mat (2, 2, CvType.CV_64FC1);
            m3.put (0, 0, 1.2, 2.2, 3.2, 4.2);

            List<Mat> mv = new List<Mat>();
            mv.Add (m1);
            mv.Add (m2);
            mv.Add (m3);

            // mat for output must be allocated.
            Mat m_mixed1 = new Mat(2, 2, CvType.CV_64FC2);
            Mat m_mixed2 = new Mat(2, 2, CvType.CV_64FC2);
            MatOfInt fromTo = new MatOfInt (0,0, 1,1, 1,3, 2,2);

            List<Mat> mixv = new List<Mat> ();
            mixv.Add (m_mixed1);
            mixv.Add (m_mixed2);

            // mix
            Core.mixChannels (mv, mixv, fromTo);

            // dump
            Debug.Log (""m_mixed1="" + m_mixed1.dump());
            Debug.Log (""m_mixed2="" + m_mixed2.dump());
            ";

            ExecutionResultText.text = "m_mixed1=" + m_mixed1.dump() + "\n";
            ExecutionResultText.text += "m_mixed2=" + m_mixed2.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnSplitExampleButtonClick()
        {
            //
            // split example
            //
            // The Core.split function separates a single multi-channel image (e.g., an RGB image) into its individual channels; it is the counterpart to the Core.merge function.
            //

            // channels=3, 2x3 matrix
            Mat m1 = new Mat(2, 3, CvType.CV_64FC3);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

            List<Mat> planes = new List<Mat>();

            // split
            Core.split(m1, planes);

            // dump
            foreach (Mat item in planes)
            {
                Debug.Log(item.dump());
            }

            ExampleCodeText.text = @"
            //
            // split example
            //
            // The Core.split function separates a single multi-channel image (e.g., an RGB image) into its individual channels; it is the counterpart to the Core.merge function.
            //

            // channels=3, 2x3 matrix
            Mat m1 = new Mat (2, 3, CvType.CV_64FC3);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18);

            List<Mat> planes = new List<Mat>();

            // split
            Core.split (m1, planes);

            // dump
            foreach (Mat item in planes) {
                Debug.Log (item.dump());
            }
            ";

            ExecutionResultText.text = "";
            foreach (Mat item in planes)
            {
                ExecutionResultText.text += item.dump() + "\n";
            }

            UpdateScrollRect();
        }

        public void OnReduceExampleButtonClick()
        {
            //
            // reduce example
            //
            // The Core.reduce function compresses (reduces) a multidimensional array (Mat object) along a specified axis. In other words,
            // it can compress multidimensional data into lower dimensional data.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 5, 3, 4, 2, 6, 7, 8, 9);

            Mat v1 = new Mat();
            Mat v2 = new Mat();
            Mat v3 = new Mat();
            Mat v4 = new Mat();

            // reduce 3 x 3 matrix to one row
            Core.reduce(m1, v1, 0, Core.REDUCE_SUM); // total value of each column
            Core.reduce(m1, v2, 0, Core.REDUCE_AVG); // total average value of each column
            Core.reduce(m1, v3, 0, Core.REDUCE_MIN); // minimum value of each column
            Core.reduce(m1, v4, 0, Core.REDUCE_MAX); // maximum value of each column

            // dump
            Debug.Log("m1=" + m1.dump());
            Debug.Log("v1(sum)=" + v1.dump());
            Debug.Log("v2(avg)=" + v2.dump());
            Debug.Log("v3(min)=" + v3.dump());
            Debug.Log("v4(max)=" + v4.dump());

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "v1(sum)=" + v1.dump() + "\n";
            ExecutionResultText.text += "v2(avg)=" + v2.dump() + "\n";
            ExecutionResultText.text += "v3(min)=" + v3.dump() + "\n";
            ExecutionResultText.text += "v4(max)=" + v4.dump() + "\n";

            // reduce 3 x 3 matrix to one col
            Core.reduce(m1, v1, 1, Core.REDUCE_SUM); // total value of each row
            Core.reduce(m1, v2, 1, Core.REDUCE_AVG); // total average value of row
            Core.reduce(m1, v3, 1, Core.REDUCE_MIN); // minimum value of each row
            Core.reduce(m1, v4, 1, Core.REDUCE_MAX); // maximum value of each row

            // dump
            Debug.Log("m1=" + m1.dump());
            Debug.Log("v1(sum)=" + v1.dump());
            Debug.Log("v2(avg)=" + v2.dump());
            Debug.Log("v3(min)=" + v3.dump());
            Debug.Log("v4(max)=" + v4.dump());

            ExecutionResultText.text += "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "v1(sum)=" + v1.dump() + "\n";
            ExecutionResultText.text += "v2(avg)=" + v2.dump() + "\n";
            ExecutionResultText.text += "v3(min)=" + v3.dump() + "\n";
            ExecutionResultText.text += "v4(max)=" + v4.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // reduce example
            //
            // The Core.reduce function compresses (reduces) a multidimensional array (Mat object) along a specified axis. In other words,
            // it can compress multidimensional data into lower dimensional data.
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1, 5, 3, 4, 2, 6, 7, 8, 9);

            Mat v1 = new Mat ();
            Mat v2 = new Mat ();
            Mat v3 = new Mat ();
            Mat v4 = new Mat ();

            // reduce 3 x 3 matrix to one row
            Core.reduce (m1, v1, 0, Core.REDUCE_SUM); // total value of each column
            Core.reduce (m1, v2, 0, Core.REDUCE_AVG); // total average value of each column
            Core.reduce (m1, v3, 0, Core.REDUCE_MIN); // minimum value of each column
            Core.reduce (m1, v4, 0, Core.REDUCE_MAX); // maximum value of each column

            // dump
            Debug.Log (""m1="" + m1.dump());
            Debug.Log (""v1(sum)="" + v1.dump());
            Debug.Log (""v2(avg)="" + v2.dump());
            Debug.Log (""v3(min)="" + v3.dump());
            Debug.Log (""v4(max)="" + v4.dump());

            // reduce 3 x 3 matrix to one col
            Core.reduce (m1, v1, 1, Core.REDUCE_SUM); // total value of each row
            Core.reduce (m1, v2, 1, Core.REDUCE_AVG); // total average value of row
            Core.reduce (m1, v3, 1, Core.REDUCE_MIN); // minimum value of each row
            Core.reduce (m1, v4, 1, Core.REDUCE_MAX); // maximum value of each row

            // dump
            Debug.Log (""m1="" + m1.dump());
            Debug.Log (""v1(sum)="" + v1.dump());
            Debug.Log (""v2(avg)="" + v2.dump());
            Debug.Log (""v3(min)="" + v3.dump());
            Debug.Log (""v4(max)="" + v4.dump());
            ";

            UpdateScrollRect();
        }

        public void OnRandShuffleExampleButtonClick()
        {
            //
            // randShuffle example
            //
            // The Core.randShuffle function randomly shuffles the elements in a Mat object. In other words, it can randomly reorder the order of elements in a Mat object.
            //

            // 4x5 matrix
            Mat m1 = new Mat(4, 5, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Debug.Log("m1(original)=" + m1.dump());

            ExecutionResultText.text = "m1(original)=" + m1.dump() + "\n";

            // shuffle
            Core.randShuffle(m1, UnityEngine.Random.value);
            Debug.Log("m1(shuffle)=" + m1.dump());

            ExecutionResultText.text += "m1(shuffle)=" + m1.dump() + "\n";

            // submatrix
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(1, 1, 3, 2));
            Debug.Log("m2(sub-matrix)=" + m2.dump());

            ExecutionResultText.text += "m2(sub-matrix)=" + m2.dump() + "\n";

            Core.randShuffle(m2, UnityEngine.Random.value);
            Debug.Log("m2(sub-matrix)=" + m2.dump());
            Debug.Log("m1=" + m1.dump());

            ExecutionResultText.text += "m2(shuffle sub-matrix)=" + m2.dump() + "\n";
            ExecutionResultText.text += "m1=" + m1.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // randShuffle example
            //
            // The Core.randShuffle function randomly shuffles the elements in a Mat object. In other words, it can randomly reorder the order of elements in a Mat object.
            //

            // 4x5 matrix
            Mat m1 = new Mat (4, 5, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20);
            Debug.Log (""m1(original)="" + m1.dump ());

            // shuffle
            Core.randShuffle (m1, UnityEngine.Random.value);
            Debug.Log (""m1(shuffle)="" + m1.dump ());

            // submatrix
            Mat m2 = new Mat (m1, new OpenCVForUnity.CoreModule.Rect(1,1,3,2));
            Debug.Log (""m2(sub-matrix)="" + m2.dump());

            Core.randShuffle (m2, UnityEngine.Random.value);
            Debug.Log (""m2(sub-matrix)="" + m2.dump());
            Debug.Log (""m1="" + m1.dump ());
            ";

            UpdateScrollRect();
        }

        public void OnSortExampleButtonClick()
        {
            //
            // sort example
            //
            // The Core.sort function sorts the elements in a Mat object in ascending or descending order.
            // In other words, it allows you to sort the elements in a Mat object in a specific order.
            //

            // 5x5 matrix
            Mat m1 = new Mat(5, 5, CvType.CV_8UC1);
            Core.randu(m1, 0, 25);
            Debug.Log("m1=" + m1.dump());

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";

            Mat dst_mat = new Mat();

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_ASCENDING);
            Debug.Log("ROW|ASCENDING:" + dst_mat.dump());

            ExecutionResultText.text += "ROW|ASCENDING:" + dst_mat.dump() + "\n";

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_DESCENDING);
            Debug.Log("ROW|DESCENDING:" + dst_mat.dump());

            ExecutionResultText.text += "ROW|DESCENDING:" + dst_mat.dump() + "\n";

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_ASCENDING);
            Debug.Log("COLUMN|ASCENDING:" + dst_mat.dump());

            ExecutionResultText.text += "COLUMN|ASCENDING:" + dst_mat.dump() + "\n";

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_DESCENDING);
            Debug.Log("COLUMN|DESCENDING:" + dst_mat.dump());

            ExecutionResultText.text += "COLUMN|DESCENDING:" + dst_mat.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // sort example
            //
            // The Core.sort function sorts the elements in a Mat object in ascending or descending order.
            // In other words, it allows you to sort the elements in a Mat object in a specific order.
            //

            // 5x5 matrix
            Mat m1 = new Mat (5, 5, CvType.CV_8UC1);
            Core.randu (m1, 0, 25);
            Debug.Log (""m1="" + m1.dump ());

            executionResultText.text = ""m1="" + m1.dump() + ""\n"";

            Mat dst_mat = new Mat ();

            // sort ascending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_ROW|Core.SORT_ASCENDING);
            Debug.Log (""ROW|ASCENDING:"" + dst_mat.dump ());

            // sort descending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_ROW|Core.SORT_DESCENDING);
            Debug.Log (""ROW|DESCENDING:"" + dst_mat.dump ());

            // sort ascending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_COLUMN|Core.SORT_ASCENDING);
            Debug.Log (""COLUMN|ASCENDING:"" + dst_mat.dump ());

            // sort descending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_COLUMN|Core.SORT_DESCENDING);
            Debug.Log (""COLUMN|DESCENDING:"" + dst_mat.dump ());
            ";

            UpdateScrollRect();
        }

        public void OnComparisonExampleButtonClick()
        {
            //
            // comparison example
            //
            // The Core.compare function compares the corresponding elements of two Mat objects and stores the result of the comparison in a new Mat object.
            // When the comparison result is true, the corresponding element of output array is set to 255.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            Debug.Log("m1=" + m1.dump());
            Debug.Log("m2=" + m2.dump());

            ExecutionResultText.text = "m1=" + m1.dump() + "\n";
            ExecutionResultText.text += "m2=" + m2.dump() + "\n";

            Mat dst_mat = new Mat();

            // GT (M1 > M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GT);
            Debug.Log("GT (M1 > M2)=" + dst_mat.dump());

            ExecutionResultText.text += "GT (M1 > M2)=" + dst_mat.dump() + "\n";

            // GE (M1 >= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GE);
            Debug.Log("GE (M1 >= M2)=" + dst_mat.dump());

            ExecutionResultText.text += "GE (M1 >= M2)=" + dst_mat.dump() + "\n";

            // EQ (M1 == M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_EQ);
            Debug.Log("EQ (M1 == M2)=" + dst_mat.dump());

            ExecutionResultText.text += "EQ (M1 == M2)=" + dst_mat.dump() + "\n";

            // NE (M1 != M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_NE);
            Debug.Log("NE (M1 != M2)=" + dst_mat.dump());

            ExecutionResultText.text += "NE (M1 != M2)=" + dst_mat.dump() + "\n";

            // LE (M1 <= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LE);
            Debug.Log("LE (M1 <= M2)=" + dst_mat.dump());

            ExecutionResultText.text += "LE (M1 <= M2)=" + dst_mat.dump() + "\n";

            // LT (M1 < M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LT);
            Debug.Log("LT (M1 < M2)=" + dst_mat.dump());

            ExecutionResultText.text += "LT (M1 < M2)=" + dst_mat.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // comparison example
            //
            // The Core.compare function compares the corresponding elements of two Mat objects and stores the result of the comparison in a new Mat object.
            // When the comparison result is true, the corresponding element of output array is set to 255.
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9);
            Mat m2 = new Mat (3, 3, CvType.CV_64FC1);
            m2.put (0, 0, 10,11,12,13,14,15,16,17,18);

            Debug.Log (""m1="" + m1.dump ());
            Debug.Log (""m2="" + m2.dump ());

            Mat dst_mat = new Mat ();

            // GT (M1 > M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_GT);
            Debug.Log (""GT (M1 > M2)="" + dst_mat.dump ());

            // GE (M1 >= M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_GE);
            Debug.Log (""GE (M1 >= M2)="" + dst_mat.dump ());

            // EQ (M1 == M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_EQ);
            Debug.Log (""EQ (M1 == M2)="" + dst_mat.dump ());

            // NE (M1 != M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_NE);
            Debug.Log (""NE (M1 != M2)="" + dst_mat.dump ());

            // LE (M1 <= M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_LE);
            Debug.Log (""LE (M1 <= M2)="" + dst_mat.dump ());

            // LT (M1 < M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_LT);
            Debug.Log (""LT (M1 < M2)="" + dst_mat.dump ());
            ";

            UpdateScrollRect();
        }

        public void OnGetAndPutExampleButtonClick()
        {
            //
            // get and put example
            //
            // mat.get() function gets the value of a specific element in a Mat object.
            // mat.put() function sets a new value for a specific element in a Mat object.
            //
            // OpenCVForUnity has several faster and more efficient functions for accessing Mat elements.
            // - Use the OpenCVMatUtils.CopyFromMat or OpenCVMatUtils.CopyToMat functions to copy through a data array in one go.
            // - Use the mat.at function to access the element of ​​Mat.
            // - Use the mat.AsSpan function to access the dara memory area of ​​Mat.
            //

            // channels=4 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));
            Debug.Log("m1=" + m1.dump());
            ExecutionResultText.text = "m1=" + m1.dump() + "\n";


            //
            // Get elements
            //

            // get an element value.
            double[] m1_1_1 = m1.get(1, 1);
            Debug.Log("m1[1,1] (use mat.get())=" + m1_1_1[0] + ", " + m1_1_1[1] + ", " + m1_1_1[2] + ", " + m1_1_1[3]);
            ExecutionResultText.text += "m1[1,1] (use mat.get())=" + m1_1_1[0] + ", " + m1_1_1[1] + ", " + m1_1_1[2] + ", " + m1_1_1[3] + "\n";

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            Span<byte> m1_2_2 = m1.at<byte>(2, 2);
            Debug.Log("m1[2,2] (use mat.at())=" + m1_2_2[0] + ", " + m1_2_2[1] + ", " + m1_2_2[2] + ", " + m1_2_2[3]);
            ExecutionResultText.text += "m1[2,2](use mat.at())=" + m1_2_2[0] + ", " + m1_2_2[1] + ", " + m1_2_2[2] + ", " + m1_2_2[3] + "\n";
#endif

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total() * m1.channels()];
            m1.get(0, 0, m1_array);
            string dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Debug.Log("m1_array (use mat.get())=" + dump_str);
            ExecutionResultText.text += "m1_array (use mat.get())=" + dump_str + "\n";

            // a faster and more efficient method using the OpenCVMatUtils.CopyFromMat function.
            OpenCVMatUtils.CopyFromMat(m1, m1_array);
            dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Debug.Log("m1_array (use OpenCVMatUtils.CopyFromMat())=" + dump_str);
            ExecutionResultText.text += "m1_array (use OpenCVMatUtils.CopyFromMat())=" + dump_str + "\n";

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            Span<byte> m1_span = m1.AsSpan<byte>();
            dump_str = "";
            for (int i = 0; i < m1_span.Length; i++)
            {
                dump_str += m1_span[i] + ", ";
            }
            Debug.Log("m1_span (use mat.AsSpan())=" + dump_str);
            ExecutionResultText.text += "m1_span (use mat.AsSpan())=" + dump_str + "\n";
#endif


            //
            // Put elements
            //

            // put an element value in a matrix.
            Mat m2 = m1.clone();
            m2.put(1, 1, 5, 6, 7, 8);
            Debug.Log("m2 (use mat.put())=" + m2.dump());
            ExecutionResultText.text += "m2 (use mat.put())=" + m2.dump() + "\n";

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_1_1 = m2.at<byte>(1, 1);
            m2_1_1[0] = 5;
            m2_1_1[1] = 6;
            m2_1_1[2] = 7;
            m2_1_1[3] = 8;
            Debug.Log("m2 (use mat.at())=" + m2.dump());
            ExecutionResultText.text += "m2 (use mat.at())=" + m2.dump() + "\n";
#endif

            // put an array of element values in a matrix.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            byte[] m2_arr = new byte[] {
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8
            };
            m2.put(0, 0, m2_arr);
            Debug.Log("m2 (use mat.put())=" + m2.dump());
            ExecutionResultText.text += "m2 (use mat.put())=" + m2.dump() + "\n";

            // a faster and more efficient method using the OpenCVMatUtils.CopyToMat function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            OpenCVMatUtils.CopyToMat(m2_arr, m2);
            Debug.Log("m2 (use OpenCVMatUtils.CopyToMat())=" + m2.dump());
            ExecutionResultText.text += "m2 (use OpenCVMatUtils.CopyToMat())=" + m2.dump() + "\n";

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_span = m2.AsSpan<byte>();
            m2_arr.AsSpan<byte>().CopyTo(m2_span);
            Debug.Log("m2 (use mat.AsSpan())=" + m2.dump());
            ExecutionResultText.text += "m2 (use mat.AsSpan())=" + m2.dump() + "\n";
#endif

            // fill element values (setTo method)
            m2.setTo(new Scalar(100, 100, 100, 100));
            Debug.Log("m2 (use mat.setTo())=" + m2.dump());
            ExecutionResultText.text += "m2 (use mat.setTo())=" + m2.dump() + "\n";

            ExampleCodeText.text = @"
            //
            // get and put example
            //
            // mat.get() function gets the value of a specific element in a Mat object.
            // mat.put() function sets a new value for a specific element in a Mat object.
            //
            // OpenCVForUnity has several faster and more efficient functions for accessing Mat elements.
            // - Use the OpenCVMatUtils.CopyFromMat or OpenCVMatUtils.CopyToMat functions to copy through a data array in one go.
            // - Use the mat.at function to access the element of ​​Mat.
            // - Use the mat.AsSpan function to access the data memory area of ​​Mat.
            //

            // channels=4 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));
            Debug.Log(""m1 = "" + m1.dump());


            //
            // Get elements
            //

            // get an element value.
            double[] m1_1_1 = m1.get(1, 1);
            Debug.Log(""m1[1,1] (use mat.get())="" + m1_1_1[0] + "", "" + m1_1_1[1] + "", "" + m1_1_1[2] + "", "" + m1_1_1[3]);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            Span<byte> m1_2_2 = m1.at<byte>(1, 1);
            Debug.Log(""m1[2, 2] (use mat.at())= "" + m1_2_2[0] + "", "" + m1_2_2[1] + "", "" + m1_2_2[2] + "", "" + m1_2_2[3]);
#endif

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total() * m1.channels()];
            m1.get(0, 0, m1_array);
            string dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + "", "";
            }
            Debug.Log(""m1_array (use mat.get())="" + dump_str);

            // a faster and more efficient method using the OpenCVMatUtils.CopyFromMat function.
            OpenCVMatUtils.CopyFromMat(m1, m1_array);
            dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + "", "";
            }
            Debug.Log(""m1_array (use OpenCVMatUtils.CopyFromMat())="" + dump_str);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            Span<byte> m1_span = m1.AsSpan<byte>();
            dump_str = "";
            for (int i = 0; i < m1_span.Length; i++)
            {
                dump_str += m1_span[i] + "", "";
            }
            Debug.Log(""m1_span (use mat.AsSpan())="" + dump_str);
#endif


            //
            // Put elements
            //

            // put an element value in a matrix.
            Mat m2 = m1.clone();
            m2.put(1, 1, 5, 6, 7, 8);
            Debug.Log(""m2 (use mat.put())="" + m2.dump());

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_1_1 = m2.at<byte>(1, 1);
            m2_1_1[0] = 5;
            m2_1_1[1] = 6;
            m2_1_1[2] = 7;
            m2_1_1[3] = 8;
            Debug.Log(""m2 (use mat.at())= "" + m2.dump());
#endif

            // put an array of element values in a matrix.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            byte[] m2_arr = new byte[] {
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8,
                5,
                6,
                7,
                8
            };
            m2.put(0, 0, m2_arr);
            Debug.Log(""m2 (use mat.put())="" + m2.dump());

            // a faster and more efficient method using the OpenCVMatUtils.CopyToMat function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            OpenCVMatUtils.CopyToMat(m2_arr, m2);
            Debug.Log(""m2 (use OpenCVMatUtils.CopyToMat())="" + m2.dump());

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_span = m2.AsSpan<byte>();
            m2_arr.AsSpan<byte>().CopyTo(m2_span);
            Debug.Log(""m2(use mat.AsSpan()) = "" + m2.dump());
#endif

            // fill element values (setTo method)
            m2.setTo(new Scalar(100, 100, 100, 100));
            Debug.Log(""m2 (use mat.setTo())="" + m2.dump());
            ";

            UpdateScrollRect();
        }

        public void OnAccessingPixelValueExampleButtonClick()
        {
            //
            // accessing pixel value example
            //
            // How access pixel values in an OpenCV Mat.
            // - 1. Use get and put method
            // - 2. Use mat.at method
            // - 3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method
            // - 4. Use mat.AsSpan method
            // - 5. Use pointer access (unsafe)
            //

            // channels=4 512x512 matrix (RGBA color image)
            Mat imgMat = new Mat(512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            //
            // 1. Use get and put method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Start();

            int rows = imgMat.rows();
            int cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    byte[] p = new byte[4];
                    imgMat.get(i0, i1, p);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B

                    imgMat.put(i0, i1, p);
                }
            }

            watch.Stop();

            Debug.Log("1. Use get and put method. time: " + watch.ElapsedMilliseconds + " ms");
            ExecutionResultText.text = "1. Use get and put method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 2. Use mat.at method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    // use the mat.at function to access the element of ​​Mat.
                    Span<byte> p = imgMat.at<byte>(i0, i1);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B
                }
            }

            watch.Stop();

            Debug.Log("2. Use mat.at method. time: " + watch.ElapsedMilliseconds + " ms");
            ExecutionResultText.text += "2. Use mat.at method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";

#endif

            //
            // 3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            OpenCVMatUtils.CopyFromMat(imgMat, img_array);

            long step0 = imgMat.step1(0);
            long step1 = imgMat.step1(1);

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    long p1 = step0 * i0 + step1 * i1;
                    long p2 = p1 + 1;
                    long p3 = p1 + 2;

                    img_array[p1] = (byte)(img_array[p1] + 127); // R
                    img_array[p2] = (byte)(img_array[p2] + 127); // G
                    img_array[p3] = (byte)(img_array[p3] + 127); // B
                }
            }
            // copies a pixel data Array to an OpenCV Mat data.
            OpenCVMatUtils.CopyToMat(img_array, imgMat);

            watch.Stop();

            Debug.Log("3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method. time: " + watch.ElapsedMilliseconds + " ms");
            ExecutionResultText.text += "3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 4. Use mat.AsSpan method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // use the mat.AsSpan function to access the data memory area of ​​Mat.
            Span<byte> img_span = imgMat.AsSpan<byte>();

            step0 = imgMat.step1(0);
            step1 = imgMat.step1(1);

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    int p1 = (int)(step0 * i0 + step1 * i1);
                    int p2 = p1 + 1;
                    int p3 = p1 + 2;

                    img_span[p1] = (byte)(img_span[p1] + 127); // R
                    img_span[p2] = (byte)(img_span[p2] + 127); // G
                    img_span[p3] = (byte)(img_span[p3] + 127); // B
                }
            }

            watch.Stop();

            Debug.Log("4. Use mat.AsSpan method. time: " + watch.ElapsedMilliseconds + " ms");
            ExecutionResultText.text += "4. Use mat.AsSpan method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";

#endif


#if !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 5. Use pointer access (unsafe)
            //

            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            step0 = imgMat.step1(0);
            step1 = imgMat.step1(1);
            long ptrVal = imgMat.dataAddr();

            unsafe
            {
                rows = imgMat.rows();
                cols = imgMat.cols();
                for (int i0 = 0; i0 < rows; i0++)
                {
                    for (int i1 = 0; i1 < cols; i1++)
                    {
                        byte* p1 = (byte*)(ptrVal + (step0 * i0) + (step1 * i1));
                        byte* p2 = p1 + 1;
                        byte* p3 = p1 + 2;

                        *p1 = (byte)(*p1 + 127); // R
                        *p2 = (byte)(*p2 + 127); // G
                        *p3 = (byte)(*p3 + 127); // B
                    }
                }
            }

            watch.Stop();

            Debug.Log("5. Use pointer access. time: " + watch.ElapsedMilliseconds + " ms");
            ExecutionResultText.text += "5. Use pointer access. time: " + watch.ElapsedMilliseconds + " ms" + "\n";

#endif


            ExampleCodeText.text = @"
            //
            // accessing pixel values example (unsafe)
            //

            // How access pixel value in an OpenCV Mat.

            // channels=4 512x512 matrix (RGBA color image)
            Mat imgMat = new Mat (512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            //
            // 1. Use get and put method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Start();

            int rows = imgMat.rows();
            int cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    byte[] p = new byte[4];
                    imgMat.get(i0, i1, p);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B

                    imgMat.put(i0, i1, p);
                }
            }

            watch.Stop();

            Debug.Log(""1.Use get and put method. time: "" + watch.ElapsedMilliseconds + "" ms"");

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 2. Use mat.at method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    // use the mat.at function to access the element of ​​Mat.
                    Span<byte> p = imgMat.at<byte>(i0, i1);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B
                }
            }

            watch.Stop();

            Debug.Log(""2.Use mat.at method. time: "" + watch.ElapsedMilliseconds + "" ms"");

#endif

            //
            // 3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            OpenCVMatUtils.CopyFromMat(imgMat, img_array);

            long step0 = imgMat.step1(0);
            long step1 = imgMat.step1(1);

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    long p1 = step0 * i0 + step1 * i1;
                    long p2 = p1 + 1;
                    long p3 = p1 + 2;

                    img_array[p1] = (byte)(img_array[p1] + 127); // R
                    img_array[p2] = (byte)(img_array[p2] + 127); // G
                    img_array[p3] = (byte)(img_array[p3] + 127); // B
                }
            }
            // copies a pixel data Array to an OpenCV Mat data.
            OpenCVMatUtils.CopyToMat(img_array, imgMat);

            watch.Stop();

            Debug.Log(""3. Use OpenCVMatUtils.CopyFromMat and OpenCVMatUtils.CopyToMat method. time: "" + watch.ElapsedMilliseconds + "" ms"");


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 4. Use mat.AsSpan method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // use the mat.AsSpan function to access the data memory area of ​​Mat.
            Span<byte> img_span = imgMat.AsSpan<byte>();

            step0 = imgMat.step1(0);
            step1 = imgMat.step1(1);

            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    int p1 = (int)(step0 * i0 + step1 * i1);
                    int p2 = p1 + 1;
                    int p3 = p1 + 2;

                    img_span[p1] = (byte)(img_span[p1] + 127); // R
                    img_span[p2] = (byte)(img_span[p2] + 127); // G
                    img_span[p3] = (byte)(img_span[p3] + 127); // B
                }
            }

            watch.Stop();

            Debug.Log(""4.Use mat.AsSpan method. time: "" + watch.ElapsedMilliseconds + "" ms"");

#endif


#if !OPENCV_DONT_USE_UNSAFE_CODE

            //
            // 5. Use pointer access
            //

            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            step0 = imgMat.step1(0);
            step1 = imgMat.step1(1);
            long ptrVal = imgMat.dataAddr();

            unsafe
            {
                rows = imgMat.rows();
                cols = imgMat.cols();
                for (int i0 = 0; i0 < rows; i0++)
                {
                    for (int i1 = 0; i1 < cols; i1++)
                    {
                        byte* p1 = (byte*)(ptrVal + (step0 * i0) + (step1 * i1));
                        byte* p2 = p1 + 1;
                        byte* p3 = p1 + 2;

                        *p1 = (byte)(*p1 + 127); // R
                        *p2 = (byte)(*p2 + 127); // G
                        *p3 = (byte)(*p3 + 127); // B
                    }
                }
            }

            watch.Stop();

            Debug.Log(""5. Use pointer access. time: "" + watch.ElapsedMilliseconds + "" ms"");

#endif
            ";

            UpdateScrollRect();
        }
    }
}
