# BusTicket — version .NET MAUI

Réécriture en .NET MAUI (C# / XAML, MVVM) de l'application Flutter `bus_ticket`,
d'après la maquette Figma « BusTicket — Écrans (360 × 800) ».

## Lancer l'application

```bash
dotnet build -t:Run -f net10.0-android            # Android (émulateur ou téléphone branché)
dotnet build -t:Run -f net10.0-windows10.0.19041.0 # Windows
```

## Organisation

| Dossier | Contenu |
|---|---|
| `Views/` | Les écrans en XAML, un dossier par partie : `Auth`, `Search`, `Booking`, `Tickets`, `Profile` |
| `ViewModels/` | La logique de chaque écran (MVVM, CommunityToolkit.Mvvm). `Items/` = éléments de liste |
| `Controls/` | Composants réutilisables : `AppTextField`, `AppButton`, `AlertBanner`, `StatusChip`, `EmptyState`… |
| `Models/` | Données : `Trip`, `Booking`, `User`, `SearchQuery`, `PaymentMethod` |
| `Services/` | Données simulées et navigation. **Pour brancher une vraie API, ne modifier que ce dossier.** |
| `Helpers/` | Formatage (dates, prix en FCFA), validation des formulaires, icônes Material |
| `Theme/` | Couleurs (`AppColors.cs`), polices et espacements — source unique |
| `Resources/Styles/Styles.xaml` | Styles XAML (titres, cartes, liens…) |

## Ajouter un écran

1. Créer `Views/<Partie>/MonEcranPage.xaml` + son `ViewModels/MonEcranViewModel.cs`.
2. Les enregistrer dans `MauiProgram.cs` (`AddTransient`).
3. Ajouter la route dans `Services/Navigator.cs` (`Routes`) et dans `AppShell.xaml.cs`.

## Règles de démonstration (données simulées)

- **Connexion** : tout mot de passe de 8 caractères avec un chiffre (ex. `voyage123`).
- **Paiement refusé** : numéro se terminant par `000`.
- **Aucun départ** : le dimanche.
- **Connexion impossible** : `MockData.SimulateNetworkError = true`.
