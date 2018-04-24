import * as forge from 'node-forge';

enum ScannerState {
  Idle,
  Scanning,
  Canceled,
}

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
  private readonly slicer: FileSlicer;
  private readonly reader: FileReader;

  private state: ScannerState = ScannerState.Idle;

  constructor(readonly file: File, readonly chunkSize: number = 2 ** 20) {
    this.slicer = new FileSlicer(file, chunkSize);
    this.reader = new FileReader();
  }
  public scan(fn: (result: any) => void, progress: (ratio: number) => void) {
    this.state = ScannerState.Scanning;
    return new Promise((resolve, reject) => {
      this.reader.onload = (event) => {
        if (this.state !== ScannerState.Scanning) {
          if (this.state === ScannerState.Canceled) reject();
          return;
        }
        fn((event.target as FileReaderOnLoadEventTarget).result);
        if (this.slicer.isEmpty()) {
          progress(1);
          resolve();
        } else {
          progress(this.slicer.progress)
          this.reader.readAsBinaryString(this.slicer.next());
        }
      };
      this.reader.onerror = () => reject;
      this.reader.readAsBinaryString(this.slicer.next());
    });
  }
  public pause() {
    this.state = ScannerState.Idle;
  }
  public resume() {
    this.state = ScannerState.Scanning;
    this.reader.readAsBinaryString(this.slicer.next());
  }
  public cancel() {
    this.state = ScannerState.Canceled;
  }
}
