using BusTicket.ViewModels;

namespace BusTicket.Views.Search;

public partial class SearchPage : ContentPage
{
    private readonly SearchViewModel _viewModel;

    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }

    /// <summary>Le bouton fait un demi-tour pendant l'inversion des villes.</summary>
    private async void OnSwapTapped(object? sender, TappedEventArgs e)
    {
        _viewModel.SwapCommand.Execute(null);
        await SwapButton.RotateToAsync(SwapButton.Rotation + 180, 250, Easing.CubicInOut);
    }
}
