using BusTicket.Theme;

namespace BusTicket.Controls;

public enum Entrance { None, FadeUp, Fade, Pop, SlideLeft }

/// <summary>
/// Animations d'apparition déclarées en XAML :
/// <c>c:Motion.Entrance="FadeUp" c:Motion.Delay="80"</c>.
/// L'élément apparaît en fondu (et glisse ou grossit) à son premier affichage.
/// </summary>
public static class Motion
{
    public static readonly BindableProperty EntranceProperty = BindableProperty.CreateAttached(
        "Entrance", typeof(Entrance), typeof(Motion), Entrance.None, propertyChanged: OnEntranceChanged);

    public static readonly BindableProperty DelayProperty = BindableProperty.CreateAttached(
        "Delay", typeof(int), typeof(Motion), 0);

    public static Entrance GetEntrance(BindableObject view) => (Entrance)view.GetValue(EntranceProperty);
    public static void SetEntrance(BindableObject view, Entrance value) => view.SetValue(EntranceProperty, value);
    public static int GetDelay(BindableObject view) => (int)view.GetValue(DelayProperty);
    public static void SetDelay(BindableObject view, int value) => view.SetValue(DelayProperty, value);

    private static void OnEntranceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement view) return;
        view.Loaded -= OnLoaded;
        if ((Entrance)newValue == Entrance.None) return;
        Prepare(view, (Entrance)newValue);
        view.Loaded += OnLoaded;
    }

    private static void Prepare(VisualElement view, Entrance entrance)
    {
        view.Opacity = 0;
        switch (entrance)
        {
            case Entrance.FadeUp: view.TranslationY = 18; break;
            case Entrance.SlideLeft: view.TranslationX = 28; break;
            case Entrance.Pop: view.Scale = 0.85; break;
        }
    }

    private static async void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        view.Loaded -= OnLoaded;
        await PlayAsync(view, GetEntrance(view), GetDelay(view));
    }

    /// <summary>Rejoue une entrée (ex. à chaque retour sur un onglet).</summary>
    public static Task ReplayAsync(VisualElement view)
    {
        var entrance = GetEntrance(view);
        if (entrance == Entrance.None) return Task.CompletedTask;
        view.AbortAnimation("entrance");
        Prepare(view, entrance);
        return PlayAsync(view, entrance, GetDelay(view));
    }

    private static async Task PlayAsync(VisualElement view, Entrance entrance, int delay)
    {
        try
        {
            if (delay > 0) await Task.Delay(delay);
            var easing = entrance == Entrance.Pop ? Easing.SpringOut : Easing.CubicOut;
            var duration = entrance == Entrance.Pop ? AppMotion.Slow : AppMotion.Normal + 80;
            await Task.WhenAll(
                view.FadeToAsync(1, duration, Easing.CubicOut),
                view.TranslateToAsync(0, 0, duration, easing),
                view.ScaleToAsync(1, duration, easing));
        }
        catch (Exception)
        {
            // Vue retirée pendant l'animation : rien à faire, mais on la laisse visible.
            view.Opacity = 1;
        }
    }

    /// <summary>Petit enfoncement au toucher (boutons, cartes).</summary>
    public static async Task PressAsync(VisualElement view)
    {
        await view.ScaleToAsync(0.97, 70, Easing.CubicOut);
        await view.ScaleToAsync(1, 110, Easing.CubicOut);
    }

    /// <summary>Secousse horizontale (erreur de saisie).</summary>
    public static async Task ShakeAsync(VisualElement view)
    {
        foreach (var x in new double[] { -10, 9, -6, 4, 0 })
            await view.TranslateToAsync(x, 0, 45);
    }

    /// <summary>Pulsation continue (indicateurs « en direct »). S'arrête avec view.AbortAnimation("pulse").</summary>
    public static void Pulse(VisualElement view, double min = 0.45, uint period = 900)
    {
        var animation = new Animation
        {
            { 0, 0.5, new Animation(v => view.Opacity = v, 1, min, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => view.Opacity = v, min, 1, Easing.SinInOut) },
        };
        animation.Commit(view, "pulse", length: period, repeat: () => true);
    }
}
