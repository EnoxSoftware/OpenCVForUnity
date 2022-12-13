using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mat Basic Processing Example
    /// </summary>
    public class MatBasicProcessingExample : MonoBehaviour
    {
        public ScrollRect exampleCodeScrollRect;
        public UnityEngine.UI.Text exampleCodeText;
        public ScrollRect executionResultScrollRect;
        public UnityEngine.UI.Text executionResultText;

        // Use this for initialization
        IEnumerator Start()
        {
            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private void UpdateScrollRect()
        {
            exampleCodeScrollRect.verticalNormalizedPosition = executionResultScrollRect.verticalNormalizedPosition = 1f;
        }

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }


        public void OnInitializationExampleButtonClick()
        {
            //
            // initialization example
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
            Debug.Log("mat9.rows=" + mat9.rows() + " //For Mats of 3 dimensions or more, rows == cols == -1");
            Debug.Log("mat9.cols=" + mat9.cols());

            exampleCodeText.text = @"
            //
            // initialization example
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
            Debug.Log (""mat9.rows="" + mat9.rows () + "" //For Mats of 3 dimensions or more, rows == cols == -1"");
            Debug.Log (""mat9.cols="" + mat9.cols ());
            ";

            executionResultText.text = "mat1=" + mat1.dump() + "\n";
            executionResultText.text += "mat2=" + mat2.dump() + "\n";
            executionResultText.text += "mat3=" + mat3.dump() + "\n";
            executionResultText.text += "mat4=" + mat4.dump() + "\n";
            executionResultText.text += "mat5=" + mat5.dump() + "\n";
            executionResultText.text += "mat6=" + mat6.dump() + "\n";
            executionResultText.text += "mat7=" + mat7.dump() + "\n";
            executionResultText.text += "mat8=" + mat8.dump() + "\n";
            executionResultText.text += "mat9.dims=" + mat9.dims() + "\n";
            executionResultText.text += "mat9.rows=" + mat9.rows() + " //For Mats of 3 dimensions or more, rows == cols == -1" + "\n";
            executionResultText.text += "mat9.cols=" + mat9.cols() + "\n";

            UpdateScrollRect();
        }


        public void OnMultiChannelExampleButtonClick()
        {
            //
            // multi channel example
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

            exampleCodeText.text = @"
            //
            // multi channel example
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

            executionResultText.text = "mat1" + "\n";
            executionResultText.text += "   dim:" + mat1.dims() + " elemSize1:" + mat1.elemSize1() + " channels:" + mat1.channels() + "\n";
            executionResultText.text += "mat2" + "\n";
            executionResultText.text += "   dim:" + mat2.dims() + " elemSize1:" + mat2.elemSize1() + " channels:" + mat2.channels() + "\n";
            executionResultText.text += "mat3" + "\n";
            executionResultText.text += "   dim:" + mat3.dims() + " elemSize1:" + mat3.elemSize1() + " channels:" + mat3.channels() + "\n";

            UpdateScrollRect();
        }

        public void OnDumpExampleButtonClick()
        {
            //
            // dump example
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // dump
            Debug.Log("mat1=" + mat1);
            Debug.Log("mat1.dump()=" + mat1.dump());
            Debug.Log("mat1=" + mat2);
            Debug.Log("mat2.dump()=" + mat2.dump());

            exampleCodeText.text = @"
            //
            // dump example
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat (3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat (3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // dump
            Debug.Log (""mat1="" + mat1);
            Debug.Log (""mat1.dump()="" + mat1.dump());
            Debug.Log (""mat1="" + mat2);
            Debug.Log (""mat2.dump()="" + mat2.dump());
            ";

            executionResultText.text = "mat1=" + mat1 + "\n";
            executionResultText.text += "mat1.dump()=" + mat1.dump() + "\n";
            executionResultText.text += "mat2=" + mat2 + "\n";
            executionResultText.text += "mat2.dump()=" + mat2.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnCVExceptionHandlingExampleButtonClick()
        {
            //
            // CVException handling example
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

            executionResultText.text = "m1=" + m1 + "\n";
            executionResultText.text += "m1.dump()=" + m1.dump() + "\n";
            executionResultText.text += "m2=" + m2 + "\n";
            executionResultText.text += "m2.dump()=" + m2.dump() + "\n";

            // CVException handling
            // Publish CVException to Debug.LogError.
            Utils.setDebugMode(true, false);

            Mat m3 = m1 / m2; // element type is different.
            Debug.Log("m3=" + m3);
            executionResultText.text += "m3=" + m3 + "\n";

            Utils.setDebugMode(false);

            // Throw CVException.
            Utils.setDebugMode(true, true);
            try
            {
                Mat m4 = m1 / m2; // element type is different.
                Debug.Log("m4=" + m4);
                executionResultText.text += "m4=" + m4 + "\n";
            }
            catch (Exception e)
            {
                Debug.Log("CVException: " + e);
                executionResultText.text += "CVException: " + e + "\n";
            }
            Utils.setDebugMode(false);


            exampleCodeText.text = @"
            //
            // CVException handling example
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
            Utils.setDebugMode (true, false);

            Mat m3 = m1 / m2;
            Debug.Log(""m3="" + m3);

            Utils.setDebugMode (false);

            // Throw CVException.
            Utils.setDebugMode (true, true);
            try
            {
                Mat m4 = m1 / m2;
                Debug.Log(""m4="" + m4);
            }
            catch (Exception e)
            {
                Debug.Log (""CVException: "" + e);
            }
            Utils.setDebugMode (false);
            ";

            UpdateScrollRect();
        }

        public void OnPropertyExampleButtonClick()
        {
            //
            // property example
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


            exampleCodeText.text = @"
            //
            // property example
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

            executionResultText.text = "rows:" + mat1.rows() + "\n";
            executionResultText.text += "cols:" + mat1.cols() + "\n";
            executionResultText.text += "dims:" + mat1.dims() + "\n";
            executionResultText.text += "size[]:" + mat1.size().width + ", " + mat1.size().height + "\n";
            executionResultText.text += "depth (ID):" + mat1.depth() + "(=" + CvType.CV_64F + ")" + "\n";
            executionResultText.text += "channels:" + mat1.channels() + "\n";
            executionResultText.text += "elemSize:" + mat1.elemSize() + "[byte]" + "\n";
            executionResultText.text += "elemSize1 (elemSize/channels):" + mat1.elemSize1() + "[byte]" + "\n";
            executionResultText.text += "total:" + mat1.total() + "\n";
            executionResultText.text += "step (step1*elemSize1):" + mat1.step1() * mat1.elemSize1() + "[byte]" + "\n";
            executionResultText.text += "step1 (step/elemSize1):" + mat1.step1() + "\n";
            executionResultText.text += "isContinuous:" + mat1.isContinuous() + "\n";
            executionResultText.text += "isSubmatrix:" + mat1.isSubmatrix() + "\n";
            executionResultText.text += "empty:" + mat1.empty() + "\n";

            executionResultText.text += "==============================" + "\n";

            executionResultText.text += "rows:" + r1.rows() + "\n";
            executionResultText.text += "cols:" + r1.cols() + "\n";
            executionResultText.text += "dims:" + r1.dims() + "\n";
            executionResultText.text += "size[]:" + r1.size().width + ", " + r1.size().height + "\n";
            executionResultText.text += "depth (ID):" + r1.depth() + "(=" + CvType.CV_32F + ")" + "\n";
            executionResultText.text += "channels:" + r1.channels() + "\n";
            executionResultText.text += "elemSize:" + r1.elemSize() + "[byte]" + "\n";
            executionResultText.text += "elemSize1 (elemSize/channels):" + r1.elemSize1() + "[byte]" + "\n";
            executionResultText.text += "total:" + r1.total() + "\n";
            executionResultText.text += "step (step1*elemSize1):" + r1.step1() * r1.elemSize1() + "[byte]" + "\n";
            executionResultText.text += "step1 (step/elemSize1):" + r1.step1() + "\n";
            executionResultText.text += "isContinuous:" + r1.isContinuous() + "\n";
            executionResultText.text += "isSubmatrix:" + r1.isSubmatrix() + "\n";
            executionResultText.text += "empty:" + r1.empty() + "\n";

            executionResultText.text += "==============================" + "\n";

            executionResultText.text += "rows:" + mat3.rows() + "\n";
            executionResultText.text += "cols:" + mat3.cols() + "\n";
            executionResultText.text += "dims:" + mat3.dims() + "\n";
            executionResultText.text += "size[]:" + size + "\n";
            executionResultText.text += "depth (ID):" + mat3.depth() + "(=" + CvType.CV_32S + ")" + "\n";
            executionResultText.text += "channels:" + mat3.channels() + "\n";
            executionResultText.text += "elemSize:" + mat3.elemSize() + "[byte]" + "\n";
            executionResultText.text += "elemSize1 (elemSize/channels):" + mat3.elemSize1() + "[byte]" + "\n";
            executionResultText.text += "total:" + mat3.total() + "\n";
            executionResultText.text += "step (step1*elemSize1):" + step + "[byte]" + "\n";
            executionResultText.text += "step1 (step/elemSize1):" + mat3.step1() + "\n";
            executionResultText.text += "isContinuous:" + mat3.isContinuous() + "\n";
            executionResultText.text += "isSubmatrix:" + mat3.isSubmatrix() + "\n";
            executionResultText.text += "empty:" + mat3.empty() + "\n";

            UpdateScrollRect();
        }

        public void OnFourArithmeticOperationExampleButtonClick()
        {
            //
            // four arithmetic operation example
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0);

            Debug.Log("m1=" + m1.dump());
            executionResultText.text = "m1=" + m1.dump() + "\n";

            // matrix and scalar
            Mat m2 = m1 + new Scalar(3);
            Mat m3 = m1 - new Scalar(3);
            Mat m4 = m1 * 3; //scaling
            Mat m5 = m1 / 3;

            Debug.Log("m1+3=" + m2.dump());
            Debug.Log("m1-3=" + m3.dump());
            Debug.Log("m1*3=" + m4.dump());
            Debug.Log("m1/3=" + m5.dump());

            executionResultText.text += "m1+3=" + m2.dump() + "\n";
            executionResultText.text += "m1-3=" + m3.dump() + "\n";
            executionResultText.text += "m1*3=" + m4.dump() + "\n";
            executionResultText.text += "m1/3=" + m5.dump() + "\n";

            // matrix and matrix
            Mat m6 = m1 + m1;
            Mat m7 = m1.mul(m2);
            Mat m8 = m1.mul(m2, 2); //add scaling factor

            Debug.Log("m1+m1=" + m6.dump());
            Debug.Log("m1.mul(m2)=" + m7.dump());
            Debug.Log("m1.mul(m2, 2)=" + m8.dump());

            executionResultText.text += "m1+m1=" + m6.dump() + "\n";
            executionResultText.text += "m1.mul(m2)=" + m7.dump() + "\n";
            executionResultText.text += "m1.mul(m2, 2)=" + m8.dump() + "\n";

            // CVException handling
            // 8U, channels=1, 3x3
            Mat m9 = new Mat(3, 3, CvType.CV_8UC1);
            m9.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            // 64F, channels=1, 3x3
            Mat m10 = new Mat(2, 2, CvType.CV_64FC1);
            m10.put(0, 0, 1.0, 2.0, 3.0, 4.0);


            // Publish CVException to Debug.LogError.
            Utils.setDebugMode(true, false);

            Mat m11 = m1 / m9; // element type is different.
            Debug.Log("m1/m9=" + m11);
            executionResultText.text += "m1/m9=" + m11 + "\n";

            Mat m12 = m1 / m10; // matrix size is different.
            Debug.Log("m1/m10=" + m12);
            executionResultText.text += "m1/m10=" + m12 + "\n";

            Utils.setDebugMode(false);



            exampleCodeText.text = @"
            //
            // four arithmetic operation example
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0);

            Debug.Log (""m1="" + m1.dump ());
            executionResultText.text = ""m1="" + m1.dump () + ""\n"";

            // matrix and scalar
            Mat m2 = m1 + new Scalar (3);
            Mat m3 = m1 - new Scalar (3);
            Mat m4 = m1 * 3; //scaling
            Mat m5 = m1 / 3;

            Debug.Log (""m1+3="" + m2.dump ());
            Debug.Log (""m1-3="" + m3.dump ());
            Debug.Log (""m1*3="" + m4.dump ());
            Debug.Log (""m1/3="" + m5.dump ());

            executionResultText.text += ""m1+3="" + m2.dump () + ""\n"";
            executionResultText.text += ""m1-3="" + m3.dump () + ""\n"";
            executionResultText.text += ""m1*3="" + m4.dump () + ""\n"";
            executionResultText.text += ""m1/3="" + m5.dump () + ""\n"";

            // matrix and matrix
            Mat m6 = m1 + m1;
            Mat m7 = m1.mul (m2);
            Mat m8 = m1.mul (m2, 2); //add scaling factor

            Debug.Log (""m1+m1="" + m6.dump ());
            Debug.Log (""m1.mul(m2)="" + m7.dump ());
            Debug.Log (""m1.mul(m2, 2)="" + m8.dump ());

            executionResultText.text += ""m1+m1="" + m6.dump () + ""\n"";
            executionResultText.text += ""m1.mul(m2)="" + m7.dump () + ""\n"";
            executionResultText.text += ""m1.mul(m2, 2)="" + m8.dump () + ""\n"";

            // CVException handling
            // 8U, channels=1, 3x3
            Mat m9 = new Mat (3, 3, CvType.CV_8UC1);
            m9.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            // 64F, channels=1, 3x3
            Mat m10 = new Mat (2, 2, CvType.CV_64FC1);
            m10.put (0, 0, 1.0, 2.0, 3.0, 4.0);


            // Publish CVException to Debug.LogError.
            Utils.setDebugMode (true, false);

            Mat m11 = m1 / m9; // element type is different.
            Debug.Log (""m1/m9="" + m11);
            executionResultText.text += ""m1/m9="" + m11 + ""\n"";

            Mat m12 = m1 / m10; // matrix size is different.
            Debug.Log (""m1/m10="" + m12);
            executionResultText.text += ""m1/m10="" + m12 + ""\n"";

            Utils.setDebugMode (false);
            ";

            UpdateScrollRect();
        }

        public void OnConvertToExampleButtonClick()
        {
            //
            // convertTo example
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Debug.Log("m1=" + m1.dump());

            // dst mat, type
            Mat m2 = new Mat();
            m1.convertTo(m2, CvType.CV_8U);
            Debug.Log("m2=" + m2.dump());

            // dst mat, type, scale factor, added to the scaled value
            Mat m3 = new Mat();
            m1.convertTo(m3, CvType.CV_8U, 2, 10);
            Debug.Log("m3=" + m3.dump());

            exampleCodeText.text = @"
            //
            // convertTo example
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Debug.Log (""m1="" + m1.dump());

            // dst mat, type
            Mat m2 = new Mat ();
            m1.convertTo (m2, CvType.CV_8U);
            Debug.Log (""m2="" + m2.dump());

            // dst mat, type, scale factor, added to the scaled value
            Mat m3 = new Mat ();
            m1.convertTo (m3, CvType.CV_8U, 2, 10);
            Debug.Log (""m3="" + m3.dump());
            ";

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "m2=" + m2.dump() + "\n";
            executionResultText.text += "m3=" + m3.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnReshapeExampleButtonClick()
        {
            //
            // reshape example
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

            exampleCodeText.text = @"
            //
            // reshape example
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

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "ch=" + m1.channels() + "\n";
            executionResultText.text += "m2=" + m2.dump() + "\n";
            executionResultText.text += "ch=" + m2.channels() + "\n";
            executionResultText.text += "m3=" + m3.dump() + "\n";
            executionResultText.text += "ch=" + m3.channels() + "\n";
            executionResultText.text += "m4.dims=" + m4.dims() + "\n";
            executionResultText.text += "m4.size[]=" + size + "\n";
            executionResultText.text += "ch=" + m4.channels() + "\n";
            executionResultText.text += "m5=" + m5 + "\n";
            executionResultText.text += "ch=" + m5.channels() + "\n";

            UpdateScrollRect();
        }

        public void OnRangeExampleButtonClick()
        {
            //
            // range example
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

            exampleCodeText.text = @"
            //
            // range example
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

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "m1.rowRange(Range.all())=" + m1.rowRange(Range.all()).dump() + "\n";
            executionResultText.text += "m1.rowRange(new Range(0,2))=" + m1.rowRange(new Range(0, 2)).dump() + "\n";
            executionResultText.text += "m1.row(0)=" + m1.row(0).dump() + "\n";
            executionResultText.text += "m1.colRange(Range.all())=" + m1.colRange(Range.all()).dump() + "\n";
            executionResultText.text += "m1.colRange(new Range(0,2))=" + m1.colRange(new Range(0, 2)).dump() + "\n";
            executionResultText.text += "m1.col(0)=" + m1.col(0).dump() + "\n";

            UpdateScrollRect();
        }

        public void OnShallowCopyAndDeepCopyExampleButtonClick()
        {
            //
            // shallow copy and deep copy example
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

            executionResultText.text = "mat1=" + mat1.dump() + "\n";
            executionResultText.text += "m_shallow=" + m_shallow.dump() + "\n";
            executionResultText.text += "m_deep1=" + m_deep1.dump() + "\n";
            executionResultText.text += "m_deep2=" + m_deep2.dump() + "\n";

            // rewrite (0, 0) element of matrix mat1
            mat1.put(0, 0, 100);

            Debug.Log("mat1=" + mat1.dump());
            Debug.Log("m_shallow=" + m_shallow.dump());
            Debug.Log("m_deep1=" + m_deep1.dump());
            Debug.Log("m_deep2=" + m_deep2.dump());

            executionResultText.text += "mat1=" + mat1.dump() + "\n";
            executionResultText.text += "m_shallow=" + m_shallow.dump() + "\n";
            executionResultText.text += "m_deep1=" + m_deep1.dump() + "\n";
            executionResultText.text += "m_deep2=" + m_deep2.dump() + "\n";

            Debug.Log("mat1.Equals(m_shallow)=" + mat1.Equals(m_shallow));
            Debug.Log("mat1.Equals(m_deep1)=" + mat1.Equals(m_deep1));
            Debug.Log("mat1.Equals(m_deep2)=" + mat1.Equals(m_deep2));

            executionResultText.text += "mat1.Equals(m_shallow)=" + mat1.Equals(m_shallow) + "\n";
            executionResultText.text += "mat1.Equals(m_deep1)=" + mat1.Equals(m_deep1) + "\n";
            executionResultText.text += "mat1.Equals(m_deep2)=" + mat1.Equals(m_deep2) + "\n";

            exampleCodeText.text = @"
            //
            // shallow copy and deep copy example
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
            // simple composition: Merge example
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

            exampleCodeText.text = @"
            //
            // simple composition: Merge example
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

            executionResultText.text = "m_merged=" + m_merged.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnMixChannelsExampleButtonClick()
        {
            //
            // complex composition: mixChannels example
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

            exampleCodeText.text = @"
            //
            // complex composition: mixChannels example
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

            executionResultText.text = "m_mixed1=" + m_mixed1.dump() + "\n";
            executionResultText.text += "m_mixed2=" + m_mixed2.dump() + "\n";

            UpdateScrollRect();
        }

        public void OnSplitExampleButtonClick()
        {
            //
            // split example
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

            exampleCodeText.text = @"
            //
            // split example
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

            executionResultText.text = "";
            foreach (Mat item in planes)
            {
                executionResultText.text += item.dump() + "\n";
            }

            UpdateScrollRect();
        }

        public void OnReduceExampleButtonClick()
        {
            //
            // reduce example
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

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "v1(sum)=" + v1.dump() + "\n";
            executionResultText.text += "v2(avg)=" + v2.dump() + "\n";
            executionResultText.text += "v3(min)=" + v3.dump() + "\n";
            executionResultText.text += "v4(max)=" + v4.dump() + "\n";

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

            executionResultText.text += "m1=" + m1.dump() + "\n";
            executionResultText.text += "v1(sum)=" + v1.dump() + "\n";
            executionResultText.text += "v2(avg)=" + v2.dump() + "\n";
            executionResultText.text += "v3(min)=" + v3.dump() + "\n";
            executionResultText.text += "v4(max)=" + v4.dump() + "\n";

            exampleCodeText.text = @"
            //
            // reduce example
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

        public void OnSubmatrixExampleButtonClick()
        {
            //
            // submatrix (ROI) example
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Debug.Log("m1=" + m1.dump());

            executionResultText.text = "m1=" + m1.dump() + "\n";

            // get submatrix (ROI) of range (row[0_2] col[0_2])
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(0, 0, 2, 2));
            Debug.Log("m2=" + m2.dump());
            executionResultText.text += "m2=" + m2.dump() + "\n";
            Debug.Log("m2.submat()=" + m2.submat(0, 2, 0, 2).dump());
            executionResultText.text += "m2.submat()=" + m2.submat(0, 2, 0, 2).dump() + "\n";

            // find the parent matrix size of the submatrix (ROI) m2 and its position in it
            Size wholeSize = new Size();
            Point ofs = new Point();
            m2.locateROI(wholeSize, ofs);
            Debug.Log("wholeSize:" + wholeSize.width + "x" + wholeSize.height);
            Debug.Log("offset:" + ofs.x + ", " + ofs.y);

            executionResultText.text += "wholeSize:" + wholeSize.width + "x" + wholeSize.height + "\n";
            executionResultText.text += "offset:" + ofs.x + ", " + ofs.y + "\n";

            // expand the range of submatrix (ROI)
            m2.adjustROI(0, 1, 0, 1);
            Debug.Log("rows=" + m2.rows() + ", " + "cols=" + m2.cols());
            Debug.Log("m2=" + m2.dump());

            executionResultText.text += "rows=" + m2.rows() + ", " + "cols=" + m2.cols() + "\n";
            executionResultText.text += "m2=" + m2.dump() + "\n";

            exampleCodeText.text = @"
            //
            // submatrix (ROI) example
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

        public void OnRandShuffleExampleButtonClick()
        {
            //
            // randShuffle example
            //

            // 4x5 matrix
            Mat m1 = new Mat(4, 5, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Debug.Log("m1(original)=" + m1.dump());

            executionResultText.text = "m1(original)=" + m1.dump() + "\n";

            // shuffle
            Core.randShuffle(m1, UnityEngine.Random.value);
            Debug.Log("m1(shuffle)=" + m1.dump());

            executionResultText.text += "m1(shuffle)=" + m1.dump() + "\n";

            // submatrix
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(1, 1, 3, 2));
            Debug.Log("m2(sub-matrix)=" + m2.dump());

            executionResultText.text += "m2(sub-matrix)=" + m2.dump() + "\n";

            Core.randShuffle(m2, UnityEngine.Random.value);
            Debug.Log("m2(sub-matrix)=" + m2.dump());
            Debug.Log("m1=" + m1.dump());

            executionResultText.text += "m2(shuffle sub-matrix)=" + m2.dump() + "\n";
            executionResultText.text += "m1=" + m1.dump() + "\n";

            exampleCodeText.text = @"
            //
            // randShuffle example
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

            // 5x5 matrix
            Mat m1 = new Mat(5, 5, CvType.CV_8UC1);
            Core.randu(m1, 0, 25);
            Debug.Log("m1=" + m1.dump());

            executionResultText.text = "m1=" + m1.dump() + "\n";

            Mat dst_mat = new Mat();

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_ASCENDING);
            Debug.Log("ROW|ASCENDING:" + dst_mat.dump());

            executionResultText.text += "ROW|ASCENDING:" + dst_mat.dump() + "\n";

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_DESCENDING);
            Debug.Log("ROW|DESCENDING:" + dst_mat.dump());

            executionResultText.text += "ROW|DESCENDING:" + dst_mat.dump() + "\n";

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_ASCENDING);
            Debug.Log("COLUMN|ASCENDING:" + dst_mat.dump());

            executionResultText.text += "COLUMN|ASCENDING:" + dst_mat.dump() + "\n";

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_DESCENDING);
            Debug.Log("COLUMN|DESCENDING:" + dst_mat.dump());

            executionResultText.text += "COLUMN|DESCENDING:" + dst_mat.dump() + "\n";

            exampleCodeText.text = @"
            //
            // sort example
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

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            Debug.Log("m1=" + m1.dump());
            Debug.Log("m2=" + m2.dump());

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "m2=" + m2.dump() + "\n";

            Mat dst_mat = new Mat();

            // GT (M1 > M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GT);
            Debug.Log("GT (M1 > M2)=" + dst_mat.dump());

            executionResultText.text += "GT (M1 > M2)=" + dst_mat.dump() + "\n";

            // GE (M1 >= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GE);
            Debug.Log("GE (M1 >= M2)=" + dst_mat.dump());

            executionResultText.text += "GE (M1 >= M2)=" + dst_mat.dump() + "\n";

            // EQ (M1 == M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_EQ);
            Debug.Log("EQ (M1 == M2)=" + dst_mat.dump());

            executionResultText.text += "EQ (M1 == M2)=" + dst_mat.dump() + "\n";

            // NE (M1 != M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_NE);
            Debug.Log("NE (M1 != M2)=" + dst_mat.dump());

            executionResultText.text += "NE (M1 != M2)=" + dst_mat.dump() + "\n";

            // LE (M1 <= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LE);
            Debug.Log("LE (M1 <= M2)=" + dst_mat.dump());

            executionResultText.text += "LE (M1 <= M2)=" + dst_mat.dump() + "\n";

            // LT (M1 < M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LT);
            Debug.Log("LT (M1 < M2)=" + dst_mat.dump());

            executionResultText.text += "LT (M1 < M2)=" + dst_mat.dump() + "\n";

            exampleCodeText.text = @"
            //
            // comparison example
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

        public void OnOperatorsExampleButtonClick()
        {
            //
            // operators example
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            Scalar s = new Scalar(5);
            double alpha = 3;

            Debug.Log("m1=" + m1.dump());
            Debug.Log("m2=" + m2.dump());
            Debug.Log("s=" + s);
            Debug.Log("alpha=" + alpha);

            executionResultText.text = "m1=" + m1.dump() + "\n";
            executionResultText.text += "m2=" + m2.dump() + "\n";
            executionResultText.text += "s=" + s + "\n";
            executionResultText.text += "alpha=" + alpha + "\n";

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            // (M1 + M2 = Core.add (M1, M2, M_dst))
            Debug.Log("m1+m2=" + (m1 + m2).dump());
            executionResultText.text += "m1+m2=" + (m1 + m2).dump() + "\n";
            // (M1 + s = Core.add (M1, s, M_dst))
            Debug.Log("m1+s=" + (m1 + s).dump());
            executionResultText.text += "m1+s=" + (m1 + s).dump() + "\n";

            // (M1 - M2 = Core.subtract (M1, M2, M_dst))
            Debug.Log("m1-m2=" + (m1 - m2).dump());
            executionResultText.text += "m1-m2=" + (m1 - m2).dump() + "\n";
            // (M1 - s = Core.subtract (M1, s, M_dst))
            Debug.Log("m1-s=" + (m1 - s).dump());
            executionResultText.text += "m1-s=" + (m1 - s).dump() + "\n";

            // (-M1 = Core.multiply (M1, Scalar.all (-1), M_dst))
            Debug.Log("-m1=" + (-m1).dump());
            executionResultText.text += "-m1=" + (-m1).dump() + "\n";


            // Scaling: A*alpha A/alpha
            // (M1 * 3 = Core.multiply (M1, Scalar.all (3), M_dst))
            Debug.Log("m1*alpha=" + (m1 * alpha).dump());
            executionResultText.text += "m1*alpha=" + (m1 * alpha).dump() + "\n";
            // (M1 / 3 = Core.divide (M1, Scalar.all (3), M_dst))
            Debug.Log("m1/alpha=" + (m1 / alpha).dump());
            executionResultText.text += "m1/alpha=" + (m1 / alpha).dump() + "\n";


            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            // (M1.mul(M2) = M1.mul (M2))
            Debug.Log("m1.mul(m2)=" + (m1.mul(m2)).dump());
            executionResultText.text += "m1.mul(m2)=" + (m1.mul(m2)).dump() + "\n";

            // (M1 / M2 = Core.divide (M1, M2, M_dst))
            Debug.Log("m1/m2=" + (m1 / m2).dump());
            executionResultText.text += "m1/m2=" + (m1 / m2).dump() + "\n";

            // (3 / M1 = Core.divide (new Mat (M1.size (), M1.type (), Scalar.all (3)), M1, M_dst))
            Debug.Log("alpha/m2=" + (alpha / m2).dump());
            executionResultText.text += "alpha/m2=" + (alpha / m2).dump() + "\n";


            // Matrix multiplication: A*B
            // (M1 * M2 = Core.gemm (M1, M2, 1, new Mat (), 0, M_dst))
            Debug.Log("m1*m2=" + (m1 * m2).dump());
            executionResultText.text += "m1*m2=" + (m1 * m2).dump() + "\n";


            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            // (M1 & M2 = Core.bitwise_and (M1, M2, M_dst))
            Debug.Log("m1&m2=" + (m1 & m2).dump());
            executionResultText.text += "m1&m2=" + (m1 & m2).dump() + "\n";

            // (M1 | M2 = Core.bitwise_or (M1, M2, M_dst))
            Debug.Log("m1|m2=" + (m1 | m2).dump());
            executionResultText.text += "m1|m2=" + (m1 | m2).dump() + "\n";

            // (M1 ^ M2 = Core.bitwise_xor (M1, M2, M_dst))
            Debug.Log("m1^m2=" + (m1 ^ m2).dump());
            executionResultText.text += "m1^m2=" + (m1 ^ m2).dump() + "\n";

            // (~M1 = Core.bitwise_not (M1, M_dst))
            Debug.Log("~m1=" + (~m1).dump());
            executionResultText.text += "~m1=" + (~m1).dump() + "\n";

            // Note.
            // The assignment operator behavior is different from OpenCV (c ++). 
            // For example, C = A + B will not be expanded to cv :: add (A, B, C).
            // Also cannot assign a scalar to Mat like C = s.
            // In c#, it is not possible to explicitly overload compound assignment operators such as “A *= B“.
            // Instead, binary operator overloading is used implicitly.
            // Therefore, whenever an operator is used, a new mat is created and assigned.


            exampleCodeText.text = @"
            //
            // operators example
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9);
            Mat m2 = new Mat (3, 3, CvType.CV_64FC1);
            m2.put (0, 0, 10,11,12,13,14,15,16,17,18);
            Scalar s = new Scalar (5);
            double alpha = 3;

            Debug.Log (""m1="" + m1.dump ());
            Debug.Log (""m2="" + m2.dump ());
            Debug.Log (""s="" + s);
            Debug.Log (""alpha="" + alpha);

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            // (M1 + M2 = Core.add (M1, M2, M_dst))
            Debug.Log (""m1+m2="" + (m1 + m2).dump());
            // (M1 + s = Core.add (M1, s, M_dst))
            Debug.Log (""m1+s="" + (m1 + s).dump());

            // (M1 - M2 = Core.subtract (M1, M2, M_dst))
            Debug.Log (""m1-m2="" + (m1 - m2).dump());
            // (M1 - s = Core.subtract (M1, s, M_dst))
            Debug.Log (""m1-s="" + (m1 - s).dump());

            // (-M1 = Core.multiply (M1, Scalar.all (-1), M_dst))
            Debug.Log (""-m1="" + (-m1).dump());


            // Scaling: A*alpha A/alpha
            // (M1 * 3 = Core.multiply (M1, Scalar.all (3), M_dst))
            Debug.Log (""m1*alpha="" + (m1*alpha).dump());
            // (M1 / 3 = Core.divide (M1, Scalar.all (3), M_dst))
            Debug.Log (""m1/alpha="" + (m1/alpha).dump());


            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            // (M1.mul(M2) = M1.mul (M2))
            Debug.Log (""m1.mul(m2)="" + (m1.mul(m2)).dump());

            // (M1 / M2 = Core.divide (M1, M2, M_dst))
            Debug.Log (""m1/m2="" + (m1 / m2).dump());

            // (3 / M1 = Core.divide (new Mat (M1.size (), M1.type (), Scalar.all (3)), M1, M_dst))
            Debug.Log (""alpha/m2="" + (alpha / m2).dump());


            // Matrix multiplication: A*B
            // (M1 * M2 = Core.gemm (M1, M2, 1, new Mat (), 0, M_dst))
            Debug.Log (""m1*m2="" + (m1 * m2).dump());


            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            // (M1 & M2 = Core.bitwise_and (M1, M2, M_dst))
            Debug.Log (""m1&m2="" + (m1 & m2).dump());

            // (M1 | M2 = Core.bitwise_or (M1, M2, M_dst))
            Debug.Log (""m1|m2="" + (m1 | m2).dump());

            // (M1 ^ M2 = Core.bitwise_xor (M1, M2, M_dst))
            Debug.Log (""m1^m2="" + (m1 ^ m2).dump());

            // (~M1 = Core.bitwise_not (M1, M_dst))
            Debug.Log (""~m1="" + (~m1).dump());


            // Note.
            // The assignment operator behavior is different from OpenCV (c ++). 
            // For example, C = A + B will not be expanded to cv :: add (A, B, C).
            // Also cannot assign a scalar to Mat like C = s.
            // In c#, it is not possible to explicitly overload compound assignment operators such as “A *= B“.
            // Instead, binary operator overloading is used implicitly.
            // Therefore, whenever an operator is used, a new mat is created and assigned.
            ";

            UpdateScrollRect();
        }

        public void OnGetAndPutExampleButtonClick()
        {
            //
            // get and put example
            //

            // channels=4 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));
            Debug.Log("m1=" + m1.dump());

            executionResultText.text = "m1=" + m1.dump() + "\n";


            // get an element value.
            double[] m1_1_1 = m1.get(1, 1);
            Debug.Log("m1[1,1]=" + m1_1_1[0] + ", " + m1_1_1[1] + ", " + m1_1_1[2] + ", " + m1_1_1[3]);

            executionResultText.text += "m1[1,1]=" + m1_1_1[0] + ", " + m1_1_1[1] + ", " + m1_1_1[2] + ", " + m1_1_1[3] + "\n";

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total() * m1.channels()];
            m1.get(0, 0, m1_array);

            string dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Debug.Log("m1_array=" + dump_str);

            executionResultText.text += "m1_array=" + dump_str + "\n";

            // another faster way. (use MatUtils.copyFromMat())
            MatUtils.copyFromMat(m1, m1_array);

            dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Debug.Log("m1_array (use MatUtils.copyFromMat())=" + dump_str);

            executionResultText.text += "m1_array (use MatUtils.copyFromMat())=" + dump_str + "\n";


            // put an element value in a matrix.
            Mat m2 = m1.clone();
            m2.put(1, 1, 5, 6, 7, 8);
            Debug.Log("m2=" + m2.dump());

            executionResultText.text += "m2=" + m2.dump() + "\n";

            // put an array of element values in a matrix.
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
            Debug.Log("m2=" + m2.dump());

            executionResultText.text += "m2=" + m2.dump() + "\n";

            // another faster way. (use MatUtils.copyToMat())
            MatUtils.copyToMat(m2_arr, m2);
            Debug.Log("m2 (use MatUtils.copyToMat())=" + m2.dump());

            executionResultText.text += "m2 (use MatUtils.copyToMat())=" + m2.dump() + "\n";


            // fill element values (setTo method)
            m2.setTo(new Scalar(100, 100, 100, 100));
            Debug.Log("m2=" + m2.dump());

            executionResultText.text += "m2=" + m2.dump() + "\n";

            exampleCodeText.text = @"
            //
            // get and put example
            //

            // channels=4 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_8UC4 , new Scalar(1,2,3,4));
            Debug.Log (""m1="" + m1.dump ());

            // get an element value.
            double[] m1_1_1 = m1.get(1,1);
            Debug.Log (""m1[1,1]="" + m1_1_1[0] + "", "" + m1_1_1[1] + "", "" + m1_1_1[2] + "", "" + m1_1_1[3]);

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total () * m1.channels()];
            m1.get (0, 0, m1_array);

            string dump_str = """";
            foreach (var i in m1_array){
                dump_str += i + "", "";
            }
            Debug.Log (""m1_array="" + dump_str);

            // another faster way. (use MatUtils.copyFromMat())
            MatUtils.copyFromMat (m1, m1_array);

            dump_str = """";
            foreach (var i in m1_array) {
                dump_str += i + "", "";
            }
            Debug.Log (""m1_array (use MatUtils.copyFromMat())="" + dump_str);


            // put an element value in a matrix.
            Mat m2 = m1.clone ();
            m2.put (1, 1, 5,6,7,8);
            Debug.Log (""m2="" + m2.dump ());

            // put an array of element values in a matrix.
            byte[] m2_arr = new byte[]{5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8,5,6,7,8};
            m2.put (0, 0, m2_arr);
            Debug.Log (""m2="" + m2.dump ());

            // another faster way. (use MatUtils.copyToMat())
            MatUtils.copyToMat (m2_arr, m2);
            Debug.Log (""m2 (use MatUtils.copyToMat())="" + m2.dump ());


            // fill element values (setTo method)
            m2.setTo(new Scalar(100,100,100,100));
            Debug.Log (""m2="" + m2.dump ());
            ";

            UpdateScrollRect();
        }

        public void OnAccessingPixelValueExampleButtonClick()
        {
            //
            // accessing pixel value example
            //

            // How access pixel value in an OpenCV Mat.

            // channels=4 512x512 matrix (RGBA color image)
            Mat imgMat = new Mat(512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            //
            // 1. Use get and put method.
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
            executionResultText.text = "1. Use get and put method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";


            //
            // 2. Use MatIndexer.
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            MatIndexer indexer = new MatIndexer(imgMat);
            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    byte[] p = new byte[4];
                    indexer.get(i0, i1, p);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B

                    indexer.put(i0, i1, p);
                }
            }

            watch.Stop();

            Debug.Log("2. Use MatIndexer. time: " + watch.ElapsedMilliseconds + " ms");
            executionResultText.text += "2. Use MatIndexer. time: " + watch.ElapsedMilliseconds + " ms" + "\n";


            //
            // 3. Use MatUtils.copyFromMat and MatUtils.copyToMat method.
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // Copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            MatUtils.copyFromMat(imgMat, img_array);

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
            // Copies a pixel data Array to an OpenCV Mat data.
            MatUtils.copyToMat(img_array, imgMat);

            watch.Stop();

            Debug.Log("3. Use MatUtils.copyFromMat and MatUtils.copyToMat method. time: " + watch.ElapsedMilliseconds + " ms");
            executionResultText.text += "3. Use MatUtils.copyFromMat and MatUtils.copyToMat method. time: " + watch.ElapsedMilliseconds + " ms" + "\n";


