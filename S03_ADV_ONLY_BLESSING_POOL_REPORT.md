# S03 An Dương Vương Only Blessing Pool Report

## 1. Scene Checked

Scene checked:

`Assets/Scenes/S03.unity`

The S03 blessing selection runtime is driven by the scene object:

`S03_BlessingManager`

The manager has a serialized `allBlessings` list. That list is the active S03 runtime card pool used by `BlessingManager.PresentChoices()`.

## 2. Blessing System Files Found

Runtime blessing selection:

- `Assets/Scripts/Blessing/BlessingManager.cs`
- `Assets/Scripts/Blessing/BlessingChoiceUI.cs`
- `Assets/Scripts/Blessing/BlessingDefinition.cs`
- `Assets/Scripts/Blessing/BlessingEnums.cs`
- `Assets/Scripts/Blessing/BlessingRuntimeController.cs`
- `Assets/Scripts/Scene/S03ArenaDirector.cs`

Blessing assets:

- `Assets/Blessings/S03/*.asset`

Editor scene builder inspected, not modified:

- `Assets/Scripts/Editor/S03SinglePlayerArenaBuilder.cs`

Effect handling:

- Blessing assets store `BlessingEffectType`.
- Runtime effects are applied by `BlessingRuntimeController.ApplyBlessing()`.
- Attack-related blessing effects are used through `PlayerCombat3D` and `BlessingRuntimeController.CreateAttackContext()`.

## 3. Current Blessing Pool Before Change

Before this change, `S03_BlessingManager.allBlessings` referenced 20 blessing assets:

| Asset | Display Name | Hero | Rarity | Effect |
| --- | --- | --- | --- | --- |
| `ADV_ThanhGiapAuLac.asset` | Thành Giáp Âu Lạc | An Dương Vương | Common | Armor |
| `ADV_NoThan.asset` | Nỏ Thần | An Dương Vương | Rare | DivineCrossbow |
| `ADV_TuongThanh.asset` | Tường Thành | An Dương Vương | Epic | DashBarrier |
| `ADV_CanhGioi.asset` | Cảnh Giới | An Dương Vương | Common | Awareness |
| `ADV_ThanhCoLoa.asset` | Thành Cổ Loa | An Dương Vương | Legendary | CoLoaCitadel |
| `TT_HieuTrieu.asset` | Hiệu Triệu | Trưng Trắc | Rare | LowHealthDamage |
| `TT_CoKhoiNghia.asset` | Cờ Khởi Nghĩa | Trưng Trắc | Common | AttackSpeed |
| `TT_KhoiNghiaMeLinh.asset` | Khởi Nghĩa Mê Linh | Trưng Trắc | Epic | KillSkillEnergy |
| `TT_NuVuong.asset` | Nữ Vương | Trưng Trắc | Legendary | Revive |
| `TT_HaiBaKhoiNghia.asset` | Hai Bà Khởi Nghĩa | Trưng Trắc | Legendary | Uprising |
| `TN_KyTuong.asset` | Kỵ Tượng | Trưng Nhị | Common | MoveSpeed |
| `TN_XungPhong.asset` | Xung Phong | Trưng Nhị | Rare | DashDamage |
| `TN_TruyKich.asset` | Truy Kích | Trưng Nhị | Epic | PostDashDamage |
| `TN_BongChienTruong.asset` | Bóng Chiến Trường | Trưng Nhị | Epic | DashDecoy |
| `TN_VoiChien.asset` | Voi Chiến | Trưng Nhị | Legendary | WarElephant |
| `QT_HanhQuanThanToc.asset` | Hành Quân Thần Tốc | Quang Trung | Common | AttackSpeed |
| `QT_DongDa.asset` | Đống Đa | Quang Trung | Rare | CriticalPower |
| `QT_ThanTocBacTien.asset` | Thần Tốc Bắc Tiến | Quang Trung | Common | DashCooldown |
| `QT_ThienLoiTaySon.asset` | Thiên Lôi Tây Sơn | Quang Trung | Epic | CriticalLightning |
| `QT_XuanKyDau.asset` | Xuân Kỷ Dậu | Quang Trung | Legendary | KyDauFrenzy |

## 4. Allowed S03 Blessings

S03 now allows only these 3 An Dương Vương blessings:

| Asset | Display Name | GUID | Rarity | Max Stack | Effect |
| --- | --- | --- | --- | --- | --- |
| `Assets/Blessings/S03/ADV_CanhGioi.asset` | Cảnh Giới | `43d0f7fa41c4c4c4081052484ad829d1` | Common | 3 | Awareness |
| `Assets/Blessings/S03/ADV_ThanhGiapAuLac.asset` | Thành Giáp Âu Lạc | `cae748df425c1154ba19fee029ef2111` | Common | 3 | Armor |
| `Assets/Blessings/S03/ADV_NoThan.asset` | Nỏ Thần | `d8ca84020990a354097f1c7138db717d` | Rare | 3 | DivineCrossbow |

The current S03 runtime pool has exactly 3 references.

## 5. Removed From S03 Runtime Pool

The following assets were removed only from the `S03.unity` runtime `allBlessings` list. Their asset files were not deleted or modified.

