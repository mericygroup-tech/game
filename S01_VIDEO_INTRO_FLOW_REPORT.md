# S01 Video Intro Flow Report

## 1. MainMenu Checked

Checked:

`Assets/Scenes/MainMenu.unity`

The MainMenu uses `MainMenuController` on:

`MainMenu_Controller`

The Start button is:

`MainMenu_StartButton`

## 2. Start Button Found

Found: Yes.

The button has no persistent scene OnClick calls. It is wired at runtime by:

`Assets/Scripts/UI/MainMenuController.cs`

`MainMenuController.BindButtons()` connects the Start button to `StartGame()`.

## 3. Start Button Flow Updated

Updated: Yes.

The serialized `MainMenuController.startSceneName` value in `MainMenu.unity` was changed from:

`S01_CityPrototype`

to:

`S01`

The existing `StartGame()` and `SceneManager.LoadScene(startSceneName)` flow is preserved.

## 4. S01 Scene Created

Created:

`Assets/Scenes/S01.unity`

Scene objects:

- `Main Camera`
- `EventSystem`
- `Canvas`
- `VideoRawImage`
- `FadeOverlay`
- `VideoPlayer_1`
- `VideoPlayer_2`
- `S01VideoSequenceController`

The scene is a video-only intro scene.

## 5. Video Assets Found

Found:

| Video | Path | Size | GUID |
| --- | --- | --- | --- |
| Video 1 | `Assets/Models/video/1.mp4` | 91,047,288 bytes | `42d6192fc57bcd84b8b6dc3b3375d9a5` |
| Video 2 | `Assets/Models/video/2.mp4` | 3,200,080 bytes | `e6b5d202c80dc2346b3c6f6455d4e2e1` |

The video files were not renamed, moved, deleted, compressed, or converted.

## 6. Video Playback Setup

Created:

`Assets/Scripts/S01/S01VideoSequenceController.cs`

The controller has serialized fields for:

- `VideoPlayer videoPlayer1`
- `VideoPlayer videoPlayer2`
- `VideoClip videoClip1`
- `VideoClip videoClip2`
- `RawImage videoDisplay`
- `CanvasGroup fadeOverlay`
- `AspectRatioFitter aspectFitter`
- `nextSceneName = "S03"`
- `fadeDuration = 0.5`
- `allowSkip = true`

Because Unity batch scene generation was blocked before execution, the scene is authored defensively: `S01VideoSequenceController` creates or completes missing runtime components on scene start. It finds the named `VideoPlayer_1` and `VideoPlayer_2` objects, adds `VideoPlayer` and `AudioSource` components if needed, assigns the serialized clips, and displays video through `VideoRawImage`.

## 7. Transition / Fade Setup

Implemented in `S01VideoSequenceController`:

- Fade from black into video 1
- Prepare video 2 while video 1 is playing
- Fade to black between video 1 and video 2
- Fade into video 2
- Fade to black after video 2
- Load S03

Skip controls:

- `Space` or `Enter`: skip to the next video
- `Escape`: skip directly to S03

## 8. Scene Loading Flow

Final configured flow:

`MainMenu -> S01 -> video 1 -> video 2 -> S03`

S01 loads S03 through:

`SceneManager.LoadScene("S03")`

## 9. Build Settings

Updated:

`ProjectSettings/EditorBuildSettings.asset`

First enabled scenes are now:

0. `Assets/Scenes/MainMenu.unity`
1. `Assets/Scenes/S01.unity`
2. `Assets/Scenes/S03.unity`

Existing older scenes were preserved after those entries:

- `Assets/Scenes/S01_CityPrototype.unity`
- `Assets/Scenes/S02_UndergroundCave.unity`

## 10. Files Modified

Created:

- `Assets/Scenes/S01.unity`
- `Assets/Scenes/S01.unity.meta`
- `Assets/Scripts/S01/S01VideoSequenceController.cs`
- `Assets/Scripts/S01/S01VideoSequenceController.cs.meta`
- `Assets/Scripts/S01.meta`
- `Assets/Scripts/Editor/S01VideoIntroSceneBuilder.cs`
- `Assets/Scripts/Editor/S01VideoIntroSceneBuilder.cs.meta`

Modified:

- `Assets/Scenes/MainMenu.unity`
- `ProjectSettings/EditorBuildSettings.asset`

Not modified:

- `Assets/Scenes/S03.unity`
- Player systems
- Sword setup
- Blessing systems
- Combat systems
- Wave/minion systems
- Map systems
- Video files

## 11. Backup Files Created

Created:

- `Assets/Scenes/MainMenu_before_s01_intro_flow.unity`
- `Assets/Scripts/S01/S01VideoSequenceController.cs.bak`
- `Assets/Scripts/Editor/S01VideoIntroSceneBuilder.cs.bak`

## 12. Warnings

Unity batch mode was attempted to generate the scene through Unity APIs, but Unity exited before executing the builder due to a local Unity startup/licensing/cache issue.

Log file:

`S01VideoIntroBuild.log`

Relevant warnings/errors from the log:

- `CreateDirectory 'C:/Users/ndpho/AppData/Local/Unity/Caches' failed`
- Licensing client channel connection was refused
- `Assertion failed on expression: 'SUCCEEDED(hr)'`
- Unity terminated with return code `1`

No MP4 encoding warning was available from Unity because the editor did not reach import/playback validation.

If either video fails to play in Unity, convert the affected file to Unity-friendly MP4:

- H.264 video
- AAC audio
- `yuv420p` pixel format
- 1920x1080 or lower if needed

Do not convert unless needed.

## 13. Manual Test Checklist

1. Open `Assets/Scenes/MainMenu.unity`.
2. Press Play.
3. Click `BẮT ĐẦU`.
4. Confirm `S01` loads.
5. Confirm video 1 plays from `Assets/Models/video/1.mp4`.
6. Confirm video 2 plays automatically after video 1.
7. Confirm S03 loads after video 2 finishes.
8. Press `Space` or `Enter` during S01 and confirm it skips to the next video.
9. Press `Escape` during S01 and confirm it skips directly to S03.
10. Confirm there are no Console errors.
11. Confirm S03 gameplay still works.

## 14. Final Result

The requested intro flow is configured:

`MainMenu -> S01 -> 1.mp4 -> 2.mp4 -> S03`

S03 gameplay logic was not modified.

The video files were not modified.
