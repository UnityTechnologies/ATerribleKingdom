using System;
using UnityEngine;
using Cinemachine.Utility;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the the Body section of the component pipeline. 
    /// Its job is to position the camera in a variable relationship to a the vcam's 
    /// Follow target object, with offsets and damping.
    /// 
    /// This component is typically used to implement a camera that follows its target.
    /// It can accept player input from an input device, which allows the player to 
    /// dynamically control the relationship between the camera and the target, 
    /// for example with a joystick.
    /// 
    /// The OrbitalTransposer introduces the concept of __Heading__, which is the direction
    /// in which the target is moving, and the OrbitalTransposer will attempt to position 
    /// the camera in relationship to the heading, which is by default directly behind the target.
    /// You can control the default relationship by adjusting the Heading Bias setting.
    /// 
    /// If you attach an input controller to the OrbitalTransposer, then the player can also
    /// control the way the camera positions itself in relation to the target heading.  This allows
    /// the camera to move to any spot on an orbit around the target.
    /// </summary>
    [DocumentationSorting(6, DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineOrbitalTransposer : CinemachineTransposer
    {
        /// <summary>
        /// How the "forward" direction is defined.  Orbital offset is in relation to the forward
        /// direction.
        /// </summary>
        [DocumentationSorting(6.2f, DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public class Heading
        {
            /// <summary>
            /// Sets the algorithm for determining the target's heading for purposes
            /// of re-centering the camera
            /// </summary>
            [DocumentationSorting(6.21f, DocumentationSortingAttribute.Level.UserRef)]
            public enum HeadingDefinition
            {
                /// <summary>
                /// Target heading calculated from the difference between its position on
                /// the last update and current frame.
                /// </summary>
                PositionDelta,
                /// <summary>
                /// Target heading calculated from its <b>Rigidbody</b>'s velocity.
                /// If no <b>Rigidbody</b> exists, it will fall back
                /// to HeadingDerivationMode.PositionDelta
                /// </summary>
                Velocity,
                /// <summary>
                /// Target heading calculated from the Target <b>Transform</b>'s euler Y angle
                /// </summary>
                TargetForward,
                /// <summary>
                /// Default heading is a constant world space heading.
                /// </summary>
                WorldForward,
            }
            /// <summary>The method by which the 'default heading' is calculated if
            /// recentering to target heading is enabled</summary>
            [Tooltip("How 'forward' is defined.  The camera will be placed by default behind the target.  PositionDelta will consider 'forward' to be the direction in which the target is moving.")]
            public HeadingDefinition m_HeadingDefinition = HeadingDefinition.TargetForward;

            /// <summary>Size of the velocity sampling window for target heading filter.
            /// Used only if deriving heading from target's movement</summary>
            [Range(0, 10)]
            [Tooltip("Size of the velocity sampling window for target heading filter.  This filters out irregularities in the target's movement.  Used only if deriving heading from target's movement (PositionDelta or Velocity)")]
            public int m_VelocityFilterStrength = 4;

            /// <summary>Additional Y rotation applied to the target heading.
            /// When this value is 0, the camera will be placed behind the target</summary>
            [Range(-180f, 180f)]
            [Tooltip("Where the camera is placed when the X-axis value is zero.  This is a rotation in degrees around the Y axis.  When this value is 0, the camera will be placed behind the target.  Nonzero offsets will rotate the zero position around the target.")]
            public float m_HeadingBias = 0;
        };

        /// <summary>The definition of Forward.  Camera will follow behind.</summary>
        [Space]
        [Tooltip("The definition of Forward.  Camera will follow behind.")]
        public Heading m_Heading = new Heading();

        /// <summary>Controls how automatic orbit recentering occurs</summary>
        [DocumentationSorting(6.5f, DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct Recentering
        {
            /// <summary>If checked, will enable automatic recentering of the
            /// camera based on the heading calculation mode. If FALSE, recenting is disabled.</summary>
            [Tooltip("If checked, will enable automatic recentering of the camera based on the heading definition. If unchecked, recenting is disabled.")]
            public bool m_enabled;

            /// <summary>If no input has been detected, the camera will wait
            /// this long in seconds before moving its heading to the default heading.</summary>
            [Tooltip("If no input has been detected, the camera will wait this long in seconds before moving its heading to the zero position.")]
            public float m_RecenterWaitTime;

            /// <summary>Maximum angular speed of recentering.  Will accelerate into and decelerate out of this</summary>
            [Tooltip("Maximum angular speed of recentering.  Will accelerate into and decelerate out of this.")]
            public float m_RecenteringTime;

            /// <summary>Constructor with specific field values</summary>
            public Recentering(bool enabled, float recenterWaitTime,  float recenteringSpeed)
            {
                m_enabled = enabled;
                m_RecenterWaitTime = recenterWaitTime;
                m_RecenteringTime = recenteringSpeed;
                m_LegacyHeadingDefinition = m_LegacyVelocityFilterStrength = -1;
            }

            // Legacy support
            [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingDefinition")] private int m_LegacyHeadingDefinition;
            [SerializeField] [HideInInspector] [FormerlySerializedAs("m_VelocityFilterStrength")] private int m_LegacyVelocityFilterStrength;
            internal bool LegacyUpgrade(ref Heading.HeadingDefinition heading, ref int velocityFilter)
            {
                if (m_LegacyHeadingDefinition != -1 && m_LegacyVelocityFilterStrength != -1)
                {
                    heading = (Heading.HeadingDefinition)m_LegacyHeadingDefinition;
                    velocityFilter = m_LegacyVelocityFilterStrength;
                    m_LegacyHeadingDefinition = m_LegacyVelocityFilterStrength = -1;
                    return true;
                }
                return false;
            }
        };

        /// <summary>Parameters that control Automating Heading Recentering</summary>
        [Tooltip("Automatic heading recentering.  The settings here defines how the camera will reposition itself in the absence of player input.")]
        public Recentering m_RecenterToTargetHeading = new Recentering(true, 1, 2);

        /// <summary>
        /// Axis state for defining how
        /// this CinemachineOrbitalTransposer reacts to player input.  
        /// The settings here control the responsiveness of the axis to player input.
        /// </summary>
        [DocumentationSorting(6.4f, DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct AxisState
        {
            /// <summary>The current position on the axis/summary>
            [NoSaveDuringPlay]
            [Tooltip("The current value of the axis.")]
            public float Value;

            /// <summary>How fast the axis value can travel.  Increasing this number
            /// makes the behaviour more responsive to joystick input</summary>
            [Tooltip("The maximum speed of this axis in units/second")]
            public float m_MaxSpeed;

            /// <summary>The amount of time in seconds it takes to accelerate to
            /// MaxSpeed with the supplied Axis at its maximum value</summary>
            [Tooltip("The amount of time in seconds it takes to accelerate to MaxSpeed with the supplied Axis at its maximum value")]
            public float m_AccelTime;

            /// <summary>The amount of time in seconds it takes to decelerate
            /// the axis to zero if the supplied axis is in a neutral position</summary>
            [Tooltip("The amount of time in seconds it takes to decelerate the axis to zero if the supplied axis is in a neutral position")]
            public float m_DecelTime;

            /// <summary>The name of this axis as specified in Unity Input manager.
            /// Setting to an empty string will disable the automatic updating of this axis</summary>
            [FormerlySerializedAs("m_AxisName")]
            [Tooltip("The name of this axis as specified in Unity Input manager. Setting to an empty string will disable the automatic updating of this axis")]
            public string m_InputAxisName;

            /// <summary>The value of the input axis.  A value of 0 means no input
            /// You can drive this directly from a
            /// custom input system, or you can set the Axis Name and have the value
            /// driven by the internal Input Manager</summary>
            [NoSaveDuringPlay]
            [Tooltip("The value of the input axis.  A value of 0 means no input.  You can drive this directly from a custom input system, or you can set the Axis Name and have the value driven by the internal Input Manager")]
            public float m_InputAxisValue;

            /// <summary>If checked, then the raw value of the input axis will be inverted 
            /// before it is used.</summary>
            [NoSaveDuringPlay]
            [Tooltip("If checked, then the raw value of the input axis will be inverted before it is used")]
            public bool m_InvertAxis;

            private float mCurrentSpeed;
            private float mMinValue;
            private float mMaxValue;
            private bool mWrapAround;

            /// <summary>Constructor with specific values</summary>
            public AxisState(float maxSpeed, float accelTime, float decelTime, float val, string name, bool invert)
            {
                m_MaxSpeed = maxSpeed;
                m_AccelTime = accelTime;
                m_DecelTime = decelTime;
                Value = val;
                m_InputAxisName = name;
                m_InputAxisValue = 0;
                m_InvertAxis = invert;

                mCurrentSpeed = 0f;
                mMinValue = 0f;
                mMaxValue = 0f;
                mWrapAround = false;
            }

            /// <summary>
            /// Sets the constraints by which this axis will operate on
            /// </summary>
            /// <param name="minValue">The lowest value this axis can achieve</param>
            /// <param name="maxValue">The highest value this axis can achieve</param>
            /// <param name="wrapAround">If <b>TRUE</b>, values commanded greater
            /// than mMaxValue or less than mMinValue will wrap around.
            /// If <b>FALSE</b>, the value will be clamped within the range.</param>
            public void SetThresholds(float minValue, float maxValue, bool wrapAround)
            {
                mMinValue = minValue;
                mMaxValue = maxValue;
                mWrapAround = wrapAround;
            }

            /// <summary>
            /// Updates the state of this axis based on the axis defined
            /// by AxisState.m_AxisName
            /// </summary>
            /// <param name="dt">Delta time in seconds</param>
            /// <return>Returns <b>TRUE</b> if this axis' input was non-zero this Update,
            /// <b>FALSE</b> otherwise</return>
            public bool Update(float dt)
            {
                if (!string.IsNullOrEmpty(m_InputAxisName))
                {
                    try
                    {
                        m_InputAxisValue = CinemachineCore.GetInputAxis(m_InputAxisName);
                    }
                    catch (ArgumentException e)
                    {
                        Debug.LogError(e.ToString());
                    }
                }

                float input = m_InputAxisValue;
                if (m_InvertAxis)
                    input *= -1f;

                float absInput = Mathf.Abs(input);
                bool axisNonZero = absInput > UnityVectorExtensions.Epsilon;

                // Test to see if we're commanding a speed faster than we are going
                float accelTime = Mathf.Max(0.001f, m_AccelTime);
                if (axisNonZero && (absInput >= Mathf.Abs(mCurrentSpeed / m_MaxSpeed)))
                {
                    if (m_MaxSpeed > UnityVectorExtensions.Epsilon)
                        mCurrentSpeed += ((m_MaxSpeed / accelTime) * input) * dt;
                }
                else
                {
                    // Otherwise brake
                    // TODO: Can the fluctuation between these two cause nasty behaviour? Must monitor..
                    float decelTime = Mathf.Max(0.001f, m_DecelTime);
                    float reduction = Mathf.Sign(mCurrentSpeed) * (m_MaxSpeed / decelTime) * dt;
                    mCurrentSpeed = (Mathf.Abs(reduction) >= Mathf.Abs(mCurrentSpeed))
                        ? 0f : (mCurrentSpeed - reduction);
                }

                // Clamp our max speeds so we don't go crazy
                float maxSpeed = GetMaxSpeed();
                mCurrentSpeed = Mathf.Clamp(mCurrentSpeed, -maxSpeed, maxSpeed);

                Value += mCurrentSpeed * dt;
                bool isOutOfRange = (Value > mMaxValue) || (Value < mMinValue);
                if (isOutOfRange)
                {
                    if (mWrapAround)
                    {
                        if (Value > mMaxValue)
                            Value = mMinValue + (Value - mMaxValue);
                        else
                            Value = mMaxValue + (Value - mMinValue);
                    }
                    else
                    {
                        Value = Mathf.Clamp(Value, mMinValue, mMaxValue);
                        mCurrentSpeed = 0f;
                    }
                }
                return axisNonZero;
            }

            // MaxSpeed may be limited as we approach the range ends, in order
            // to prevent a hard bump
            private float GetMaxSpeed()
            {
                float range = mMaxValue - mMinValue;
                if (!mWrapAround && range > 0)
                {
                    float threshold = range / 10f;
                    if (mCurrentSpeed > 0 && (mMaxValue - Value) < threshold)
                    {
                        float t = (mMaxValue - Value) / threshold;
                        return Mathf.Lerp(0, m_MaxSpeed, t);
                    }
                    else if (mCurrentSpeed < 0 && (Value - mMinValue) < threshold)
                    {
                        float t = (Value - mMinValue) / threshold;
                        return Mathf.Lerp(0, m_MaxSpeed, t);
                    }
                }
                return m_MaxSpeed;
            }
        }

        /// <summary>Axis representing the current heading.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Heading Control.  The settings here control the behaviour of the camera in response to the player's input.")]
        public AxisState m_XAxis = new AxisState(3000f, 2f, 1f, 0f, "Mouse X", true);

        // Legacy support
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_Radius")] private float m_LegacyRadius = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeightOffset")] private float m_LegacyHeightOffset = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingBias")] private float m_LegacyHeadingBias = float.MaxValue;
        private void OnValidate()
        {
            // Upgrade after a legacy deserialize
            if (m_LegacyRadius != float.MaxValue 
                && m_LegacyHeightOffset != float.MaxValue
                && m_LegacyHeadingBias != float.MaxValue)
            {
                m_FollowOffset = new Vector3(0, m_LegacyHeightOffset, -m_LegacyRadius);
                m_LegacyHeightOffset = m_LegacyRadius = float.MaxValue;

                m_Heading.m_HeadingBias = m_LegacyHeadingBias;
                m_XAxis.m_MaxSpeed /= 10;
                m_XAxis.m_AccelTime /= 10;
                m_XAxis.m_DecelTime /= 10;
                m_LegacyHeadingBias = float.MaxValue;
                m_RecenterToTargetHeading.LegacyUpgrade(
                    ref m_Heading.m_HeadingDefinition, ref m_Heading.m_VelocityFilterStrength);
            }
        }

        /// <summary>
        /// Drive the x-axis setting programmatically.
        /// Automatic heading updating will be disabled.
        /// </summary>
        [HideInInspector, NoSaveDuringPlay]
        public bool m_HeadingIsSlave = false;

        /// <summary>
        /// When in slave mode, this should be called once and only
        /// once every hrame to update the heading.  When not in slave mode, this is called automatically.
        /// </summary>
        public void UpdateHeading(float deltaTime, Vector3 up)
        {
            // Only read joystick when game is playing
            if (deltaTime > 0)
            {
                bool xAxisInput = m_XAxis.Update(deltaTime);
                if (xAxisInput)
                {
                    mLastHeadingAxisInputTime = Time.time;
                    mHeadingRecenteringVelocity = 0;
                }
            }
            float targetHeading = GetTargetHeading(
                m_XAxis.Value, GetReferenceOrientation(up), deltaTime);

            if (deltaTime <= 0)
            {
                mHeadingRecenteringVelocity = 0;
                if (m_RecenterToTargetHeading.m_enabled)
                    m_XAxis.Value = targetHeading;
            }
            else
            {
                // Recentering
                if (m_RecenterToTargetHeading.m_enabled
                    && (Time.time > (mLastHeadingAxisInputTime + m_RecenterToTargetHeading.m_RecenterWaitTime)))
                {
                    // Scale value determined heuristically, to account for accel/decel
                    float recenterTime = m_RecenterToTargetHeading.m_RecenteringTime / 3f;
                    if (recenterTime <= deltaTime)
                        m_XAxis.Value = targetHeading;
                    else
                    {
                        float headingError = Mathf.DeltaAngle(m_XAxis.Value, targetHeading);
                        float absHeadingError = Mathf.Abs(headingError);
                        if (absHeadingError < UnityVectorExtensions.Epsilon)
                        {
                            m_XAxis.Value = targetHeading;
                            mHeadingRecenteringVelocity = 0;
                        }
                        else 
                        {
                            float scale = deltaTime / recenterTime;
                            float desiredVelocity = Mathf.Sign(headingError)
                                * Mathf.Min(absHeadingError, absHeadingError * scale);
                            // Accelerate to the desired velocity
                            float accel = desiredVelocity - mHeadingRecenteringVelocity;
                            if ((desiredVelocity < 0 && accel < 0) || (desiredVelocity > 0 && accel > 0))
                                desiredVelocity = mHeadingRecenteringVelocity + desiredVelocity * scale;
                            m_XAxis.Value += desiredVelocity;
                            mHeadingRecenteringVelocity = desiredVelocity;
                        }
                    }
                }
            }
        }

        /// <summary>Internal API for FreeLook, so that it can interpolate radius</summary>
        internal bool UseOffsetOverride { get; set; }

        /// <summary>Internal API for FreeLook, so that it can interpolate radius</summary>
        internal Vector3 OffsetOverride { get; set; }

        Vector3 EffectiveOffset 
        { 
            get { return UseOffsetOverride ? OffsetOverride : m_FollowOffset; } 
        }

        private void OnEnable()
        {
            m_XAxis.SetThresholds(0f, 360f, true);
            PreviousTarget = null;
            mLastTargetPosition = Vector3.zero;
        }

        private float mLastHeadingAxisInputTime = 0f;
        private float mHeadingRecenteringVelocity = 0f;
        private Vector3 mLastTargetPosition = Vector3.zero;
        private HeadingTracker mHeadingTracker;
        private Rigidbody mTargetRigidBody = null;
        private Transform PreviousTarget { get; set; }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If 0 or less, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            InitPrevFrameStateInfo(ref curState, deltaTime);

            // Update the heading
            if (VirtualCamera.Follow != PreviousTarget)
            {
                PreviousTarget = VirtualCamera.Follow;
                mTargetRigidBody = (PreviousTarget == null) ? null : PreviousTarget.GetComponent<Rigidbody>();
                mLastTargetPosition = (PreviousTarget == null) ? Vector3.zero : PreviousTarget.position;
                mHeadingTracker = null;
            }
            if (!m_HeadingIsSlave)
                UpdateHeading(deltaTime, curState.ReferenceUp);

            if (IsValid)
            {
                mLastTargetPosition = VirtualCamera.Follow.position;

                // Place the camera
                Quaternion targetOrientation = GetReferenceOrientation(curState.ReferenceUp);
                float heading = m_XAxis.Value + m_Heading.m_HeadingBias;
                targetOrientation = targetOrientation * Quaternion.AngleAxis(heading, Vector3.up);
                DoTracking(ref curState, deltaTime, targetOrientation, EffectiveOffset);
            }
        }

        static string GetFullName(GameObject current)
        {
            if (current == null)
                return "";
            if (current.transform.parent == null)
                return "/" + current.name;
            return GetFullName(current.transform.parent.gameObject) + "/" + current.name;
        }

        // Make sure this is calld only once per frame
        private float GetTargetHeading(
            float currentHeading, Quaternion targetOrientation, float deltaTime)
        {
            if (VirtualCamera.Follow == null)
                return currentHeading;

            if (m_Heading.m_HeadingDefinition == Heading.HeadingDefinition.Velocity
                && mTargetRigidBody == null)
            {
                Debug.Log(string.Format(
                        "Attempted to use HeadingDerivationMode.Velocity to calculate heading for {0}. No RigidBody was present on '{1}'. Defaulting to position delta",
                        GetFullName(VirtualCamera.VirtualCameraGameObject), VirtualCamera.Follow));
                m_Heading.m_HeadingDefinition = Heading.HeadingDefinition.PositionDelta;
            }

            Vector3 velocity = Vector3.zero;
            switch (m_Heading.m_HeadingDefinition)
            {
                case Heading.HeadingDefinition.PositionDelta:
                    velocity = VirtualCamera.Follow.position - mLastTargetPosition;
                    break;
                case Heading.HeadingDefinition.Velocity:
                    velocity = mTargetRigidBody.velocity;
                    break;
                default:
                case Heading.HeadingDefinition.TargetForward:
                case Heading.HeadingDefinition.WorldForward:
                    return 0;
            }

            // Process the velocity and derive the heading from it.
            int filterSize = m_Heading.m_VelocityFilterStrength * 5;
            if (mHeadingTracker == null || mHeadingTracker.FilterSize != filterSize)
                mHeadingTracker = new HeadingTracker(filterSize);
            mHeadingTracker.DecayHistory();
            Vector3 up = targetOrientation * Vector3.up;
            velocity = velocity.ProjectOntoPlane(up);
            if (!velocity.AlmostZero())
                mHeadingTracker.Add(velocity);

            velocity = mHeadingTracker.GetReliableHeading();
            if (!velocity.AlmostZero())
                return UnityVectorExtensions.SignedAngle(targetOrientation * Vector3.forward, velocity, up);

            // If no reliable heading, then stay where we are.
            return currentHeading;
        }

        class HeadingTracker
        {
            struct Item
            {
                public Vector3 velocity;
                public float weight;
                public float time;
            };
            Item[] mHistory;
            int mTop;
            int mBottom;
            int mCount;

            Vector3 mHeadingSum;
            float mWeightSum = 0;
            float mWeightTime = 0;

            Vector3 mLastGoodHeading = Vector3.zero;

            public HeadingTracker(int filterSize)
            {
                mHistory = new Item[filterSize];
                float historyHalfLife = filterSize / 5f; // somewhat arbitrarily
                mDecayExponent = -Mathf.Log(2f) / historyHalfLife;
                ClearHistory();
            }

            public int FilterSize { get { return mHistory.Length; } }

            void ClearHistory()
            {
                mTop = mBottom = mCount = 0;
                mWeightSum = 0;
                mHeadingSum = Vector3.zero;
            }

            static float mDecayExponent;
            static float Decay(float time) { return Mathf.Exp(time * mDecayExponent); }

            public void Add(Vector3 velocity)
            {
                if (FilterSize == 0)
                {
                    mLastGoodHeading = velocity;
                    return;
                }
                float weight = velocity.magnitude;
                if (weight > UnityVectorExtensions.Epsilon)
                {
                    Item item = new Item();
                    item.velocity = velocity;
                    item.weight = weight;
                    item.time = Time.time;
                    if (mCount == FilterSize)
                        PopBottom();
                    ++mCount;
                    mHistory[mTop] = item;
                    if (++mTop == FilterSize)
                        mTop = 0;

                    mWeightSum *= Decay(item.time - mWeightTime);
                    mWeightTime = item.time;
                    mWeightSum += weight;
                    mHeadingSum += item.velocity;
                }
            }

            void PopBottom()
            {
                if (mCount > 0)
                {
                    float time = Time.time;
                    Item item = mHistory[mBottom];
                    if (++mBottom == FilterSize)
                        mBottom = 0;
                    --mCount;

                    float decay = Decay(time - item.time);
                    mWeightSum -= item.weight * decay;
                    mHeadingSum -= item.velocity * decay;
                    if (mWeightSum <= UnityVectorExtensions.Epsilon || mCount == 0)
                        ClearHistory();
                }
            }

            public void DecayHistory()
            {
                float time = Time.time;
                float decay = Decay(time - mWeightTime);
                mWeightSum *= decay;
                mWeightTime = time;
                if (mWeightSum < UnityVectorExtensions.Epsilon)
                    ClearHistory();
                else
                    mHeadingSum = mHeadingSum * decay;
            }

            public Vector3 GetReliableHeading()
            {
                // Update Last Good Heading
                if (mWeightSum > UnityVectorExtensions.Epsilon
                    && (mCount == mHistory.Length || mLastGoodHeading.AlmostZero()))
                {
                    Vector3  h = mHeadingSum / mWeightSum;
                    if (!h.AlmostZero())
                        mLastGoodHeading = h.normalized;
                }
                return mLastGoodHeading;
            }
        }
    }
}
