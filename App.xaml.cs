using System;
using System.Windows;
using Syncfusion.Licensing;

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register Syncfusion license
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCekx3WmFZfVtgcl9HYlZRRWY/P1ZhSXxWdkZhXH5WcXZVR2lZVkF9XUs=");
            
            base.OnStartup(e);
        }
    }
}
