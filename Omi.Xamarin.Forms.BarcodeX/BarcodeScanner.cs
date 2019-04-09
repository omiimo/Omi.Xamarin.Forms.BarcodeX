using System;
using Xamarin.Forms;

namespace Omi.Xamarin.Forms.BarcodeX
{
	public class BarcodeScanner : View
	{
		public BarcodeScanner()
		{
		}
		public static readonly BindableProperty BarcodeProperty =
			BindableProperty.Create(nameof(Barcode), typeof(string), typeof(BarcodeScanner), null, propertyChanged: OnCarcodeChanged);

		/// <summary>
		/// Event will be fired as soon as barcode is detected.
		/// </summary>
		public event EventHandler<string> BarcodeChanged;

		public static readonly BindableProperty SlideToZoomProperty =
			BindableProperty.Create(nameof(SlideToZoom), typeof(float), typeof(BarcodeScanner), 1f);

		public static readonly BindableProperty IsFlashOnProperty =
			BindableProperty.Create(nameof(IsFlashOn), typeof(bool), typeof(BarcodeScanner), false);

		public static readonly BindableProperty MaxZoomProperty =
			BindableProperty.Create(nameof(MaxZoom), typeof(float), typeof(BarcodeScanner), 1f);

		public static readonly BindableProperty IsScannerActiveProperty =
			BindableProperty.Create(nameof(IsScannerActive), typeof(bool), typeof(BarcodeScanner), false);



		/// <summary>
		/// Start Barcode scanning process
		/// </summary>
		public void StartScan()
		{
			IsScannerActive = true;
		}

		/// <summary>
		/// Stop barcode scanning process
		/// </summary>
		public void StopScan()
		{
			IsScannerActive = false;
		}

		public bool IsScannerActive
		{
			get { return (bool)GetValue(IsScannerActiveProperty); }
			set { SetValue(IsScannerActiveProperty, value); }
		}

		/// <summary>
		/// Turn camera flash on/off
		/// </summary>
		public bool IsFlashOn
		{
			get { return (bool)GetValue(IsFlashOnProperty); }
			set
			{
				SetValue(IsFlashOnProperty, value);
			}
		}

		/// <summary>
		/// Zoom value
		/// </summary>
		public float SlideToZoom
		{
			get { return (float)GetValue(SlideToZoomProperty); }
			set { SetValue(SlideToZoomProperty, value); }
		}

		/// <summary>
		/// Max available zoom.
		/// </summary>
		public float MaxZoom
		{
			get { return (float)GetValue(MaxZoomProperty); }
			set { SetValue(MaxZoomProperty, value); }
		}

		/// <summary>
		/// Current barcode value
		/// </summary>
		public string Barcode
		{
			get { return (string)GetValue(BarcodeProperty); }
			set { SetValue(BarcodeProperty, value); }
		}



		private static void OnCarcodeChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (newValue == null)
			{
				return;
			}
			var bobj = (BarcodeScanner)bindable;

			bobj.BarcodeChanged?.Invoke(bobj, newValue.ToString());

		}
	}

}
