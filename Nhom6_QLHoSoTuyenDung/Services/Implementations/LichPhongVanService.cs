using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Entities;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.NguoiPhongVanVM;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.PhongVanVM;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;

public class LichPhongVanService : ILichPhongVanService
{
    private readonly AppDbContext _context;
    private readonly ITaiKhoanService _taiKhoanService;

    public LichPhongVanService(AppDbContext context, ITaiKhoanService taiKhoanService)
    {
        _context = context;
        _taiKhoanService = taiKhoanService;
    }

    public async Task<LichPhongVan?> GetLichByUngVienIdAsync(string ungVienId)
    {
        return await _context.LichPhongVans
            .Include(l => l.PhongPhongVan)
            .Include(l => l.UngVien)
            .FirstOrDefaultAsync(l => l.UngVienId == ungVienId);
    }

    public async Task<TaoLichPhongVanVM?> GetFormDataAsync(string? ungVienId)
    {
        var phongList = await _context.PhongPhongVans
            .Select(p => new SelectListItem
            {
                Value = p.Id,
                Text = p.TenPhong + " - " + p.DiaDiem
            }).ToListAsync();

        var model = new TaoLichPhongVanVM
        {
            PhongList = phongList,
            NguoiPhongVanOptions = new List<SelectListItem>()
        };

        if (!string.IsNullOrEmpty(ungVienId))
        {
            var ungVien = await _context.UngViens
                .Include(u => u.ViTriUngTuyen)
                .FirstOrDefaultAsync(u => u.MaUngVien == ungVienId);

            if (ungVien != null)
            {
                model.UngVienId = ungVien.MaUngVien;
                model.TenUngVien = ungVien.HoTen;
                model.ViTriId = ungVien.ViTriUngTuyenId;
                model.TenViTri = ungVien.ViTriUngTuyen?.TenViTri;
            }
        }

        return model;
    }

    public async Task<(bool, string)> CreateLichAsync(CreateLichPhongVanVM vm, bool isReschedule = false)

