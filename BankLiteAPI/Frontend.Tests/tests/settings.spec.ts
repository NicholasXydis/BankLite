import { test, expect } from '@playwright/test';
import { testUser, generatePassword } from '../helpers/data';
import { registerUser, loginUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

test.beforeEach(async () => {
  await resetDatabase();
});

test('should_open_settings_render_profile_and_legal_links', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.getByRole('link', { name: /settings/i }).click();
  await expect(page.getByText('Profile')).toBeVisible();
  await expect(page.getByText(user.fullName)).toBeVisible();
  await expect(page.getByText(user.email)).toBeVisible();
  await page.getByRole('link', { name: 'Privacy Policy' }).click();
  await page.waitForURL('**/privacy.html');
  await expect(page.getByRole('heading', { name: 'Privacy Policy' })).toBeVisible();
  await page.goto('/dashboard.html');
  await page.getByRole('link', { name: /settings/i }).click();
  await page.getByRole('link', { name: 'Terms of Service' }).click();
  await page.waitForURL('**/terms.html');
  await expect(page.getByRole('heading', { name: 'Terms of Service' })).toBeVisible();
});

test('should_persist_dark_mode_after_reload', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.getByRole('link', { name: /settings/i }).click();
  await page.getByTestId('dark-mode-control').click();
  await expect(page.getByTestId('dark-mode-toggle')).toBeChecked();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.locator('body')).toHaveClass(/dark-mode/);
});

test('should_change_password_and_allow_login_with_new_password', async ({ page }) => {
  const user = testUser();
  const newPassword = generatePassword();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.getByRole('link', { name: /settings/i }).click();
  await page.getByLabel('Current Password').fill(user.password);
  await page.getByLabel('New Password').fill(newPassword);
  await page.getByRole('button', { name: 'Change Password' }).click();
  await expect(page.getByText(/password changed successfully/i)).toBeVisible();
  await page.getByRole('button', { name: 'Close settings' }).click();
  await page.getByRole('button', { name: 'Logout' }).click();
  await page.getByRole('button', { name: 'Log Out' }).click();
  await page.waitForURL('**/index.html');
  await loginUser(page, user.email, newPassword);
  await expect(page).toHaveURL(/dashboard\.html/);
});
