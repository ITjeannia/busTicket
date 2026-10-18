using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Ligne pointillée horizontale (séparation du billet).</summary>
public class DashedLine : GraphicsView
{
    public DashedLine()
    {
        HeightRequest = 2;
        Drawable = new DashDrawable();
    }

    private sealed class DashDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.StrokeColor = AppColors.Border;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = [4, 3];
            canvas.DrawLine(rect.Left, rect.Center.Y, rect.Right, rect.Center.Y);
        }
    }
}
