document.addEventListener("DOMContentLoaded", function () {
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
