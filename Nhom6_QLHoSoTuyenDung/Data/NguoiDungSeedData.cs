using Nhom6_QLHoSoTuyenDung.Models.Entities;

namespace Nhom6_QLHoSoTuyenDung.Data
{
    public static class NguoiDungSeedData
    {
        public static void Seed(AppDbContext context)
        {
            if (context.NguoiDungs.Any()) return;

            var ngayTao = new DateTime(2024, 1, 1);

            var nguoiDungs = new List<NguoiDung>
            {
                // Admin hệ thống
                new NguoiDung {
                    NhanVienId = "NV000",
                    TenDangNhap = "mdang2186",
                    MatKhau = "123456",
                    VaiTro = "Admin",
                    PhongBanId = "PBIT",
                    HoTen = "Đỗ Công Minh",
                    Email = "mdang2186@gmail.com",
                    SoDienThoai = "0372986808",
                    NgayTao = ngayTao
                },
                new NguoiDung {
                    NhanVienId = "NV001",
                    TenDangNhap = "admin",
                    MatKhau = "123456",
                    VaiTro = "Admin",
                    PhongBanId = "PBIT",
                    HoTen = "Đinh Thị Diễm Quỳnh",
                    Email = "diemquynhdinh1010@gmail.com",
                    SoDienThoai = "0329801388",
                    NgayTao = ngayTao
                },

                // HR phụ trách tuyển dụng
                new NguoiDung {
                    NhanVienId = "NV002",
                    TenDangNhap = "hr1",
                    MatKhau = "123456",
                    VaiTro = "HR",
                    PhongBanId = "PBNS",
                    HoTen = "Trần Thị Bích",
                    Email = "bich.ns@example.com",
                    SoDienThoai = "0901000002",
                    NgayTao = ngayTao
                },
                new NguoiDung {
                    NhanVienId = "NV010",
                    TenDangNhap = "hr2",
                    MatKhau = "123456",
                    VaiTro = "HR",
                    PhongBanId = "PBNS",
                    HoTen = "Lưu Thị Hằng",
                    Email = "hang.ns@example.com",
                    SoDienThoai = "0901000010",
                    NgayTao = ngayTao
                },

                // Người phỏng vấn
                new NguoiDung {
                    NhanVienId = "NV003",
                    TenDangNhap = "pv1",
                    MatKhau = "123456",
                    VaiTro = "Interviewer",
                    PhongBanId = "PBIT",
                    HoTen = "Lê Hoàng Giang",
                    Email = "giang.it@example.com",
                    SoDienThoai = "0901000003",
                    NgayTao = ngayTao
                },
                new NguoiDung {
                    NhanVienId = "NV004",
                    TenDangNhap = "pv2",
                    MatKhau = "123456",
                    VaiTro = "Interviewer",
                    PhongBanId = "PBDA",
                    HoTen = "Phạm Thị Lan",
                    Email = "lan.da@example.com",
                    SoDienThoai = "0901000004",
                    NgayTao = ngayTao
                },
                new NguoiDung {
                    NhanVienId = "NV011",
                    TenDangNhap = "pv3",
                    MatKhau = "123456",
                    VaiTro = "Interviewer",
                    PhongBanId = "PBIT",
                    HoTen = "Nguyễn Văn Long",
                    Email = "long.full@example.com",
                    SoDienThoai = "0901000011",
                    NgayTao = ngayTao
                }
            };

            context.NguoiDungs.AddRange(nguoiDungs);
            context.SaveChanges();
        }
    }
}
