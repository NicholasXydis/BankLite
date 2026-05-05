async function loadDashboard() {
  const token = requireAuth();
  if (!token) return;

  const userName = sessionStorage.getItem("fullName");
  if (userName) {
    const firstName = userName.split(" ")[0];
    document.getElementById("welcome-msg").textContent =
      `Welcome back, ${firstName}!`;
    document.getElementById("welcome-msg").style.display = "block";
  }

  const errorMsg = document.getElementById("error-msg");
  const accountsContainer = document.getElementById("accounts-container");

  try {
    const accounts = await getAccounts(token);
    accountsContainer.innerHTML = "";

    accounts.forEach((account) => {
      const card = document.createElement("div");
      card.className = "account-card";
      card.innerHTML = `
      <div class="card-header">
      <div class="account-icon">
      ${
        account.type === "Chequing"
          ? `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#1a3a5c" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="14" rx="3"/><line x1="2" y1="10" x2="22" y2="10"/><line x1="6" y1="15" x2="10" y2="15"/><circle cx="17" cy="15" r="1.5" fill="#1a3a5c"/></svg>`
          : `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#1a3a5c" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><ellipse cx="12" cy="7" rx="8" ry="3"/><path d="M4 7v5c0 1.657 3.582 3 8 3s8-1.343 8-3V7"/><path d="M4 12v5c0 1.657 3.582 3 8 3s8-1.343 8-3v-5"/></svg>`
      }
        </div>
        <h2>${account.type}</h2>
        </div>
        <div class="account-number-row">
        <p class="account-number">${account.accountNumber}</p>
        <button class="copy-btn" data-number="${account.accountNumber}">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#1a3a5c" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>
        </button>
        </div>
        <p class="account-balance-amount" data-balance="${account.balance}">$0.00</p>
      `;
      accountsContainer.appendChild(card);
    });

    let delay = 400;
    document.querySelectorAll(".account-balance-amount").forEach((el) => {
      const target = parseFloat(el.dataset.balance);
      setTimeout(() => countUp(el, target, 1100), delay);
      delay += 400;
    });

    if (accounts.length > 0) {
      const totalBalance = accounts.reduce(
        (sum, account) => sum + account.balance,
        0,
      );
      const totalEl = document.createElement("div");
      totalEl.className = "total-balance";
      totalEl.innerHTML = `
        <p>Total Balance</p>
        <h2 id="total-balance-amount">$0.00</h2>
      `;

      accountsContainer.insertBefore(totalEl, accountsContainer.firstChild);
      countUp(
        document.getElementById("total-balance-amount"),
        totalBalance,
        1100,
      );
    }

    const hasChequing = accounts.some((a) => a.type === "Chequing");
    const hasSavings = accounts.some((a) => a.type === "Savings");

    if (hasChequing && hasSavings) {
      document.getElementById("create-account-section").style.display = "none";
    } else {
      document.getElementById("create-account-section").style.display = "block";
    }
  } catch (error) {
    errorMsg.textContent = error.message;
    errorMsg.style.display = "block";
  }
}

document
  .getElementById("create-account-btn")
  .addEventListener("click", async function () {
    const token = requireAuth();
    if (!token) return;

    const accountType = parseInt(
      document.getElementById("account-type-select").value,
    );
    const errorMsg = document.getElementById("create-error-msg");
    const successMsg = document.getElementById("create-success-msg");
    const btn = document.getElementById("create-account-btn");

    errorMsg.style.display = "none";
    successMsg.style.display = "none";

    btn.disabled = true;
    btn.classList.add("btn-loading");

    try {
      await createAccount(token, accountType);
      successMsg.textContent = "Account created successfully!";
      successMsg.style.display = "block";
      await loadDashboard();
    } catch (error) {
      errorMsg.textContent = error.message;
      errorMsg.style.display = "block";
    } finally {
      btn.disabled = false;
      btn.classList.remove("btn-loading");
    }
  });

function typeText(elementId, text, speed = 180) {
  const el = document.getElementById(elementId);
  let i = 0;
  el.textContent = "";
  el.classList.add("typing-cursor");
  const interval = setInterval(() => {
    el.textContent += text[i];
    i++;
    if (i >= text.length) {
      clearInterval(interval);
      el.classList.remove("typing-cursor");
    }
  }, speed);
}

function countUp(element, target, duration = 1000) {
  const start = 0;
  const increment = target / (duration / 16);
  let current = start;
  const timer = setInterval(() => {
    current += increment;
    if (current >= target) {
      current = target;
      clearInterval(timer);
    }
    element.textContent = `$${current.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }, 16);
}

loadDashboard();
typeText("dashboard-title", "My Accounts");

document
  .getElementById("accounts-container")
  .addEventListener("click", function (e) {
    const btn = e.target.closest(".copy-btn");
    if (!btn) return;

    const number = btn.dataset.number;
    navigator.clipboard.writeText(number).then(() => {
      if (btn._copyTimeout) clearTimeout(btn._copyTimeout);
      if (!btn._originalHTML) btn._originalHTML = btn.innerHTML;
      btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#059669" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`;
      btn.style.background = "#dcfce7";
      btn._copyTimeout = setTimeout(() => {
        btn.innerHTML = btn._originalHTML;
        btn.style.background = "";
        btn._originalHTML = null;
        btn._copyTimeout = null;
      }, 1200);
    });
  });
