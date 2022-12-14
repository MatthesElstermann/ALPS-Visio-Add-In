// AnchorBarsUsage.cs
// <copyright>Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>This class demonstrates creating an add-on created anchor bar with a
// list view control and shows how to set its docking, positioning, and merge
// behavior.
// Displays the DisplayDirectory as anchored window </summary>

using System;
using System.Windows.Interop;
using VisioAddIn.Snapping;

namespace VisioAddIn
{

    /// <summary>This class demonstrates creating an add-on created anchor bar
    /// with a list view control and shows how to set its docking, positioning,
    /// and merge behavior.</summary>
    public class AnchorBarsUsage
    {
        private ThisAddIn Addin;
        private ModelController ModelController;
        private readonly string anchorBarTitle = VisioAddIn.Resources.strings.AnchorBarTitle;
        private readonly string anchorBarMergeTitle = VisioAddIn.Resources.strings.AnchorBarMergeTitle;

        /// <summary>GUID that identifies the custom anchor window when it 
        /// is merged.</summary>
        private const string customMergeId =
            "{91439584-A97D-46e8-92E3-AD10BA4C8B6B}";

        /// <summary>This constructor is intentionally left blank.</summary>
        public AnchorBarsUsage(ThisAddIn addin, ModelController modelController)
        {
            Addin = addin;
            ModelController = modelController;
            // No initialization is required.
        }

        /// <summary>This method adds an anchor bar, sets the properties of
        /// the anchor bar, and adds a form as contents of the anchor bar. The
        /// form will contain a list view of masters from stencils in the Basic
        /// Flowchart template. The list view items can be dragged onto the
        /// drawing page.</summary>
        /// <param name="visioApplication">A running Visio application</param>
        /// <param name="runningFromAddIn">whether or not we are running from an add-in</param>
        /// <returns>true if successful, otherwise false</returns>
        public WindowDirectory CreateAnchorBar(
            Microsoft.Office.Interop.Visio.Application visioApplication)
        {

            Microsoft.Office.Interop.Visio.Window anchorWindow;
            WindowDirectory directory;
            //Directory directory;
            object windowStates;
            object windowTypes;

            try
            {
                // The anchor bar will be docked to the bottom-left corner of the
                // drawing window and set the anchor bar to auto-hide when not in
                // use.
                windowStates = Microsoft.Office.Interop.Visio.
                        VisWindowStates.visWSAnchorBottom |
                    Microsoft.Office.Interop.Visio.
                        VisWindowStates.visWSAnchorLeft |
                    Microsoft.Office.Interop.Visio.
                        VisWindowStates.visWSAnchorAutoHide |
                    Microsoft.Office.Interop.Visio.
                        VisWindowStates.visWSVisible;

                // The anchor bar is a window created by an add-on
                windowTypes = Microsoft.Office.Interop.Visio.
                    VisWinTypes.visAnchorBarAddon;

                // Add a custom anchor bar window.
                anchorWindow = addAnchorWindow(visioApplication,
                    anchorBarTitle,
                    windowStates,
                    windowTypes);

                // Set the form as contents of the anchor bar.
                //directory = new Directory(Addin, ModelController);
                //directory.ParentVisioApplication = visioApplication;

                directory = new WindowDirectory(Addin, ModelController);
                directory.ParentVisioApplication = visioApplication;


                addFormToAnchorWindow(anchorWindow,
                    directory);

                // The MergeID allows the anchor bar window to be identified 
                // when it is merged with another window.
                anchorWindow.MergeID = customMergeId;

                // Allow the anchor window to be merged with other windows that
                // have a zero-length MergeClass property value.
                anchorWindow.MergeClass = "";

                // Set the MergeCaption property with string that is shorter 
                // than the window caption. The MergeCaption property value 
                // appears on the tab of the merged window.
                anchorWindow.MergeCaption = anchorBarMergeTitle;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine(err.Message);
                throw;
            }

            return directory;
        }

