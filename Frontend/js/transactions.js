let currentPage = 1;
const pageSize = 10;

async function loadTransactions(accountId, page) {
  const token = requireAuth();
  if (!token) return;

  const errorMsg = document.getElementById("error-msg");
  const transactionsList = document.getElementById("transactions-list");
  const pageInfo = document.getElementById("page-info");
  const prevBtn = document.getElementById("prev-btn");
  const nextBtn = document.getElementById("next-btn");

  errorMsg.style.display = "none";
  transactionsList.innerHTML = "<p>Loading...</p>";

  try {
    const result = await getTransactions(token, accountId, page, pageSize);
    transactionsList.innerHTML = "";

    if (result.items.length === 0) {
      transactionsList.innerHTML =
        "<p class='no-transactions'>No transactions found</p>";
      pageInfo.textContent = "";
      prevBtn.disabled = true;
      nextBtn.disabled = true;
      document.querySelector(".pagination").style.display = "none";
      return;
    }

    let lastDate = null;
    result.items.forEach((transaction) => {
      const txDate = new Date(transaction.createdAt + "Z");
      const today = new Date();
      const yesterday = new Date();
      yesterday.setDate(today.getDate() - 1);
      const isToday = txDate.toDateString() === today.toDateString();
      const isYesterday = txDate.toDateString() === yesterday.toDateString();
      const dateStr = txDate.toLocaleDateString("en-CA", {
        weekday: "long",
        month: "long",
        day: "numeric",
        year: "numeric",
      });
      const label = isToday ? "Today" : isYesterday ? "Yesterday" : dateStr;

      if (label !== lastDate) {
        lastDate = label;
        const header = document.createElement("div");
        header.className = "transaction-date-header";
        header.textContent = label;
        transactionsList.appendChild(header);
      }
      const row = document.createElement("div");
      row.className = `transaction-row ${transaction.type.toLowerCase()}`;
      row.innerHTML = `
        <div class="transaction-left">
        <span class="transaction-type">${transaction.type} ${transaction.type === "Deposit" ? '<span class="transaction-arrow">↑</span>' : '<span class="transaction-arrow">↓</span>'}</span>
        <span class="transaction-date">${txDate.toLocaleString("en-CA", { hour: "numeric", minute: "2-digit" })}</span>
        </div>
        <span class="transaction-amount">${transaction.type === "Deposit" ? "+" : "-"}$${transaction.amount.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
      `;
      transactionsList.appendChild(row);
    });

    const totalPages = Math.ceil(result.totalCount / pageSize);
    pageInfo.textContent = `Page ${page} of ${totalPages}`;
    prevBtn.disabled = page <= 1;
    nextBtn.disabled = page >= totalPages;
    currentPage = page;
    document.querySelector(".pagination").style.display = "flex";
  } catch (error) {
    transactionsList.innerHTML = "";
    document.querySelector(".pagination").style.display = "flex";
    errorMsg.textContent = error.message;
    errorMsg.style.display = "block";
  }
}
async function loadAccounts() {
  const token = requireAuth();
  if (!token) return;

  const accountSelect = document.getElementById("account-select");

  try {
    const accounts = await getAccounts(token);

    if (accounts.length === 0) {
      document.getElementById("empty-state").style.display = "block";
      document.querySelector(".form-card").style.display = "none";
      return;
    }
    document.querySelector(".form-card").style.display = "block";
    document.querySelector(".pagination").style.display = "none";

    accountSelect.innerHTML = "";
    accounts.forEach((account) => {
      const option = document.createElement("option");
      option.value = account.id;
      option.textContent = `${account.type} | $${account.balance.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      accountSelect.appendChild(option);
    });

    const urlParams = new URLSearchParams(window.location.search);
    const preSelectedId = urlParams.get("accountId");

    if (preSelectedId) {
      const match = accounts.find((a) => a.id === preSelectedId);
      if (match) {
        accountSelect.value = match.id;
        await loadTransactions(match.id, 1);
      } else {
        await loadTransactions(accounts[0].id, 1);
      }
    } else {
      await loadTransactions(accounts[0].id, 1);
    }
  } catch (error) {
    document.getElementById("error-msg").textContent = error.message;
    document.getElementById("error-msg").style.display = "block";
  }
}

document
  .getElementById("account-select")
  .addEventListener("change", async function () {
    currentPage = 1;
    document.querySelector(".pagination").style.display = "none";
    await loadTransactions(this.value, 1);
  });

document
  .getElementById("prev-btn")
  .addEventListener("click", async function () {
    const accountId = document.getElementById("account-select").value;
    await loadTransactions(accountId, currentPage - 1);
  });

document
  .getElementById("next-btn")
  .addEventListener("click", async function () {
    const accountId = document.getElementById("account-select").value;
    await loadTransactions(accountId, currentPage + 1);
  });

document
  .getElementById("export-csv-btn")
  .addEventListener("click", async function () {
    const token = requireAuth();
    if (!token) return;

    const accountId = document.getElementById("account-select").value;
    if (!accountId) return;

    try {
      const result = await getTransactions(token, accountId, 1, 10000);
      const rows = [["Date", "Type", "Amount", "Description"]];

      result.items.forEach((t) => {
        const date = new Date(t.createdAt + "Z").toLocaleString("en-CA", {
          month: "short",
          day: "numeric",
          year: "numeric",
          hour: "numeric",
          minute: "2-digit",
        });
        const amount = `${t.type === "Deposit" ? "+" : "-"}$${t.amount.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
        rows.push([date, t.type, amount, t.description]);
      });

      const csv = rows
        .map((r) => r.map((cell) => `"${cell}"`).join(","))
        .join("\n");
      const blob = new Blob([csv], { type: "text/csv" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "transactions.csv";
      a.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error("Export failed:", error);
    }
  });

loadAccounts();
