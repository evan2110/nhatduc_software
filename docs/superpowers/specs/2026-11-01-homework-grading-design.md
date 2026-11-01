# Bài tập và chấm bài bằng Gemini

Ngày: 2026-11-01

## Mục tiêu

Giáo viên tạo bài tập trên hệ thống, sao chép tin giao bài vào nhóm Zalo bằng tay, tải bài học viên gửi trong nhóm lên để Gemini chấm, sửa phiếu, rồi sao chép phiếu dán lại nhóm Zalo. Phần mềm không gọi Zalo và không dùng thư viện Zalo không chính thức.

Học viên không đăng nhập. Giáo viên là người tải bài.

## Phạm vi

Có trong phiên bản này:

- Trang giáo viên `/teacher/homework`, tên menu **Bài tập**, đặt cạnh **Nhận xét / Điểm**. Chỉ tài khoản vai trò Teacher. Giáo viên chỉ thấy lớp mình dạy, theo `ClassService.GetClassesByTeacher`.
- Hai loại bài: **Viết** và **Nói**. Giáo viên chọn lúc tạo bài.
- Đáp án bài viết: gõ chữ, tải file, hoặc cả hai.
- Chấm bằng Gemini. Giáo viên sửa bản nháp rồi mới lưu.
- Sao chép tin giao bài và sao chép phiếu.
- Nút **Đưa vào đánh giá tháng** cho từng học viên, do giáo viên chọn bài nào được ghi.
- Ảnh, PDF và file đáp án lưu trên Google Drive. Cơ sở dữ liệu chỉ giữ mã file và link.

Không có trong phiên bản này:

- Gửi Zalo tự động, Official Account, nhóm GMF.
- Tài khoản học viên hoặc phụ huynh.
- Trang quản trị cho bài tập.
- Màn hình bài tập trên ứng dụng desktop.
- Lưu video hoặc ghi âm.
- Tự động chấm lại các bài cũ khi đáp án đổi.
- Đổi điểm tháng thành điểm trung bình.

## Luồng giáo viên

1. Chọn lớp, nhập tên bài, ngày giao, loại bài và đề bài. Tên bài và đề bài là bắt buộc. Bài viết có thể gõ đáp án hoặc tải file đáp án.
2. **Sao chép tin giao bài** chỉ gồm lớp, tên bài, ngày giao và đề. Không gồm đáp án.
3. Mở bài tập. Danh sách học viên là học viên đang trong lớp.
4. Tải bài của một học viên. Bài viết nhận `jpg`, `jpeg`, `png`, `webp` hoặc `pdf`. Bài nói nhận `mp3`, `m4a`, `wav`, `mp4`, `webm` hoặc `mov`.
5. **Chấm**. Gemini trả điểm, nhận xét ngắn và phiếu. Giáo viên sửa trên form.
6. **Lưu**. Trước khi lưu, không sao chép phiếu và không đưa vào đánh giá tháng.
7. **Sao chép phiếu** lấy đúng phiếu đã lưu. Trình duyệt không cho chép vào clipboard thì hiện đoạn chữ để giáo viên tự chép.
8. **Đưa vào đánh giá tháng** ghi điểm và nhận xét ngắn của em đó. Phiếu đầy đủ ở lại bài tập.

Rời trang khi chưa lưu thì bản nháp mất. Phiếu đã lưu lần trước vẫn còn.

## Dữ liệu

Tạo bảng trong `DatabaseInitializer` bằng `CREATE TABLE IF NOT EXISTS`. Câu lệnh ghi dữ liệu viết một dòng.

### HomeworkAssignments

Mỗi bài thuộc một lớp và một giáo viên.

- `ClassId`, `TeacherId`
- `Title`
- `AssignedOn` là ngày giao
- `Kind` nhận `Written` hoặc `Speaking`
- `Prompt` là đề bài
- `AnswerKeyText` được để trống
- `AnswerKeyFileName`, `AnswerKeyDriveFileId`, `AnswerKeyDriveWebViewLink` được để trống
- `DriveFolderId` được để trống cho đến khi có file đầu tiên
- `CreatedAt`

Bài viết phải có `AnswerKeyText` hoặc file đáp án trước khi chấm. Bài nói không có đáp án.

### HomeworkSubmissions

Mỗi học viên một dòng cho mỗi bài. Ràng buộc duy nhất `(AssignmentId, StudentId)`.

- `Score` kiểu `NUMERIC(5,2)`, để trống khi chưa có phiếu đã lưu
- `ShortComment` là nhận xét ngắn đưa sang đánh giá tháng
- `ReportText` là phiếu đầy đủ
- `FileName`, `DriveFileId`, `DriveWebViewLink` chỉ dùng cho bài viết đã lưu
- `PendingFileName`, `PendingDriveFileId`, `PendingDriveWebViewLink` là file bài viết đang chờ lưu
- `GradedAt`

Bài nói không ghi các cột file.

