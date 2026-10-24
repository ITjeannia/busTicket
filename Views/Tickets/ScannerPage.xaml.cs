using System.ComponentModel;
using BusTicket.Core;
using BusTicket.Theme;
using BusTicket.ViewModels;

namespace BusTicket.Views.Tickets;

public partial class ScannerPage : ContentPage
{
    private readonly ScannerViewModel _viewModel;
#if ANDROID || IOS || MACCATALYST
    private ZXing.Net.Maui.Controls.CameraBarcodeReaderView? _camera;
#endif

    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        viewModel.ResultShown += OnResultShown;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartCameraAsync();
        StartScanLine();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ScanLine.AbortAnimation("scan");
#if ANDROID || IOS || MACCATALYST
        if (_camera is not null) _camera.IsDetecting = false;
#endif
    }

    /// <summary>Caméra (Android / iOS) ; sinon, la saisie manuelle reste disponible.</summary>
    private async Task StartCameraAsync()
    {
#if ANDROID || IOS || MACCATALYST
        if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
        {
            ShowNoCamera();
            return;
        }
        if (_camera is null)
        {
            _camera = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView
            {
                Options = new ZXing.Net.Maui.BarcodeReaderOptions
                {
                    Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
                    AutoRotate = true,
                    Multiple = false,
                },
            };
            _camera.BarcodesDetected += (_, e) =>
            {
                var value = e.Results.FirstOrDefault()?.Value;
                if (value is not null) MainThread.BeginInvokeOnMainThread(async () => await _viewModel.OnCodeDetectedAsync(value));
            };
            CameraHost.Content = _camera;
        }
        _camera.IsDetecting = _viewModel.IsScanning;
#else
        await Task.CompletedTask;
        ShowNoCamera();
#endif
    }

    private void ShowNoCamera()
    {
        NoCamera.IsVisible = true;
        Viewfinder.IsVisible = false;
        AimHint.IsVisible = false;
    }

    /// <summary>La ligne verte parcourt le cadre de haut en bas, en boucle.</summary>
    private void StartScanLine()
    {
        if (!Viewfinder.IsVisible) return;
        var animation = new Animation
        {
            { 0, 0.5, new Animation(v => ScanLine.TranslationY = v, 6, 230, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => ScanLine.TranslationY = v, 230, 6, Easing.SinInOut) },
        };
        animation.Commit(ScanLine, "scan", length: 2200, repeat: () => true);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
#if ANDROID || IOS || MACCATALYST
        if (e.PropertyName == nameof(ScannerViewModel.IsScanning) && _camera is not null)
            _camera.IsDetecting = _viewModel.IsScanning;
#endif
    }

    /// <summary>Feuille de résultat qui monte, icône qui « pop », vibration (longue si refusé).</summary>
    private async void OnResultShown(object? sender, VerificationResult result)
    {
        try
        {
            if (result is VerificationResult.Valid or VerificationResult.ValidOffline)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            else
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400));
        }
        catch (Exception) { /* pas de vibreur */ }

        ResultLayer.Opacity = 0;
        ResultSheet.TranslationY = 320;
        ResultIconLabel.Scale = 0.3;
        await Task.WhenAll(
            ResultLayer.FadeToAsync(1, AppMotion.Fast),
            ResultSheet.TranslateToAsync(0, 0, AppMotion.Slow, Easing.CubicOut));
        await ResultIconLabel.ScaleToAsync(1, AppMotion.Slow, Easing.SpringOut);
    }
}
