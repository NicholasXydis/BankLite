import { randomBytes } from 'node:crypto';

export function generateEmail() {
  const random = randomBytes(6).toString('hex');
  return `e2e-${Date.now()}-${random}@banklite.test`;
}

export function generatePassword() {
  const random = randomBytes(6).toString('hex');
  return `Test@${random}123!`;
}

export function testUser() {
  const password = generatePassword();
  return {
    fullName: 'End To End User',
    email: generateEmail(),
    password
  };
}
