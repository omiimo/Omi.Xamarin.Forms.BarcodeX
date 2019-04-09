using Omi.Xamarin.Forms.BarcodeX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BarcodeXSampleApp
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ScannerPage : ContentPage
	{
		public ScannerPage ()
		{
			InitializeComponent ();
		}

		private async void BarcodeScan_BarcodeChanged(object sender, string e)
		{
			BarcodeScanner barcodeScanner = sender as BarcodeScanner;
			barcodeScanner.StopScan();
			await DisplayAlert("Barcode", e, "OK");
			barcodeScan.StartScan();

		}
	}
}