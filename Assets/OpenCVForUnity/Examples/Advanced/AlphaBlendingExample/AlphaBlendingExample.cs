//using Unity.IL2CPP.CompilerServices;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    ///
    /// ######
    /// </summary>
    public class AlphaBlendingExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Image size type enum
        /// </summary>
        public enum ImageSizeType
        {
            Original,
            Large,
            Small
        }

        // Public Fields
        /// <summary>
        /// The image size.
        /// </summary>
        public ImageSizeType ImageSize = ImageSizeType.Original;

        /// <summary>
        /// The count dropdown.
        /// </summary>
        public Dropdown ImageSizeDropdown;

        /// <summary>
        /// The count.
        /// </summary>
        public int Count = 100;

        /// <summary>
        /// The image size dropdown.
        /// </summary>
        public Dropdown CountDropdown;

        /// <summary>
        /// The foreground quad mesh renderer.
        /// </summary>
        public MeshRenderer FgQuad;

        /// <summary>
        /// The background quad mesh renderer.
        /// </summary>
        public MeshRenderer BgQuad;

        /// <summary>
        /// The alpha quad mesh renderer.
        /// </summary>
        public MeshRenderer AlphaQuad;

        /// <summary>
        /// The destination quad mesh renderer.
        /// </summary>
        public MeshRenderer DstQuad;

        // Private Fields
        private Texture2D _fgTex;
        private Texture2D _bgTex;
        private Texture2D _alphaTex;
        private Texture2D _dstTex;

        private Mat _fgMat;
        private Mat _bgMat;
        private Mat _alphaMat;
        private Mat _dstMat;

        private Mat _fgMatLarge;
        private Mat _bgMatLarge;
        private Mat _alphaMatLarge;
        private Mat _dstMatLarge;

        private Mat _fgMatROI;
        private Mat _bgMatROI;
        private Mat _alphaMatROI;
        private Mat _dstMatROI;

        private Mat _currentFgMat;
        private Mat _currentBgMat;
        private Mat _currentAlphaMat;
        private Mat _currentDstMat;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            ImageSizeDropdown.value = (int)ImageSize;
            CountDropdown.value = 2;

            _fgTex = Resources.Load("face") as Texture2D;
            _bgTex = new Texture2D(_fgTex.width, _fgTex.height, TextureFormat.RGBA32, false);
            _alphaTex = new Texture2D(_fgTex.width, _fgTex.height, TextureFormat.RGBA32, false);
            _dstTex = new Texture2D(_fgTex.width, _fgTex.height, TextureFormat.RGBA32, false);

            _fgMat = new Mat(_fgTex.height, _fgTex.width, CvType.CV_8UC3);
            _bgMat = new Mat(_fgTex.height, _fgTex.width, CvType.CV_8UC3);
            _alphaMat = new Mat(_fgTex.height, _fgTex.width, CvType.CV_8UC1);
            _dstMat = new Mat(_fgTex.height, _fgTex.width, CvType.CV_8UC3, new Scalar(0, 0, 0));

            // Generate fgMat.
            OpenCVMatUtils.Texture2DToMat(_fgTex, _fgMat);

            // Generate bgMat.
            Core.flip(_fgMat, _bgMat, 1);
            Core.bitwise_not(_bgMat, _bgMat);

            // Generate alphaMat.
            for (int r = 0; r < _alphaMat.rows(); r++)
            {
                _alphaMat.row(r).setTo(new Scalar(r / (_alphaMat.rows() / 256)));
            }
#pragma warning disable 0618
            Imgproc.linearPolar(_alphaMat, _alphaMat, new Point(_alphaMat.cols() / 2, _alphaMat.rows() / 2), _alphaMat.rows(), Imgproc.INTER_CUBIC | Imgproc.WARP_FILL_OUTLIERS | Imgproc.WARP_INVERSE_MAP);
