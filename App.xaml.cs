using System;
using System.Windows;
using Syncfusion.Licensing;

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        public App()
        {
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCekx3WmFZfVtgcl9HYlZRRWY/P1ZhSXxWdkZhXH5WcXZVR2lZVkV9XUs=");

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register Syncfusion license
            
            base.OnStartup(e);
        }
    }
}
