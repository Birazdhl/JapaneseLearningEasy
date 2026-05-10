(function () {
    const storageKey = "jl-bs-theme";

    /**
     * Apply Bootstrap's dual theme palettes (light/dark) with gentle defaults.
     */
    function bootstrapTheme(theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);
        try {
            localStorage.setItem(storageKey, theme);
        } catch {
            /* non-fatal in lockdown/private modes */
        }
    }

    function initStoredTheme() {
        let stored = "light";
        try {
            stored = localStorage.getItem(storageKey) || "light";
        } catch {
            stored = "light";
        }
        bootstrapTheme(stored === "dark" ? "dark" : "light");
    }

    function wireToggle() {
        const toggle = document.getElementById("jl-theme-toggle");
        if (!toggle) {
            return;
        }

        toggle.addEventListener("click", function () {
            const current = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
            bootstrapTheme(current === "dark" ? "light" : "dark");
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initStoredTheme();
        wireToggle();
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: "toast-top-end",
            timeOut: 4500,
            extendedTimeOut: 1200
        };
    });
})();
