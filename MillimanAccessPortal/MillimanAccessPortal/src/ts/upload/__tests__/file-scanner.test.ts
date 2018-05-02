import { FileScanner } from '../file-scanner';
import { MockFile, MockBlob, MockFileReader } from '../__mocks__/file';


jest.useFakeTimers();

describe('file scanner', () => {
  const RealFileReader = FileReader;
  const scanCallback = jest.fn();

  beforeEach(() => {
    (global as any).FileReader = MockFileReader;
  });
  afterEach(() => {
    (global as any).FileReader = RealFileReader;

    scanCallback.mockClear();
  });

  it('scans files', async () => {
    const mockFile = new MockFile('foobar');
    const scanner = new FileScanner(4);

    scanner.open(mockFile as any);
    expect(scanner.scan(scanCallback)).resolves.toBe(6);

    expect(scanCallback).toHaveBeenCalledTimes(2);
    expect(scanCallback.mock.calls.map((call) => call[0]))
      .toEqual(['foob', 'ar']);

    scanner.open(mockFile as any);
    expect(scanner.scan(scanCallback)).resolves.toBe(6);
    expect(scanCallback).toHaveBeenCalledTimes(4);
  });
  it('tracks scan progress', async () => {
    const mockFile = new MockFile('foobar');
    const scanner = new FileScanner(4);

    expect(scanner.progress).toBe(0);
    scanner.open(mockFile as any);
    expect(scanner.progress).toBe(0);
    expect(scanner.scan(scanCallback)).resolves.toBe(6);
    expect(scanner.progress).toBe(1);
  });
  it('cancels a scan', () => {
    const mockFile = new MockFile('foobar');
    const scanner = new FileScanner(4);

    scanner.open(mockFile as any);
    expect(scanner.scan(() => scanner.cancel())).rejects.toBe('Scan cancelled');
    expect(scanner.progress).toBe(0);
  });
});
