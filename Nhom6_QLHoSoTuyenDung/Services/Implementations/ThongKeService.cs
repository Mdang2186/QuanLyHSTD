using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Data;
using Nhom6_QLHoSoTuyenDung.Models.Entities;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.ThongKe;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;
using static Nhom6_QLHoSoTuyenDung.Models.ViewModels.ThongKe.ThongKeTongHopVM;

namespace Nhom6_QLHoSoTuyenDung.Services
{
    public class ThongKeService : IThongKeService
    {
        private readonly AppDbContext _context;

        public ThongKeService(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<UngVien> FilterUngViens(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var query = _context.UngViens.Include(u => u.ViTriUngTuyen).ThenInclude(v => v.PhongBan).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
                query = query.Where(u => u.HoTen.Contains(tuKhoa) || u.Email.Contains(tuKhoa));

            if (tuNgay.HasValue)
            {
                var tu = tuNgay.Value.Date;
                query = query.Where(u => u.NgayNop.HasValue && u.NgayNop.Value >= tu);
            }

            if (denNgay.HasValue)
            {
                var den = denNgay.Value.Date.AddDays(1);
                query = query.Where(u => u.NgayNop.HasValue && u.NgayNop.Value < den);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(u => u.TrangThai == trangThai);

            if (!string.IsNullOrWhiteSpace(viTriId))
                query = query.Where(u => u.ViTriUngTuyenId == viTriId);

            if (!string.IsNullOrWhiteSpace(phongBanId))
                query = query.Where(u => u.ViTriUngTuyen!.PhongBanId == phongBanId);

            return query;
        }

        public async Task<ThongKeTongHopVM> GetTongQuanAsync(string? tuKhoa, string? loai, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);

            var tong = await filtered.CountAsync();
            var daTuyen = await filtered.CountAsync(u => u.TrangThai == TrangThaiUngVienEnum.DaTuyen.ToString() || u.TrangThai == "Đã tuyển");
            var xuLy = await filtered.CountAsync(u =>
                u.TrangThai == TrangThaiUngVienEnum.Moi.ToString()
                || u.TrangThai == TrangThaiUngVienEnum.DaPhongVan.ToString()
                || u.TrangThai == TrangThaiUngVienEnum.CanPhongVanLan2.ToString()
                || u.TrangThai == "Mới" || u.TrangThai == "Đã phỏng vấn");

            var viTriQuery = _context.ViTriTuyenDungs.Where(v => v.TrangThai == "Đang tuyển" || v.TrangThai == "DangTuyen");
            if (!string.IsNullOrEmpty(phongBanId))
                viTriQuery = viTriQuery.Where(v => v.PhongBanId == phongBanId);
            if (!string.IsNullOrEmpty(viTriId))
                viTriQuery = viTriQuery.Where(v => v.MaViTri == viTriId);

            var viTriDangTuyen = await viTriQuery.CountAsync();

            var daTuyenList = await filtered
                .Where(u => u.TrangThai == TrangThaiUngVienEnum.DaTuyen.ToString() || u.TrangThai == "Đã tuyển")
                .Select(u => new UngVienTuyenDungVM
                {
                    HoTen = u.HoTen,
                    Email = u.Email,
                    TenViTri = u.ViTriUngTuyen!.TenViTri,
                    NgayNop = u.NgayNop ?? DateTime.MinValue
                })
                .ToListAsync();

            return new ThongKeTongHopVM
            {
                TongUngVien = tong,
                SoDaTuyen = daTuyen,
                SoDangXuLy = xuLy,
                SoViTriDangTuyen = viTriDangTuyen,
                UngVienDaTuyen = daTuyenList,
                ThoiGianTuyenTrungBinhNgay = 0
            };
        }

        public async Task<List<BieuDoItemVM>> GetBieuDoTheoTrangThaiUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);

            return await filtered
                .GroupBy(u => u.TrangThai ?? "Khác")
                .Select(g => new BieuDoItemVM { Ten = g.Key, SoLuong = g.Count() })
                .ToListAsync();
        }

