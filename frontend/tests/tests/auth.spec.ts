import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser, loginUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

const apiBase = process.env.E2E_API_URL ?? 'https://localhost:7205';

test.beforeEach(async () => {
  await resetDatabase();
});

test('should_register_through_ui_and_reach_dashboard', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await expect(page.getByText(/welcome/i)).toBeVisible();
});

test('should_login_through_ui_and_reach_dashboard', async ({ page, request }) => {
  const user = testUser();
  const registerResponse = await request.post(`${apiBase}/api/auth/register`, { data: user });
  expect(registerResponse.ok()).toBeTruthy();
  await loginUser(page, user.email, user.password);
  await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
});

test('should_open_forgot_password_validate_empty_and_submit', async ({ page }) => {
  await page.route('**/api/auth/forgot-password', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'If that email exists, a reset link has been sent.' })
    });
  });
  await page.goto('/index.html');
  await page.getByRole('link', { name: /forgot password/i }).click();
  await page.getByRole('button', { name: 'Send Reset Link' }).click();
  await expect(page.getByText(/please enter your email/i)).toBeVisible();
  await page.getByTestId('forgot-email-input').fill('nobody@banklite.test');
  await page.getByRole('button', { name: 'Send Reset Link' }).click();
  await expect(page.getByText(/reset link has been sent/i)).toBeVisible();
});

test('should_keep_session_on_logout_cancel_and_return_to_login_on_confirm', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.getByRole('button', { name: 'Logout' }).click();
  await page.getByRole('button', { name: 'Cancel' }).click();
  await expect(page).toHaveURL(/dashboard\.html/);
  await page.getByRole('button', { name: 'Logout' }).click();
  await page.getByRole('button', { name: 'Log Out' }).click();
  await page.waitForURL('**/index.html');
});
