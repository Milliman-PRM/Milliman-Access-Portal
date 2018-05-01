import { Promise } from 'es6-promise';

interface FileReaderOnLoadEventTarget extends EventTarget {
  result: Blob | ArrayBuffer;
}

class FileSlicer {
  private offset: number = 0;
  constructor(readonly file: File, readonly chunkSize: number) {
  }
  public next(): Blob {
    const chunk = this.file.slice(this.offset, this.offset + this.chunkSize);
    this.offset += this.chunkSize;
    return chunk;
  }
  public isEmpty(): boolean {
    return this.offset >= this.file.size;
  }
  public get progress(): number {
    return Math.min(this.offset / this.file.size, 1);
  }
}

class ArrayBufferSlicer {
  private offset: number = 0;
  constructor(readonly buffer: ArrayBuffer, readonly chunkSize: number) {
  }
  public next(): ArrayBuffer {
    const chunk = this.buffer.slice(this.offset, this.offset + this.chunkSize);
    this.offset += this.chunkSize;
    return chunk;
  }
  public isEmpty(): boolean {
    return this.offset >= this.buffer.byteLength;
  }
  public get progress(): number {
    return Math.min(this.offset / this.buffer.byteLength, 1);
  }
}

export class FileScanner {
  private readonly reader: FileReader;

  private slicer: FileSlicer;
  private active: boolean = false;

  constructor(readonly chunkSize: number = 2 ** 20) {
    this.reader = new FileReader();
  }
  public open(file: File) {
    this.slicer = new FileSlicer(file, this.chunkSize);
  }
  public scan(chunkLoadedCallback: (result: any) => void) {
    this.active = true;
    return new Promise((resolve, reject) => {
      this.reader.onload = (event) => {
        if (!this.active) {
          this.slicer = undefined;
          reject('Scan cancelled');
          return;
        }
        const chunk = (event.target as FileReaderOnLoadEventTarget).result;
        const chunkSlicer = new ArrayBufferSlicer(chunk, 1024);
        for (let slice = chunkSlicer.next(); slice.byteLength > 0; slice = chunkSlicer.next()) {
          const binaryString = String.fromCharCode.apply(null, new Uint8Array(slice));
          chunkLoadedCallback(binaryString);
        }
        if (this.slicer.isEmpty()) {
          this.active = false;
          resolve(this.slicer.file.size);
        } else {
          this.reader.readAsArrayBuffer(this.slicer.next());
        }
      };
      this.reader.onerror = () => reject;
      this.reader.readAsArrayBuffer(this.slicer.next());
    });
  }
  public get progress() {
    return this.slicer
      ? this.slicer.progress
      : 0;
  }
  public cancel() {
    this.active = false;
  }
}