#pragma warning restore 0618

            // Generate large size Mat.
            _fgMatLarge = new Mat();
            _bgMatLarge = new Mat();
            _alphaMatLarge = new Mat();
            _dstMatLarge = new Mat();
            Imgproc.resize(_fgMat, _fgMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(_bgMat, _bgMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(_alphaMat, _alphaMatLarge, new Size(), 2, 2, 0);
            Imgproc.resize(_dstMat, _dstMatLarge, new Size(), 2, 2, 0);

            // Generate small size Mat (ROI).
            OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect(127, 127, 256, 256);
            _fgMatROI = new Mat(_fgMat, rect);
            _bgMatROI = new Mat(_bgMat, rect);
            _alphaMatROI = new Mat(_alphaMat, rect);
            _dstMatROI = new Mat(_dstMat, rect);

            OpenCVMatUtils.MatToTexture2D(_fgMat, _fgTex);
            OpenCVMatUtils.MatToTexture2D(_bgMat, _bgTex);
            OpenCVMatUtils.MatToTexture2D(_alphaMat, _alphaTex);
            OpenCVMatUtils.MatToTexture2D(_dstMat, _dstTex);
            FgQuad.GetComponent<Renderer>().material.mainTexture = _fgTex;
            BgQuad.GetComponent<Renderer>().material.mainTexture = _bgTex;
            AlphaQuad.GetComponent<Renderer>().material.mainTexture = _alphaTex;
            DstQuad.GetComponent<Renderer>().material.mainTexture = _dstTex;
        }

        // Private Methods
        private IEnumerator AlphaBlending(Action action, int count = 100)
        {
            _dstMat.setTo(new Scalar(0, 0, 0));
            OpenCVMatUtils.MatToTexture2D(_dstMat, _dstTex);

            yield return null;

            switch (ImageSize)
            {
                default:
                case ImageSizeType.Original:
                    _currentFgMat = _fgMat;
                    _currentBgMat = _bgMat;
                    _currentAlphaMat = _alphaMat;
                    _currentDstMat = _dstMat;
                    break;
                case ImageSizeType.Large:
                    _currentFgMat = _fgMatLarge;
                    _currentBgMat = _bgMatLarge;
                    _currentAlphaMat = _alphaMatLarge;
                    _currentDstMat = _dstMatLarge;
                    break;
                case ImageSizeType.Small:
                    _currentFgMat = _fgMatROI;
                    _currentBgMat = _bgMatROI;
                    _currentAlphaMat = _alphaMatROI;
                    _currentDstMat = _dstMatROI;
                    break;
            }

            long ms = Time(action, count);

            if (ImageSize == ImageSizeType.Large)
                Imgproc.resize(_dstMatLarge, _dstMat, new Size(), 1.0 / 2.0, 1.0 / 2.0, 0);

            OpenCVMatUtils.MatToTexture2D(_dstMat, _dstTex);

#if UNITY_WSA && ENABLE_DOTNET
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = ImageSize + " : " + count + " : " + ms + " ms";
            }
            Debug.Log(ImageSize + " : " + count + " : " + ms + " ms");
#else
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = ImageSize + " : " + count + " : " + action.Method.Name + " : " + ms + " ms";
            }
            Debug.Log(ImageSize + " : " + count + " : " + action.Method.Name + " : " + ms + " ms");
