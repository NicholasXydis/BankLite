import { test, expect } from '@playwright/test';

test('should_serve_branded_404_page_with_404_status_for_unknown_paths', async ({ page }) => {
  const response = await page.goto('/this-page-does-not-exist');

  expect(response?.status()).toBe(404);
  await expect(page).toHaveTitle(/Page Not Found/i);
  await expect(page.getByText('404')).toBeVisible();
});

test('should_not_expose_internal_configuration_files', async ({ request }) => {
  for (const path of ['/nginx.conf', '/nginx-main.conf', '/Dockerfile']) {
    const response = await request.get(path);
    const body = await response.text();

    expect(response.status()).toBe(404);
    expect(body).not.toContain('proxy_pass');
    expect(body).not.toContain('listen 8080');
    expect(body).not.toContain('FROM nginx');
  }
});

test('should_still_serve_known_pages_and_assets', async ({ request }) => {
  for (const path of [
    '/',
    '/index.html',
    '/landing.html',
    '/dashboard.html',
    '/deposit.html',
    '/transfer.html',
    '/js/api.js',
    '/robots.txt',
  ]) {
    const response = await request.get(path);
    expect(response.status(), `expected 200 for ${path}`).toBe(200);
  }
});

test('should_not_replace_api_error_responses_with_html_error_pages', async ({ request }) => {
  const unauthorized = await request.get('/api/account');
  expect(unauthorized.status()).toBe(401);
  const unauthorizedBody = await unauthorized.text();
  expect(unauthorizedBody).not.toContain('<html');
  expect(unauthorizedBody).not.toContain('Page Not Found');

  const unknownRoute = await request.get('/api/does-not-exist');
  const unknownBody = await unknownRoute.text();
  expect(unknownRoute.status()).toBe(404);
  expect(unknownBody).not.toContain('Page Not Found');
  expect(unknownBody).not.toContain('Server Error');
});
