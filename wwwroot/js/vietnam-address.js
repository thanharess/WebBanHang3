/** Địa chỉ VN 2 cấp: Tỉnh/TP → Phường/Xã (dữ liệu từ vn-address-data.json) */
window.initVnAddress = function (opts) {
  var province = document.querySelector(opts.province);
  var ward = document.querySelector(opts.ward);
  var street = document.querySelector(opts.street);
  var hidden = document.querySelector(opts.hidden);
  if (!province || !ward || !hidden) return;

  function fill(sel, items, placeholder) {
    sel.innerHTML = "";
    var o0 = document.createElement("option");
    o0.value = "";
    o0.textContent = placeholder;
    sel.appendChild(o0);
    (items || []).forEach(function (name) {
      var o = document.createElement("option");
      o.value = name;
      o.textContent = name;
      sel.appendChild(o);
    });
    sel.disabled = false;
  }

  function compose() {
    var parts = [];
    if (street && street.value.trim()) parts.push(street.value.trim());
    if (ward.value) parts.push(ward.value);
    if (province.value) parts.push(province.value);
    hidden.value = parts.join(", ");
  }

  function bindData(data) {
    fill(province, data.provinces, "-- Chọn Tỉnh / TP --");
    fill(ward, [], "-- Chọn Phường / Xã --");

    province.addEventListener("change", function () {
      var list = (data.wardsByProvince && data.wardsByProvince[province.value]) || [];
      fill(ward, list, list.length ? "-- Chọn Phường / Xã --" : "-- Không có dữ liệu --");
      compose();
    });
    ward.addEventListener("change", compose);
    if (street) street.addEventListener("input", compose);

    var form = hidden.closest("form");
    if (form) {
      form.addEventListener("submit", function (e) {
        compose();
        if (!province.value || !ward.value) {
          e.preventDefault();
          alert("Vui lòng chọn Tỉnh/TP và Phường/Xã.");
          return false;
        }
        if (!street || !street.value.trim()) {
          e.preventDefault();
          alert("Vui lòng nhập địa chỉ chi tiết (số nhà, tên đường).");
          return false;
        }
      });
    }
  }

  fetch("/js/vn-address-data.json")
    .then(function (r) { return r.json(); })
    .then(bindData)
    .catch(function (err) {
      console.error("Không tải được dữ liệu địa chỉ", err);
      fill(province, [], "-- Lỗi tải Tỉnh/TP --");
    });
};
