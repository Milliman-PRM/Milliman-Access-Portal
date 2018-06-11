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

test('is interactive', async (t) => {
  const buttons = Selector('.button-container-update');
  await t
    .useRole(contentUser)
    .navigateTo('/Account/Settings')
    .typeText('#UserName', 'readonly, nothing should happen')
    .expect(buttons.visible).notOk()
    .typeText('#PhoneNumber', '123')
    .expect(buttons.visible).ok();
});
