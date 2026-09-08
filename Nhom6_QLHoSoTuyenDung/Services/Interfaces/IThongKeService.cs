using Nhom6_QLHoSoTuyenDung.Models.ViewModels.ThongKe;

namespace Nhom6_QLHoSoTuyenDung.Services.Interfaces
{
    public interface IThongKeService
    {
        Task<ThongKeTongHopVM> GetTongQuanAsync(string? tuKhoa, string? loai, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null);

        Task<List<BieuDoItemVM>> GetBieuDoTheoTrangThaiUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null);

        Task<List<BieuDoItemVM>> GetBieuDoNguonUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null);

        Task<List<BieuDoItemVM>> GetBieuDoTheoViTriUngTuyenAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId);

        Task<List<BieuDoItemVM>> GetBieuDoTheoPhongBanAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId);

        Task<List<BieuDoItemVM>> GetBieuDoDanhGiaUngVienAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null);

        Task<List<BieuDoItemVM>> GetXuHuongTheoThangAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId);
        
        Task<XuHuongTuyenDungVM> GetXuHuongTuyenDung12ThangAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? viTriId, string? phongBanId);

        Task<List<ViTriThanhCongVM>> GetViTriTuyenThanhCongAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null);
        Task<List<BaoCaoDayDuVM>> XuatBaoCaoDayDuAsync(BaoCaoRequestVM request);

    }
}
