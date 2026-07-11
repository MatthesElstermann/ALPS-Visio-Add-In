// Compiler-Polyfills fuer den portierten pass-bpmn-converter (net9 → net48):
// Der Roslyn-Compiler von VS 2022 unterstuetzt C#-12-Sprachfeatures auch auf
// .NET Framework, verlangt fuer "required"-Member aber diese Attribut-Typen,
// die es erst ab .NET 7 in der BCL gibt. Die Definitionen sind intern und
// wirken nur zur Compilezeit.
using System;
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>Ermoeglicht "init"-Accessoren auf .NET Framework.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }

    /// <summary>Markiert "required"-Member (C# 11) fuer den Compiler.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    /// <summary>Vom Compiler auf Typen mit "required"-Membern gesetzt.</summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }
        public bool IsOptional { get; set; }

        public const string RefStructs = nameof(RefStructs);
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Kennzeichnet Konstruktoren, die alle "required"-Member setzen.</summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}
