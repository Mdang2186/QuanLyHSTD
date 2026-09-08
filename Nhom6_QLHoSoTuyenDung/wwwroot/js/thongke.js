document.addEventListener("DOMContentLoaded", function () {
    veTatCaBieuDo();
});

// Cache chart instances to properly destroy and re-render on filter update
const chartInstances = {};

// Palette Màu Lam / Sky Blue mềm mại (Không bị quá đậm, hài hòa tự nhiên)
const softBluePalette = [
    '#3b82f6', // Bright Blue
    '#0ea5e9', // Sky Cyan Main
    '#38bdf8', // Light Sky Blue
    '#60a5fa', // Soft Blue Pastel
    '#818cf8', // Indigo Soft
    '#06b6d4', // Cyan Soft
    '#93c5fd', // Light Pastel Blue
    '#bae6fd'  // Very Light Sky
];

// ==================== TỔNG HỢP VẼ TẤT CẢ BIỂU ĐỒ ====================
function veTatCaBieuDo() {
    const { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId } = layBoLoc();
    const loaiBaoCao = document.querySelector('select[name="loaiBaoCao"]')?.value || "";

    const shouldShow = (loai) => loaiBaoCao === "" || loaiBaoCao === loai;

    if (shouldShow("trangthai")) {
        veBieuDo("/ThongKe/BieuDoTrangThaiUngVien", "chartTrangThai", "bar", "Ứng viên theo trạng thái", { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId });
    }
    if (shouldShow("nguon")) {
        veBieuDo("/ThongKe/BieuDoNguonUngVien", "chartNguon", "doughnut", "Ứng viên theo nguồn", { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId });
    }
    if (shouldShow("vitri")) {
        veBieuDo("/ThongKe/BieuDoTheoViTri", "chartViTri", "bar", "Ứng viên theo vị trí", { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId });
    }
    if (shouldShow("phongban")) {
        veBieuDo("/ThongKe/BieuDoTheoPhongBan", "chartPhongBan", "bar", "Ứng viên theo phòng ban", { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId });
    }

    // Luôn hiển thị:
    veBieuDo("/ThongKe/BieuDoDiemDanhGia", "chartDanhGia", "doughnut", "Phân loại đánh giá", { tuKhoa, tuNgay, denNgay });
    veBieuDoXuHuong("/ThongKe/BieuDoXuHuong12Thang", "chartXuHuongThang", { tuKhoa, tuNgay, denNgay, trangThai, viTriId, phongBanId });
}

