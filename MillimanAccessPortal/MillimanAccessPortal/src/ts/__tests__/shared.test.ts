import * as shared from '../shared';

interface User {
  Email: string;
  UserName: string;
  FirstName: string;
  LastName: string;
};

test('matches user substring', () => {
  const users: Array<User> = [
    {
      Email: 'email1@client.zxc',
      UserName: 'username1',
      FirstName: 'John',
      LastName: 'Smith',
    },
    {
      Email: 'email2@client.zxc',
      UserName: 'username2',
      FirstName: 'Mike',
      LastName: 'Adams',
    },
  ];
  const matcher: Function = shared.userSubstringMatcher(users);
  let matches: Array<User>;

  matcher('query', (m) => { matches = m; });
  expect(matches.length).toBe(0);

  matcher('John Smith', (m) => { matches = m; });
  expect(matches.length).toBe(1);

  matcher('email', (m) => { matches = m; });
  expect(matches.length).toBe(2);
});
