using System;

namespace OpenCVForUnityExample.MOT.ByteTrack
{
    public interface IRectBase
    {
        float Top { get; }
        float Left { get; }
        float Width { get; }
        float Height { get; }
    }

    public static class RectOperations
    {
        public static float CalcIoU(IRectBase A, IRectBase B)
        {
            float box_area = (B.Width + 1) * (B.Height + 1);
            float iw = Math.Min(A.Left + A.Width, B.Left + B.Width) - Math.Max(A.Left, B.Left) + 1;
            float iou = 0;
            if (iw > 0)
            {
                float ih = Math.Min(A.Top + A.Height, B.Top + B.Height) - Math.Max(A.Top, B.Top) + 1;
                if (ih > 0)
                {
                    float ua = (A.Width + 1) * (A.Height + 1) + box_area - iw * ih;
                    iou = iw * ih / ua;
                }
            }
            return iou;
        }
    }

    public class TlwhRect : IRectBase
    {
        private float _top;
        private float _left;
        private float _width;
        private float _height;

        public TlwhRect(float top = 0, float left = 0, float width = 0, float height = 0)
        {
            _top = top;
            _left = left;
            _width = width;
            _height = height;
        }

        public TlwhRect(IRectBase other)
        {
            _top = other.Top;
            _left = other.Left;
            _width = other.Width;
            _height = other.Height;
        }

        public float Top => _top;
        public float Left => _left;
        public float Width => _width;
        public float Height => _height;

        public override string ToString()
        {
            return "[" + Left + ", " + Top + ", " + Width + ", " + Height + ", " + "]";
        }
    }
}
