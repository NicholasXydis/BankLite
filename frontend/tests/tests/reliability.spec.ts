import { test, expect } from '@playwright/test';
import { testUser } from '../helpers/data';
import { registerUser } from '../helpers/auth';
import { resetDatabase } from '../helpers/database';

test.beforeEach(async () => {
  await resetDatabase();
});

test('should_open_chatbot_send_message_render_response_and_close', async ({ page }) => {
  const user = testUser();
  await registerUser(page, user.email, user.password, user.fullName);
  await page.route('**/api/chat/message', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ response: 'Your balance summary is ready.' })
    });
  });
  await page.getByRole('link', { name: /ai chat|chat ia/i }).click();
  await page.getByPlaceholder(/ask alfred anything|demandez à alfred/i).fill('Summarize my balance');
  await page.getByTestId('chat-send').click();
  await expect(page.getByText('Your balance summary is ready.')).toBeVisible();
  await page.getByRole('button', { name: 'Close chat' }).click();
});
