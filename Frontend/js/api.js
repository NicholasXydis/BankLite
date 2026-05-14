const API_URL = "https://localhost:7205";
async function login(email, password) {
  const response = await fetch(API_URL + "/api/auth/login", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  const data = await response.json();

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
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ fullName, email, password }),
  });

  const data = await response.json();

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
    headers: {
      Authorization: "Bearer " + token,
    },
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch accounts");
  }
  return data;
}

async function deposit(token, accountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/deposit", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ accountId, amount }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to deposit funds");
  }

  return data;
}

async function withdraw(token, accountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/withdraw", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ accountId, amount }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to withdraw funds");
  }

  return data;
}

async function transfer(token, fromAccountId, toAccountId, amount) {
  const response = await fetch(API_URL + "/api/transaction/transfer", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ fromAccountId, toAccountId, amount }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to transfer funds");
  }

  return data;
}

async function transferExternal(token, fromAccountId, toAccountNumber, amount) {
  const response = await fetch(API_URL + "/api/transaction/transferexternal", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ fromAccountId, toAccountNumber, amount }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to transfer funds");
  }

  return data;
}

async function getTransactions(token, accountId, page = 1, pageSize = 10) {
  const response = await fetch(
    API_URL + `/api/transaction/${accountId}?page=${page}&pageSize=${pageSize}`,
    {
      method: "GET",
      headers: {
        Authorization: "Bearer " + token,
      },
    },
  );

  const data = await response.json();

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
      headers: {
        Authorization: "Bearer " + token,
      },
    },
  );

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to fetch transactions");
  }

  return data;
}

async function createAccount(token, accountType) {
  const response = await fetch(API_URL + "/api/account/create", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ type: accountType }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to create account");
  }

  return data;
}

async function getUserProfile(token) {
  const response = await fetch(API_URL + "/api/user/profile", {
    method: "GET",
    headers: {
      Authorization: "Bearer " + token,
    },
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to load profile");
  return data;
}

async function changePassword(token, currentPassword, newPassword) {
  const response = await fetch(API_URL + "/api/user/change-password", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: JSON.stringify({ currentPassword, newPassword }),
  });
  const data = await response.json();
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
    headers: {
      Authorization: "Bearer " + token,
    },
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to delete account");
}

async function sendChatMessage(token, message) {
  const response = await fetch(`${API_URL}/api/chat/message`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ content: message }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Failed to get response");
  }

  return data.response;
}
