using System;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    /// EF6's property-level <c>[Index]</c>, reimplemented so the entity classes compile
    /// unchanged on .NET 9.
    ///
    /// EF Core dropped this attribute in favour of a class-level
    /// <c>[Index(nameof(Prop), IsUnique = true)]</c>. The entity files use the EF6 form in
    /// 36 places, and EF6 still needs them exactly as they are until the switch is
    /// complete - so rewriting them now would mean touching 36 lines twice and breaking
    /// the running site in between.
    ///
    /// Instead the attribute is redefined here, in EF6's own namespace, and
    /// <see cref="ZkData.Core.Ef6Compat.IndexConventions"/> reads it back and declares the
    /// indexes to EF Core. The entity sources stay byte-identical between the two models,
    /// which is what makes the schema diff meaningful: a difference in the diff is a real
    /// difference, not an artefact of transcription.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IndexAttribute : Attribute
    {
        public IndexAttribute() { }

        public IndexAttribute(string name) { Name = name; }

        public IndexAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; set; }
        public int Order { get; set; } = -1;
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
    }
}
