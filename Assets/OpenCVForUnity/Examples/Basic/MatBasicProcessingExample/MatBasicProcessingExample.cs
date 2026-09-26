using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mat Basic Processing Example
    /// Interactive reference for common OpenCV <see cref="Mat"/> operations; each button runs a demo and shows sample code in <see cref="ExampleCodeInputField"/>.
    ///
    /// Demonstrates:
    /// - Mat shape model: empty, 0D scalar, true 1D vector, 2D matrix/image, and ND arrays (OpenCV 5)
    /// - Mat creation (ones, zeros, eye, random fill, 0D/1D/ND constructors)
    /// - <see cref="Mat.checkVector"/> for element count / vector-shape validation
    /// - Java-wrapper <c>MatOf*</c> helpers (typed Mat, usually 1D; can also wrap compatible 2D Mats)
    /// - Channel layout vs dims, properties, reshape, transpose, and submatrix views
    /// - Shallow vs deep copy, merge/split/mixChannels, and element-wise math
    /// - Element access with get/put, mat.at, AsSpan, and MatBufferUtils (including 0D/1D indexing)
    /// - Native OpenCV error reporting via <see cref="OpenCVDebug"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="CvType"/>, <see cref="Scalar"/>, <see cref="Range"/>, <see cref="MatOfInt"/>, <see cref="MatOfPoint"/>
    /// - <see cref="Core"/>: add, subtract, multiply, divide, compare, convertScaleAbs, merge, split, mixChannels, reduce, randu, randn, sort, transpose
    /// - <see cref="MatBufferUtils"/>, <see cref="OpenCVDebug"/>
    ///
    /// Unity integration:
    /// - Demo methods call <see cref="Log"/> so Console output is also collected into <see cref="ExecutionResultInputField"/>.
    /// - <see cref="ExampleCodeInputField"/> mirrors the executed sample (including those Log calls). When copying into your own scripts, replace Log with Debug.Log.
    /// - Both panels use read-only <see cref="InputField"/> so users can select and copy text.
    /// </summary>
    public class MatBasicProcessingExample : MonoBehaviour
    {
        // Public Fields
        public ScrollRect ExampleCodeScrollRect;
        public InputField ExampleCodeInputField;
        public ScrollRect ExecutionResultScrollRect;
        public InputField ExecutionResultInputField;

        // Private Fields
        private readonly System.Text.StringBuilder _resultBuf = new System.Text.StringBuilder();

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

        /// <summary>
        /// Clears the result buffer before a demo button runs.
        /// </summary>
        private void BeginExample()
        {
            _resultBuf.Clear();
        }

        /// <summary>
        /// Writes to the Unity Console and appends the same line to the on-screen execution result.
        /// </summary>
        /// <param name="message">Message object (same usage as Debug.Log).</param>
        private void Log(object message)
        {
            string line = message != null ? message.ToString() : string.Empty;
            Debug.Log(line, this);
            _resultBuf.AppendLine(line);
        }

        /// <summary>
        /// Flushes collected log lines to <see cref="ExecutionResultInputField"/> and shows the mirrored sample in <see cref="ExampleCodeInputField"/>.
        /// </summary>
        /// <param name="exampleCode">Source text shown in the Example Code panel (should match the executed demo).</param>
        private void EndExample(string exampleCode)
        {
            ExecutionResultInputField.text = _resultBuf.ToString();
            ExampleCodeInputField.text = exampleCode;
            UpdateScrollRect();
        }

        private static string FormatShapeRow(string kind, Mat m, string note)
        {
            return string.Format("{0,-8} {1,5} {2,6} {3,6} {4,5} {5,5}  {6}",
                kind, m.dims(), m.empty(), (int)m.total(), m.rows(), m.cols(), note);
        }

        // Public Methods
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        public void OnShapeOverviewExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  shape overview example (0D / 1D / 2D / ND)
            // ---------------------------------------------------------------------------------------
            // OpenCV 5 Mat is shape (dims) x type (depth + channels).
            // empty and 0D scalar both can report dims==0 — distinguish them with empty() / total().
            // A true 1D vector is NOT the same as a 2D Nx1 (or 1xN) matrix.
            //
            // kind     dims  empty  total  rows  cols  note
            // empty    0     true   0      0     0     no data
            // 0D       0     false  1      1     1     one scalar value
            // 1D       1     false  N      1     N     true vector (OpenCV 5)
            // 2D       2     false  R*C    R     C     image / matrix
            // ND       >=3   false  prod   -1    -1    use size(i)
            //

            Log("kind     dims  empty  total  rows  cols  note");
            Log("-------- ----- ------ ------ ----- ----- ----");

            // Empty Mat (no data)
            Mat empty = new Mat();
            Log(FormatShapeRow("empty", empty, "no data"));
            Log("empty = " + empty.dump());

            // 0D scalar (one value; empty()==false)
            Mat scalar = new Mat(Array.Empty<int>(), CvType.CV_64FC1, new Scalar(3.14));
            Log(FormatShapeRow("0D", scalar, "one scalar value"));
            Log("scalar = " + scalar.dump());

            // True 1D vector of length 4
            Mat vec = new Mat(new int[] { 4 }, CvType.CV_64FC1);
            vec.put(0, 0, 1, 2, 3, 4);
            Log(FormatShapeRow("1D", vec, "true vector; put(0,i) / get(0,i)"));
            Log("vec = " + vec.dump());

            // Contrast: 2D column vector (Nx1) — same element count, different dims
            Mat colVec2d = new Mat(4, 1, CvType.CV_64FC1);
            colVec2d.put(0, 0, 1, 2, 3, 4);
            Log(FormatShapeRow("2D Nx1", colVec2d, "NOT a true 1D; put(i,0)"));
            Log("colVec2d = " + colVec2d.dump());
            Log("vec.step1() = " + vec.step1());
            Log("colVec2d.step1() = " + colVec2d.step1());

            // 2D matrix / image
            Mat m2d = new Mat(2, 3, CvType.CV_64FC1);
            m2d.put(0, 0, 1, 2, 3, 4, 5, 6);
            Log(FormatShapeRow("2D", m2d, "image / matrix"));
            Log("m2d = " + m2d.dump());

            // ND tensor
            Mat nd = new Mat(new int[] { 2, 2, 3 }, CvType.CV_8UC1, Scalar.all(0));
            Log(FormatShapeRow("ND", nd, "use size(i); rows/cols are -1"));
            Log("nd = " + nd);

            Log("");
            Log("OpenCV 4 vs 5 tip for length-N vectors:");
            Log("  OpenCV 4 often used Nx1 (dims=2, rows=N, cols=1, put(i,0)).");
            Log("  OpenCV 5 true 1D: new Mat(new int[]{ N }, type) -> dims=1, rows=1, cols=N, put(0,i).");
            Log("  Prefer total() or checkVector() for element count — do not assume rows()==N.");
            Log("  channels() is independent of dims (e.g. CV_8UC3 has 3 channels on any shape).");
            Log("  dump() works for dims<=2 (empty/0D/1D/2D). For dims>2, reshape to 2D first.");

            Log("");
            Log("empty vs 0D — when to use which:");
            Log("  empty (new Mat()): no data yet; common as an output destination before Core ops fill it.");
            Log("  0D scalar: a real one-element Mat (empty()==false, total()==1); not the same as empty.");
            Log("  Tip: if (m.empty()) means no buffer; a 0D Mat is valid data with one element.");

            Log("");
            Log("checkVector(elemChannels) — element count if the Mat is a vector-like layout, else -1:");
            // True 1D length-4, 1 channel -> 4 elements
            Log("vec.checkVector(1) = " + vec.checkVector(1));
            // 2D Nx1 is also accepted as a column vector of 1-channel elements
            Log("colVec2d.checkVector(1) = " + colVec2d.checkVector(1));
            // General 2x3 matrix is not a vector for elemChannels==1
            Log("m2d.checkVector(1) = " + m2d.checkVector(1) + " // -1: not a row/column vector");
            // Multi-channel column vector: Nx1 CV_32FC2 -> checkVector(2) == N
            Mat ch2col = new Mat(3, 1, CvType.CV_32FC2);
            Log("ch2col (3x1 CV_32FC2).checkVector(2) = " + ch2col.checkVector(2));
            Log("ch2col.checkVector(1) = " + ch2col.checkVector(1) + " // -1: channel count mismatch");

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  shape overview example (0D / 1D / 2D / ND)
            // ---------------------------------------------------------------------------------------
            // OpenCV 5 Mat is shape (dims) x type (depth + channels).
            // empty and 0D scalar both can report dims==0 — distinguish them with empty() / total().
            // A true 1D vector is NOT the same as a 2D Nx1 (or 1xN) matrix.
            //
            // kind     dims  empty  total  rows  cols  note
            // empty    0     true   0      0     0     no data
            // 0D       0     false  1      1     1     one scalar value
            // 1D       1     false  N      1     N     true vector (OpenCV 5)
            // 2D       2     false  R*C    R     C     image / matrix
            // ND       >=3   false  prod   -1    -1    use size(i)
            //

            Log(""kind     dims  empty  total  rows  cols  note"");
            Log(""-------- ----- ------ ------ ----- ----- ----"");

            // Empty Mat (no data)
            Mat empty = new Mat();
            Log(FormatShapeRow(""empty"", empty, ""no data""));
            Log(""empty = "" + empty.dump());

            // 0D scalar (one value; empty()==false)
            Mat scalar = new Mat(Array.Empty<int>(), CvType.CV_64FC1, new Scalar(3.14));
            Log(FormatShapeRow(""0D"", scalar, ""one scalar value""));
            Log(""scalar = "" + scalar.dump());

            // True 1D vector of length 4
            Mat vec = new Mat(new int[] { 4 }, CvType.CV_64FC1);
            vec.put(0, 0, 1, 2, 3, 4);
            Log(FormatShapeRow(""1D"", vec, ""true vector; put(0,i) / get(0,i)""));
            Log(""vec = "" + vec.dump());

            // Contrast: 2D column vector (Nx1) — same element count, different dims
            Mat colVec2d = new Mat(4, 1, CvType.CV_64FC1);
            colVec2d.put(0, 0, 1, 2, 3, 4);
            Log(FormatShapeRow(""2D Nx1"", colVec2d, ""NOT a true 1D; put(i,0)""));
            Log(""colVec2d = "" + colVec2d.dump());
            Log(""vec.step1() = "" + vec.step1());
            Log(""colVec2d.step1() = "" + colVec2d.step1());

            // 2D matrix / image
            Mat m2d = new Mat(2, 3, CvType.CV_64FC1);
            m2d.put(0, 0, 1, 2, 3, 4, 5, 6);
            Log(FormatShapeRow(""2D"", m2d, ""image / matrix""));
            Log(""m2d = "" + m2d.dump());

            // ND tensor
            Mat nd = new Mat(new int[] { 2, 2, 3 }, CvType.CV_8UC1, Scalar.all(0));
            Log(FormatShapeRow(""ND"", nd, ""use size(i); rows/cols are -1""));
            Log(""nd = "" + nd);

            Log("""");
            Log(""OpenCV 4 vs 5 tip for length-N vectors:"");
            Log(""  OpenCV 4 often used Nx1 (dims=2, rows=N, cols=1, put(i,0))."");
            Log(""  OpenCV 5 true 1D: new Mat(new int[]{ N }, type) -> dims=1, rows=1, cols=N, put(0,i)."");
            Log(""  Prefer total() or checkVector() for element count — do not assume rows()==N."");
            Log(""  channels() is independent of dims (e.g. CV_8UC3 has 3 channels on any shape)."");
            Log(""  dump() works for dims<=2 (empty/0D/1D/2D). For dims>2, reshape to 2D first."");

            Log("""");
            Log(""empty vs 0D — when to use which:"");
            Log(""  empty (new Mat()): no data yet; common as an output destination before Core ops fill it."");
            Log(""  0D scalar: a real one-element Mat (empty()==false, total()==1); not the same as empty."");
            Log(""  Tip: if (m.empty()) means no buffer; a 0D Mat is valid data with one element."");

            Log("""");
            Log(""checkVector(elemChannels) — element count if the Mat is a vector-like layout, else -1:"");
            // True 1D length-4, 1 channel -> 4 elements
            Log(""vec.checkVector(1) = "" + vec.checkVector(1));
            // 2D Nx1 is also accepted as a column vector of 1-channel elements
            Log(""colVec2d.checkVector(1) = "" + colVec2d.checkVector(1));
            // General 2x3 matrix is not a vector for elemChannels==1
            Log(""m2d.checkVector(1) = "" + m2d.checkVector(1) + "" // -1: not a row/column vector"");
            // Multi-channel column vector: Nx1 CV_32FC2 -> checkVector(2) == N
            Mat ch2col = new Mat(3, 1, CvType.CV_32FC2);
            Log(""ch2col (3x1 CV_32FC2).checkVector(2) = "" + ch2col.checkVector(2));
            Log(""ch2col.checkVector(1) = "" + ch2col.checkVector(1) + "" // -1: channel count mismatch"");
            ");
        }

        public void OnInitializationExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  initialization example
            // ---------------------------------------------------------------------------------------
            // Showcase initialization methods for different matrix types and sizes,
            // including OpenCV 5 true 0D / 1D constructors.
            //

            // 3x3 matrix (set array value)
            // CvType.CV_64FC1 = 64-bit float, 1 channel per pixel.
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            mat1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log("mat1 = " + mat1.dump());

            // 2x2 rotation matrix
            double angle = 30, a = Math.Cos(angle * Math.PI / 180), b = Math.Sin(angle * Math.PI / 180);
            Mat mat2 = new Mat(2, 2, CvType.CV_64FC1);
            mat2.put(0, 0, a, -b, b, a);
            Log("mat2 = " + mat2.dump());

            // 5x5 all 1's matrix
            Mat mat3 = Mat.ones(5, 5, CvType.CV_64FC1);
            Log("mat3 = " + mat3.dump());

            // 5x5 all zero's matrix
            Mat mat4 = Mat.zeros(5, 5, CvType.CV_64FC1);
            Log("mat4 = " + mat4.dump());

            // 5x5 identity matrix
            Mat mat5 = Mat.eye(5, 5, CvType.CV_64FC1);
            Log("mat5 = " + mat5.dump());

            // 3x3 initialize with a constant
            Mat mat6 = new Mat(3, 3, CvType.CV_64FC1, new Scalar(5));
            Log("mat6 = " + mat6.dump());

            // 3x2 initialize with a uniform distribution random number
            Mat mat7 = new Mat(3, 2, CvType.CV_8UC1);
            Core.randu(mat7, 0, 256);
            Log("mat7 = " + mat7.dump());

            // 3x2 initialize with a normal distribution random number
            Mat mat8 = new Mat(3, 2, CvType.CV_8UC1);
            Core.randn(mat8, 128, 10);
            Log("mat8 = " + mat8.dump());

            // 0D scalar (OpenCV 5): one value, dims==0, empty()==false
            Mat mat0d = new Mat(Array.Empty<int>(), CvType.CV_64FC1, new Scalar(42));
            Log("mat0d.dims() = " + mat0d.dims());
            Log("mat0d.empty() = " + mat0d.empty());
            Log("mat0d.total() = " + mat0d.total());
            Log("mat0d = " + mat0d.dump());

            // True 1D vector (OpenCV 5): dims==1, rows==1, cols==N
            // Note: new Mat(5, 1, type) creates a 2D 5x1 matrix — not a true 1D Mat.
            Mat mat1d = new Mat(new int[] { 5 }, CvType.CV_64FC1);
            mat1d.put(0, 0, 1, 2, 3, 4, 5);
            Log("mat1d.dims() = " + mat1d.dims());
            Log("mat1d.rows() = " + mat1d.rows());
            Log("mat1d.cols() = " + mat1d.cols());
            Log("mat1d = " + mat1d.dump());
            Mat mat1dOnes = Mat.ones(new int[] { 5 }, CvType.CV_64FC1);
            Mat mat1dZeros = Mat.zeros(new int[] { 5 }, CvType.CV_64FC1);
            Log("mat1dOnes = " + mat1dOnes.dump());
            Log("mat1dZeros = " + mat1dZeros.dump());

            // 2x2x3x4 matrix (4 dimensional array)
            // For ndim > 2, rows()/cols() return (-1, -1); use size(i) and dims() instead.
            int[] sizes = new int[] { 2, 2, 3, 4 };
            Mat mat9 = new Mat(sizes, CvType.CV_8UC1, Scalar.all(0));
            Log("mat9.dims() = " + mat9.dims());
            Log("mat9.rows() = " + mat9.rows() + " //When the matrix is more than 2-dimensional, the returned size is (-1, -1).");
            Log("mat9.cols() = " + mat9.cols());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  initialization example
            // ---------------------------------------------------------------------------------------
            // Showcase initialization methods for different matrix types and sizes,
            // including OpenCV 5 true 0D / 1D constructors.
            //

            // 3x3 matrix (set array value)
            // CvType.CV_64FC1 = 64-bit float, 1 channel per pixel.
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            mat1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log(""mat1 = "" + mat1.dump());

            // 2x2 rotation matrix
            double angle = 30, a = Math.Cos(angle*Math.PI/180), b = Math.Sin(angle*Math.PI/180);
            Mat mat2 = new Mat (2, 2, CvType.CV_64FC1);
            mat2.put (0, 0, a, -b, b, a);
            Log(""mat2 = "" + mat2.dump());

            // 5x5 all 1's matrix
            Mat mat3 = Mat.ones(5, 5, CvType.CV_64FC1);
            Log(""mat3 = "" + mat3.dump());

            // 5x5 all zero's matrix
            Mat mat4 = Mat.zeros(5, 5, CvType.CV_64FC1);
            Log(""mat4 = "" + mat4.dump());

            // 5x5 identity matrix
            Mat mat5 = Mat.eye(5, 5, CvType.CV_64FC1);
            Log(""mat5 = "" + mat5.dump());

            // 3x3 initialize with a constant
            Mat mat6 = new Mat (3, 3, CvType.CV_64FC1, new Scalar(5));
            Log(""mat6 = "" + mat6.dump());

            // 3x2 initialize with a uniform distribution random number
            Mat mat7 = new Mat (3, 2, CvType.CV_8UC1);
            Core.randu (mat7, 0, 256);
            Log(""mat7 = "" + mat7.dump());

            // 3x2 initialize with a normal distribution random number
            Mat mat8 = new Mat (3, 2, CvType.CV_8UC1);
            Core.randn (mat8, 128, 10);
            Log(""mat8 = "" + mat8.dump());

            // 0D scalar (OpenCV 5): one value, dims==0, empty()==false
            Mat mat0d = new Mat(Array.Empty<int>(), CvType.CV_64FC1, new Scalar(42));
            Log(""mat0d.dims() = "" + mat0d.dims());
            Log(""mat0d.empty() = "" + mat0d.empty());
            Log(""mat0d.total() = "" + mat0d.total());
            Log(""mat0d = "" + mat0d.dump());

            // True 1D vector (OpenCV 5): dims==1, rows==1, cols==N
            // Note: new Mat(5, 1, type) creates a 2D 5x1 matrix — not a true 1D Mat.
            Mat mat1d = new Mat(new int[] { 5 }, CvType.CV_64FC1);
            mat1d.put(0, 0, 1, 2, 3, 4, 5);
            Log(""mat1d.dims() = "" + mat1d.dims());
            Log(""mat1d.rows() = "" + mat1d.rows());
            Log(""mat1d.cols() = "" + mat1d.cols());
            Log(""mat1d = "" + mat1d.dump());
            Mat mat1dOnes = Mat.ones(new int[] { 5 }, CvType.CV_64FC1);
            Mat mat1dZeros = Mat.zeros(new int[] { 5 }, CvType.CV_64FC1);
            Log(""mat1dOnes = "" + mat1dOnes.dump());
            Log(""mat1dZeros = "" + mat1dZeros.dump());

            // 2x2x3x4 matrix (4 dimensional array)
            // For ndim > 2, rows()/cols() return (-1, -1); use size(i) and dims() instead.
            int[] sizes = new int[]{ 2, 2, 3, 4 };
            Mat mat9 = new Mat (sizes, CvType.CV_8UC1, Scalar.all (0));
            Log(""mat9.dims() = "" + mat9.dims());
            Log(""mat9.rows() = "" + mat9.rows () + "" //When the matrix is more than 2-dimensional, the returned size is (-1, -1)."");
            Log(""mat9.cols() = "" + mat9.cols ());
            ");
        }

        public void OnMultiChannelExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  multi channel example
            // ---------------------------------------------------------------------------------------
            // Initialization of matrices with various numbers of channels, including those with four or more channels.
            // channels() is independent of dims(): e.g. a 1D Mat can still be CV_8UC3.
            //

            // 64F, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            Log("mat1.dims() = " + mat1.dims());
            Log("mat1.elemSize1() = " + mat1.elemSize1());
            Log("mat1.channels() = " + mat1.channels());

            // 64F, channels=10, 3x3 — CV_64FC(n) creates n channels interleaved per pixel.
            Mat mat2 = new Mat(3, 3, CvType.CV_64FC(10));
            Log("mat2.dims() = " + mat2.dims());
            Log("mat2.elemSize1() = " + mat2.elemSize1());
            Log("mat2.channels() = " + mat2.channels());

            // 64F, channels=1, 2x2x3x4 (4 dimensional array)
            int[] sizes = new int[] { 2, 2, 3, 4 };
            Mat mat3 = new Mat(sizes, CvType.CV_64FC1);
            Log("mat3.dims() = " + mat3.dims());
            Log("mat3.elemSize1() = " + mat3.elemSize1());
            Log("mat3.channels() = " + mat3.channels());

            // 1D + multi-channel: shape is still 1D; each element has 3 channels
            Mat mat4 = new Mat(new int[] { 4 }, CvType.CV_8UC3, new Scalar(1, 2, 3));
            Log("mat4.dims() = " + mat4.dims());
            Log("mat4.cols() = " + mat4.cols());
            Log("mat4.channels() = " + mat4.channels());
            Log("mat4 = " + mat4.dump());

            // Color / RGBA images are still dims==2; channels are not Mat dimensions.
            Mat rgba = new Mat(2, 2, CvType.CV_8UC4, new Scalar(255, 128, 64, 255));
            Log("rgba.dims() = " + rgba.dims() + " // color image remains dims==2");
            Log("rgba.channels() = " + rgba.channels());
            Log("rgba = " + rgba.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  multi channel example
            // ---------------------------------------------------------------------------------------
            // channels() is independent of dims() (a 1D Mat can still be CV_8UC3).
            // A color image (e.g. CV_8UC4) is still dims==2 — channels are not Mat dimensions.
            //

            // 64F, channels=1, 3x3
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            Log(""mat1.dims() = "" + mat1.dims());
            Log(""mat1.elemSize1() = "" + mat1.elemSize1());
            Log(""mat1.channels() = "" + mat1.channels());

            // 64F, channels=10, 3x3 — CV_64FC(n) creates n channels interleaved per pixel.
            Mat mat2 = new Mat (3, 3, CvType.CV_64FC(10));
            Log(""mat2.dims() = "" + mat2.dims());
            Log(""mat2.elemSize1() = "" + mat2.elemSize1());
            Log(""mat2.channels() = "" + mat2.channels());

            // 64F, channels=1, 2x2x3x4 (4 dimensional array)
            int[] sizes = new int[]{ 2, 2, 3, 4 };
            Mat mat3 = new Mat (sizes, CvType.CV_64FC1);
            Log(""mat3.dims() = "" + mat3.dims());
            Log(""mat3.elemSize1() = "" + mat3.elemSize1());
            Log(""mat3.channels() = "" + mat3.channels());

            // 1D + multi-channel
            Mat mat4 = new Mat(new int[] { 4 }, CvType.CV_8UC3, new Scalar(1, 2, 3));
            Log(""mat4.dims() = "" + mat4.dims());
            Log(""mat4.cols() = "" + mat4.cols());
            Log(""mat4.channels() = "" + mat4.channels());
            Log(""mat4 = "" + mat4.dump());

            // Color image: dims==2, channels==4
            Mat rgba = new Mat(2, 2, CvType.CV_8UC4, new Scalar(255, 128, 64, 255));
            Log(""rgba.dims() = "" + rgba.dims() + "" // color image remains dims==2"");
            Log(""rgba.channels() = "" + rgba.channels());
            Log(""rgba = "" + rgba.dump());
            ");
        }

        public void OnMatOfExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  MatOf* example (OpenCVForUnity / Java-wrapper typed Mats)
            // ---------------------------------------------------------------------------------------
            // MatOf* classes (MatOfInt, MatOfPoint, MatOfByte, ...) are Mat subclasses from the
            // OpenCV Java bindings. C++ OpenCV has no separate MatOf* types — they exist to
            // round-trip C# arrays/lists with a known depth/channel layout.
            //
            // Shape notes (OpenCV 5):
            // - fromArray / alloc normally create a true 1D Mat (dims==1, rows==1, cols==N).
            // - You can also wrap an existing Mat (including compatible 2D Nx1 / 1xN) via new MatOf*(mat)
            //   when checkVector(channels, depth) succeeds; otherwise CvException("Incompatible Mat").
            // - Do not assume rows()==N (OpenCV 4 Nx1 habit); use checkVector / toArray length / cols() for 1D.
            //

            // Typical path: fromArray -> true 1D
            MatOfInt moi = new MatOfInt(10, 20, 30, 40);
            Log("moi.dims() = " + moi.dims());
            Log("moi.rows() = " + moi.rows());
            Log("moi.cols() = " + moi.cols());
            Log("moi.checkVector(1, CvType.CV_32S) = " + moi.checkVector(1, CvType.CV_32S));
            Log("moi = " + moi.dump());
            int[] moiArr = moi.toArray();
            Log("moi.toArray() length = " + moiArr.Length);

            // Round-trip
            moi.fromArray(1, 2, 3);
            Log("moi after fromArray = " + moi.dump());

            // Wrap a compatible 2D Mat (Nx1) — still valid MatOfInt, but dims==2
            Mat col2d = new Mat(4, 1, CvType.CV_32SC1);
            col2d.put(0, 0, 5, 6, 7, 8);
            MatOfInt moiFrom2d = new MatOfInt(col2d);
            Log("moiFrom2d.dims() = " + moiFrom2d.dims() + " // wrapped 2D Nx1, not true 1D");
            Log("moiFrom2d.checkVector(1, CvType.CV_32S) = " + moiFrom2d.checkVector(1, CvType.CV_32S));
            Log("moiFrom2d = " + moiFrom2d.dump());

            // Multi-channel typed Mat: MatOfPoint uses CV_32SC2 (x,y per element)
            MatOfPoint mop = new MatOfPoint(new Point(1, 2), new Point(3, 4), new Point(5, 6));
            Log("mop.dims() = " + mop.dims());
            Log("mop.channels() = " + mop.channels());
            Log("mop.checkVector(2, CvType.CV_32S) = " + mop.checkVector(2, CvType.CV_32S));
            Log("mop = " + mop.dump());

            // Same pattern as transposeND / mixChannels demos (MatOfInt as int parameter Mat)
            MatOfInt order = new MatOfInt(0, 2, 1, 3);
            Log("order (transposeND-style) = " + order.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  MatOf* example (OpenCVForUnity / Java-wrapper typed Mats)
            // ---------------------------------------------------------------------------------------
            // MatOf* are Mat subclasses for typed array/list round-trips.
            // fromArray/alloc usually create true 1D; wrapping a Mat can keep 2D (Nx1/1xN) if checkVector passes.
            //

            MatOfInt moi = new MatOfInt(10, 20, 30, 40);
            Log(""moi.dims() = "" + moi.dims());
            Log(""moi.rows() = "" + moi.rows());
            Log(""moi.cols() = "" + moi.cols());
            Log(""moi.checkVector(1, CvType.CV_32S) = "" + moi.checkVector(1, CvType.CV_32S));
            Log(""moi = "" + moi.dump());
            int[] moiArr = moi.toArray();
            Log(""moi.toArray() length = "" + moiArr.Length);

            moi.fromArray(1, 2, 3);
            Log(""moi after fromArray = "" + moi.dump());

            Mat col2d = new Mat(4, 1, CvType.CV_32SC1);
            col2d.put(0, 0, 5, 6, 7, 8);
            MatOfInt moiFrom2d = new MatOfInt(col2d);
            Log(""moiFrom2d.dims() = "" + moiFrom2d.dims() + "" // wrapped 2D Nx1, not true 1D"");
            Log(""moiFrom2d.checkVector(1, CvType.CV_32S) = "" + moiFrom2d.checkVector(1, CvType.CV_32S));
            Log(""moiFrom2d = "" + moiFrom2d.dump());

            MatOfPoint mop = new MatOfPoint(new Point(1, 2), new Point(3, 4), new Point(5, 6));
            Log(""mop.dims() = "" + mop.dims());
            Log(""mop.channels() = "" + mop.channels());
            Log(""mop.checkVector(2, CvType.CV_32S) = "" + mop.checkVector(2, CvType.CV_32S));
            Log(""mop = "" + mop.dump());

            MatOfInt order = new MatOfInt(0, 2, 1, 3);
            Log(""order (transposeND-style) = "" + order.dump());
            ");
        }

        public void OnDumpExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  dump example
            // ---------------------------------------------------------------------------------------
            // Output the elements of the matrix as a string.
            // Native dump() supports dims <= 2 (empty, 0D scalar, 1D, and 2D).
            // Mats with dims > 2 throw CvException — reshape to 2D first.
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // 0D scalar
            Mat mat0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));

            // True 1D
            Mat mat1d = new Mat(new int[] { 3 }, CvType.CV_32F);
            mat1d.put(0, 0, 1f, 3f, 2f);

            // 32F, channels=1, 1x3x4x3 (dims=4)
            Mat mat3 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            mat3.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            // dump
            Log("mat1 = " + mat1);
            Log("mat1.dump() = " + mat1.dump());
            Log("mat2 = " + mat2);
            Log("mat2.dump() = " + mat2.dump());
            Log("mat0d.dims() = " + mat0d.dims());
            Log("mat0d = " + mat0d.dump());
            Log("mat1d.dims() = " + mat1d.dims());
            Log("mat1d = " + mat1d.dump());
            Log("mat3 = " + mat3);
            // dump() requires dims<=2; reshape ND Mats to 2D to inspect contents.
            Log("mat3.reshape(3, new int[] { 3, 4 }).dump() = " + mat3.reshape(3, new int[] { 3, 4 }).dump() + " // dims>2: reshape to 2D before dump()");

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  dump example
            // ---------------------------------------------------------------------------------------
            // Output the elements of the matrix as a string.
            // Native dump() supports dims <= 2 (empty, 0D scalar, 1D, and 2D).
            // Mats with dims > 2 throw CvException — reshape to 2D first.
            //

            // 8U, channels=1, 3x3
            Mat mat1 = new Mat(3, 3, CvType.CV_8UC1, new Scalar(1));

            // 8U, channels=4, 3x3
            Mat mat2 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));

            // 0D scalar
            Mat mat0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));

            // True 1D
            Mat mat1d = new Mat(new int[] { 3 }, CvType.CV_32F);
            mat1d.put(0, 0, 1f, 3f, 2f);

            // 32F, channels=1, 1x3x4x3 (dims=4)
            Mat mat3 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            mat3.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            // dump
            Log(""mat1 = "" + mat1);
            Log(""mat1.dump() = "" + mat1.dump());
            Log(""mat2 = "" + mat2);
            Log(""mat2.dump() = "" + mat2.dump());
            Log(""mat0d.dims() = "" + mat0d.dims());
            Log(""mat0d = "" + mat0d.dump());
            Log(""mat1d.dims() = "" + mat1d.dims());
            Log(""mat1d = "" + mat1d.dump());
            Log(""mat3 = "" + mat3);
            // dump() requires dims<=2; reshape ND Mats to 2D to inspect contents.
            Log(""mat3.reshape(3, new int[] { 3, 4 }).dump() = "" + mat3.reshape(3, new int[] { 3, 4 }).dump() + "" // dims>2: reshape to 2D before dump()"");
            ");
        }

        public void OnCVExceptionHandlingExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  CVException handling example
            // ---------------------------------------------------------------------------------------
            // How to display Native-side OpenCV error logs in the Unity Editor Console.
            // Common causes: mismatched depth/type, incompatible sizes/shapes, invalid arguments.
            // OpenCVDebug.SetDebugMode(true, throwException) controls LogError vs thrown CvException.
            //

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // dump
            Log("m1 = " + m1);
            Log("m1.dump() = " + m1.dump());
            Log("m2 = " + m2);
            Log("m2.dump() = " + m2.dump());

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false);

            Mat m3 = new Mat();
            // Core.divide requires matching element types; CV_32FC1 vs CV_8UC1 triggers a native error.
            Core.divide(m1, m2, m3); // element type is different.
            Log("m3 = " + m3);

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true);
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4); // element type is different.
                Log("m4 = " + m4);
            }
            catch (Exception e)
            {
                Log("CVException: " + e);
            }
            OpenCVDebug.SetDebugMode(false);

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  CVException handling example
            // ---------------------------------------------------------------------------------------
            // How to display Native-side OpenCV error logs in the Unity Editor Console.
            // Common causes: mismatched depth/type, incompatible sizes/shapes, invalid arguments.
            // OpenCVDebug.SetDebugMode(true, throwException) controls LogError vs thrown CvException.
            //

            // 32F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_32FC1);
            m1.put (0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat (3, 3, CvType.CV_8UC1);
            m2.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // dump
            Log(""m1 = "" + m1);
            Log(""m1.dump() = "" + m1.dump ());
            Log(""m2 = "" + m2);
            Log(""m2.dump() = "" + m2.dump ());

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false);

            Mat m3 = new Mat();
            // Core.divide requires matching element types; CV_32FC1 vs CV_8UC1 triggers a native error.
            Core.divide(m1, m2, m3);
            Log(""m3 = "" + m3);

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true);
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4);
                Log(""m4 = "" + m4);
            }
            catch (Exception e)
            {
                Log(""CVException: "" + e);
            }
            OpenCVDebug.SetDebugMode (false);
            ");
        }

        public void OnPropertyExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  property example
            // ---------------------------------------------------------------------------------------
            // List the properties of an OpenCV matrix, including empty / 0D / true 1D / 2D / ND contrasts.
            //

            // 64F, channels=1, 3x4
            Mat mat1 = new Mat(3, 4, CvType.CV_64FC1);

            // number of rows
            Log("mat1.rows() = " + mat1.rows());
            // number of columns
            Log("mat1.cols() = " + mat1.cols());
            // number of dimensions
            Log("mat1.dims() = " + mat1.dims());
            // size
            Log("mat1.size() = " + mat1.size().width + ", " + mat1.size().height);
            // bit depth ID
            Log("mat1.depth() = " + mat1.depth() + "( = " + CvType.CV_64F + ")");
            // number of channels
            Log("mat1.channels() = " + mat1.channels());
            // size of one element
            Log("mat1.elemSize() = " + mat1.elemSize() + "[byte]");
            // size for one channel in one element
            Log("mat1.elemSize1() = " + mat1.elemSize1() + "[byte]");
            // total number of elements
            Log("mat1.total() = " + mat1.total());
            // size of step
            Log("mat1.step1()*elemSize1() = " + mat1.step1() * mat1.elemSize1() + "[byte]");
            // total number of channels within one step
            Log("mat1.step1() = " + mat1.step1());
            // is the data continuous?
            Log("mat1.isContinuous() = " + mat1.isContinuous());
            // is it a submatrix?
            Log("mat1.isSubmatrix() = " + mat1.isSubmatrix());
            // is the data empty?
            Log("mat1.empty() = " + mat1.empty());

            Log("==============================");

            // 32FC, channels=5, 4x5, 3x4 Submatrix
            Mat mat2 = new Mat(4, 5, CvType.CV_32FC(5));
            OpenCVForUnity.CoreModule.Rect roi_rect = new OpenCVForUnity.CoreModule.Rect(0, 0, 3, 4);
            Mat r1 = new Mat(mat2, roi_rect);

            // number of rows
            Log("r1.rows() = " + r1.rows());
            // number of columns
            Log("r1.cols() = " + r1.cols());
            // number of dimensions
            Log("r1.dims() = " + r1.dims());
            // size
            Log("r1.size() = " + r1.size().width + ", " + r1.size().height);
            // bit depth ID
            Log("r1.depth() = " + r1.depth() + "( = " + CvType.CV_32F + ")");
            // number of channels
            Log("r1.channels() = " + r1.channels());
            // size of one element
            Log("r1.elemSize() = " + r1.elemSize() + "[byte]");
            // size for one channel in one element
            Log("r1.elemSize1() = " + r1.elemSize1() + "[byte]");
            // total number of elements
            Log("r1.total() = " + r1.total());
            // size of step
            Log("r1.step1()*elemSize1() = " + r1.step1() * r1.elemSize1() + "[byte]");
            // total number of channels within one step
            Log("r1.step1() = " + r1.step1());
            // is the data continuous?
            Log("r1.isContinuous() = " + r1.isContinuous());
            // is it a submatrix?
            Log("r1.isSubmatrix() = " + r1.isSubmatrix());
            // is the data empty?
            Log("r1.empty() = " + r1.empty());

            Log("==============================");

            // 32S, channles=2, 2x3x3x4x6 (5 dimensional array)
            int[] sizes = new int[] { 2, 3, 3, 4, 6 };
            Mat mat3 = new Mat(sizes, CvType.CV_32SC2);

            // number of rows
            Log("mat3.rows() = " + mat3.rows());
            // number of columns
            Log("mat3.cols() = " + mat3.cols());
            // number of dimensions
            Log("mat3.dims() = " + mat3.dims());
            // size
            string size = "";
            for (int i = 0; i < mat3.dims(); ++i)
            {
                size += mat3.size(i) + ", ";
            }
            Log("mat3.size() = " + size);
            // bit depth ID
            Log("mat3.depth() = " + mat3.depth() + "( = " + CvType.CV_32S + ")");
            // number of channels
            Log("mat3.channels() = " + mat3.channels());
            // size of one element
            Log("mat3.elemSize() = " + mat3.elemSize() + "[byte]");
            // size for one channel in one element
            Log("mat3.elemSize1() = " + mat3.elemSize1() + "[byte]");
            // total number of elements
            Log("mat3.total() = " + mat3.total());
            // size of step
            string step = "";
            for (int i = 0; i < mat3.dims(); ++i)
            {
                step += mat3.step1(i) * mat3.elemSize1() + ", ";
            }
            Log("mat3.step1()*elemSize1() = " + step + "[byte]");
            // total number of channels within one step
            Log("mat3.step1() = " + mat3.step1());
            // is the data continuous?
            Log("mat3.isContinuous() = " + mat3.isContinuous());
            // is it a submatrix?
            Log("mat3.isSubmatrix() = " + mat3.isSubmatrix());
            // is the data empty?
            Log("mat3.empty() = " + mat3.empty());

            Log("==============================");

            // 0D scalar vs empty Mat (both can report dims==0)
            Mat emptyMat = new Mat();
            Mat scalar0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));
            Log("emptyMat.dims() = " + emptyMat.dims());
            Log("emptyMat.empty() = " + emptyMat.empty());
            Log("emptyMat.total() = " + emptyMat.total());
            Log("emptyMat.rows() = " + emptyMat.rows());
            Log("emptyMat.cols() = " + emptyMat.cols());
            Log("scalar0d.dims() = " + scalar0d.dims());
            Log("scalar0d.empty() = " + scalar0d.empty());
            Log("scalar0d.total() = " + scalar0d.total());
            Log("scalar0d.rows() = " + scalar0d.rows());
            Log("scalar0d.cols() = " + scalar0d.cols());
            Log("scalar0d = " + scalar0d.dump());

            Log("==============================");

            // True 1D vs 2D Nx1 (same element count, different dims / step1)
            Mat vec1d = new Mat(new int[] { 4 }, CvType.CV_32FC1, new Scalar(0));
            Mat col2d = new Mat(4, 1, CvType.CV_32FC1, new Scalar(0));
            Log("vec1d.dims() = " + vec1d.dims());
            Log("vec1d.rows() = " + vec1d.rows());
            Log("vec1d.cols() = " + vec1d.cols());
            Log("vec1d.size(0) = " + vec1d.size(0));
            Log("vec1d.total() = " + vec1d.total());
            Log("vec1d.step1() = " + vec1d.step1());
            Log("col2d.dims() = " + col2d.dims());
            Log("col2d.rows() = " + col2d.rows());
            Log("col2d.cols() = " + col2d.cols());
            Log("col2d.total() = " + col2d.total());
            Log("col2d.step1() = " + col2d.step1());
            Log("vec1d.size() = " + vec1d.size().width + ", " + vec1d.size().height + " // Size looks like (N,1) but dims==1");

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  property example
            // ---------------------------------------------------------------------------------------
            // List the properties of an OpenCV matrix, including empty / 0D / true 1D / 2D / ND contrasts.
            //

            // 64F, channels=1, 3x4
            Mat mat1 = new Mat (3, 4, CvType.CV_64FC1);

            // number of rows
            Log(""mat1.rows() = "" + mat1.rows ());
            // number of columns
            Log(""mat1.cols() = "" + mat1.cols ());
            // number of dimensions
            Log(""mat1.dims() = "" + mat1.dims ());
            // size
            Log(""mat1.size() = "" + mat1.size ().width + "", "" + mat1.size ().height);
            // bit depth ID
            Log(""mat1.depth() = "" + mat1.depth () + ""( = "" + CvType.CV_64F + "")"");
            // number of channels
            Log(""mat1.channels() = "" + mat1.channels ());
            // size of one element
            Log(""mat1.elemSize() = "" + mat1.elemSize () + ""[byte]"");
            // size for one channel in one element
            Log(""mat1.elemSize1() = "" + mat1.elemSize1 () + ""[byte]"");
            // total number of elements
            Log(""mat1.total() = "" + mat1.total ());
            // size of step
            Log(""mat1.step1()*elemSize1() = "" + mat1.step1 () * mat1.elemSize1 () + ""[byte]"");
            // total number of channels within one step
            Log(""mat1.step1() = "" + mat1.step1 ());
            // is the data continuous?
            Log(""mat1.isContinuous() = "" + mat1.isContinuous ());
            // is it a submatrix?
            Log(""mat1.isSubmatrix() = "" + mat1.isSubmatrix ());
            // is the data empty?
            Log(""mat1.empty() = "" + mat1.empty ());

            Log(""=============================="");


            // 32FC, channels=5, 4x5, 3x4 Submatrix
            Mat mat2 = new Mat (4, 5, CvType.CV_32FC (5));
            OpenCVForUnity.CoreModule.Rect roi_rect = new OpenCVForUnity.CoreModule.Rect (0, 0, 3, 4);
            Mat r1 = new Mat (mat2, roi_rect);

            // number of rows
            Log(""r1.rows() = "" + r1.rows ());
            // number of columns
            Log(""r1.cols() = "" + r1.cols ());
            // number of dimensions
            Log(""r1.dims() = "" + r1.dims ());
            // size
            Log(""r1.size() = "" + r1.size ().width + "", "" + r1.size ().height);
            // bit depth ID
            Log(""r1.depth() = "" + r1.depth () + ""( = "" + CvType.CV_32F + "")"");
            // number of channels
            Log(""r1.channels() = "" + r1.channels ());
            // size of one element
            Log(""r1.elemSize() = "" + r1.elemSize () + ""[byte]"");
            // size for one channel in one element
            Log(""r1.elemSize1() = "" + r1.elemSize1 () + ""[byte]"");
            // total number of elements
            Log(""r1.total() = "" + r1.total ());
            // size of step
            Log(""r1.step1()*elemSize1() = "" + r1.step1 () * r1.elemSize1 () + ""[byte]"");
            // total number of channels within one step
            Log(""r1.step1() = "" + r1.step1 ());
            // is the data continuous?
            Log(""r1.isContinuous() = "" + r1.isContinuous ());
            // is it a submatrix?
            Log(""r1.isSubmatrix() = "" + r1.isSubmatrix ());
            // is the data empty?
            Log(""r1.empty() = "" + r1.empty ());

            Log(""=============================="");


            // 32S, channels=2, 2x3x3x4x6 (5 dimensional array)
            int[] sizes = new int[]{ 2, 3, 3, 4, 6 };
            Mat mat3 = new Mat (sizes, CvType.CV_32SC2);

            // number of rows
            Log(""mat3.rows() = "" + mat3.rows ());
            // number of columns
            Log(""mat3.cols() = "" + mat3.cols ());
            // number of dimensions
            Log(""mat3.dims() = "" + mat3.dims ());
            // size
            string size = """";
            for (int i = 0; i < mat3.dims (); ++i) {
                size += mat3.size (i) + "", "";
            }
            Log(""mat3.size() = "" + size);
            // bit depth ID
            Log(""mat3.depth() = "" + mat3.depth () + ""( = "" + CvType.CV_32S + "")"");
            // number of channels
            Log(""mat3.channels() = "" + mat3.channels ());
            // size of one element
            Log(""mat3.elemSize() = "" + mat3.elemSize () + ""[byte]"");
            // size for one channel in one element
            Log(""mat3.elemSize1() = "" + mat3.elemSize1 () + ""[byte]"");
            // total number of elements
            Log(""mat3.total() = "" + mat3.total ());
            // size of step
            string step = """";
            for (int i = 0; i < mat3.dims (); ++i) {
                step += mat3.step1 (i) * mat3.elemSize1 () + "", "";
            }
            Log(""mat3.step1()*elemSize1() = "" + step + ""[byte]"");
            // total number of channels within one step
            Log(""mat3.step1() = "" + mat3.step1 ());
            // is the data continuous?
            Log(""mat3.isContinuous() = "" + mat3.isContinuous ());
            // is it a submatrix?
            Log(""mat3.isSubmatrix() = "" + mat3.isSubmatrix ());
            // is the data empty?
            Log(""mat3.empty() = "" + mat3.empty ());

            Log(""=============================="");

            // 0D scalar vs empty Mat (both can report dims==0)
            Mat emptyMat = new Mat();
            Mat scalar0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));
            Log(""emptyMat.dims() = "" + emptyMat.dims());
            Log(""emptyMat.empty() = "" + emptyMat.empty());
            Log(""emptyMat.total() = "" + emptyMat.total());
            Log(""emptyMat.rows() = "" + emptyMat.rows());
            Log(""emptyMat.cols() = "" + emptyMat.cols());
            Log(""scalar0d.dims() = "" + scalar0d.dims());
            Log(""scalar0d.empty() = "" + scalar0d.empty());
            Log(""scalar0d.total() = "" + scalar0d.total());
            Log(""scalar0d.rows() = "" + scalar0d.rows());
            Log(""scalar0d.cols() = "" + scalar0d.cols());
            Log(""scalar0d = "" + scalar0d.dump());

            Log(""=============================="");

            // True 1D vs 2D Nx1 (same element count, different dims / step1)
            Mat vec1d = new Mat(new int[] { 4 }, CvType.CV_32FC1, new Scalar(0));
            Mat col2d = new Mat(4, 1, CvType.CV_32FC1, new Scalar(0));
            Log(""vec1d.dims() = "" + vec1d.dims());
            Log(""vec1d.rows() = "" + vec1d.rows());
            Log(""vec1d.cols() = "" + vec1d.cols());
            Log(""vec1d.size(0) = "" + vec1d.size(0));
            Log(""vec1d.total() = "" + vec1d.total());
            Log(""vec1d.step1() = "" + vec1d.step1());
            Log(""col2d.dims() = "" + col2d.dims());
            Log(""col2d.rows() = "" + col2d.rows());
            Log(""col2d.cols() = "" + col2d.cols());
            Log(""col2d.total() = "" + col2d.total());
            Log(""col2d.step1() = "" + col2d.step1());
            Log(""vec1d.size() = "" + vec1d.size().width + "", "" + vec1d.size().height + "" // Size looks like (N,1) but dims==1"");
            ");
        }

        public void OnFourArithmeticOperationExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  four arithmetic operation example
            // ---------------------------------------------------------------------------------------
            // Performs four arithmetic methods on matrices.
            // This demo uses 2D Mats; element-wise Core ops also work on matching shapes (use total() for element count on any rank).
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

            Log("m1 = " + m1.dump());
            Log("m2 = " + m2.dump());
            Log("s = " + s);
            Log("alpha = " + alpha);

            Mat mat_dst = new Mat();

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            Core.add(m1, m2, mat_dst);
            Log("m1+m2 = " + mat_dst.dump());
            Core.add(m1, s, mat_dst);
            Log("m1+s = " + mat_dst.dump());

            Core.subtract(m1, m2, mat_dst);
            Log("m1-m2 = " + mat_dst.dump());
            Core.subtract(m1, s, mat_dst);
            Log("m1-s = " + mat_dst.dump());

            Core.multiply(m1, Scalar.all(-1), mat_dst);
            Log("-m1 = " + mat_dst.dump());

            // Scaling: A*alpha A/alpha
            Core.multiply(m1, Scalar.all(3), mat_dst);
            Log("m1*alpha = " + mat_dst.dump());
            Core.divide(m1, Scalar.all(3), mat_dst);
            Log("m1/alpha = " + mat_dst.dump());

            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            Log("m1.mul(m2) = " + (m1.mul(m2)).dump());

            Core.divide(m1, m2, mat_dst);
            Log("m1/m2 = " + mat_dst.dump());

            Core.divide(new Mat(m1.size(), m1.type(), Scalar.all(3)), m1, mat_dst);
            Log("alpha/m2 = " + mat_dst.dump());

            // Matrix multiplication: A*B
            Core.gemm(m1, m2, 1, new Mat(), 0, mat_dst);
            Log("m1*m2 = " + mat_dst.dump());

            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            Core.bitwise_and(m1, m2, mat_dst);
            Log("m1&m2 = " + mat_dst.dump());

            Core.bitwise_or(m1, m2, mat_dst);
            Log("m1|m2 = " + mat_dst.dump());

            Core.bitwise_xor(m1, m2, mat_dst);
            Log("m1^m2 = " + mat_dst.dump());

            Core.bitwise_not(m1, mat_dst);
            Log("~m1 = " + mat_dst.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  four arithmetic operation example
            // ---------------------------------------------------------------------------------------
            // Performs four arithmetic methods on matrices.
            // This demo uses 2D Mats; element-wise Core ops also work on matching shapes (use total() for element count on any rank).
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

            Log(""m1 = "" + m1.dump());
            Log(""m2 = "" + m2.dump());
            Log(""s = "" + s);
            Log(""alpha = "" + alpha);

            Mat mat_dst = new Mat();

            // Addition, subtraction, negation: A+B, A-B, A+s, A-s, s+A, s-A, -A
            Core.add(m1, m2, mat_dst);
            Log(""m1+m2 = "" + mat_dst.dump());
            Core.add(m1, s, mat_dst);
            Log(""m1+s = "" + mat_dst.dump());

            Core.subtract(m1, m2, mat_dst);
            Log(""m1-m2 = "" + mat_dst.dump());
            Core.subtract(m1, s, mat_dst);
            Log(""m1-s = "" + mat_dst.dump());

            Core.multiply(m1, Scalar.all(-1), mat_dst);
            Log(""-m1 = "" + mat_dst.dump());


            // Scaling: A*alpha A/alpha
            Core.multiply(m1, Scalar.all(3), mat_dst);
            Log(""m1*alpha = "" + mat_dst.dump());
            Core.divide(m1, Scalar.all(3), mat_dst);
            Log(""m1/alpha = "" + mat_dst.dump());


            // Per-element multiplication and division: A.mul(B), A/B, alpha/A
            Log(""m1.mul(m2) = "" + (m1.mul(m2)).dump());

            Core.divide(m1, m2, mat_dst);
            Log(""m1/m2 = "" + mat_dst.dump());

            Core.divide(new Mat(m1.size(), m1.type(), Scalar.all(3)), m1, mat_dst);
            Log(""alpha/m2 = "" + mat_dst.dump());


            // Matrix multiplication: A*B
            Core.gemm(m1, m2, 1, new Mat(), 0, mat_dst);
            Log(""m1*m2 = "" + mat_dst.dump());


            // Bitwise logical operations: A logicop B, A logicop s, s logicop A, ~A, where logicop is one of :  &, |, ^.
            Core.bitwise_and(m1, m2, mat_dst);
            Log(""m1&m2 = "" + mat_dst.dump());

            Core.bitwise_or(m1, m2, mat_dst);
            Log(""m1|m2 = "" + mat_dst.dump());

            Core.bitwise_xor(m1, m2, mat_dst);
            Log(""m1^m2 = "" + mat_dst.dump());

            Core.bitwise_not(m1, mat_dst);
            Log(""~m1 = "" + mat_dst.dump());
            ");
        }

        public void OnConvertToExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  convertTo example
            // ---------------------------------------------------------------------------------------
            // Mat.convertTo changes depth (and optional alpha/beta scale) while keeping the same shape
            // (including 0D/1D/ND). Channel count stays the same unless you change type to another Cn.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Log("m1 = " + m1.dump());

            // 64F -> 8U (dst mat, type)
            Mat m2 = new Mat();
            m1.convertTo(m2, CvType.CV_8U);
            Log("m2 = " + m2.dump());

            // 64F -> 8U (dst mat, type, scale factor, added to the scaled value)
            Mat m3 = new Mat();
            m1.convertTo(m3, CvType.CV_8U, 2, 10);
            Log("m3 = " + m3.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  convertTo example
            // ---------------------------------------------------------------------------------------
            // Mat.convertTo changes depth (and optional alpha/beta scale) while keeping the same shape
            // (including 0D/1D/ND). Channel count stays the same unless you change type to another Cn.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3);
            Log(""m1 = "" + m1.dump());

            // 64F -> 8U (dst mat, type)
            Mat m2 = new Mat ();
            m1.convertTo (m2, CvType.CV_8U);
            Log(""m2 = "" + m2.dump());

            // 64F -> 8U (dst mat, type, scale factor, added to the scaled value)
            Mat m3 = new Mat ();
            m1.convertTo (m3, CvType.CV_8U, 2, 10);
            Log(""m3 = "" + m3.dump());
            ");
        }

        public void OnReshapeExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  reshape example
            // ---------------------------------------------------------------------------------------
            // Changes the shape and/or the number of channels of a  matrix without copying the data.
            // The method makes a new matrix header for this elements.The new matrix may have a different size and / or different number of channels.Any combination is possible if:
            // - No extra elements are included into the new matrix and no elements are excluded.Consequently, the product rows* cols*channels() must stay the same after the transformation.
            // - No data is copied.That is, this is an O(1) operation.Consequently, if you change the number of rows, or the operation changes the indices of elements row in some other way, the matrix must be continuous.See "Mat.isContinuous".
            //

            // 64F, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Log("m1 = " + m1.dump());
            Log("m1.channels() = " + m1.channels());

            // channels=1, 3x4 -> channels=2, 3x2
            Mat m2 = m1.reshape(2);
            Log("m2 = " + m2.dump());
            Log("m2.channels() = " + m2.channels());

            // channels=1, 3x4 -> channels=1, 2x6
            Mat m3 = m1.reshape(1, 2);
            Log("m3 = " + m3.dump());
            Log("m3.channels() = " + m3.channels());

            // 2D -> 4D
            Mat src = new Mat(6, 5, CvType.CV_8UC3, new Scalar(0));
            Mat m4 = src.reshape(1, new int[] { 1, src.channels() * src.cols(), 1, src.rows() });
            Log("m4.dims() = " + m4.dims());
            string size = "";
            for (int i = 0; i < m4.dims(); ++i)
            {
                size += m4.size(i) + ", ";
            }
            Log("m4.size() = " + size);
            Log("m4.channels() = " + m4.channels());

            // 3D -> 2D
            src = new Mat(new int[] { 4, 6, 7 }, CvType.CV_8UC3, new Scalar(0));
            Mat m5 = src.reshape(1, new int[] { src.channels() * src.size(2), src.size(0) * src.size(1) });
            Log("m5 = " + m5);
            Log("m5.channels() = " + m5.channels());

            // 1D <-> 2D (reshape changes dims; keep total()*channels the same)
            Mat v1d = new Mat(new int[] { 6 }, CvType.CV_8UC1, new Scalar(0));
            v1d.put(0, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
            Mat vAs2d = v1d.reshape(1, new int[] { 2, 3 });
            Mat backTo1d = vAs2d.reshape(1, new int[] { 6 });
            Log("vAs2d.dims() = " + vAs2d.dims());
            Log("vAs2d = " + vAs2d.dump());
            Log("backTo1d.dims() = " + backTo1d.dims());
            Log("backTo1d = " + backTo1d.dump());

            // 0D scalar -> 1x1 2D (handy when an API expects a 2D header)
            Mat s0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));
            Mat s11 = s0d.reshape(1, new int[] { 1, 1 });
            Log("s11.dims() = " + s11.dims());
            Log("s11 = " + s11.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  reshape example
            // ---------------------------------------------------------------------------------------
            // Changes the shape and/or the number of channels of a  matrix without copying the data.
            // The method makes a new matrix header for this elements.The new matrix may have a different size and / or different number of channels.Any combination is possible if:
            // - No extra elements are included into the new matrix and no elements are excluded.Consequently, the product rows* cols*channels() must stay the same after the transformation.
            // - No data is copied.That is, this is an O(1) operation.Consequently, if you change the number of rows, or the operation changes the indices of elements row in some other way, the matrix must be continuous.See ""Mat.isContinuous"".
            //

            // 64F, channels=1, 3x4
            Mat m1 = new Mat (3, 4, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12);
            Log(""m1 = "" + m1.dump());
            Log(""m1.channels() = "" + m1.channels());

            // channels=1, 3x4 -> channels=2, 3x2
            Mat m2 = m1.reshape (2);
            Log(""m2 = "" + m2.dump ());
            Log(""m2.channels() = "" + m2.channels ());

            // channels=1, 3x4 -> channels=1, 2x6
            Mat m3 = m1.reshape (1, 2);
            Log(""m3 = "" + m3.dump ());
            Log(""m3.channels() = "" + m3.channels ());

            // 2D -> 4D
            Mat src = new Mat (6, 5, CvType.CV_8UC3, new Scalar (0));
            Mat m4 = src.reshape (1, new int[]{ 1, src.channels () * src.cols (), 1, src.rows () });
            Log(""m4.dims() = "" + m4.dims ());
            string size = """";
            for (int i = 0; i < m4.dims (); ++i) {
                size += m4.size (i) + "", "";
            }
            Log(""m4.size() = "" + size);
            Log(""m4.channels() = "" + m4.channels ());

            // 3D -> 2D
            src = new Mat (new int[]{ 4, 6, 7 }, CvType.CV_8UC3, new Scalar (0));
            Mat m5 = src.reshape (1, new int[]{ src.channels () * src.size (2), src.size (0) * src.size (1) });
            Log(""m5 = "" + m5);
            Log(""m5.channels() = "" + m5.channels ());

            // 1D <-> 2D
            Mat v1d = new Mat(new int[] { 6 }, CvType.CV_8UC1, new Scalar(0));
            v1d.put(0, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
            Mat vAs2d = v1d.reshape(1, new int[] { 2, 3 }); // dims 1 -> 2
            Mat backTo1d = vAs2d.reshape(1, new int[] { 6 }); // dims 2 -> 1
            Log(""vAs2d.dims() = "" + vAs2d.dims());
            Log(""vAs2d = "" + vAs2d.dump());
            Log(""backTo1d.dims() = "" + backTo1d.dims());
            Log(""backTo1d = "" + backTo1d.dump());

            // 0D -> 1x1 2D
            Mat s0d = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(7));
            Mat s11 = s0d.reshape(1, new int[] { 1, 1 });
            Log(""s11.dims() = "" + s11.dims());
            Log(""s11 = "" + s11.dump());
            ");
        }

        public void OnTransposeExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  transpose example
            // ---------------------------------------------------------------------------------------
            // The Core.transpose function can be used for various image processing tasks such as rotating images by 90 degrees and changing the shape of matrices by swapping rows and columns of Mat.
            // - The Core.transpose function is a function that performs a transposition operation on a two-dimensional matrix.
            // - The Core.transposeND function is a function that performs a transposition operation on a tensor of arbitrary dimensions.For example, it can be used to swap specific dimensions of a 3D tensor(such as video data).
            //

            // Transposes a matrix.
            // 8U, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_8UC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Log("m1 = " + m1.dump());

            // [3x4] -> [4x3]
            Mat m1_t = new Mat();
            Core.transpose(m1, m1_t);
            Log("Core.transpose(m1, m1_t) = " + m1_t.dump());

            // Transpose for n-dimensional matrices.
            // 32F, channels=1, 1x3x4x3
            Mat m2 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            m2.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            string m2_size = "";
            for (int i = 0; i < m2.dims(); ++i)
            {
                m2_size += m2.size(i) + ", ";
            }
            Log("m2 = " + m2.reshape(3, new int[] { 3, 4 }).dump());
            Log("m2 size[] = " + m2_size);

            // [1x3x4x3] -> [1x4x3x3]
            Mat m2_t = new Mat();
            MatOfInt order = new MatOfInt(0, 2, 1, 3); // See MatOf* example: typed 1D int Mat for permute order
            Core.transposeND(m2, order, m2_t);
            string m2_t_size = "";
            for (int i = 0; i < m2_t.dims(); ++i)
            {
                m2_t_size += m2_t.size(i) + ", ";
            }
            Log("Core.transposeND(m2, m2_t) = " + m2_t.reshape(3, new int[] { 4, 3 }).dump());
            Log("m2_t size[] = " + m2_t_size);

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  transpose example
            // ---------------------------------------------------------------------------------------
            // The Core.transpose function can be used for various image processing tasks such as rotating images by 90 degrees and changing the shape of matrices by swapping rows and columns of Mat.
            // - The Core.transpose function is a function that performs a transposition operation on a two-dimensional matrix.
            // - The Core.transposeND function is a function that performs a transposition operation on a tensor of arbitrary dimensions.For example, it can be used to swap specific dimensions of a 3D tensor(such as video data).
            //

            // Transposes a matrix.
            // 8U, channels=1, 3x4
            Mat m1 = new Mat(3, 4, CvType.CV_8UC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Log(""m1 = "" + m1.dump());

            // [3x4] -> [4x3]
            Mat m1_t = new Mat();
            Core.transpose(m1, m1_t);
            Log(""Core.transpose(m1, m1_t) = "" + m1_t.dump());

            // Transpose for n-dimensional matrices.
            // 32F, channels=1, 1x3x4x3
            Mat m2 = new Mat(new int[] { 1, 3, 4, 3 }, CvType.CV_32FC1);
            m2.put(new int[] { 0, 0, 0, 0 }, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            string m2_size = "";
            for (int i = 0; i < m2.dims(); ++i)
            {
                m2_size += m2.size(i) + "", "";
            }
            Log(""m2 = "" + m2.reshape(3, new int[] { 3, 4 }).dump());
            Log(""m2 size[] = "" + m2_size);

            // [1x3x4x3] -> [1x4x3x3]
            Mat m2_t = new Mat();
            MatOfInt order = new MatOfInt(0, 2, 1, 3); // See MatOf* example: typed 1D int Mat for permute order
            Core.transposeND(m2, order, m2_t);
            string m2_t_size = "";
            for (int i = 0; i < m2_t.dims(); ++i)
            {
                m2_t_size += m2_t.size(i) + "", "";
            }
            Log(""Core.transposeND(m2, m2_t) = "" + m2_t.reshape(3, new int[] { 4, 3 }).dump());
            Log(""m2_t size[] = "" + m2_t_size);
            ");
        }

        public void OnRangeExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  range example
            // ---------------------------------------------------------------------------------------
            // Mat.rowRange and Mat.colRange efficiently extract submatrices from a Mat by creating new Mat headers that point to specified row or column ranges of the original data, without copying the underlying data.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log("m1 = " + m1.dump());

            // all rows
            Log("m1.rowRange(Range.all()) = " + m1.rowRange(Range.all()).dump());

            // rowRange(0,2)
            Log("m1.rowRange(new Range(0,2)) = " + m1.rowRange(new Range(0, 2)).dump());

            // row(0)
            Log("m1.row(0) = " + m1.row(0).dump());

            // all cols
            Log("m1.colRange(Range.all()) = " + m1.colRange(Range.all()).dump());

            // colRange(0,2)
            Log("m1.colRange(new Range(0,2)) = " + m1.colRange(new Range(0, 2)).dump());

            // col(0)
            Log("m1.col(0) = " + m1.col(0).dump());

            // True 1D: prefer colRange / submat(Range[]); rowRange is a 2D-oriented API.
            Mat v = new Mat(new int[] { 5 }, CvType.CV_8UC1);
            v.put(0, 0, new byte[] { 10, 20, 30, 40, 50 });
            Log("v = " + v.dump());
            Log("v.colRange(1,4) = " + v.colRange(1, 4).dump());
            Log("v.submat(new Range[] { new Range(2, 5) }) = " + v.submat(new Range[] { new Range(2, 5) }).dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  range example
            // ---------------------------------------------------------------------------------------
            // Mat.rowRange and Mat.colRange efficiently extract submatrices from a Mat by creating new Mat headers that point to specified row or column ranges of the original data, without copying the underlying data.
            //

            // 64F, channels=1, 3x3
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log(""m1 = "" + m1.dump());

            // all rows
            Log(""m1.rowRange(Range.all()) = "" + m1.rowRange(Range.all()).dump());

            // rowRange(0,2)
            Log(""m1.rowRange(new Range(0,2)) = "" + m1.rowRange(new Range(0,2)).dump());

            // row(0)
            Log(""m1.row(0) = "" + m1.row(0).dump());

            // all cols
            Log(""m1.colRange(Range.all()) = "" + m1.colRange(Range.all()).dump());

            // colRange(0,2)
            Log(""m1.colRange(new Range(0,2)) = "" + m1.colRange(new Range(0,2)).dump());

            // col(0)
            Log(""m1.col(0) = "" + m1.col(0).dump());

            // True 1D: use colRange / submat(Range[])
            Mat v = new Mat(new int[] { 5 }, CvType.CV_8UC1);
            v.put(0, 0, new byte[] { 10, 20, 30, 40, 50 });
            Log(""v = "" + v.dump());
            Log(""v.colRange(1,4) = "" + v.colRange(1, 4).dump());
            Log(""v.submat(new Range[] { new Range(2, 5) }) = "" + v.submat(new Range[] { new Range(2, 5) }).dump());
            ");
        }

        public void OnSubmatrixExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  submatrix (ROI) example
            // ---------------------------------------------------------------------------------------
            // A submatrix (Region of Interest, ROI) is a region cut out of an image or matrix. OpenCV allows you to create a submatrix that manipulates only that region without copying the original data.
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log("m1 = " + m1.dump());

            // Submatrix (ROI) shares the parent Mat's memory; modifying m2 also changes m1.
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(0, 0, 2, 2));
            Log("m2 = " + m2.dump());
            Log("m2.submat() = " + m2.submat(0, 2, 0, 2).dump());

            // find the parent matrix size of the submatrix (ROI) m2 and its position in it
            Size wholeSize = new Size();
            Point ofs = new Point();
            m2.locateROI(wholeSize, ofs);
            Log("wholeSize = " + wholeSize.width + "x" + wholeSize.height);
            Log("offset = " + ofs.x + ", " + ofs.y);

            // expand the range of submatrix (ROI)
            m2.adjustROI(0, 1, 0, 1);
            Log("m2.rows() = " + m2.rows());
            Log("m2.cols() = " + m2.cols());
            Log("m2 = " + m2.dump());

            // True 1D submatrix via colRange / submat(Range[]) — remains continuous
            Mat v = new Mat(new int[] { 5 }, CvType.CV_8UC1);
            v.put(0, 0, new byte[] { 10, 20, 30, 40, 50 });
            Mat vSlice = v.colRange(1, 4);
            Log("v = " + v.dump());
            Log("vSlice.dims() = " + vSlice.dims());
            Log("vSlice.isSubmatrix() = " + vSlice.isSubmatrix());
            Log("vSlice.isContinuous() = " + vSlice.isContinuous());
            Log("vSlice = " + vSlice.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  submatrix (ROI) example
            // ---------------------------------------------------------------------------------------
            // A submatrix (Region of Interest, ROI) is a region cut out of an image or matrix. OpenCV allows you to create a submatrix that manipulates only that region without copying the original data.
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Log(""m1 = "" + m1.dump ());

            // Submatrix (ROI) shares the parent Mat's memory; modifying m2 also changes m1.
            Mat m2 = new Mat (m1, new OpenCVForUnity.CoreModule.Rect(0,0,2,2));
            Log(""m2 = "" + m2.dump());
            Log(""m2.submat() = "" + m2.submat(0,2,0,2).dump());

            // find the parent matrix size of the submatrix (ROI) m2 and its position in it
            Size wholeSize = new Size ();
            Point ofs = new Point ();
            m2.locateROI (wholeSize, ofs);
            Log(""wholeSize = "" + wholeSize.width + ""x"" + wholeSize.height);
            Log(""offset = "" + ofs.x + "", "" + ofs.y);

            // expand the range of submatrix (ROI)
            m2.adjustROI(0, 1, 0, 1);
            Log(""m2.rows() = "" + m2.rows());
            Log(""m2.cols() = "" + m2.cols());
            Log(""m2 = "" + m2.dump());

            // True 1D slice (stays continuous)
            Mat v = new Mat(new int[] { 5 }, CvType.CV_8UC1);
            v.put(0, 0, new byte[] { 10, 20, 30, 40, 50 });
            Mat vSlice = v.colRange(1, 4);
            // or: v.submat(new Range[] { new Range(1, 4) });
            Log(""v = "" + v.dump());
            Log(""vSlice.dims() = "" + vSlice.dims());
            Log(""vSlice.isSubmatrix() = "" + vSlice.isSubmatrix());
            Log(""vSlice.isContinuous() = "" + vSlice.isContinuous());
            Log(""vSlice = "" + vSlice.dump());
            ");
        }

        public void OnShallowCopyAndDeepCopyExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  shallow copy and deep copy example
            // ---------------------------------------------------------------------------------------
            // When working with image and matrix data in OpenCVForUnity, the concepts of shallow copy and deep copy are important. These two methods differ in how they duplicate data, and can significantly affect the behavior of your program.
            // - Shallow copy: Creates a new Mat object that references the same memory region as the original data.
            // - Deep copy: Creates a new Mat object by copying the data into a new memory region, independent of the original data.
            //

            // 3x3 matrix
            Mat mat1 = new Mat(3, 3, CvType.CV_64FC1);
            mat1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // shallow copy — assignment shares the same native buffer (not a data copy).
            Mat mat_shallow = mat1;

            // deep copy (clone, copyTo) — independent buffers; changes to mat1 do not affect these.
            Mat mat_deep1 = mat1.clone();
            Mat mat_deep2 = new Mat();
            mat1.copyTo(mat_deep2);

            Log("mat1 = " + mat1.dump());
            Log("mat_shallow = " + mat_shallow.dump());
            Log("mat_deep1 = " + mat_deep1.dump());
            Log("mat_deep2 = " + mat_deep2.dump());

            // rewrite (0, 0) element of matrix mat1
            mat1.put(0, 0, 100);

            Log("mat1 = " + mat1.dump());
            Log("mat_shallow = " + mat_shallow.dump());
            Log("mat_deep1 = " + mat_deep1.dump());
            Log("mat_deep2 = " + mat_deep2.dump());

            Log("mat1.Equals(mat_shallow) = " + mat1.Equals(mat_shallow));
            Log("mat1.Equals(mat_deep1) = " + mat1.Equals(mat_deep1));
            Log("mat1.Equals(mat_deep2) = " + mat1.Equals(mat_deep2));

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  shallow copy and deep copy example
            // ---------------------------------------------------------------------------------------
            // When working with image and matrix data in OpenCVForUnity, the concepts of shallow copy and deep copy are important. These two methods differ in how they duplicate data, and can significantly affect the behavior of your program.
            // - Shallow copy: Creates a new Mat object that references the same memory region as the original data.
            // - Deep copy: Creates a new Mat object by copying the data into a new memory region, independent of the original data.
            //

            // 3x3 matrix
            Mat mat1 = new Mat (3, 3, CvType.CV_64FC1);
            mat1.put (0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);

            // shallow copy — assignment shares the same native buffer (not a data copy).
            Mat mat_shallow = mat1;

            // deep copy (clone, copyTo) — independent buffers; changes to mat1 do not affect these.
            Mat mat_deep1 = mat1.clone();
            Mat mat_deep2 = new Mat();
            mat1.copyTo (mat_deep2);

            Log(""mat1 = "" + mat1.dump());
            Log(""mat_shallow = "" + mat_shallow.dump());
            Log(""mat_deep1 = "" + mat_deep1.dump());
            Log(""mat_deep2 = "" + mat_deep2.dump());

            // rewrite (0, 0) element of matrix mat1
            mat1.put(0, 0, 100);

            Log(""mat1 = "" + mat1.dump());
            Log(""mat_shallow = "" + mat_shallow.dump());
            Log(""mat_deep1 = "" + mat_deep1.dump());
            Log(""mat_deep2 = "" + mat_deep2.dump());

            Log(""mat1.Equals(mat_shallow) = "" + mat1.Equals(mat_shallow));
            Log(""mat1.Equals(mat_deep1) = "" + mat1.Equals(mat_deep1));
            Log(""mat1.Equals(mat_deep2) = "" + mat1.Equals(mat_deep2));
            ");
        }

        public void OnMergeExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  merge example
            // ---------------------------------------------------------------------------------------
            // Core.merge stacks single-channel Mats into one multi-channel Mat.
            // - Inputs must share the same shape and depth (2D: same rows/cols; 1D: same length / total()).
            // - Output channels() == number of input Mats.
            //

            // 2x2 matrices (classic 2D merge)
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

            Mat mat_merged = new Mat();
            Core.merge(mv, mat_merged);
            Log("mat_merged = " + mat_merged.dump());
            Log("mat_merged.channels() = " + mat_merged.channels());

            // 1D merge: match length with total()/cols(), not "image rows x cols"
            Mat a1d = new Mat(new int[] { 3 }, CvType.CV_64FC1);
            a1d.put(0, 0, 1, 2, 3);
            Mat b1d = new Mat(new int[] { 3 }, CvType.CV_64FC1);
            b1d.put(0, 0, 4, 5, 6);
            List<Mat> mv1d = new List<Mat>();
            mv1d.Add(a1d);
            mv1d.Add(b1d);
            Mat merged1d = new Mat();
            Core.merge(mv1d, merged1d);
            Log("merged1d.dims() = " + merged1d.dims());
            Log("merged1d.channels() = " + merged1d.channels());
            Log("merged1d = " + merged1d.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  merge example
            // ---------------------------------------------------------------------------------------
            // Core.merge stacks single-channel Mats into one multi-channel Mat.
            // Inputs must share shape/depth (2D: rows/cols; 1D: length/total()).
            //

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

            Mat mat_merged = new Mat();
            Core.merge (mv, mat_merged);
            Log(""mat_merged = "" + mat_merged.dump());
            Log(""mat_merged.channels() = "" + mat_merged.channels());

            Mat a1d = new Mat(new int[] { 3 }, CvType.CV_64FC1);
            a1d.put(0, 0, 1, 2, 3);
            Mat b1d = new Mat(new int[] { 3 }, CvType.CV_64FC1);
            b1d.put(0, 0, 4, 5, 6);
            List<Mat> mv1d = new List<Mat>();
            mv1d.Add(a1d);
            mv1d.Add(b1d);
            Mat merged1d = new Mat();
            Core.merge(mv1d, merged1d);
            Log(""merged1d.dims() = "" + merged1d.dims());
            Log(""merged1d.channels() = "" + merged1d.channels());
            Log(""merged1d = "" + merged1d.dump());
            ");
        }

        public void OnMixChannelsExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  mixChannels example
            // ---------------------------------------------------------------------------------------
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
            Mat mat_mixed1 = new Mat(2, 2, CvType.CV_64FC2);
            Mat mat_mixed2 = new Mat(2, 2, CvType.CV_64FC2);
            MatOfInt fromTo = new MatOfInt(0, 0, 1, 1, 1, 3, 2, 2); // See MatOf* example: int pairs (from,to) as 1D Mat

            List<Mat> mixv = new List<Mat>();
            mixv.Add(mat_mixed1);
            mixv.Add(mat_mixed2);

            // mix
            Core.mixChannels(mv, mixv, fromTo);

            // dump
            Log("mat_mixed1 = " + mat_mixed1.dump());
            Log("mat_mixed2 = " + mat_mixed2.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  mixChannels example
            // ---------------------------------------------------------------------------------------
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
            Mat mat_mixed1 = new Mat(2, 2, CvType.CV_64FC2);
            Mat mat_mixed2 = new Mat(2, 2, CvType.CV_64FC2);
            MatOfInt fromTo = new MatOfInt (0,0, 1,1, 1,3, 2,2); // See MatOf* example

            List<Mat> mixv = new List<Mat> ();
            mixv.Add (mat_mixed1);
            mixv.Add (mat_mixed2);

            // mix
            Core.mixChannels (mv, mixv, fromTo);

            // dump
            Log(""mat_mixed1 = "" + mat_mixed1.dump());
            Log(""mat_mixed2 = "" + mat_mixed2.dump());
            ");
        }

        public void OnSplitExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  split example
            // ---------------------------------------------------------------------------------------
            // The Core.split function separates a single multi-channel image (e.g., an RGB image) into its individual channels; it is the counterpart to the Core.merge function.
            //

            // channels=3, 2x3 matrix
            Mat m1 = new Mat(2, 3, CvType.CV_64FC3);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

            List<Mat> planes = new List<Mat>();

            // split
            Core.split(m1, planes);

            // dump
            for (int i = 0; i < planes.Count; i++)
            {
                Log("planes[" + i + "] = " + planes[i].dump());
            }

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  split example
            // ---------------------------------------------------------------------------------------
            // The Core.split function separates a single multi-channel image (e.g., an RGB image) into its individual channels; it is the counterpart to the Core.merge function.
            //

            // channels=3, 2x3 matrix
            Mat m1 = new Mat (2, 3, CvType.CV_64FC3);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18);

            List<Mat> planes = new List<Mat>();

            // split
            Core.split (m1, planes);

            // dump
            for (int i = 0; i < planes.Count; i++) {
                Log(""planes["" + i + ""] = "" + planes[i].dump());
            }
            ");
        }

        public void OnReduceExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  reduce example
            // ---------------------------------------------------------------------------------------
            // The Core.reduce function compresses (reduces) a multidimensional array (Mat object) along a specified axis. In other words,
            // it can compress multidimensional data into lower dimensional data.
            // Note: reduce results are typically 2D row/column vectors (1xN or Nx1), not true 1D Mats.
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
            Log("m1 = " + m1.dump());
            Log("v1(sum) = " + v1.dump() + " dims = " + v1.dims() + " rows = " + v1.rows() + " cols = " + v1.cols());
            Log("v2(avg) = " + v2.dump());
            Log("v3(min) = " + v3.dump());
            Log("v4(max) = " + v4.dump());

            // reduce 3 x 3 matrix to one col
            Core.reduce(m1, v1, 1, Core.REDUCE_SUM); // total value of each row
            Core.reduce(m1, v2, 1, Core.REDUCE_AVG); // total average value of row
            Core.reduce(m1, v3, 1, Core.REDUCE_MIN); // minimum value of each row
            Core.reduce(m1, v4, 1, Core.REDUCE_MAX); // maximum value of each row

            // dump
            Log("m1 = " + m1.dump());
            Log("v1(sum) = " + v1.dump() + " dims = " + v1.dims() + " rows = " + v1.rows() + " cols = " + v1.cols());
            Log("v2(avg) = " + v2.dump());
            Log("v3(min) = " + v3.dump());
            Log("v4(max) = " + v4.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  reduce example
            // ---------------------------------------------------------------------------------------
            // Core.reduce compresses a Mat along an axis.
            // Results are typically 2D (1xN or Nx1), not true 1D Mats.
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
            Log(""m1 = "" + m1.dump());
            Log(""v1(sum) = "" + v1.dump() + "" dims = "" + v1.dims() + "" rows = "" + v1.rows() + "" cols = "" + v1.cols());
            Log(""v2(avg) = "" + v2.dump());
            Log(""v3(min) = "" + v3.dump());
            Log(""v4(max) = "" + v4.dump());

            // reduce 3 x 3 matrix to one col
            Core.reduce (m1, v1, 1, Core.REDUCE_SUM); // total value of each row
            Core.reduce (m1, v2, 1, Core.REDUCE_AVG); // total average value of row
            Core.reduce (m1, v3, 1, Core.REDUCE_MIN); // minimum value of each row
            Core.reduce (m1, v4, 1, Core.REDUCE_MAX); // maximum value of each row

            // dump
            Log(""m1 = "" + m1.dump());
            Log(""v1(sum) = "" + v1.dump() + "" dims = "" + v1.dims() + "" rows = "" + v1.rows() + "" cols = "" + v1.cols());
            Log(""v2(avg) = "" + v2.dump());
            Log(""v3(min) = "" + v3.dump());
            Log(""v4(max) = "" + v4.dump());
            ");
        }

        public void OnRandShuffleExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  randShuffle example
            // ---------------------------------------------------------------------------------------
            // The Core.randShuffle function randomly shuffles the elements in a Mat object. In other words, it can randomly reorder the order of elements in a Mat object.
            // Core.randShuffle randomly reorders elements. Prefer a continuous Mat; shuffling a ROI
            // also rearranges those elements inside the parent Mat.
            //

            // 4x5 matrix
            Mat m1 = new Mat(4, 5, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Log("m1(original) = " + m1.dump());

            // shuffle
            Core.randShuffle(m1, UnityEngine.Random.value);
            Log("m1(shuffle) = " + m1.dump());

            // submatrix
            Mat m2 = new Mat(m1, new OpenCVForUnity.CoreModule.Rect(1, 1, 3, 2));
            Log("m2(sub-matrix) = " + m2.dump());

            Core.randShuffle(m2, UnityEngine.Random.value);
            Log("m2(sub-matrix) = " + m2.dump());
            Log("m1 = " + m1.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  randShuffle example
            // ---------------------------------------------------------------------------------------
            // The Core.randShuffle function randomly shuffles the elements in a Mat object. In other words, it can randomly reorder the order of elements in a Mat object.
            // Core.randShuffle randomly reorders elements. Prefer a continuous Mat; shuffling a ROI
            // also rearranges those elements inside the parent Mat.
            //

            // 4x5 matrix
            Mat m1 = new Mat (4, 5, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20);
            Log(""m1(original) = "" + m1.dump ());

            // shuffle
            Core.randShuffle (m1, UnityEngine.Random.value);
            Log(""m1(shuffle) = "" + m1.dump ());

            // submatrix
            Mat m2 = new Mat (m1, new OpenCVForUnity.CoreModule.Rect(1,1,3,2));
            Log(""m2(sub-matrix) = "" + m2.dump());

            Core.randShuffle (m2, UnityEngine.Random.value);
            Log(""m2(sub-matrix) = "" + m2.dump());
            Log(""m1 = "" + m1.dump ());
            ");
        }

        public void OnSortExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  sort example
            // ---------------------------------------------------------------------------------------
            // Core.sort sorts each row or each column of a 2D Mat (SORT_EVERY_ROW / SORT_EVERY_COLUMN).
            // Flags combine direction with SORT_ASCENDING or SORT_DESCENDING.
            //

            // 5x5 matrix
            Mat m1 = new Mat(5, 5, CvType.CV_8UC1);
            Core.randu(m1, 0, 25);
            Log("m1 = " + m1.dump());

            Mat dst_mat = new Mat();

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_ASCENDING);
            Log("dst_mat (SORT_EVERY_ROW|SORT_ASCENDING) = " + dst_mat.dump());

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_ROW | Core.SORT_DESCENDING);
            Log("dst_mat (SORT_EVERY_ROW|SORT_DESCENDING) = " + dst_mat.dump());

            // sort ascending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_ASCENDING);
            Log("dst_mat (SORT_EVERY_COLUMN|SORT_ASCENDING) = " + dst_mat.dump());

            // sort descending
            Core.sort(m1, dst_mat, Core.SORT_EVERY_COLUMN | Core.SORT_DESCENDING);
            Log("dst_mat (SORT_EVERY_COLUMN|SORT_DESCENDING) = " + dst_mat.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  sort example
            // ---------------------------------------------------------------------------------------
            // Core.sort sorts each row or each column of a 2D Mat (SORT_EVERY_ROW / SORT_EVERY_COLUMN).
            // Flags combine direction with SORT_ASCENDING or SORT_DESCENDING.
            //

            // 5x5 matrix
            Mat m1 = new Mat (5, 5, CvType.CV_8UC1);
            Core.randu (m1, 0, 25);
            Log(""m1 = "" + m1.dump ());


            Mat dst_mat = new Mat ();

            // sort ascending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_ROW|Core.SORT_ASCENDING);
            Log(""dst_mat (SORT_EVERY_ROW|SORT_ASCENDING) = "" + dst_mat.dump ());

            // sort descending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_ROW|Core.SORT_DESCENDING);
            Log(""dst_mat (SORT_EVERY_ROW|SORT_DESCENDING) = "" + dst_mat.dump ());

            // sort ascending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_COLUMN|Core.SORT_ASCENDING);
            Log(""dst_mat (SORT_EVERY_COLUMN|SORT_ASCENDING) = "" + dst_mat.dump ());

            // sort descending
            Core.sort (m1, dst_mat, Core.SORT_EVERY_COLUMN|Core.SORT_DESCENDING);
            Log(""dst_mat (SORT_EVERY_COLUMN|SORT_DESCENDING) = "" + dst_mat.dump ());
            ");
        }

        public void OnComparisonExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  comparison example
            // ---------------------------------------------------------------------------------------
            // Core.compare is element-wise; inputs must match in size (and typically depth).
            // True comparisons write 255 to dst; false writes 0 (single-channel 8U result).
            //

            // 3x3 matrix
            Mat m1 = new Mat(3, 3, CvType.CV_64FC1);
            m1.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Mat m2 = new Mat(3, 3, CvType.CV_64FC1);
            m2.put(0, 0, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            Log("m1 = " + m1.dump());
            Log("m2 = " + m2.dump());

            Mat dst_mat = new Mat();

            // GT (M1 > M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GT);
            Log("GT (M1 > M2) = " + dst_mat.dump());

            // GE (M1 >= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_GE);
            Log("GE (M1 >= M2) = " + dst_mat.dump());

            // EQ (M1 == M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_EQ);
            Log("EQ (M1 == M2) = " + dst_mat.dump());

            // NE (M1 != M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_NE);
            Log("NE (M1 != M2) = " + dst_mat.dump());

            // LE (M1 <= M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LE);
            Log("LE (M1 <= M2) = " + dst_mat.dump());

            // LT (M1 < M2)
            Core.compare(m1, m2, dst_mat, Core.CMP_LT);
            Log("LT (M1 < M2) = " + dst_mat.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  comparison example
            // ---------------------------------------------------------------------------------------
            // Core.compare is element-wise; inputs must match in size (and typically depth).
            // True comparisons write 255 to dst; false writes 0 (single-channel 8U result).
            //

            // 3x3 matrix
            Mat m1 = new Mat (3, 3, CvType.CV_64FC1);
            m1.put (0, 0, 1,2,3,4,5,6,7,8,9);
            Mat m2 = new Mat (3, 3, CvType.CV_64FC1);
            m2.put (0, 0, 10,11,12,13,14,15,16,17,18);

            Log(""m1 = "" + m1.dump ());
            Log(""m2 = "" + m2.dump ());

            Mat dst_mat = new Mat ();

            // GT (M1 > M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_GT);
            Log(""GT (M1 > M2) = "" + dst_mat.dump ());

            // GE (M1 >= M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_GE);
            Log(""GE (M1 >= M2) = "" + dst_mat.dump ());

            // EQ (M1 == M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_EQ);
            Log(""EQ (M1 == M2) = "" + dst_mat.dump ());

            // NE (M1 != M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_NE);
            Log(""NE (M1 != M2) = "" + dst_mat.dump ());

            // LE (M1 <= M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_LE);
            Log(""LE (M1 <= M2) = "" + dst_mat.dump ());

            // LT (M1 < M2)
            Core.compare (m1, m2, dst_mat, Core.CMP_LT);
            Log(""LT (M1 < M2) = "" + dst_mat.dump ());
            ");
        }

        public void OnGetAndPutExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  get and put example
            // ---------------------------------------------------------------------------------------
            // mat.get() function gets the value of a specific element in a Mat object.
            // mat.put() function sets a new value for a specific element in a Mat object.
            //
            // OpenCVForUnity has several faster and more efficient functions for accessing Mat elements.
            // - Use the MatBufferUtils.CopyFromMat or MatBufferUtils.CopyToMat functions to copy through a data array in one go.
            // - Use the mat.at function to access the element of ​​Mat.
            // - Use the mat.AsSpan function to access the dara memory area of ​​Mat.
            //

            // channels=4 3x3 matrix (RGBA layout: index 0=R, 1=G, 2=B, 3=A)
            Mat m1 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));
            Log("m1 = " + m1.dump());

            //
            // Get elements
            //

            // get returns double[] even for CV_8UC4; cast or use mat.at for typed access.
            double[] m1_1_1 = m1.get(1, 1);
            Log("m1[1,1] (use mat.get()) = " + m1_1_1[0] + ", " + m1_1_1[1] + ", " + m1_1_1[2] + ", " + m1_1_1[3]);

            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            Span<byte> m1_2_2 = m1.at<byte>(2, 2);
            Log("m1[2,2] (use mat.at()) = " + m1_2_2[0] + ", " + m1_2_2[1] + ", " + m1_2_2[2] + ", " + m1_2_2[3]);

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total() * m1.channels()];
            m1.get(0, 0, m1_array);
            string dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Log("m1_array (use mat.get()) = " + dump_str);

            // a faster and more efficient method using the MatBufferUtils.CopyFromMat function.
            MatBufferUtils.CopyFromMat(m1, m1_array);
            dump_str = "";
            foreach (var i in m1_array)
            {
                dump_str += i + ", ";
            }
            Log("m1_array (use MatBufferUtils.CopyFromMat()) = " + dump_str);

            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            Span<byte> m1_span = m1.AsSpan<byte>();
            dump_str = "";
            for (int i = 0; i < m1_span.Length; i++)
            {
                dump_str += m1_span[i] + ", ";
            }
            Log("m1_span (use mat.AsSpan()) = " + dump_str);

            //
            // Put elements
            //

            // put an element value in a matrix.
            Mat m2 = m1.clone();
            m2.put(1, 1, 5, 6, 7, 8);
            Log("m2 (use mat.put()) = " + m2.dump());

            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_1_1 = m2.at<byte>(1, 1);
            m2_1_1[0] = 5;
            m2_1_1[1] = 6;
            m2_1_1[2] = 7;
            m2_1_1[3] = 8;
            Log("m2 (use mat.at()) = " + m2.dump());

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
            Log("m2 (use mat.put()) = " + m2.dump());

            // a faster and more efficient method using the MatBufferUtils.CopyToMat function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            MatBufferUtils.CopyToMat(m2_arr, m2);
            Log("m2 (use MatBufferUtils.CopyToMat()) = " + m2.dump());

            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_span = m2.AsSpan<byte>();
            m2_arr.AsSpan<byte>().CopyTo(m2_span);
            Log("m2 (use mat.AsSpan()) = " + m2.dump());

            // fill element values (setTo method)
            m2.setTo(new Scalar(100, 100, 100, 100));
            Log("m2 (use mat.setTo()) = " + m2.dump());

            //
            // 0D / 1D access (OpenCV 5)
            // Element count is total(); 1D indexes along columns with row fixed at 0 (get(0,i) / put(0,i)).
            // 0D scalar: prefer put(Array.Empty<int>(), value) / get with empty idx; get(0,0)/put(0,0) also work
            // because rows/cols report 1 for a non-empty 0D Mat.
            // AsSpan / MatBufferUtils copy total()*elemSize bytes for any rank (including 0D/1D).
            //
            Mat v = new Mat(new int[] { 3 }, CvType.CV_8UC1);
            v.put(0, 0, 10, 20, 30);
            Log("v = " + v.dump());
            Log("v.get(0,1) = " + v.get(0, 1)[0]);
            Span<byte> vAt1 = v.at<byte>(1);
            Log("v.at(1) = " + vAt1[0]);

            Mat s = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(9));
            Log("s = " + s.dump());
            Log("s.get(0,0) = " + s.get(0, 0)[0] + " // also valid for 0D");
            s.put(Array.Empty<int>(), 11); // empty idx = 0D address
            Log("s after put(Array.Empty<int>(), 11) = " + s.dump());

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  get and put example
            // ---------------------------------------------------------------------------------------
            // mat.get() function gets the value of a specific element in a Mat object.
            // mat.put() function sets a new value for a specific element in a Mat object.
            //
            // OpenCVForUnity has several faster and more efficient functions for accessing Mat elements.
            // - Use the MatBufferUtils.CopyFromMat or MatBufferUtils.CopyToMat functions to copy through a data array in one go.
            // - Use the mat.at function to access the element of ​​Mat.
            // - Use the mat.AsSpan function to access the dara memory area of ​​Mat.
            //

            // channels=4 3x3 matrix (RGBA layout: index 0=R, 1=G, 2=B, 3=A)
            Mat m1 = new Mat(3, 3, CvType.CV_8UC4, new Scalar(1, 2, 3, 4));
            Log(""m1 = "" + m1.dump());

            //
            // Get elements
            //

            // get returns double[] even for CV_8UC4; cast or use mat.at for typed access.
            double[] m1_1_1 = m1.get(1, 1);
            Log(""m1[1,1] (use mat.get()) = "" + m1_1_1[0] + "", "" + m1_1_1[1] + "", "" + m1_1_1[2] + "", "" + m1_1_1[3]);

            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            Span<byte> m1_2_2 = m1.at<byte>(2, 2);
            Log(""m1[2,2] (use mat.at()) = "" + m1_2_2[0] + "", "" + m1_2_2[1] + "", "" + m1_2_2[2] + "", "" + m1_2_2[3]);

            // get an array of all element values.
            byte[] m1_array = new byte[m1.total() * m1.channels()];
            m1.get(0, 0, m1_array);
            string dump_str = """";
            foreach (var i in m1_array)
            {
                dump_str += i + "", "";
            }
            Log(""m1_array (use mat.get()) = "" + dump_str);

            // a faster and more efficient method using the MatBufferUtils.CopyFromMat function.
            MatBufferUtils.CopyFromMat(m1, m1_array);
            dump_str = """";
            foreach (var i in m1_array)
            {
                dump_str += i + "", "";
            }
            Log(""m1_array (use MatBufferUtils.CopyFromMat()) = "" + dump_str);

            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            Span<byte> m1_span = m1.AsSpan<byte>();
            dump_str = """";
            for (int i = 0; i < m1_span.Length; i++)
            {
                dump_str += m1_span[i] + "", "";
            }
            Log(""m1_span (use mat.AsSpan()) = "" + dump_str);

            //
            // Put elements
            //

            // put an element value in a matrix.
            Mat m2 = m1.clone();
            m2.put(1, 1, 5, 6, 7, 8);
            Log(""m2 (use mat.put()) = "" + m2.dump());

            // an even faster, more efficient, non-memory-allocated method using the mat.at function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_1_1 = m2.at<byte>(1, 1);
            m2_1_1[0] = 5;
            m2_1_1[1] = 6;
            m2_1_1[2] = 7;
            m2_1_1[3] = 8;
            Log(""m2 (use mat.at()) = "" + m2.dump());

            // put an array of element values in a matrix.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            byte[] m2_arr = new byte[] {
                5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8,
                5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8,
                5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8
            };
            m2.put(0, 0, m2_arr);
            Log(""m2 (use mat.put()) = "" + m2.dump());

            // a faster and more efficient method using the MatBufferUtils.CopyToMat function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            MatBufferUtils.CopyToMat(m2_arr, m2);
            Log(""m2 (use MatBufferUtils.CopyToMat()) = "" + m2.dump());

            // an even faster, more efficient, non-memory-allocated method using the mat.AsSpan function.
            m2.setTo(new Scalar(1, 2, 3, 4));// reset values
            Span<byte> m2_span = m2.AsSpan<byte>();
            m2_arr.AsSpan<byte>().CopyTo(m2_span);
            Log(""m2 (use mat.AsSpan()) = "" + m2.dump());

            // fill element values (setTo method)
            m2.setTo(new Scalar(100, 100, 100, 100));
            Log(""m2 (use mat.setTo()) = "" + m2.dump());

            //
            // 0D / 1D access (OpenCV 5)
            // Element count is total(); 1D indexes along columns with row fixed at 0 (get(0,i) / put(0,i)).
            // 0D scalar: prefer put(Array.Empty<int>(), value); get(0,0)/put(0,0) also work for non-empty 0D.
            // AsSpan / MatBufferUtils copy total()*elemSize bytes for any rank (including 0D/1D).
            //
            Mat v = new Mat(new int[] { 3 }, CvType.CV_8UC1);
            v.put(0, 0, 10, 20, 30);
            Log(""v = "" + v.dump());
            Log(""v.get(0,1) = "" + v.get(0, 1)[0]);
            Span<byte> vAt1 = v.at<byte>(1);
            Log(""v.at(1) = "" + vAt1[0]);

            Mat s = new Mat(Array.Empty<int>(), CvType.CV_8UC1, new Scalar(9));
            Log(""s = "" + s.dump());
            Log(""s.get(0,0) = "" + s.get(0, 0)[0] + "" // also valid for 0D"");
            s.put(Array.Empty<int>(), 11); // empty idx = 0D address
            Log(""s after put(Array.Empty<int>(), 11) = "" + s.dump());
            ");
        }

        public void OnAccessingPixelValueExampleButtonClick()
        {
            BeginExample();
            // ---------------------------------------------------------------------------------------
            //  accessing pixel value example
            // ---------------------------------------------------------------------------------------
            // How access pixel values in an OpenCV Mat (2D image).
            // Pixel channels (e.g. RGBA) are not Mat dims — a color image is still dims==2.
            // - 1. Use get and put method
            // - 2. Use mat.at method
            // - 3. Use MatBufferUtils.CopyFromMat and MatBufferUtils.CopyToMat method
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

            // Per-pixel get/put allocates and crosses the native boundary each iteration (slowest path).
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

            Log("1. Use get and put method. time: " + watch.ElapsedMilliseconds + " ms");

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

            Log("2. Use mat.at method. time: " + watch.ElapsedMilliseconds + " ms");

            //
            // 3. Use MatBufferUtils.CopyFromMat and MatBufferUtils.CopyToMat method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            MatBufferUtils.CopyFromMat(imgMat, img_array);

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
            MatBufferUtils.CopyToMat(img_array, imgMat);

            watch.Stop();

            Log("3. Use MatBufferUtils.CopyFromMat and MatBufferUtils.CopyToMat method. time: " + watch.ElapsedMilliseconds + " ms");

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

            Log("4. Use mat.AsSpan method. time: " + watch.ElapsedMilliseconds + " ms");

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

            Log("5. Use pointer access. time: " + watch.ElapsedMilliseconds + " ms");

            EndExample(@"
            // ---------------------------------------------------------------------------------------
            //  accessing pixel values example (unsafe)
            // ---------------------------------------------------------------------------------------

            // How access pixel values in a 2D OpenCV Mat (image).
            // Pixel channels (RGBA) are not Mat dims — color images remain dims==2.

            // channels=4 512x512 matrix (RGBA color image)
            Mat imgMat = new Mat (512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            //
            // 1. Use get and put method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Start();

            // Per-pixel get/put allocates and crosses the native boundary each iteration (slowest path).
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

            Log(""1.Use get and put method. time: "" + watch.ElapsedMilliseconds + "" ms"");


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

            Log(""2.Use mat.at method. time: "" + watch.ElapsedMilliseconds + "" ms"");


            //
            // 3. Use MatBufferUtils.CopyFromMat and MatBufferUtils.CopyToMat method
            //
            imgMat.setTo(new Scalar(0, 0, 0, 255));

            watch.Reset();
            watch.Start();

            // copies an OpenCV Mat data to a pixel data Array.
            byte[] img_array = new byte[imgMat.total() * imgMat.channels()];
            MatBufferUtils.CopyFromMat(imgMat, img_array);

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
            MatBufferUtils.CopyToMat(img_array, imgMat);

            watch.Stop();

            Log(""3. Use MatBufferUtils.CopyFromMat and MatBufferUtils.CopyToMat method. time: "" + watch.ElapsedMilliseconds + "" ms"");



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

            Log(""4.Use mat.AsSpan method. time: "" + watch.ElapsedMilliseconds + "" ms"");




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

            Log(""5. Use pointer access. time: "" + watch.ElapsedMilliseconds + "" ms"");

            ");
        }
    }
}
