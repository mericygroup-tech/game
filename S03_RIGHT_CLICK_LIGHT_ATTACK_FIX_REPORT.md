# S03 Right Click Light Attack Fix Report

## 1. Scene Checked

Scene checked: `Assets/Scenes/S03.unity`.

The scene contains the `Player` object and an attached `PlayerCombat3D` component. The scene also contains `RightHandWeaponSocket` and the real sword object `e121696d973d293d9951a6ff238e1149`.

No sword transform, sword hierarchy, player model, camera, wave, minion, or blessing system was modified.

## 2. Player Object Found

Found: Yes.

Scene object:

`Player`

Relevant attached player scripts found in the scene include:

- `PlayerCombat3D`
- `PlayerController3D`
- `PlayerAnimatorDriver`
- `PlayerWeaponSlot3D`

## 3. PlayerVisual Animator Found

Found: Yes.

The player visual is a prefab/model instance using the Van An player model. The scene has an Animator Controller override/reference on the player visual Animator component.

Animator Controller GUID found in `S03.unity`:

`142ffef3f6152b5478d073311aea6726`

This resolves to:

`Assets/Animations/Player/VanAn.controller`

`PlayerAnimatorDriver` is already attached to `Player` and resolves the child `Animator` with `GetComponentInChildren<Animator>(true)` when its serialized Animator field is not assigned.

## 4. Animator Controller

Controller inspected:

`Assets/Animations/Player/VanAn.controller`

No Animator Controller modification was needed.

No Animator Controller backup was created because the controller was not modified.

## 5. LightAttack State Check

Found: Yes.

The Animator Controller contains a state named:

`LightAttack`

The state uses the light attack animation clip from the Van An animation assets.

## 6. LightAttack Parameter Check

Found: Yes.

The Animator Controller already contains an Animator parameter named:

`LightAttack`

Parameter type:

`Trigger`

No new Animator parameter was created.

## 7. Transition Check

Valid transition found: Yes.

The Animator Controller already has:

`Any State -> LightAttack`

Condition:

`LightAttack`

The `LightAttack` state also has an exit transition back to `Idle`, so the attack can finish naturally after the animation clip plays.

No Animator transitions were changed.

## 8. Existing Input/Combat Script Check

Existing combat/input script found:

`Assets/Scripts/Player/PlayerCombat3D.cs`

Existing animation trigger script found:

`Assets/Scripts/Player/PlayerAnimatorDriver.cs`

Before the fix, `PlayerCombat3D.Update()` checked `Input.GetMouseButtonDown(1)` and called `StartHeavyAttack()`, which played the push/heavy attack path.

The existing `Attack()` method already calls:

`animatorDriver?.PlayLightAttack();`

`PlayerAnimatorDriver.PlayLightAttack()` already calls:

`animator.SetTrigger(lightAttackHash);`

So the minimal fix was to route right mouse button input to the existing `Attack()` method.

## 9. Files Modified

Modified:

`Assets/Scripts/Player/PlayerCombat3D.cs`

Change made:

```csharp
if (Input.GetMouseButtonDown(1))
{
    Attack();
    return;
}
```

Created backups:

- `Assets/Scenes/S03_before_right_click_light_attack.unity`
- `Assets/Scripts/Player/PlayerCombat3D.cs.bak`

Not modified:

- `Assets/Scenes/S03.unity`
- `Assets/Animations/Player/VanAn.controller`
- Sword model files
- Sword hierarchy
- Player prefab/model

## 10. New Script Created

New script created: No.

A new `S03RightClickLightAttack.cs` script was not needed because `PlayerCombat3D` and `PlayerAnimatorDriver` already provide the correct attack and Animator trigger path.

## 11. Scene Attachment

Automatic scene attachment needed: No.

`PlayerCombat3D` is already attached to the `Player` object in `S03.unity`.

Because the fix was made in the existing attached script, no new scene component had to be added and `S03.unity` was not modified.

## 12. Manual Steps If Needed

No manual attachment steps should be needed.

If Unity reports a missing Animator reference during testing, verify the following in the Inspector:

1. Open `Assets/Scenes/S03.unity`.
2. Select `Player`.
3. Confirm `PlayerAnimatorDriver` is attached.
4. Confirm `PlayerVisual` has an Animator using `Assets/Animations/Player/VanAn.controller`.
5. Confirm the controller still contains the `LightAttack` Trigger parameter.

## 13. Test Checklist

1. Open `Assets/Scenes/S03.unity`.
2. Press Play.
3. Right-click the mouse.
4. Player should play the `LightAttack` animation.
5. Sword should swing because it is attached to the right hand.
6. Movement should still work.
7. Wave/minion system should still work.
8. No missing references should appear in the Console.

## 14. Final Result

Right mouse button is now connected to the existing `LightAttack` Animator trigger through the existing player combat flow.

Final runtime path:

`Input.GetMouseButtonDown(1)` -> `PlayerCombat3D.Attack()` -> `PlayerAnimatorDriver.PlayLightAttack()` -> `Animator.SetTrigger("LightAttack")`

The sword was not moved, rotated, reparented, deleted, or otherwise modified.
