document.addEventListener("DOMContentLoaded", function () {
  const langToggleBtn = document.getElementById("lang-toggle-btn");
  if (langToggleBtn) {
    const lang = localStorage.getItem("language") || "en";
    const flag = document.getElementById("lang-flag");
    const label = document.getElementById("lang-label");
    if (lang === "fr") {
      flag.innerHTML =
        '<svg width="24" height="18" viewBox="0 0 24 18" xmlns="http://www.w3.org/2000/svg"><g transform="translate(0,1)"><rect width="24" height="17" fill="#fff"/><rect width="6" height="17" fill="#D80621"/><rect x="18" width="6" height="17" fill="#D80621"/><path d="M12 4l1 3h3l-2.5 2 1 3L12 10.5 9.5 12l1-3L8 7h3z" fill="#D80621"/></g></svg>';
      label.textContent = "EN";
    } else {
      flag.innerHTML =
        '<svg width="24" height="18" viewBox="0 0 24 18" xmlns="http://www.w3.org/2000/svg"><g transform="translate(0,1)"><rect width="24" height="17" fill="#003DA5"/><rect x="11" width="2" height="17" fill="#fff"/><rect y="7" width="24" height="2" fill="#fff"/><text x="3.5" y="7" font-size="5" fill="#fff" font-family="serif">⚜</text><text x="15.5" y="7" font-size="5" fill="#fff" font-family="serif">⚜</text><text x="3.5" y="15" font-size="5" fill="#fff" font-family="serif">⚜</text><text x="15.5" y="15" font-size="5" fill="#fff" font-family="serif">⚜</text></g></svg>';
      label.textContent = "FR";
    }
    langToggleBtn.addEventListener("click", function () {
      setLanguage(localStorage.getItem("language") === "fr" ? "en" : "fr");
      window.location.reload();
    });
  }

  const params = new URLSearchParams(window.location.search);
  const token = params.get("token");
  const errorMsg = document.getElementById("error-msg");
  const successMsg = document.getElementById("success-msg");

  if (!token) {
    errorMsg.textContent = t("error_invalid_token");
    errorMsg.style.display = "block";
    document.getElementById("reset-btn").disabled = true;
    return;
  }

  document
    .getElementById("reset-form")
    .addEventListener("submit", async function (e) {
      e.preventDefault();
      const newPassword = document.getElementById("new-password").value;
      const confirmPassword = document.getElementById("confirm-password").value;
      const btn = document.getElementById("reset-btn");

      errorMsg.style.display = "none";
      successMsg.style.display = "none";

      if (!newPassword || !confirmPassword) {
        errorMsg.textContent = t("error_fill_both_fields");
        errorMsg.style.display = "block";
        return;
      }

      if (newPassword.length < 8) {
        errorMsg.textContent = t("error_min_8_chars");
        errorMsg.style.display = "block";
        return;
      }

      if (newPassword !== confirmPassword) {
        errorMsg.textContent = t("error_passwords_no_match");
        errorMsg.style.display = "block";
        return;
      }

      btn.disabled = true;
      btn.classList.add("btn-loading");

      try {
        await resetPassword(token, newPassword);
        successMsg.textContent = t("success_password_reset");
        successMsg.style.display = "block";
        setTimeout(() => (window.location.href = "index.html"), 2000);
      } catch (error) {
        errorMsg.textContent = error.message;
        errorMsg.style.display = "block";
      } finally {
        btn.disabled = false;
        btn.classList.remove("btn-loading");
      }
    });

  initPasswordToggle("toggle-new-password", "new-password", "eye-icon-new");
  initPasswordToggle(
    "toggle-confirm-password",
    "confirm-password",
    "eye-icon-confirm-reset",
  );
});
