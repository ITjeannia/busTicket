using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Ligne pointillée (perforation du billet). Horizontale par défaut, verticale avec Vertical="True".</summary>
public class DashedLine : GraphicsView
{
    public static readonly BindableProperty VerticalProperty =
        Ui.Prop<DashedLine, bool>(nameof(Vertical), false, d => d.OnOrientationChanged());

    public bool Vertical { get => (bool)GetValue(VerticalProperty); set => SetValue(VerticalProperty, value); }

    private readonly DashDrawable _drawable = new();

    public DashedLine()
    {
        HeightRequest = 2;
        Drawable = _drawable;
    }

    private void OnOrientationChanged()
    {
        _drawable.Vertical = Vertical;
        if (Vertical)
        {
            HeightRequest = -1;
            WidthRequest = 2;
        }
        Invalidate();
    }

    private sealed class DashDrawable : IDrawable
    {
        public bool Vertical { get; set; }

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.StrokeColor = AppColors.Border;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = [4, 3];
            if (Vertical) canvas.DrawLine(rect.Center.X, rect.Top, rect.Center.X, rect.Bottom);
            else canvas.DrawLine(rect.Left, rect.Center.Y, rect.Right, rect.Center.Y);
        }
    }
}
