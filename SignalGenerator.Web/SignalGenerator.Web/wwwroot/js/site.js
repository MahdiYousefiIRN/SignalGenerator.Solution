// ✅ اطلاعات مربوط به بهینه‌سازی فایل‌های استاتیک را در لینک زیر مشاهده کنید:
// https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification

// ℹ️ این فایل برای نوشتن کدهای جاوا اسکریپت عمومی در پروژه استفاده می‌شود.

console.log("%c[INIT] site.js loaded successfully!", "color: green; font-weight: bold;");

/**
 * ✅ تابع نمایش لاگ‌های رنگی در کنسول
 * @param {string} message - پیغام موردنظر
 * @param {string} type - نوع پیغام (info, success, warning, error)
 */
function logMessage(message, type = "info") {
    const styles = {
        info: "color: blue; font-weight: bold;",
        success: "color: green; font-weight: bold;",
        warning: "color: orange; font-weight: bold;",
        error: "color: red; font-weight: bold;"
    };
    console.log(`%c[LOG] ${message}`, styles[type] || styles.info);
}

/**
 * ✅ اجرای تابع خاص پس از لود شدن کامل صفحه
 */
document.addEventListener("DOMContentLoaded", function () {
    logMessage("Document fully loaded!", "success");
});

/**
 * ✅ تابعی برای مقداردهی اولیه به دکمه‌ها
 */
function initializeButtons() {
    logMessage("Initializing buttons...", "info");

    const buttons = document.querySelectorAll("button");
    buttons.forEach(button => {
        button.addEventListener("click", function () {
            logMessage(`Button clicked: ${button.innerText}`, "success");
        });
    });
}

// اجرای مقداردهی اولیه به دکمه‌ها پس از لود صفحه
window.onload = initializeButtons;
