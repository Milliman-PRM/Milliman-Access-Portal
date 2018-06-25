import * as shared from '../shared';

interface User {
  Email: string;
  UserName: string;
  FirstName: string;
  LastName: string;
}

test('matches user substring', () => {
  const users: User[] = [
    {
      Email: 'email1@client.zxc',
      FirstName: 'John',
      LastName: 'Smith',
      UserName: 'username1',
    },
    {
      Email: 'email2@client.zxc',
      FirstName: 'Mike',
      LastName: 'Adams',
      UserName: 'username2',
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
