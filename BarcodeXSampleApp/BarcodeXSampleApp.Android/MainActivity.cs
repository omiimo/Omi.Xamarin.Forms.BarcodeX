using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Omi.Xamarin.Forms.BarcodeX.Android;
using Android.Support.V4.App;
using Android;

namespace BarcodeXSampleApp.Droid
{
    [Activity(Label = "BarcodeXSampleApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {			
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
			if (ActivityCompat.CheckSelfPermission(Application.Context, Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
			{
				ActivityCompat.RequestPermissions(this, new string[]
					{
						Manifest.Permission.Camera
					}, 1001);
			}

			LoadApplication(new App());
        }
    }
}