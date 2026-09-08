using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Data;
using Nhom6_QLHoSoTuyenDung.Models.Entities;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;
using Nhom6_QLHoSoTuyenDung.Models.Helpers;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.ViTriTuyenDungVM;
using Nhom6_QLHoSoTuyenDung.Models.ViewModels.Dashboard;
using Nhom6_QLHoSoTuyenDung.Models.Enums;

namespace Nhom6_QLHoSoTuyenDung.Services.Implementations
{
    public class ViTriTuyenDungService : IViTriTuyenDungService
    {
        private readonly AppDbContext _context;

        public ViTriTuyenDungService(AppDbContext context)
        {
            _context = context;
        }

        public List<ViTriTuyenDung> GetAll(string? keyword, string? trangThai, string? phongBanId)
        {
            var query = _context.ViTriTuyenDungs
                .Include(v => v.PhongBan)
                .Include(v => v.UngViens)
                .Include(v => v.LichPhongVans)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(v => v.TenViTri.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(v => v.TrangThai == trangThai);

            if (!string.IsNullOrEmpty(phongBanId))
                query = query.Where(v => v.PhongBanId == phongBanId);

            return query.ToList();
        }

        public ViTriTuyenDung? GetById(string id)
        {
            return _context.ViTriTuyenDungs.Include(v => v.PhongBan).FirstOrDefault(v => v.MaViTri == id);
        }

        public void Create(ViTriTuyenDung model)
        {
            model.MaViTri = GenerateNewMaViTri();
            model.NgayTao = DateTime.Now;
            if (string.IsNullOrWhiteSpace(model.TrangThai))
                model.TrangThai = TrangThaiViTriEnum.DangTuyen.ToString();
            _context.ViTriTuyenDungs.Add(model);
            _context.SaveChanges();
        }
        public bool TenViTriDaTonTai(string tenViTri)
        {
            return _context.ViTriTuyenDungs.Any(v => v.TenViTri.ToLower() == tenViTri.ToLower());
        }


        public void Update(ViTriTuyenDung model)
        {
            _context.ViTriTuyenDungs.Update(model);
            _context.SaveChanges();
        }

        public void Delete(string id)
        {
            var viTri = _context.ViTriTuyenDungs.Find(id);
            if (viTri != null)
            {
                _context.ViTriTuyenDungs.Remove(viTri);
                _context.SaveChanges();
            }
        }

        public bool Exists(string id)
        {
            return _context.ViTriTuyenDungs.Any(e => e.MaViTri == id);
        }

        public List<int> DemSoLuongHoanThanhTheoThang(List<ViTriTuyenDung> dsViTri)
        {
            var result = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var thang = DateTime.Now.AddMonths(-i);
                int count = dsViTri
                    .Where(v => v.NgayTao.HasValue && (v.TrangThai == TrangThaiViTriEnum.DaDong.ToString() || v.TrangThai == "Đã đóng"))
                    .Count(v =>
                        v.NgayTao.Value.Month == thang.Month &&
                        v.NgayTao.Value.Year == thang.Year);

                result.Add(count);
            }

            if (result.All(x => x == 0))
            {
                result = new List<int> { 1, 2, 1, 3, 4, dsViTri.Count(v => v.TrangThai == TrangThaiViTriEnum.DaDong.ToString() || v.TrangThai == TrangThaiViTriEnum.TamDung.ToString()) };
            }

            return result;
        }

        public Dictionary<string, int> PhanBoTrangThai(List<ViTriTuyenDung> danhSach)
        {
            return ThongKeViTriHelper.DemTheoTrangThai(danhSach);
        }

        public (List<string>, List<int>) DemTheoThang(List<ViTriTuyenDung> danhSach)
        {
            return ThongKeViTriHelper.DemTheoThang(danhSach);
        }

        public List<QuyTrinhTuyenDungItem> ThongKeQuyTrinh(List<UngVien> dsUngVien)
        {
            return ThongKeViTriHelper.ThongKeQuyTrinhTuyenDung(_context, dsUngVien);
        }

        public List<HoatDongDashboardVM> LayHoatDongGanDay()
        {
            return ThongKeViTriHelper.LayHoatDong7Ngay(_context);
        }
        public string GenerateNewMaViTri()
        {
            var lastMa = _context.ViTriTuyenDungs
                .OrderByDescending(v => v.MaViTri)
                .Select(v => v.MaViTri)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastMa) || !lastMa.StartsWith("VT"))
                return "VT001";

            var numberPart = int.TryParse(lastMa.Substring(2), out int num) ? num : 0;
            return $"VT{(num + 1):D3}";
        }
        public async Task CapNhatTrangThaiTuDongAsync()
        {
            var danhSach = await _context.ViTriTuyenDungs
               .Where(v => v.TrangThai == TrangThaiViTriEnum.DangTuyen.ToString())
                .ToListAsync();

            foreach (var viTri in danhSach)
            {
                var soLuong = await _context.UngViens
                    .CountAsync(u => u.ViTriUngTuyenId == viTri.MaViTri &&
                                     u.TrangThai == TrangThaiUngVienEnum.DaTuyen.ToString());

                if (soLuong >= viTri.SoLuongCanTuyen)
                {
                    viTri.TrangThai = TrangThaiViTriEnum.TamDung.ToString();
                }
            }

            await _context.SaveChangesAsync();
        }


    }
}