#endif
        }

        private void Getput()
        {
            AlphaBlend_getput(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void MatOp()
        {
            AlphaBlend_matOp(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void MatOp_alpha3c()
        {
            AlphaBlend_matOp_alpha3c(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void CopyFromMat()
        {
            AlphaBlend_copyFromMat(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void MarshalMethod()
        {
            AlphaBlend_MarshalMethod(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void PointerAccess()
        {
            AlphaBlend_pointerAccess(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private void AsSpan()
        {
            AlphaBlend_AsSpan(_currentFgMat, _currentBgMat, _currentAlphaMat, _currentDstMat);
        }

        private long Time(Action action, int count)
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
            byte[] dst_byte = new byte[dst.total() * dst.channels()];

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                byte a = alpha_byte[i];
                dst_byte[pixel_i] = (byte)((fg_byte[pixel_i] * a + bg_byte[pixel_i] * (255 - a)) >> 8);
                dst_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * a + bg_byte[pixel_i + 1] * (255 - a)) >> 8);
                dst_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * a + bg_byte[pixel_i + 2] * (255 - a)) >> 8);
                pixel_i += channels;
            }

            dst.put(0, 0, dst_byte);
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

        // OpenCVMatUtils.copyFromMat
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_copyFromMat(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            OpenCVMatUtils.CopyFromMat<byte>(fg, fg_byte);
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            OpenCVMatUtils.CopyFromMat<byte>(bg, bg_byte);
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            OpenCVMatUtils.CopyFromMat<byte>(alpha, alpha_byte);
            byte[] dst_byte = new byte[dst.total() * dst.channels()];

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                byte a = alpha_byte[i];
                dst_byte[pixel_i] = (byte)((fg_byte[pixel_i] * a + bg_byte[pixel_i] * (255 - a)) >> 8);
                dst_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * a + bg_byte[pixel_i + 1] * (255 - a)) >> 8);
                dst_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * a + bg_byte[pixel_i + 2] * (255 - a)) >> 8);
                pixel_i += channels;
            }

            OpenCVMatUtils.CopyToMat(dst_byte, dst);
        }

        // MarshalMethod
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_MarshalMethod(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            byte[] dst_byte = new byte[dst.total() * dst.channels()];

            long fg_addr = fg.dataAddr();
            long bg_addr = bg.dataAddr();
            long alpha_addr = alpha.dataAddr();
            long dst_addr = dst.dataAddr();

            if (fg.isContinuous())
            {
                Marshal.Copy(new IntPtr(fg_addr), fg_byte, 0, fg_byte.Length);
                Marshal.Copy(new IntPtr(bg_addr), bg_byte, 0, bg_byte.Length);
                Marshal.Copy(new IntPtr(alpha_addr), alpha_byte, 0, alpha_byte.Length);
            }
            else
            {
                long stride = bg.step1() * bg.elemSize1();
                int w = bg.cols();
                int w_c = bg.cols() * bg.channels();
                int h = bg.rows();
                long alpha_stride = alpha.step1() * alpha.elemSize1();
                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(new IntPtr(fg_addr), fg_byte, y * w_c, w_c);
                    Marshal.Copy(new IntPtr(bg_addr), bg_byte, y * w_c, w_c);
                    Marshal.Copy(new IntPtr(alpha_addr), alpha_byte, y * w, w);

                    fg_addr += stride;
                    bg_addr += stride;
                    alpha_addr += alpha_stride;
                }
            }

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                byte a = alpha_byte[i];
                dst_byte[pixel_i] = (byte)((fg_byte[pixel_i] * a + bg_byte[pixel_i] * (255 - a)) >> 8);
                dst_byte[pixel_i + 1] = (byte)((fg_byte[pixel_i + 1] * a + bg_byte[pixel_i + 1] * (255 - a)) >> 8);
                dst_byte[pixel_i + 2] = (byte)((fg_byte[pixel_i + 2] * a + bg_byte[pixel_i + 2] * (255 - a)) >> 8);
                pixel_i += channels;
            }

            if (fg.isContinuous())
            {
                Marshal.Copy(dst_byte, 0, new IntPtr(dst_addr), dst_byte.Length);
            }
            else
            {
                dst_addr = dst.dataAddr();

                long stride = bg.step1() * bg.elemSize1();
                int w_c = bg.cols() * bg.channels();
                int h = bg.rows();
                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(dst_byte, y * w_c, new IntPtr(dst_addr), w_c);

                    dst_addr += stride;
                }
            }
        }

        // pointer access
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_pointerAccess(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
#if !OPENCV_DONT_USE_UNSAFE_CODE

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
                long stride = bg.step1() * bg.elemSize1();
                int w = bg.cols();
                int w_c = bg.cols() * bg.channels();
                int h = bg.rows();
                long alpha_stride = alpha.step1() * alpha.elemSize1();

                unsafe
                {
                    byte* fg_p = (byte*)fg_ptr;
                    byte* bg_p = (byte*)bg_ptr;
                    byte* alpha_p = (byte*)alpha_ptr;
                    byte* dst_p = (byte*)dst_ptr;

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;
                            *dst_p = (byte)(((*fg_p) * (*alpha_p) + (*bg_p) * (255 - *alpha_p)) >> 8);
                            fg_p++; bg_p++; dst_p++;

                            alpha_p++;
                        }

                        fg_p += stride - w_c;
                        bg_p += stride - w_c;
                        alpha_p += alpha_stride - w;
                        dst_p += stride - w_c;
                    }
                }
            }

