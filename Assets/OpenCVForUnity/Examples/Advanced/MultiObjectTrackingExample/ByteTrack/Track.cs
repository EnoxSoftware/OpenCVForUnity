namespace OpenCVForUnityExample.MOT.ByteTrack
{
    public enum TrackState
    {
        Tracked = 0,
        Lost = 1,
        Removed = 2,
    }

    public class Track
    {
        public Detection Detection { get; private set; }
        public TlwhRect PredictedRect { get; private set; }

        private KalmanFilter _kalmanFilter;

        public Track(Detection detection, int start_frame_id, int track_id)
        {
            Detection = detection;
            PredictedRect = (TlwhRect)detection.Rect;
            _kalmanFilter = new KalmanFilter();
            DetectionState = TrackState.Tracked;
            // Detections registered on first frame are considered as confirmed
            IsConfirmed = start_frame_id == 1;
            TrackId = track_id;
            FrameId = start_frame_id;
            StartFrameId = start_frame_id;
            TrackletLength = 0;

            _kalmanFilter.Initiate(detection.Rect);
        }

        public TrackState DetectionState { get; private set; }
        public bool IsConfirmed { get; private set; }
        public int TrackId { get; }
        public int FrameId { get; private set; }
        public int StartFrameId { get; }
        public int TrackletLength { get; private set; }

        public void Predict()
        {
            PredictedRect = (TlwhRect)_kalmanFilter.Predict(DetectionState != TrackState.Tracked);
            Detection.SetRect(PredictedRect);
        }

        public void Update(Detection matched_detection, int frame_id)
        {
            PredictedRect = (TlwhRect)_kalmanFilter.Update(matched_detection.Rect);
            Detection.SetRect(PredictedRect);
            Detection.SetScore(matched_detection.Score);

            if (DetectionState == TrackState.Tracked)
            {
                TrackletLength++;
            }
            else
            {
                DetectionState = TrackState.Tracked;
                TrackletLength = 0;
            }
            IsConfirmed = true;
            FrameId = frame_id;
        }

        public void MarkAsLost()
        {
            DetectionState = TrackState.Lost;
        }

        public void MarkAsRemoved()
        {
            DetectionState = TrackState.Removed;
        }

        public override string ToString()
        {
            return "OT_" + TrackId + "_(" + StartFrameId + "-" + FrameId + ")";
        }
    }
}
