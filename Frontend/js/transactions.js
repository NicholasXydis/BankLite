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
      document.querySelector(".pagination").style.display = "flex";
      return;
    }

    result.items.forEach((transaction) => {
      const row = document.createElement("div");
      row.className = `transaction-row ${transaction.type.toLowerCase()}`;
      row.innerHTML = `
      <div class="transaction-left">
        <span class="transaction-type">${transaction.type} ${transaction.type === "Deposit" ? '<span class="transaction-arrow">↑</span>' : '<span class="transaction-arrow">↓</span>'}</span>
        <span class="transaction-date">${new Date(transaction.createdAt).toLocaleString("en-CA", { month: "short", day: "numeric", year: "numeric", hour: "numeric", minute: "2-digit" })}</span>
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
      option.textContent = `${account.type} - $${account.balance.toFixed(2)}`;
      accountSelect.appendChild(option);
    });

    await loadTransactions(accounts[0].id, 1);
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

loadAccounts();
