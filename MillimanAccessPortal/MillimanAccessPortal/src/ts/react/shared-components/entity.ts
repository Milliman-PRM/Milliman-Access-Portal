// Represents an object displayable on a card

export interface CardStat {
  value: number;
  name: string;
  icon: string;
}

export interface Entity {
  id: number;
  primaryText: string;
  secondaryText?: string;
  primaryStat?: CardStat;
  secondaryStat?: CardStat;
  detailList?: string[];
  indent?: number;
  readOnly?: boolean;
  activated?: boolean;
}

export class EntityHelper {
  public static applyFilter(entity: Entity, filterText: string): boolean {
    const filterTextLower = filterText.toLowerCase();
    const primaryMatch = entity.primaryText
      ? entity.primaryText.toLowerCase().indexOf(filterTextLower) !== -1
      : false;
    const secondaryMatch = entity.secondaryText
      ? entity.secondaryText.toLowerCase().indexOf(filterTextLower) !== -1
      : false;
    return primaryMatch || secondaryMatch;
  }
}
