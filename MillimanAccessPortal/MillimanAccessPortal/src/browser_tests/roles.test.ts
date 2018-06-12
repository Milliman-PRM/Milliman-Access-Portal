import { Role } from 'testcafe';

export const contentUser = Role('https://localhost:44336/Account/Login', async (t) => {
  await t
    .typeText('#email', 'contentUser@domain.domain')
    .typeText('#password', 'Password!0')
    .click('button[type="submit"]');
});
