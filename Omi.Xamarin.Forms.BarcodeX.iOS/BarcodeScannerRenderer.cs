using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using AudioToolbox;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Omi.Xamarin.Forms.BarcodeX.iOS;
using Omi.Xamarin.Forms.BarcodeX;

[assembly: ExportRenderer(typeof(BarcodeScanner), typeof(BarcodeScannerRenderer))]
namespace Omi.Xamarin.Forms.BarcodeX.iOS
{
	public class BarcodeScannerRenderer : ViewRenderer, IAVCaptureMetadataOutputObjectsDelegate
	{
		public BarcodeScannerRenderer()
		{

		}
		private BarcodeScanner barcodeScanner;
		private UIView view;
		private AVCaptureVideoPreviewLayer captureVideoPreviewLayer;
		private AVCaptureSession session;
		private NSObject orientationObserverToken;
		private AVCaptureDevice device;
		private AVCaptureDeviceInput input;
		private AVCaptureMetadataOutput output;

		[Export("captureOutput:didOutputMetadataObjects:fromConnection:")]
		public void DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
		{

			foreach (var metadata in metadataObjects)
			{
				if (!barcodeScanner.IsScannerActive)
					return;
				SystemSound.Vibrate.PlaySystemSound();
				string resultstring = ((AVMetadataMachineReadableCodeObject)metadata).StringValue;
				barcodeScanner.Barcode = resultstring;
				barcodeScanner.IsScannerActive = false;
				barcodeScanner.Barcode = null;
				return;
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<View> e)
		{
			base.OnElementChanged(e);
			if (e.NewElement == null)
				return;

			barcodeScanner = (BarcodeScanner)Element;

			if (!InitScanner(barcodeScanner.BarcodeType))
				return;

			view = new UIView(CGRect.Empty)
			{
				BackgroundColor = UIColor.Gray
			};
			view.Layer.AddSublayer(captureVideoPreviewLayer);

			session.StartRunning();
			captureVideoPreviewLayer.Connection.VideoOrientation = GetDeviceOrientation();
			orientationObserverToken = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, UpdateViewOnOrientationChanged);

			SetNativeControl(view);

		}


		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			if (input == null)
				return;
			barcodeScanner.MaxZoom = (float)input.Device.MaxAvailableVideoZoomFactor;
			updateSize(e);
			UpdateZoom(e);
			UpdateFlash(e);
			IsScannerActive(e);
		}

		private void IsScannerActive(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(barcodeScanner.IsScannerActive))
			{
				if (barcodeScanner.IsScannerActive)
					session.StartRunning();
				else
					session.StopRunning();

			}
		}

		private void UpdateZoom(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(barcodeScanner.SlideToZoom))
			{
				if (barcodeScanner.SlideToZoom < 1)
					return;

				input.Device.LockForConfiguration(out NSError err);
				if (err != null)
					return;

				nfloat maxzoom = input.Device.MaxAvailableVideoZoomFactor;

				input.Device.VideoZoomFactor = (float)NMath.Min(input.Device.ActiveFormat.VideoMaxZoomFactor, barcodeScanner.SlideToZoom);
				input.Device.UnlockForConfiguration();

			}

		}

		private void UpdateFlash(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(barcodeScanner.IsFlashOn))
			{
				try
				{
					input.Device.LockForConfiguration(out NSError err);
					if (err != null)
						return;

					if (input.Device.HasTorch)
					{
						if (barcodeScanner.IsFlashOn)
							input.Device.TorchMode = AVCaptureTorchMode.On;
						else
							input.Device.TorchMode = AVCaptureTorchMode.Off;
					}
					else
					{
						barcodeScanner.IsFlashOn = false;
					}

					input.Device.UnlockForConfiguration();
				}
				catch { }
			}

		}

		private void updateSize(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.WidthProperty.PropertyName || e.PropertyName == VisualElement.HeightProperty.PropertyName)
				captureVideoPreviewLayer.Frame = new CGRect(0, 0, Element.Width, Element.Height);
		}



		private bool InitScanner(BarcodeScanner.BarcodeFormat barcodeType)
		{
			device = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
			if (device == null)
				return false;

			input = AVCaptureDeviceInput.FromDevice(device);
			if (input.Device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
			{
				input.Device.LockForConfiguration(out NSError err);
				input.Device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
				input.Device.UnlockForConfiguration();
			}

			if (input == null)
				return false;

			output = new AVCaptureMetadataOutput();
			output.SetDelegate(this, DispatchQueue.MainQueue);

			session = new AVCaptureSession();
			session.AddInput(input);
			session.AddOutput(output);
			output.MetadataObjectTypes = GetBarcodeFormat(barcodeType);

			captureVideoPreviewLayer = AVCaptureVideoPreviewLayer.FromSession(session);
			captureVideoPreviewLayer.Frame = CGRect.Empty;
			captureVideoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			captureVideoPreviewLayer.Connection.VideoOrientation = GetDeviceOrientation();			
			return true;
		}

		private AVMetadataObjectType GetBarcodeFormat(BarcodeScanner.BarcodeFormat barcodeType)
		{
			switch(barcodeType)
			{
				case BarcodeScanner.BarcodeFormat.DataMatrix:
					return AVMetadataObjectType.DataMatrixCode;
				case BarcodeScanner.BarcodeFormat.QrCode:
					return AVMetadataObjectType.QRCode;
				case BarcodeScanner.BarcodeFormat.Pdf417:
					return AVMetadataObjectType.PDF417Code;
				case BarcodeScanner.BarcodeFormat.Code128:
					return AVMetadataObjectType.Code128Code;
				case BarcodeScanner.BarcodeFormat.Code39:
					return AVMetadataObjectType.Code39Code;
				case BarcodeScanner.BarcodeFormat.Code93:
					return AVMetadataObjectType.Code93Code;
				case BarcodeScanner.BarcodeFormat.Ean13:
					return AVMetadataObjectType.EAN13Code;
				case BarcodeScanner.BarcodeFormat.Ean8:
					return AVMetadataObjectType.EAN8Code;
				default:
					return AVMetadataObjectType.DataMatrixCode;
			}
		}

		private void UpdateViewOnOrientationChanged(NSNotification obj)
		{
			if (Element == null)
				return;

			var previewLayer = captureVideoPreviewLayer.Connection;
			captureVideoPreviewLayer.Frame = new CGRect(0, 0, Element.Width, Element.Height);

			if (previewLayer.SupportsVideoOrientation)
			{
				previewLayer.VideoOrientation = GetDeviceOrientation();
			}
		}

		private AVCaptureVideoOrientation GetDeviceOrientation()
		{
			return (AVCaptureVideoOrientation)UIApplication.SharedApplication.StatusBarOrientation;
		}

		private void removeOrientationObserver()
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(orientationObserverToken);
		}

	}

}
