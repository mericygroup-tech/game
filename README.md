# Dong Chay Anh Hung 3D

Prototype Unity 6 / URP cho game 3D third-person historical fantasy adventure **Dong Chay Anh Hung**.

## Tong quan

Game theo chan nhom hoc sinh hien dai bi cuon vao mot bien co lich su - ky ao. Prototype hien tai tap trung vao hai canh dau:

- **MainMenu**: man hinh menu chinh, bam Bat Dau de vao S01.
- **S01_CityPrototype**: canh mo dau, tutorial chay tron trong thanh pho.
- **S02_UndergroundCave**: canh sinh ton - bi an duoi long dat, dan toi TimeRift.
- **S03**: single-player combat arena, wave quai va he thong Blessing Anh Linh.

Nhan vat chinh o S01 la hoc sinh binh thuong, nen khong co chien dau trong canh nay. Combat chi bat dau tam thoi trong S02 sau khi TimeRift cong huong voi Van An. S03 la prototype rieng cho combat arena va build roguelite.

## MainMenu

### Muc tieu canh

MainMenu la man hinh vao game theo phong cach hanh dong lich su Viet Nam:

- Logo lon `DONG CHAY ANH HUNG`.
- Nen toi, khoi lua, cong thanh Co Loa va tong vang dong / do tham.
- Intro fade tu man hinh den, logo hien dan, kiem roi xuong, sau do hien menu.
- Nut **BAT DAU** load sang `Assets/Scenes/S01_CityPrototype.unity`.
- Nut **CAI DAT** va **THANH TUU** co panel placeholder de noi tinh nang sau.
- Nut **THOAT** thoat game hoac dung Play Mode trong Unity Editor.
- Ho tro chuot, ban phim va dieu huong UI bang EventSystem.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild Main Menu`
- `Tools > Dong Chay Anh Hung > Verify Main Menu`

### File chinh

- Scene: `Assets/Scenes/MainMenu.unity`
- Runtime controller: `Assets/Scripts/UI/MainMenuController.cs`
- Hieu ung nut: `Assets/Scripts/UI/MainMenuButtonFX.cs`
- Editor builder: `Assets/Scripts/Editor/MainMenuBuilder.cs`

## S01_CityPrototype

### Muc tieu canh

Nguoi choi hoc cac thao tac co ban:

- Di chuyen bang WASD.
- Chay nhanh bang Shift.
- Tranh chuong ngai vat.
- Tuong tac QTE voi vat can hop ly.
- Chay tron khoi Hac Tinh.
- Den khu sap mat dat de chuyen sang S02.

### Flow hien tai

1. **Khu thanh pho / bao tang**
   - Van An bat dau o khu duong hien dai.
   - UI huong dan cach di chuyen va chay.
   - Tin hieu bat thuong xuat hien qua story text.

2. **Duong bi chan**
   - Main road bi chan bang vat can cong trinh va do do nat.
   - Nguoi choi bi dan vao side route thay vi di thang tren duong lon.

3. **Duong dat / cong trinh**
   - Route rong va ro hon so voi ban cu.
   - Co bien chi dan, den, mui ten vang va cac marker de nguoi choi biet di dau.

4. **QTE blocker**
   - Khong dung metal gate giua duong nua.
   - Dung cac vat can hop ly hon nhu construction fence / fallen tree / debris.
   - Text QTE: `Nhan E lien tuc de vuot chuong ngai: X/Y`.

5. **Slow zone va collapse**
   - SlowZone dung bun / debris thay vi san dien.
   - Cuoi canh co crack/collapse zone va exit trigger chuan bi load S02.

### He thong chase S01

- `S01ChaseThreat` la Hac Tinh khong co HP, khong the giet.
- Uu tien chase Player truc tiep neu thay duong.
- Neu bi tuong/goc chan, dung waypoint lam navigation helper.
- Cham Player hoac vao catch distance thi Player chet ngay.
- Khong dung NavMesh.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S01 City Escape Zigzag`
- `Tools > Dong Chay Anh Hung > Create S01 Chase Threat`

## S02_UndergroundCave

### Muc tieu canh

S02 duoc thiet ke lai thanh canh survival mystery:

- Van An tinh day mot minh duoi long dat.
- Kham pha hang co, ky hieu Dong Son / Co Loa.
- Nghe tieng ban goi tu trong bong toi.
- Hac Tinh xuat hien tu ho sup phia tren, khong phai tu TimeRift.
- TimeRift cong huong va mo khoa phan kich tam thoi.
- Nguoi choi on dinh TimeRift trong 30 giay.
- TimeRift qua tai va keo nhom sang S03_CoLoaArrival.

