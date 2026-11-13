# BrainJump v1.0 — 3D Maths Platformer

A complete 3D maths platformer for Android (portrait 9:16), built in
**Unity 6.3 LTS (6000.3) with URP 17.3**. A cute hero jumps between three
floating answer platforms to solve maths questions. 3 worlds × 10 levels,
stars, coins, combos, lives and power-ups — a real game, not a prototype.

## How to open it

**Recommended — Unity Hub template path:**

1. Install **Unity 6.3 LTS (6000.3.x)** from Unity Hub.
2. In Hub, create a new project from the **3D (URP)** template (any name).
   This gives you a working URP setup with the right packages.
3. Copy the `Assets/BrainJump` folder from this project into the new
   project's `Assets/` folder. (Copy `Packages/manifest.json` too if you
   want the exact package set; the template's own manifest also works.)
4. Open the project. Unity imports everything.
5. If TextMeshPro asks, import **TMP Essentials** once:
   `Window > TextMeshPro > Import TMP Essential Resources`.
6. Open `Assets/BrainJump/Scenes/MainScene.unity` — or build it fresh:
   `BrainJump > Build Prototype Scene` (menu bar). The builder creates
   the whole scene: islands, platforms, hero, camera, lighting, UI and
   managers, wired up and ready to play.
7. Press **Play**.

**Direct path:** open this folder itself as a Unity project
(`ProjectVersion.txt` pins 6000.3.0f1, `Packages/manifest.json` lists
URP 17.3.0 + TextMeshPro 3.0.9). If Package Manager complains about a
version, let it resolve to the matching 6000.3 version.

**Player Settings to verify** (Edit > Project Settings > Player):
- Product Name: `BrainJump`, Package: `com.marcus.brainjump`
- Default Orientation: **Portrait**
- Android Min API: 24, Target API: 35, Scripting Backend: IL2CPP
- **Active Input Handler: Input Manager (Old)** — the game uses the old
  `UnityEngine.Input` API and no Input System package is installed.

**Game view:** set the Game view aspect to **9:16 portrait** — the HUD is
built for portrait and sits off-screen in a wide landscape view.

## How it plays

- **Main Menu** → Play (continues at your highest unlocked level) or
  Levels (pick any unlocked level and see its stars).
- Move with the **virtual joystick** (bottom-left) or **WASD/arrows** in
  the Editor. **Jump** with the big button (bottom-right) or **Space**.
- Jump onto the platform showing the **correct answer**. Only a real
  landing counts — brushing past a platform does nothing.
- Correct → celebration, confetti, combo grows, next question. Wrong, or
  the **timer** runs out → you lose a heart (3 hearts per level).
  Lose all 3 hearts → Game Over. Fall off the island → same.
- **Coins** float on the jump path — jump through them to collect.
  **Power-ups** sometimes appear on a platform: Extra Time (+5s),
  Double Jump (one mid-air jump), Shield (one wrong answer free).
- **10 correct answers** = level complete: stars (3 = no mistakes,
  2 = one mistake, 1 = finished), coins banked, next level unlocked.
- **Combo multiplier:** consecutive correct answers raise the points
  per answer (x1 → x5). A wrong answer resets the combo.

## Worlds & difficulty

| World | Name | Maths | Extra |
|---|---|---|---|
| 1 | Addition Meadows | Addition | — |
| 2 | Subtraction Hills | Subtraction, missing-number from level 5 | Platforms drift sideways |
| 3 | Multiplication Peaks | Multiplication + Division, missing-number from level 5 | Faster platform drift |

Number ranges grow with each level; the per-question timer shrinks
slightly at higher levels (10s down to a 6s floor). Missing-number
questions look like `7 + ? = 12` — the answer is the missing number.

## Screens

Main Menu, Level Select (3 worlds × 10 levels, stars + locks), Gameplay
HUD (score, coins, lives, combo, timer bar, question progress, pause),
Pause (Resume / Restart / Settings / Menu), Level Complete (stars, score,
coins, Next Level), Game Over (score, best, Retry Level), Settings
(music + SFX toggles, how to play, reset progress). Progress, stars,
coins and best score are saved on the device (PlayerPrefs).

## Project layout

