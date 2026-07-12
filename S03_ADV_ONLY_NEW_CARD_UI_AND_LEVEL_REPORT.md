# S03 An Dương Vương Only New Card UI And Level Report

## 1. Scene Checked

Scene checked:

`Assets/Scenes/S03.unity`

The active blessing UI is under the S03 Canvas and uses:

- `S03_BlessingManager`
- `S03_BlessingChoiceRoot`
- `S03_BlessingCard_1`
- `S03_BlessingCard_2`
- `S03_BlessingCard_3`
- `S03_BlessingRerollButton`
- `S03_BlessingSkipButton`

## 2. Blessing System Files Found

Runtime and UI files:

- `Assets/Scripts/Blessing/BlessingManager.cs`
- `Assets/Scripts/Blessing/BlessingChoiceUI.cs`
- `Assets/Scripts/Blessing/BlessingDefinition.cs`
- `Assets/Scripts/Blessing/BlessingEnums.cs`
- `Assets/Scripts/Blessing/BlessingRuntimeController.cs`
- `Assets/Scripts/Scene/S03ArenaDirector.cs`

S03 blessing data assets:

- `Assets/Blessings/S03/ADV_CanhGioi.asset`
- `Assets/Blessings/S03/ADV_ThanhGiapAuLac.asset`
- `Assets/Blessings/S03/ADV_NoThan.asset`

## 3. Original S03 Blessing Pool

At the start of this task, `S03_BlessingManager.allBlessings` was already restricted to 3 An Dương Vương blessings:

| Display Name | Owner | Rarity | Max Level | Asset Path | Effect |
| --- | --- | --- | --- | --- | --- |
| Cảnh Giới | An Dương Vương | Common | 3 | `Assets/Blessings/S03/ADV_CanhGioi.asset` | Awareness |
| Thành Giáp Âu Lạc | An Dương Vương | Common | 3 | `Assets/Blessings/S03/ADV_ThanhGiapAuLac.asset` | Armor |
| Nỏ Thần | An Dương Vương | Rare | 3 | `Assets/Blessings/S03/ADV_NoThan.asset` | DivineCrossbow |

## 4. New S03 Allowed Blessing Pool

S03 remains restricted to only:

- Cảnh Giới
- Thành Giáp Âu Lạc
- Nỏ Thần

Verified `S03.unity` currently contains exactly these 3 blessing GUIDs in `allBlessings`:

- `43d0f7fa41c4c4c4081052484ad829d1`
- `cae748df425c1154ba19fee029ef2111`
- `d8ca84020990a354097f1c7138db717d`

## 5. Removed From S03 Runtime Pool

No additional blessings had to be removed during this pass because non-An Dương Vương blessings were already absent from the current S03 runtime pool.

Verified still excluded from S03:

- Khởi Nghĩa Mê Linh
- Hiệu Triệu
- Cờ Khởi Nghĩa
- Kỳ Tượng
- Truy Kích
- Xung Phong
- Bóng Chiến Trường
- Hành Quân Thần Tốc
- Thiên Lôi Tây Sơn
- Any Trưng Trắc blessing
- Any Trưng Nhị blessing
- Any Quang Trung blessing

No blessing assets were deleted.

## 6. New Card Image Assets Found

Found in the exact required folder:

`Assets/Models/skill/An Duong Vuong`

| File | Size | Dimensions | Import Change |
| --- | --- | --- | --- |
| `Cảnh giới ht.jpg` | 132,920 bytes | 749 x 1024 | Texture Type set to Sprite |
| `nỏ thần ht.jpg` | 102,449 bytes | 737 x 1024 | Texture Type set to Sprite |
| `Thánh giáp ht.jpg` | 150,460 bytes | 731 x 1024 | Texture Type set to Sprite |

Note: the actual armor card file found is named `Thánh giáp ht.jpg`. It was mapped to `Thành Giáp Âu Lạc`.

## 7. Blessing To Card Image Mapping

| Blessing | Card Image |
| --- | --- |
| Cảnh Giới | `Assets/Models/skill/An Duong Vuong/Cảnh giới ht.jpg` |
| Nỏ Thần | `Assets/Models/skill/An Duong Vuong/nỏ thần ht.jpg` |
| Thành Giáp Âu Lạc | `Assets/Models/skill/An Duong Vuong/Thánh giáp ht.jpg` |

These mappings were assigned through the existing `BlessingDefinition.icon` field.

## 8. UI Objects Modified

Modified scene UI components:

- `S03_BlessingCard_1`
- `S03_BlessingCard_2`
- `S03_BlessingCard_3`

Each existing `BlessingChoiceUI` component now has:

`useIconAsFullCardArtwork: 1`

The card positions, Button components, click handling, reroll button, skip button, and selection flow were preserved.

## 9. Old Text / Placeholder Handling

Old overlapping card children are not deleted.

When full-card mode is active and a blessing has an assigned sprite, `BlessingChoiceUI` hides the old direct child UI objects except:

- the card image `Icon`
- the dynamic `Stack` / Bậc TextMeshPro object

This hides old title, rarity, description, faction text, icon placeholder/backplate, decorative lines, and old card body children during binding.

## 10. Dynamic Bậc Text Handling

The existing TextMeshPro `stackText` is reused as the dynamic Bậc overlay.

It displays:

- `Bậc 1/3`
- `Bậc 2/3`
- `Bậc 3/3`

The value is computed from the real blessing stack passed by `BlessingManager`:

`nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack)`

This is not hardcoded cosmetic text. It reflects the real currently owned blessing level and updates whenever the card is rebound, including when the selection opens and when reroll is pressed.

