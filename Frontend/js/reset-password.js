document.addEventListener("DOMContentLoaded", function () {
  const params = new URLSearchParams(window.location.search);
  const token = params.get("token");
  const errorMsg = document.getElementById("error-msg");
  const successMsg = document.getElementById("success-msg");

  if (!token) {
    errorMsg.textContent = "Invalid or missing reset token.";
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
        errorMsg.textContent = "Please fill in both fields.";
        errorMsg.style.display = "block";
        return;
      }

      if (newPassword.length < 8) {
        errorMsg.textContent = "Password must be at least 8 characters.";
        errorMsg.style.display = "block";
        return;
      }

      if (newPassword !== confirmPassword) {
        errorMsg.textContent = "Passwords do not match.";
        errorMsg.style.display = "block";
        return;
      }

      btn.disabled = true;
      btn.classList.add("btn-loading");

      try {
        await resetPassword(token, newPassword);
        successMsg.textContent =
          "Password reset successfully! Redirecting to login...";
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

  const toggleNew = document.getElementById("toggle-new-password");
  if (toggleNew) {
    toggleNew.addEventListener("click", function () {
      const input = document.getElementById("new-password");
      const icon = document.getElementById("eye-icon-new");
      if (input.type === "password") {
        input.type = "text";
        icon.innerHTML = `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/>`;
        icon.setAttribute("stroke", "#1a3a5c");
      } else {
        input.type = "password";
        icon.innerHTML = `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>`;
        icon.setAttribute("stroke", "#9ca3af");
      }
    });
  }

  const toggleConfirm = document.getElementById("toggle-confirm-password");
  if (toggleConfirm) {
    toggleConfirm.addEventListener("click", function () {
      const input = document.getElementById("confirm-password");
      const icon = document.getElementById("eye-icon-confirm-reset");
      if (input.type === "password") {
        input.type = "text";
        icon.innerHTML = `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/>`;
        icon.setAttribute("stroke", "#1a3a5c");
      } else {
        input.type = "password";
        icon.innerHTML = `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>`;
        icon.setAttribute("stroke", "#9ca3af");
      }
    });
  }
});
