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
  public contents: Uint8Array;
  constructor(contents: string) {
    const buffer = new ArrayBuffer(contents.length);
    this.contents = new Uint8Array(buffer);
    for (let i = 0; i < contents.length; i += 1) {
      this.contents[i] = contents.charCodeAt(i);
    }
  }
}

export class MockFileReader {
  public onload: (ev: any) => void;
  public onerror: (ev: any) => void;

  constructor() {
    this.onload = () => undefined;
    this.onerror = () => undefined;
  }

  public readAsArrayBuffer(blob: MockBlob) {
    const event = {
      target: {
        result: blob.contents,
      }
    };
    this.onload(event);
    this.onerror(event);
  }
}
