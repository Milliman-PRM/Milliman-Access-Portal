import { RetainedValue } from '../retained-value';

describe('retained value', () => {
  it('retains values', () => {
    const rv = new RetainedValue<number>(4);
    [1, 2, 3, 4, 5].forEach((value) => rv.insert(value));
    expect(rv.lengthLimit).toEqual(4);
    expect(rv.now).toBe(5);
    expect(rv.ref).toBe(2);
    expect(rv.values).toEqual([5, 4, 3, 2]);
  });
  it('allows a length limit of 0', () => {
    const rv = new RetainedValue<number>(0);
    [1].forEach((value) => rv.insert(value));
    expect(rv.now).toBeUndefined();
    expect(rv.ref).toBeUndefined();
    expect(rv.values).toEqual([]);
  });
  it('is resetable', () => {
    const rv = new RetainedValue<number>(2);
    [1].forEach((value) => rv.insert(value));
    expect(rv.now).toBe(1);
    rv.reset();
    expect(rv.now).toBeUndefined();
  });
});
