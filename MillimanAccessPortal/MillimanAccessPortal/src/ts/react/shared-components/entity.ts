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
}

export class EntityHelper {
  public static applyFilter(entity: Entity, filterText: string): boolean {
    const entityTextLower = entity.primaryText.toLowerCase();
    const filterTextLower = filterText.toLowerCase();
    return entityTextLower.indexOf(filterTextLower) !== -1;
  }
}
