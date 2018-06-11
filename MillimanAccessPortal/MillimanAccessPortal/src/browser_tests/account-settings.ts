import { Role, Selector } from 'testcafe';

import { contentUser } from './roles';

fixture('Account settings')
  .page('https://localhost:44336');

test('loads properly', async (t) => {
  const container = Selector('#account-settings-container');
  await t
    .useRole(contentUser)
    .navigateTo('/Account/Settings')
    .expect(container.exists).ok();
});
