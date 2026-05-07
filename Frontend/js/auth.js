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
