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

async function getAccounts(token) {
  const response = await fetch(API_URL + "/api/account", {
    method: "GET",
    credentials: "include",
  });

  const data = await response.json();

  handleServerError(response);
  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch accounts");
  }
  return data;
}

async function deposit(token, accountId, amount) {
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

async function withdraw(token, accountId, amount) {
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

async function transfer(token, fromAccountId, toAccountId, amount) {
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

async function transferExternal(token, fromAccountId, toAccountNumber, amount) {
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
  token,
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

async function getTransactionsByDateRange(
  token,
  accountId,
  startDate,
  endDate,
) {
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

async function createAccount(token, accountType) {
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

async function getUserProfile(token) {
  const response = await fetch(API_URL + "/api/user/profile", {
    method: "GET",
    credentials: "include",
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to load profile");
  return data;
}

async function changePassword(token, currentPassword, newPassword) {
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

async function deleteAccount(token) {
  const response = await fetch(API_URL + "/api/user/delete-account", {
    method: "DELETE",
    credentials: "include",
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to delete account");
}

async function sendChatMessage(token, message) {
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
