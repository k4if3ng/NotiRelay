using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace NotiRelay.Infrastructure
{
	internal sealed class WindowMinimumSizeService : IDisposable
	{
		private const uint DefaultDpi = 96;
		private const uint WindowMessageGetMinMaxInfo = 0x0024;
		private static readonly nuint SubclassId = 0x4E6F7469;

		private readonly nint _windowHandle;
		private readonly SizeInt32 _minimumLogicalSize;
		private readonly SubclassProcedure _subclassProcedure;
		private bool _disposed;

		public WindowMinimumSizeService(Window window, SizeInt32 minimumLogicalSize)
		{
			_windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
			_minimumLogicalSize = minimumLogicalSize;
			_subclassProcedure = WindowSubclassProcedure;

			if (!SetWindowSubclass(_windowHandle, _subclassProcedure, SubclassId, 0))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		public static SizeInt32 ToPhysicalPixels(Window window, SizeInt32 logicalSize)
		{
			var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
			var dpi = GetDpiForWindow(windowHandle);
			return new SizeInt32(
				ScaleForDpi(logicalSize.Width, dpi),
				ScaleForDpi(logicalSize.Height, dpi));
		}

		private nint WindowSubclassProcedure(
			nint windowHandle,
			uint message,
			nuint wParam,
			nint lParam,
			nuint subclassId,
			nuint referenceData)
		{
			if (message == WindowMessageGetMinMaxInfo)
			{
				var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
				var dpi = GetDpiForWindow(windowHandle);
				minMaxInfo.MinimumTrackSize.X = ScaleForDpi(_minimumLogicalSize.Width, dpi);
				minMaxInfo.MinimumTrackSize.Y = ScaleForDpi(_minimumLogicalSize.Height, dpi);
				Marshal.StructureToPtr(minMaxInfo, lParam, false);
			}

			return DefSubclassProc(windowHandle, message, wParam, lParam);
		}

		private static int ScaleForDpi(int logicalPixels, uint dpi)
		{
			var effectiveDpi = dpi == 0 ? DefaultDpi : dpi;
			return (int)Math.Ceiling(logicalPixels * effectiveDpi / (double)DefaultDpi);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			RemoveWindowSubclass(_windowHandle, _subclassProcedure, SubclassId);
		}

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate nint SubclassProcedure(
			nint windowHandle,
			uint message,
			nuint wParam,
			nint lParam,
			nuint subclassId,
			nuint referenceData);

		[StructLayout(LayoutKind.Sequential)]
		private struct NativePoint
		{
			public int X;
			public int Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MinMaxInfo
		{
			public NativePoint Reserved;
			public NativePoint MaximumSize;
			public NativePoint MaximumPosition;
			public NativePoint MinimumTrackSize;
			public NativePoint MaximumTrackSize;
		}

		[DllImport("comctl32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowSubclass(
			nint windowHandle,
			SubclassProcedure subclassProcedure,
			nuint subclassId,
			nuint referenceData);

		[DllImport("comctl32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool RemoveWindowSubclass(
			nint windowHandle,
			SubclassProcedure subclassProcedure,
			nuint subclassId);

		[DllImport("comctl32.dll")]
		private static extern nint DefSubclassProc(
			nint windowHandle,
			uint message,
			nuint wParam,
			nint lParam);

		[DllImport("user32.dll")]
		private static extern uint GetDpiForWindow(nint windowHandle);
	}
}
