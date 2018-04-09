import testcafe = require('testcafe');

fixture('Login')
  .page('https://localhost:44336');

test('loads properly', async (t) => {
  const loginContainer = testcafe.Selector('#login-container');
  await t.expect(loginContainer.exists).ok();
});
