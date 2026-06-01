import { APIRequestContext, Page, expect } from '@playwright/test';

const apiBase = process.env.E2E_API_URL ?? 'https://localhost:7205';

export async function registerUser(page: Page, email: string, password: string, fullName = 'End To End User') {
  await page.goto('/register.html');
  await page.getByLabel('Full Name').fill(fullName);
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByLabel('Confirm Password').fill(password);
  await page.getByRole('button', { name: 'Register' }).click();
  await page.waitForURL('**/dashboard.html');
}

export async function loginUser(page: Page, email: string, password: string) {
  await page.goto('/index.html');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL('**/dashboard.html');
}

export async function loginViaAPI(request: APIRequestContext, email: string, password: string) {
  const response = await request.post(`${apiBase}/api/auth/login`, {
    data: { email, password }
  });
  expect(response.ok()).toBeTruthy();
  return response;
}

export async function logout(page: Page) {
  await page.getByRole('button', { name: 'Logout' }).click();
  await page.getByRole('button', { name: 'Log Out' }).click();
  await page.waitForURL('**/index.html');
}
