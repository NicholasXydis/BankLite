import dotenv from 'dotenv';
import { request } from '@playwright/test';

dotenv.config();

async function globalSetup() {
  const frontend = process.env.E2E_FRONTEND_URL ?? 'http://localhost:5500';
  const api = process.env.E2E_API_URL ?? 'https://localhost:7205';

  const ctx = await request.newContext({ ignoreHTTPSErrors: true });
  try {
    const frontendResp = await ctx.get(`${frontend}/index.html`);
    if (!frontendResp.ok()) {
      throw new Error(`Frontend unavailable at ${frontend}/index.html`);
    }

    const apiResp = await ctx.get(`${api}/health`);
    if (!apiResp.ok()) {
      throw new Error(`API unavailable at ${api}`);
    }
  } finally {
    await ctx.dispose();
  }
}

export default globalSetup;