// ==================== VẼ BIỂU ĐỒ CHUNG (BAR / DOUGHNUT) ====================
function veBieuDo(apiUrl, canvasId, chartType, chartTitle, params) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const url = taoUrl(apiUrl, params);

    fetch(url)
        .then(res => res.json())
        .then(data => {
            if (!Array.isArray(data)) return;

            const labels = data.map(x => x.ten);
            const values = data.map(x => x.soLuong);

            // Xóa instance cũ nếu tồn tại
            if (chartInstances[canvasId]) {
                chartInstances[canvasId].destroy();
            }

            const ctx = canvas.getContext('2d');
            let datasetConfig = {};

            if (chartType === "doughnut" || chartType === "pie") {
                datasetConfig = {
                    label: "Số lượng",
                    data: values,
                    backgroundColor: softBluePalette.slice(0, values.length),
                    borderWidth: 2,
                    borderColor: "#ffffff"
                };
            } else {
                // Biểu đồ cột Bar bo tròn góc với màu tươi mát không quá đậm
                datasetConfig = {
                    label: "Số lượng",
                    data: values,
                    backgroundColor: softBluePalette.slice(0, values.length),
                    borderRadius: 6,
                    borderSkipped: false
                };
            }

            const chartOptions = {
                maintainAspectRatio: false,
                responsive: true,
                plugins: {
                    legend: {
                        display: (chartType === "doughnut" || chartType === "pie"),
                        position: 'bottom',
                        labels: {
                            font: { family: 'Inter', size: 11 },
                            usePointStyle: true,
                            pointStyleWidth: 8,
                            boxHeight: 8
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(15, 23, 42, 0.85)',
                        titleFont: { family: 'Inter', size: 12, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 10,
                        cornerRadius: 8
                    }
                }
            };

            if (chartType !== "doughnut" && chartType !== "pie") {
                chartOptions.scales = {
                    y: {
                        beginAtZero: true,
                        grid: { color: '#f1f5f9' },
                        ticks: { font: { family: 'Inter', size: 11 }, precision: 0 }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                };
            }

            if (chartType === "doughnut") {
                chartOptions.cutout = '65%';
            }

            chartInstances[canvasId] = new Chart(ctx, {
                type: chartType,
                data: {
                    labels: labels,
                    datasets: [datasetConfig]
                },
                options: chartOptions
            });
        })
        .catch(err => console.error("Lỗi khi nạp biểu đồ " + canvasId + ":", err));
}

// ==================== VẼ BIỂU ĐỒ XU HƯỚNG 12 THÁNG CHÍNH XÁC NHƯ TRANG VỊ TRÍ TUYỂN DỤNG ====================
function veBieuDoXuHuong(apiUrl, canvasId, params) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const url = taoUrl(apiUrl, params);

    fetch(url)
        .then(res => res.json())
        .then(data => {
            if (!data) return;

            // Xử lý dữ liệu trả về từ BieuDoXuHuong12Thang ({ thang: [...], viTriMoi: [...], hoanThanh: [...] })
            const labels = data.thang || data.Thang || [];
            const valuesMoi = data.viTriMoi || data.ViTriMoi || [];
            const valuesHoanThanh = data.hoanThanh || data.HoanThanh || [];

            if (chartInstances[canvasId]) {
                chartInstances[canvasId].destroy();
            }

            const ctx = canvas.getContext('2d');

            // Tạo dải màu gradient sinh động mịn màng giống hệt trang Vị trí tuyển dụng
            const gradientBlue = ctx.createLinearGradient(0, 0, 0, 220);
            gradientBlue.addColorStop(0, 'rgba(37, 99, 235, 0.28)');
            gradientBlue.addColorStop(1, 'rgba(37, 99, 235, 0.01)');

            const gradientSky = ctx.createLinearGradient(0, 0, 0, 220);
            gradientSky.addColorStop(0, 'rgba(14, 165, 233, 0.22)');
            gradientSky.addColorStop(1, 'rgba(14, 165, 233, 0.01)');

            chartInstances[canvasId] = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: "Vị trí mới",
                            data: valuesMoi,
                            borderColor: "#2563eb",
                            backgroundColor: gradientBlue,
                            fill: true,
                            tension: 0.42,
                            borderWidth: 2.5,
                            pointRadius: 4,
                            pointBackgroundColor: "#2563eb",
                            pointBorderColor: "#ffffff",
                            pointBorderWidth: 2,
                            pointHoverRadius: 6
                        },
                        {
                            label: "Hoàn thành",
                            data: valuesHoanThanh,
                            borderColor: "#0ea5e9",
                            backgroundColor: gradientSky,
                            fill: true,
                            tension: 0.42,
                            borderWidth: 2.5,
                            pointRadius: 4,
                            pointBackgroundColor: "#0ea5e9",
                            pointBorderColor: "#ffffff",
                            pointBorderWidth: 2,
                            pointHoverRadius: 6
                        }
                    ]
                },
                options: {
                    maintainAspectRatio: false,
                    responsive: true,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'bottom',
                            labels: {
                                font: { family: 'Inter', size: 11, weight: '500' },
                                usePointStyle: true,
                                pointStyleWidth: 10,
                                boxHeight: 8
                            }
                        },
                        tooltip: {
                            backgroundColor: 'rgba(15, 23, 42, 0.85)',
                            titleFont: { family: 'Inter', size: 12, weight: 'bold' },
                            bodyFont: { family: 'Inter', size: 12 },
                            padding: 10,
                            cornerRadius: 8
                        }
                    },
                    scales: {
                        x: {
                            grid: { display: false },
                            ticks: { font: { family: 'Inter', size: 11 }, color: '#64748b' }
                        },
                        y: {
                            beginAtZero: true,
                            grid: { color: '#f1f5f9' },
                            ticks: { font: { family: 'Inter', size: 11 }, color: '#64748b', precision: 0 }
                        }
                    }
                }
            });
        })
        .catch(err => console.error("Lỗi khi nạp biểu đồ xu hướng 12 tháng " + canvasId + ":", err));
}

// ==================== TẠO URL VỚI PARAMS ====================
function taoUrl(apiUrl, params) {
    const query = new URLSearchParams();
    for (let key in params) {
        if (params[key]) query.append(key, params[key]);
    }
    return `${apiUrl}?${query.toString()}`;
}

// ==================== LẤY BỘ LỌC ====================
function layBoLoc() {
    return {
        tuKhoa: document.querySelector('input[name="tuKhoa"]')?.value || "",
        tuNgay: document.querySelector('input[name="tuNgay"]')?.value || "",
        denNgay: document.querySelector('input[name="denNgay"]')?.value || "",
        trangThai: document.querySelector('select[name="trangThai"]')?.value || "",
        viTriId: document.querySelector('select[name="viTriId"]')?.value || "",
        phongBanId: document.querySelector('select[name="phongBanId"]')?.value || ""
    };
}

// ==================== XUẤT BÁO CÁO EXCEL ====================
const btnExport = document.getElementById("btnExportExcel");
if (btnExport) {
    btnExport.addEventListener("click", async () => {
        const getValue = selector => {
            const value = document.querySelector(selector)?.value;
            return value && value.trim() !== "" ? value : null;
        };

        const request = {
            tuKhoa: getValue("input[name='tuKhoa']"),
            loaiBaoCao: getValue("select[name='loaiBaoCao']"),
            trangThai: getValue("select[name='trangThai']"),
            viTriId: getValue("select[name='viTriId']"),
            phongBanId: getValue("select[name='phongBanId']"),
            tuNgay: getValue("input[name='tuNgay']"),
            denNgay: getValue("input[name='denNgay']")
        };

        try {
            const response = await fetch("/ThongKe/XuatBaoCaoDayDu", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = `BaoCaoUngVien_${new Date().toISOString().slice(0, 10)}.xlsx`;
                document.body.appendChild(a);
                a.click();
                a.remove();
            } else {
                const errorText = await response.text();
                alert("❌ " + errorText);
            }
        } catch (e) {
            console.error("Lỗi khi xuất excel:", e);
        }
    });
}
