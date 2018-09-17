const ORIGIN = 1577836800000; // Jan 1, 2020 GMT
const OFFSET = 1000;

export class MockDate {
  public static reset(offset: number = 0) {
    MockDate._now = ORIGIN + offset;
  }
  private static _now: number;
  private static get now(): number {
    const now = this._now;
    this._now += OFFSET;
    return now;
  }

  private date: number;

  constructor() {
    this.date = MockDate.now;
  }

  public getTime(): number {
    return this.date;
  }
}
