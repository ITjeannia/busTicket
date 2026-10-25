# BusTicket — application .NET MAUI + API ASP.NET Core

Réservation de billets de bus au Cameroun : recherche d'un trajet, choix des sièges,
paiement (simulé), reçu, billet PDF avec QR code signé, et contrôle à l'embarquement.

```
BusTicket.slnx
├── BusTicket.csproj        Application mobile (.NET MAUI : Android, iOS, Windows)
├── Core/                   Cœur métier partagé (bibliothèque .NET, sans MAUI ni ASP.NET)
└── Server/                 API REST (ASP.NET Core + SQLite)
```

## Démarrage rapide

```bash
# 1. L'API (port 5080, joignable depuis le réseau local)
dotnet run --project Server

# 2. L'application
dotnet build -t:Run -p:WindowsOnly=true          # Windows (sans les workloads Android / iOS)
dotnet build -t:Run -f net10.0-android           # Android (émulateur ou téléphone branché)
```

## Accès de test

L'écran de connexion n'affiche aucun identifiant. Pour les tests, utiliser le compte de démonstration,
créé automatiquement au premier lancement (côté serveur et dans la base locale hors ligne) :

| Rôle | Identifiant | Mot de passe |
|---|---|---|
| Voyageur | `jean.dupont@mail.com` (ou `+237 6 90 12 34 56`) | `voyage123` |

- **Espace contrôleur** (scan des billets) : se connecter avec ce même compte, puis **Profil › Espace contrôleur**.
- **Nouveau compte** : « Créer un compte » sur l'écran de connexion (mot de passe : 8 caractères minimum, dont 1 chiffre).
- **Paiement** : simulé, n'importe quel numéro Orange Money / MTN MoMo ou carte est accepté.

Ces identifiants sont définis dans `Core/BookingEngine.cs` (`DemoEmail`, `DemoPassword`).
À retirer avant toute mise en production.

### Générer l'APK Android

```bash
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk ^
  -p:AndroidSdkDirectory=D:\sdk -p:JavaSdkDirectory="C:\Program Files\Java\jdk-17" ^
  -p:ApiBaseUrl=http://<IP du PC>:5080
```

`ApiBaseUrl` (facultatif) inscrit l'adresse de l'API dans l'application : le téléphone la joint sans réglage.

L'APK signé se trouve dans `bin/Release/net10.0-android/publish/com.busticket.app-Signed.apk`.
Il est signé avec la clé de débogage : il s'installe sur un téléphone (autoriser les « sources inconnues »)
mais ne peut pas être publié sur le Play Store tel quel.

### Utiliser l'API depuis un téléphone

1. Le téléphone et le PC doivent être sur le même Wi-Fi.
2. Autoriser le port 5080 dans le pare-feu Windows (PowerShell administrateur) :
   `New-NetFirewallRule -DisplayName "BusTicket API" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow`
3. Dans l'application : **Profil › Adresse du serveur** → `http://<IP du PC>:5080` (commande `ipconfig`).

Par défaut, l'application vise `http://10.0.2.2:5080` (le PC vu depuis l'émulateur Android)
ou `http://localhost:5080` (Windows).

## Parcours

| Étape | Écran | Ce qui se passe |
|---|---|---|
| Connexion | `LoginPage` / `RegisterPage` | Compte vérifié par l'API (mot de passe haché PBKDF2), session gardée sur le téléphone |
| Recherche | `SearchPage` → `ResultsPage` | Départs du jour (12 villes, 6 compagnies, Classique / VIP), places restantes en temps réel |
| Sièges | `SeatSelectionPage` | Plan 2+2 (Classique, 28 places) ou 2+1 (VIP, 21 places), rechargé depuis l'API |
| Paiement | `PaymentPage` | Orange Money, MTN MoMo ou carte. **Toujours accepté**, quel que soit le numéro (aucune passerelle branchée) |
| Reçu | `ReceiptPage` | Référence, n° de transaction, détail du prix ; téléchargement du PDF |
| Billet | `TicketPage` | QR code signé, téléchargement PDF (dossier Téléchargements), partage |
| Contrôle | `ScannerPage` (Profil › Espace contrôleur) | Scan caméra (Android / iOS) ou saisie de la référence ; « Valider l'embarquement » rend le billet inutilisable |

Le seul refus possible au paiement : le siège a été vendu entre-temps à quelqu'un d'autre (l'utilisateur choisit un autre siège).

## Hors ligne

Chaque appel part d'abord vers l'API. Si elle est injoignable, **le même moteur** (`Core/BookingEngine.cs`)
tourne sur le téléphone avec une base locale (`local_database.json`) : recherche, réservation, paiement,
PDF et contrôle continuent de fonctionner, avec un bandeau « Mode hors ligne ».
Les billets achetés en ligne sont aussi gardés sur le téléphone (`tickets.json`).

Limite connue : un billet acheté hors ligne n'est pas renvoyé au serveur quand la connexion revient.

## Sécurité des billets

Le QR code contient `BT1|référence|trajet|sièges|signature` (HMAC-SHA256, `Core/Security.cs`).
Un billet modifié ou inventé est refusé, y compris hors ligne. La clé de signature est une clé de démonstration
présente dans l'application ; en production, signer côté serveur avec une clé privée et ne livrer que la clé publique.

## API

| Méthode | Route | Rôle |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | Compte → jeton de session (30 jours) |
| GET | `/api/cities`, `/api/trips?from=&to=&date=`, `/api/trips/{id}` | Villes, départs, plan des sièges |
| POST | `/api/bookings` 🔒 | Réserver + payer (simulé) |
| GET | `/api/bookings` 🔒, `/api/bookings/{ref}` 🔒, `/api/bookings/{ref}/pdf` 🔒 | Billets, billet PDF |
| POST | `/api/tickets/verify` 🔒 | Contrôle (`board: true` = valider l'embarquement) |

🔒 = en-tête `Authorization: Bearer <jeton>`. Exemples dans `Server/BusTicket.Api.http`.
La base `Server/busticket.db` est créée au premier lancement ; la supprimer pour repartir de zéro.

## Organisation de l'application

| Dossier | Contenu |
|---|---|
| `Views/` | Écrans XAML : `Auth`, `Search`, `Booking`, `Tickets`, `Profile` |
| `ViewModels/` | Logique des écrans (MVVM, CommunityToolkit.Mvvm). `Items/` = éléments de liste |
| `Controls/` | Composants : `AppButton`, `AppTextField`, `SegmentedTabs`, `AlertBanner`, `EmptyState`, `Motion` (animations)… |
| `Services/` | `Backend` (API + repli hors ligne), `BookingRepository`, `TicketFiles` (PDF), `Navigator` |
| `Theme/` | Couleurs, polices, espacements, durées d'animation |
| `Resources/Images/` | Illustrations SVG (converties en PNG à la compilation) |

Animations : entrée des éléments (`c:Motion.Entrance="FadeUp"` + `c:Motion.Delay`), cartes en cascade,
indicateur des onglets qui glisse, squelettes qui pulsent, rebond des sièges, étapes du paiement,
coche du reçu, ligne du scanner.

## Ajouter un écran

1. Créer `Views/<Partie>/MonEcranPage.xaml` + `ViewModels/MonEcranViewModel.cs`.
2. Les enregistrer dans `MauiProgram.cs` (`AddTransient`).
3. Ajouter la route dans `Services/Navigator.cs` (`Routes`) et dans `AppShell.xaml.cs`.
