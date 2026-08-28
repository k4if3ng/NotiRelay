using Microsoft.UI.Windowing;
using System;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;

namespace NotiRelay.Infrastructure
{
	internal sealed class WindowPlacementService
	{
		private const string HeightKey = "WindowHeight";
		private const string IsMaximizedKey = "WindowIsMaximized";
		private const string WidthKey = "WindowWidth";
		private const string XKey = "WindowX";
		private const string YKey = "WindowY";

		private readonly AppWindow _appWindow;
		private readonly SizeInt32 _minimumSize;
		private readonly IPropertySet _settings =
			ApplicationData.Current.LocalSettings.Values;
		private bool _isRestoring;

		public WindowPlacementService(
			AppWindow appWindow,
			SizeInt32 defaultSize,
			SizeInt32 minimumSize)
		{
			_appWindow = appWindow;
			_minimumSize = minimumSize;
			Restore(defaultSize);
			_appWindow.Changed += AppWindow_Changed;
		}

		private void Restore(SizeInt32 defaultSize)
		{
			_isRestoring = true;
			try
			{
				var requestedBounds = TryReadBounds() ?? CenterOnCurrentDisplay(defaultSize);
				var displayArea = DisplayArea.GetFromRect(requestedBounds, DisplayAreaFallback.Primary);
				_appWindow.MoveAndResize(ClampToWorkArea(requestedBounds, displayArea.WorkArea, _minimumSize));

				if (_settings[IsMaximizedKey] is true &&
					_appWindow.Presenter is OverlappedPresenter presenter)
				{
					presenter.Maximize();
				}
			}
			finally
			{
				_isRestoring = false;
			}
		}

		private RectInt32? TryReadBounds()
		{
			return _settings[XKey] is int x &&
				_settings[YKey] is int y &&
				_settings[WidthKey] is int width &&
				_settings[HeightKey] is int height
				? new RectInt32(x, y, width, height)
				: null;
		}

		private RectInt32 CenterOnCurrentDisplay(SizeInt32 defaultSize)
		{
			var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
			var workArea = displayArea.WorkArea;
			var width = Math.Min(defaultSize.Width, workArea.Width);
			var height = Math.Min(defaultSize.Height, workArea.Height);
			return new RectInt32(
				workArea.X + (workArea.Width - width) / 2,
				workArea.Y + (workArea.Height - height) / 2,
				width,
				height);
		}

		private static RectInt32 ClampToWorkArea(
			RectInt32 requested,
			RectInt32 workArea,
			SizeInt32 minimumSize)
		{
			var minimumWidth = Math.Min(minimumSize.Width, workArea.Width);
			var minimumHeight = Math.Min(minimumSize.Height, workArea.Height);
			var width = Math.Clamp(requested.Width, minimumWidth, workArea.Width);
			var height = Math.Clamp(requested.Height, minimumHeight, workArea.Height);
			var x = Math.Clamp(requested.X, workArea.X, workArea.X + workArea.Width - width);
			var y = Math.Clamp(requested.Y, workArea.Y, workArea.Y + workArea.Height - height);
			return new RectInt32(x, y, width, height);
		}

		private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
		{
			if (_isRestoring || sender.Presenter is not OverlappedPresenter presenter)
			{
				return;
			}

			if (presenter.State == OverlappedPresenterState.Maximized)
			{
				_settings[IsMaximizedKey] = true;
				return;
			}

			if (presenter.State != OverlappedPresenterState.Restored)
			{
				return;
			}

			_settings[IsMaximizedKey] = false;
			if (!args.DidPositionChange && !args.DidSizeChange && !args.DidPresenterChange)
			{
				return;
			}

			_settings[XKey] = sender.Position.X;
			_settings[YKey] = sender.Position.Y;
			_settings[WidthKey] = sender.Size.Width;
			_settings[HeightKey] = sender.Size.Height;
		}
	}
}
