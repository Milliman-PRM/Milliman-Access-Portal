// Mock a file as a string
export class MockFile {
  public get size(): number {
    return this.contents.length;
  }
  constructor(readonly contents: string) { }

  public slice(start: number, end: number): MockBlob {
    return new MockBlob(this.contents.substring(start, end));
  }
}

export class MockBlob {
  constructor(readonly contents: string) { }
}

export class MockFileReader {
  public onload: (ev: any) => void;
  public onerror: (ev: any) => void;

  constructor() {
    this.onload = () => {};
    this.onerror = () => {};
  }

  public readAsBinaryString(blob: MockBlob) {
    const event = {
      target: {
        result: blob.contents
      }
    };
    this.onload(event);
    this.onerror(event);
  }
}
