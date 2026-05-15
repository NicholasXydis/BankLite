const API_URL = "https://localhost:7205";

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

  if (response.status === 429)
    throw new Error("Too many login attempts. Please wait a minute.");
  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      throw new Error(data.map((e) => e.errorMessage).join(", "));
    }
    throw new Error(data.message || "Login failed");
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

  if (response.status === 429)
    throw new Error("Too many registration attempts. Please wait a minute.");

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      const uniqueErrors = [...new Set(data.map((e) => e.errorMessage))];
      throw new Error(uniqueErrors.join(". "));
    }
    throw new Error(data.message || "Registration failed");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch accounts");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to deposit funds");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to withdraw funds");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to transfer funds");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to transfer funds");
  }

  return data;
}

async function getTransactions(
  accountId,
  page = 1,
  pageSize = 10,
  type = null,
) {
  const typeParam = type && type !== "all" ? `&type=${type}` : "";
  const response = await fetch(
    API_URL +
      `/api/transaction/${accountId}?page=${page}&pageSize=${pageSize}${typeParam}`,
    {
      method: "GET",
      credentials: "include",
    },
  );

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch transactions");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch transactions");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to create account");
  }

  return data;
}

async function getUserProfile() {
  const response = await fetch(API_URL + "/api/user/profile", {
    method: "GET",
    credentials: "include",
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to load profile");
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
  const data = await response.json();
  handleServerError(response);
  if (!response.ok) {
    if (Array.isArray(data)) {
      throw new Error(data.map((e) => e.errorMessage).join(", "));
    }
    throw new Error(data.message || "Failed to change password");
  }
  return data;
}

async function deleteAccount() {
  const response = await fetch(API_URL + "/api/user/delete-account", {
    method: "DELETE",
    credentials: "include",
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to delete account");
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

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to get response");
  }

  return data.response;
}

async function logoutApi() {
  await fetch(API_URL + "/api/auth/logout", {
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

  const data = await response.json();
  return data;
}

async function forgotPassword(email) {
  const response = await fetch(API_URL + "/api/auth/forgot-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });
  const data = await response.json();
  if (!response.ok)
    throw new Error(data.message || "Failed to send reset email");
  return data;
}

async function resetPassword(token, newPassword) {
  const response = await fetch(API_URL + "/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword }),
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to reset password");
  return data;
}
