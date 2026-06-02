let _sessionTimerInterval = null;

async function logout() {
  try {
    await logoutApi();
  } finally {
    sessionStorage.removeItem("expiresAt");
    sessionStorage.removeItem("fullName");
    sessionStorage.removeItem("userId");
    sessionStorage.removeItem("cachedAccounts");
  }
}

function getTokenExpiry() {
  const expiry = sessionStorage.getItem("expiresAt");
  if (!expiry) return null;
  return new Date(expiry).getTime();
}

function startSessionTimer() {
  const expiry = getTokenExpiry();
  if (!expiry) return;

  const warningTime = 59.9 * 60 * 1000;

  if (_sessionTimerInterval) clearInterval(_sessionTimerInterval);
  _sessionTimerInterval = setInterval(async function () {
    const now = Date.now();
    const timeLeft = expiry - now;

    if (timeLeft <= 0) {
      await logout();
      window.location.href = "index.html";
      return;
    }

    if (timeLeft <= warningTime) {
      const minutes = Math.floor(timeLeft / 60000);
      const seconds = Math.floor((timeLeft % 60000) / 1000);
      const warning = document.getElementById("session-warning");
      const countdown = document.getElementById("session-countdown");
      if (warning && countdown) {
        warning.style.display = "flex";
        countdown.textContent = `${t("session_expires")} ${minutes}:${seconds.toString().padStart(2, "0")}`;
      }
    }
  }, 1000);
}

function requireAuth() {
  if (!sessionStorage.getItem("expiresAt")) {
    window.location.href = "index.html";
  }
}

const loginForm = document.getElementById("login-form");
if (loginForm) {
  loginForm.addEventListener("submit", async function (e) {
    e.preventDefault();
    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;
    const button = document.getElementById("login-btn");
    const errorMsg = document.getElementById("error-msg");
    errorMsg.style.display = "none";
    button.disabled = true;
    button.classList.add("btn-loading");

    if (!email || !password) {
      errorMsg.textContent = t("error_email_password");
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }
    try {
      const data = await login(email, password);
      sessionStorage.setItem("fullName", data.fullName);
      sessionStorage.setItem("expiresAt", data.expiresAt);
      sessionStorage.setItem("userId", data.userId);
      window.location.href = "dashboard.html";
    } catch (error) {
      errorMsg.textContent = error.message;
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
    }
  });
}