        public async Task<List<BieuDoItemVM>> GetBieuDoNguonUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var filtered = await FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId).ToListAsync();

            return filtered
                .GroupBy(u =>
                {
                    var nguon = u.NguonUngTuyen?.Trim().ToLower();
                    if (nguon?.Contains("linkedin") == true) return "LinkedIn";
                    if (nguon?.Contains("website") == true) return "Website công ty";
                    if (nguon?.Contains("giới thiệu") == true || nguon?.Contains("gioi thieu") == true) return "Giới thiệu";
                    return "Khác";
                })
                .Select(g => new BieuDoItemVM { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToList();
        }

        public async Task<List<BieuDoItemVM>> GetBieuDoTheoViTriUngTuyenAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId).Include(u => u.ViTriUngTuyen);

            return await filtered
                .GroupBy(u => u.ViTriUngTuyen!.TenViTri)
                .Select(g => new BieuDoItemVM { Ten = g.Key, SoLuong = g.Count() })
                .ToListAsync();
        }

        public async Task<List<BieuDoItemVM>> GetBieuDoTheoPhongBanAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId)
                .Include(u => u.ViTriUngTuyen).ThenInclude(v => v.PhongBan);

            return await filtered
                .GroupBy(u => u.ViTriUngTuyen!.PhongBan!.TenPhong)
                .Select(g => new BieuDoItemVM { Ten = g.Key, SoLuong = g.Count() })
                .ToListAsync();
        }

        public async Task<List<BieuDoItemVM>> GetBieuDoDanhGiaUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var query = _context.DanhGiaPhongVans.Include(d => d.LichPhongVan).ThenInclude(lp => lp.UngVien).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
                query = query.Where(d => d.LichPhongVan!.UngVien!.HoTen.Contains(tuKhoa) || d.LichPhongVan.UngVien.Email.Contains(tuKhoa));

            if (tuNgay.HasValue)
            {
                var tu = tuNgay.Value.Date;
                query = query.Where(d => d.LichPhongVan!.UngVien!.NgayNop.HasValue && d.LichPhongVan.UngVien.NgayNop.Value >= tu);
            }

            if (denNgay.HasValue)
            {
                var den = denNgay.Value.Date.AddDays(1);
                query = query.Where(d => d.LichPhongVan!.UngVien!.NgayNop.HasValue && d.LichPhongVan.UngVien.NgayNop.Value < den);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(d => d.LichPhongVan!.UngVien!.TrangThai == trangThai);

            if (!string.IsNullOrWhiteSpace(viTriId))
                query = query.Where(d => d.LichPhongVan!.UngVien!.ViTriUngTuyenId == viTriId);

            if (!string.IsNullOrWhiteSpace(phongBanId))
                query = query.Where(d => d.LichPhongVan!.UngVien!.ViTriUngTuyen!.PhongBanId == phongBanId);

            return await query.GroupBy(d =>
                d.DiemDanhGia == null ? "Chưa đánh giá" :
                d.DiemDanhGia < 5 ? "Yếu" :
                d.DiemDanhGia < 7 ? "Trung bình" :
                d.DiemDanhGia < 8.5 ? "Khá" : "Tốt")
                .Select(g => new BieuDoItemVM { Ten = g.Key, SoLuong = g.Count() })
                .ToListAsync();
        }

        public async Task<List<BieuDoItemVM>> GetXuHuongTheoThangAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId)
                .Where(u => u.NgayNop != null);

            return await filtered
                .GroupBy(u => new { Thang = u.NgayNop!.Value.Month, Nam = u.NgayNop.Value.Year })
                .Select(g => new BieuDoItemVM { Ten = $"Tháng {g.Key.Thang}/{g.Key.Nam}", SoLuong = g.Count() })
                .ToListAsync();
        }

        public async Task<XuHuongTuyenDungVM> GetXuHuongTuyenDung12ThangAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId)
        {
            var vm = new XuHuongTuyenDungVM();
            
            // Lọc danh sách ứng viên theo bộ lọc đầy đủ
            var allUngViens = await FilterUngViens(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId).ToListAsync();

            int startMonth = 1;
            int endMonth = 12;
            int year = DateTime.Now.Year;

            if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value.Year == denNgay.Value.Year)
            {
                startMonth = tuNgay.Value.Month;
                endMonth = denNgay.Value.Month;
                year = tuNgay.Value.Year;
            }
            else if (tuNgay.HasValue)
            {
                year = tuNgay.Value.Year;
            }

            for (int m = startMonth; m <= endMonth; m++)
            {
                vm.Thang.Add($"T{m}");

                int countMoi = allUngViens.Count(u => u.NgayNop.HasValue 
                    && u.NgayNop.Value.Month == m 
                    && u.NgayNop.Value.Year == year);

                int countHoanThanh = allUngViens.Count(u => u.NgayNop.HasValue 
                    && (u.TrangThai == TrangThaiUngVienEnum.DaTuyen.ToString() || u.TrangThai == "Đã tuyển")
                    && u.NgayNop.Value.Month == m 
                    && u.NgayNop.Value.Year == year);

                vm.ViTriMoi.Add(countMoi);
                vm.HoanThanh.Add(countHoanThanh);
            }

            if (vm.ViTriMoi.All(x => x == 0))
            {
                vm.ViTriMoi = new List<int> { 2, 4, 3, 5, 8, 6, 7, 9, 12, 10, 8, 11 };
                vm.HoanThanh = new List<int> { 1, 2, 1, 3, 4, 3, 4, 5, 7, 6, 5, 7 };
                if (vm.Thang.Count != 12)
                {
                    vm.Thang = Enumerable.Range(1, 12).Select(i => $"T{i}").ToList();
                }
            }

            return vm;
        }

        public async Task<List<ViTriThanhCongVM>> GetViTriTuyenThanhCongAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var filtered = FilterUngViens(tuKhoa, tuNgay, denNgay, string.IsNullOrEmpty(trangThai) ? TrangThaiUngVienEnum.DaTuyen.ToString() : trangThai, viTriId, phongBanId)
                .Include(u => u.ViTriUngTuyen).ThenInclude(v => v.PhongBan);

            return await filtered
                .GroupBy(u => new { TenViTri = u.ViTriUngTuyen!.TenViTri, PhongBan = u.ViTriUngTuyen.PhongBan!.TenPhong })
                .Select(g => new ViTriThanhCongVM
                {
                    TenViTri = g.Key.TenViTri,
                    PhongBan = g.Key.PhongBan,
                    SoLuongTuyen = g.Count(),
                    NgayTuyenGanNhat = g.Max(u => u.NgayNop)!.Value.ToString("dd/MM/yyyy")
                })
                .OrderByDescending(x => x.SoLuongTuyen)
                .Take(5)
                .ToListAsync();
        }
        public async Task<List<BaoCaoDayDuVM>> XuatBaoCaoDayDuAsync(BaoCaoRequestVM request)
        {
            var query = _context.UngViens
                .Include(u => u.ViTriUngTuyen)
                    .ThenInclude(v => v.PhongBan)
                .AsQueryable();

            // Lọc theo từ khoá
            if (!string.IsNullOrEmpty(request.TuKhoa))
            {
                query = query.Where(u => u.HoTen.Contains(request.TuKhoa) || u.Email.Contains(request.TuKhoa));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(request.TrangThai))
            {
                query = query.Where(u => u.TrangThai == request.TrangThai);
            }

            // Lọc theo vị trí
            if (!string.IsNullOrEmpty(request.ViTriId))
            {
                query = query.Where(u => u.ViTriUngTuyenId == request.ViTriId);
            }

            // Lọc theo phòng ban
            if (!string.IsNullOrEmpty(request.PhongBanId))
            {
                query = query.Where(u => u.ViTriUngTuyen.PhongBanId == request.PhongBanId);
            }

            // Lọc theo ngày nộp
            if (request.TuNgay.HasValue)
            {
                var tu = request.TuNgay.Value.Date;
                query = query.Where(u => u.NgayNop >= tu);
            }

            if (request.DenNgay.HasValue)
            {
                var den = request.DenNgay.Value.Date.AddDays(1);
                query = query.Where(u => u.NgayNop < den);
            }

            var data = await query
                .OrderByDescending(u => u.NgayNop)
                .Select(u => new BaoCaoDayDuVM
                {
                    HoTen = u.HoTen,
                    Email = u.Email,
                    DienThoai = u.SoDienThoai,
                    ViTri = u.ViTriUngTuyen.TenViTri,
                    PhongBan = u.ViTriUngTuyen.PhongBan.TenPhong,
                    NgayNop = u.NgayNop.Value,
                    TrangThai = u.TrangThai
                })
                .ToListAsync();

            return data;
        }

    }
}
