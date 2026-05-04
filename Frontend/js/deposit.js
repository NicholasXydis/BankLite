async function loadDeposit() {
  const token = requireAuth();
  if (!token) return;

  const accountSelect = document.getElementById("account-select");
  const errorMsg = document.getElementById("error-msg");
  const successMsg = document.getElementById("success-msg");
  let accounts = [];

  try {
    accounts = await getAccounts(token);
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
      option.textContent = `${account.type}  $${account.balance.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
      accountSelect.appendChild(option);
    });
  } catch (error) {
    errorMsg.textContent = error.message.includes("entity")
      ? "An error occurred. Please try again."
      : error.message;
    errorMsg.style.display = "block";
  }
}

document
  .getElementById("deposit-btn")
  .addEventListener("click", async function () {
    const token = requireAuth();
    if (!token) return;

    const errorMsg = document.getElementById("error-msg");
    const successMsg = document.getElementById("success-msg");
    const accountId = document.getElementById("account-select").value;
    const amount = parseFloat(document.getElementById("amount").value);

    errorMsg.style.display = "none";
    successMsg.style.display = "none";

    if (!accountId) {
      errorMsg.textContent = "Please select an account.";
      errorMsg.style.display = "block";
      return;
    }

    if (!amount || amount <= 0) {
      errorMsg.textContent = "Please enter a valid amount.";
      errorMsg.style.display = "block";
      return;
    }

    if (amount > 1000000) {
      errorMsg.textContent = "Maximum deposit amount is $1,000,000.";
      errorMsg.style.display = "block";
      return;
    }

    const btn = document.getElementById("deposit-btn");
    btn.disabled = true;
    btn.classList.add("btn-loading");

    try {
      await deposit(token, accountId, amount);
      successMsg.textContent = `Successfully deposited $${amount.toLocaleString("en-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}!`;
      successMsg.style.display = "block";

      setTimeout(() => {
        successMsg.style.display = "none";
      }, 3000);

      const selectEl = document.getElementById("account-select");
      selectEl.classList.add("flash-green");
      setTimeout(() => selectEl.classList.remove("flash-green"), 1000);
      const selectedId = accountId;
      await loadDeposit();
      document.getElementById("account-select").value = selectedId;

      const isFirstDeposit = !sessionStorage.getItem("hasDeposited");
      if (isFirstDeposit) {
        sessionStorage.setItem("hasDeposited", "true");
        confetti({
          particleCount: 150,
          spread: 100,
          origin: { y: 0.6 },
          colors: ["#1a3a5c", "#4f86c6", "#ffffff", "#FFD700", "#e2e8f0"],
        });
        setTimeout(
          () =>
            confetti({
              particleCount: 80,
              angle: 60,
              spread: 80,
              origin: { x: 0, y: 0.6 },
            }),
          200,
        );
        setTimeout(
          () =>
            confetti({
              particleCount: 80,
              angle: 120,
              spread: 80,
              origin: { x: 1, y: 0.6 },
            }),
          400,
        );
      }

      document.getElementById("amount").value = "";
    } catch (error) {
      errorMsg.textContent = error.message;
      errorMsg.style.display = "block";
    } finally {
      btn.disabled = false;
      btn.classList.remove("btn-loading");
    }
  });
loadDeposit();
