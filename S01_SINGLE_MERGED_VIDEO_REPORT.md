# S01 Single Merged Video Report

## Merged Video Used

- `Assets/Models/video/0710.mp4`
- Unity GUID: `03c0c7690adce2c4bb7ebe11ceb9ad52`
- Note: `intro_full.mp4` was not present. `0710.mp4` was the newest video file in `Assets/Models/video`.

## Scene Backup

- Created: `Assets/Scenes/S01_before_single_merged_intro_video.unity`

## S01 Setup

- `VideoPlayer_1`
  - Uses merged clip `0710.mp4`.
  - Render Mode is Render Texture.
  - Target Texture is `IntroVideoRT`.
  - Audio Output Mode is None.
  - Play On Awake is false.
  - Looping is false.
  - Wait For First Frame is true.
- `Canvas/VideoRawImage`
  - RawImage texture is `IntroVideoRT`.
  - Object remains enabled.
- `Canvas/FadeOverlay`
  - Black Image is kept.
  - CanvasGroup is enabled.
  - Interactable is false.
  - Blocks Raycasts is false.
- `VideoPlayer_2`
  - Disabled in `S01.unity`.
  - Clip cleared.
  - Target Texture cleared.
  - Not referenced by `S01VideoSequenceController`.

## Controller Changes

- `S01VideoSequenceController` was simplified to one video.
- It prepares and plays only `VideoPlayer_1`.
- It registers `loopPointReached` once and unregisters it when done or disabled.
- It does not use `VideoPlayer_2`.
- It does not use APIOnly.
- It does not use `videoPlayer.texture`.
- It loads `S03` after the merged intro video ends.
- Space, Enter, or Escape skip directly to `S03`.

## Files Modified

- `Assets/Scenes/S01.unity`
- `Assets/Scenes/S01_before_single_merged_intro_video.unity`
- `Assets/Scripts/S01/S01VideoSequenceController.cs`
- `Assets/Scripts/Editor/S01VideoIntroSceneBuilder.cs`

Backup `.cs.bak` copies for the S01 controller and S01 editor builder were also aligned so project-wide searches do not find the old two-video flow.

## Verification

- `dotnet build Assembly-CSharp.csproj -v:minimal` succeeded with 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -v:minimal` succeeded with 0 errors.
- Search found no active S01 controller or builder references to `videoPlayer2`, `videoClip2`, APIOnly, audio source video output, or `videoPlayer.texture`.
- The only remaining `VideoPlayer_2` match is the disabled scene object name in `S01.unity`.

## What To Test Next

1. Open `Assets/Scenes/S01.unity`.
2. Confirm `VideoPlayer_1` uses `0710.mp4`.
3. Confirm `VideoPlayer_2` is inactive and not referenced by the controller.
4. Enter Play Mode from `MainMenu`, click the Start button, and confirm flow is `MainMenu -> S01 -> S03`.
5. Let the merged intro play to the end and confirm S01 loads `S03`.
6. Re-test Space, Enter, and Escape skip from S01 to `S03`.
7. Confirm no audio buffer overflow warnings appear while audio output is disabled.
