using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLHoSoTuyenDung.Models.Enums;
using Nhom6_QLHoSoTuyenDung.Services.Interfaces;

namespace Nhom6_QLHoSoTuyenDung.Controllers
{
    [Authorize(Roles = $"{RoleNames.HR},{RoleNames.Admin}")]
    public class HRPhongVanController : Controller
    {
        private readonly ILichPhongVanService _lichService;
        private readonly AppDbContext _context;

        public HRPhongVanController(ILichPhongVanService lichService, AppDbContext context)
        {
            _lichService = lichService;
            _context = context;
        }

        public async Task<IActionResult> TrangThaiCho()
        {
            var danhSach = await _lichService.GetTrangThaiChoHRAsync();
            ViewBag.ViTriList = await _context.ViTriTuyenDungs.ToListAsync();
            return View("TrangThaiChoHR", danhSach);
        }
    }
}