```
Assets/BrainJump/
  Scenes/          MainScene.unity (built by the SceneBuilder)
  Scripts/
    Player/        PlayerController.cs, CharacterAnimator.cs, PlayerTrail.cs
    Camera/        CameraFollow.cs
    Gameplay/      GameManager.cs, GameProgression.cs, GameTimer.cs,
                   SaveManager.cs, ScoreManager.cs, LivesManager.cs,
                   CoinManager.cs, PowerUpManager.cs
    Questions/     QuestionGenerator.cs, QuestionManager.cs
    Platforms/     AnswerPlatform.cs, Coin.cs, PowerUpPickup.cs
    Effects/       EffectsManager.cs
    UI/            UIManager.cs, LevelSelectUI.cs, MobileJoystick.cs, JumpButton.cs
    Audio/         AudioManager.cs
  Editor/          SceneBuilder.cs, SceneBuilderEnv2.cs, SceneBuilderUI.cs
  Prefabs/         Player/, Platforms/
  Materials/       Palette materials (created by the SceneBuilder)
  Models/ Textures/ Audio/ Settings/   (reserved for future assets)
Packages/manifest.json   (URP 17.3.0, TextMeshPro 3.0.9, UGUI 2.0.0)
ProjectSettings/         (6000.3.0f1, portrait, com.marcus.brainjump)
```

## Palette (used consistently)

| Name | Hex |
|---|---|
| Midnight Blue | `#172B42` |
| Sky Blue | `#75B9D4` |
| Leaf Green | `#4C9670` |
| Warm Gold | `#F2B85B` |
| Coral | `#E98270` |
| Warm Off-white | `#F7F4EC` |

## Graphics & effects

Everything is procedural (primitives + materials, no external assets):
layered floating islands with rock undersides, hanging vines and
waterfalls, trees, flowers, glowing crystals, drifting puffy clouds,
distant islands, sun and fog. The hero is a detailed primitive-built
character (cheeks, hair tuft, shoes) with squash & stretch, blinking,
running bob, jump trail, celebrate and dizzy animations. Particles for
landing dust, confetti, explosions, coin sparkles and star bursts;
camera shake + FOV kick on jumps; floating `+100` score popups.

## Sounds & music

All sounds are **generated in code** by `AudioManager` (no audio files):
button click, jump, landing, correct arpeggio, wrong buzz, explosion,
coin chime, power-up fanfare, game-over theme, level-complete fanfare,
plus a cheerful procedural background music loop. Music and SFX can be
toggled separately in Settings. Swap in real clips later by replacing
the `MakeTone` calls.

## Test checklist v2 (run in Play mode, Game view 9:16)

1. Menu shows; Play starts the highest unlocked level, HUD appears
   (score, coins, lives, timer bar, progress, pause button).
2. Levels opens the level select: 3 worlds, 10 levels each, level 1
   unlocked, the rest locked; stars show after completing a level.
3. A question appears; the 3 platforms show 3 different answers,
   one of them correct; the labels face up and are readable.
4. Joystick moves the hero; it rotates toward movement; jump works
   once per press; no double-jump in mid-air (without the power-up).
5. Landing on the correct platform → celebration, confetti, `+100`
   popup, combo grows, next question.
6. Landing on a wrong platform → dizzy animation, one heart lost,
   fail panel; Retry gives a fresh question.
7. Lose all 3 hearts → Game Over panel with score and best; Retry
   Level restarts the level with 3 hearts.
8. Let the timer run out → same as a wrong answer (heart lost).
9. Jump off the island → same fail flow.
10. Jump through coins → coin count and score rise, sparkle plays.
11. Land on a power-up → it applies (Extra Time adds 5s, Double Jump
    allows one mid-air jump, Shield saves one wrong answer).
12. Answer 10 questions correctly → level-complete screen with stars,
    score and coins; Next Level unlocks and starts the next level.
13. World 2+: platforms drift sideways slowly; standing on one
    carries the hero along.
14. Pause → Resume / Restart / Settings / Menu all work; Settings
    music + SFX toggles work; music keeps playing.
15. Quit to menu and reopen: unlocked levels, stars, coins and best
    score are still there.
16. No console errors. No platform is unreachable.
17. Build Settings > Android: the project builds an APK.

## Known limits (v1.0)

- Sounds and music are procedural placeholder tones, not recorded audio.
- No shop, characters or skins yet — coins are collected and banked,
  but there is nothing to spend them on yet.
- The scene is built by an Editor script, not hand-placed — tweak
  positions in the SceneBuilder files and re-run
  `BrainJump > Build Prototype Scene` (the builder creates a NEW scene
  each run, so scene-object edits are discarded).
- Unity Editor cannot run on the machine that generated this code, so
  Play-mode testing was done by the developer in their own Editor;
  the C# was compile-checked offline (Mono mcs + Unity API stubs).
