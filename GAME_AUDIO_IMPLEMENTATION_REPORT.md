# Báo cáo tích hợp âm thanh

## Kiến trúc đã thêm

- `GameAudioDirector` được khởi tạo trước scene đầu tiên và tồn tại xuyên scene.
- Hai `AudioSource` nhạc nền crossfade bằng `unscaledDeltaTime`, nên vẫn chuyển nhạc đúng khi màn chọn Blessing tạm dừng `Time.timeScale`.
- Các bus `Master`, `Music`, `Ambience`, `Sfx`, `Ui` có volume độc lập và lưu bằng `PlayerPrefs`.
- Nhạc/ambience dài dùng Streaming + Vorbis; SFX/UI ngắn dùng Decompress On Load + PCM để giảm độ trễ.
- Sáu voice SFX luân phiên cho phép nhiều âm thanh combat phát chồng mà không tạo/destroy GameObject liên tục.
- Button được gắn tiếng click tự động ở runtime; slider âm lượng/sensitivity tương lai được hỗ trợ tiếng tick có throttle.
- `GameAudio` là facade null-safe để gameplay không phụ thuộc scene reference hay singleton lookup thủ công.

## Mapping đang hoạt động

| Tình huống trong dự án | Clip | Trạng thái |
|---|---|---|
| Main Menu | `MUSIC_MainTheme_VietnamHeroic.mp3` | Đã tự động map theo scene |
| Intro game S01 | `MUSIC_Intro_VietnamFlute.mp3` | Đã map ở volume nền thấp để không lấn audio video |
| S01 City Prototype | `City ambience.mp3` hiện có | Giữ `S01Soundscape`, đã nối vào bus volume mới |
| S02 Underground Cave | `Cave ambience.mp3` | Đã map theo scene |
| TimeRift cộng hưởng | `Time Rift Hum.mp3` | Đã map vào `ActivateResonance` |
| Chuẩn bị chiến đấu S03 | `SFX_WarDrums_Loop.wav` | Đã map khi vào S03 intro |
| Combat S02/S03 | `MUSIC_Battle_Pursuit.wav` | Đã map khi stabilization/arena bắt đầu |
| Cảnh thất bại | `UI_Lose.wav` + `MUSIC_WarLament.mp3` | Đã map vào `PlayerHealth3D.Die` |
| Hoàn thành toàn bộ wave hữu hạn | `UI_Win.wav` + `MUSIC_Victory.mp3` | Đã map vào nhánh `maxWaves > 0` |
| Bấm button | `UI_Click.wav` | Đã gắn tự động cho toàn bộ Button |
| Kéo slider âm lượng/sensitivity | `UI_SliderTick.wav` | Hệ thống đã sẵn sàng; hiện chưa có slider phù hợp trong scene |
| Vung kiếm | `Slow Motion Whoosh.mp3` (fallback) | Đã map vào light/heavy attack, có random pitch/gain |
| Kiếm trúng mục tiêu | `Impact Hit.mp3` (fallback) | Chỉ phát khi attack thật sự trúng ít nhất một enemy |

## Clip đã nhập nhưng chưa kích hoạt

- `MUSIC_FinalBoss_Epic.wav`: dự án chưa có boss cuối hoặc boss director.
- `MUSIC_LastStand_Heroic.wav`: dự án chưa có trận/cutscene cuối được định danh.
- `MUSIC_SadMemory_Vietnam.mp3`: được giữ làm fallback thứ hai cho cảnh thất bại; `WarLament` đang là lựa chọn chính.

Các state `FinalBoss` và `LastStand` đã có trong API, nhưng không tự suy đoán tình huống để tránh phát sai ngữ cảnh.

## Mục còn thiếu trong tài nguyên/dự án

- Không có `AMB_RiverForest.wav` trong `D:/PRU213/Sounds` hoặc trong dự án. S01 dùng ambience thành phố sẵn có, S02 dùng ambience hang động phù hợp ngữ cảnh.
- Không có file sword slash/sword impact chuyên dụng. Hiện dùng hai clip fallback sẵn có; chỉ cần thay resource tương ứng là không phải sửa combat logic.
- Main Menu settings hiện chỉ là panel placeholder, chưa có thanh âm lượng thật. API volume và slider feedback đã sẵn sàng để nối khi UI được bổ sung.
- `S03.unity` đang cấu hình `maxWaves: 0` (vô hạn), nên nhánh Victory đã được code nhưng không thể đạt trong cấu hình hiện tại.

## Xác minh

- Full runtime C# compile check: **0 errors**.
- Audio editor importer/validator compile check: **0 errors, 0 warnings**.
- Unity Editor `6000.5.1f1` đã compile dự án thành công và validator xác nhận đủ 17 clip có thể load.
- Dự án được nâng từ `6000.4.6f1` lên `6000.5.1f1` để nhận bản sửa chính thức cho lỗi `Internal error - unexpected guid mismatch` của Unity/AI Assistant; log sau migration có 0 GUID mismatch và 0 compiler error.
- Main Menu đã được chạy một vòng Play Mode rồi thoát; kết quả sau domain reload là 0 GUID mismatch, 0 compiler error và 0 runtime exception.
- Có menu kiểm tra: `Tools > Dong Chay Anh Hung > Audio > Validate Audio Setup`.
