using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Entities;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.PhongVanVM;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;
using Nhom6_QLHoSoTuyenDung.Data;

namespace Nhom6_QLHoSoTuyenDung.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.HR}")]
    public class LichPhongVansController : Controller
    {
        private readonly ILichPhongVanService _lichService;
        private readonly AppDbContext _context;

        public LichPhongVansController(ILichPhongVanService lichService, AppDbContext context)
        {
            _lichService = lichService;
            _context = context;
        }

        // 1. Dashboard lịch phỏng vấn với bộ lọc đa dạng
        public async Task<IActionResult> Index(string? keyword, string? trangThai, string? viTriId, string? phongId, DateTime? tuNgay, DateTime? denNgay)
        {
            var dashboard = await _lichService.GetDashboardAsync(keyword, trangThai, viTriId, phongId, tuNgay, denNgay);
            var chuaCoLich = await _lichService.GetUngViensChuaCoLichAsync();
            var biHuy = await _lichService.GetUngViensBiHuyLichAsync();

            ViewBag.UngViensChuaCoLich = chuaCoLich;
            ViewBag.LichBiHuy = biHuy;
            ViewBag.PhongBans = await _context.PhongPhongVans.ToListAsync();
            ViewBag.ViTriList = await _context.ViTriTuyenDungs.ToListAsync();

            ViewBag.CurrentKeyword = keyword;
            ViewBag.CurrentTrangThai = trangThai;
            ViewBag.CurrentViTriId = viTriId;
            ViewBag.CurrentPhongId = phongId;
            ViewBag.CurrentTuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.CurrentDenNgay = denNgay?.ToString("yyyy-MM-dd");

            return View(dashboard);
        }


        // 2. Trả về popup tạo lịch (giao diện HR)
        public async Task<IActionResult> TaoLichPopup(string? ungVienId = null)
        {
            var vm = await _lichService.GetFormDataAsync(ungVienId); // null cũng được
            if (vm == null)
                return NotFound();

            // ✅ Lọc người dùng có vai trò là Người phỏng vấn
            var nguoiPhongVanIds = await _context.NguoiDungs
                .Where(nd => nd.VaiTro == "Interviewer") // hoặc dùng enum nếu có
                .Select(nd => nd.NhanVienId)
                .ToListAsync();

            // ✅ Lấy danh sách nhân viên tương ứng
            vm.NguoiPhongVanOptions = await _context.NhanViens
                .Where(nv => nguoiPhongVanIds.Contains(nv.MaNhanVien))
                .Select(nv => new SelectListItem
                {
                    Value = nv.MaNhanVien,
                    Text = nv.HoTen + " (" + nv.Email + ")"
                }).ToListAsync();
            ViewBag.UngViensChuaCoLich = await _context.UngViens
    .Where(u => !_context.LichPhongVans.Any(l => l.UngVienId == u.MaUngVien))
    .Select(u => new { u.MaUngVien, u.HoTen, u.Email })
    .ToListAsync();

            return PartialView("_FormTaoLichPhongVan", vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLichFromPopup(TaoLichPhongVanVM vm)
        {
            if (string.IsNullOrEmpty(vm.TrangThai))
                vm.TrangThai = TrangThaiPhongVanEnum.DaLenLich.ToString();

            // Tạo DTO từ ViewModel
            var lichVM = new CreateLichPhongVanVM
            {
                UngVienId = vm.UngVienId,
                PhongPhongVanId = vm.PhongPhongVanId,
                ThoiGian = vm.ThoiGian,
                NhanVienIds = vm.NguoiPhongVanIds ?? new()
            };

            // Gọi service tạo lịch
            (bool success, string message) = await _lichService.CreateLichAsync(lichVM, isReschedule: vm.IsReschedule);

            // ❗️Nếu lỗi thì return ngay
            if (!success)
                return Json(new { success = false, message });

            // ✅ Chỉ chạy khi thành công
            var ungVien = await _context.UngViens.FindAsync(vm.UngVienId);
            if (ungVien != null && ungVien.TrangThai == TrangThaiUngVienEnum.CanPhongVanLan2.ToString())
            {
                ungVien.TrangThai = TrangThaiUngVienEnum.DaCoLichVong2.ToString();
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message });
        }



        // 4. Xem chi tiết lịch phỏng vấn của ứng viên
        public async Task<IActionResult> ByUngVien(string id)
        {
            var lich = await _lichService.GetLichByUngVienIdAsync(id);
            if (lich == null)
                return Content("<p class='text-muted'>Chưa có lịch phỏng vấn cho ứng viên này.</p>", "text/html");

            var html = $@"
                <div>
                    <p><strong>Ứng viên:</strong> {lich.UngVien?.HoTen}</p>
                    <p><strong>Thời gian:</strong> {lich.ThoiGian:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Phòng:</strong> {lich.PhongPhongVan?.TenPhong} - {lich.PhongPhongVan?.DiaDiem}</p>
                    <p><strong>Trạng thái:</strong> {lich.TrangThai}</p>
                    <p><strong>Ghi chú:</strong> {lich.GhiChu}</p>
                </div>";

            return Content(html, "text/html");
        }
       
        [HttpGet]
        public async Task<IActionResult> TimUngVienDon(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
                return Json(null);

            var ungVien = await _context.UngViens
                .Include(u => u.ViTriUngTuyen)
                .Where(u => u.HoTen.Contains(tuKhoa) || u.Email.Contains(tuKhoa))
                .Select(u => new
                {
                    hoTen = u.HoTen,
                    email = u.Email,
                    viTri = u.ViTriUngTuyen.TenViTri,
                    trangThai = u.TrangThai.ToString()
                })
                .FirstOrDefaultAsync();

            return Json(ungVien);
        }



        [HttpGet]
        public async Task<IActionResult> TimUngVienSelect2(string tuKhoa)
        {
            var query = _context.UngViens
                .Include(u => u.ViTriUngTuyen)
                .Where(u => !_context.LichPhongVans.Any(l => l.UngVienId == u.MaUngVien));

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(u => u.HoTen.Contains(tuKhoa) || u.Email.Contains(tuKhoa));
            }

            var result = await query
                .OrderBy(u => u.HoTen)
                .Take(20)
                .Select(u => new
                {
                    id = u.MaUngVien,
                    text = $"{u.HoTen} ({u.Email})",
                    viTri = u.ViTriUngTuyen.TenViTri
                }).ToListAsync();

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> FormTaoLichVong2(string ungVienId)
        {
            // ✅ Nếu không đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // Nếu là Ajax → trả JSON để client hiểu
                    return Json(new { error = true, message = "Phiên đăng nhập đã hết" });
                }

                // Nếu không phải Ajax → redirect như bình thường
                return RedirectToAction("DangNhap", "NguoiDungs");
            }

            // ✅ Lấy ứng viên
            var ungVien = await _context.UngViens
                .Include(uv => uv.ViTriUngTuyen)
                .FirstOrDefaultAsync(uv => uv.MaUngVien == ungVienId);

            if (ungVien == null)
                return NotFound();

            if (ungVien.TrangThai != TrangThaiUngVienEnum.CanPhongVanLan2.ToString())
                return BadRequest("Ứng viên này chưa được đề xuất vòng 2!");

            // ✅ Lấy danh sách người phỏng vấn
            var nguoiPhongVanIds = await _context.NguoiDungs
                .Where(nd => nd.VaiTro == "Interviewer")
                .Select(nd => nd.NhanVienId)
                .ToListAsync();

            var vm = new TaoLichPhongVanVM
            {
                UngVienId = ungVien.MaUngVien,
                ViTriId = ungVien.ViTriUngTuyenId,
                TenUngVien = ungVien.HoTen,
                TenViTri = ungVien.ViTriUngTuyen?.TenViTri,
                PhongList = await _context.PhongPhongVans
                    .Select(p => new SelectListItem { Value = p.Id, Text = p.TenPhong })
                    .ToListAsync(),
                NguoiPhongVanOptions = await _context.NhanViens
                    .Where(nv => nguoiPhongVanIds.Contains(nv.MaNhanVien))
                    .Select(nv => new SelectListItem
                    {
                        Value = nv.MaNhanVien,
                        Text = nv.HoTen + " (" + nv.Email + ")"
                    }).ToListAsync()
            };

            return PartialView("_FormTaoLich", vm);
        }


        // tạo lịch cho trang ứng viên
        [HttpGet]
        public async Task<IActionResult> FormTaoLichLanDau(string ungVienId)
        {
            var ungVien = await _context.UngViens
                .Include(uv => uv.ViTriUngTuyen)
                .FirstOrDefaultAsync(uv => uv.MaUngVien == ungVienId);

            if (ungVien == null)
                return NotFound();

            // Trạng thái không cần kiểm tra vì đây là lần đầu

            var nguoiPhongVanIds = await _context.NguoiDungs
                .Where(nd => nd.VaiTro == "Interviewer")
                .Select(nd => nd.NhanVienId)
                .ToListAsync();

            var vm = new TaoLichPhongVanVM
            {
                UngVienId = ungVien.MaUngVien,
                ViTriId = ungVien.ViTriUngTuyenId,
                TenUngVien = ungVien.HoTen,
                TenViTri = ungVien.ViTriUngTuyen?.TenViTri,
                PhongList = await _context.PhongPhongVans
                    .Select(p => new SelectListItem { Value = p.Id, Text = p.TenPhong })
                    .ToListAsync(),
                NguoiPhongVanOptions = await _context.NhanViens
                    .Where(nv => nguoiPhongVanIds.Contains(nv.MaNhanVien))
                    .Select(nv => new SelectListItem
                    {
                        Value = nv.MaNhanVien,
                        Text = nv.HoTen + " (" + nv.Email + ")"
                    }).ToListAsync()
            };

            return PartialView("_FormTaoLich", vm); // dùng lại view có sẵn
        }

        [HttpGet]
        public async Task<IActionResult> TaoLichLaiPopup(string ungVienId)
        {
            var formVM = await _lichService.GetFormDataAsync(ungVienId);
            if (formVM == null)
            {
                return NotFound("Không tìm thấy ứng viên.");
            }

            var interviewerIds = await _context.NguoiDungs
                .Where(nd => nd.VaiTro == "Interviewer")
                .Select(nd => nd.NhanVienId)
                .ToListAsync();

            formVM.NguoiPhongVanOptions = await _context.NhanViens
                .Where(nv => interviewerIds.Contains(nv.MaNhanVien))
                .Select(nv => new SelectListItem
                {
                    Value = nv.MaNhanVien,
                    Text = nv.HoTen + " (" + nv.Email + ")"
                }).ToListAsync();

            formVM.IsReschedule = true;

            return PartialView("_FormTaoLich", formVM); // View dùng cho popup
        }


    }
}
