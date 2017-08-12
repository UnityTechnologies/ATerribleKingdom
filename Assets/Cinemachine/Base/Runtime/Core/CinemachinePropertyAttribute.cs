using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Property applied to LensSettings.  Used for custom drawing in the inspector.
    /// </summary>
    public sealed class LensSettingsPropertyAttribute : PropertyAttribute
    {
    }
    
    /// <summary>
    /// Property applied to CinemachineBlendDefinition.  Used for custom drawing in the inspector.
    /// </summary>
    public sealed class CinemachineBlendDefinitionPropertyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Invoke play-mode-save for a class.  This class's fields will be scanned
    /// upon exiting play mode, and its property values will be applied to the scene object.
    /// This is a stopgap measure that will become obsolete once Unity implements
    /// play-mode-save in a more general way.
    /// </summary>
    public sealed class SaveDuringPlayAttribute : System.Attribute
    {
    }

    /// <summary>
    /// Suppresses play-mode-save for a field.  Use it if the calsee has [SaveDuringPlay] 
    /// attribute but there are fields in the class that shouldn't be saved.
    /// </summary>
    public sealed class NoSaveDuringPlayAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Specify a minimum value on an int, float, or vector
    /// </summary>
    public sealed class MinAttribute : PropertyAttribute
    {
        /// <summary>The minimum value to enforce</summary>
        public readonly float min;
        public MinAttribute(float min)
        {
            this.min = min;
        }
    }

    /// <summary>
    /// Get the inspector to invoke Get/Set property accessors for a field
    /// </summary>
    public sealed class GetSetAttribute : PropertyAttribute
    {
        /// <summary>The name of the property to access instead of the field</summary>
        public readonly string name;
        /// <summary>True if the inspector has changed the field</summary>
        public bool dirty;
        public GetSetAttribute(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// Atrtribute to control the automatic generation of documentation.
    /// </summary>
    [DocumentationSorting(0f, DocumentationSortingAttribute.Level.Undoc)]
    public sealed class DocumentationSortingAttribute : System.Attribute
    {
        /// <summary>Refinement level of the documentation</summary>
        public enum Level 
        { 
            /// <summary>Type is excluded from documentation</summary>
            Undoc, 
            /// <summary>Type is documented in the API reference</summary>
            API, 
            /// <summary>Type is documented in the highly-refined User Manual</summary>
            UserRef 
        };
        /// <summary>Where this type appears in the manual.  Smaller number sort earlier.</summary>
        public float SortOrder { get; private set; }
        /// <summary>Refinement level of the documentation.  The more refined, the more is excluded.</summary>
        public Level Category { get; private set; }

        public DocumentationSortingAttribute(float sortOrder, Level category)
        {
            SortOrder = sortOrder;
            Category = category;
        }
    }
}
