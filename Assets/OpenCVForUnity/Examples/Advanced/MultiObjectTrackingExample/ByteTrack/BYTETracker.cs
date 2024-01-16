using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCVForUnityExample.MOT.ByteTrack
{
    /// <summary>
    /// BYTETracker
    /// C# implementation of ByteTrack that does not include an object detection algorithm.
    /// The implementation is based on "ByteTrack-cpp". (https://github.com/derpda/ByteTrack-cpp/)
    /// Only tracking algorithm are implemented.
    /// Any object detection algorithm can be easily combined.
    /// Some code has been modified to obtain the same processing results as the original code below.
    /// https://github.com/ifzhang/ByteTrack/tree/main/deploy/ncnn/cpp
    /// https://github.com/ifzhang/ByteTrack/tree/main/yolox/tracker
    /// </summary>
    public class BYTETracker
    {
        private float track_thresh_;
        private float high_thresh_;
        private float match_thresh_;

        private int max_time_lost_;
        private int frame_id_;
        private int track_id_count_;
        private bool mot20_;

        private List<Track> tracked_tracks_ = new List<Track>();
        private List<Track> lost_tracks_ = new List<Track>();
        private List<Track> removed_tracks_ = new List<Track>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame_rate">frame rate (fps).</param>
        /// <param name="track_buffer">the frames for keep lost tracks.</param>
        /// <param name="track_thresh">tracking confidence threshold.</param>
        /// <param name="high_thresh">confidence threshold for new tracking to be added. (track_thresh + 1)</param>
        /// <param name="match_thresh">matching threshold for tracking.</param>
        /// <param name="mot20">test mot20.</param>
        public BYTETracker(int frame_rate = 30, int track_buffer = 30, float track_thresh = 0.5f,
                            float high_thresh = 0.6f, float match_thresh = 0.8f, bool mot20 = false)
        {
            track_thresh_ = track_thresh;
            high_thresh_ = high_thresh;
            match_thresh_ = match_thresh;
            max_time_lost_ = (int)(frame_rate / 30.0 * track_buffer);
            frame_id_ = 0;
            track_id_count_ = 0;
            mot20_ = mot20;
        }

        public List<Track> Update(List<Detection> input_detections)
        {
            frame_id_++;

            ////////// Step 1: Get detections                                   //////////

            // Sort new tracks from detection by score
            List<Detection> detections = new List<Detection>();
            List<Detection> low_score_detections = new List<Detection>();
            foreach (var detection in input_detections)
            {
                if (detection.Score >= track_thresh_)
                    detections.Add(detection);
                else
                    low_score_detections.Add(detection);
            }

            // Sort existing tracks by confirmed status
            List<Track> confirmed_tracks = new List<Track>();
            List<Track> unconfirmed_tracks = new List<Track>();

            foreach (var track in tracked_tracks_)
            {
                if (!track.IsConfirmed)
                    unconfirmed_tracks.Add(track);
                else
                    confirmed_tracks.Add(track);
            }

            List<Track> track_pool = JointTracks(confirmed_tracks, lost_tracks_);

            // Predict current pose by KF
            foreach (var track in track_pool)
                track.Predict();


            ////////// Step 2: Find matches between tracks and detections       //////////
            ////////// Step 2: First association, with IoU                      //////////
            var associationResult = IOUAssociation(track_pool, detections);
            List<Track> matched_tracks = associationResult.Item1;
            List<Track> unmatched_tracked_tracks = associationResult.Item2;
            List<Detection> unmatched_detections = associationResult.Item3;

            ////////// Step 3: Second association, using low score dets         //////////
            List<Track> new_lost_tracks =
                LowScoreAssociation(matched_tracks, low_score_detections, unmatched_tracked_tracks);

            ////////// Step 4: Init new tracks                                  //////////
            List<Track> new_removed_tracks =
                InitNewTracks(matched_tracks, unconfirmed_tracks, unmatched_detections);

            ////////// Step 5: Update state                                     //////////
            foreach (var lost_track in lost_tracks_)
            {
                if (frame_id_ - lost_track.FrameId > max_time_lost_)
                {
                    lost_track.MarkAsRemoved();
                    new_removed_tracks.Add(lost_track);
                }
            }

            lost_tracks_ = SubTracks(
                JointTracks(SubTracks(lost_tracks_, matched_tracks), new_lost_tracks),
                removed_tracks_);
            removed_tracks_ = JointTracks(removed_tracks_, new_removed_tracks);

            (tracked_tracks_, lost_tracks_) =
                RemoveDuplicateTracks(matched_tracks, lost_tracks_);

            List<Track> output_tracks = new List<Track>();
            foreach (var track in tracked_tracks_)
            {
                if (track.IsConfirmed) output_tracks.Add(track);
            }

            return output_tracks;
        }

        public void Clear()
        {
            tracked_tracks_.Clear();
            lost_tracks_.Clear();
            removed_tracks_.Clear();
            frame_id_ = 0;
            track_id_count_ = 0;
        }

        private (List<Track>, List<Track>, List<Detection>) IOUAssociation(
        List<Track> track_pool, List<Detection> detections)
        {
            var (matches, unmatched_tracks, unmatched_detections) =
                LinearAssignment(track_pool, detections, match_thresh_, !mot20_);

            var matched_tracks = new List<Track>();
            foreach (var match in matches)
            {
                var track = match.Item1;
                var detection = match.Item2;
                track.Update(detection, frame_id_);
                matched_tracks.Add(track);
            }

            var unmatched_tracked_tracks = new List<Track>();
            foreach (var unmatch in unmatched_tracks)
            {
                if (unmatch.DetectionState == TrackState.Tracked)
                {
                    unmatched_tracked_tracks.Add(unmatch);
                }
            }

            return (matched_tracks, unmatched_tracked_tracks, unmatched_detections);
        }

        private List<Track> LowScoreAssociation(
            List<Track> matched_tracks,
            List<Detection> low_score_detections,
            List<Track> unmatched_tracked_tracks)
        {
            var (matches, unmatched_tracks, unmatch_detection) =
                LinearAssignment(unmatched_tracked_tracks, low_score_detections, 0.5f, false);

            foreach (var match in matches)
            {
                var track = match.Item1;
                var detection = match.Item2;
                track.Update(detection, frame_id_);
                matched_tracks.Add(track);
            }

            var new_lost_tracks = new List<Track>();
            foreach (var track in unmatched_tracks)
            {
                if (track.DetectionState != TrackState.Lost)
                {
                    track.MarkAsLost();
                    new_lost_tracks.Add(track);
                }
            }

            return new_lost_tracks;
        }

        private List<Track> InitNewTracks(
            List<Track> matched_tracks,
            List<Track> unconfirmed_tracks,
            List<Detection> unmatched_detections)
        {
            // Deal with unconfirmed tracks, usually tracks with only one beginning frame
            var (matches, unmatched_unconfirmed_tracks, new_detections) =
                LinearAssignment(unconfirmed_tracks, unmatched_detections, 0.7f, !mot20_);

            foreach (var match in matches)
            {
                match.Item1.Update(match.Item2, frame_id_);
                matched_tracks.Add(match.Item1);
            }

            var new_removed_tracks = new List<Track>();
            foreach (var track in unmatched_unconfirmed_tracks)
            {
                track.MarkAsRemoved();
                new_removed_tracks.Add(track);
            }

            // Add new tracks
            foreach (var detection in new_detections)
            {
                if (detection.Score < high_thresh_) continue;
                track_id_count_++;
                var newTrack = new Track(detection, frame_id_, track_id_count_);
                matched_tracks.Add(newTrack);
            }

            return new_removed_tracks;
        }

        private List<Track> JointTracks(
            List<Track> a_tlist,
            List<Track> b_tlist)
        {
            var exists = new HashSet<int>();
            var res = new List<Track>();

            foreach (var track in a_tlist)
            {
                exists.Add(track.TrackId);
                res.Add(track);
            }

            foreach (var track in b_tlist)
            {
                if (!exists.Contains(track.TrackId))
                {
                    res.Add(track);
                }
            }

            return res;
        }

        private List<Track> SubTracks(
            List<Track> a_tlist,
            List<Track> b_tlist)
        {
            var tracks = new Dictionary<int, Track>();
            foreach (var track in a_tlist)
            {
                tracks[track.TrackId] = track;
            }

            foreach (var track in b_tlist)
            {
                tracks.Remove(track.TrackId);
            }

            var res = new List<Track>();
            foreach (var kvp in tracks)
            {
                res.Add(kvp.Value);
            }

            return res;
        }

        private (List<Track>, List<Track>) RemoveDuplicateTracks(
            List<Track> a_tracks,
            List<Track> b_tracks)
        {
            if (a_tracks.Count == 0 || b_tracks.Count == 0)
            {
                return (a_tracks, b_tracks);
            }

            float[,] ious = new float[a_tracks.Count, b_tracks.Count];
            for (int ai = 0; ai < a_tracks.Count; ai++)
            {
                for (int bi = 0; bi < b_tracks.Count; bi++)
                {
                    ious[ai, bi] = 1 - RectOperations.CalcIoU(b_tracks[bi].PredictedRect, a_tracks[ai].PredictedRect);
                }
            }

            var a_overlapping = new bool[a_tracks.Count];
            var b_overlapping = new bool[b_tracks.Count];

            for (int ai = 0; ai < ious.GetLength(0); ai++)
            {
                for (int bi = 0; bi < ious.GetLength(1); bi++)
                {
                    if (ious[ai, bi] < 0.15)
                    {
                        int timep = a_tracks[ai].FrameId - a_tracks[ai].StartFrameId;
                        int timeq = b_tracks[bi].FrameId - b_tracks[bi].StartFrameId;
                        if (timep > timeq)
                        {
                            b_overlapping[bi] = true;
                        }
                        else
                        {
                            a_overlapping[ai] = true;
                        }
                    }
                }
            }

            var a_tracks_out = new List<Track>();
            var b_tracks_out = new List<Track>();

            for (int ai = 0; ai < a_tracks.Count; ai++)
            {
                if (!a_overlapping[ai]) a_tracks_out.Add(a_tracks[ai]);
            }

            for (int bi = 0; bi < b_tracks.Count; bi++)
            {
                if (!b_overlapping[bi]) b_tracks_out.Add(b_tracks[bi]);
            }

            return (a_tracks_out, b_tracks_out);
        }

        private (
            List<(Track, Detection)>,
            List<Track>,
            List<Detection>
        ) LinearAssignment(
            List<Track> tracks,
            List<Detection> detections,
            float thresh,
            bool use_fuse_score = true)
        {
            if (tracks.Count == 0 || detections.Count == 0)
            {
                return (new List<(Track, Detection)>(), tracks, detections);
            }

            var cost_matrix = new List<List<float>>();
            for (int i = 0; i < tracks.Count; i++)
            {
                var row = new List<float>();
                for (int j = 0; j < detections.Count; j++)
                {
                    row.Add(1 - RectOperations.CalcIoU(detections[j].Rect, tracks[i].PredictedRect));
                }
                cost_matrix.Add(row);
            }

            if (use_fuse_score)
            {
                cost_matrix = FuseScore(cost_matrix, detections);
            }

            var matches = new List<(Track, Detection)>();
            var a_unmatched = new List<Track>();
            var b_unmatched = new List<Detection>();

            var (rowsol, colsol, _) = ExecLapjv(cost_matrix, true, thresh);
            for (int i = 0; i < rowsol.Count; i++)
            {
                if (rowsol[i] >= 0)
                {
                    matches.Add((tracks[i], detections[rowsol[i]]));
                }
                else
                {
                    a_unmatched.Add(tracks[i]);
                }
            }

            for (int i = 0; i < colsol.Count; i++)
            {
                if (colsol[i] < 0) b_unmatched.Add(detections[i]);
            }

            return (matches, a_unmatched, b_unmatched);
        }

        private List<List<float>> FuseScore(List<List<float>> cost_matrix, List<Detection> detections)
        {
            if (cost_matrix.Count == 0)
                return cost_matrix;

            int n_rows = cost_matrix.Count;
            int n_cols = cost_matrix[0].Count;

            for (int i = 0; i < n_rows; i++)
            {
                for (int j = 0; j < n_cols; j++)
                {
                    float det_score = detections[j].Score;
                    float iou_sim = 1.0f - cost_matrix[i][j];
                    float fuse_sim = iou_sim * det_score;
                    cost_matrix[i][j] = 1.0f - fuse_sim;
                }
            }

            return cost_matrix;
        }


        private (
            List<int>,
            List<int>,
            double
        ) ExecLapjv(
            List<List<float>> cost,
            bool extend_cost = false,
            float cost_limit = float.MaxValue,
            bool return_cost = true)
        {
            int n_rows = cost.Count;
            int n_cols = cost[0].Count;
            List<int> rowsol = new List<int>(new int[n_rows]);
            List<int> colsol = new List<int>(new int[n_cols]);

            if (n_rows != n_cols && !extend_cost)
            {
                throw new InvalidOperationException("The `extendCost` variable should be set to true.");
            }

            int n = 0;
            List<float> cost_c;

            if (extend_cost || cost_limit < float.MaxValue)
            {
                n = n_rows + n_cols;
                cost_c = new List<float>(n * n);

                if (cost_limit < float.MaxValue)
                {
                    cost_c.AddRange(Enumerable.Repeat(cost_limit / 2.0f, n * n));
                }
                else
                {
                    float cost_max = -1;
                    foreach (var row in cost)
                    {
                        foreach (var val in row)
                        {
                            if (val > cost_max)
                                cost_max = val;
                        }
                    }
                    cost_c.AddRange(Enumerable.Repeat(cost_max + 1, n * n));
                }

                for (int i = n_rows; i < n; i++)
                {
                    for (int j = n_cols; j < n; j++)
                    {
                        cost_c[i * n + j] = 0;
                    }
                }

                for (int i = 0; i < n_rows; i++)
                {
                    for (int j = 0; j < n_cols; j++)
                    {
                        cost_c[i * n + j] = cost[i][j];
                    }
                }
            }
            else
            {
                n = n_rows;
                cost_c = new List<float>(n_rows * n_cols);

                for (int i = 0; i < n_rows; i++)
                {
                    for (int j = 0; j < n_cols; j++)
                    {
                        cost_c[i * n_cols + j] = cost[i][j];
                    }
                }
            }

            List<int> x_c = Enumerable.Repeat(0, n).ToList();
            List<int> y_c = Enumerable.Repeat(0, n).ToList();

            int ret = Lapjv.LapjvInternal(n, cost_c, x_c, y_c);
            if (ret != 0)
            {
                throw new InvalidOperationException("The result of LapjvInternal() is invalid.");
            }

            double opt = 0.0;

            if (n != n_rows)
            {
                for (int i = 0; i < n; i++)
                {
                    if (x_c[i] >= n_cols) x_c[i] = -1;
                    if (y_c[i] >= n_rows) y_c[i] = -1;
                }
                for (int i = 0; i < n_rows; i++)
                {
                    rowsol[i] = x_c[i];
                }
                for (int i = 0; i < n_cols; i++)
                {
                    colsol[i] = y_c[i];
                }

                if (return_cost)
                {
                    for (int i = 0; i < rowsol.Count; i++)
                    {
                        if (rowsol[i] != -1)
                        {
                            opt += cost_c[i * n_cols + rowsol[i]];
                        }
                    }
                }
            }
            else if (return_cost)
            {
                for (int i = 0; i < rowsol.Count; i++)
                {
                    opt += cost_c[i * n_cols + rowsol[i]];
                }
            }

            return (rowsol, colsol, opt);
        }
    }
}
