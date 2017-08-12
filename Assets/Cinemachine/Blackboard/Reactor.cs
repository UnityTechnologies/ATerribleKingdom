using System;
using System.Reflection;
using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine.Blackboard
{
    /// <summary>
    /// Cinemachine Reactor is a decorator of the CinemachineVirtualCamera
    /// which modulates its fields based on the input mappings specified here.
    /// and the data present in Blackboard.CinemachineBlackboard
    /// </summary>
    [DocumentationSorting(301, DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    public class Reactor : MonoBehaviour
    {
        /// <summary>
        /// Specifies how the entries in the reactor mapping are combined with each other
        /// </summary>
        public enum CombineMode
        {
            Set,        /// Sets the output value to this value
            Add,        /// Adds this value to the previous Reactor value
            Subtract,   /// Subtracts this value from the previous Reactor value
            Multiply,   /// Multiplies the previous Reactor value by this value
            Divide      /// Divides the previous Reactor value by this value
        };

        /// <summary>
        /// Wrapper class containing a series of remappings to convert blackboard variables into
        /// an input into Reactor
        /// </summary>
        [Serializable]
        public struct BlackboardExpression
        {
            /// <summary>
            /// A single entry in the Reactor input mapper
            /// </summary>
            [Serializable]
            public struct Line
            {
                /// <summary>
                /// Specifies how this remapping is combined with the previous entry in the Input mapper
                /// </summary>
                [Tooltip("How to modify the resulting value")]
                public CombineMode m_Operation;

                /// <summary>
                /// The key in the Blackboard.CinemachineBlackboard to get the value for
                /// </summary>
                [Tooltip("Value to look up on the Blackboard.  You need to have a script post this value on the blackboard.")]
                public string m_BlackboardKey;

                /// <summary>
                /// Whether to remap the value through a remap curve
                /// </summary>
                [Tooltip("Whether to remap the value through a remap curve")]
                public bool m_Remap;

                /// <summary>
                /// A remap curve for the value found in the Blackboard.CinemachineBlackboard.
                /// The X-axis represents the Blackboard value, and the Y-axis represents the output value from the remapping
                /// </summary>
                [Tooltip("How to modily the value read from the blackboard before using it.  The X-axis represents the Blackboard value, and the Y-axis represents the output value.")]
                public AnimationCurve m_RemapCurve;
            }

            [Tooltip("The inputs from the blackboard to be used in deriving the final value for this input mapper")]
            public Line[] m_Lines;

            public int GetNumLines() { return m_Lines == null ? 0 : m_Lines.Length; }

            /// <summary>
            /// Evaluates this BlackboardExpression against the supplied Blackboard.
            /// Will attempt find and remap values from the blackboard and combine them as defined in the
            /// array of Mapping
            /// </summary>
            /// <param name="againstBlackboard">The blackboard used to retrieve values from for the
            /// remappings</param>
            /// <returns>The computed result of the remappings against the blackboard.
            internal bool Evaluate(Blackboard againstBlackboard, out float result)
            {
                result = 0;
                for (int i = 0; m_Lines != null && i < m_Lines.Length; ++i)
                {
                    Line line = m_Lines[i];
                    float blackboardValue;
                    if (!againstBlackboard.TryGetValue(line.m_BlackboardKey, out blackboardValue))
                        return false;
                    if (line.m_Remap && line.m_RemapCurve != null && line.m_RemapCurve.keys.Length > 1)
                        blackboardValue = line.m_RemapCurve.Evaluate(blackboardValue);
                    if (i == 0)
                        result = blackboardValue;
                    else
                    {
                        switch (line.m_Operation)
                        {
                            case CombineMode.Set: result = blackboardValue; break;
                            case CombineMode.Add: result += blackboardValue; break;
                            case CombineMode.Subtract: result -= blackboardValue; break;
                            case CombineMode.Multiply: result *= blackboardValue; break;
                            case CombineMode.Divide: result /= blackboardValue; break;
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Wrapper class containing a series of remappings to convert blackboard variables into
        /// an input into Reactor
        /// </summary>
        [Serializable]
        public struct TargetModifier
        {
            [Tooltip("Which setting to modify")]
            public string m_Field;

            [Tooltip("How to apply the expression to the field")]
            public CombineMode m_Operation;

            [Tooltip("The expression that is used to modify the field")]
            public BlackboardExpression m_Expression;

            /// This gets set at runtime when mapping is first evaluated.
            internal class TargetBinding
            {
                FieldInfo[] mTargetFieldInfo;
                object[] mTargetFieldOwner;
                float mInitialValue;

                // Use reflection to find the named float field
                public static TargetBinding BindTarget(GameObject target, string fieldName)
                {
                    fieldName = fieldName.Trim();
                    TargetBinding binding = new TargetBinding();

                    GameObjectFieldScanner scanner = new GameObjectFieldScanner();
                    scanner.OnLeafField = (fullName, fieldInfo, rootFieldOwner, value) =>
                        {
                            //Debug.Log(fullName);
                            if (fullName == fieldName)
                            {
                                binding.mTargetFieldInfo = fieldInfo.ToArray();
                                binding.mTargetFieldOwner = new object[binding.mTargetFieldInfo.Length];
                                binding.mTargetFieldOwner[0] = rootFieldOwner;
                                binding.mInitialValue = Convert.ToSingle(value);
                                return false; // abort scan, we're done
                            }
                            return true;
                        };
                    scanner.ScanFields(target);

                    if (!binding.IsValid)
                        Debug.Log(string.Format(
                                GetFullName(target) + " Reactor: can't find " +
                                ((fieldName.Length == 0) ? "(empty)" : fieldName)));

                    return binding;
                }

                static string GetFullName(GameObject current)
                {
                    if (current == null)
                        return "";
                    if (current.transform.parent == null)
                        return current.name;
                    return GetFullName(current.transform.parent.gameObject) + "/" + current.name;
                }

                public bool IsValid { get { return mTargetFieldInfo != null && mTargetFieldOwner != null; } }
                public float Value
                {
                    get
                    {
                        int last = mTargetFieldInfo.Length - 1;
                        object obj = mTargetFieldOwner[0];
                        for (int i = 0; i < last; ++i)
                            obj = mTargetFieldInfo[i].GetValue(obj);
                        return Convert.ToSingle(mTargetFieldInfo[last].GetValue(obj));
                    }
                    set
                    {
                        int last = mTargetFieldInfo.Length - 1;
                        for (int i = 0; i < last; ++i)
                            mTargetFieldOwner[i + 1] = mTargetFieldInfo[i].GetValue(mTargetFieldOwner[i]);
                        mTargetFieldInfo[last].SetValue(mTargetFieldOwner[last], value);
                        for (int i = last - 1; i >= 0; --i)
                            mTargetFieldInfo[i].SetValue(mTargetFieldOwner[i], mTargetFieldOwner[i + 1]);
                    }
                }

                // This is the initial value that was saved when the binding was created
                public float InitialValue { get { return mInitialValue; } }
            }
            internal TargetBinding Binding { get; set; }
        }

        [Tooltip("The Cinemachine fields to modify")]
        public TargetModifier[] m_TargetMappings = null;

        void OnEnable()
        {
            InvalidateBindings();
        }

        void OnDisable()
        {
            // Restore initial values
            InvalidateBindings();
        }

        /// Call this from the editor when something changes
        public void InvalidateBindings()
        {
            if (m_TargetMappings == null)
                return;
            for (int i = 0; i < m_TargetMappings.Length; ++i)
            {
                // Restore initial values
                if (m_TargetMappings[i].Binding != null)
                    m_TargetMappings[i].Binding.Value = m_TargetMappings[i].Binding.InitialValue;
                m_TargetMappings[i].Binding = null;
            }
        }

        private void LateUpdate()
        {
            if (m_TargetMappings == null)
                return;
            for (int i = 0; i < m_TargetMappings.Length; ++i)
            {
                if (m_TargetMappings[i].m_Expression.GetNumLines() == 0)
                    continue;

                if (m_TargetMappings[i].Binding == null)
                    m_TargetMappings[i].Binding = TargetModifier.TargetBinding.BindTarget(
                            gameObject, m_TargetMappings[i].m_Field);
                if (!m_TargetMappings[i].Binding.IsValid)
                    continue;

                float value = m_TargetMappings[i].Binding.InitialValue;
                float expr = 0;
                if (!m_TargetMappings[i].m_Expression.Evaluate(Blackboard.CinemachineBlackboard, out expr))
                    continue;
                switch (m_TargetMappings[i].m_Operation)
                {
                    case CombineMode.Set: value = expr; break;
                    case CombineMode.Add: value += expr; break;
                    case CombineMode.Subtract: value -= expr; break;
                    case CombineMode.Multiply: value *= expr; break;
                    case CombineMode.Divide: value /= expr; break;
                }
                if (!float.IsInfinity(value) && !float.IsNaN(value))
                    m_TargetMappings[i].Binding.Value = value;
            }
        }

        /// Will return only public float fields
        public class GameObjectFieldScanner
        {
            /// <summary>
            /// Called for each leaf field.  Return value should be false to abort, true to continue.
            /// It will be propagated back to the caller.
            /// </summary>
            public OnLeafFieldDelegate OnLeafField;
            public delegate bool OnLeafFieldDelegate(
                string fullName, List<FieldInfo> fieldInfo, object rootFieldOwner, object value);

            /// <summary>
            /// Which fields will be scanned
            /// </summary>
            public BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            bool ScanFields(string fullName, List<FieldInfo> fieldChain, object obj, object rootOwner)
            {
                FieldInfo fieldInfo = fieldChain[fieldChain.Count - 1];

                // Check if it's a complex type
                bool isLeaf = true;
                if (obj != null
                    && !fieldInfo.FieldType.IsSubclassOf(typeof(Component))
                    && !fieldInfo.FieldType.IsSubclassOf(typeof(GameObject)))
                {
                    // Check if it's a complex type
                    FieldInfo[] fields = fieldInfo.FieldType.GetFields(bindingFlags);
                    if (fields.Length > 0)
                    {
                        isLeaf = false;
                        fieldChain.Add(fields[0]);
                        for (int i = 0; i < fields.Length; ++i)
                        {
                            fieldChain[fieldChain.Count - 1] = fields[i];
                            string name = fullName + "." + fields[i].Name;
                            if (!ScanFields(name, fieldChain, fields[i].GetValue(obj), rootOwner))
                                return false;
                        }
                        fieldChain.RemoveAt(fieldChain.Count - 1);
                    }
                }
                // If it's a leaf field of the right type then call the leaf handler
                if (isLeaf && OnLeafField != null && fieldInfo.FieldType == typeof(float))
                    if (!OnLeafField(fullName, fieldChain, rootOwner, obj))
                        return false;

                return true;
            }

            public bool ScanFields(string fullName, MonoBehaviour b)
            {
                // A little special handling for some known classes
                if (b.GetType() == typeof(Reactor) || b.GetType().IsSubclassOf(typeof(Reactor)))
                    return true;
                CinemachineVirtualCameraBase vcam = b as CinemachineVirtualCameraBase;

                List<FieldInfo> fieldChain = new List<FieldInfo>();
                FieldInfo[] fields = b.GetType().GetFields(bindingFlags);
                if (fields.Length > 0)
                {
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        if (vcam != null && Array.FindIndex(
                                vcam.m_ExcludedPropertiesInInspector, match => match == fields[i].Name) >= 0)
                        {
                            // Not settable by user, so don't show it
                            continue;
                        }
                        string name = fullName + "." + fields[i].Name;
                        object fieldValue = fields[i].GetValue(b);
                        if (fieldValue != null)
                        {
                            fieldChain.Clear();
                            fieldChain.Add(fields[i]);
                            if (!ScanFields(name, fieldChain, fieldValue, b))
                                return false;
                        }
                    }
                }
                return true;
            }

            /// <summary>
            /// Recursively scan the MonoBehaviours of a GameObject and its children.
            /// For each leaf field found, call the OnLeafField delegate.
            /// Returns false if the operation was aborted by the delegate, tru if it went to completion.
            /// </summary>
            public bool ScanFields(GameObject go, string prefix = null)
            {
                if (prefix == null)
                    prefix = "";
                else if (prefix.Length > 0)
                    prefix += ".";

                MonoBehaviour[] components = go.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour c in components)
                    if (c != null && !ScanFields(prefix + c.GetType().FullName, c))
                        return false;

                foreach (Transform child in go.transform)
                    if (!ScanFields(child.gameObject, prefix + child.gameObject.name))
                        return false;

                return true;
            }
        };
    }
}
