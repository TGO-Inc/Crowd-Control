﻿#pragma checksum "..\..\..\..\Windows\Main.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "20C2FE5DFD9D212825D3E8DF889E69DA048B1BA3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using CrowdControl.Pages;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Wpf.Ui;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Navigation;
using Wpf.Ui.Converters;
using Wpf.Ui.Markup;
using Wpf.Ui.ValidationRules;


namespace CrowdControl {
    
    
    /// <summary>
    /// Main
    /// </summary>
    public partial class Main : Wpf.Ui.Controls.UiWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 27 "..\..\..\..\Windows\Main.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RootMainGrid;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\..\..\Windows\Main.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.NavigationStore RootNavigation;
        
        #line default
        #line hidden
        
        
        #line 104 "..\..\..\..\Windows\Main.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Frame RootFrame;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "7.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Crowd Control;component/windows/main.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Windows\Main.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "7.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 17 "..\..\..\..\Windows\Main.xaml"
            ((CrowdControl.Main)(target)).Closed += new System.EventHandler(this.UiWindow_Closed);
            
            #line default
            #line hidden
            
            #line 20 "..\..\..\..\Windows\Main.xaml"
            ((CrowdControl.Main)(target)).StateChanged += new System.EventHandler(this.UiWindow_StateChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.RootMainGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 3:
            this.RootNavigation = ((Wpf.Ui.Controls.NavigationStore)(target));
            
            #line 41 "..\..\..\..\Windows\Main.xaml"
            this.RootNavigation.Navigated += new Wpf.Ui.Common.RoutedNavigationEvent(this.RootNavigation_OnNavigated);
            
            #line default
            #line hidden
            return;
            case 4:
            this.RootFrame = ((System.Windows.Controls.Frame)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

