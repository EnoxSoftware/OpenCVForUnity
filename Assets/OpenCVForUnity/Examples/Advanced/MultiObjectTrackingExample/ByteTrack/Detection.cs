using System;

namespace OpenCVForUnityExample.MOT.ByteTrack
{
    public interface IDetectionBase
    {
        IRectBase Rect { get; }
        float Score { get; }

        void SetRect(IRectBase rect);
        void SetScore(float score);
    }

    public class Detection : IDetectionBase
    {
        private TlwhRect _rect;
        private float _score;

        public Detection(TlwhRect rect, float score = 0)
        {
            _rect = rect;
            _score = score;
        }

        public IRectBase Rect => _rect;
        public float Score => _score;

        public void SetRect(IRectBase rect)
        {
            if (rect is TlwhRect tlwhRect)
            {
                _rect = new TlwhRect(tlwhRect);
            }
            else
            {
                // Handle the case when a different type of rect is passed
                throw new ArgumentException("Invalid rectangle type");
            }
        }

        public void SetScore(float score = 0)
        {
            _score = score;
        }

        public override string ToString()
        {
            return "[" + Rect + ", " + Score + "]";
        }
    }
}
