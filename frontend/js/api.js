const API_URL = window.ENV_API_URL ?? "";

async function readJsonSafe(response) {
  const text = await response.text();
  if (!text) return {};
  try {
    return JSON.parse(text);
  } catch {
    if (response.ok) {
      throw new Error("Invalid JSON response.");
    }
    return {};
  }
}

function handleServerError(response) {
  if (response.status === 404) {
    window.location.href = "404.html";
  }
  if (response.status >= 500) {
    window.location.href = "500.html";
  }
}

async function login(email, password) {
  const response = await fetch(API_URL + "/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (response.status === 429) throw new Error(t("error_too_many_login"));
  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      throw new Error(
        data
          .map((e) => {
            if (e.errorMessage.includes("valid email"))
              return t("error_valid_email");
            if (e.errorMessage.includes("least 8"))
              return t("error_min_8_chars");
            return e.errorMessage;
          })
          .join(" "),
      );
    }
    const msg = data.message || "Login failed";
    if (msg.includes("Invalid Credentials"))
      throw new Error(t("error_invalid_credentials"));
    throw new Error(msg);
  }

  return data;
}

async function register(fullName, email, password) {
  const response = await fetch(API_URL + "/api/auth/register", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ fullName, email, password }),
  });

  if (response.status === 429) throw new Error(t("error_too_many_register"));
  const data = await readJsonSafe(response);
  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      const uniqueErrors = [
        ...new Set(
          data.map((e) => {
            if (e.errorMessage.includes("least 8"))
              return t("error_min_8_chars");
            if (e.errorMessage.includes("valid email"))
              return t("error_valid_email");
            if (e.errorMessage.includes("already"))
              return t("error_email_taken");
            return e.errorMessage;
          }),
        ),
      ];
      throw new Error(uniqueErrors.join(" "));
    }
    const msg = data.message || "";
    if (msg.includes("already")) throw new Error(t("error_email_taken"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function getAccounts(forceRefresh = false) {
  if (!forceRefresh) {
    const cached = sessionStorage.getItem("cachedAccounts");
    if (cached) return JSON.parse(cached);
  }
  const response = await fetch(API_URL + "/api/account", {
    method: "GET",
    credentials: "include",
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || t("error_load_accounts"));
  }
  sessionStorage.setItem("cachedAccounts", JSON.stringify(data));
  return data;
}

async function deposit(accountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/deposit", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ accountId, amount }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("Insufficient"))
      throw new Error(t("error_insufficient_funds"));
    if (msg.includes("access")) throw new Error(t("error_no_access"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function withdraw(accountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/withdraw", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ accountId, amount }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("Insufficient"))
      throw new Error(t("error_insufficient_funds"));
    if (msg.includes("access")) throw new Error(t("error_no_access"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function transfer(fromAccountId, toAccountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/transfer", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ fromAccountId, toAccountId, amount }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("Insufficient"))
      throw new Error(t("error_insufficient_funds"));
    if (msg.includes("access")) throw new Error(t("error_no_access"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function transferExternal(fromAccountId, toAccountNumber, amount) {
  const response = await fetch(API_URL + "/api/transaction/transferexternal", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ fromAccountId, toAccountNumber, amount }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("Insufficient"))
      throw new Error(t("error_insufficient_funds"));
    if (msg.includes("not found") || msg.includes("Account"))
      throw new Error(t("error_account_not_found"));
    if (msg.includes("access")) throw new Error(t("error_no_access"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function getTransactions(
  accountId,
  page = 1,
  pageSize = 10,
  type = null,
  signal = null,
) {
  const typeParam = type && type !== "all" ? `&type=${type}` : "";
  const response = await fetch(
    API_URL +
      `/api/transaction/${accountId}?page=${page}&pageSize=${pageSize}${typeParam}`,
    {
      method: "GET",
      credentials: "include",
      signal,
    },
  );

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("access")) throw new Error(t("error_no_access"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function getTransactionsByDateRange(accountId, startDate, endDate) {
  const response = await fetch(
    `${API_URL}/api/transaction/${accountId}/range?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`,
    {
      method: "GET",
      credentials: "include",
    },
  );

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || t("error_occurred"));
  }

  return data;
}

async function createAccount(accountType) {
  const response = await fetch(API_URL + "/api/account/create", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ type: accountType }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    const msg = data.message || "";
    if (msg.includes("already have"))
      throw new Error(t("error_account_exists"));
    throw new Error(msg || t("error_occurred"));
  }

  return data;
}

async function getUserProfile() {
  const response = await fetch(API_URL + "/api/user/profile", {
    method: "GET",
    credentials: "include",
  });
  const data = await readJsonSafe(response);
  handleServerError(response);
  if (!response.ok) throw new Error(data.message || t("error_load_profile"));
  return data;
}

async function changePassword(currentPassword, newPassword) {
  const response = await fetch(API_URL + "/api/user/change-password", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ currentPassword, newPassword }),
  });

  if (response.status === 429) throw new Error(t("error_too_many_attempts"));
  const data = await readJsonSafe(response);
  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      throw new Error(
        data
          .map((e) => {
            if (e.errorMessage.includes("least 8"))
              return t("error_min_8_chars");
            if (e.errorMessage.includes("different"))
              return t("error_password_same");
            return e.errorMessage;
          })
          .join(" "),
      );
    }
    const msg = data.message || "";
    if (msg.includes("different")) throw new Error(t("error_password_same"));
    if (msg.includes("incorrect")) throw new Error(t("error_wrong_password"));
    throw new Error(msg || t("error_occurred"));
  }
  return data;
}

async function deleteAccount() {
  const response = await fetch(API_URL + "/api/user/delete-account", {
    method: "DELETE",
    credentials: "include",
  });
  const data = await readJsonSafe(response);
  handleServerError(response);
  if (!response.ok) throw new Error(data.message || t("error_occurred"));
}

async function sendChatMessage(message) {
  const response = await fetch(`${API_URL}/api/chat/message`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ content: message }),
  });

  const data = await readJsonSafe(response);

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || t("error_occurred"));
  }

  return data.response;
}

async function logoutApi() {
  await fetch(API_URL + "/api/auth/refresh/logout", {
    method: "POST",
    credentials: "include",
  });
}

async function refreshToken() {
  const response = await fetch(API_URL + "/api/auth/refresh", {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok) return null;

  const data = await readJsonSafe(response);
  return data;
}

async function forgotPassword(email) {
  const lang = localStorage.getItem("language") || "en";
  const response = await fetch(API_URL + "/api/auth/forgot-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, lang }),
  });
  if (response.status === 429) throw new Error(t("error_too_many_attempts"));
  const data = await readJsonSafe(response);
  if (!response.ok) throw new Error(data.message || t("error_occurred"));
  return data;
}

async function resetPassword(token, newPassword) {
  const response = await fetch(API_URL + "/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword }),
  });
  const data = await readJsonSafe(response);
  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      throw new Error(
        data.map((error) => error.errorMessage).join(" ") ||
          t("error_occurred"),
      );
    }
    const msg = data.message || "";
    if (msg.includes("Invalid") || msg.includes("expired"))
      throw new Error(t("error_invalid_token"));
    throw new Error(msg || t("error_occurred"));
  }
  return data;
}

function initPasswordToggle(toggleId, inputId, iconId) {
  const toggle = document.getElementById(toggleId);
  const input = document.getElementById(inputId);
  const icon = document.getElementById(iconId);
  if (!toggle || !input || !icon || toggle.dataset.passwordToggleReady) return;
  toggle.dataset.passwordToggleReady = "true";
  toggle.addEventListener("click", function () {
    if (input.type === "password") {
      input.type = "text";
      icon.innerHTML = `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/><line x1="1" y1="1" x2="23" y2="23"/>`;
    } else {
      input.type = "password";
      icon.innerHTML = `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>`;
    }
    icon.setAttribute("stroke", "#9ca3af");
  });
}
