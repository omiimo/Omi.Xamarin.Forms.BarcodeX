using System;
using System.ComponentModel;
using System.IO;
using Android.Content;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Omi.Xamarin.Forms.BarcodeX;
using Omi.Xamarin.Forms.BarcodeX.Android;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using static Android.Gms.Vision.Detector;
using static Android.Hardware.Camera;
using Camera = Android.Hardware.Camera;
using Frame = Android.Gms.Vision.Frame;

[assembly: ExportRenderer(typeof(BarcodeScanner), typeof(BarcodeScannerRenderer))]
namespace Omi.Xamarin.Forms.BarcodeX.Android
{
	public class BarcodeScannerRenderer : ViewRenderer<BarcodeScanner, SurfaceView>, ISurfaceHolderCallback, IProcessor, Camera.IPreviewCallback
	{
		BarcodeDetector barcodeDetector;
		SurfaceView surfaceView;
		int cheight;
		int cwidth;
		int maxZoom = 1;
		Camera cam;
		CameraInfo cameraInfo;
		Camera.Parameters parameters;

		public BarcodeScannerRenderer(Context context) : base(context)
		{
			cameraInfo = new Camera.CameraInfo();
			Camera.GetCameraInfo(0, cameraInfo);
			cam = Camera.Open(0);
			parameters = cam.GetParameters();

			Camera.Size size = parameters.PictureSize;
			cheight = size.Height;
			cwidth = size.Width;

			maxZoom = parameters.MaxZoom;

			barcodeDetector = new BarcodeDetector.Builder(Context)
			   .SetBarcodeFormats(BarcodeFormat.DataMatrix)
			   .Build();

			barcodeDetector.SetProcessor(this);
			cam.SetPreviewCallback(this);
		}

		public void OnPreviewFrame(byte[] data, Camera camera)
		{
			var pars = camera.GetParameters();
			var imageformat = pars.PreviewFormat;
			if (imageformat == ImageFormatType.Nv21)
			{
				byte[] jpegData = ConvertYuvToJpeg(data, camera);

				Frame frame = new Frame.Builder().SetBitmap(bytesToBitmap(jpegData)).Build();
				//SparseArray barcodes =  barcodeDetector.Detect(frame);
				barcodeDetector.ReceiveFrame(frame);

			}
		}

		private byte[] ConvertYuvToJpeg(byte[] yuvData, Camera camera)
		{
			var cameraParameters = camera.GetParameters();
			var width = cameraParameters.PreviewSize.Width;
			var height = cameraParameters.PreviewSize.Height;
			var yuv = new YuvImage(yuvData, cameraParameters.PreviewFormat, width, height, null);
			var ms = new MemoryStream();
			var quality = 80;   // adjust this as needed
			yuv.CompressToJpeg(new Rect(0, 0, width, height), quality, ms);
			var jpegData = ms.ToArray();
			return jpegData;
		}

		public static Bitmap bytesToBitmap(byte[] imageBytes)
		{
			Bitmap bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

			return bitmap;
		}

		public void ReceiveDetections(Detections detections)
		{
			if (!Element.IsScannerActive)
				return;
			SparseArray qrcodes = detections.DetectedItems;
			if (qrcodes.Size() != 0)
			{
				Vibrator vibrator = (Vibrator)Context.GetSystemService(Context.VibratorService);
				vibrator.Vibrate(100);
				Element.Barcode = ((Barcode)qrcodes.ValueAt(0)).RawValue;
				cam.StopPreview();
				Element.IsScannerActive = false;
				Element.Barcode = null;
			}
		}

		public void Release()
		{

		}

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
		{
			if (height > width)
			{
				cam.SetDisplayOrientation(90);
			}
			else
			{
				cam.SetDisplayOrientation(0);
			}
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			InitCam();
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			cam.StopPreview();
			cam.Release();
		}

		protected override void OnElementChanged(ElementChangedEventArgs<BarcodeScanner> e)
		{
			base.OnElementChanged(e);
			surfaceView = new SurfaceView(Context);
			surfaceView.Holder.AddCallback(this);
			SetNativeControl(surfaceView);

			Element.MaxZoom = maxZoom;
			Element.SlideToZoom = 1;
		}


		private void InitCam()
		{
			cam.SetDisplayOrientation(90);
			cam.SetPreviewDisplay(surfaceView.Holder);
			cam.StartPreview();

			if (parameters.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
				parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
			else if (parameters.SupportedFocusModes.Contains(Camera.Parameters.FocusModeMacro))
				parameters.FocusMode = Camera.Parameters.FocusModeMacro;
						

			cam.SetParameters(parameters);

		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			UpdateZoom(e);
			UpdateFlash(e);
			IsScannerActive(e);
		}

		private void IsScannerActive(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Element.IsScannerActive))
			{
				if (Element.IsScannerActive)
					cam.StartPreview();
				else
					cam.StopPreview();

			}
		}

		private void UpdateZoom(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Element.SlideToZoom))
			{
				if (Element.SlideToZoom < 1)
					return;

				if (parameters.IsSmoothZoomSupported)
				{
					cam.StartSmoothZoom(Math.Min(maxZoom, (int)Element.SlideToZoom));
				}
				else if (parameters.IsZoomSupported)
				{
					parameters.Zoom = Math.Min(maxZoom, (int)Element.SlideToZoom);
					cam.SetParameters(parameters);
				}


			}

		}

		private void UpdateFlash(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Element.IsFlashOn))
			{
				var flashModes = parameters.SupportedFlashModes;
				if (flashModes == null)
					return;
				if (flashModes.Contains("torch"))
				{
					if (Element.IsFlashOn)
						parameters.FlashMode = "torch";
					else
						parameters.FlashMode = "off";
				}
				else if (flashModes.Contains("on"))
				{
					if (Element.IsFlashOn)
						parameters.FlashMode = "torch";
					else
						parameters.FlashMode = "off";
				}
				cam.SetParameters(parameters);


			}

		}
	}

}
