using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Entities;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.UngVien;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;

namespace Nhom6_QLHoSoTuyenDung.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.HR}")]
    public class UngViensController : Controller
    {
        private readonly IUngVienService _ungVienService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UngViensController(IUngVienService ungVienService, AppDbContext context, IWebHostEnvironment env)
        {
            _ungVienService = ungVienService;
            _context = context;
            _env = env;
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.ViTriList = new SelectList(
                 await _context.ViTriTuyenDungs
                     .Where(v => v.TrangThai == "Đang tuyển")
                     .ToListAsync(),
                 "MaViTri", "TenViTri");
                 
            ViewBag.FilterViTriList = new SelectList(
                 await _context.ViTriTuyenDungs.ToListAsync(),
                 "MaViTri", "TenViTri");
            ViewBag.GioiTinhList = new SelectList(
                Enum.GetValues(typeof(GioiTinhEnum))
                    .Cast<GioiTinhEnum>()
                    .Select(gt => new
                    {
                        Value = gt,
                        Text = gt.GetDisplayName()
                    }),
                "Value", "Text");
        }

        public async Task<IActionResult> Index(UngVienBoLocDonGianVM filter, int page = 1, int pageSize = 10)
        {
            if (HttpContext.Session.GetString("VaiTro") == "interviewer")
                return RedirectToAction("Index", "LichPhongVan");

            await LoadDropdownsAsync();

            var allUngViens = await _ungVienService.GetAllAsync(filter);
            var stats = await _ungVienService.GetDashboardStatsAsync(allUngViens);

            ViewBag.TongUngVien = stats["TongUngVien"];
            ViewBag.MoiTuanNay = stats["MoiTuanNay"];
            ViewBag.DaPhongVan = stats["DaPhongVan"];
            ViewBag.DaTuyen = stats["DaTuyen"];
            ViewBag.TyLeChuyenDoi = stats["TyLeChuyenDoi"];
            ViewBag.NguonLabels = stats["NguonLabels"];
            ViewBag.NguonValues = stats["NguonValues"];
            ViewBag.TrangThaiLabels = stats["TrangThaiLabels"];
            ViewBag.TrangThaiValues = stats["TrangThaiValues"];
            ViewBag.ViTriLabels = stats["ViTriLabels"];
            ViewBag.ViTriValues = stats["ViTriValues"];

            var filterViewModel = new BoLocViewModel
            {
                Keyword = filter.Keyword,
                TrangThai = filter.TrangThai,
                GioiTinh = filter.GioiTinh,
                ViTriId = filter.ViTriId,
                FromDate = filter.FromDate?.ToString("yyyy-MM-dd"),
                ToDate = filter.ToDate?.ToString("yyyy-MM-dd"),
                ViTriList = ((SelectList)ViewBag.FilterViTriList).ToList(),
                GioiTinhList = ((SelectList)ViewBag.GioiTinhList).ToList(),
                TrangThaiList = Enum.GetValues(typeof(TrangThaiUngVienEnum)) // 🟦 hoặc TrangThaiPhongVanEnum tuỳ form lọc
         .Cast<Enum>()
         .Select(tt => new SelectListItem
         {
             Value = tt.ToString(),
             Text = tt.GetDisplayName()
         }).ToList(),
                ResetUrl = Url.Action("Index", "UngViens")
            };

            ViewBag.FilterViewModel = filterViewModel;

            var pagedUngViens = allUngViens.OrderByDescending(x => x.NgayNop).ToList();


            ViewBag.LichPhongVanMap = await _context.LichPhongVans.GroupBy(l => l.UngVienId).ToDictionaryAsync(g => g.Key, g => g.First());

            return View(pagedUngViens);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var ungVien = await _ungVienService.GetByIdAsync(id);
            if (ungVien == null) return NotFound();
            return PartialView("_UngVienDetailsPartial", ungVien);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UngVien model, IFormFile CvFile)
        {
            if (!ModelState.IsValid ||CvFile == null)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin và chọn CV.";
                return RedirectToAction("Index");
            }

            var result = await _ungVienService.AddAsync(model, CvFile, _env);
            if (result == -1)
            {
                TempData["ErrorMessage"] = "Ứng viên đã tồn tại.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Thêm ứng viên thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ImportFromExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel hợp lệ.";
                return RedirectToAction("Index");
            }

            try
            {
                var count = await _ungVienService.ImportFromExcelAsync(excelFile);
                TempData["SuccessMessage"] = $"✅ Đã nhập {count} ứng viên.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Lỗi khi xử lý file: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // nếu đã copy sẵn file vào thư mục cv
        [HttpPost]
        public async Task<IActionResult> CapNhatLinkCVHangLoat()
        {
            try
            {
                var wwwrootPath = _env.WebRootPath;
                var cvFolder = Path.Combine(wwwrootPath, "cv");

                if (!Directory.Exists(cvFolder))
                {
                    TempData["ErrorMessage"] = "❌ Thư mục chứa CV không tồn tại.";
                    return RedirectToAction("Index");
                }

                var pdfFiles = Directory.GetFiles(cvFolder, "*.pdf")
                                        .Select(Path.GetFileName)
                                        .ToHashSet(); // VD: "UV0984.pdf"

                var ungViens = await _context.UngViens.ToListAsync();
                int updated = 0;

                foreach (var uv in ungViens)
                {
                    var expectedFile = $"{uv.MaUngVien}.pdf";
                    var correctLink = $"/cv/{expectedFile}";

                    if (pdfFiles.Contains(expectedFile) && uv.LinkCV != correctLink)
                    {
                        uv.LinkCV = correctLink;
                        updated++;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Đã cập nhật {updated} link CV mới.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Có lỗi: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UploadCvTheoMaUngVien(List<IFormFile> cvFiles)
        {
            if (cvFiles == null || cvFiles.Count == 0)
            {
                TempData["ErrorMessage"] = "❌ Vui lòng chọn file để tải lên.";
                return RedirectToAction("Index");
            }

            var danhSachUngVien = await _context.UngViens.ToListAsync();
            var pathCv = Path.Combine(_env.WebRootPath, "cv");
            if (!Directory.Exists(pathCv)) Directory.CreateDirectory(pathCv);

            int demThanhCong = 0;
            int demKhongTimThay = 0;
            int daCoCv = 0;

            foreach (var file in cvFiles)
            {
                var tenFileGoc = Path.GetFileNameWithoutExtension(file.FileName).Trim();
                var ungVien = danhSachUngVien.FirstOrDefault(u =>
                    u.MaUngVien.Equals(tenFileGoc, StringComparison.OrdinalIgnoreCase));

                if (ungVien == null)
                {
                    demKhongTimThay++;
                    continue;
                }

                if (!string.IsNullOrEmpty(ungVien.LinkCV))
                {
                    daCoCv++;
                    continue;
                }

                var ext = Path.GetExtension(file.FileName);
                var tenMoi = $"{ungVien.MaUngVien}_{Guid.NewGuid().ToString().Substring(0, 5)}{ext}";
                var filePath = Path.Combine(pathCv, tenMoi);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                ungVien.LinkCV = $"/cv/{tenMoi}";
                demThanhCong++;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ {demThanhCong} CV đã được cập nhật.";
            if (demKhongTimThay > 0)
                TempData["ErrorMessage"] = $"⚠️ Có {demKhongTimThay} file không khớp mã ứng viên.";
            if (daCoCv > 0)
                TempData["WarningMessage"] = $"🔁 Bỏ qua {daCoCv} ứng viên đã có CV.";

            return RedirectToAction("Index");
        }

    }
}
