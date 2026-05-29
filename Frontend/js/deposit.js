async function loadDeposit() {
    requireAuth();

    const accountSelect = document.getElementById("account-select");
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
            option.textContent = `${account.type === "Chequing" ? t("dashboard_chequing") : t("dashboard_savings")} | $${account.balance.toLocaleString("en-CA", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            })}`;
            accountSelect.appendChild(option);
        });
    } catch (error) {
        errorMsg.textContent = error.message.includes("entity")
            ? t("error_occurred")
            : error.message;
    }
}

document
    .getElementById("deposit-btn")
    .addEventListener("click", async function () {
        requireAuth();

        const errorMsg = document.getElementById("error-msg");
        const successMsg = document.getElementById("success-msg");
        const accountId = document.getElementById("account-select").value;
        const amount = parseFloat(
            document.getElementById("amount").value.replace(/[$,]/g, ""),
        );

        errorMsg.style.display = "none";
        successMsg.style.display = "none";

        if (!accountId) {
            errorMsg.textContent = t("error_select_account");
            errorMsg.style.display = "block";
            return;
        }

        if (!amount || amount <= 0) {
            errorMsg.textContent = t("error_valid_amount");
            errorMsg.style.display = "block";
            return;
        }

        if (amount > 1000000) {
            errorMsg.textContent = t("error_max_deposit");
            errorMsg.style.display = "block";
            return;
        }

        const btn = document.getElementById("deposit-btn");
        btn.disabled = true;
        btn.classList.add("btn-loading");

        try {
            await deposit(accountId, amount);
            successMsg.textContent = `${t("success_deposited")} $${amount.toLocaleString("en-CA", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            })}!`;
            successMsg.style.display = "block";

            setTimeout(() => {
                successMsg.style.display = "none";
            }, 3000);

            const selectEl = document.getElementById("account-select");
            selectEl.classList.add("flash-green");
            setTimeout(() => selectEl.classList.remove("flash-green"), 1000);
            const selectedId = accountId;
            await getAccounts(true);
            await loadDeposit();
            document.getElementById("account-select").value = selectedId;

            const isFirstDeposit = !sessionStorage.getItem(
                `hasDeposited_${accountId}`,
            );
            if (isFirstDeposit) {
                sessionStorage.setItem(`hasDeposited_${accountId}`, "true");
                const sidebarWidth = window.innerWidth <= 768 ? 0 : 220;
                const centerX =
                    (sidebarWidth + (window.innerWidth - sidebarWidth) / 2) /
                    window.innerWidth;
                confetti({
                    particleCount: 150,
                    spread: 100,
                    origin: {x: centerX, y: 0.6},
                    colors: ["#1a3a5c", "#4f86c6", "#ffffff", "#FFD700", "#e2e8f0"],
                });
                setTimeout(
                    () =>
                        confetti({
                            particleCount: 80,
                            angle: 60,
                            spread: 80,
                            origin: {x: 0, y: 0.6},
                        }),
                    200,
                );
                setTimeout(
                    () =>
                        confetti({
                            particleCount: 80,
                            angle: 120,
                            spread: 80,
                            origin: {x: 1, y: 0.6},
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

document.getElementById("amount").addEventListener("input", function () {
    let raw = this.value.replace(/[^0-9.]/g, "");
    const parts = raw.split(".");
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    if (parts.length > 2) parts.splice(2);
    if (parts[1] !== undefined) parts[1] = parts[1].slice(0, 2);
    this.value = raw === "" ? "" : "$" + parts.join(".");
});

loadDeposit();
