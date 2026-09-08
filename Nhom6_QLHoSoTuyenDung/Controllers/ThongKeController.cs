using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.ThongKe;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;
using OfficeOpenXml;
using System.Drawing;

namespace Nhom6_QLHoSoTuyenDung.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ThongKeController : Controller
    {
        private readonly IThongKeService _thongKeService;
        private readonly AppDbContext _context;

        public ThongKeController(IThongKeService thongKeService, AppDbContext context)
        {
            _thongKeService = thongKeService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? loaiBaoCao, DateTime? tuNgay, DateTime? denNgay,
     string? trangThai, string? viTriId, string? phongBanId)
        {
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.Loai = loaiBaoCao;
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;
            ViewBag.TrangThai = trangThai;
            ViewBag.ViTriId = viTriId;
            ViewBag.PhongBanId = phongBanId;

            // 🟢 Gán danh sách filter cho ViewBag (thêm dòng này nếu thiếu)
            ViewBag.ListTrangThai = Enum.GetValues(typeof(TrangThaiUngVienEnum))
     .Cast<TrangThaiUngVienEnum>()
     .Select(e => new SelectListItem
     {
         Value = e.ToString(),
         Text = EnumExtensions.GetDisplayName(e)
     }).ToList();


            ViewBag.ListViTri = await _context.ViTriTuyenDungs
                .Select(v => new SelectListItem { Value = v.MaViTri, Text = v.TenViTri })
                .ToListAsync();

            ViewBag.ListPhongBan = await _context.PhongBans
                .Select(p => new SelectListItem { Value = p.Id, Text = p.TenPhong })
                .ToListAsync();

            var model = await _thongKeService.GetTongQuanAsync(tuKhoa, loaiBaoCao, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> BieuDoTrangThaiUngVien(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetBieuDoTheoTrangThaiUngVienAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoNguonUngVien(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetBieuDoNguonUngVienAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoTheoViTri(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetBieuDoTheoViTriUngTuyenAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoTheoPhongBan(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetBieuDoTheoPhongBanAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoXuHuongNopHoSo(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetXuHuongTheoThangAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoXuHuong12Thang(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetXuHuongTuyenDung12ThangAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> BieuDoDiemDanhGia(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetBieuDoDanhGiaUngVienAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> ViTriTuyenThanhCong(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var data = await _thongKeService.GetViTriTuyenThanhCongAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return PartialView("_ViTriThanhCongPartial", data);
        }

        [HttpGet]
        public async Task<IActionResult> XuatBaoCao(string? tuKhoa, string? loaiBaoCao, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var reportData = await _thongKeService.GetTongQuanAsync(tuKhoa, loaiBaoCao, tuNgay, denNgay, trangThai, viTriId, phongBanId);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BaoCaoUngVien");

            ws.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ ỨNG VIÊN";
            ws.Cell(2, 1).Value = $"Từ ngày: {(tuNgay?.ToString("dd/MM/yyyy") ?? "Không rõ")}";
            ws.Cell(2, 2).Value = $"Đến ngày: {(denNgay?.ToString("dd/MM/yyyy") ?? "Không rõ")}";

            ws.Cell(4, 1).Value = "Tổng ứng viên";
            ws.Cell(4, 2).Value = reportData.TongUngVien;

            ws.Cell(5, 1).Value = "Đã tuyển";
            ws.Cell(5, 2).Value = reportData.SoDaTuyen;

            ws.Cell(6, 1).Value = "Đang xử lý";
            ws.Cell(6, 2).Value = reportData.SoDangXuLy;

            ws.Cell(7, 1).Value = "Vị trí đang tuyển";
            ws.Cell(7, 2).Value = reportData.SoViTriDangTuyen;

            ws.Cell(9, 1).Value = "STT";
            ws.Cell(9, 2).Value = "Họ tên";
            ws.Cell(9, 3).Value = "Email";
            ws.Cell(9, 4).Value = "Vị trí";
            ws.Cell(9, 5).Value = "Ngày nộp";

            int row = 10;
            int stt = 1;
            foreach (var uv in reportData.UngVienDaTuyen)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = uv.HoTen;
                ws.Cell(row, 3).Value = uv.Email;
                ws.Cell(row, 4).Value = uv.TenViTri;
                ws.Cell(row, 5).Value = uv.NgayNop.ToString("dd/MM/yyyy");
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"BaoCaoUngVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> XuatBaoCaoDayDu([FromBody] BaoCaoRequestVM request)
        {
            if (request == null)
                return BadRequest("Dữ liệu không hợp lệ");

            if (!request.TuNgay.HasValue || !request.DenNgay.HasValue)
                return BadRequest("Vui lòng chọn khoảng thời gian cần xuất báo cáo.");

            if (request.LoaiBaoCao != null && request.LoaiBaoCao != "daydu")
            {
                return await XuatBaoCaoThongKe(request); // 👉 nếu không phải loại daydu thì chuyển sang hàm mới
            }

            var report = await _thongKeService.XuatBaoCaoDayDuAsync(new()
            {
                TuKhoa = request.TuKhoa,
                TuNgay = request.TuNgay,
                DenNgay = request.DenNgay,
                TrangThai = request.TrangThai,
                ViTriId = request.ViTriId,
                PhongBanId = request.PhongBanId,
                LoaiBaoCao = request.LoaiBaoCao
            });

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BaoCaoDayDu");

            ws.Cell(1, 1).Value = "BÁO CÁO CHI TIẾT ỨNG VIÊN";
            ws.Cell(2, 1).Value = $"Từ ngày: {request.TuNgay?.ToString("dd/MM/yyyy") ?? "Không rõ"}";
            ws.Cell(2, 2).Value = $"Đến ngày: {request.DenNgay?.ToString("dd/MM/yyyy") ?? "Không rõ"}";
            ws.Cell(3, 1).Value = $"Loại báo cáo: {request.LoaiBaoCao ?? "Tổng hợp"}";

            ws.Cell(5, 1).InsertTable(report);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"BaoCaoUngVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        private async Task<IActionResult> XuatBaoCaoThongKe(BaoCaoRequestVM request)
        {
            List<BieuDoItemVM> data;
            string tieuDe;

            switch (request.LoaiBaoCao)
            {
                case "trangthai":
                    data = await _thongKeService.GetBieuDoTheoTrangThaiUngVienAsync(
                        request.TuKhoa, request.TuNgay, request.DenNgay, request.TrangThai, request.ViTriId, request.PhongBanId);
                    tieuDe = "Thống kê theo trạng thái ứng viên";
                    break;

                case "nguon":
                    data = await _thongKeService.GetBieuDoNguonUngVienAsync(
                        request.TuKhoa, request.TuNgay, request.DenNgay, request.TrangThai, request.ViTriId, request.PhongBanId);
                    tieuDe = "Thống kê theo nguồn ứng viên";
                    break;

                case "vitri":
                    data = await _thongKeService.GetBieuDoTheoViTriUngTuyenAsync(
                        request.TuKhoa, request.TuNgay, request.DenNgay, request.TrangThai, request.ViTriId, request.PhongBanId);
                    tieuDe = "Thống kê theo vị trí ứng tuyển";
                    break;

                case "phongban":
                    data = await _thongKeService.GetBieuDoTheoPhongBanAsync(
                        request.TuKhoa, request.TuNgay, request.DenNgay, request.TrangThai, request.ViTriId, request.PhongBanId);
                    tieuDe = "Thống kê theo phòng ban";
                    break;

                default:
                    return BadRequest("Loại báo cáo thống kê không hợp lệ.");
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ThongKe");

            ws.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ";
            ws.Cell(2, 1).Value = $"Từ ngày: {request.TuNgay?.ToString("dd/MM/yyyy") ?? "Không rõ"}";
            ws.Cell(2, 2).Value = $"Đến ngày: {request.DenNgay?.ToString("dd/MM/yyyy") ?? "Không rõ"}";
            ws.Cell(3, 1).Value = $"Loại thống kê: {tieuDe}";

            ws.Cell(5, 1).Value = "Nội dung";
            ws.Cell(5, 2).Value = "Số lượng";

            int row = 6;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.Ten;
                ws.Cell(row, 2).Value = item.SoLuong;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"ThongKe_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }




    }

}
