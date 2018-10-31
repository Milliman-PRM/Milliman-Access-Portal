import { EntityInfo, isRootContentItemInfo, isUserInfo, isClientInfo, isProfitCenterInfo } from '../system-admin/interfaces';
import { Guid } from './interfaces';

// Represents an object displayable on a card

export interface CardStat {
  value: number;
  name: string;
  icon: string;
}

export interface Entity {
  id: Guid;
  primaryText: string;
  secondaryText?: string;
  primaryStat?: CardStat;
  secondaryStat?: CardStat;
  sublist?: Entity[];
  indent?: number;
  readOnly?: boolean;
  activated?: boolean;
  suspended?: boolean;
  email?: string;
  isProfitCenter?: boolean;
  isUserInProfitCenter?: number;
}

export class EntityHelper {
  public static applyFilter(entity: EntityInfo, filterText: string): boolean {
    const filterTextLower = filterText.toLowerCase();
    const primaryText = isUserInfo(entity)
      ? `${entity.FirstName} ${entity.LastName}`
      : entity.Name;
    const secondaryText = isUserInfo(entity)
      ? entity.UserName
      : isRootContentItemInfo(entity)
        ? entity.ClientName
        : (isClientInfo(entity) || isProfitCenterInfo(entity))
          ? entity.Code
          : '';
    const primaryMatch = primaryText
      ? primaryText.toLowerCase().indexOf(filterTextLower) !== -1
      : false;
    const secondaryMatch = secondaryText
      ? secondaryText.toLowerCase().indexOf(filterTextLower) !== -1
      : false;
    return primaryMatch || secondaryMatch;
  }
}
