import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase, seedExternalRecipientAccount } from '../helpers/database';

async function submitAndExpectOK(page: any, path: string, action: () => Promise<void>) {
  const responsePromise = page.waitForResponse((response) => {
    return response.url().includes(path) && response.request().method() === 'POST';
  });
  await action();
  const response = await responsePromise;
  expect(response.status()).toBeGreaterThanOrEqual(200);
  expect(response.status()).toBeLessThan(300);
}

async function createAccounts(page: any) {
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.getByLabel('Account Type').selectOption('1');
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
}

test.beforeEach(async () => {
  await resetDatabase();
});

test('should_deposit_and_update_displayed_balance', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await createAccounts(page);
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('100');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await expect(page.getByText(/successfully deposited/i)).toBeVisible();
  await expect(page.getByLabel('Select Account')).toContainText('$100.00');
});

test('should_withdraw_and_update_displayed_balance', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('200');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await page.goto('/withdraw.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('50');
  await submitAndExpectOK(page, '/api/transaction/withdraw', () => page.getByRole('button', { name: 'Withdraw' }).click());
  await expect(page.getByText(/successfully withdrew/i)).toBeVisible();
  await expect(page.getByLabel('Select Account')).toContainText('$150.00');
});

test('should_transfer_between_internal_accounts_and_update_balances', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await createAccounts(page);
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('300');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await page.goto('/transfer.html');
  await expect(page.getByLabel('From Account')).toBeVisible();
  const savingsOption = (await page.getByLabel('To Account').locator('option').allTextContents())
    .find((option: string) => option.includes('Savings'));
  expect(savingsOption).toBeTruthy();
  await page.getByLabel('To Account').selectOption({ label: savingsOption! });
  await page.getByLabel('Amount').fill('100');
  await submitAndExpectOK(page, '/api/transaction/transfer', () => page.getByRole('button', { name: 'Transfer' }).click());
  await expect(page.getByText(/successfully transferred/i)).toBeVisible();
  await expect(page.getByLabel('From Account')).toContainText('$200.00');
  await expect(page.getByLabel('To Account')).toContainText('$100.00');
});

test('should_transfer_to_external_account_successfully', async ({ page }) => {
  const recipientAccountNumber = await seedExternalRecipientAccount();
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('300');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await page.goto('/transfer.html');
  await expect(page.getByLabel('From Account')).toBeVisible();
  await page.getByRole('button', { name: /send to someone/i }).click();
  await page.getByLabel('Recipient Account Number').fill(recipientAccountNumber);
  await page.getByLabel('Amount').fill('25');
  await submitAndExpectOK(page, '/api/transaction/transferexternal', () => page.getByRole('button', { name: 'Transfer' }).click());
  await expect(page.getByText(/successfully transferred/i)).toBeVisible();
  await expect(page.getByLabel('From Account')).toContainText('$275.00');
});
