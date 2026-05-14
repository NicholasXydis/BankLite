function saveToken(token) {
  sessionStorage.setItem("authToken", token);
}

function getToken() {
  return sessionStorage.getItem("authToken");
}

function isLoggedIn() {
  const token = getToken();
  return !!token;
}

function logout() {
  sessionStorage.removeItem("authToken");
}

function getTokenExpiry() {
  const token = getToken();
  if (!token) return null;
  const payload = JSON.parse(atob(token.split(".")[1]));
  return payload.exp * 1000;
}

function startSessionTimer() {
  const expiry = getTokenExpiry();
  if (!expiry) return;

  const warningTime = 59.9 * 60 * 1000;

  setInterval(function () {
    const now = Date.now();
    const timeLeft = expiry - now;

    if (timeLeft <= 0) {
      logout();
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
        countdown.textContent = `Your session expires in ${minutes}:${seconds.toString().padStart(2, "0")}`;
      }
    }
  }, 1000);
}

function requireAuth() {
  const token = getToken();
  if (!token) {
    window.location.href = "index.html";
    return;
  }
  return token;
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
      errorMsg.textContent = "Please enter both email and password.";
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }
    try {
      const data = await login(email, password);
      saveToken(data.token);
      sessionStorage.setItem("fullName", data.fullName);
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
      errorMsg.textContent = "Please fill in all fields.";
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }

    if (!/^[a-zA-Z\s]+$/.test(fullName.trim())) {
      errorMsg.textContent = "Full name can only contain letters and spaces.";
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }

    if (password !== confirmPassword) {
      errorMsg.textContent = "Passwords do not match.";
      errorMsg.style.display = "block";
      button.disabled = false;
      button.classList.remove("btn-loading");
      return;
    }
    try {
      const data = await register(fullName, email, password);
      saveToken(data.token);
      sessionStorage.setItem("fullName", data.fullName);
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
        <div class="chatbot-status">BankLite Assistant</div>
      </div>
    </div>
    <button class="chatbot-close" id="chatbot-close">
      <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
      </svg>
    </button>
  </div>
  <div class="chatbot-messages" id="chatbot-messages">
    <div class="chatbot-message chatbot-message--alfred">
      <div class="chatbot-bubble">Hi! I'm Alfred, your BankLite assistant. How can I help you today?</div>
    </div>
  </div>
  <div class="chatbot-input-area">
    <input type="text" class="chatbot-input" id="chatbot-input" placeholder="Ask Alfred anything..." maxlength="200" />
    <button class="chatbot-send" id="chatbot-send">
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>
      </svg>
    </button>
  </div>
</div>`,
  );
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
    modalConfirm.addEventListener("click", function () {
      logout();
      window.location.href = "index.html";
    });
  }

  if (getToken()) startSessionTimer();

  const stayBtn = document.getElementById("session-stay-btn");
  if (stayBtn) {
    stayBtn.addEventListener("click", function () {
      document.getElementById("session-warning").style.display = "none";
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

  const togglePassword = document.getElementById("toggle-password");
  if (togglePassword) {
    togglePassword.addEventListener("click", function () {
      const passwordInput = document.getElementById("password");
      const eyeIcon = document.getElementById("eye-icon");
      if (passwordInput.type === "password") {
        passwordInput.type = "text";
        eyeIcon.innerHTML = `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/>`;
        eyeIcon.setAttribute("stroke", "#1a3a5c");
      } else {
        passwordInput.type = "password";
        eyeIcon.innerHTML = `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>`;
        eyeIcon.setAttribute("stroke", "#9ca3af");
      }
    });
  }

  const toggleConfirmPassword = document.getElementById(
    "toggle-password-confirm",
  );
  if (toggleConfirmPassword) {
    toggleConfirmPassword.addEventListener("click", function () {
      const passwordInput = document.getElementById("confirmPassword");
      const eyeIconConfirm = document.getElementById("eye-icon-confirm");
      if (passwordInput.type === "password") {
        passwordInput.type = "text";
        eyeIconConfirm.innerHTML = `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/>`;
        eyeIconConfirm.setAttribute("stroke", "#1a3a5c");
      } else {
        passwordInput.type = "password";
        eyeIconConfirm.innerHTML = `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>`;
        eyeIconConfirm.setAttribute("stroke", "#9ca3af");
      }
    });
  }

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
    const token = getToken();
    if (!token) return;
    try {
      const data = await getUserProfile(token);
      document.getElementById("settings-name").textContent = data.fullName;
      document.getElementById("settings-email").textContent = data.email;
      document.getElementById("settings-since").textContent = new Date(
        data.createdAt,
      ).toLocaleDateString("en-CA", {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
      document.getElementById("settings-last-login").textContent =
        data.lastLoginAt
          ? new Date(data.lastLoginAt).toLocaleDateString("en-CA", {
              year: "numeric",
              month: "long",
              day: "numeric",
              hour: "2-digit",
              minute: "2-digit",
            })
          : "First login";
    } catch (e) {
      console.error("Failed to load profile", e);
    }
  }

  const changePasswordBtn = document.getElementById("change-password-btn");
  if (changePasswordBtn) {
    changePasswordBtn.addEventListener("click", async function () {
      const token = getToken();
      const currentPassword = document.getElementById("current-password").value;
      const newPassword = document.getElementById("new-password").value;
      const successMsg = document.getElementById("change-password-success");
      const errorMsg = document.getElementById("change-password-error");
      successMsg.style.display = "none";
      errorMsg.style.display = "none";
      if (!currentPassword || !newPassword) {
        errorMsg.textContent = "Please fill in both fields.";
        errorMsg.style.display = "block";
        return;
      }
      changePasswordBtn.disabled = true;
      changePasswordBtn.classList.add("btn-loading");
      try {
        await changePassword(token, currentPassword, newPassword);
        successMsg.textContent = "Password changed successfully!";
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
      const token = getToken();
      if (!token) return;
      closeSettings();
      const modal = document.getElementById("logout-modal");
      const modalTitle = document.querySelector(".modal-title");
      const modalText = document.querySelector(".modal-text");
      const modalConfirm = document.getElementById("modal-confirm-btn");
      modalTitle.textContent = "Delete Account";
      modalText.textContent =
        "This will permanently delete your account and all data. This cannot be undone.";
      modalConfirm.textContent = "Delete";
      modal.style.display = "flex";
      modalConfirm.onclick = async function () {
        try {
          await deleteAccount(token);
          logout();
          window.location.href = "index.html";
        } catch (error) {
          modal.style.display = "none";
        }
      };
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
      localStorage.setItem("language", this.checked ? "fr" : "en");
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
    div.innerHTML = `<div class="chatbot-bubble">${text}</div>`;
    chatbotMessages.appendChild(div);
    chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
  }

  function addTyping() {
    const div = document.createElement("div");
    div.className = "chatbot-message chatbot-message--alfred chatbot-typing";
    div.id = "chatbot-typing";
    div.innerHTML = `<div class="chatbot-bubble">Alfred is typing...</div>`;
    chatbotMessages.appendChild(div);
    chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
  }

  function removeTyping() {
    const typing = document.getElementById("chatbot-typing");
    if (typing) typing.remove();
  }

  async function sendMessage() {
    const token = getToken();
    if (!token) return;
    const message = chatbotInput.value.trim();
    if (!message) return;

    chatbotInput.value = "";
    chatbotSend.disabled = true;
    addMessage(message, true);
    addTyping();

    try {
      const response = await sendChatMessage(token, message);
      removeTyping();
      addMessage(response, false);
    } catch (error) {
      removeTyping();
      addMessage(
        "Sorry, I couldn't process your request. Please try again.",
        false,
      );
    } finally {
      chatbotSend.disabled = false;
      chatbotInput.focus();
    }
  }

  if (chatbotBtn) chatbotBtn.addEventListener("click", openChatbot);
  if (chatbotClose) chatbotClose.addEventListener("click", closeChatbot);
  if (chatbotOverlay) chatbotOverlay.addEventListener("click", closeChatbot);
  if (chatbotSend) chatbotSend.addEventListener("click", sendMessage);
  if (chatbotInput) {
    chatbotInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") sendMessage();
    });
  }
});