## 11. Upgrade Logic Check

Existing upgrade logic is preserved.

`BlessingManager.SelectBlessing()` increments the real stack:

`newStack = Mathf.Min(GetStack(blessing.Id) + 1, blessing.MaxStack)`

Then it stores the stack and applies the blessing:

- `ownedStacks[blessing.Id] = newStack`
- `blessing.SetRuntimeStack(newStack)`
- `playerEffects.ApplyBlessing(blessing, newStack)`

Max level remains 3 for all three S03 blessings.

## 12. Actual Effect Scaling Check

The real gameplay effects already scale by stack level.

Cảnh Giới:

- Uses `BlessingEffectType.Awareness`
- Spawn warning delay scales in `GetAwarenessSpawnDelay()`
- Enemy awareness range bonus scales with `GetEnemyAwarenessRangeBonus()`

Thành Giáp Âu Lạc:

- Uses `BlessingEffectType.Armor`
- Damage reduction scales in `RecalculatePassiveStats()`
- Current formula includes `Armor stack * 0.075f`

Nỏ Thần:

- Uses `BlessingEffectType.DivineCrossbow`
- The extra arrow effect activates every 5th attack when the stack is greater than 0
- Arrow damage scales in `AfterPlayerAttack()` using `0.42f + crossbowStack * 0.08f`

No combat code change was needed.

## 13. Reroll Logic Check

Reroll still uses the same current S03 pool through `RollDistinctChoices(3)`.

Because the S03 pool contains only the three allowed An Dương Vương blessings, reroll can only show:

- Cảnh Giới
- Thành Giáp Âu Lạc
- Nỏ Thần

## 14. Skip Logic Check

Skip logic was not changed.

`SkipBlessing()` still closes the selection, disables card interaction, shows skip feedback, and continues wave progression through the existing callback.

## 15. Edge Case Handling

The existing system safely handles the 3-blessing pool:

- No blessings selected: up to 3 valid cards are shown.
- Bậc 1/3 or 2/3: the blessing can appear again as an upgrade.
- Bậc 3/3: the blessing is excluded by `GetStack(blessing.Id) < blessing.MaxStack`.
- Fewer than 3 valid blessings: empty card slots are hidden by `BlessingChoiceUI.Bind(null)`.
- All 3 blessings maxed: `PresentChoices()` receives zero choices and calls `FinishSelection()` without an infinite loop.
- Reroll with zero valid choices calls `SkipBlessing()` safely.

## 16. Files Modified

Modified:

- `Assets/Scenes/S03.unity`
- `Assets/Scripts/Blessing/BlessingChoiceUI.cs`
- `Assets/Blessings/S03/ADV_CanhGioi.asset`
- `Assets/Blessings/S03/ADV_NoThan.asset`
- `Assets/Blessings/S03/ADV_ThanhGiapAuLac.asset`
- `Assets/Models/skill/An Duong Vuong/Cảnh giới ht.jpg.meta`
- `Assets/Models/skill/An Duong Vuong/nỏ thần ht.jpg.meta`
- `Assets/Models/skill/An Duong Vuong/Thánh giáp ht.jpg.meta`

No prefab was modified.

No blessing asset was deleted.

No Trưng Trắc, Trưng Nhị, or Quang Trung asset was deleted or modified.

## 17. Backup Files Created

Created before changes:

- `Assets/Scenes/S03_before_adv_only_new_card_ui.unity`
- `Assets/Scripts/Blessing/BlessingChoiceUI.cs.bak`

No prefab backup was needed because no prefab was edited.

## 18. Manual Steps If Needed

No manual assignment should be required.

If Unity does not immediately show the new images, right-click these three files in the Project window and choose Reimport:

- `Assets/Models/skill/An Duong Vuong/Cảnh giới ht.jpg`
- `Assets/Models/skill/An Duong Vuong/nỏ thần ht.jpg`
- `Assets/Models/skill/An Duong Vuong/Thánh giáp ht.jpg`

Then open `Assets/Scenes/S03.unity` and verify the three `S03_BlessingCard_*` objects have `Use Icon As Full Card Artwork` enabled.

## 19. Test Checklist

1. Open `Assets/Scenes/S03.unity`.
2. Press Play.
3. Reach the blessing selection screen.
4. Confirm only An Dương Vương cards appear.
5. Confirm the visible card art uses the new images from `Assets/Models/skill/An Duong Vuong`.
6. Confirm old title, rarity, description, faction, and placeholder UI do not overlap the art.
7. Confirm each card still shows dynamic `Bậc 1/3`, `Bậc 2/3`, or `Bậc 3/3`.
8. Click each card and confirm the blessing is selected.
9. Confirm the selected blessing effect applies.
10. Select the same blessing in later choices and confirm the Bậc text increases.
11. Confirm the real effect scales with the blessing level.
12. Press reroll and confirm only the three An Dương Vương cards appear.
13. Press skip and confirm wave progression continues.
14. Confirm no Trưng Trắc, Trưng Nhị, or Quang Trung card appears.
15. Confirm no missing reference errors appear in the Console.
16. Confirm no infinite loop occurs after maxing all three blessings.

## 20. Final Result

S03 remains restricted to the three An Dương Vương blessings:

- Cảnh Giới
- Thành Giáp Âu Lạc
- Nỏ Thần

The three S03 blessing cards now use the new designed full-card image assets from:

`Assets/Models/skill/An Duong Vuong`

The old overlapping card text and placeholder visuals are hidden at bind time, while the real dynamic Bậc level text remains visible and tied to the existing blessing upgrade stack.
