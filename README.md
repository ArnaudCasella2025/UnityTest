# Mini RPG : Les bandits de la colline

Petit RPG d'action à la 3e personne, construit avec des formes primitives :
capsules pour les personnages, cubes, sphères et cylindres pour le décor. Aucun modèle 3D ni prefab.

## Lancer le jeu

1. Ouvrir le dossier avec **Unity 6000.3.12f1** (Unity 6.3) via Unity Hub > *Add project from disk*.
   Si Unity propose de redémarrer pour activer le nouveau système de saisie (Input System), accepter.
2. À la première ouverture, la scène `Assets/Scenes/Main.unity` est créée automatiquement, décor compris
   (sinon : menu **Mini RPG > Créer ou ouvrir la scène principale**).
3. Appuyer sur **Play**.

## Modifier le décor

Menu **Mini RPG > Générer le décor dans la scène** : tout le décor (lumière, sol, village, forêt, chemin, camp)
est créé dans la scène ouverte, sous l'objet **Environnement**. Ce sont des GameObjects ordinaires, avec leurs
matériaux enregistrés dans `Assets/Materials` : déplacez, dupliquez, supprimez ou ajoutez ce que vous voulez,
puis enregistrez la scène (Ctrl/Cmd+S). Au lancement, le jeu utilise ce décor tel quel.

- **Points d'apparition** (`Environnement > Village > Points d'apparition`) : objets *Spawn - Joueur*, *Spawn - Aldric*,
  etc. Déplacez-les et tournez-les pour changer où apparaissent le joueur et les PNJ (repères visibles dans la vue Scene).
- **Camp des bandits** : déplacer l'objet *BanditCamp* déplace aussi les bandits et la zone de quête
  (le marqueur *Spawn - Camp des bandits* en fait partie).
- Relancer le menu propose de remplacer le décor par un décor neuf (annulable avec Ctrl/Cmd+Z).
- Les objets ajoutés à la main ont besoin d'un collider pour bloquer le joueur (les primitives Unity en ont un par défaut).

Les personnages, les dialogues et la quête restent créés en code au lancement (sous l'objet *Gameplay*).
Sans décor dans la scène, le jeu le génère lui-même au lancement. Pour désactiver complètement le démarrage
automatique, ajouter le symbole `MINIRPG_NO_AUTOSTART` dans *Project Settings > Player > Scripting Define Symbols*.

Le projet utilise le pipeline de rendu intégré (Built-in). Les commandes passent par `GameInput`, qui fonctionne avec le package
Input System comme avec l'ancien Input Manager, quel que soit le réglage *Active Input Handling*.

## Commandes

| Action | Touche |
|---|---|
| Se déplacer | ZQSD (AZERTY), WASD (QWERTY) ou flèches |
| Courir | Maj gauche |
| Sauter | Espace |
| Esquiver (invulnérable pendant l'esquive) | Ctrl gauche ou C |
| Attaquer (combo de 3 coups, le 3e frappe plus fort) | Clic gauche ou J |
| Boire une potion | H ou 1 |
| Parler / faire avancer le dialogue | E (ou Espace, Entrée, clic) |
| Choisir une réponse | 1, 2... ou clic |
| Tourner la caméra | Clic droit maintenu + souris |
| Zoom | Molette |
| Afficher/masquer l'aide | F1 |
| Se relever après une défaite | Entrée |

## La quête

1. **Parler à Aldric**, le chef du village de Clairval (le PNJ violet avec un « ! » au-dessus de la tête, près du puits).
2. **Aller au camp des bandits** au nord-est : suivre le chemin de terre et la colonne lumineuse jaune.
3. **Vaincre les bandits** : trois bandits et leur chef Brutus.
4. **Revenir voir Aldric** (« ? » au-dessus de sa tête) pour la récompense.

Un repère jaune sur l'écran indique toujours le prochain objectif et sa distance.
D'autres villageois (le garde Hugo, la marchande Berthe, Paul, la petite Lina) donnent des conseils
qui changent selon l'avancement de la quête. Berthe offre une potion quand la quête est acceptée.

## Organisation du code (`Assets/Scripts`)

| Dossier | Contenu |
|---|---|
| `Core/` | `RPGGame` (point d'entrée, réapparition), `GameInput` (clavier et souris, Input System ou ancien système), `WorldBuilder` (construction du décor, PNJ, ennemis, textes de la quête), `LevelRoot` et `SpawnMarker` (décor présent dans la scène, points d'apparition), `Prim` (primitives et matériaux), `CharacterFactory` (personnages en capsule), `Flicker`, `Spinner` |
| `Player/` | `PlayerController` (déplacements relatifs à la caméra, saut, esquive), `PlayerCombat` (épée, combo, potions), `PlayerInteractor` (E pour parler), `ThirdPersonCamera` (orbite, zoom, anti-collision), `CameraShake` |
| `Combat/` | `Health` (PV, dégâts, flash à l'impact), `EnemyAI` (patrouille, poursuite, attaque télégraphiée, retour au camp, mort) |
| `NPC/` | `NPC` (dialogue et marqueur de quête fournis par des fonctions, déambulation, se tourne vers le joueur) |
| `Dialogue/` | `Dialogue` (répliques + choix, syntaxe fluide), `DialogueManager` (boîte de dialogue, texte progressif, choix) |
| `Quest/` | `QuestManager` (états de la quête, apparition des bandits, compteur, récompense) |
| `UI/` | `HUD` (vie, or, potions, suivi de quête, indicateur d'objectif, noms des PNJ, barres de vie ennemies, dégâts flottants, écran de défaite) |

L'interface utilise IMGUI (`OnGUI`), ce qui évite toute dépendance à un package (TextMeshPro, uGUI).

Pour modifier la quête ou les dialogues, tout se trouve dans `WorldBuilder.cs` (`AldricDialogue`, `BuildNPCs`, `SpawnBandits`).
