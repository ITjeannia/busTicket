using BusTicket.ViewModels;

namespace BusTicket.Views.Search;

public partial class ResultsPage : ContentPage
{
    public ResultsPage(ResultsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
