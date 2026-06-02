import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

test.beforeEach(async () => {
  await resetDatabase();
});

async function createAccount(page: any, type: '0' | '1') {
  if (type === '1') {
    await page.getByLabel('Account Type').selectOption('1');
  } else {
    await page.getByLabel('Account Type').selectOption('0');
  }
  const responsePromise = page.waitForResponse((response) => {
    return response.url().includes('/api/account/create') && response.request().method() === 'POST';
  });
  await page.getByTestId('create-account-btn').click();
  const response = await responsePromise;
  expect(response.ok()).toBeTruthy();
}

test('should_render_dashboard_welcome_and_empty_state', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await expect(page.getByText(/welcome/i)).toBeVisible();
  await expect(page.getByTestId('create-account-btn')).toBeVisible();
});

test('should_create_chequing_and_savings_accounts_and_render_cards', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await createAccount(page, '0');
  await expect(page.getByText(/account created successfully/i)).toBeVisible();
  await createAccount(page, '1');
  await expect(page.getByTestId('account-card')).toHaveCount(2);
});

test('should_change_copy_button_state_and_open_transactions_from_account_card', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await createAccount(page, '0');
  const chequingCard = page.getByTestId('account-card').filter({ hasText: 'Chequing' });
  const copyButton = chequingCard.getByTestId('copy-account-number');
  await copyButton.click();
  await expect(copyButton).toHaveCSS('background-color', 'rgb(220, 252, 231)');
  await chequingCard.click();
  await page.waitForURL(/transactions\.html\?accountId=/);
});
