import * as forge from 'node-forge';

interface FileReaderOnLoadEventTarget extends EventTarget {
  result: BinaryType;
}

class FileSlicer {
  private offset: number = 0;
  constructor(readonly file: File, readonly chunkSize: number) {
  }
  public next(): Blob {
    const blob = this.file.slice(this.offset, this.offset + this.chunkSize);
    this.offset += this.chunkSize;
    return blob;
  }
  public isEmpty(): boolean {
    return this.offset >= this.file.size;
  }
  public get progress(): number {
    return this.offset / this.file.size;
  }
}

export class FileScanner {
  private readonly reader: FileReader;

  private slicer: FileSlicer;
  private active: boolean = false;

  constructor(readonly chunkSize: number = 2 ** 20) {
    this.reader = new FileReader();
  }
  public scan(file: File, fn: (result: any) => void) {
    this.slicer = new FileSlicer(file, this.chunkSize);
    this.active = true;
    return new Promise((resolve, reject) => {
      this.reader.onload = (event) => {
        if (!this.active) {
          reject();
          return;
        }
        fn((event.target as FileReaderOnLoadEventTarget).result);
        if (this.slicer.isEmpty()) {
          this.active = false;
          resolve();
        } else {
          this.reader.readAsBinaryString(this.slicer.next());
        }
      };
      this.reader.onerror = () => reject;
      this.reader.readAsBinaryString(this.slicer.next());
    });
  }
  public get progress() {
    return this.slicer
      ? this.slicer.progress
      : 0;
  }
  public cancel() {
    this.active = false;
    this.slicer = undefined;
  }
}