#endif
        }

        // AsSpan
        //        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        //        [Il2CppSetOption(Option.NullChecks, false)]
        private void AlphaBlend_AsSpan(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

            if (fg.isContinuous())
            {
                ReadOnlySpan<Vec3b> fg_span = fg.AsSpan<Vec3b>();
                ReadOnlySpan<Vec3b> bg_span = bg.AsSpan<Vec3b>();
                ReadOnlySpan<byte> alpha_span = alpha.AsSpan<byte>();
                Span<Vec3b> dst_span = dst.AsSpan<Vec3b>();

                int total = (int)bg.total();

                for (int i = 0; i < total; i++)
                {
                    ref readonly var fg_pixel = ref fg_span[i];
                    ref readonly var bg_pixel = ref bg_span[i];
                    ref readonly var alpha_pixel = ref alpha_span[i];
                    ref var dst_pixel = ref dst_span[i];
                    dst_pixel.Item1 = (byte)((fg_pixel.Item1 * alpha_pixel + bg_pixel.Item1 * (255 - alpha_pixel)) >> 8);
                    dst_pixel.Item2 = (byte)((fg_pixel.Item2 * alpha_pixel + bg_pixel.Item2 * (255 - alpha_pixel)) >> 8);
                    dst_pixel.Item3 = (byte)((fg_pixel.Item3 * alpha_pixel + bg_pixel.Item3 * (255 - alpha_pixel)) >> 8);
                }

                /*
                // Row-by-row parallel processing method
                int w = bg.cols();
                int h = bg.rows();

                System.Threading.Tasks.Parallel.For(0, h, y =>
                {
                    ReadOnlySpan<Vec3b> fg_span = fg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<Vec3b> bg_span = bg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<byte> alpha_span = alpha.AsSpan<byte>(y);
                    Span<Vec3b> dst_span = dst.AsSpan<Vec3b>(y);

                    for (int i = 0; i < w; i++)
                    {
                        ref readonly var fg_pixel = ref fg_span[i];
                        ref readonly var bg_pixel = ref bg_span[i];
                        ref readonly var alpha_pixel = ref alpha_span[i];
                        ref var dst_pixel = ref dst_span[i];
                        dst_pixel.Item1 = (byte)((fg_pixel.Item1 * alpha_pixel + bg_pixel.Item1 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item2 = (byte)((fg_pixel.Item2 * alpha_pixel + bg_pixel.Item2 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item3 = (byte)((fg_pixel.Item3 * alpha_pixel + bg_pixel.Item3 * (255 - alpha_pixel)) >> 8);
                    }
                });
                */
            }
            else
            {
                int w = bg.cols();
                int h = bg.rows();

                for (int y = 0; y < h; y++)
                {
                    ReadOnlySpan<Vec3b> fg_span = fg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<Vec3b> bg_span = bg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<byte> alpha_span = alpha.AsSpan<byte>(y);
                    Span<Vec3b> dst_span = dst.AsSpan<Vec3b>(y);

                    for (int x = 0; x < w; x++)
                    {
                        ref readonly var fg_pixel = ref fg_span[x];
                        ref readonly var bg_pixel = ref bg_span[x];
                        ref readonly var alpha_pixel = ref alpha_span[x];
                        ref var dst_pixel = ref dst_span[x];
                        dst_pixel.Item1 = (byte)((fg_pixel.Item1 * alpha_pixel + bg_pixel.Item1 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item2 = (byte)((fg_pixel.Item2 * alpha_pixel + bg_pixel.Item2 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item3 = (byte)((fg_pixel.Item3 * alpha_pixel + bg_pixel.Item3 * (255 - alpha_pixel)) >> 8);
                    }
                }

                /*
                // Row-by-row parallel processing method
                int w = bg.cols();
                int h = bg.rows();

                System.Threading.Tasks.Parallel.For(0, h, y =>
                {
                    ReadOnlySpan<Vec3b> fg_span = fg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<Vec3b> bg_span = bg.AsSpan<Vec3b>(y);
                    ReadOnlySpan<byte> alpha_span = alpha.AsSpan<byte>(y);
                    Span<Vec3b> dst_span = dst.AsSpan<Vec3b>(y);

                    for (int x = 0; x < w; x++)
                    {
                        ref readonly var fg_pixel = ref fg_span[x];
                        ref readonly var bg_pixel = ref bg_span[x];
                        ref readonly var alpha_pixel = ref alpha_span[x];
                        ref var dst_pixel = ref dst_span[x];
                        dst_pixel.Item1 = (byte)((fg_pixel.Item1 * alpha_pixel + bg_pixel.Item1 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item2 = (byte)((fg_pixel.Item2 * alpha_pixel + bg_pixel.Item2 * (255 - alpha_pixel)) >> 8);
                        dst_pixel.Item3 = (byte)((fg_pixel.Item3 * alpha_pixel + bg_pixel.Item3 * (255 - alpha_pixel)) >> 8);
                    }
                });
                */
            }

#endif
        }

        // Public Methods
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
            if ((int)ImageSize != result)
            {
                ImageSize = (ImageSizeType)result;
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
                    Count = 1;
                    break;
                case 1:
                    Count = 10;
                    break;
                case 2:
                    Count = 100;
                    break;
            }
        }

        /// <summary>
        /// Raises the getput button click event.
        /// </summary>
        public void OnGetPutButtonClick()
        {
            StartCoroutine(AlphaBlending(Getput, Count));
        }

        /// <summary>
        /// Raises the MatOp button click event.
        /// </summary>
        public void OnMatOpButtonClick()
        {
            StartCoroutine(AlphaBlending(MatOp, Count));
        }

        /// <summary>
        /// Raises the MatOpAlpha3c button click event.
        /// </summary>
        public void OnMatOpAlpha3cButtonClick()
        {
            StartCoroutine(AlphaBlending(MatOp_alpha3c, Count));
        }

        /// <summary>
        /// Raises the copyFromMat button click event.
        /// </summary>
        public void OnCopyFromMatButtonClick()
        {
            StartCoroutine(AlphaBlending(CopyFromMat, Count));
        }

        /// <summary>
        /// Raises the MarshalMethod button click event.
        /// </summary>
        public void OnMarshalMethodButtonClick()
        {
            StartCoroutine(AlphaBlending(MarshalMethod, Count));
        }

        /// <summary>
        /// Raises the pointer access button click event.
        /// </summary>
        public void OnPointerAccessButtonClick()
        {
#if !OPENCV_DONT_USE_UNSAFE_CODE
            StartCoroutine(AlphaBlending(PointerAccess, Count));
#else
            Debug.LogWarning("Error : \"OPENCV_DONT_USE_UNSAFE_CODE\" is enabled. Please switch the UNSAFE setting. [MenuItem]->[Tools]->[OpenCV for Unity]->[Open Setup Tools]->[Enable Use Unsafe Code]");
            _fpsMonitor.ConsoleText = "Error: \"OPENCV_DONT_USE_UNSAFE_CODE\" is enabled.";
#endif
        }

        /// <summary>
        /// Raises the AsSpan button click event.
        /// </summary>
        public void OnAsSpanButtonClick()
        {
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            StartCoroutine(AlphaBlending(AsSpan, Count));
#else
            Debug.LogWarning("Error : \"NET_STANDARD_2_1\" is disabled. Please switch the Api Compatibility Level to \".NET Standard 2.1\". Edit > Project Settings > Player > Other settings");
            Debug.LogWarning("Error : \"OPENCV_DONT_USE_UNSAFE_CODE\" is enabled. Please switch the UNSAFE setting. [MenuItem]->[Tools]->[OpenCV for Unity]->[Open Setup Tools]->[Enable Use Unsafe Code]");
            _fpsMonitor.ConsoleText = "Error : \"NET_STANDARD_2_1\" is disabled. \"OPENCV_DONT_USE_UNSAFE_CODE\" is enabled.";
#endif
        }
    }
}