| Asset | Display Name | Hero | Reason Removed From S03 Pool |
| --- | --- | --- | --- |
| `ADV_TuongThanh.asset` | Tường Thành | An Dương Vương | Not one of the 3 requested S03 skills |
| `ADV_ThanhCoLoa.asset` | Thành Cổ Loa | An Dương Vương | Not one of the 3 requested S03 skills |
| `TT_HieuTrieu.asset` | Hiệu Triệu | Trưng Trắc | Non-An Dương Vương group |
| `TT_CoKhoiNghia.asset` | Cờ Khởi Nghĩa | Trưng Trắc | Non-An Dương Vương group |
| `TT_KhoiNghiaMeLinh.asset` | Khởi Nghĩa Mê Linh | Trưng Trắc | Non-An Dương Vương group |
| `TT_NuVuong.asset` | Nữ Vương | Trưng Trắc | Non-An Dương Vương group |
| `TT_HaiBaKhoiNghia.asset` | Hai Bà Khởi Nghĩa | Trưng Trắc | Non-An Dương Vương group |
| `TN_KyTuong.asset` | Kỵ Tượng | Trưng Nhị | Non-An Dương Vương group |
| `TN_XungPhong.asset` | Xung Phong | Trưng Nhị | Non-An Dương Vương group |
| `TN_TruyKich.asset` | Truy Kích | Trưng Nhị | Non-An Dương Vương group |
| `TN_BongChienTruong.asset` | Bóng Chiến Trường | Trưng Nhị | Non-An Dương Vương group |
| `TN_VoiChien.asset` | Voi Chiến | Trưng Nhị | Non-An Dương Vương group |
| `QT_HanhQuanThanToc.asset` | Hành Quân Thần Tốc | Quang Trung | Non-An Dương Vương group |
| `QT_DongDa.asset` | Đống Đa | Quang Trung | Non-An Dương Vương group |
| `QT_ThanTocBacTien.asset` | Thần Tốc Bắc Tiến | Quang Trung | Non-An Dương Vương group |
| `QT_ThienLoiTaySon.asset` | Thiên Lôi Tây Sơn | Quang Trung | Non-An Dương Vương group |
| `QT_XuanKyDau.asset` | Xuân Kỷ Dậu | Quang Trung | Non-An Dương Vương group |

## 6. Files Modified

Modified:

- `Assets/Scenes/S03.unity`

Changed field:

- `S03_BlessingManager.allBlessings`

The list was reduced from 20 blessing asset references to 3 blessing asset references.

Not modified:

- Blessing `.asset` files
- Blessing scripts
- Card UI layout
- Reroll/skip mechanics
- Player movement
- Camera
- Wave/minion systems
- Combat systems
- Sword setup

## 7. Backup Files Created

Created before modifying the scene:

`Assets/Scenes/S03_before_adv_only_blessings.unity`

No `.cs.bak` files were created because no C# files were modified.

No `.asset` files were modified.

## 8. Upgrade / Level Logic Check

The existing upgrade logic is preserved.

`BlessingManager.RollDistinctChoices(3)` filters out blessings where:

- the blessing is null
- the blessing is ultimate
- the blessing already reached `MaxStack`

The 3 allowed S03 blessings all have `maxStack: 3`, so they can still appear for Bậc 1/3, 2/3, and 3/3.

When one of the 3 blessings is maxed, it naturally drops out of the roll pool. If fewer than 3 valid blessings remain, the current UI binding hides empty cards. If all 3 are maxed, `PresentChoices()` receives zero choices and finishes the selection without entering an infinite loop.

## 9. Reroll Logic Check

The existing reroll logic is preserved.

`RerollChoices()` calls the same `RollDistinctChoices(3)` method, so reroll can only pull from the current S03 `allBlessings` list:

- Cảnh Giới
- Thành Giáp Âu Lạc
- Nỏ Thần

If reroll happens when no valid blessings remain, the existing code calls `SkipBlessing()` safely.

## 10. Skip Logic Check

The existing skip logic is preserved.

`SkipBlessing()` closes the selection, disables card interaction, shows skip feedback, and calls the wave continuation callback through `FinishSelection()`.

No skip UI references were changed.

## 11. Test Checklist

1. Open `Assets/Scenes/S03.unity`.
2. Press Play.
3. Reach blessing selection.
4. Confirm the first selection shows only An Dương Vương cards.
5. Confirm the normal pool contains only `Cảnh Giới`, `Thành Giáp Âu Lạc`, and `Nỏ Thần`.
6. Press reroll and confirm it still only shows those 3 blessings.
7. Choose each blessing in separate playthroughs and confirm the effect applies.
8. Confirm Bậc 1/3, Bậc 2/3, and Bậc 3/3 still update correctly.
9. Confirm skip still closes the blessing UI and continues wave progression.
10. Confirm no Trưng Trắc, Trưng Nhị, Quang Trung, or other non-allowed card appears in S03.
11. Confirm no missing references appear in the Console.
12. Confirm no infinite loop occurs after rerolling or after blessings reach max stack.

## 12. Final Result

S03 now uses only the requested An Dương Vương blessing pool at runtime:

- Cảnh Giới
- Thành Giáp Âu Lạc
- Nỏ Thần

No blessing assets were deleted.

No blessing effects were removed from the project.

No Trưng Trắc, Trưng Nhị, Quang Trung, or other non-allowed blessing remains in the S03 runtime `allBlessings` pool.
