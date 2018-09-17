import { MockDate } from '../__mocks__/date';
import { ProgressMonitor, ProgressSummary } from '../progress-monitor';

describe('progress monitor', () => {
  // tslint:disable-next-line:variable-name
  const RealDate = Date;

  const progressCallback = jest.fn();
  const renderCallback = jest.fn();

  beforeEach(() => {
    jest.useFakeTimers();

    global.Date = MockDate as any;
    MockDate.reset();

    progressCallback.mockReturnValueOnce(0.0);
    progressCallback.mockReturnValueOnce(0.2);
    progressCallback.mockReturnValueOnce(0.4);
    progressCallback.mockReturnValueOnce(0.6);
    progressCallback.mockReturnValueOnce(0.8);
    progressCallback.mockReturnValue(1.0);
  });
  afterEach(() => {
    global.Date = RealDate;

    jest.runAllTimers(); // drain remaining timer callbacks

    jest.clearAllMocks();
    progressCallback.mockReset();
  });

  it('reads progress from callback', () => {
    const monitor = new ProgressMonitor(
      progressCallback, renderCallback, 1000000);
    monitor.activate();

    expect(progressCallback.mock.calls.length).toBe(1);
    expect(progressCallback.mock.calls[0].length).toBe(0);
  });
  it('renders progress', () => {
    const monitor = new ProgressMonitor(
      progressCallback, renderCallback, 1000000);
    monitor.activate();
    jest.runOnlyPendingTimers();

    expect(renderCallback.mock.calls.length).toBe(2);
    expect(renderCallback.mock.calls[0].length).toBe(1);
    expect(renderCallback.mock.calls).toMatchSnapshot();
  });
  it('stops monitoring when told', () => {
    const monitor = new ProgressMonitor(
      progressCallback, renderCallback, 1000000);

    monitor.activate();

    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    monitor.deactivate();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();

    expect(setTimeout).toHaveBeenCalledTimes(3);
    expect(renderCallback.mock.calls.map(
      (call) => (call[0] as ProgressSummary).percentage,
    )).toEqual(['0%', '20%', '40%']);
  });
  it('stops monitoring when complete', () => {
    const monitor = new ProgressMonitor(
      progressCallback, renderCallback, 1000000);
    monitor.activate();

    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();
    jest.runOnlyPendingTimers();

    expect(setTimeout).toHaveBeenCalledTimes(6);
    expect(renderCallback.mock.calls.map(
      (call) => (call[0] as ProgressSummary).percentage,
    )).toEqual(['0%', '20%', '40%', '60%', '80%', '100%']);
  });
});