#if OPENCV_USE_UNSAFE_CODE

            //
            // 4. Use pointer access.
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

            Debug.Log("4. Use pointer access. time: " + watch.ElapsedMilliseconds + " ms");
            executionResultText.text += "4. Use pointer access. time: " + watch.ElapsedMilliseconds + " ms" + "\n";

#endif


            exampleCodeText.text = @"
            //
            // accessing pixel value example
            //

            // How access pixel value in an OpenCV Mat.

            // channels=4 512x512 matrix (RGBA color image)
            Mat imgMat = new Mat (512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            //
            // 1. Use get and put method.
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


            //
            // 2. Use MatIndexer.
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            MatIndexer indexer = new MatIndexer(imgMat);
            rows = imgMat.rows();
            cols = imgMat.cols();
            for (int i0 = 0; i0 < rows; i0++)
            {
                for (int i1 = 0; i1 < cols; i1++)
                {
                    byte[] p = new byte[4];
                    indexer.get(i0, i1, p);

                    p[0] = (byte)(p[0] + 127); // R
                    p[1] = (byte)(p[1] + 127); // G
                    p[2] = (byte)(p[2] + 127); // B

                    indexer.put(i0, i1, p);
                }
            }

            watch.Stop();

            Debug.Log(""2. Use MatIndexer. time: "" + watch.ElapsedMilliseconds + "" ms"");


            //
            // 3. Use MatUtils.copyFromMat and MatUtils.copyToMat method.
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // Copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            MatUtils.copyFromMat(imgMat, img_array);

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
            // Copies a pixel data Array to an OpenCV Mat data.
            MatUtils.copyToMat(img_array, imgMat);

            watch.Stop();

            Debug.Log(""3. Use MatUtils.copyFromMat and MatUtils.copyToMat method. time: "" + watch.ElapsedMilliseconds + "" ms"");


#if OPENCV_USE_UNSAFE_CODE

            //
            // 4. Use pointer access.
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

            Debug.Log(""4. Use pointer access. time: "" + watch.ElapsedMilliseconds + "" ms"");

#endif
            ";

            UpdateScrollRect();
        }
    }
}