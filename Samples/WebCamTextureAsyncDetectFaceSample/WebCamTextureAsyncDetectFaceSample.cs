using UnityEngine;
using System;
using System.Collections;
using OpenCVForUnity;
using System.Collections.Generic;
using Rect = OpenCVForUnity.Rect;

#if UNITY_WSA
#if NETFX_CORE

using System.Threading;
using System.Threading.Tasks;

#else

#endif
#else
using System.Threading;

#endif
using PositionsVector = System.Collections.Generic.List<OpenCVForUnity.Rect>;


namespace OpenCVForUnitySample
{
		/// <summary>
		/// WebCamTexture async detect face sample.
		/// This cord referred to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
		/// </summary>
		public class WebCamTextureAsyncDetectFaceSample : MonoBehaviour
		{

				/// <summary>
				/// The web cam texture.
				/// </summary>
				WebCamTexture webCamTexture;

				/// <summary>
				/// The web cam device.
				/// </summary>
				WebCamDevice webCamDevice;

				/// <summary>
				/// The colors.
				/// </summary>
				Color32[] colors;

                /// <summary>
                /// Should use front facing.
                /// </summary>
                public bool shouldUseFrontFacing = false;

				/// <summary>
				/// The width.
				/// </summary>
				int width = 640;

				/// <summary>
				/// The height.
				/// </summary>
				int height = 480;

				/// <summary>
				/// The rgba mat.
				/// </summary>
				Mat rgbaMat;

				/// <summary>
				/// The gray mat.
				/// </summary>
				Mat grayMat;

				/// <summary>
				/// The texture.
				/// </summary>
				Texture2D texture;

				/// <summary>
				/// The cascade.
				/// </summary>
				CascadeClassifier cascade;

				/// <summary>
				/// The init done.
				/// </summary>
				bool initDone = false;

                /// <summary>
                /// The screenOrientation.
                /// </summary>
                ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

				private CascadeClassifier regionCascade;
				Rect[] rectsWhereRegions;
				List<Rect> detectedObjectsInRegions = new List<Rect> ();
				List<Rect> resultObjects = new List<Rect> ();
				private List<TrackedObject> trackedObjects = new List<TrackedObject> ();
				private List<float> weightsPositionsSmoothing = new List<float> ();
				private List<float> weightsSizesSmoothing = new List<float> ();
				private Parameters parameters;
				private InnerParameters innerParameters;


				// for Thread
#if UNITY_WSA
#if NETFX_CORE
                private Task task_ = null;
                private CancellationTokenSource tokenSource_ = null;
#else

#endif
#else
				private volatile bool shouldStopThread = false;
#endif

				private volatile ThreadComm threadComm = new ThreadComm ();
				private System.Object thisLock = new System.Object ();
				private volatile bool isThreadRunning = false;
				private volatile bool didUpdateTheDetectionResult = false;
				private Mat grayMat4Thread;
				private MatOfRect resultDetect;


				// Use this for initialization
				void Start ()
				{
						weightsPositionsSmoothing.Add (1);
						weightsSizesSmoothing.Add (0.5f);
						weightsSizesSmoothing.Add (0.3f);
						weightsSizesSmoothing.Add (0.2f);
						parameters = new Parameters ();
						innerParameters = new InnerParameters ();


						StartCoroutine (init ());

				}

				private IEnumerator init ()
				{

						if (webCamTexture != null) {
								webCamTexture.Stop ();
								initDone = false;

								rgbaMat.Dispose ();
								grayMat.Dispose ();
						}

						// Checks how many and which cameras are available on the device
						for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {


								if (WebCamTexture.devices [cameraIndex].isFrontFacing == shouldUseFrontFacing) {


										Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);

										webCamDevice = WebCamTexture.devices [cameraIndex];

										webCamTexture = new WebCamTexture (webCamDevice.name, width, height);


										break;
								}


						}

