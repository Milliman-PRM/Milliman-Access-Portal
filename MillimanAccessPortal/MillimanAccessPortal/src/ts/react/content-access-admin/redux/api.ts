import { ClientWithEligibleUsers } from '../../models';

export function fetchClients() {
  return [
    { id: 'client1', name: 'client1', code: 'c1', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
    { id: 'client2', name: 'client2', code: 'c2', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
    { id: 'client3', name: 'client3', code: 'c3', eligibleUsers: ['user1', 'user2', 'user3'] },
    { id: 'client4', name: 'client4', code: 'c4', eligibleUsers: [] },
  ];
}
