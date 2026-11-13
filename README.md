# BrainJump — 3D Maths Platformer Prototype

A fully playable 3D maths platformer for Android (portrait 9:16), built in
**Unity 6.3 LTS (6000.3) with URP 17.3**. A cute character jumps between
three floating answer platforms to solve maths questions. 10 questions per
level. Built to the master spec in `MASTER_PROMPT.md` (not included in the
project — see the notes below).

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
   the whole scene: island, platforms, player, camera, lighting, UI and
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

## How it plays

- **Main Menu** → Play. A maths question appears at the top
  (addition, subtraction, multiplication, division).
- Move with the **virtual joystick** (bottom-left) or **WASD/arrows** in
  the Editor. **Jump** with the big button (bottom-right) or **Space**.
- Jump onto the platform showing the **correct answer**. Only a real
  landing counts — brushing past a platform does nothing.
- Correct → celebration + next question. Wrong, or the **10-second
  timer** runs out → cartoon explosion + retry screen (retry restarts
  the question). Falling off the island also fails the question.
- **10 correct answers** = level complete screen with your score.
- Difficulty rises as you answer correctly (1 → 5). Questions never
  repeat back-to-back. Answer positions are shuffled every question.

## Project layout

```
Assets/BrainJump/
  Scenes/          MainScene.unity (built by the SceneBuilder)
  Scripts/
    Player/        PlayerController.cs, CharacterAnimator.cs
    Camera/        CameraFollow.cs
    Gameplay/      GameManager.cs, GameProgression.cs, GameTimer.cs
    Questions/     QuestionGenerator.cs, QuestionManager.cs
    Platforms/     AnswerPlatform.cs
    UI/            UIManager.cs, MobileJoystick.cs, JumpButton.cs
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

## Sounds

All sounds are **generated in code** by `AudioManager` (no audio files):
button click, jump, landing, correct arpeggio, wrong buzz, explosion,
level-complete fanfare. Swap in real clips later by replacing the
`MakeTone` calls.

## Character

The player is a temporary primitive-built character (capsule body,
oversized head, eyes, arms, legs) with procedural animation: idle
breathing + blinking, running bob, jump stretch, landing squash,
celebration, explosion. It is deliberately replaceable — swap the
`Visual` child for a real model later; the controller and animator
only need the named parts (`Body`, `Head`, `EyeL`, `EyeR`, `ArmL`,
`ArmR`, `LegL`, `LegR`).

## Test checklist (run in Play mode)

1. Menu shows; Play starts the game, HUD appears.
2. A question appears; the 3 platforms show 3 different answers,
   one of them correct.
3. Joystick moves the character; it rotates toward movement.
4. Jump works once per press; no double-jump in mid-air.
5. Landing on the correct platform → celebration, next question.
6. Landing on a wrong platform → explosion + retry screen; Retry
   gives a fresh question.
7. Let the 10s timer run out → same fail flow as a wrong answer.
8. Jump off the island → fail flow.
9. Answer 10 questions correctly → level-complete screen with score.
10. Pause button → Resume / Restart / Main Menu all work.
11. No console errors. No platform is unreachable.
12. Build Settings > Android: the project builds an APK.

## Known prototype limits

- Single level (10 questions), one island, no saved progress.
- No Settings screen yet — the Settings button plays a click sound.
- Sounds are procedural placeholder tones, not recorded audio.
- The scene is built by an Editor script, not hand-placed — tweak
  positions in `SceneBuilder.cs` and re-run
  `BrainJump > Build Prototype Scene`.
- Unity Editor cannot run on the machine that generated this code, so
  Play-mode testing was done by the developer in their own Editor;
  the C# was compile-checked offline (Mono mcs + Unity API stubs).
