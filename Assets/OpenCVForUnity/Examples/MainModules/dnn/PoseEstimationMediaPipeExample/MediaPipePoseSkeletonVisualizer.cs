#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using UnityEngine;
using KeyPoint = OpenCVForUnityExample.DnnModel.MediaPipePoseEstimator.KeyPoint;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/digital-standard/ThreeDPoseUnityBarracuda/blob/master/Assets/Scripts/VNectModel.cs
    /// </summary>
    public class MediaPipePoseSkeletonVisualizer : MonoBehaviour
    {
        public class Skeleton
        {
            public GameObject LineObject;
            public LineRenderer Line;
        }

        private const int numSkeletons = 35;
        private List<Skeleton> skeletons = new List<Skeleton>();
        public Material skeletonMaterial;

        private bool _showSkeleton = true;
        public bool showSkeleton
        {
            get
            {
                return _showSkeleton;
            }
            set
            {
                _showSkeleton = value;
                ClearLine();
            }
        }

        public float skeletonX;
        public float skeletonY;
        public float skeletonZ;
        public float skeletonScale = 1f;

        private Vector3[] landmarks_world_buffer;

        public void UpdatePose(Vector3[] landmarks_world)
        {
            if (landmarks_world == null || landmarks_world.Length < 33)
                return;

            if (skeletons.Count == 0)
            {
                for (int i = 0; i < numSkeletons; ++i)
                {
                    AddSkeleton();
                }
            }

            // Add scaling and shifting to world landmark coordinates, and convert from a right-handed coordinate system to a left-handed coordinate system (Unity).
            void _set_line_position(int index, int idx1, int idx2)
            {
                skeletons[index].Line.SetPosition(0, new Vector3(
                    landmarks_world[idx1].x * skeletonScale * 1 + skeletonX,
                    landmarks_world[idx1].y * skeletonScale * -1 + skeletonY,
                    landmarks_world[idx1].z * skeletonScale * 1 + skeletonZ
                    ));
                skeletons[index].Line.SetPosition(1, new Vector3(
                    landmarks_world[idx2].x * skeletonScale * 1 + skeletonX,
                    landmarks_world[idx2].y * skeletonScale * -1 + skeletonY,
                    landmarks_world[idx2].z * skeletonScale * 1 + skeletonZ
                    ));
            }

            _set_line_position(0, (int)KeyPoint.Nose, (int)KeyPoint.LeftEyeInner);
            _set_line_position(1, (int)KeyPoint.LeftEyeInner, (int)KeyPoint.LeftEye);
            _set_line_position(2, (int)KeyPoint.LeftEye, (int)KeyPoint.LeftEyeOuter);
            _set_line_position(3, (int)KeyPoint.LeftEyeOuter, (int)KeyPoint.LeftEar);
            _set_line_position(4, (int)KeyPoint.Nose, (int)KeyPoint.RightEyeInner);
            _set_line_position(5, (int)KeyPoint.RightEyeInner, (int)KeyPoint.RightEye);
            _set_line_position(6, (int)KeyPoint.RightEye, (int)KeyPoint.RightEyeOuter);
            _set_line_position(7, (int)KeyPoint.RightEyeOuter, (int)KeyPoint.RightEar);

            _set_line_position(8, (int)KeyPoint.MouthLeft, (int)KeyPoint.MouthRight);

            _set_line_position(9, (int)KeyPoint.RightShoulder, (int)KeyPoint.RightElbow);
            _set_line_position(10, (int)KeyPoint.RightElbow, (int)KeyPoint.RightWrist);
            _set_line_position(11, (int)KeyPoint.RightWrist, (int)KeyPoint.RightThumb);
            _set_line_position(12, (int)KeyPoint.RightWrist, (int)KeyPoint.RightPinky);
            _set_line_position(13, (int)KeyPoint.RightWrist, (int)KeyPoint.RightIndex);
            _set_line_position(14, (int)KeyPoint.RightPinky, (int)KeyPoint.RightIndex);

            _set_line_position(15, (int)KeyPoint.LeftShoulder, (int)KeyPoint.LeftElbow);
            _set_line_position(16, (int)KeyPoint.LeftElbow, (int)KeyPoint.LeftWrist);
            _set_line_position(17, (int)KeyPoint.LeftWrist, (int)KeyPoint.LeftThumb);
            _set_line_position(18, (int)KeyPoint.LeftWrist, (int)KeyPoint.LeftIndex);
            _set_line_position(19, (int)KeyPoint.LeftWrist, (int)KeyPoint.LeftPinky);
            _set_line_position(20, (int)KeyPoint.LeftPinky, (int)KeyPoint.LeftIndex);

            _set_line_position(21, (int)KeyPoint.LeftShoulder, (int)KeyPoint.RightShoulder);
            _set_line_position(22, (int)KeyPoint.LeftShoulder, (int)KeyPoint.LeftHip);
            _set_line_position(23, (int)KeyPoint.LeftHip, (int)KeyPoint.RightHip);
            _set_line_position(24, (int)KeyPoint.RightHip, (int)KeyPoint.RightShoulder);

            _set_line_position(25, (int)KeyPoint.RightHip, (int)KeyPoint.RightKnee);
            _set_line_position(26, (int)KeyPoint.RightKnee, (int)KeyPoint.RightAnkle);
            _set_line_position(27, (int)KeyPoint.RightAnkle, (int)KeyPoint.RightHeel);
            _set_line_position(28, (int)KeyPoint.RightAnkle, (int)KeyPoint.RightFootIndex);
            _set_line_position(29, (int)KeyPoint.RightHeel, (int)KeyPoint.RightFootIndex);

            _set_line_position(30, (int)KeyPoint.LeftHip, (int)KeyPoint.LeftKnee);
            _set_line_position(31, (int)KeyPoint.LeftKnee, (int)KeyPoint.LeftAnkle);
            _set_line_position(32, (int)KeyPoint.LeftAnkle, (int)KeyPoint.LeftFootIndex);
            _set_line_position(33, (int)KeyPoint.LeftAnkle, (int)KeyPoint.LeftHeel);
            _set_line_position(34, (int)KeyPoint.LeftHeel, (int)KeyPoint.LeftFootIndex);
        }

        public void UpdatePose(Mat result)
        {
            if (result.empty() || result.rows() < 317)
                return;

            if (landmarks_world_buffer == null)
                landmarks_world_buffer = new Vector3[33];

            // Copy only world landmarks data from pose data.
            MatUtils.copyFromMat<Vector3>(result.rowRange(199, 199 + 99), landmarks_world_buffer);

            UpdatePose(landmarks_world_buffer);
        }

        private void AddSkeleton()
        {
            var lineObject = new GameObject("Line");
            lineObject.transform.parent = gameObject.transform;

            lineObject.layer = gameObject.layer;

            var sk = new Skeleton()
            {
                LineObject = lineObject
            };

            sk.Line = sk.LineObject.AddComponent<LineRenderer>();
            sk.Line.startWidth = 0.032f * skeletonScale;
            sk.Line.endWidth = 0.008f * skeletonScale;

            // define the number of vertex
            sk.Line.positionCount = 2;
            sk.Line.material = skeletonMaterial;

            skeletons.Add(sk);
        }

        private void ClearLine()
        {
            if (skeletons.Count != numSkeletons)
                return;

            for (int i = 0; i < numSkeletons; ++i)
            {
                skeletons[i].Line.positionCount = 0;
                skeletons[i].Line.positionCount = 2;
            }
        }
    }
}
#endif