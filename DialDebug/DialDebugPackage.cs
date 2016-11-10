//------------------------------------------------------------------------------
// <copyright file="DialDebugPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Windows.UI.Input;

namespace DialDebug
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(DialDebugPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class DialDebugPackage : Package
    {
        private DTE _dte;
        private RadialController _radialController;
        private List<RadialControllerMenuItem> _menuItems;

        /// <summary>
        /// DialDebugPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f573c40b-d4a3-4d24-9d43-dc7ef98b4201";

        /// <summary>
        /// Initializes a new instance of the <see cref="DialDebugPackage"/> class.
        /// </summary>
        public DialDebugPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _dte = GetService(typeof(DTE)) as DTE;

            if (_dte == null)
            {
                throw new NullReferenceException("DTE is null");
            }

            CreateController();
            CreateMenuItem();
            HookUpEvents();
        }

        private void CreateController()
        {
            IRadialControllerInterop interop = (IRadialControllerInterop)System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.GetActivationFactory(typeof(RadialController));
            Guid guid = typeof(RadialController).GetInterface("IRadialController").GUID;

            _radialController = interop.CreateForWindow(new IntPtr(_dte.ActiveWindow.HWnd), ref guid);
        }

        private void CreateMenuItem()
        {
            _menuItems = new List<RadialControllerMenuItem>
            {
                RadialControllerMenuItem.CreateFromKnownIcon("Debug", RadialControllerMenuKnownIcon.InkColor),
            };

            foreach (var item in _menuItems)
            {
                _radialController.Menu.Items.Add(item);
            }
        }

        private void HookUpEvents()
        {
            _radialController.RotationChanged += OnRotationChanged;
            _radialController.ButtonClicked += OnButtonClicked;

            _dte.Events.SolutionEvents.AfterClosing += () =>
            {
                _radialController.Menu.Items.Clear();
            };
        }

        private void OnButtonClicked(RadialController sender, RadialControllerButtonClickedEventArgs args)
        {
            if (_dte.Application.Debugger.CurrentMode == dbgDebugMode.dbgRunMode)
            {
                _dte.Application.Debugger.Stop();
            }
            else
            {
                _dte.Application.Debugger.Go();
            }
        }

        private void OnRotationChanged(RadialController sender, RadialControllerRotationChangedEventArgs args)
        {
            if (args.RotationDeltaInDegrees > 0)
            {
                _dte.Application.Debugger.StepOver();
            }
            else
            {
                _dte.Application.Debugger.StepInto();
            }
        }
    }
}