Tải bài viết mới tạo hoặc cập nhật dòng nộp và ghi vào các cột `Pending*`. File và phiếu đã lưu giữ nguyên. Tải một file chờ khác thì xóa file `Pending` cũ trên Drive. Khi **Lưu** phiếu mới, các cột chính nhận file `Pending`, rồi xóa file Drive cũ trong các cột chính. Chấm lỗi hoặc không lưu thì phiếu đã lưu và file đã lưu vẫn còn, file `Pending` còn để chấm lại.

Đổi đáp án không tự chấm lại. Phiếu cũ giữ đến khi giáo viên chấm và lưu lại.

## Google Drive

Dùng `GoogleDriveService` và tài khoản Google hiện có. Thêm `HomeworkRootFolderId`, biến môi trường `GOOGLE_DRIVE_HOMEWORK_ROOT_FOLDER_ID`, mặc định `1i9Y9XjhW2emcYJzC5F3Ho1HzFzDLLIiU`.

Thư mục tạo khi chưa có, theo thứ tự:

`{HomeworkRootFolderId} / {Tên giáo viên} / {Tên môn} / {Tên lớp} - {Tên bài}`

Tên môn là `Courses.Name` của `Classes.CourseId`. Tên môn trống thì dùng `CourseCode`. Cả hai trống thì dùng tên lớp. `DriveFolderId` lưu trên bài tập sau lần tạo thư mục đầu. Đổi tên bài sau đó không đổi tên thư mục trên Drive.

Tải lại file đã lưu chỉ xóa file cũ trên Drive sau khi phiếu mới được lưu. Giới hạn mỗi file 50MB, cùng mức tài liệu giảng dạy.

Video và ghi âm chỉ nằm trong bộ nhớ của lần gọi Gemini, rồi bỏ. Không đưa lên Drive và không ghi vào cơ sở dữ liệu.

## Đánh giá tháng

**Đưa vào đánh giá tháng** gọi `EvaluationService.Save` với điểm, nhận xét ngắn, và năm tháng lấy từ `AssignedOn`. Không ghi `ReportText`.

Trang đánh giá tháng vẫn hiện dòng mới nhất của học viên trong tháng đó. Bấm nút cho một bài khác thì dòng mới trở thành điểm tháng. Các bài tập vẫn còn đủ phiếu.

Điểm phải đã lưu và nằm từ 0 đến 10. Nhận xét ngắn được để trống.

## Gemini

Khóa API chỉ ở biến môi trường `GEMINI_API_KEY`. Tên model ở cấu hình `Gemini:Model`, ghi đè bằng `GEMINI_MODEL`. Giá trị mặc định là `gemini-2.5-flash`. Model này phải nhận ảnh, PDF, âm thanh và video. Đổi model không cần sửa mã.

Điểm trên thang 10, tối đa hai chữ số thập phân. Chấp nhận `8.67` và `8,67`. Ngoài khoảng 0 đến 10 thì coi như Gemini không trả được điểm: giáo viên tự điền, hệ thống không tự đặt điểm.

Phiếu bài viết bắt đầu bằng `PHIẾU CHẤM & SỬA BÀI`, rồi tên học viên, lớp, tên bài, điểm, từng chỗ đúng hoặc sai kèm bài làm, đáp án đúng và câu sửa, cuối là nhận xét chung.

Phiếu bài nói có điểm và nhận xét phát âm, độ trôi chảy, ngữ pháp, từ vựng, nội dung.

Giáo viên sửa cả ba phần trên form trước khi lưu.

## Lỗi

- Sai loại file, hoặc file quá 50MB: báo lỗi, không đổi phiếu đã lưu.
- Bài viết chưa có đáp án: không gọi Gemini.
- Drive hoặc Gemini chưa cấu hình, hoặc Gemini lỗi: báo trên trang. Ảnh đã lên Drive trong cột `Pending` vẫn giữ. Phiếu đã lưu vẫn còn.
- Giáo viên không dạy lớp đó thì không mở, không sửa bài.
- Học viên không còn trong lớp thì không tải bài và không chấm.
- Ứng dụng desktop không có màn hình này và không xóa hai bảng mới.

## Kiểm tra

Thử tự động, không gọi Gemini hay Drive thật:

- Ghép tên thư mục giáo viên, môn, lớp và bài, kể cả khi tên môn trống.
- Bài viết thiếu cả chữ đáp án và file đáp án thì chặn.
- `jpg`, `jpeg`, `png`, `webp`, `pdf` được nhận cho bài viết. `mp3`, `m4a`, `wav`, `mp4`, `webm`, `mov` được nhận cho bài nói. Loại còn lại bị từ chối.
- `8,67` và `7,5` thành số trên thang 10. Giá trị ngoài 0 đến 10 không được nhận.
- Đưa vào đánh giá tháng gửi điểm, nhận xét ngắn, và tháng của ngày giao bài. Không gửi phiếu đầy đủ.
- Lưu phiếu mới trỏ bài nộp sang file `Pending` và giữ lịch sử bài tập khi điểm tháng đổi sang bài khác.
