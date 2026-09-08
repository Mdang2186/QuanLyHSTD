using Microsoft.AspNetCore.Mvc;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;

namespace Nhom6_QLHoSoTuyenDung.Components
{
    public class ViTriThanhCongViewComponent : ViewComponent
    {
        private readonly IThongKeService _thongKeService;

        public ViTriThanhCongViewComponent(IThongKeService thongKeService)
        {
            _thongKeService = thongKeService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, string? trangThai = null, string? viTriId = null, string? phongBanId = null)
        {
            var danhSach = await _thongKeService.GetViTriTuyenThanhCongAsync(tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId);
            return View(danhSach);
        }

    }
}
