using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// The output of the Cinemachine engine for a specific virtual camera.  The information
    /// in this struct can be blended, and provides what is needed to calculate an
    /// appropriate camera position, orientation, and lens setting.
    /// 
    /// Raw values are what the Cinemachine behaviours generate.  The correction channel
    /// holds perturbations to the raw values - e.g. noise or smoothing, or obstacle
    /// avoidance corrections.  Coirrections are not considered when making time-based
    /// calculations such as damping.
    /// 
    /// The Final position and orientation is the comination of the raw values and
    /// their corrections.
    /// </summary>
    public struct CameraState
    {
        /// <summary>
        /// Camera Lens Settings.
        /// </summary>
        public LensSettings Lens { get; set; }

        /// <summary>
        /// Which way is up.  World space unit vector.
        /// </summary>
        public Vector3 ReferenceUp { get; set; }

        /// <summary>
        /// The world space focus point of the camera.  What the camera wants to look at.
        /// There is a special constant define to represent "nothing".  Be careful to 
        /// check for that (or check the HasLookAt property).
        /// </summary>
        public Vector3 ReferenceLookAt { get; set; }

        /// <summary>
        /// Returns true if this state has a valid ReferenceLookAt value.
        /// </summary>
        public bool HasLookAt { get { return ReferenceLookAt == ReferenceLookAt; } } // will be false if NaN

        /// <summary>
        /// This constant represents "no point in space" or "no direction".
        /// </summary>
        public static Vector3 kNoPoint = new Vector3(float.NaN, float.NaN, float.NaN);

        /// <summary>
        /// Raw (un-corrected) world space position of this camera
        /// </summary>
        public Vector3 RawPosition { get; set; }

        /// <summary>
        /// Raw (un-corrected) world space orientation of this camera
        /// </summary>
        public Quaternion RawOrientation { get; set; }

        /// <summary>
        /// Subjective estimation of how "good" the shot is.
        /// Larger values mean better quality.  Default is 1.
        /// </summary>
        public float ShotQuality { get; set; }

        /// <summary>
        /// Position correction.  This will be added to the raw position.
        /// This value doesn't get fed back into the system when calculating the next frame.
        /// Can be noise, or smoothing, or both, or something else.
        /// </summary>
        public Vector3 PositionCorrection { get; set; }

        /// <summary>
        /// Orientation correction.  This will be added to the raw orientation.
        /// This value doesn't get fed back into the system when calculating the next frame.
        /// Can be noise, or smoothing, or both, or something else.
        /// </summary>
        public Quaternion OrientationCorrection { get; set; }

        /// <summary>
        /// Position with correction applied.
        /// </summary>
        public Vector3 CorrectedPosition { get { return RawPosition + PositionCorrection; } }

        /// <summary>
        /// Orientation with correction applied.
        /// </summary>
        public Quaternion CorrectedOrientation { get { return RawOrientation * OrientationCorrection; } }

        /// <summary>
        /// Position with correction applied.  This is what the final camera gets.
        /// </summary>
        public Vector3 FinalPosition { get { return RawPosition + PositionCorrection; } }

        /// <summary>
        /// Orientation with correction and dutch applied.  This is what the final camera gets.
        /// </summary>
        public Quaternion FinalOrientation
        {
            get
            {
                if (Mathf.Abs(Lens.Dutch) > UnityVectorExtensions.Epsilon)
                    return CorrectedOrientation * Quaternion.AngleAxis(Lens.Dutch, Vector3.forward);
                return CorrectedOrientation;
            }
        }

        /// <summary>
        /// State with default values
        /// </summary>
        public static CameraState Default
        {
            get
            {
                CameraState state = new CameraState();
                state.Lens = LensSettings.Default;
                state.ReferenceUp = Vector3.up;
                state.ReferenceLookAt = kNoPoint;
                state.RawPosition = Vector3.zero;
                state.RawOrientation = Quaternion.identity;
                state.ShotQuality = 1;
                state.PositionCorrection = Vector3.zero;
                state.OrientationCorrection = Quaternion.identity;
                return state;
            }
        }

        /// <summary>Intelligently blend the contents of two states.</summary>
        /// <param name="stateA">The first state, corresponding to t=0</param>
        /// <param name="stateB">The second state, corresponding to t=1</param>
        /// <param name="t">How much to interpolate.  Internally clamped to 0..1</param>
        /// <returns>Linearly interpolated CameraState</returns>
        public static CameraState Lerp(CameraState stateA, CameraState stateB, float t)
        {
            t = Mathf.Clamp01(t);
            float adjustedT = t;

            CameraState state = new CameraState();
            state.Lens = LensSettings.Lerp(stateA.Lens, stateB.Lens, t);
            state.ReferenceUp = Vector3.Slerp(stateA.ReferenceUp, stateB.ReferenceUp, t);
            state.RawPosition = Vector3.Lerp(stateA.RawPosition, stateB.RawPosition, t);

            state.ShotQuality = Mathf.Lerp(stateA.ShotQuality, stateB.ShotQuality, t);
            state.PositionCorrection = Vector3.Lerp(
                    stateA.PositionCorrection, stateB.PositionCorrection, t);
            // GML todo: is this right?  Can it introduce a roll?
            state.OrientationCorrection = Quaternion.Slerp(
                    stateA.OrientationCorrection, stateB.OrientationCorrection, t);

            Vector3 dirTarget = Vector3.zero;
            if (!stateA.HasLookAt || !stateB.HasLookAt)
                state.ReferenceLookAt = kNoPoint;   // can't interpolate if undefined
            else
            {
                // Re-interpolate FOV to preserve target composition, if possible
                float fovA = stateA.Lens.FieldOfView;
                float fovB = stateB.Lens.FieldOfView;
                if (!state.Lens.Orthographic && !Mathf.Approximately(fovA, fovB))
                {
                    LensSettings lens = state.Lens;
                    lens.FieldOfView = state.InterpolateFOV(
                            fovA, fovB,
                            Mathf.Max((stateA.ReferenceLookAt - stateA.CorrectedPosition).magnitude, stateA.Lens.NearClipPlane),
                            Mathf.Max((stateB.ReferenceLookAt - stateB.CorrectedPosition).magnitude, stateB.Lens.NearClipPlane), t);
                    state.Lens = lens;

                    // Make sure we preserve the screen composition through FOV changes
                    adjustedT = Mathf.Abs((lens.FieldOfView - fovA) / (fovB - fovA));
                }

                // Spherical linear interpolation about CorrectedPosition
                state.ReferenceLookAt = state.CorrectedPosition + Vector3.Slerp(
                        stateA.ReferenceLookAt - state.CorrectedPosition,
                        stateB.ReferenceLookAt - state.CorrectedPosition, adjustedT);
                dirTarget = state.ReferenceLookAt - state.CorrectedPosition;
            }

            // Clever orientation interpolation
            if (dirTarget.AlmostZero())
            {
                // Don't know what we're looking at - can only slerp
                state.RawOrientation = UnityQuaternionExtensions.SlerpWithReferenceUp(
                        stateA.RawOrientation, stateB.RawOrientation, t, state.ReferenceUp);
            }
            else
            {
                // Rotate while preserving our lookAt target
                dirTarget = dirTarget.normalized;
                if ((dirTarget - state.ReferenceUp).AlmostZero()
                    || (dirTarget + state.ReferenceUp).AlmostZero())
                {
                    // Looking up or down at the pole
                    state.RawOrientation = UnityQuaternionExtensions.SlerpWithReferenceUp(
                            stateA.RawOrientation, stateB.RawOrientation, t, state.ReferenceUp);
                }
                else
                {
                    // Put the target in the center
                    state.RawOrientation = Quaternion.LookRotation(dirTarget, state.ReferenceUp);

                    // Blend the desired offsets from center
                    Vector2 deltaA = -stateA.RawOrientation.GetCameraRotationToTarget(
                            stateA.ReferenceLookAt - stateA.CorrectedPosition, stateA.ReferenceUp);
                    Vector2 deltaB = -stateB.RawOrientation.GetCameraRotationToTarget(
                            stateB.ReferenceLookAt - stateB.CorrectedPosition, stateB.ReferenceUp);
                    state.RawOrientation = state.RawOrientation.ApplyCameraRotation(
                            Vector2.Lerp(deltaA, deltaB, adjustedT), state.ReferenceUp);
                }
            }
            return state;
        }

        float InterpolateFOV(float fovA, float fovB, float dA, float dB, float t)
        {
            // We interpolate shot height
            float hA = dA * 2f * Mathf.Tan(fovA * Mathf.Deg2Rad / 2f);
            float hB = dB * 2f * Mathf.Tan(fovB * Mathf.Deg2Rad / 2f);
            float h = Mathf.Lerp(hA, hB, t);
            float fov = 179f;
            float d = Mathf.Lerp(dA, dB, t);
            if (d > UnityVectorExtensions.Epsilon)
                fov = 2f * Mathf.Atan(h / (2 * d)) * Mathf.Rad2Deg;
            return Mathf.Clamp(fov, Mathf.Min(fovA, fovB), Mathf.Max(fovA, fovB));
        }
    }
}
