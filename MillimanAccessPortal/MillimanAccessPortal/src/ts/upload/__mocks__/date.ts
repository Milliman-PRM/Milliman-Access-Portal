const ORIGIN = 1577836800000;
const OFFSET = 1000;

export class MockDate {
  private date: number;
  private static _now: number;
  private static get now(): number {
    const now = this._now;
    this._now += OFFSET;
    return now;
  }

  public static reset(offset: number = 0) {
    MockDate._now = ORIGIN + offset;
  }

  constructor() {
    this.date = MockDate.now;
  }

  public getTime(): number {
    return this.date;
  }
}
