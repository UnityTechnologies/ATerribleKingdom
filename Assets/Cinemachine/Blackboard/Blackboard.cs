using System.Collections.Generic;

namespace Cinemachine.Blackboard
{
    /// <summary>
    /// A lightweight string to float blackboard to use in Cinemachine Reactor and other places
    /// </summary>
    [DocumentationSorting(300, DocumentationSortingAttribute.Level.UserRef)]
    public class Blackboard
    {
        /// <summary>
        /// The globally accessible blackboard used by Reactor
        /// </summary>
        public static readonly Blackboard CinemachineBlackboard = new Blackboard("Cinemachine");

        /// <summary>
        /// Gets the name of this blackboard
        /// </summary>
        public string Name { get; private set; }
        private Dictionary<string, float> mValues = new Dictionary<string, float>();

        /// <summary>
        /// Returns all keys present in this blackboard
        /// </summary>
        public IEnumerable<string> Keys { get { return mValues.Keys; } }

        public Blackboard(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets the value of the supplied key on the blackboard
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, float value)
        {
            mValues[key] = value;
        }

        /// <summary>
        /// Attempts to retrieve the value of a key on this blackboard
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns><b>TRUE</b> if the key exists. <b>FALSE</b> otherwise</returns>
        public bool TryGetValue(string key, out float value)
        {
            return mValues.TryGetValue(key, out value);
        }

        /// <summary>
        /// Removes the supplied key from this blackboard
        /// </summary>
        /// <param name="key"></param>
        public void RemoveKey(string key)
        {
            mValues.Remove(key);
        }
    }
}
