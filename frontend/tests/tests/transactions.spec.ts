import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

test.beforeEach(async () => {
  await resetDatabase();
});

async function submitAndExpectOK(page: any, path: string, action: () => Promise<void>) {
  const responsePromise = page.waitForResponse((response) => {
    return response.url().includes(path) && response.request().method() === 'POST';
  });
  await action();
  const response = await responsePromise;
  expect(response.status()).toBeGreaterThanOrEqual(200);
  expect(response.status()).toBeLessThan(300);
}

test('should_render_transaction_list_and_filter_operations', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('70');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await page.goto('/transactions.html');
  await expect(page.getByRole('button', { name: 'Deposits' })).toBeVisible();
  await page.getByRole('button', { name: 'Deposits' }).click();
  await expect(page.getByTestId('transaction-row').getByText(/deposit|dépôt/i)).toBeVisible();
});

test('should_export_csv_and_download_expected_file', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.goto('/deposit.html');
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('70');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());
  await page.goto('/transactions.html');
  const downloadPromise = page.waitForEvent('download');
  await page.getByTestId('export-csv').click();
  const download = await downloadPromise;
  await expect(download.suggestedFilename()).toBe('transactions.csv');
  const filePath = await download.path();
  expect(filePath).toBeTruthy();
  const csv = await fs.readFile(filePath!, 'utf-8');
  expect(csv).toMatch(/Date/);
  expect(csv).toMatch(/Type/);
  expect(csv).toMatch(/Amount/);
  expect(csv).toMatch(/Description/);
});

test('should_translate_transfer_rows_in_french_csv', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());
  await page.getByLabel('Account Type').selectOption('1');
  await submitAndExpectOK(page, '/api/account/create', () => page.getByTestId('create-account-btn').click());

  await page.goto('/deposit.html');
  await page.getByLabel('Amount').fill('300');
  await submitAndExpectOK(page, '/api/transaction/deposit', () => page.getByRole('button', { name: 'Deposit' }).click());

  await page.goto('/transfer.html');
  const savingsOption = (await page.getByLabel('To Account').locator('option').allTextContents())
    .find((option: string) => option.includes('Savings'));
  await page.getByLabel('To Account').selectOption({ label: savingsOption! });
  await page.getByLabel('Amount').fill('100');
  await submitAndExpectOK(page, '/api/transaction/transfer', () => page.getByRole('button', { name: 'Transfer' }).click());

  const exportCsv = async (language: 'en' | 'fr') => {
    await page.goto('/transactions.html');
    await page.evaluate((lang) => localStorage.setItem('language', lang), language);
    await page.reload();
    await expect(page.getByTestId('export-csv')).toBeVisible();
    const downloadPromise = page.waitForEvent('download');
    await page.getByTestId('export-csv').click();
    const filePath = await (await downloadPromise).path();
    return fs.readFile(filePath!, 'utf-8');
  };

  const englishCsv = await exportCsv('en');
  expect(englishCsv).toContain('Internal transfer $100.00');
  expect(englishCsv).toContain('Deposit $300.00');
  expect(englishCsv).not.toContain('Internal Transfer to account');

  const frenchCsv = await exportCsv('fr');
  expect(frenchCsv).toContain('Virement interne $100.00');
  expect(frenchCsv).toContain('Dépôt $300.00');
  expect(frenchCsv).not.toContain('Internal Transfer');
  expect(frenchCsv).not.toContain('External Transfer');
});
