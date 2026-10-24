using BusTicket.Core;

namespace BusTicket.Services;

/// <summary>
/// Billet PDF (billet + reçu), généré sur le téléphone : fonctionne même hors ligne.
/// - Télécharger : dossier « Téléchargements » (Android, Windows) ; ailleurs, feuille de partage « Enregistrer dans Fichiers ».
/// - Partager : WhatsApp, e-mail, Drive…
/// </summary>
public class TicketFiles
{
    /// <summary>Écrit le PDF dans le cache de l'application et renvoie son chemin.</summary>
    public async Task<string> CreatePdfAsync(BookingDto booking)
    {
        var bytes = await Task.Run(() => TicketPdf.Create(booking));
        var path = Path.Combine(FileSystem.CacheDirectory, TicketPdf.FileName(booking));
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    /// <summary>Enregistre le PDF dans les téléchargements. Renvoie le chemin, ou null si le partage a été utilisé à la place.</summary>
    public async Task<string?> DownloadAsync(BookingDto booking)
    {
        var bytes = await Task.Run(() => TicketPdf.Create(booking));
        var fileName = TicketPdf.FileName(booking);

#if WINDOWS
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(downloads);
        var target = Path.Combine(downloads, fileName);
        await File.WriteAllBytesAsync(target, bytes);
        return target;
#else
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var resolver = Platform.AppContext.ContentResolver!;
            var values = new Android.Content.ContentValues();
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "application/pdf");
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
            var uri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values)
                      ?? throw new IOException("Impossible de créer le fichier dans Téléchargements");
            await using (var output = resolver.OpenOutputStream(uri) ?? throw new IOException("Écriture impossible"))
                await output.WriteAsync(bytes);
            return $"Téléchargements/{fileName}";
        }
#endif
        // Android < 10, iOS, macOS : la feuille de partage propose « Enregistrer dans Fichiers ».
        await ShareAsync(booking);
        return null;
#endif
    }

    public async Task OpenAsync(BookingDto booking)
    {
        var path = await CreatePdfAsync(booking);
        await Launcher.Default.OpenAsync(new OpenFileRequest($"Billet {booking.Reference}", new ReadOnlyFile(path, "application/pdf")));
    }

    public async Task ShareAsync(BookingDto booking)
    {
        var path = await CreatePdfAsync(booking);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Billet {booking.Reference} · {booking.Trip.Route}",
            File = new ShareFile(path, "application/pdf"),
        });
    }
}
