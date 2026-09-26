#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// DNN Object Detection Example
    /// Generic object detection on each input frame using configurable DNN models (Caffe, TensorFlow, Darknet, ONNX, etc.).
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Resolving model, config, and class-name files from StreamingAssets
    /// - Converting RGBA frames to BGR and building input blobs with blobFromImage
    /// - Synchronous <see cref="Net"/> inference and multi-format postprocessing (Faster-RCNN, YOLO Region, DetectionOutput)
    /// - Drawing bounding boxes and class labels on the preview <see cref="Mat"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Net"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Rect2d"/>
    /// - <see cref="Dnn"/>: readNet, blobFromImage, forward, NMSBoxes
    /// - <see cref="Imgproc"/>: cvtColor, rectangle, putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/object_detection.cpp
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class DnnObjectDetectionExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string Model;

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string Config;

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string Classes;

        [TooltipAttribute("Optional list of classes to label detected objects.")]
        public List<string> ClassesList;

        [TooltipAttribute("Confidence threshold.")]
        public float ConfThreshold = 0.5f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float NmsThreshold = 0.4f;

        [TooltipAttribute("Preprocess input image by multiplying on a scale factor.")]
        public float Scale = 1.0f;

        [TooltipAttribute("Preprocess input image by subtracting mean values. Mean values should be in BGR order and delimited by spaces.")]
        public Scalar Mean = new Scalar(0, 0, 0, 0);

        [TooltipAttribute("Indicate that model works with RGB input images instead BGR ones.")]
        public bool SwapRB = false;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int InpWidth = 320;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int InpHeight = 320;

        // Protected Fields
        protected Texture2D _texture;

        protected MultiSourceToMatHelper _multiSourceToMatHelper;

        protected Mat _bgrMat;

        protected Net _net;

        protected FpsMonitor _fpsMonitor;

        protected SourceToMatControlPanel _controlPanel;

        protected List<string> _classNames;
        protected List<string> _outBlobNames;
        protected List<string> _outBlobTypes;

        protected string _classesFilepath;
        protected string _configFilepath;
        protected string _modelFilepath;

        protected CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Resolves <see cref="Model"/> (StreamingAssets-relative) to the path passed to <see cref="OpenCVForUnityEnv.GetFilePathAsync"/>.
        /// For example, rewrites <c>.onnx</c> to <c>.sentis</c> for Sentis. The default implementation returns <paramref name="modelRelativePath"/> unchanged.
        /// </summary>
        /// <param name="modelRelativePath">Value of the <see cref="Model"/> field (only called when non-null and non-empty).</param>
        /// <returns>String used as the same relative key for display and download-instruction messages.</returns>

        // Unity Lifecycle Methods
        /// <summary>
        /// Resolves paths under StreamingAssets asynchronously, then initializes the DNN and helper. Override <see cref="GetModelFilePathForStreamingAssetsLoad"/>
        /// or this method to substitute Sentis model paths, etc.
        /// </summary>
        protected virtual async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = GetComponent<MultiSourceToMatHelper>();

            // RGBA matches Unity TextureFormat.RGBA32 used for RawImage preview.
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            if (!string.IsNullOrEmpty(Classes))
            {
                _classesFilepath = await OpenCVForUnityEnv.GetFilePathAsync(Classes, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_classesFilepath))
                {
                    Debug.Log("The file:" + Classes + " did not exist.", this);
                }
            }
            if (!string.IsNullOrEmpty(Config))
            {
                _configFilepath = await OpenCVForUnityEnv.GetFilePathAsync(Config, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_configFilepath))
                {
                    Debug.Log("The file:" + Config + " did not exist.", this);
                }
            }
            if (!string.IsNullOrEmpty(Model))
            {
                string modelPathForLoad = GetModelFilePathForStreamingAssetsLoad(Model);
                _modelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(modelPathForLoad, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_modelFilepath))
                {
                    Debug.Log("The file:" + modelPathForLoad + " did not exist.", this);
                }
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (!string.IsNullOrEmpty(Classes))
            {
                _classNames = ReadClassNames(_classesFilepath);
                if (_classNames == null)
                {
                    Debug.LogError(Classes + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                    if (_fpsMonitor != null)
                    {
                        _fpsMonitor.ConsoleText = "classes file is not loaded.\nPlease read console message.";
                    }
                }
            }
            else if (ClassesList.Count > 0)
            {
                _classNames = ClassesList;
            }

            if (string.IsNullOrEmpty(_modelFilepath))
            {
                Debug.LogError(Model + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
            }
            else
            {
                // Load DNN model from StreamingAssets-resolved paths (weights + optional config).
                //! [Initialize network]
                _net = Dnn.readNet(_modelFilepath, _configFilepath);
                //! [Initialize network]

                _outBlobNames = GetOutputsNames(_net);
                //for (int i = 0; i < outBlobNames.Count; i++)
                //{
                //    Debug.Log("names [" + i + "] " + outBlobNames[i]);
                //}

                _outBlobTypes = GetOutputsTypes(_net);
                //for (int i = 0; i < outBlobTypes.Count; i++)
                //{
                //    Debug.Log("types [" + i + "] " + outBlobTypes[i]);
                //}
            }

            if (_net == null)
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        protected virtual void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _net?.Dispose();
            _net = null;

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
            _cts = null;
        }
        // Public Methods

        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Runs DNN inference and updates the preview texture when a new frame is available during playback.
        /// </summary>
        public virtual void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat == null)
            {
                return;
            }

            ProcessFrameMatUpdated(rgbaMat);

            if (_texture != null)
            {
                OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public virtual void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            RecreatePreviewTexture();

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat != null)
            {
                _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net);
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public virtual void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat != null)
            {
                _bgrMat?.Dispose();
                _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public virtual void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public virtual void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            _bgrMat?.Dispose();
            _bgrMat = null;

            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public virtual void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public virtual async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public virtual void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public virtual void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public virtual void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public virtual void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public virtual void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public virtual void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public virtual void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
        }

        /// <summary>
        /// Resolves <see cref="Model"/> (StreamingAssets-relative) to the path passed to <see cref="OpenCVForUnityEnv.GetFilePathAsync"/>.
        /// For example, rewrites <c>.onnx</c> to <c>.sentis</c> for Sentis. The default implementation returns <paramref name="modelRelativePath"/> unchanged.
        /// </summary>
        /// <param name="modelRelativePath">Value of the <see cref="Model"/> field (only called when non-null and non-empty).</param>
        /// <returns>String used as the same relative key for display and download-instruction messages.</returns>
        protected virtual string GetModelFilePathForStreamingAssetsLoad(string modelRelativePath)
        {
            return modelRelativePath;
        }

        /// <summary>
        /// Processes the current frame Mat for DNN inference and annotation.
        /// Override in derived examples that replace the default forward/postprocess path.
        /// </summary>
        /// <param name="rgbaMat">Current RGBA frame owned by the helper; do not dispose.</param>
        protected virtual void ProcessFrameMatUpdated(Mat rgbaMat)
        {
            if (_net == null || _bgrMat == null)
            {
                return;
            }

            // Convert RGBA camera frame to BGR for DNN blobFromImage input.
            Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

            // Create a 4D blob from a frame.
            Size inpSize = new Size(InpWidth > 0 ? InpWidth : _bgrMat.cols(),
                               InpHeight > 0 ? InpHeight : _bgrMat.rows());
            Mat blob = Dnn.blobFromImage(_bgrMat, Scale, inpSize, Mean, SwapRB, false);

            // Run a model.
            _net.setInput(blob);

            if (_net.getLayer(0).outputNameToIndex("im_info") != -1)
            {  // Faster-RCNN or R-FCN
                Imgproc.resize(_bgrMat, _bgrMat, inpSize);
                Mat imInfo = new Mat(1, 3, CvType.CV_32FC1);
                imInfo.put(0, 0, new float[] {
                    (float)inpSize.height,
                    (float)inpSize.width,
                    1.6f
                });
                _net.setInput(imInfo, "im_info");
            }

            //TickMeter tm = new TickMeter();
            //tm.start();

            List<Mat> outs = new List<Mat>();
            _net.forward(outs, _outBlobNames);

            //tm.stop();
            //Debug.Log("Inference time, ms: " + tm.getTimeMilli());

            Postprocess(rgbaMat, outs, _net, Dnn.DNN_BACKEND_OPENCV);

            blob.Dispose();
            foreach (var out_mat in outs)
            {
                out_mat.Dispose();
            }
        }

        /// <summary>
        /// Reads the class names.
        /// </summary>
        /// <returns>The class names.</returns>
        /// <param name="filename">Filename.</param>
        protected virtual List<string> ReadClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message, this);
                return null;
            }
            finally
            {
                if (cReader != null)
                {
                    cReader.Close();
                }
            }

            return classNames;
        }

        /// <summary>
        /// Postprocess the specified frame, outs and net.
        /// </summary>
        /// <param name="frame">Frame.</param>
        /// <param name="outs">Outs.</param>
        /// <param name="net">Net.</param>
        /// <param name="backend">Backend.</param>
        protected virtual void Postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {
            MatOfInt outLayers = net.getUnconnectedOutLayers();
            string outLayerType = _outBlobTypes[0];

            List<int> classIdsList = new List<int>();
            List<float> confidencesList = new List<float>();
            List<Rect2d> boxesList = new List<Rect2d>();

            if (net.getLayer(0).outputNameToIndex("im_info") != -1)
            {
                // Faster-RCNN or R-FCN
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {
                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {
                        outs[0].get(i, 0, data);

                        float confidence = data[2];
                        if (confidence > ConfThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 1); // Skip 0th background class id.
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "DetectionOutput")
            {
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {
                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];
                    for (int i = 0; i < outs[0].rows(); i++)
                    {
                        outs[0].get(i, 0, data);

                        float confidence = data[2];
                        if (confidence > ConfThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 1); // Skip 0th background class id.
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "Region")
            {
                for (int i = 0; i < outs.Count; ++i)
                {
                    // Network produces output blob with a shape NxC where N is a number of
                    // detected objects and C is a number of classes + 4 where the first 4
                    // numbers are [center_x, center_y, width, height]

                    //Debug.Log ("outs[i].ToString() "+outs[i].ToString());

                    float[] positionData = new float[5];
                    float[] confidenceData = new float[outs[i].cols() - 5];
                    for (int p = 0; p < outs[i].rows(); p++)
                    {
                        outs[i].get(p, 0, positionData);
                        outs[i].get(p, 5, confidenceData);

                        int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                        float confidence = confidenceData[maxIdx];
                        if (confidence > ConfThreshold)
                        {
                            float centerX = positionData[0] * frame.cols();
                            float centerY = positionData[1] * frame.rows();
                            float width = positionData[2] * frame.cols();
                            float height = positionData[3] * frame.rows();
                            float left = centerX - width / 2;
                            float top = centerY - height / 2;

                            classIdsList.Add(maxIdx);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Unknown output layer type: " + outLayerType, this);
            }

            // NMS is used inside Region layer only on DNN_BACKEND_OPENCV for another backends we need NMS in sample
            // or NMS is required if number of outputs > 1
            if (outLayers.total() > 1 || (outLayerType == "Region" && backend != Dnn.DNN_BACKEND_OPENCV))
            {
                Dictionary<int, List<int>> class2indices = new Dictionary<int, List<int>>();
                for (int i = 0; i < classIdsList.Count; i++)
                {
                    if (confidencesList[i] >= ConfThreshold)
                    {
                        if (!class2indices.ContainsKey(classIdsList[i]))
                        {
                            class2indices.Add(classIdsList[i], new List<int>());
                        }

                        class2indices[classIdsList[i]].Add(i);
                    }
                }

                List<Rect2d> nmsBoxesList = new List<Rect2d>();
                List<float> nmsConfidencesList = new List<float>();
                List<int> nmsClassIdsList = new List<int>();
                foreach (int key in class2indices.Keys)
                {
                    List<Rect2d> localBoxesList = new List<Rect2d>();
                    List<float> localConfidencesList = new List<float>();
                    List<int> classIndicesList = class2indices[key];
                    for (int i = 0; i < classIndicesList.Count; i++)
                    {
                        localBoxesList.Add(boxesList[classIndicesList[i]]);
                        localConfidencesList.Add(confidencesList[classIndicesList[i]]);
                    }

                    using (MatOfRect2d localBoxes = new MatOfRect2d(localBoxesList.ToArray()))
                    using (MatOfFloat localConfidences = new MatOfFloat(localConfidencesList.ToArray()))
                    using (MatOfInt nmsIndices = new MatOfInt())
                    {
                        Dnn.NMSBoxes(localBoxes, localConfidences, ConfThreshold, NmsThreshold, nmsIndices);
                        ReadOnlySpan<int> nmsIndexSpan = nmsIndices.AsSpan<int>();
                        for (int i = 0; i < nmsIndexSpan.Length; i++)
                        {
                            int idx = nmsIndexSpan[i];
                            nmsBoxesList.Add(localBoxesList[idx]);
                            nmsConfidencesList.Add(localConfidencesList[idx]);
                            nmsClassIdsList.Add(key);
                        }
                    }
                }

                boxesList = nmsBoxesList;
                classIdsList = nmsClassIdsList;
                confidencesList = nmsConfidencesList;
            }

            for (int idx = 0; idx < boxesList.Count; ++idx)
            {
                Rect2d box = boxesList[idx];
                DrawPred(classIdsList[idx], confidencesList[idx], box.x, box.y,
                    box.x + box.width, box.y + box.height, frame);
            }
        }

        /// <summary>
        /// Draws the pred.
        /// </summary>
        /// <param name="classId">Class identifier.</param>
        /// <param name="conf">Conf.</param>
        /// <param name="left">Left.</param>
        /// <param name="top">Top.</param>
        /// <param name="right">Right.</param>
        /// <param name="bottom">Bottom.</param>
        /// <param name="frame">Frame.</param>
        protected virtual void DrawPred(int classId, float conf, double left, double top, double right, double bottom, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

            string label = conf.ToString();
            if (_classNames != null && _classNames.Count != 0)
            {
                if (classId < (int)_classNames.Count)
                {
                    label = _classNames[classId] + ": " + label;
                }
            }

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            top = Mathf.Max((float)top, (float)labelSize.height);
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        /// <summary>
        /// Gets the outputs names.
        /// </summary>
        /// <returns>The outputs names.</returns>
        /// <param name="net">Net.</param>
        protected virtual List<string> GetOutputsNames(Net net)
        {
            return net.getUnconnectedOutLayersNames();
        }

        /// <summary>
        /// Gets the outputs types.
        /// </summary>
        /// <returns>The outputs types.</returns>
        /// <param name="net">Net.</param>
        protected virtual List<string> GetOutputsTypes(Net net)
        {
            List<string> types = new List<string>();

            MatOfInt outLayers = net.getUnconnectedOutLayers();
            ReadOnlySpan<int> layerIds = outLayers.AsSpan<int>();
            for (int i = 0; i < layerIds.Length; ++i)
            {
                types.Add(net.getLayer(layerIds[i]).get_type());
            }
            outLayers.Dispose();

            return types;
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="net"/> (or "-" when a value is not available).
        /// </summary>
        protected static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, Net net)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            if (net != null)
            {
                // cv::dnn::Net: No PreferredBackend/PreferredTarget getters in the C# binding; treat as default OpenCV DNN inference.
                fpsMonitor.Add("dnnBackend", "OPENCV");
                fpsMonitor.Add("dnnTarget", "CPU");
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
            fpsMonitor.Add("useAsyncInference", "False");
        }
        // Private Methods

        protected virtual void RecreatePreviewTexture()
        {
            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
            }

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        protected virtual void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        protected virtual void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        protected virtual string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        protected virtual void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        protected virtual void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }
    }
}

#endif
