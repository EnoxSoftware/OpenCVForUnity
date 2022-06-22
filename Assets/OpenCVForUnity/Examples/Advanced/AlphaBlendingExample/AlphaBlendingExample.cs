using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//using Unity.IL2CPP.CompilerServices;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Alpha Blending Example
    /// An example of alpha blending in multiple ways.
    /// 
    /// ### How to speed up pixel array access. (optional) ###
    /// 
    /// # IL2CPP Compiler options:
    /// [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    /// [Il2CppSetOption(Option.NullChecks, false)]
    /// The runtime checks can be enabled or disabled in C# code using the Il2CppSetOptions attribute. To use this attribute, 
    /// find the Il2CppSetOptionsAttribute.cs source file in the IL2CPP directory in the Unity Editor installation on your computer. 
    /// (Data\il2cpp on Windows, Contents/Frameworks/il2cpp on OS X). Copy this source file into the Assets folder in your project.
    /// https://docs.unity3d.com/Manual/IL2CPP-CompilerOptions.html
    /// 
    /// To use these options, need to uncomment the code that enables the feature. 
    /// 
    /// 
    /// # Pointer acccess. (use -unsafe):
    /// (Unity version 2018.1 or later)
    /// Unsafe code may only appear if compiling with /unsafe. Enable "Allow 'unsafe' code" in Player Settings.
    /// 
    /// (older version)
    /// Unsafe code requires the `unsafe' command-line option to be specified.
    /// You need to add a file "smcs.rsp" (or "gmcs.rsp") in your "Assets" directory, which contains the line: -unsafe
    /// https://answers.unity.com/questions/804103/how-to-enable-unsafe-and-use-pointers.html
    /// 
    /// To use this example,  need to add "OPENCV_USE_UNSAFE_CODE" to Scripting Define Symbols in Player Settings.
    /// 
    /// ######
    /// </summary>

    public class AlphaBlendingExample : MonoBehaviour
    {
        public enum ImageSize
        {
            Original,
            Large,
            Small
        }

        /// <summary>
        /// The image size.
        /// </summary>
        public ImageSize imageSize = ImageSize.Original;

        /// <summary>
        /// The count dropdown.
        /// </summary>
        public Dropdown imageSizeDropdown;

        /// <summary>
        /// The count.
        /// </summary>
        public int count = 100;

        /// <summary>
        /// The image size dropdown.
        /// </summary>
        public Dropdown countDropdown;

        public MeshRenderer fgQuad;
        public MeshRenderer bgQuad;
        public MeshRenderer alphaQuad;
        public MeshRenderer dstQuad;

        Texture2D fgTex;
        Texture2D bgTex;
        Texture2D alphaTex;
        Texture2D dstTex;

        Mat fgMat;
        Mat bgMat;
        Mat alphaMat;
        Mat dstMat;

        Mat fgMatLarge;
        Mat bgMatLarge;
        Mat alphaMatLarge;
        Mat dstMatLarge;

        Mat fgMatROI;
        Mat bgMatROI;
        Mat alphaMatROI;
        Mat dstMatROI;

        Mat _fgMat;
        Mat _bgMat;
        Mat _alphaMat;
        Mat _dstMat;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;


        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            imageSizeDropdown.value = (int)imageSize;
            countDropdown.value = 2;

            fgTex = Resources.Load("face") as Texture2D;
            bgTex = new Texture2D(fgTex.width, fgTex.height, TextureFormat.RGBA32, false);
            alphaTex = new Texture2D(fgTex.width, fgTex.height, TextureFormat.RGBA32, false);
            dstTex = new Texture2D(fgTex.width, fgTex.height, TextureFormat.RGBA32, false);

            fgMat = new Mat(fgTex.height, fgTex.width, CvType.CV_8UC3);
            bgMat = new Mat(fgTex.height, fgTex.width, CvType.CV_8UC3);
            alphaMat = new Mat(fgTex.height, fgTex.width, CvType.CV_8UC1);
            dstMat = new Mat(fgTex.height, fgTex.width, CvType.CV_8UC3, new Scalar(0, 0, 0));


            // Generate fgMat.
            Utils.texture2DToMat(fgTex, fgMat);

            // Generate bgMat.
            Core.flip(fgMat, bgMat, 1);
            Core.bitwise_not(bgMat, bgMat);

            // Generate alphaMat.
            for (int r = 0; r < alphaMat.rows(); r++)
            {
                alphaMat.row(r).setTo(new Scalar(r / (alphaMat.rows() / 256)));
            }
#pragma warning disable 0618
            Imgproc.linearPolar(alphaMat, alphaMat, new Point(alphaMat.cols() / 2, alphaMat.rows() / 2), alphaMat.rows(), Imgproc.INTER_CUBIC | Imgproc.WARP_FILL_OUTLIERS | Imgproc.WARP_INVERSE_MAP);
#pragma warning restore 0618


            // Generate large size Mat.
            fgMatLarge = new Mat();
            bgMatLarge = new Mat();
            alphaMatLarge = new Mat();
            dstMatLarge = new Mat();
            Imgproc.resize(fgMat, fgMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(bgMat, bgMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(alphaMat, alphaMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(dstMat, dstMatLarge, new Size(), 2, 2, 0);

            // Generate small size Mat (ROI).
            OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect(127, 127, 256, 256);
            fgMatROI = new Mat(fgMat, rect);
            bgMatROI = new Mat(bgMat, rect);
            alphaMatROI = new Mat(alphaMat, rect);
            dstMatROI = new Mat(dstMat, rect);


            Utils.matToTexture2D(fgMat, fgTex, true, 0, true);
            Utils.matToTexture2D(bgMat, bgTex, true, 0, true);
            Utils.matToTexture2D(alphaMat, alphaTex, true, 0, true);
            Utils.matToTexture2D(dstMat, dstTex, true, 0, true);
            fgQuad.GetComponent<Renderer>().material.mainTexture = fgTex;
            bgQuad.GetComponent<Renderer>().material.mainTexture = bgTex;
            alphaQuad.GetComponent<Renderer>().material.mainTexture = alphaTex;
            dstQuad.GetComponent<Renderer>().material.mainTexture = dstTex;
        }

        private IEnumerator AlphaBlending(Action action, int count = 100)
        {
            dstMat.setTo(new Scalar(0, 0, 0));
            Utils.matToTexture2D(dstMat, dstTex);

            yield return null;

            switch (imageSize)
            {
                default:
                case ImageSize.Original:
                    _fgMat = fgMat;
                    _bgMat = bgMat;
                    _alphaMat = alphaMat;
                    _dstMat = dstMat;
                    break;
                case ImageSize.Large:
                    _fgMat = fgMatLarge;
                    _bgMat = bgMatLarge;
                    _alphaMat = alphaMatLarge;
                    _dstMat = dstMatLarge;
                    break;
                case ImageSize.Small:
                    _fgMat = fgMatROI;
                    _bgMat = bgMatROI;
                    _alphaMat = alphaMatROI;
                    _dstMat = dstMatROI;
                    break;
            }

            long ms = time(action, count);

            if (imageSize == ImageSize.Large)
                Imgproc.resize(dstMatLarge, dstMat, new Size(), 1.0 / 2.0, 1.0 / 2.0, 0);


            Utils.matToTexture2D(dstMat, dstTex);

#if UNITY_WSA && ENABLE_DOTNET
            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = imageSize + " : " + count + " : " + ms + " ms";
            }
            Debug.Log(imageSize + " : " + count + " : " + ms + " ms");
#else
            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = imageSize + " : " + count + " : " + action.Method.Name + " : " + ms + " ms";
            }
            Debug.Log(imageSize + " : " + count + " : " + action.Method.Name + " : " + ms + " ms");
#endif
        }

        private void getput()
        {
            AlphaBlend_getput(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private void matOp()
        {
            AlphaBlend_matOp(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private void matOp_alpha3c()
        {
            AlphaBlend_matOp_alpha3c(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private void copyFromMat()
        {
            AlphaBlend_copyFromMat(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private void marshal()
        {
            AlphaBlend_Marshal(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private void pointerAccess()
        {
            AlphaBlend_pointerAccess(_fgMat, _bgMat, _alphaMat, _dstMat);
        }

        private long time(Action action, int count)
        {
            System.GC.Collect();

            var tw = new System.Diagnostics.Stopwatch();
            tw.Start();
            for (int i = 0; i < count; i++)
                action();
            tw.Stop();

            System.GC.Collect();

            return tw.ElapsedMilliseconds;
        }

        // mat.get() mat.put()
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_getput(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            fg.get(0, 0, fg_byte);
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            bg.get(0, 0, bg_byte);
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            alpha.get(0, 0, alpha_byte);

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                if (alpha_byte[i] == 0)
                {
                }
                else if (alpha_byte[i] == 255)
                {
                    bg_byte[pixel_i] = fg_byte[pixel_i];
                    bg_byte[pixel_i + 1] = fg_byte[pixel_i + 1];
                    bg_byte[pixel_i + 2] = fg_byte[pixel_i + 2];
                }
                else
                {
                    bg_byte[pixel_i] = (byte)((fg_byte[pixel_i] * alpha_byte[i] + bg_byte[pixel_i] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * alpha_byte[i] + bg_byte[pixel_i + 1] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * alpha_byte[i] + bg_byte[pixel_i + 2] * (255 - alpha_byte[i])) >> 8);
                }
                pixel_i += channels;
            }

            dst.put(0, 0, bg_byte);
        }

        // Mat operation
        private void AlphaBlend_matOp(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            List<Mat> channels = new List<Mat>();

            using (Mat _bg = new Mat())
            using (Mat inv_alpha = new Mat(alpha.width(), alpha.height(), alpha.type()))
            {
                Core.bitwise_not(alpha, inv_alpha);

                Core.split(bg, channels);
                Core.multiply(inv_alpha, channels[0], channels[0], 1.0 / 255);
                Core.multiply(inv_alpha, channels[1], channels[1], 1.0 / 255);
                Core.multiply(inv_alpha, channels[2], channels[2], 1.0 / 255);
                Core.merge(channels, _bg);

                using (Mat _fg = new Mat())
                {
                    Core.split(fg, channels);
                    Core.multiply(alpha, channels[0], channels[0], 1.0 / 255);
                    Core.multiply(alpha, channels[1], channels[1], 1.0 / 255);
                    Core.multiply(alpha, channels[2], channels[2], 1.0 / 255);
                    Core.merge(channels, _fg);

                    Core.add(_fg, _bg, dst);
                }
            }
        }

        // Mat operation (3channel alpha)
        private void AlphaBlend_matOp_alpha3c(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            using (Mat inv_alpha = new Mat(alpha.width(), alpha.height(), alpha.type()))
            using (Mat alpha3c = new Mat())
            using (Mat inv_alpha3c = new Mat())
            {
                List<Mat> channels = new List<Mat>();
                channels.Add(alpha);
                channels.Add(alpha);
                channels.Add(alpha);
                Core.merge(channels, alpha3c);

                Core.bitwise_not(alpha, inv_alpha);

                channels.Clear();
                channels.Add(inv_alpha);
                channels.Add(inv_alpha);
                channels.Add(inv_alpha);
                Core.merge(channels, inv_alpha3c);

                using (Mat _bg = new Mat())
                using (Mat _fg = new Mat())
                {
                    Core.multiply(inv_alpha3c, bg, _bg, 1.0 / 255);
                    Core.multiply(alpha3c, fg, _fg, 1.0 / 255);
                    Core.add(_fg, _bg, dst);
                }
            }
        }

        // MatUtils.copyFromMat
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_copyFromMat(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            MatUtils.copyFromMat<byte>(fg, fg_byte);
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            MatUtils.copyFromMat<byte>(bg, bg_byte);
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            MatUtils.copyFromMat<byte>(alpha, alpha_byte);

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                if (alpha_byte[i] == 0)
                {
                }
                else if (alpha_byte[i] == 255)
                {
                    bg_byte[pixel_i] = fg_byte[pixel_i];
                    bg_byte[pixel_i + 1] = fg_byte[pixel_i + 1];
                    bg_byte[pixel_i + 2] = fg_byte[pixel_i + 2];
                }
                else
                {
                    bg_byte[pixel_i] = (byte)((fg_byte[pixel_i] * alpha_byte[i] + bg_byte[pixel_i] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * alpha_byte[i] + bg_byte[pixel_i + 1] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * alpha_byte[i] + bg_byte[pixel_i + 2] * (255 - alpha_byte[i])) >> 8);
                }
                pixel_i += channels;
            }

            MatUtils.copyToMat(bg_byte, dst);
        }

        // Marshal
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_Marshal(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            IntPtr fg_ptr = new IntPtr(fg.dataAddr());
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            IntPtr bg_ptr = new IntPtr(bg.dataAddr());
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            IntPtr alpha_ptr = new IntPtr(alpha.dataAddr());
            byte[] dst_byte = new byte[dst.total() * dst.channels()];
            IntPtr dst_ptr = new IntPtr(dst.dataAddr());

            if (fg.isContinuous())
            {

                Marshal.Copy(fg_ptr, fg_byte, 0, fg_byte.Length);
                Marshal.Copy(bg_ptr, bg_byte, 0, bg_byte.Length);
                Marshal.Copy(alpha_ptr, alpha_byte, 0, alpha_byte.Length);
                Marshal.Copy(dst_ptr, dst_byte, 0, dst_byte.Length);

            }
            else
            {

                Size wholeSize = new Size();
                Point ofs = new Point();
                bg.locateROI(wholeSize, ofs);

                long stride = (long)wholeSize.width * bg.elemSize();
                int w = bg.cols() * bg.channels();
                int h = bg.rows();
                long alpha_stride = (long)wholeSize.width * alpha.channels();
                int alpha_w = alpha.cols() * alpha.channels();
                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(fg_ptr, fg_byte, y * w, w);
                    Marshal.Copy(bg_ptr, bg_byte, y * w, w);
                    Marshal.Copy(alpha_ptr, alpha_byte, y * alpha_w, alpha_w);
                    Marshal.Copy(dst_ptr, dst_byte, y * w, w);

                    fg_ptr = new IntPtr(fg_ptr.ToInt64() + stride);
                    bg_ptr = new IntPtr(bg_ptr.ToInt64() + stride);
                    alpha_ptr = new IntPtr(alpha_ptr.ToInt64() + alpha_stride);
                    dst_ptr = new IntPtr(dst_ptr.ToInt64() + stride);
                }
            }


            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                if (alpha_byte[i] == 0)
                {
                }
                else if (alpha_byte[i] == 255)
                {
                    bg_byte[pixel_i] = fg_byte[pixel_i];
                    bg_byte[pixel_i + 1] = fg_byte[pixel_i + 1];
                    bg_byte[pixel_i + 2] = fg_byte[pixel_i + 2];
                }
                else
                {
                    bg_byte[pixel_i] = (byte)((fg_byte[pixel_i] * alpha_byte[i] + bg_byte[pixel_i] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * alpha_byte[i] + bg_byte[pixel_i + 1] * (255 - alpha_byte[i])) >> 8);
                    bg_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * alpha_byte[i] + bg_byte[pixel_i + 2] * (255 - alpha_byte[i])) >> 8);
                }
                pixel_i += channels;
            }


            if (fg.isContinuous())
            {
                Marshal.Copy(bg_byte, 0, dst_ptr, bg_byte.Length);
            }
            else
            {
                dst_ptr = new IntPtr(dst.dataAddr());

                Size wholeSize = new Size();
                Point ofs = new Point();
                bg.locateROI(wholeSize, ofs);

                long stride = (long)wholeSize.width * bg.elemSize();
                int w = bg.cols() * bg.channels();
                int h = bg.rows();
                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(bg_byte, y * w, dst_ptr, w);

                    dst_ptr = new IntPtr(dst_ptr.ToInt64() + stride);
                }
            }
        }

        // pointer access
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_pointerAccess(Mat fg, Mat bg, Mat alpha, Mat dst)
        {

#if OPENCV_USE_UNSAFE_CODE

            IntPtr fg_ptr = new IntPtr(fg.dataAddr());
            IntPtr bg_ptr = new IntPtr(bg.dataAddr());
            IntPtr alpha_ptr = new IntPtr(alpha.dataAddr());
            IntPtr dst_ptr = new IntPtr(dst.dataAddr());

            if (fg.isContinuous())
            {
                int total = (int)bg.total();

                unsafe
                {
                    byte* fg_p = (byte*)fg_ptr;
                    byte* bg_p = (byte*)bg_ptr;
                    byte* alpha_p = (byte*)alpha_ptr;
                    byte* dst_p = (byte*)dst_ptr;

                    for (int i = 0; i < total; i++)
                    {
                        *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                        fg_p++; bg_p++; dst_p++;
                        *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                        fg_p++; bg_p++; dst_p++;
                        *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                        fg_p++; bg_p++; dst_p++;

                        alpha_p++;
                    }
                }
            }
            else
            {
                Size wholeSize = new Size();
                Point ofs = new Point();
                bg.locateROI(wholeSize, ofs);

                long stride = (long)wholeSize.width * bg.channels();
                int w = bg.cols() * bg.channels();
                int h = bg.rows();
                long alpha_stride = (long)wholeSize.width * alpha.channels();
                int alpha_w = alpha.cols();

                unsafe
                {
                    byte* fg_p = (byte*)fg_ptr;
                    byte* bg_p = (byte*)bg_ptr;
                    byte* alpha_p = (byte*)alpha_ptr;
                    byte* dst_p = (byte*)dst_ptr;

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < alpha_w; x++)
                        {
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;

                            alpha_p++;
                        }

                        fg_p += stride - w;
                        bg_p += stride - w;
                        alpha_p += alpha_stride - alpha_w;
                        dst_p += stride - w;
                    }
                }
            }

#endif

        }


        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the image size dropdown value changed event.
        /// </summary>
        public void OnImageSizeDropdownValueChanged(int result)
        {
            if ((int)imageSize != result)
            {
                imageSize = (ImageSize)result;

            }
        }

        /// <summary>
        /// Raises the count dropdown value changed event.
        /// </summary>
        public void OnCountDropdownValueChanged(int result)
        {
            switch (result)
            {
                default:
                case 0:
                    count = 1;
                    break;
                case 1:
                    count = 10;
                    break;
                case 2:
                    count = 100;
                    break;
            }
        }

        /// <summary>
        /// Raises the getput button click event.
        /// </summary>
        public void OnGetPutButtonClick()
        {
            StartCoroutine(AlphaBlending(getput, count));
        }

        /// <summary>
        /// Raises the MatOp button click event.
        /// </summary>
        public void OnMatOpButtonClick()
        {
            StartCoroutine(AlphaBlending(matOp, count));
        }

        /// <summary>
        /// Raises the MatOpAlpha3c button click event.
        /// </summary>
        public void OnMatOpAlpha3cButtonClick()
        {
            StartCoroutine(AlphaBlending(matOp_alpha3c, count));
        }

        /// <summary>
        /// Raises the copyFromMat button click event.
        /// </summary>
        public void OnCopyFromMatButtonClick()
        {
            StartCoroutine(AlphaBlending(copyFromMat, count));
        }

        /// <summary>
        /// Raises the Marshal button click event.
        /// </summary>
        public void OnMarshalButtonClick()
        {
            StartCoroutine(AlphaBlending(marshal, count));
        }

        /// <summary>
        /// Raises the pointer access button click event.
        /// </summary>
        public void OnPointerAccessButtonClick()
        {
#if OPENCV_USE_UNSAFE_CODE
            StartCoroutine(AlphaBlending(pointerAccess, count));
#else
            Debug.LogWarning("To use this example,  need to add \"OPENCV_USE_UNSAFE_CODE\" to Scripting Define Symbols in Player Settings. In addition, unsafe code requires the `unsafe' command-line option to be specified.");
#endif
        }
    }
}