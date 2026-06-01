export function generateEmail() {
  const random = Math.random().toString(36).slice(2, 8);
  return `e2e-${Date.now()}-${random}@banklite.test`;
}

export function generatePassword() {
  const random = Math.random().toString(36).slice(2, 8);
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