						if (webCamTexture == null) {
								webCamDevice = WebCamTexture.devices [0];
								webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
						}

						Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);


						// Starts the camera
						webCamTexture.Play ();

						while (true) {
								//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
                                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                                #else
								if (webCamTexture.didUpdateThisFrame) {
                                #endif

										Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
										Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);


										colors = new Color32[webCamTexture.width * webCamTexture.height];
										rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
										grayMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

                                        gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                                        updateLayout();
										
										regionCascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface.xml"));


										initThread ();

                                        screenOrientation = Screen.orientation;
										initDone = true;

										break;
								} else {
										yield return 0;
								}
						}
				}


                private void updateLayout()
                {
                    gameObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
                    gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);

                    if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
                    {
                        gameObject.transform.eulerAngles = new Vector3(0, 0, -90);
                    }


                    float width = 0;
                    float height = 0;
                    if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
                    {
                        width = gameObject.transform.localScale.y;
                        height = gameObject.transform.localScale.x;
                    }
                    else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180)
                    {
                        width = gameObject.transform.localScale.x;
                        height = gameObject.transform.localScale.y;
                    }

                    float widthScale = (float)Screen.width / width;
                    float heightScale = (float)Screen.height / height;
                    if (widthScale < heightScale)
                    {
                        Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                    }
                    else
                    {
                        Camera.main.orthographicSize = height / 2;
                    }
                }


				// Update is called once per frame
				void Update ()
				{
						if (!initDone)
								return;


                        if (screenOrientation != Screen.orientation)
                        {
                            screenOrientation = Screen.orientation;
                            updateLayout();
                        }

                        #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				        if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                        #else
						if (webCamTexture.didUpdateThisFrame) {
                        #endif

								Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

                                if (webCamDevice.isFrontFacing)
                                {
                                    if (webCamTexture.videoRotationAngle == 0)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 1);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 90)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 0);
                                    }
                                    if (webCamTexture.videoRotationAngle == 180)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 0);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 270)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 1);
                                    }
                                }
                                else
                                {
                                    if (webCamTexture.videoRotationAngle == 180)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, -1);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 270)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, -1);
                                    }
                                }


								Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
								Imgproc.equalizeHist (grayMat, grayMat);

                                
								if (!threadComm.shouldDetectInMultiThread) {
										lock (thisLock) {
												grayMat.copyTo (grayMat4Thread);
										}
										threadComm.shouldDetectInMultiThread = true;
								}


								OpenCVForUnity.Rect[] rects;

								if (didUpdateTheDetectionResult) {
										lock (thisLock) {
												//Debug.Log("DetectionBasedTracker::process: get _rectsWhereRegions were got from resultDetect");
												rectsWhereRegions = resultDetect.toArray ();
												rects = resultDetect.toArray ();
										}
										didUpdateTheDetectionResult = false;


										for (int i = 0; i < rects.Length; i++) {
												Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 0, 255, 255), 2);
										}

								} else {
										//Debug.Log("DetectionBasedTracker::process: get _rectsWhereRegions from previous positions");
										rectsWhereRegions = new Rect[trackedObjects.Count];

										for (int i = 0; i < trackedObjects.Count; i++) {
												int n = trackedObjects [i].lastPositions.Count;
												//if (n > 0) UnityEngine.Debug.LogError("n > 0 is false");

												Rect r = trackedObjects [i].lastPositions [n - 1];
												if (r.area () == 0) {
														Debug.Log ("DetectionBasedTracker::process: ERROR: ATTENTION: strange algorithm's behavior: trackedObjects[i].rect() is empty");
														continue;
												}

												//correction by speed of rectangle
												if (n > 1) {
														Point center = centerRect (r);
														Point center_prev = centerRect (trackedObjects [i].lastPositions [n - 2]);
														Point shift = new Point ((center.x - center_prev.x) * innerParameters.coeffObjectSpeedUsingInPrediction,
                        (center.y - center_prev.y) * innerParameters.coeffObjectSpeedUsingInPrediction);

														r.x += (int)Math.Round (shift.x);
														r.y += (int)Math.Round (shift.y);
												}
												rectsWhereRegions [i] = r;
										}

										rects = rectsWhereRegions;
										for (int i = 0; i < rects.Length; i++) {
												Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 255, 0, 255), 2);
										}
								}

								if (rectsWhereRegions.Length > 0) {
										detectedObjectsInRegions.Clear ();

										int len = rectsWhereRegions.Length;
										for (int i = 0; i < len; i++) {
												detectInRegion (grayMat, rectsWhereRegions [i], detectedObjectsInRegions);
										}
								}

								updateTrackedObjects (detectedObjectsInRegions);

								getObjects (resultObjects);


								rects = resultObjects.ToArray ();
								for (int i = 0; i < rects.Length; i++) {
										//Debug.Log ("detect faces " + rects [i]);

										Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
								}

								Utils.matToTexture2D (rgbaMat, texture, colors);

						}

				}

				private void initThread ()
				{
						lock (thisLock) {
								cascade = new CascadeClassifier (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
								grayMat4Thread = new Mat ();
						}
						threadComm.shouldDetectInMultiThread = false;

						StartThread ();
				}


#if UNITY_WSA
#if NETFX_CORE


        private void ThreadWorker()
        {
            if (isThreadRunning) return;

            Debug.Log("Thread Start");

            isThreadRunning = true;

            threadComm.shouldDetectInMultiThread = false;
            didUpdateTheDetectionResult = false;

            tokenSource_ = new CancellationTokenSource();

            task_ =Task.Factory.StartNew(
                (o)=>
                {
                    ThreadComm comm = o as ThreadComm;

                    while(true)
                    {
                        tokenSource_.Token.ThrowIfCancellationRequested();

                        if(!comm.shouldDetectInMultiThread) continue;
          
                        lock (thisLock)
                        {
            

                            MatOfRect faces = new MatOfRect();
                            if (cascade != null)
                                cascade.detectMultiScale(grayMat4Thread, faces, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                                    new Size(grayMat4Thread.height() * 0.2, grayMat4Thread.height() * 0.2), new Size());

                            resultDetect = faces;
                        }
                        comm.shouldDetectInMultiThread = false;

                        didUpdateTheDetectionResult = true;
                    }

                }
                , threadComm
                , tokenSource_.Token
            ).ContinueWith(t =>
            {
                tokenSource_.Dispose();
                tokenSource_ = null;

                isThreadRunning = false;
            });

        }

#else

                private void ThreadWorker()
                {
                    if (isThreadRunning) return;

                    Debug.Log("Thread Start");

                    isThreadRunning = true;

                    threadComm.shouldDetectInMultiThread = false;
                    didUpdateTheDetectionResult = false;

                    StartCoroutine("ThreadCoroutine", threadComm);
                }


                IEnumerator ThreadCoroutine(System.Object o)
                {
                    ThreadComm comm = o as ThreadComm;


                    while (true)
                    {
                        while (!comm.shouldDetectInMultiThread) { yield return null; }

                        lock (thisLock)
                        {
                            MatOfRect faces = new MatOfRect();
                            if (cascade != null)
                                cascade.detectMultiScale(grayMat4Thread, faces, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                                    new Size(grayMat4Thread.height() * 0.2, grayMat4Thread.height() * 0.2), new Size());

                            resultDetect = faces;
                        }
                        comm.shouldDetectInMultiThread = false;

                        didUpdateTheDetectionResult = true;
                    }
                }


#endif
#else

				private void ThreadWorker ()
				{
						if (isThreadRunning)
								return;

						Debug.Log ("Thread Start");

						isThreadRunning = true;
						shouldStopThread = false;

						threadComm.shouldDetectInMultiThread = false;
						didUpdateTheDetectionResult = false;

						ThreadPool.QueueUserWorkItem (_ThreadWorker, threadComm);
				}

				private void _ThreadWorker (System.Object o)
				{
						ThreadComm comm = o as ThreadComm;


						while (!shouldStopThread) {
								if (!comm.shouldDetectInMultiThread)
										continue;


								lock (thisLock) {
										MatOfRect faces = new MatOfRect ();
										if (cascade != null)
												cascade.detectMultiScale (grayMat4Thread, faces, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                                    new Size (grayMat4Thread.height () * 0.2, grayMat4Thread.height () * 0.2), new Size ());

										resultDetect = faces;
								}
								comm.shouldDetectInMultiThread = false;

								didUpdateTheDetectionResult = true;
						}

						isThreadRunning = false;
				}


#endif

				public class ThreadComm : System.Object
				{
						public bool shouldDetectInMultiThread = false;
				}



				public void StartThread ()
				{
						ThreadWorker ();
				}

				public void StopThread ()
				{
						if (!isThreadRunning)
								return;


#if UNITY_WSA
#if NETFX_CORE
			tokenSource_.Cancel();
            task_ = null;
#else
            StopCoroutine("ThreadCoroutine");
            isThreadRunning = false;
#endif
#else
						shouldStopThread = true;
#endif


						while (isThreadRunning) {
								//Wait threading stop
						}
						Debug.Log ("Thread Stop");
				}

				private void getObjects (List<Rect> result)
				{
						result.Clear ();

						for (int i = 0; i < trackedObjects.Count; i++) {
								Rect r = calcTrackedObjectPositionToShow (i);
								if (r.area () == 0) {
										continue;
								}
								result.Add (r);
								//LOGD("DetectionBasedTracker::process: found a object with SIZE %d x %d, rect={%d, %d, %d x %d}", r.width, r.height, r.x, r.y, r.width, r.height);
						}
				}


				public enum TrackedState : int
				{
						NEW_RECTANGLE = -1,
						INTERSECTED_RECTANGLE = -2
				}

				private void updateTrackedObjects (List<Rect> detectedObjects)
				{
						int N1 = (int)trackedObjects.Count;
						int N2 = (int)detectedObjects.Count;

						for (int i = 0; i < N1; i++) {
								trackedObjects [i].numDetectedFrames++;
						}

						int[] correspondence = new int[N2];
						for (int i = 0; i < N2; i++) {
								correspondence [i] = (int)TrackedState.NEW_RECTANGLE;
						}


						for (int i = 0; i < N1; i++) {
								TrackedObject curObject = trackedObjects [i];

								int bestIndex = -1;
								int bestArea = -1;

								int numpositions = (int)curObject.lastPositions.Count;

								//if (numpositions > 0) UnityEngine.Debug.LogError("numpositions > 0 is false");

								Rect prevRect = curObject.lastPositions [numpositions - 1];

								for (int j = 0; j < N2; j++) {
										if (correspondence [j] >= 0) {
												//Debug.Log("DetectionBasedTracker::updateTrackedObjects: j=" + i + " is rejected, because it has correspondence=" + correspondence[j]);
												continue;
										}
										if (correspondence [j] != (int)TrackedState.NEW_RECTANGLE) {
												//Debug.Log("DetectionBasedTracker::updateTrackedObjects: j=" + j + " is rejected, because it is intersected with another rectangle");
												continue;
										}

										Rect r = Intersect (prevRect, detectedObjects [j]);
										if ((r.width > 0) && (r.height > 0)) {
												//LOGD("DetectionBasedTracker::updateTrackedObjects: There is intersection between prevRect and detectedRect, r={%d, %d, %d x %d}",
												//        r.x, r.y, r.width, r.height);
												correspondence [j] = (int)TrackedState.INTERSECTED_RECTANGLE;

												if (r.area () > bestArea) {
														//LOGD("DetectionBasedTracker::updateTrackedObjects: The area of intersection is %d, it is better than bestArea=%d", r.area(), bestArea);
														bestIndex = j;
														bestArea = (int)r.area ();
												}
										}
								}

								if (bestIndex >= 0) {
										//LOGD("DetectionBasedTracker::updateTrackedObjects: The best correspondence for i=%d is j=%d", i, bestIndex);
										correspondence [bestIndex] = i;

										for (int j = 0; j < N2; j++) {
												if (correspondence [j] >= 0)
														continue;

												Rect r = Intersect (detectedObjects [j], detectedObjects [bestIndex]);
												if ((r.width > 0) && (r.height > 0)) {
														//LOGD("DetectionBasedTracker::updateTrackedObjects: Found intersection between "
														//    "rectangles j=%d and bestIndex=%d, rectangle j=%d is marked as intersected", j, bestIndex, j);
														correspondence [j] = (int)TrackedState.INTERSECTED_RECTANGLE;
												}
										}
								} else {
										//LOGD("DetectionBasedTracker::updateTrackedObjects: There is no correspondence for i=%d ", i);
										curObject.numFramesNotDetected++;
								}
						}

						//LOGD("DetectionBasedTracker::updateTrackedObjects: start second cycle");
						for (int j = 0; j < N2; j++) {
								int i = correspondence [j];
								if (i >= 0) {//add position
										//Debug.Log("DetectionBasedTracker::updateTrackedObjects: add position");
										trackedObjects [i].lastPositions.Add (detectedObjects [j]);
										while ((int)trackedObjects[i].lastPositions.Count > (int)innerParameters.numLastPositionsToTrack) {
												trackedObjects [i].lastPositions.Remove (trackedObjects [i].lastPositions [0]);
										}
										trackedObjects [i].numFramesNotDetected = 0;
								} else if (i == (int)TrackedState.NEW_RECTANGLE) { //new object
										//Debug.Log("DetectionBasedTracker::updateTrackedObjects: new object");
										trackedObjects.Add (new TrackedObject (detectedObjects [j]));
								} else {
										Debug.Log ("DetectionBasedTracker::updateTrackedObjects: was auxiliary intersection");
								}
						}

						int t = 0;
						TrackedObject it;
						while (t < trackedObjects.Count) {
								it = trackedObjects [t];

								if ((it.numFramesNotDetected > parameters.maxTrackLifetime)
        ||
										((it.numDetectedFrames <= innerParameters.numStepsToWaitBeforeFirstShow)
            &&
										(it.numFramesNotDetected > innerParameters.numStepsToTrackWithoutDetectingIfObjectHasNotBeenShown))
        ) {
										//int numpos = (int)it.lastPositions.Count;
										//if (numpos > 0) UnityEngine.Debug.LogError("numpos > 0 is false");
										//Rect r = it.lastPositions [numpos - 1];

										//Debug.Log("DetectionBasedTracker::updateTrackedObjects: deleted object " + r.x + " " + r.y + " " + r.width + " " + r.height);

										trackedObjects.Remove (it);

								} else {
										t++;
								}
						}
				}

				private void detectInRegion (Mat img, Rect r, List<Rect> detectedObjectsInRegions)
				{
						Rect r0 = new Rect (new Point (), img.size ());
						Rect r1 = new Rect (r.x, r.y, r.width, r.height);
						Inflate (r1, (int)((r1.width * innerParameters.coeffTrackingWindowSize) - r1.width) / 2,
            (int)((r1.height * innerParameters.coeffTrackingWindowSize) - r1.height) / 2);
						r1 = Intersect (r0, r1);

						if ((r1.width <= 0) || (r1.height <= 0)) {
								Debug.Log ("DetectionBasedTracker::detectInRegion: Empty intersection");
								return;
						}


						int d = Math.Min (r.width, r.height);
						d = (int)Math.Round (d * innerParameters.coeffObjectSizeToTrack);


						MatOfRect tmpobjects = new MatOfRect ();

						Mat img1 = new Mat (img, r1);//subimage for rectangle -- without data copying

						regionCascade.detectMultiScale (img1, tmpobjects, parameters.scaleFactor, parameters.minNeighbors, 0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size (d, d), new Size ());


						Rect[] tmpobjectsArray = tmpobjects.toArray ();
						int len = tmpobjectsArray.Length;
						for (int i = 0; i < len; i++) {
								Rect tmp = tmpobjectsArray [i];
								Rect curres = new Rect (new Point (tmp.x + r1.x, tmp.y + r1.y), tmp.size ());
								detectedObjectsInRegions.Add (curres);
						}
				}

				private Rect calcTrackedObjectPositionToShow (int i)
				{
						if ((i < 0) || (i >= trackedObjects.Count)) {
								Debug.Log ("DetectionBasedTracker::calcTrackedObjectPositionToShow: ERROR: wrong i=" + i);
								return new Rect ();
						}
						if (trackedObjects [i].numDetectedFrames <= innerParameters.numStepsToWaitBeforeFirstShow) {
								//Debug.Log("DetectionBasedTracker::calcTrackedObjectPositionToShow: " + "trackedObjects[" + i + "].numDetectedFrames=" + trackedObjects[i].numDetectedFrames + " <= numStepsToWaitBeforeFirstShow=" + innerParameters.numStepsToWaitBeforeFirstShow + " --- return empty Rect()");
								return new Rect ();
						}
						if (trackedObjects [i].numFramesNotDetected > innerParameters.numStepsToShowWithoutDetecting) {
								return new Rect ();
						}

						List<Rect> lastPositions = trackedObjects [i].lastPositions;

						int N = lastPositions.Count;
						if (N <= 0) {
								Debug.Log ("DetectionBasedTracker::calcTrackedObjectPositionToShow: ERROR: no positions for i=" + i);
								return new Rect ();
						}

						int Nsize = Math.Min (N, (int)weightsSizesSmoothing.Count);
						int Ncenter = Math.Min (N, (int)weightsPositionsSmoothing.Count);

						Point center = new Point ();
						double w = 0, h = 0;
						if (Nsize > 0) {
								double sum = 0;
								for (int j = 0; j < Nsize; j++) {
										int k = N - j - 1;
										w += lastPositions [k].width * weightsSizesSmoothing [j];
										h += lastPositions [k].height * weightsSizesSmoothing [j];
										sum += weightsSizesSmoothing [j];
								}
								w /= sum;
								h /= sum;
						} else {
								w = lastPositions [N - 1].width;
								h = lastPositions [N - 1].height;
						}

						if (Ncenter > 0) {
								double sum = 0;
								for (int j = 0; j < Ncenter; j++) {
										int k = N - j - 1;
										Point tl = lastPositions [k].tl ();
										Point br = lastPositions [k].br ();
										Point c1;
										//c1=tl;
										//c1=c1* 0.5f;//
										c1 = new Point (tl.x * 0.5f, tl.y * 0.5f);
										Point c2;
										//c2=br;
										//c2=c2*0.5f;
										c2 = new Point (br.x * 0.5f, br.y * 0.5f);
										//c1=c1+c2;
										c1 = new Point (c1.x + c2.x, c1.y + c2.y);

										//center=center+  (c1  * weightsPositionsSmoothing[j]);
										center = new Point (center.x + (c1.x * weightsPositionsSmoothing [j]), center.y + (c1.y * weightsPositionsSmoothing [j]));
										sum += weightsPositionsSmoothing [j];
								}
								//center *= (float)(1 / sum);
								center = new Point (center.x * (1 / sum), center.y * (1 / sum));
						} else {
								int k = N - 1;
								Point tl = lastPositions [k].tl ();
								Point br = lastPositions [k].br ();
								Point c1;
								//c1=tl;
								//c1=c1* 0.5f;
								c1 = new Point (tl.x * 0.5f, tl.y * 0.5f);
								Point c2;
								//c2=br;
								//c2=c2*0.5f;
								c2 = new Point (br.x * 0.5f, br.y * 0.5f);

								//center=c1+c2;
								center = new Point (c1.x + c2.x, c1.y + c2.y);
						}
						//Point2f tl=center-(Point2f(w,h)*0.5);
						Point tl2 = new Point (center.x - (w * 0.5f), center.y - (h * 0.5f));
						//Rect res(cvRound(tl.x), cvRound(tl.y), cvRound(w), cvRound(h));
						Rect res = new Rect ((int)Math.Round (tl2.x), (int)Math.Round (tl2.y), (int)Math.Round (w), (int)Math.Round (h));
						//LOGD("DetectionBasedTracker::calcTrackedObjectPositionToShow: Result for i=%d: {%d, %d, %d x %d}", i, res.x, res.y, res.width, res.height);

						return res;
				}

				public Point centerRect (Rect r)
				{
						return new Point (r.x + (r.width / 2), r.y + (r.height / 2));
				}

				public Rect Intersect (Rect a, Rect b)
				{
						int x1 = Math.Max (a.x, b.x);
						int x2 = Math.Min (a.x + a.width, b.x + b.width);
						int y1 = Math.Max (a.y, b.y);
						int y2 = Math.Min (a.y + a.height, b.y + b.height);

						if (x2 >= x1 && y2 >= y1)
								return new Rect (x1, y1, x2 - x1, y2 - y1);
						else
								return new Rect ();
				}

				public Rect Inflate (Rect rect, int x, int y)
				{
						rect.x -= x;
						rect.y -= y;
						rect.width += (2 * x);
						rect.height += (2 * y);
						return rect;
				}

				void OnDisable ()
				{
						StopThread ();

						webCamTexture.Stop ();
				}

				void OnGUI ()
				{
                        float screenScale = Screen.height / 240.0f;
						Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
						GUI.matrix = scaledMatrix;


						GUILayout.BeginVertical ();
						if (GUILayout.Button ("back")) {
                
								Application.LoadLevel ("OpenCVForUnitySample");

						}
						if (GUILayout.Button ("change camera")) {

                                shouldUseFrontFacing = !shouldUseFrontFacing;
								StartCoroutine (init ());

						}

						GUILayout.EndVertical ();
				}

				public class Parameters
				{
						public int minObjectSize = 96;
						public int maxObjectSize = int.MaxValue;
						public double scaleFactor = 1.1;
						public int maxTrackLifetime = 2;//5
						public int minNeighbors = 2;
						public int minDetectionPeriod = 0; //the minimal time between run of the big object detector (on the whole frame) in ms (1000 mean 1 sec), default=0

						public Parameters ()
						{
						}
				};

				public class InnerParameters
				{
						public int numLastPositionsToTrack = 4;
						public int numStepsToWaitBeforeFirstShow = 6;
						public int numStepsToTrackWithoutDetectingIfObjectHasNotBeenShown = 3;
						public int numStepsToShowWithoutDetecting = 3;
						public float coeffTrackingWindowSize = 2.0f;//2.0f
						public float coeffObjectSizeToTrack = 0.85f;
						public float coeffObjectSpeedUsingInPrediction = 0.8f;

						public InnerParameters ()
						{
						}
				};

				public class TrackedObject
				{
						public PositionsVector lastPositions;
						public int numDetectedFrames;
						public int numFramesNotDetected;
						public int id;
						static private int _id = 0;

						public TrackedObject (OpenCVForUnity.Rect rect)
						{
								lastPositions = new PositionsVector ();

								numDetectedFrames = 1;
								numFramesNotDetected = 0;

								lastPositions.Add (rect);

								_id = getNextId ();
								id = _id;
						}

						static int getNextId ()
						{
								_id++;
								return _id;
						}
				}
		}
}
