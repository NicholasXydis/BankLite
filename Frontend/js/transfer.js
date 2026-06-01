async function loadTransfer() {
  requireAuth();

  const accountSelect = document.getElementById("account-select");
  const toAccountSelect = document.getElementById("to-account-select");
  const errorMsg = document.getElementById("error-msg");
  const successMsg = document.getElementById("success-msg");
  let accounts = [];

  try {
    accounts = await getAccounts();
    if (accounts.length === 0) {
      document.getElementById("empty-state").style.display = "block";
      document.querySelector(".form-card").style.display = "none";
      return;
    }
    document.querySelector(".form-card").style.display = "block";

    accountSelect.innerHTML = "";
    accounts.forEach((account) => {
      const option = document.createElement("option");
      option.value = account.id;
      option.textContent = `${account.type === "Chequing" ? t("dashboard_chequing") : t("dashboard_savings")} | $${account.balance.toLocaleString(
        "en-CA",
        {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        },
      )}`;
      accountSelect.appendChild(option);
    });

    toAccountSelect.innerHTML = "";
    accounts.forEach((account) => {
      const option = document.createElement("option");
      option.value = account.id;
      option.textContent = `${account.type === "Chequing" ? t("dashboard_chequing") : t("dashboard_savings")} | $${account.balance.toLocaleString(
        "en-CA",
        {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        },
      )}`;
      toAccountSelect.appendChild(option);
    });
  } catch (error) {
    errorMsg.textContent = error.message;
    errorMsg.style.display = "block";
  }
}

document
  .getElementById("transfer-btn")
  .addEventListener("click", async function () {
    const errorMsg = document.getElementById("error-msg");
    const successMsg = document.getElementById("success-msg");
    const accountId = document.getElementById("account-select").value;
    const isExternal = toggleExternal.classList.contains("active");
    const toAccountId = document.getElementById("to-account-select").value;
    const toAccountNumber = document.getElementById("to-account-number").value;
    const amount = Number.parseFloat(
      document.getElementById("amount").value.replace(/[$,]/g, ""),
    );

    errorMsg.style.display = "none";
    successMsg.style.display = "none";

    if (!accountId) {
      errorMsg.textContent = t("error_select_account");
      errorMsg.style.display = "block";
      return;
    }

    if (isExternal && !toAccountNumber.trim()) {
      errorMsg.textContent = t("error_enter_recipient");
      errorMsg.style.display = "block";
      return;
    }

    if (!isExternal && !toAccountId) {
      errorMsg.textContent = t("error_select_destination");
      errorMsg.style.display = "block";
      return;
    }

    if (!amount || amount <= 0) {
      errorMsg.textContent = t("error_valid_amount");
      errorMsg.style.display = "block";
      return;
    }

    if (amount > 1000000) {
      errorMsg.textContent = t("error_max_transfer");
      errorMsg.style.display = "block";
      return;
    }

    if (!isExternal && toAccountId === accountId) {
      errorMsg.textContent = t("error_same_account");
      errorMsg.style.display = "block";
      return;
    }

    const btn = document.getElementById("transfer-btn");
    btn.disabled = true;
    btn.classList.add("btn-loading");

    try {
      if (isExternal) {
        if (!/^[a-zA-Z0-9]+$/.test(toAccountNumber.trim())) {
          errorMsg.textContent = t("error_enter_recipient");
          errorMsg.style.display = "block";
          return;
        }
        await transferExternal(accountId, toAccountNumber.trim(), amount);
      } else {
        await transfer(accountId, toAccountId, amount);
      }
      successMsg.textContent = `${t("success_transferred")} $${amount.toLocaleString(
        "en-CA",
        {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        },
      )}!`;
      successMsg.style.display = "block";
      setTimeout(() => {
        successMsg.style.display = "none";
      }, 3000);
      document.getElementById("amount").value = "";
      const selectEl = document.getElementById("account-select");
      selectEl.classList.add("flash-red");
      setTimeout(() => selectEl.classList.remove("flash-red"), 1000);

      if (isExternal) {
        const toNumberInput = document.getElementById("to-account-number");
        toNumberInput.classList.add("flash-green");
        setTimeout(() => toNumberInput.classList.remove("flash-green"), 1000);
      } else {
        const toSelectEl = document.getElementById("to-account-select");
        toSelectEl.classList.add("flash-green");
        setTimeout(() => toSelectEl.classList.remove("flash-green"), 1000);
      }
      const selectedId = accountId;
      const selectedToId = toAccountId;
      await getAccounts(true);
      await loadTransfer();
      document.getElementById("account-select").value = selectedId;
      document.getElementById("to-account-select").value = selectedToId;
    } catch (error) {
      errorMsg.textContent = error.message.includes("entity")
        ? t("error_occurred")
        : error.message;
      errorMsg.style.display = "block";
    } finally {
      btn.disabled = false;
      btn.classList.remove("btn-loading");
    }
  });

document.getElementById("amount").addEventListener("input", function () {
  let raw = this.value.replace(/[^0-9.]/g, "");
  const parts = raw.split(".");
  parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  if (parts.length > 2) parts.splice(2);
  if (parts[1] !== undefined) parts[1] = parts[1].slice(0, 2);
  this.value = raw === "" ? "" : "$" + parts.join(".");
});

const toggleMyAccounts = document.getElementById("toggle-my-accounts");
const toggleExternal = document.getElementById("toggle-external");
const toAccountSelectGroup = document
  .querySelector("#to-account-select")
  .closest(".form-group");
const externalGroup = document.getElementById("external-account-group");

toggleMyAccounts.addEventListener("click", function () {
  toggleMyAccounts.classList.add("active");
  toggleExternal.classList.remove("active");
  toAccountSelectGroup.style.display = "flex";
  externalGroup.style.display = "none";
});

toggleExternal.addEventListener("click", function () {
  toggleExternal.classList.add("active");
  toggleMyAccounts.classList.remove("active");
  toAccountSelectGroup.style.display = "none";
  externalGroup.style.display = "flex";
});

loadTransfer();