### Flow hien tai

1. **Wake Area**
   - Player spawn an toan o dau hang.
   - Combat bi tat.
   - Co intro text va camera cutscene ngan.

2. **Ancient Signs Path**
   - Hang co co cac dau hieu Dong Son / Co Loa.
   - Co interactable symbol de tao cam giac bi an.
   - Anh sang xanh dan duong.

3. **Voices Path**
   - UI story text goi y ban be dang o gan do.
   - Route tiep tuc dan den vung ap luc.

4. **Hac Tinh Descent**
   - Hac Tinh roi/xuat hien tu `HacTinh_Descent_Hole`.
   - Camera shake va warning text bao nguoi choi chay.
   - Enemy spawn phia sau player, khong spawn truoc mat.

5. **TimeRift Chamber**
   - Chamber co san di lai an toan, khong con bi ket o vong TimeRift.
   - Vong/core TimeRift la visual, collider chan duong bi tat.
   - Co prompt `Nhan E de cong huong voi khe nut thoi gian`.

6. **Stabilization Event**
   - Sau khi nhan E, PlayerCombat3D duoc bat tam thoi.
   - UI hien tien do: `On dinh khe nut: X%`.
   - Hac Tinh pressure enemies spawn tu cave spawn points.
   - Sau 30 giay, TimeRift qua tai va load S03 neu scene da co trong Build Settings.

### Cutscene S02

Da them `S02CutsceneController`:

- Intro fade/camera move.
- Hac Tinh descent camera shake.
- TimeRift resonance orbit shot.
- Ending fade out sang S03.

Cutscene duoc gan tu `S02CaveBuilder` va co fallback tu runtime neu scene cu chua rebuild.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S02 Underground Cave`

## S03 Combat Arena

### Muc tieu canh

S03 hien la arena single player lay cam hung tu flow chon suc manh sau moi round:

- Player xuat hien tren map Co Loa that duoc lay tu nhanh PhongHT.
- Moi wave spawn Hac Tinh tu 8 diem spawn preset cua PhongHT quanh khu chien dau.
- Player di chuyen bang WASD, danh thuong bang Mouse0, heavy attack bang Mouse1, Dash bang Shift.
- Khi ha het quai trong wave, combat tam dung.
- UI hien 3 Blessing ngau nhien.
- Player chon 1 Blessing, hieu ung ap dung ngay.
- Wave tiep theo bat dau, Blessing co the cong don de tao build.

### Combat S03 nam o dau?

Combat S03 khong nam trong mot thu muc `Combat` rieng. No dang duoc chia theo vai tro de de quan ly:

- Scene combat: `Assets/Scenes/S03.unity`
- Map Cổ Loa S03 từ nhánh PhongHT: `Assets/Models/CoLoa/coloa_map_stage03_unity_colored.glb`
- Player di chuyen, dash, light/heavy attack, mau va death: `Assets/Scripts/Player`
- Quai Hac Tinh, chase, damage va HP quai: `Assets/Scripts/Minion`
- Dieu phoi wave, spawn quai, tam dung combat va mo Blessing UI: `Assets/Scripts/Scene/S03ArenaDirector.cs`
- UI mau Player, mau quai, dash: `Assets/Scripts/UI`
- Blessing Anh Linh, random 3 lua chon, cong don buff: `Assets/Scripts/Blessing`
- Du lieu Blessing S03 dang luu o: `Assets/Blessings/S03`
- Material runtime cho map Cổ Loa: `Assets/Models/CoLoa/Materials_RuntimeFixed`
- Tool dung de rebuild lai toan bo arena S03: `Assets/Scripts/Editor/S03SinglePlayerArenaBuilder.cs`

Neu muon sua gameplay chinh cua S03, thu tu nen xem la:

1. `Assets/Scripts/Player/PlayerCombat3D.cs`
2. `Assets/Scripts/Player/PlayerController3D.cs`
3. `Assets/Scripts/Player/PlayerHealth3D.cs`
4. `Assets/Scripts/Scene/S03ArenaDirector.cs`
5. `Assets/Scripts/Blessing/BlessingManager.cs`

### Blessing Anh Linh

He thong S03 dung cac Anh Hung Lich Su Viet Nam thay cho pantheon cua Hades/SWORN:

- **An Duong Vuong**: phong thu, la chan, canh gioi, No Than.
- **Trung Trac**: y chi chien dau, toc danh, hoi nang luong, hoi sinh.
- **Trung Nhi**: co dong, dash damage, truy kich, phan than.
- **Quang Trung**: tan cong boc phat, chi mang, giam hoi Dash, set danh.

Moi Blessing la `ScriptableObject` trong `Assets/Blessings/S03`.

Ultimate Blessing tu dong mo khi nguoi choi co du 3 Blessing khac nhau cua cung mot nhanh:

- `Thanh Co Loa`
- `Hai Ba Khoi Nghia`
- `Voi Chien`
- `Xuan Ky Dau`

### Blessing Choice UI

Sau khi clear wave, S03 hien man hinh `CHON BLESSING`:

- Hien 3 card Blessing ngau nhien.
- Card co ten, rarity `COMMON/RARE/EPIC/LEGENDARY`, icon placeholder, mo ta, stack va ten Anh Hung.
- Hover/chon card se doi nen theo nhanh Anh Hung va hien thong tin chi tiet.
- Nen hero dung Sprite trong `Assets/Blessings/S03/Backgrounds`.
- Co nut `LAM MOI (1)` de roll lai 3 card mot lan moi luot chon.
- Co nut `BO QUA` de quay lai gameplay ma khong nhan Blessing.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S03 Combat Arena`

