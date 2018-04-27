interface FileReaderOnLoadEventTarget extends EventTarget {
  result: BinaryType;
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
        chunkLoadedCallback(chunk);
        if (this.slicer.isEmpty()) {
          this.active = false;
          resolve(this.slicer.file.size);
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
  }
}
