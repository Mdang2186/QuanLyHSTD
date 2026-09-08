using Nhom6_QLHoSoTuyenDung.Models.Entities;

namespace Nhom6_QLHoSoTuyenDung.Data
{
    public static class NhanVienSeedData
    {
        public static void Seed(AppDbContext context)
        {
            if (context.NhanViens.Any()) return;

            var nhanViens = new List<NhanVien>
            {
                new NhanVien {
                    MaNhanVien = "NV000", HoTen = "Đỗ Công Minh", MaSoNV = "PBIT00", NgaySinh = new DateTime(2003, 5, 21),
                    ChucVu = "Admin", PhongBanId = "PBIT", Email = "mdang2186@gmail.com",
                    SoDienThoai = "0372986808", NgayVaoCongTy = new DateTime(2024, 1, 1),
                    KinhNghiem = "2", MoTa = "Quản trị hệ thống", MucLuong = 30000000
                },
                new NhanVien {
                    MaNhanVien = "NV001", HoTen = "Đinh Thị Diễm Quỳnh", MaSoNV = "KT01", NgaySinh = new DateTime(2004, 10, 10),
                    ChucVu = "Trưởng phòng", PhongBanId = "PBIT", Email = "diemquynhdinh1010@gmail.com",
                    SoDienThoai = "0901000001", NgayVaoCongTy = new DateTime(2015, 2, 1),
                    KinhNghiem = "8", MoTa = "Giỏi nghiệp vụ", MucLuong = 25000000
                },
                new NhanVien {
                    MaNhanVien = "NV002", HoTen = "Trần Thị Bích", MaSoNV = "NS01", NgaySinh = new DateTime(1991, 5, 10),
                    ChucVu = "HR", PhongBanId = "PBNS", Email = "bich.ns@example.com",
                    SoDienThoai = "0901000002", NgayVaoCongTy = new DateTime(2017, 3, 15),
                    KinhNghiem = "6", MoTa = "Phụ trách tuyển dụng", MucLuong = 18000000
                },
                new NhanVien {
                    MaNhanVien = "NV003", HoTen = "Lê Hoàng Giang", MaSoNV = "IT01", NgaySinh = new DateTime(1992, 7, 20),
                    ChucVu = "Backend Dev", PhongBanId = "PBIT", Email = "giang.it@example.com",
                    SoDienThoai = "0901000003", NgayVaoCongTy = new DateTime(2018, 6, 1),
                    KinhNghiem = "5", MoTa = "Backend C# .NET", MucLuong = 20000000
                },
                new NhanVien {
                    MaNhanVien = "NV004", HoTen = "Phạm Thị Lan", MaSoNV = "DA01", NgaySinh = new DateTime(1993, 3, 18),
                    ChucVu = "Data Analyst", PhongBanId = "PBDA", Email = "lan.da@example.com",
                    SoDienThoai = "0901000004", NgayVaoCongTy = new DateTime(2019, 1, 5),
                    KinhNghiem = "4", MoTa = "Phân tích dữ liệu", MucLuong = 19000000
                },
                new NhanVien {
                    MaNhanVien = "NV005", HoTen = "Vũ Đức Minh", MaSoNV = "KD01", NgaySinh = new DateTime(1989, 11, 11),
                    ChucVu = "Kinh doanh", PhongBanId = "PBKD", Email = "minh.kd@example.com",
                    SoDienThoai = "0901000005", NgayVaoCongTy = new DateTime(2014, 8, 10),
                    KinhNghiem = "9", MoTa = "Bán hàng chiến lược", MucLuong = 23000000
                },
                new NhanVien {
                    MaNhanVien = "NV006", HoTen = "Trần Văn Phúc", MaSoNV = "IT02", NgaySinh = new DateTime(1994, 8, 8),
                    ChucVu = "Tester", PhongBanId = "PBIT", Email = "phuc.test@example.com",
                    SoDienThoai = "0901000006", NgayVaoCongTy = new DateTime(2020, 9, 1),
                    KinhNghiem = "3", MoTa = "Tester manual", MucLuong = 16000000
                },
                new NhanVien {
                    MaNhanVien = "NV007", HoTen = "Ngô Thị Hiền", MaSoNV = "NS02", NgaySinh = new DateTime(1995, 12, 5),
                    ChucVu = "HR Assistant", PhongBanId = "PBNS", Email = "hien.hr@example.com",
                    SoDienThoai = "0901000007", NgayVaoCongTy = new DateTime(2021, 4, 15),
                    KinhNghiem = "2", MoTa = "Hỗ trợ tuyển dụng", MucLuong = 15000000
                },
                new NhanVien {
                    MaNhanVien = "NV008", HoTen = "Đinh Văn Sơn", MaSoNV = "IT03", NgaySinh = new DateTime(1990, 4, 2),
                    ChucVu = "Frontend", PhongBanId = "PBIT", Email = "son.fe@example.com",
                    SoDienThoai = "0901000008", NgayVaoCongTy = new DateTime(2016, 1, 20),
                    KinhNghiem = "7", MoTa = "React + JS", MucLuong = 21000000
                },
                new NhanVien {
                    MaNhanVien = "NV009", HoTen = "Hồ Thị Thảo", MaSoNV = "DA02", NgaySinh = new DateTime(1992, 10, 3),
                    ChucVu = "Data Analyst", PhongBanId = "PBDA", Email = "thao.da@example.com",
                    SoDienThoai = "0901000009", NgayVaoCongTy = new DateTime(2018, 2, 15),
                    KinhNghiem = "5", MoTa = "Dữ liệu doanh nghiệp", MucLuong = 18500000
                },
                new NhanVien {
                    MaNhanVien = "NV010", HoTen = "Lưu Thị Hằng", MaSoNV = "NS03", NgaySinh = new DateTime(1993, 6, 1),
                    ChucVu = "HR", PhongBanId = "PBNS", Email = "hang.ns@example.com",
                    SoDienThoai = "0901000010", NgayVaoCongTy = new DateTime(2020, 2, 1),
                    KinhNghiem = "3", MoTa = "Đăng tuyển và lọc hồ sơ", MucLuong = 16000000
                },
                new NhanVien {
                    MaNhanVien = "NV011", HoTen = "Nguyễn Văn Long", MaSoNV = "IT04", NgaySinh = new DateTime(1991, 9, 9),
                    ChucVu = "Fullstack Dev", PhongBanId = "PBIT", Email = "long.full@example.com",
                    SoDienThoai = "0901000011", NgayVaoCongTy = new DateTime(2015, 6, 20),
                    KinhNghiem = "8", MoTa = "ASP.NET + JS", MucLuong = 25000000
                },
                new NhanVien {
                    MaNhanVien = "NV012", HoTen = "Bùi Thị Dung", MaSoNV = "NS04", NgaySinh = new DateTime(1988, 2, 12),
                    ChucVu = "Trưởng phòng NS", PhongBanId = "PBNS", Email = "dung.truongns@example.com",
                    SoDienThoai = "0901000012", NgayVaoCongTy = new DateTime(2013, 5, 1),
                    KinhNghiem = "10", MoTa = "Quản lý nhân sự", MucLuong = 27000000
                }
            };

            context.NhanViens.AddRange(nhanViens);
            context.SaveChanges();
        }
    }
}