## Scripts chinh

### Runtime

- `Assets/Scripts/Player/PlayerController3D.cs`
- `Assets/Scripts/Player/PlayerHealth3D.cs`
- `Assets/Scripts/Player/PlayerCombat3D.cs`
- `Assets/Scripts/Player/PlayerAnimatorDriver.cs`
- `Assets/Scripts/Minion/S01ChaseThreat.cs`
- `Assets/Scripts/Minion/MinionChase3D.cs`
- `Assets/Scripts/Minion/MinionHealth3D.cs`
- `Assets/Scripts/UI/PlayerHealthUI.cs`
- `Assets/Scripts/UI/MinionHealthBarUI.cs`
- `Assets/Scripts/UI/DashChargeUI.cs`
- `Assets/Scripts/Scene/S02CaveEventController.cs`
- `Assets/Scripts/Scene/S02CutsceneController.cs`
- `Assets/Scripts/Scene/S02TimeRiftTrigger.cs`
- `Assets/Scripts/Scene/S03ArenaDirector.cs`
- `Assets/Scripts/Blessing/BlessingDefinition.cs`
- `Assets/Scripts/Blessing/BlessingManager.cs`
- `Assets/Scripts/Blessing/BlessingRuntimeController.cs`
- `Assets/Scripts/Blessing/BlessingChoiceUI.cs`
- `Assets/Scripts/Scene/EscapeDoorQTE.cs`
- `Assets/Scripts/Player/SlowZone.cs`

### Editor builders

- `Assets/Scripts/Editor/S01CityEscapeBuilder.cs`
- `Assets/Scripts/Editor/S01ChaseSetupBuilder.cs`
- `Assets/Scripts/Editor/S02CaveBuilder.cs`
- `Assets/Scripts/Editor/S03SinglePlayerArenaBuilder.cs`
- `Assets/Scripts/Editor/PlayerVisualBuilder.cs`

## Cach test nhanh

### Test MainMenu

1. Mo scene `Assets/Scenes/MainMenu.unity`.
2. Bam Play.
3. Doi intro hien logo va menu.
4. Bam `BAT DAU`.
5. Kiem tra game load sang `S01_CityPrototype`.

### Test S01

1. Mo scene `Assets/Scenes/S01_CityPrototype.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S01 City Escape Zigzag`.
3. Chay `Tools > Dong Chay Anh Hung > Create S01 Chase Threat`.
4. Bam Play.
5. Test route: tutorial start -> side route -> QTE blocker -> slow zone -> collapse zone.

### Test S02

1. Mo scene `Assets/Scenes/S02_UndergroundCave.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S02 Underground Cave`.
3. Bam Play.
4. Test flow: intro -> ancient signs -> voices -> Hac Tinh descent -> TimeRift resonance -> 30s stabilization -> ending cutscene.

### Test S03

1. Mo scene `Assets/Scenes/S03.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S03 Combat Arena`.
3. Bam Play.
4. Test flow: Wave bat dau -> ha het quai -> chon 1 trong 3 Blessing -> wave tiep theo.
5. Neu Unity Hub bao `No active licenses`, vao `Manage licenses` va kich hoat Unity Personal/Student truoc khi bam Play.

## Ghi chu hien tai

- Project da co `.gitignore` Unity de khong commit `Library`, `Temp`, `obj`, `.vs`.
- Hinh anh/animation player hien van la prototype va can refactor sau neu muon chat luong nhan vat tot hon.
- Mot so warning `System.Net.Http` / `System.IO.Compression` den tu Unity AI / package editor, khong phai loi gameplay script.
