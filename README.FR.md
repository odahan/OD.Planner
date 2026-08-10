# OD.Planner

Un gestionnaire de tâches léger et natif pour Windows, développé avec WPF et .NET. OD.Planner garde votre liste de tâches à portée de main dans une petite fenêtre toujours prête et s'assure que les échéances ne passent pas inaperçues — avec des alarmes sonores et une priorité par code couleur.

![Version](https://img.shields.io/badge/version-1.3.0-blue)
![License](https://img.shields.io/badge/license-Donationware-green)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)

## Fonctionnalités

- **Gestion des tâches** — ajoutez, modifiez, réorganisez, terminez et supprimez des tâches depuis une fenêtre compacte et redimensionnable.
- **Échéances flexibles** — les échéances peuvent être une date fixe ou un nombre de jours après la création, et peuvent être reportées d'un jour ou d'un simple clic.
- **Urgence d'un coup d'œil** — les tâches sont triées et codées par couleur selon l'échéance, avec un soulignement clignotant pour les tâches qui sont sur le point de devenir en retard (les animations peuvent être réduites ou désactivées).
- **Catégories** — organisez les tâches en catégories et filtrez la liste d'un simple clic.
- **Alarmes sonores** — alertes sonores optionnelles la veille de l'échéance (J-1), le jour de l'échéance (J0), et quand une tâche est en retard. Chaque alarme peut être activée ou désactivée indépendamment.
- **Compatible zone de notification** — minimisez en arrière-plan ; les alarmes et les passages de minuit continuent de fonctionner lorsque l'application est en arrière-plan.
- **Thèmes sombre et clair** — changez à tout moment ; le choix est mémorisé entre les sessions.
- **Disposition persistante** — la fenêtre mémorise sa position et sa taille. Au premier lancement, elle s'ancre en haut à droite de l'écran principal.
- **Stockage SQLite** — vos tâches résident dans un fichier `tasks.db` portable que vous pouvez déplacer depuis les paramètres.

## Prérequis

- Windows 10 ou ultérieur
- [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) (publication autonome également prise en charge)

## Compilation

```powershell
git clone https://github.com/odahan/OD.Planner.git
cd OD.Planner
dotnet build OD.Planner.slnx -c Release
```

Exécuter l'application :

```powershell
dotnet run --project src/OD.Planner -c Release
```

Vous pouvez également ouvrir `OD.Planner.slnx` dans Visual Studio 2022 ou ultérieur.

## Publication d'un exécutable autonome

Un `OD.Planner.exe` autonome en un seul fichier (aucun runtime .NET requis) peut être produit avec le profil de publication fourni :

```powershell
dotnet publish src/OD.Planner -c Release -p:PublishProfile=win-x64
```

L'exécutable est écrit dans `src/OD.Planner/bin/Release/net10.0-windows/win-x64/publish/`. Au premier lancement, il crée `settings.json` et `tasks.db` à côté de l'exécutable.

## Utilisation

- `Ctrl+N` — nouvelle tâche
- `F2` — modifier la tâche sélectionnée
- `Delete` — supprimer la tâche sélectionnée
- Cliquez sur **+1 jour** / **+1 semaine** sur une tâche pour reporter son échéance.
- Ouvrez les **Paramètres** (icône engrenage) pour configurer le thème, les alarmes, les catégories, le démarrage automatique et l'emplacement de la base de données.

## Documentation

Les manuels d'utilisation sont disponibles dans le dossier `docs/` :

- [Manuel d'utilisation (Français)](docs/user-manual-fr.html)
- [English User Manual](docs/user-manual-en.html)

## Données

Les tâches sont stockées dans une base de données SQLite (`tasks.db`). Par défaut, le fichier est créé à côté de l'application ; utilisez **Paramètres → Base de données → Changer…** pour choisir un autre emplacement. Les paramètres sont enregistrés sous `settings.json` à côté de l'application (ou sous `%LOCALAPPDATA%\OD.Planner` si le dossier de l'application n'est pas accessible en écriture).

## Licence

OD.Planner est publié en tant que **donationware**. Voir les fichiers [License.txt](License.txt) et [License.FR.txt](License.FR.txt) pour plus de détails. Si vous trouvez l'application utile, un petit don est apprécié.

## Version

Version actuelle : **1.3.0**

## Auteur

**Olivier Dahan** — [GitHub](https://github.com/odahan)
