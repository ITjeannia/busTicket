using System.Text.Json;

namespace BusTicket.Services;

/// <summary>
/// Petits fichiers JSON dans le dossier privé de l'application (session, billets, recherches récentes).
/// Les données restent disponibles hors connexion et après la fermeture de l'application.
/// </summary>
public class LocalStore
{
    public static string PathOf(string name) => Path.Combine(FileSystem.AppDataDirectory, $"{name}.json");

    /// <summary>Renvoie default si le fichier n'existe pas ou est illisible.</summary>
    public T? Load<T>(string name)
    {
        try
        {
            var path = PathOf(name);
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;
        }
        catch (Exception) // fichier corrompu ou d'une ancienne version : on repart de zéro
        {
            return default;
        }
    }

    public void Save<T>(string name, T value)
    {
        try
        {
            File.WriteAllText(PathOf(name), JsonSerializer.Serialize(value));
        }
        catch (IOException)
        {
            // Disque plein ou inaccessible : l'application continue avec les données en mémoire.
        }
    }

    public void Delete(string name)
    {
        try { File.Delete(PathOf(name)); }
        catch (IOException) { }
    }
}
