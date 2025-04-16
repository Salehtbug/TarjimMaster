document.addEventListener("DOMContentLoaded", function () {
    console.log("✅ Tarjim Custom JS Loaded");

    // === 1. Navbar scroll behavior ===
    const navbar = document.getElementById("mainNavbar");
    if (navbar) {
        window.addEventListener("scroll", function () {
            if (window.scrollY > 30) {
                navbar.classList.add("bg-white", "shadow", "py-2");
                navbar.classList.remove("bg-transparent", "py-4");
            } else {
                navbar.classList.remove("bg-white", "shadow", "py-2");
                navbar.classList.add("bg-transparent", "py-4");
            }
        });
    }

    // === 2. Mobile menu toggle ===
    const toggleBtn = document.getElementById("mobileMenuToggle");
    const mobileMenu = document.getElementById("mobileMenu");

    if (toggleBtn && mobileMenu) {
        toggleBtn.addEventListener("click", function () {
            mobileMenu.classList.toggle("hidden");
        });

        mobileMenu.querySelectorAll("a").forEach(link => {
            link.addEventListener("click", () => {
                mobileMenu.classList.add("hidden");
            });
        });
    }

    // === 3. Fade-in animation on scroll ===
    const fadeInElements = document.querySelectorAll(".fade-in");
    if (fadeInElements.length > 0) {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add("opacity-100", "translate-y-0");
                    entry.target.classList.remove("opacity-0", "translate-y-10");
                }
            });
        }, { threshold: 0.1 });

        fadeInElements.forEach(el => observer.observe(el));
    }

    // === 4. Smooth scroll to anchors ===
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener("click", function (e) {
            const target = document.querySelector(this.getAttribute("href"));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: "smooth", block: "start" });
            }
        });
    });
});
