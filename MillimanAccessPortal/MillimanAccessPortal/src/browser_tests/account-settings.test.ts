import { Role, Selector } from 'testcafe';

import { contentUser } from './roles.test';

fixture('Account settings')
  .page('https://localhost:44336');

test('loads properly', async (t) => {
  const container = Selector('#account-settings-container');
  await t
    .useRole(contentUser)
    .navigateTo('/Account/Settings')
    .expect(container.exists).ok();
});

test('reacts to changed inputs', async (t) => {
  const buttons = Selector('.button-container-update');
  await t
    .useRole(contentUser)
    .navigateTo('/Account/Settings')
    .typeText('#UserName', 'readonly, nothing should happen')
    .expect(buttons.visible).notOk()
    .typeText('#PhoneNumber', '123')
    .expect(buttons.visible).ok()
    .click('button.button-reset')
    .click('button.vex-first')
    .expect(buttons.visible).notOk()
    .typeText('#CurrentPassword', 'notapassword')
    .expect(buttons.visible).ok();
});

test('accepts updates', async (t) => {
  const buttons = Selector('.button-container-update');
  const container = () => Selector('#toast-container');
  await t
    .useRole(contentUser)
    .navigateTo('/Account/Settings')

    .typeText('#LastName', '123')
    .click('button.button-submit')
    .expect(container().childElementCount).eql(1)
    .expect(container().find('.toast-success .toast-message').innerText)
      .eql('Your account has been updated')
    .expect(buttons.visible).notOk();
});
