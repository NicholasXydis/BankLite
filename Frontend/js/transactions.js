let currentPage = 1;
const pageSize = 10;
let currentFilter = "all";

async function loadTransactions(accountId, page, type = null) {
  requireAuth();

  const errorMsg = document.getElementById("error-msg");
  const transactionsList = document.getElementById("transactions-list");
  const pageInfo = document.getElementById("page-info");
  const prevBtn = document.getElementById("prev-btn");
  const nextBtn = document.getElementById("next-btn");

  errorMsg.style.display = "none";
  transactionsList.innerHTML = "";
  document.querySelector(".pagination").style.opacity = "0";
  prevBtn.style.opacity = "0";
  nextBtn.style.opacity = "0";
  transactionsList.style.opacity = "0";
  try {
    const result = await getTransactions(accountId, page, pageSize, type);
    transactionsList.innerHTML = "";
    document.getElementById("no-filter-results").style.display = "none";

    if (result.items.length === 0) {
      document.getElementById("export-csv-btn").style.display = "none";
      transactionsList.innerHTML = "";
      document.getElementById("no-filter-results").textContent = t(
        "label_no_transactions",
      );
      document.getElementById("no-filter-results").style.display = "block";
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
      const lang = localStorage.getItem("language") || "en";
      const dateStr = txDate.toLocaleDateString(
        lang === "fr" ? "fr-CA" : "en-CA",
        {
          weekday: "long",
          month: "long",
          day: "numeric",
          year: "numeric",
        },
      );
      const label = isToday
        ? t("label_today")
        : isYesterday
          ? t("label_yesterday")
          : dateStr;

      if (label !== lastDate) {
        lastDate = label;
        const header = document.createElement("div");
        header.className = "transaction-date-header";
        header.textContent = label;
        transactionsList.appendChild(header);
      }
      const row = document.createElement("div");
      const isTransfer = transaction.description
        .toLowerCase()
        .includes("transfer");
      const displayType = isTransfer
        ? t("nav_transfer")
        : transaction.type === "Deposit"
          ? t("nav_deposit")
          : t("nav_withdraw");
      const isIncoming =
        isTransfer && transaction.description.toLowerCase().includes("from");
      const transferClass = isTransfer
        ? isIncoming
          ? "deposit"
          : "withdrawal"
        : "";
      row.className = `transaction-row ${isTransfer ? transferClass : transaction.type.toLowerCase()}${isTransfer ? " transfer" : ""}`;
      row.innerHTML = `
    <div class="transaction-left">
    <span class="transaction-type">${displayType} ${isTransfer ? '<span class="transaction-arrow transaction-arrow--transfer"><svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg></span>' : transaction.type === "Deposit" ? '<span class="transaction-arrow">↑</span>' : '<span class="transaction-arrow">↓</span>'}</span>
    <span class="transaction-date">${txDate.toLocaleString(lang === "fr" ? "fr-CA" : "en-CA", { hour: "numeric", minute: "2-digit" })}</span>
    </div>
    <span class="transaction-amount">${transaction.type === "Deposit" || isIncoming ? "+" : "-"}$${transaction.amount.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
`;
      transactionsList.appendChild(row);
    });

    document.getElementById("export-csv-btn").style.display = "flex";
    const totalPages = Math.ceil(result.totalCount / pageSize);
    pageInfo.textContent = `${t("page_of")} ${page} ${t("page_of_total")} ${totalPages}`;
    prevBtn.disabled = page <= 1;
    nextBtn.disabled = page >= totalPages;
    currentPage = page;
    document.querySelector(".pagination").style.display = "flex";
    document.querySelector(".pagination").style.opacity = "1";
    prevBtn.style.opacity = "1";
    nextBtn.style.opacity = "1";
    transactionsList.style.opacity = "1";
  } catch (error) {
    transactionsList.innerHTML = "";
    document.querySelector(".pagination").style.display = "flex";
    errorMsg.textContent = error.message;
    errorMsg.style.display = "block";
  }
}
async function loadAccounts() {
  requireAuth();

  const accountSelect = document.getElementById("account-select");

  try {
    const accounts = await getAccounts();

    if (accounts.length === 0) {
      document.getElementById("empty-state").style.display = "block";
      document.querySelector(".form-card").style.display = "none";
      return;
    }
    document.querySelector(".form-card").style.display = "block";
    document.querySelector(".pagination").style.display = "none";
    document.getElementById("export-csv-btn").style.display = "none";

    accountSelect.innerHTML = "";
    accounts.forEach((account) => {
      const option = document.createElement("option");
      option.value = account.id;
      option.textContent = `${account.type === "Chequing" ? t("dashboard_chequing") : t("dashboard_savings")} | $${account.balance.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
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
    currentFilter = "all";
    document
      .querySelectorAll(".filter-btn")
      .forEach((b) => b.classList.remove("active"));
    document.querySelector(".filter-btn").classList.add("active");
    await loadTransactions(this.value, 1);
  });

document
  .getElementById("prev-btn")
  .addEventListener("click", async function () {
    const accountId = document.getElementById("account-select").value;
    await loadTransactions(
      accountId,
      currentPage - 1,
      currentFilter === "all" ? null : currentFilter,
    );
  });

document
  .getElementById("next-btn")
  .addEventListener("click", async function () {
    const accountId = document.getElementById("account-select").value;
    await loadTransactions(
      accountId,
      currentPage + 1,
      currentFilter === "all" ? null : currentFilter,
    );
  });

document
  .getElementById("export-csv-btn")
  .addEventListener("click", async function () {
    requireAuth();

    const accountId = document.getElementById("account-select").value;
    if (!accountId) return;

    try {
      const result = await getTransactions(accountId, 1, 10000);
      const rows = [["Date", "Type", "Amount", "Description"]];

      result.items.forEach((tx) => {
        const date = new Date(tx.createdAt + "Z").toLocaleString("en-CA", {
          month: "short",
          day: "numeric",
          year: "numeric",
          hour: "numeric",
          minute: "2-digit",
        });
        const amount = `${tx.type === "Deposit" ? "+" : "-"}$${tx.amount.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
        rows.push([date, tx.type, amount, tx.description]);
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
    } catch (error) {}
  });

document.querySelectorAll(".filter-btn").forEach((btn) => {
  btn.addEventListener("click", function () {
    document
      .querySelectorAll(".filter-btn")
      .forEach((b) => b.classList.remove("active"));
    this.classList.add("active");
    currentFilter = this.dataset.filter;

    const accountId = document.getElementById("account-select").value;
    currentPage = 1;
    loadTransactions(
      accountId,
      1,
      currentFilter === "all" ? null : currentFilter,
    );
  });
});

loadAccounts();
