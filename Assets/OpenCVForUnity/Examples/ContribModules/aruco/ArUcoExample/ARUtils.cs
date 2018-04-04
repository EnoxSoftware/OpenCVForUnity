using UnityEngine;
using System.Collections;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// AR utils.
    /// </summary>
    public class ARUtils
    {
        /// <summary>
        /// Extract translation from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Translation offset.
        /// </returns>
        public static Vector3 ExtractTranslationFromMatrix (ref Matrix4x4 matrix)
        {
            Vector3 translate;
            translate.x = matrix.m03;
            translate.y = matrix.m13;
            translate.z = matrix.m23;
            return translate;
        }
        
        /// <summary>
        /// Extract rotation quaternion from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Quaternion representation of rotation transform.
        /// </returns>
        public static Quaternion ExtractRotationFromMatrix (ref Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;
            
            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;
            
            return Quaternion.LookRotation (forward, upwards);
        }
        
        /// <summary>
        /// Extract scale from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Scale vector.
        /// </returns>
        public static Vector3 ExtractScaleFromMatrix (ref Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4 (matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4 (matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4 (matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
        
        /// <summary>
        /// Extract position, rotation and scale from TRS matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <param name="localPosition">Output position.</param>
        /// <param name="localRotation">Output rotation.</param>
        /// <param name="localScale">Output scale.</param>
        public static void DecomposeMatrix (ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
        {
            localPosition = ExtractTranslationFromMatrix (ref matrix);
            localRotation = ExtractRotationFromMatrix (ref matrix);
            localScale = ExtractScaleFromMatrix (ref matrix);
        }
        
        /// <summary>
        /// Set transform component from TRS matrix.
        /// </summary>
        /// <param name="transform">Transform component.</param>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        public static void SetTransformFromMatrix (Transform transform, ref Matrix4x4 matrix)
        {
            transform.localPosition = ExtractTranslationFromMatrix (ref matrix);
            transform.localRotation = ExtractRotationFromMatrix (ref matrix);
            transform.localScale = ExtractScaleFromMatrix (ref matrix);
        }

        /// <summary>
        /// Calculate projection matrix from camera matrix values.
        /// </summary>
        /// <param name="fx">Focal length x.</param>
        /// <param name="fy">Focal length y.</param>
        /// <param name="cx">Image center point x.(principal point x)</param>
        /// <param name="cy">Image center point y.(principal point y)</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="near">The near clipping plane distance.</param>
        /// <param name="far">The far clipping plane distance.</param>
        /// <returns>
        /// Projection matrix.
        /// </returns>
        public static Matrix4x4 CalculateProjectionMatrixFromCameraMatrixValues (float fx, float fy, float cx, float cy, float width, float height, float near, float far)
        {
            Matrix4x4 projectionMatrix = new Matrix4x4 ();
            projectionMatrix.m00 = 2.0f * fx / width;
            projectionMatrix.m02 = 1.0f - 2.0f * cx / width;
            projectionMatrix.m11 = 2.0f * fy / height;
            projectionMatrix.m12 = - 1.0f + 2.0f * cy / height;
            projectionMatrix.m22 = -(far + near) / (far - near);
            projectionMatrix.m23 = -2.0f * far * near / (far - near);
            projectionMatrix.m32 = -1.0f;

            return projectionMatrix;
        }

        /// <summary>
        /// Calculate camera matrix values from projection matrix.
        /// </summary>
        /// <param name="projectionMatrix">Projection matrix.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="fovV">Vertical field of view.</param>
        /// <returns>
        /// Camera matrix values. (fx = matrx.m00, fy = matrx.m11, cx = matrx.m02, cy = matrx.m12)
        /// </returns>
        public static Matrix4x4 CameraMatrixValuesFromCalculateProjectionMatrix (Matrix4x4 projectionMatrix, float width, float height, float fovV)
        {
            float fovH = 2.0f * Mathf.Atan (width/height * Mathf.Tan (fovV*Mathf.Deg2Rad / 2.0f)) * Mathf.Rad2Deg;

            Matrix4x4 cameraMatrix = new Matrix4x4 ();
            cameraMatrix.m00 = CalculateDistance (width, fovH);
            cameraMatrix.m02 = -((projectionMatrix.m02*width - width) / 2);
            cameraMatrix.m11 = CalculateDistance (height, fovV);
            cameraMatrix.m12 = (projectionMatrix.m12*height + height) / 2;
            cameraMatrix.m22 = 1.0f;

            return cameraMatrix;
        }

        /// <summary>
        /// Calculate frustum size.
        /// https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
        /// </summary>
        /// <param name="distance">Distance.</param>
        /// <param name="fov">Field of view. (horizontal or vertical direction)</param>
        /// <returns>
        /// Frustum height.
        /// </returns>
        public static float CalculateFrustumSize (float distance, float fov)
        {
            return 2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Calculate distance.
        /// https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
        /// </summary>
        /// <param name="frustumHeight">One side size of a frustum.</param>
        /// <param name="fov">Field of view. (horizontal or vertical direction)</param>
        /// <returns>
        /// Distance.
        /// </returns>
        public static float CalculateDistance (float frustumSize, float fov)
        {
            return frustumSize * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Calculate FOV angle.
        /// https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
        /// </summary>
        /// <param name="frustumHeight">One side size of a frustum.</param>
        /// <param name="distance">Distance.</param>
        /// <returns>
        /// FOV angle.
        /// </returns>
        public static float CalculateFOVAngle (float frustumSize, float distance)
        {
            return 2.0f * Mathf.Atan (frustumSize * 0.5f / distance) * Mathf.Rad2Deg;
        }
    }
}