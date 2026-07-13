import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

const publicPages = ['/', '/index.html', '/register.html', '/privacy.html', '/terms.html', '/404.html'];

const authenticatedPages = [
  '/dashboard.html',
  '/deposit.html',
  '/withdraw.html',
  '/transfer.html',
  '/transactions.html'
];

const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'];

async function scan(page: import('@playwright/test').Page) {
  const { violations } = await new AxeBuilder({ page }).withTags(wcagTags).analyze();
  return violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact,
    targets: violation.nodes.map((node) => node.target)
  }));
}

test.beforeEach(async () => {
  await resetDatabase();
});

for (const path of publicPages) {
  test(`public page ${path} has no accessibility violations`, async ({ page }) => {
    await page.goto(path);
    await page.waitForLoadState('networkidle');

    expect(await scan(page)).toEqual([]);
  });
}

test('authenticated pages have no accessibility violations', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.getByTestId('create-account-btn').click();
  await page.waitForResponse((response) => response.url().includes('/api/account/create'));

  for (const path of authenticatedPages) {
    await page.goto(path);
    await page.waitForLoadState('networkidle');

    expect(await scan(page), `violations on ${path}`).toEqual([]);
  }
});