    {
        try
        {
            var ungVien = await _context.UngViens.FirstOrDefaultAsync(u => u.MaUngVien == vm.UngVienId);
            if (ungVien == null)
                return (false, "Ứng viên không tồn tại.");

            var isVong2 = ungVien.TrangThai == TrangThaiUngVienEnum.CanPhongVanLan2.ToString();


            if (!vm.ThoiGian.HasValue)
            return (false, "Vui lòng chọn thời gian hợp lệ.");
        if (vm.ThoiGian < DateTime.Now)
            return (false, "Không thể tạo lịch với thời gian trong quá khứ.");

        var viTriId = ungVien.ViTriUngTuyenId;
        var thoiGianPhongVan = vm.ThoiGian.Value;

        var lichTrungPhong = await _context.LichPhongVans
            .Where(l => l.PhongPhongVanId == vm.PhongPhongVanId && l.ThoiGian.HasValue)
            .ToListAsync();

        var clashPhong = lichTrungPhong.Any(l =>
            Math.Abs((l.ThoiGian!.Value - thoiGianPhongVan).TotalMinutes) < 30);
        if (clashPhong)
            return (false, "Phòng đã có lịch gần thời gian này.");

        var nhanVienIds = vm.NhanVienIds?.Distinct().ToList() ?? new();
        if (nhanVienIds.Any())
        {
            var lichTrungNV = await _context.LichPhongVans
                .Include(l => l.NhanVienThamGiaPVs)
                .Where(l => l.ThoiGian.HasValue)
                .ToListAsync();

            var clash = lichTrungNV.Any(l =>
                Math.Abs((l.ThoiGian!.Value - thoiGianPhongVan).TotalMinutes) < 30 &&
                l.NhanVienThamGiaPVs.Any(nv => nhanVienIds.Contains(nv.NhanVienId)));

            if (clash)
                return (false, "Một người phỏng vấn đã có lịch gần thời gian này.");
        }

        // ✅ Dùng Guid thực sự làm Id
        var idLichMoi = Guid.NewGuid().ToString();

        var lichEntity = new LichPhongVan
        {
            Id = idLichMoi,
            UngVienId = vm.UngVienId,
            ViTriId = viTriId,
            PhongPhongVanId = vm.PhongPhongVanId,
            ThoiGian = vm.ThoiGian,
            TrangThai = TrangThaiPhongVanEnum.DaLenLich.ToString()
        };

        await _context.LichPhongVans.AddAsync(lichEntity);

        var nguoiPV = nhanVienIds.Select(id => new NhanVienThamGiaPhongVan
        {
            Id = Guid.NewGuid().ToString(),
            NhanVienId = id,
            LichPhongVanId = idLichMoi
        }).ToList();

        await _context.NhanVienThamGiaPhongVans.AddRangeAsync(nguoiPV);
        await _context.SaveChangesAsync();

        // Gửi mail
        var uvInfo = await _context.UngViens
            .Where(u => u.MaUngVien == vm.UngVienId)
            .Select(u => new { u.HoTen, u.Email })
            .FirstOrDefaultAsync();

        var viTriInfo = await _context.ViTriTuyenDungs
            .Where(v => v.MaViTri == viTriId)
            .Select(v => v.TenViTri)
            .FirstOrDefaultAsync();

        var phongInfo = await _context.PhongPhongVans
            .Where(p => p.Id == vm.PhongPhongVanId)
            .Select(p => new { p.TenPhong, p.DiaDiem })
            .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(uvInfo?.Email))
            {
                var email = uvInfo.Email;

                if (isReschedule)
                {
                    var subject = "🔁 Lịch phỏng vấn mới được cập nhật";
                    var body = $"Thân gửi {uvInfo.HoTen},\n\n" +
                               $"Lịch phỏng vấn mới đã được sắp xếp lại cho bạn do lịch trước đó đã bị hủy.\n\n" +
                               $"🔁 Thông tin lịch mới:\n" +
                               $"- Vị trí: {viTriInfo}\n" +
                               $"- Thời gian: {thoiGianPhongVan:HH:mm, dd/MM/yyyy}\n" +
                               $"- Địa điểm: {phongInfo?.TenPhong} - {phongInfo?.DiaDiem}\n\n" +
                               $"Vui lòng kiểm tra email và có mặt đúng giờ để buổi phỏng vấn diễn ra thuận lợi.\n\n" +
                               $"Trân trọng,\nPhòng Tuyển dụng";

                    await _taiKhoanService.SendEmailAsync(email, subject, body);
                }
                else if (isVong2)
                {
                    var subject = "📢 Lịch phỏng vấn vòng 2";
                    var body = $"Thân gửi {uvInfo.HoTen},\n\n" +
                               $"Bạn đã được xếp lịch phỏng vấn vòng 2 cho vị trí: {viTriInfo}.\n\n" +
                               $"🕒 Thời gian: {thoiGianPhongVan:HH:mm, dd/MM/yyyy}\n" +
                               $"🏢 Địa điểm: {phongInfo?.TenPhong} - {phongInfo?.DiaDiem}\n\n" +
                               $"Vui lòng kiểm tra email và chuẩn bị kỹ càng cho vòng phỏng vấn tiếp theo.\n\n" +
                               $"Trân trọng,\nPhòng Tuyển dụng";

                    await _taiKhoanService.SendEmailAsync(email, subject, body);
                }
                else
                {
                    var subject = "📅 Thông báo lịch phỏng vấn";
                    var body = $"Thân gửi {uvInfo.HoTen},\n\n" +
                               $"Bạn đã được sắp xếp lịch phỏng vấn cho vị trí: {viTriInfo}.\n\n" +
                               $"🕒 Thời gian: {thoiGianPhongVan:HH:mm, dd/MM/yyyy}\n" +
                               $"🏢 Địa điểm: {phongInfo?.TenPhong} - {phongInfo?.DiaDiem}\n\n" +
                               $"Vui lòng có mặt đúng giờ và chuẩn bị sẵn các giấy tờ cần thiết.\n\n" +
                               $"Trân trọng,\nPhòng Tuyển dụng";

                    await _taiKhoanService.SendEmailAsync(email, subject, body);
                }
            }
            return (true, "Đã tạo lịch phỏng vấn thành công!");
    }catch (Exception ex)
{
            // Ghi log nếu cần
            return (false, "Đã có lỗi xảy ra khi tạo lịch: " + ex.Message);
        }
    }


    public async Task<PhongVanDashboardVM> GetDashboardAsync(string? keyword = null, string? trangThai = null, string? viTriId = null, string? phongId = null, DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        var query = _context.LichPhongVans
            .Include(l => l.UngVien)
            .Include(l => l.ViTriTuyenDung)
            .Include(l => l.PhongPhongVan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(l => (l.UngVien != null && l.UngVien.HoTen.ToLower().Contains(kw)) ||
                                     (l.ViTriTuyenDung != null && l.ViTriTuyenDung.TenViTri.ToLower().Contains(kw)) ||
                                     (l.PhongPhongVan != null && l.PhongPhongVan.TenPhong.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            query = query.Where(l => l.TrangThai == trangThai);
        }

        if (!string.IsNullOrWhiteSpace(viTriId))
        {
            query = query.Where(l => l.ViTriId == viTriId);
        }

        if (!string.IsNullOrWhiteSpace(phongId))
        {
            query = query.Where(l => l.PhongPhongVanId == phongId);
        }

        if (tuNgay.HasValue)
        {
            query = query.Where(l => l.ThoiGian >= tuNgay.Value.Date);
        }

        if (denNgay.HasValue)
        {
            query = query.Where(l => l.ThoiGian <= denNgay.Value.Date.AddDays(1).AddTicks(-1));
        }

        var lich = await query.ToListAsync();

        var model = new PhongVanDashboardVM
        {
            TongSoLich = lich.Count,
            DaPhongVan = lich.Count(l => l.TrangThai == TrangThaiPhongVanEnum.HoanThanh.ToString()),
            ChuaPhongVan = lich.Count(l => l.TrangThai != TrangThaiPhongVanEnum.HoanThanh.ToString()),
            DanhSachLich = lich
        };

        // ✅ Biểu đồ CỘT ĐỨNG: Vị trí tuyển dụng từ CSDL
        var viTriGroup = lich
            .Where(l => l.ViTriTuyenDung != null)
            .GroupBy(l => l.ViTriTuyenDung.TenViTri)
            .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong)
            .ToList();

        model.ViTriLabels = viTriGroup.Select(x => x.Ten).ToList();
        model.ViTriCounts = viTriGroup.Select(x => x.SoLuong).ToList();

        // ✅ Biểu đồ TRÒN: Trạng thái phỏng vấn (Chuẩn hóa tiếng Việt)
        var trangThaiGroup = lich
            .GroupBy(l => l.TrangThai)
            .Select(g => new {
                Ten = g.Key == TrangThaiPhongVanEnum.DaLenLich.ToString() ? "Đã lên lịch" :
                      g.Key == TrangThaiPhongVanEnum.HoanThanh.ToString() ? "Hoàn thành" :
                      g.Key == TrangThaiPhongVanEnum.Huy.ToString() ? "Hủy" : (g.Key ?? "Chưa rõ"),
                SoLuong = g.Count()
            })
            .OrderByDescending(x => x.SoLuong)
            .ToList();

        model.TrangThaiLabels = trangThaiGroup.Select(x => x.Ten).ToList();
        model.TrangThaiValues = trangThaiGroup.Select(x => x.SoLuong).ToList();

        model.LichPhongVanSapToi = await GetLichPhongVanSapToiAsync();
        return model;
    }
    public async Task<List<LichPhongVanSapToiVM>> GetLichPhongVanSapToiAsync()
    {
        var lich = await _context.LichPhongVans
            .Include(l => l.UngVien)
            .Include(l => l.ViTriTuyenDung)
            .Where(l => l.ThoiGian.HasValue
                && l.ThoiGian > DateTime.Now
                && (l.TrangThai == TrangThaiPhongVanEnum.DaLenLich.ToString())) // ✅ loại bỏ HoanThanh, Huy
            .OrderBy(l => l.ThoiGian)
            .Take(10)
            .ToListAsync();

        return lich.Select(l => new LichPhongVanSapToiVM
        {
            HoTen = l.UngVien?.HoTen,
            ViTri = l.ViTriTuyenDung?.TenViTri,
            Gio = l.ThoiGian!.Value.ToString("HH:mm"),
            Ngay = l.ThoiGian.Value.ToString("dd/MM")
        }).ToList();
    }



    public async Task<List<UngVien>> GetUngViensChuaCoLichAsync()
    {
        var daCoLich = await _context.LichPhongVans
            .Select(l => l.UngVienId)
            .ToListAsync();

        var chuaCoLich = await _context.UngViens
            .Where(u => !daCoLich.Contains(u.MaUngVien))
            .ToListAsync();

        return chuaCoLich;
    }

    public async Task<List<DaPhongVanVM>> GetTrangThaiChoHRAsync()
    {
        var lichHopLe = await _context.LichPhongVans
            .Include(l => l.UngVien)
            .Include(l => l.ViTriTuyenDung)
            .Include(l => l.DanhGiaPhongVans)
            .Where(l =>
                l.UngVien.TrangThai == TrangThaiUngVienEnum.CanPhongVanLan2.ToString() &&
                l.TrangThai == TrangThaiPhongVanEnum.HoanThanh.ToString())
            .ToListAsync();

        var result = lichHopLe.Select(lich => new DaPhongVanVM
        {
            LichId = lich.Id,
            TenUngVien = lich.UngVien?.HoTen ?? "Không rõ",
            UngVienId = lich.UngVien?.MaUngVien ?? "",
            Email = lich.UngVien?.Email ?? "",
            ViTri = lich.ViTriTuyenDung?.TenViTri ?? "",
            ThoiGian = lich.ThoiGian ?? DateTime.Now,
            LinkCV = lich.UngVien?.LinkCV,
            DiemTB = lich.DanhGiaPhongVans.FirstOrDefault()?.DiemDanhGia,
            NhanXet = lich.DanhGiaPhongVans.FirstOrDefault()?.NhanXet
        }).ToList();

        return result;
    }


    public async Task<List<DaPhongVanVM>> GetUngViensBiHuyLichAsync()
    {
        // B1: Lấy danh sách ứng viên đã có lịch mới
        var ungViensDaCoLichMoi = await _context.LichPhongVans
            .Where(l => l.TrangThai == TrangThaiPhongVanEnum.DaLenLich.ToString())
            .Select(l => l.UngVienId)
            .Distinct()
            .ToListAsync();

        // B2: Lấy lịch bị hủy nhưng ứng viên đó chưa có lịch mới
        var lichBiHuy = await _context.LichPhongVans
            .Include(l => l.UngVien)
            .Include(l => l.ViTriTuyenDung)
            .Where(l => l.TrangThai == TrangThaiPhongVanEnum.Huy.ToString()
                && !ungViensDaCoLichMoi.Contains(l.UngVienId))
            .ToListAsync();

        // B3: Trả về danh sách ViewModel
        return lichBiHuy.Select(l => new DaPhongVanVM
        {
            LichId = l.Id,
            TenUngVien = l.UngVien?.HoTen ?? "",
            UngVienId = l.UngVienId,
            Email = l.UngVien?.Email ?? "",
            ViTri = l.ViTriTuyenDung?.TenViTri ?? "",
            ThoiGian = l.ThoiGian ?? DateTime.Now,
            LinkCV = l.UngVien?.LinkCV,
            DiemTB = null,
            NhanXet = l.GhiChu
        }).ToList();
    }


}
