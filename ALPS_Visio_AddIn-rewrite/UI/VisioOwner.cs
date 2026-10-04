using System;
using System.Windows.Interop;

namespace ALPS_Visio_AddIn_rewrite.UI
{
    /// <summary>
    /// Liefert das Visio-Hauptfenster als Besitzer fuer Dialoge des Add-Ins. Ohne Besitzer
    /// koennen modale Dialoge (vor allem die aus Visio-Events heraus geoeffneten Snap-Dialoge)
    /// hinter Visio verschwinden — Visio wirkt dann eingefroren.
    /// </summary>
    internal static class VisioOwner
    {
        /// <summary>Visio-Hauptfenster fuer WinForms-Dialoge, oder null, wenn nicht ermittelbar.</summary>
        internal static System.Windows.Forms.IWin32Window Win32Window
        {
            get
            {
                IntPtr handle = Handle;
                return handle == IntPtr.Zero ? null : new Win32Handle(handle);
            }
        }

        /// <summary>Setzt das Visio-Hauptfenster als Besitzer eines WPF-Fensters (vor dem Anzeigen aufrufen).</summary>
        internal static void Attach(System.Windows.Window window)
        {
            IntPtr handle = Handle;
            if (handle != IntPtr.Zero)
                new WindowInteropHelper(window).Owner = handle;
        }

        private static IntPtr Handle
        {
            get
            {
                try { return new IntPtr(Globals.ThisAddIn.Application.WindowHandle32); }
                catch (Exception) { return IntPtr.Zero; } // z. B. in Tests ohne Visio-Host
            }
        }

        private sealed class Win32Handle : System.Windows.Forms.IWin32Window
        {
            internal Win32Handle(IntPtr handle) { Handle = handle; }
            public IntPtr Handle { get; }
        }
    }
}
