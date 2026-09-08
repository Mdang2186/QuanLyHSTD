using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.Dashboard;
using Nhom6_QLHoSoTuyenDung.Services;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nhom6_QLHoSoTuyenDung.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHoatDongService _hoatDongService;

        public HomeController(AppDbContext context, IHoatDongService hoatDongService)
        {
            _context = context;
            _hoatDongService = hoatDongService;
        }

        public IActionResult Index(string search, string status, string source, DateTime? startDate, DateTime? endDate)
        {
            var vm = new UngVienDashboardVM();
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(((int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek) - 1));




            var baseQuery = _context.UngViens.Include(x => x.ViTriUngTuyen).AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(search))
                baseQuery = baseQuery.Where(u => u.HoTen.Contains(search) || u.Email.Contains(search));

            // Lọc theo nguồn ứng tuyển
            if (!string.IsNullOrEmpty(source))
                baseQuery = baseQuery.Where(u => u.NguonUngTuyen == source);

            // Lọc theo thời gian
            if (startDate.HasValue)
                baseQuery = baseQuery.Where(u => u.NgayNop >= startDate.Value.Date);
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(u => u.NgayNop <= endOfDay);
            }

            // Hiệu quả: tỉ lệ đã tuyển / tổng
            var tongHoSo = baseQuery.Count();
            var daTuyen = baseQuery.Count(u => u.TrangThai == TrangThaiUngVienEnum.DaTuyen.ToString());
            vm.HieuQua = tongHoSo > 0 ? Math.Round(daTuyen * 100.0 / tongHoSo, 1) : 0;

            // Lọc theo trạng thái từ display name
            var query = baseQuery;
            if (!string.IsNullOrEmpty(status) &&
     Enum.TryParse<TrangThaiUngVienEnum>(status, out var enumStatus))
            {
                query = query.Where(u => u.TrangThai == enumStatus.ToString());
            }


            // Tổng hồ sơ sau khi lọc trạng thái
            vm.TongHoSo = query.Count();

            // Ứng viên mới
            var ungVienMoiQuery = baseQuery.Where(u => u.TrangThai == TrangThaiUngVienEnum.Moi.ToString() && u.NgayNop.HasValue);
            
            if (startDate.HasValue)
                ungVienMoiQuery = ungVienMoiQuery.Where(u => u.NgayNop >= startDate.Value.Date);
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                ungVienMoiQuery = ungVienMoiQuery.Where(u => u.NgayNop <= endOfDay);
            }
            
            vm.UngVienMoi = ungVienMoiQuery.Count();

            // Lịch phỏng vấn đã hoàn thành
            var lichQuery = _context.LichPhongVans
                .Include(l => l.UngVien)
                .Where(l => l.TrangThai == TrangThaiPhongVanEnum.HoanThanh.ToString());

            if (startDate.HasValue)
                lichQuery = lichQuery.Where(l => l.ThoiGian.HasValue && l.ThoiGian.Value >= startDate.Value.Date);
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                lichQuery = lichQuery.Where(l => l.ThoiGian.HasValue && l.ThoiGian.Value <= endOfDay);
            }

            vm.SoPhongVan = lichQuery.Count();

            // Biểu đồ trạng thái (bar chart)
            vm.TrangThaiLabels = query.GroupBy(u => u.TrangThai).Select(g => g.Key ?? "Không xác định").ToList();
            vm.TrangThaiValues = query.GroupBy(u => u.TrangThai).Select(g => g.Count()).ToList();

            // Biểu đồ theo tháng (line chart)
            vm.NopHoSoData = Enumerable.Range(1, 12)
                .Select(m => query.Count(u => u.NgayNop.HasValue && u.NgayNop.Value.Month == m)).ToList();

            vm.PhongVanData = Enumerable.Range(1, 12)
                .Select(m => _context.LichPhongVans.Count(l => l.ThoiGian.HasValue && l.ThoiGian.Value.Month == m)).ToList();

            // Biểu đồ nguồn ứng viên (pie)
            var nguonGroup = query
                .GroupBy(u =>
                    u.NguonUngTuyen != null && u.NguonUngTuyen.ToLower().Contains("linkedin") ? "LinkedIn" :
                    u.NguonUngTuyen.ToLower().Contains("website") ? "Website công ty" :
                    u.NguonUngTuyen.ToLower().Contains("giới thiệu") || u.NguonUngTuyen.ToLower().Contains("bạn bè") ? "Giới thiệu" :
                    "Khác")
                .Select(g => new { Nguon = g.Key, Count = g.Count() })
                .ToList();

            vm.NguonLabels = nguonGroup.Select(g => g.Nguon).ToList();
            vm.NguonData = nguonGroup.Select(g => g.Count).ToList();

            // Ứng viên mới nhất (hiện tại là trong tuần)
            vm.UngVienMoiNhat = baseQuery
    .Where(u => u.NgayNop.HasValue &&
                u.TrangThai == TrangThaiUngVienEnum.Moi.ToString() &&
                u.NgayNop.Value >= startOfWeek)
                .OrderByDescending(u => u.NgayNop)
                .Take(10)
                .Select(u => new
                {
                    u.HoTen,
                    ViTri = u.ViTriUngTuyen.TenViTri,
                    u.TrangThai
                })
                .ToList<dynamic>();

            // Lịch phỏng vấn sắp tới
            vm.LichPhongVanSapToi = _context.LichPhongVans
                .Include(l => l.UngVien)
                .Include(l => l.ViTriTuyenDung)
                .Where(l => l.ThoiGian > DateTime.Now)
                .OrderBy(l => l.ThoiGian)
                .Take(3)
                .Select(l => new
                {
                    HoTen = l.UngVien.HoTen,
                    ViTri = l.ViTriTuyenDung.TenViTri,
                    Ngay = l.ThoiGian.Value.ToString("dd/MM"),
                    Gio = l.ThoiGian.Value.ToString("HH:mm")
                })
                .ToList<dynamic>();

            // Hoạt động gần đây
            var currentUser = User.Identity?.Name ?? "system";
            vm.HoatDongGanDay = _hoatDongService.GetHoatDongGanDay(currentUser, startOfWeek);

            return View("~/Views/Home/Index.cshtml", vm);
        }
    }
}