        /// <summary>This method adds an anchor bar with the specified
        /// caption, window properties and anchor bar types.</summary>
        /// <param name="visioApplication">Reference to the Visio Application
        /// object</param>
        /// <param name="caption">Anchor bar caption</param>
        /// <param name="windowStates">Properties of the anchor bar</param>
        /// <param name="windowTypes">Built-in or add-on anchor bar</param>
        /// <returns>Created window, otherwise null</returns>
        private Microsoft.Office.Interop.Visio.Window addAnchorWindow(
            Microsoft.Office.Interop.Visio.Application visioApplication,
            string caption,
            object windowStates,
            object windowTypes)
        {

            Microsoft.Office.Interop.Visio.Window anchorWindow = null;

            try
            {

                // Add a new anchor bar with the required information.
                anchorWindow = visioApplication.ActiveWindow.Windows.Add(
                    caption,
                    windowStates,
                    windowTypes,
                    8, 10, 500, 300, 0, 300, 210);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine(err.Message);
                System.Diagnostics.Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                throw;
            }

            return anchorWindow;
        }

        /// <summary>This method adds a form as the contents of the anchor bar.
        /// </summary>
        /// <param name="anchorBar">Reference to the anchor bar window</param>
        /// <param name="displayWindow">Content of the anchor bar</param>
        private void addFormToAnchorWindow(
            Microsoft.Office.Interop.Visio.Window anchorBar,
            WindowDirectory displayWindow)
        {

            int left;
            int top;
            int width;
            int height;
            int windowHandle;

            try
            {
                //changeing DPI awareness



                // Show the form as a modeless dialog.
                displayWindow.Show();

                // Get the window handle of the form.
                //windowHandle = displayForm.Handle.ToInt32();
                windowHandle = new WindowInteropHelper(displayWindow).Handle.ToInt32();

                // Set the form as a visible child window.
                if (NativeMethods.SetWindowLongW(windowHandle,
                    NativeMethods.GWL_STYLE,
                    NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE) == 0 &&
                    System.Runtime.InteropServices.Marshal.GetLastWin32Error() != 0)
                {
                    throw new Exception("Can not set windowslong");
                }

                // Set the anchor bar window as the parent of the form.
                if (NativeMethods.SetParent(windowHandle,
                    anchorBar.WindowHandle32) == 0)
                    throw new Exception("Can not set parent");

                // Set the dock property of the form to fill, so that the form
                // automatically resizes to the size of the anchor bar.

                // Resize the anchor bar so it will refresh.
                anchorBar.GetWindowRect(out left,
                    out top,
                    out width,
                    out height);

                //displayForm.Dock = System.Windows.Forms.DockStyle.Fill;

                anchorBar.SetWindowRect(left,
                    top,
                    width,
                    height + 1);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine(err.Message);
                System.Diagnostics.Debug.WriteLine("Error: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                throw;
            }
        }

        /// <summary>Explicitly declare calls to unmanaged code inside a
        /// 'NativeMethods' class.  This class does not suppress stack walks for
        /// unmanaged code permission.</summary>
        private class NativeMethods
        {

            /// <summary>Windows constant - Sets a new window style.</summary>
            internal const short GWL_STYLE = (-16);

            /// <summary>Windows constant - Creates a child window..</summary>
            internal const int WS_CHILD = 0x40000000;

            /// <summary>Windows constant - Creates a window that is initially
            /// visible.</summary>
            internal const int WS_VISIBLE = 0x10000000;

            /// <summary>Declare a private constructor to prevent new instances
            /// of the NativeMethods class from being created. This constructor
            /// is intentionally left blank.</summary>
            private NativeMethods()
            {

                // No initialization is required.
            }

            /// <summary>Prototype of SetParent() for PInvoke</summary>
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            internal static extern int SetParent(int hWndChild,
                int hWndNewParent);

            /// <summary>Prototype of SetWindowLong() for PInvoke</summary>
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            internal static extern int SetWindowLongW(int hwnd,
                int nIndex,
                int dwNewLong);

            /// <summary>Prototype of SetProcessDPIAware() for PInvoke</summary>
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            internal static extern bool SetProcessDPIAware();

        }
    }
}