import * as shared from '../shared';

interface User {
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
}

test('matches user substring', () => {
  const users: User[] = [
    {
      email: 'email1@client.zxc',
      firstName: 'John',
      lastName: 'Smith',
      userName: 'username1',
    },
    {
      email: 'email2@client.zxc',
      firstName: 'Mike',
      lastName: 'Adams',
      userName: 'username2',
    },
  ];
  const matcher: (query: string, callback: (matches: any) => void) => void
    = shared.userSubstringMatcher(users);
  let matches: User[];

  matcher('query', (m) => { matches = m; });
  expect(matches.length).toBe(0);

  matcher('John Smith', (m) => { matches = m; });
  expect(matches.length).toBe(1);

  matcher('email', (m) => { matches = m; });
  expect(matches.length).toBe(2);
});
