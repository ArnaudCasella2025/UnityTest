# Vaisseau RPG : Ordre de mission

Petit RPG à la 3e personne à bord du vaisseau spatial *Aurore*, construit avec des formes primitives :
capsules pour l'équipage, cubes, sphères et cylindres pour le décor. Aucun modèle 3D ni prefab.

## Lancer le jeu

1. Ouvrir le dossier avec **Unity 6000.3.12f1** (Unity 6.3) via Unity Hub.
   Si Unity propose de redémarrer pour activer le nouveau système de saisie (Input System), accepter.
2. À la première ouverture, les scènes `Assets/Scenes/Vaisseau.unity` (décor compris) et `Assets/Scenes/Espace.unity`
   sont créées, ajoutées au Build, et le vaisseau est ouvert (sinon : menu **Vaisseau RPG > Ouvrir la scène du vaisseau**).
3. Appuyer sur **Play**.

## La quête

1. **Trouver le commandant** Vega, sur la passerelle au nord du vaisseau (« ! » au-dessus de sa tête).
2. Il donne l'ordre de mission et **déverrouille le hangar** (à bâbord, par la coursive transversale).
3. **Prendre un chasseur** : touche Action à côté. Le pilote disparaît dans le cockpit, la verrière se ferme,
   les réacteurs s'allument, le chasseur décolle et sort du hangar par le champ de force.
4. **Changement de scène** : dans l'espace, piloter le chasseur à travers le champ d'astéroïdes
   jusqu'à l'escadron qui attend au point de rendez-vous.

Un repère jaune indique toujours le prochain objectif et sa distance.

## À bord

| Salle | Équipage |
|---|---|
| Passerelle | Commandant Vega, Lt. Okoro (pilote), Enseigne Sato (communications) |
| Station de tir | Sergent Kova, Canonnier Mills ; stand avec cibles (dont deux mobiles) |
| Laboratoire | Dr Maris, Théo ; un cristal mystérieux dans sa cellule de confinement |
| Botanique | Ama, Pico le robot jardinier |
| Salle des machines | Rhea (ingénieure en chef), Bram ; le réacteur à fusion |
| Hangar | Chef Doyle (mécanicien), Nyx (ailière) ; trois chasseurs |
| Coursive | Enseigne Lou, un robot de nettoyage |

Chacun raconte son poste ; certains dialogues changent selon l'avancement de la quête.
Les portes s'ouvrent avec la touche Action et se referment seules ; leur voyant est rouge quand elles sont verrouillées.

## Commandes

À bord :

| Action | Touche |
|---|---|
| Se déplacer | ZQSD (AZERTY), WASD (QWERTY) ou flèches |
| Courir | Maj gauche |
| Sauter | Espace |
| Esquiver | Ctrl gauche ou C |
| Action : parler, ouvrir une porte, monter dans un chasseur | E |
| Tirer au pistolaser (vers l'avant de la caméra, légère aide à la visée) | Clic gauche ou J |
| Tourner la caméra | Clic droit maintenu + souris |
| Zoom | Molette |
| Afficher/masquer l'aide | F1 |

Dans l'espace :

| Action | Touche |
|---|---|
| Monter / descendre | Z ou W / S |
| Virer | Q ou A / D |
| Turbo | Maj gauche |
| Canons laser | Clic gauche ou J |
| Revenir à bord (après la mission) | Entrée |

## Modifier le décor

Le décor du vaisseau est dans la scène `Vaisseau`, sous l'objet **Vaisseau** : ce sont des GameObjects ordinaires,
avec leurs matériaux dans `Assets/Materials`. Déplacez, dupliquez, supprimez ou ajoutez ce que vous voulez,
puis enregistrez la scène. Au lancement, le jeu utilise ce décor tel quel.

- **Points d'apparition** (`Vaisseau > Points d'apparition`) : *Spawn - Pilote*, *Spawn - Commandant Vega*, etc.
  Déplacez-les et tournez-les pour changer où apparaissent le joueur et l'équipage.
- **Portes** : composant *SlidingDoor* (nom de la salle, verrouillage, déverrouillage par l'ordre de mission).
- **Chasseurs** : composant *Fighter* ; un chasseur décolle droit devant lui, orientez-le vers la sortie du hangar.
- Menu **Vaisseau RPG > Générer le décor dans la scène** : (re)crée le décor (annulable avec Ctrl/Cmd+Z).

L'équipage, les dialogues et la quête sont créés en code au lancement (objet *Gameplay*), dans `ShipBuilder.BuildCrew`.
La scène de l'espace est entièrement générée par `SpaceGame`.

## Organisation du code (`Assets/Scripts`)

| Dossier | Contenu |
|---|---|
| `Core/` | `GameBoot` (démarrage et changement de scène), `ShipGame`, `ShipBuilder` (vaisseau, équipage, dialogues), `GameInput` (clavier/souris, Input System ou ancien système), `Prim` (primitives, matériaux normaux et lumineux), `CharacterFactory`, `LevelRoot`, `SpawnMarker`, `Patrol`, `Flicker`, `Spinner` |
| `Interaction/` | `Interactable` (base de tout ce qui réagit à la touche Action), `SlidingDoor`, `Fighter` (+ `FighterFactory`, séquence de décollage) |
| `Player/` | `PlayerController`, `PlayerShooter` (pistolaser), `PlayerInteractor`, `ThirdPersonCamera`, `CameraShake` |
| `Combat/` | `Health`, `LaserBolt` (+ `Spark`), `ShootingTarget` |
| `NPC/`, `Dialogue/`, `Quest/` | `NPC`, `Dialogue`, `DialogueManager`, `QuestManager` |
| `Space/` | `SpaceGame` (scène de l'espace), `FighterFlight` (pilotage), `Asteroid` (+ `Debris`) |
| `UI/` | `HUD` (objectif, invite d'action, noms des PNJ et des salles, messages, fondu entre les scènes) |

Le projet utilise le pipeline de rendu intégré (Built-in) et une interface IMGUI (`OnGUI`), sans package supplémentaire
à part l'Input System.
