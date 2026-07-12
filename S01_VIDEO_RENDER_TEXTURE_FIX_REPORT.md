# S01 Video Render Texture Fix Report

## Summary

- APIOnly removed: Yes. `S01VideoSequenceController` no longer sets `VideoRenderMode.APIOnly`, and the stale editor builder path was updated too.
- RenderTexture mode enforced: Yes. Both S01 video players are configured for `VideoRenderMode.RenderTexture` and assigned `Assets/RenderTextures/IntroVideoRT.renderTexture`.
- Audio disabled: Yes. The controller now sets `VideoAudioOutputMode.None` and disables controlled audio tracks before playback. The S01 scene also serializes both VideoPlayers with audio output mode set to None.

## Modified Scripts

- `Assets/Scripts/S01/S01VideoSequenceController.cs`
- `Assets/Scripts/Editor/S01VideoIntroSceneBuilder.cs`

Ignored `.cs.bak` backup copies for those two files were also cleaned so searches do not find the old APIOnly path.

## Scene Objects Checked

- `Canvas/VideoRawImage`
  - RawImage texture is `IntroVideoRT`.
- `VideoPlayer_1`
  - Clip is `1_480_cfr_unity`.
  - Render Mode is RenderTexture.
  - Target Texture is `IntroVideoRT`.
  - Audio Output Mode is None.
  - Play On Awake is false.
  - Looping is false.
  - Wait For First Frame is true.
- `VideoPlayer_2`
  - Clip is `2_480_cfr_unity`.
  - Render Mode is RenderTexture.
  - Target Texture is `IntroVideoRT`.
  - Audio Output Mode is None.
  - Play On Awake is false.
  - Looping is false.
  - Wait For First Frame is true.
- `S01VideoSequenceController`
  - References both VideoPlayers, both clips, the RawImage, the fade overlay, and `IntroVideoRT`.

## What To Test Next

1. Open `Assets/Scenes/S01.unity`.
2. Enter Play Mode.
3. Select `VideoPlayer_1` and `VideoPlayer_2` during playback and confirm neither switches to API Only.
4. Confirm `Canvas/VideoRawImage` continues to show `IntroVideoRT`.
5. Confirm video 1 plays through, fades, then video 2 plays through.
6. Confirm no audio buffer overflow warnings appear.
7. Confirm S01 loads `S03` after video 2 ends.

## Verification

- Deep search found no remaining `VideoRenderMode.APIOnly`, `VideoAudioOutputMode.AudioSource`, or `player.texture` references in `Assets/Scripts` or `Assets/Scenes/S01.unity`.
- `dotnet build Assembly-CSharp.csproj -v:minimal` succeeded with 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -v:minimal` succeeded with 0 errors.
