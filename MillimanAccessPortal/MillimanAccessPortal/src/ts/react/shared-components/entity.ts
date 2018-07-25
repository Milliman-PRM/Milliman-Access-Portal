// Represents an object displayable on a card
export class Entity {
  public readonly id: number;
  public readonly detailList: string[];

  private _primaryText: string;
  private _primaryTextLower: string;
  public get primaryText() {
    return this._primaryText;
  }
  public set primaryText(value: string) {
    this._primaryText = value;
    this._primaryTextLower = value.toLowerCase();
  }
  public get primaryTextLower() {
    return this._primaryTextLower;
  }

  public constructor(id: number, primaryText: string, detailList?: string[]) {
    this.id = id;
    this.primaryText = primaryText;
    this.detailList = detailList;
  }

  public applyFilter(filterText: string): boolean {
    const filterTextLower = filterText.toLowerCase();
    return this.primaryTextLower.indexOf(filterTextLower) !== -1;
  }
}
