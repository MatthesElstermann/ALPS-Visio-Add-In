// Polyfills fuer den portierten pass-bpmn-converter (net9 → net48):
// 1. Compiler-Attribute: Der Roslyn-Compiler von VS 2022 unterstuetzt C#-12-
//    Sprachfeatures auch auf .NET Framework, verlangt fuer "required"-Member aber
//    Attribut-Typen, die es erst ab .NET 7 in der BCL gibt (compile-only).
// 2. Collection-Erweiterungen: Queue<T>.TryDequeue/Stack<T>.TryPop existieren
//    erst ab .NET Core 2.0 — auf net48 liefern Extension-Methoden den Ersatz.
//    Sie liegen im Namespace PassBpmnConverter und sind damit in allen
//    Unter-Namespaces des Ports ohne using-Direktive sichtbar.
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PassBpmnConverter
{
    internal static class CollectionPolyfills
    {
        /// <summary>net48-Ersatz fuer Queue&lt;T&gt;.TryDequeue (ab .NET Core 2.0 eingebaut).</summary>
        public static bool TryDequeue<T>(this Queue<T> queue, out T result)
        {
            if (queue.Count == 0)
            {
                result = default!;
                return false;
            }
            result = queue.Dequeue();
            return true;
        }

        /// <summary>net48-Ersatz fuer Stack&lt;T&gt;.TryPop (ab .NET Core 2.0 eingebaut).</summary>
        public static bool TryPop<T>(this Stack<T> stack, out T result)
        {
            if (stack.Count == 0)
            {
                result = default!;
                return false;
            }
            result = stack.Pop();
            return true;
        }
    }
}

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
