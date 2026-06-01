import dotenv from 'dotenv';
import { randomUUID } from 'node:crypto';
import { Client } from 'pg';

dotenv.config();

const connectionString = process.env.E2E_DATABASE_URL;

export async function resetDatabase() {
  if (!connectionString) {
    throw new Error('E2E_DATABASE_URL is required for database reset');
  }

  const client = new Client({ connectionString });
  try {
    await client.connect();

    const tablesResult = await client.query(`
      select tablename
      from pg_tables
      where schemaname = 'public'
        and tablename <> '__EFMigrationsHistory'
    `);

    const tables = tablesResult.rows.map((r) => `"public"."${r.tablename}"`);
    if (tables.length > 0) {
      await client.query(`TRUNCATE TABLE ${tables.join(', ')} RESTART IDENTITY CASCADE`);
    }
  } finally {
    await client.end();
  }
}

export async function seedExternalRecipientAccount() {
  if (!connectionString) {
    throw new Error('E2E_DATABASE_URL is required for database seed');
  }

  const client = new Client({ connectionString });
  const userId = randomUUID();
  const accountId = randomUUID();
  const accountNumber = 'E2ERECIP0001';

  try {
    await client.connect();
    await client.query(
      `
        insert into "Users" ("Id", "FullName", "Email", "PasswordHash", "CreatedAt", "FailedLoginAttempts")
        values ($1, $2, $3, $4, now(), 0)
      `,
      [userId, 'External Recipient', `recipient-${userId}@banklite.test`, 'not-used-by-e2e']
    );
    await client.query(
      `
        insert into "Accounts" ("Id", "UserId", "AccountNumber", "Type", "Balance", "CreatedAt")
        values ($1, $2, $3, 0, 0, now())
      `,
      [accountId, userId, accountNumber]
    );
    return accountNumber;
  } finally {
    await client.end();
  }
}
