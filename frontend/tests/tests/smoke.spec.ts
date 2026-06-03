import { test, expect } from '@playwright/test';

test('should_load_public_pages_without_uncaught_js_errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/');
  await expect(page.locator('body.lp-page')).toBeVisible();
  await page.goto('/landing.html');
  await page.goto('/index.html');
  await page.getByRole('link', { name: 'Register' }).click();
  await page.waitForURL('**/register.html');
  expect(errors).toEqual([]);
});

test('should_navigate_between_landing_login_register', async ({ page }) => {
  await page.goto('/landing.html');
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL('**/index.html');
  await page.getByRole('link', { name: 'Register' }).click();
  await page.waitForURL('**/register.html');
  await page.getByRole('link', { name: 'Sign in' }).click();
  await page.waitForURL('**/index.html');
});

test('should_redirect_unauthenticated_users_from_protected_pages', async ({ page }) => {
  await page.goto('/dashboard.html');
  await page.waitForURL('**/index.html');
});
