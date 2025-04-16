document.addEventListener("DOMContentLoaded", function () {

    // ================= AOS Animation Init =================
    if (typeof AOS !== "undefined") {
        AOS.init();
    }

    // ================= Country → City Dynamic Select =================
    const countrySelect = document.getElementById("country");
    const citySelect = document.getElementById("city");

    const citiesByCountry = {
        "Jordan": ["عمّان", "إربد", "الزرقاء", "العقبة", "البلقاء", "المفرق", "الكرك", "معان", "الطفيلة", "جرش", "عجلون", "مأدبا"],
        "Egypt": ["القاهرة", "الجيزة", "الإسكندرية", "المنصورة", "طنطا", "الزقازيق"],
        "Saudi Arabia": ["الرياض", "جدة", "مكة", "المدينة", "الدمام", "أبها"]
    };

    if (countrySelect && citySelect) {
        countrySelect.addEventListener("change", function () {
            const selectedCountry = this.value;
            citySelect.innerHTML = '<option value="">اختر المدينة</option>';

            if (citiesByCountry[selectedCountry]) {
                citiesByCountry[selectedCountry].forEach(city => {
                    const option = document.createElement("option");
                    option.value = city;
                    option.textContent = city;
                    citySelect.appendChild(option);
                });
            }
        });
    }

    // ================= Preview Avatar Image =================
    const avatarInput = document.getElementById("avatarUpload");
    const avatarPreview = document.querySelector(".avatar-preview");

    if (avatarInput && avatarPreview) {
        avatarInput.addEventListener("change", function () {
            const file = this.files[0];
            if (file && file.type.startsWith("image/")) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    avatarPreview.style.backgroundImage = `url('${e.target.result}')`;
                };
                reader.readAsDataURL(file);
            }
        });
    }
});
