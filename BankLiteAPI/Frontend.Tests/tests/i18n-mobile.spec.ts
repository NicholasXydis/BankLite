import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

test.beforeEach(async () => {
  await resetDatabase();
});

test('should_persist_french_across_pages_and_translate_labels_and_validation', async ({ page }) => {
  await page.goto('/index.html');
  await page.getByRole('button', { name: 'Change language' }).click();
  await expect(page.getByRole('button', { name: /se connecter/i })).toBeVisible();
  await page.getByRole('button', { name: /se connecter/i }).click();
  await expect(page.getByText(/veuillez saisir votre adresse e-mail/i)).toBeVisible();
  await page.goto('/register.html');
  await expect(page.getByRole('button', { name: /s'inscrire/i })).toBeVisible();
});

test('should_work_on_mobile_navigation_settings_and_form_without_horizontal_overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  const createResponse = page.waitForResponse((response) => {
    return response.url().includes('/api/account/create') && response.request().method() === 'POST';
  });
  await page.getByTestId('create-account-btn').click();
  const response = await createResponse;
  expect(response.status()).toBeGreaterThanOrEqual(200);
  expect(response.status()).toBeLessThan(300);
  await page.getByLabel('Toggle navigation').click();
  await page.getByRole('link', { name: /deposit/i }).click();
  await expect(page.getByLabel('Select Account')).toBeVisible();
  await page.getByLabel('Amount').fill('10');
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflow).toBeFalsy();
});
