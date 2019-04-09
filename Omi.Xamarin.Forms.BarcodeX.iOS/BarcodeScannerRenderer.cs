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

			if (!InitScanner())
				return;
			barcodeScanner = (BarcodeScanner)Element;

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



		private bool InitScanner()
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
			output.MetadataObjectTypes = AVMetadataObjectType.DataMatrixCode;

			captureVideoPreviewLayer = AVCaptureVideoPreviewLayer.FromSession(session);
			captureVideoPreviewLayer.Frame = CGRect.Empty;
			captureVideoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			captureVideoPreviewLayer.Connection.VideoOrientation = GetDeviceOrientation();

			nfloat maxzoom = input.Device.MaxAvailableVideoZoomFactor / 3;
			if (maxzoom > 0)
			{
				input.Device.LockForConfiguration(out NSError err);
				input.Device.VideoZoomFactor = (float)maxzoom;
				input.Device.UnlockForConfiguration();
			}
			return true;

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