const registerForm = document.getElementById("register-form");
if (registerForm) {
  registerForm.addEventListener("submit", async function (e) {
    e.preventDefault();
    const fullName = document.getElementById("fullName").value;
    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;
    const button = document.getElementById("register-btn");
    const errorMsg = document.getElementById("error-msg");
    const confirmPassword = document.getElementById("confirmPassword").value;
    errorMsg.style.display = "none";
    button.disabled = true;
    button.classList.add("btn-loading");

    if (!fullName || !email || !password || !confirmPassword) {
      errorMsg.textContent = t("error_fill_all_fields");
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }

    if (!/^[a-zA-Z\s]+$/.test(fullName.trim())) {
      errorMsg.textContent = t("error_fullname_letters");
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }

    if (password !== confirmPassword) {
      errorMsg.textContent = t("error_passwords_no_match");
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }
    try {
      const data = await register(fullName, email, password);
      sessionStorage.setItem("fullName", data.fullName);
      sessionStorage.setItem("expiresAt", data.expiresAt);
      sessionStorage.setItem("userId", data.userId);
      window.location.href = "dashboard.html";
    } catch (error) {
      errorMsg.textContent = error.message;
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
    }
  });
}

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

  document.body.insertAdjacentHTML(
    "beforeend",
    `
<div class="chatbot-overlay" id="chatbot-overlay"></div>
<div class="chatbot-panel" id="chatbot-panel">
  <div class="chatbot-header">
    <div class="chatbot-header-left">
      <div class="chatbot-avatar">A</div>
      <div>
        <div class="chatbot-name">Alfred</div>
        <div class="chatbot-status" id="chatbot-status-text">BankLite Assistant</div>
      </div>
    </div>
    <button aria-label="Close chat" class="chatbot-close" id="chatbot-close">
      <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
      </svg>
    </button>
  </div>
  <div class="chatbot-messages" id="chatbot-messages">
    <div class="chatbot-message chatbot-message--alfred">
     <div class="chatbot-bubble" id="chatbot-greeting"></div>
    </div>
  </div>
  <div class="chatbot-input-area">
   <input type="text" class="chatbot-input" id="chatbot-input" maxlength="200" />
    <button aria-label="Send message" class="chatbot-send" data-testid="chat-send" id="chatbot-send">
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>
      </svg>
    </button>
  </div>
</div>`,
  );
  document.getElementById("chatbot-greeting").textContent =
    t("chatbot_greeting");
  document.getElementById("chatbot-input").placeholder = t(
    "chatbot_placeholder",
  );

  document.getElementById("chatbot-status-text").textContent =
    t("chatbot_status");
  const logoutBtn = document.getElementById("logout-btn");
  if (logoutBtn) {
    logoutBtn.addEventListener("click", function () {
      const modal = document.getElementById("logout-modal");
      if (modal) modal.style.display = "flex";
      const sidebar = document.querySelector(".sidebar");
      if (sidebar) sidebar.classList.remove("open");
    });
  }

  const modalCancel = document.getElementById("modal-cancel-btn");
  if (modalCancel) {
    modalCancel.addEventListener("click", function () {
      document.getElementById("logout-modal").style.display = "none";
    });
  }

  const modalConfirm = document.getElementById("modal-confirm-btn");
  if (modalConfirm) {
    modalConfirm.addEventListener("click", async function () {
      await logout();
      window.location.href = "index.html";
    });
  }

  if (sessionStorage.getItem("expiresAt")) startSessionTimer();

  const stayBtn = document.getElementById("session-stay-btn");
  if (stayBtn) {
    stayBtn.addEventListener("click", async function () {
      const data = await refreshToken();
      if (data) {
        sessionStorage.setItem("expiresAt", data.expiresAt);
        document.getElementById("session-warning").style.display = "none";
        startSessionTimer();
      } else {
        await logout();
        window.location.href = "index.html";
      }
    });
  }

  const forgotLink = document.getElementById("forgot-password-link");
  const forgotModal = document.getElementById("forgot-modal");
  const forgotCancelBtn = document.getElementById("forgot-cancel-btn");
  const forgotSubmitBtn = document.getElementById("forgot-submit-btn");

  if (forgotLink) {
    forgotLink.addEventListener("click", function (e) {
      e.preventDefault();
      forgotModal.style.display = "flex";
    });
  }

  if (forgotCancelBtn) {
    forgotCancelBtn.addEventListener("click", function () {
      forgotModal.style.display = "none";
      document.getElementById("forgot-email").value = "";
      document.getElementById("forgot-error").style.display = "none";
      document.getElementById("forgot-success").style.display = "none";
    });
  }

  if (forgotSubmitBtn) {
    forgotSubmitBtn.addEventListener("click", async function () {
      const email = document.getElementById("forgot-email").value.trim();
      const errorEl = document.getElementById("forgot-error");
      const successEl = document.getElementById("forgot-success");
      errorEl.style.display = "none";
      successEl.style.display = "none";

      if (!email) {
        errorEl.textContent = t("error_enter_email");
        errorEl.style.display = "block";
        return;
      }

      forgotSubmitBtn.disabled = true;
      forgotSubmitBtn.classList.add("btn-loading");

      try {
        await forgotPassword(email);
        successEl.textContent = t("success_reset_link");
        successEl.style.display = "block";
        document.getElementById("forgot-email").value = "";
      } catch (error) {
        errorEl.textContent = error.message;
        errorEl.style.display = "block";
      } finally {
        forgotSubmitBtn.disabled = false;
        forgotSubmitBtn.classList.remove("btn-loading");
      }
    });
  }
  const hamburgerBtn = document.getElementById("hamburger-btn");
  if (hamburgerBtn) {
    hamburgerBtn.addEventListener("click", function () {
      document.querySelector(".sidebar").classList.toggle("open");
    });

    document.addEventListener("click", function (e) {
      const sidebar = document.querySelector(".sidebar");
      if (
        sidebar &&
        !sidebar.contains(e.target) &&
        !hamburgerBtn.contains(e.target)
      ) {
        sidebar.classList.remove("open");
      }
    });
  }

  document.querySelectorAll(".sidebar-nav a").forEach((link) => {
    link.addEventListener("click", function () {
      document.querySelector(".sidebar").classList.remove("open");
    });
  });

  initPasswordToggle("toggle-password", "password", "eye-icon");
  initPasswordToggle(
    "toggle-password-confirm",
    "confirmPassword",
    "eye-icon-confirm",
  );
  initPasswordToggle(
    "toggle-current-password",
    "current-password",
    "eye-icon-current",
  );
  initPasswordToggle(
    "toggle-new-password-settings",
    "new-password",
    "eye-icon-new-settings",
  );

  const settingsBtn = document.getElementById("settings-btn");
  const settingsPanel = document.getElementById("settings-panel");
  const settingsOverlay = document.getElementById("settings-overlay");
  const settingsCloseBtn = document.getElementById("settings-close-btn");

  function openSettings() {
    settingsPanel.classList.add("open");
    settingsOverlay.style.display = "block";
    loadSettingsProfile();
  }

  function closeSettings() {
    settingsPanel.classList.remove("open");
    settingsOverlay.style.display = "none";
  }

  if (settingsBtn)
    settingsBtn.addEventListener("click", function (e) {
      e.preventDefault();
      openSettings();
    });

  if (settingsCloseBtn)
    settingsCloseBtn.addEventListener("click", closeSettings);
  if (settingsOverlay) settingsOverlay.addEventListener("click", closeSettings);

  async function loadSettingsProfile() {
    try {
      const data = await getUserProfile();
      document.getElementById("settings-name").textContent = data.fullName;
      document.getElementById("settings-email").textContent = data.email;
      const locale =
        localStorage.getItem("language") === "fr" ? "fr-CA" : "en-CA";
      document.getElementById("settings-since").textContent = new Date(
        data.createdAt,
      ).toLocaleDateString(locale, {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
      document.getElementById("settings-last-login").textContent =
        data.lastLoginAt
          ? new Date(data.lastLoginAt).toLocaleDateString(locale, {
              year: "numeric",
              month: "long",
              day: "numeric",
              hour: "2-digit",
              minute: "2-digit",
            })
          : t("first_login");
    } catch {}
  }

  const changePasswordBtn = document.getElementById("change-password-btn");
  if (changePasswordBtn) {
    changePasswordBtn.addEventListener("click", async function () {
      if (changePasswordBtn.disabled) return;
      const currentPassword = document.getElementById("current-password").value;
      const newPassword = document.getElementById("new-password").value;
      const successMsg = document.getElementById("change-password-success");
      const errorMsg = document.getElementById("change-password-error");
      successMsg.style.display = "none";
      errorMsg.style.display = "none";
      if (!currentPassword || !newPassword) {
        errorMsg.textContent = t("error_fill_both_fields");
        errorMsg.style.display = "block";
        return;
      }
      changePasswordBtn.disabled = true;
      changePasswordBtn.classList.add("btn-loading");
      try {
        await changePassword(currentPassword, newPassword);
        successMsg.textContent = t("success_password_changed");
        successMsg.style.display = "block";
        document.getElementById("current-password").value = "";
        document.getElementById("new-password").value = "";
        setTimeout(() => (successMsg.style.display = "none"), 3000);
      } catch (error) {
        errorMsg.textContent = error.message;
        errorMsg.style.display = "block";
      } finally {
        changePasswordBtn.disabled = false;
        changePasswordBtn.classList.remove("btn-loading");
      }
    });
  }

  const deleteAccountBtn = document.getElementById("delete-account-btn");
  if (deleteAccountBtn) {
    deleteAccountBtn.addEventListener("click", function () {
      if (!sessionStorage.getItem("expiresAt")) return;
      closeSettings();
      document.getElementById("delete-modal").style.display = "flex";
    });
  }

  const deleteCancelBtn = document.getElementById("delete-cancel-btn");
  if (deleteCancelBtn) {
    deleteCancelBtn.addEventListener("click", function () {
      document.getElementById("delete-modal").style.display = "none";
    });
  }
  const deleteConfirmBtn = document.getElementById("delete-confirm-btn");
  if (deleteConfirmBtn) {
    deleteConfirmBtn.addEventListener("click", async function () {
      try {
        await deleteAccount();
        await logout();
        window.location.href = "index.html";
      } catch {
        document.getElementById("delete-modal").style.display = "none";
      }
    });
  }

  const darkModeToggle = document.getElementById("dark-mode-toggle");
  if (darkModeToggle) {
    darkModeToggle.checked = localStorage.getItem("darkMode") === "true";
    darkModeToggle.addEventListener("change", function () {
      localStorage.setItem("darkMode", this.checked);
      document.body.classList.toggle("dark-mode", this.checked);
      if (typeof loadSpendingChart === "function") {
        loadSpendingChart();
      }
    });
    if (localStorage.getItem("darkMode") === "true") {
      document.body.classList.add("dark-mode");
    }
  }

  const languageToggle = document.getElementById("language-toggle");
  if (languageToggle) {
    languageToggle.checked = localStorage.getItem("language") === "fr";
    languageToggle.addEventListener("change", function () {
      setLanguage(this.checked ? "fr" : "en");
      window.location.reload();
    });
  }

  const chatbotBtn = document.getElementById("chatbot-btn");
  const chatbotPanel = document.getElementById("chatbot-panel");
  const chatbotOverlay = document.getElementById("chatbot-overlay");
  const chatbotClose = document.getElementById("chatbot-close");
  const chatbotInput = document.getElementById("chatbot-input");
  const chatbotSend = document.getElementById("chatbot-send");
  const chatbotMessages = document.getElementById("chatbot-messages");

  function openChatbot() {
    chatbotPanel.classList.add("open");
    chatbotOverlay.style.display = "block";
    chatbotInput.focus();
  }

  function closeChatbot() {
    chatbotPanel.classList.remove("open");
    chatbotOverlay.style.display = "none";
  }

  function addMessage(text, isUser) {
    const div = document.createElement("div");
    div.className = `chatbot-message ${isUser ? "chatbot-message--user" : "chatbot-message--alfred"}`;
    const bubble = document.createElement("div");
    bubble.className = "chatbot-bubble";
    bubble.textContent = text;
    div.appendChild(bubble);
    chatbotMessages.appendChild(div);
    chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
  }

  function addTyping() {
    const div = document.createElement("div");
    div.className = "chatbot-message chatbot-message--alfred chatbot-typing";
    div.id = "chatbot-typing";
    const bubble = document.createElement("div");
    bubble.className = "chatbot-bubble";
    bubble.textContent = t("chatbot_typing");
    div.appendChild(bubble);
    chatbotMessages.appendChild(div);
    chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
  }

  function removeTyping() {
    const typing = document.getElementById("chatbot-typing");
    if (typing) typing.remove();
  }

  async function sendMessage() {
    if (!sessionStorage.getItem("expiresAt")) return;
    const message = chatbotInput.value.trim();
    if (!message) return;

    chatbotInput.value = "";
    chatbotSend.disabled = true;
    addMessage(message, true);
    addTyping();

    try {
      const response = await sendChatMessage(message);
      removeTyping();
      addMessage(response, false);
    } catch {
      removeTyping();
      addMessage(t("chatbot_error"), false);
    } finally {
      chatbotSend.disabled = false;
      chatbotInput.focus();
    }
  }

  if (chatbotBtn)
    chatbotBtn.addEventListener("click", function (e) {
      e.preventDefault();
      openChatbot();
    });
  if (chatbotClose) chatbotClose.addEventListener("click", closeChatbot);
  if (chatbotOverlay) chatbotOverlay.addEventListener("click", closeChatbot);
  if (chatbotSend) chatbotSend.addEventListener("click", sendMessage);
  if (chatbotInput) {
    chatbotInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") sendMessage();
    });
  }
});
