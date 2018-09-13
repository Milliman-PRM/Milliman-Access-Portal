export class FileSniffer {
  private readonly reader: FileReader;

  constructor(readonly file: File, readonly initialByteCount: number = 0x10) {
    this.reader = new FileReader();
  }
  public extensionMatchesInitialBytes() {
    return new Promise((resolve, reject) => {
      this.reader.onload = () => {
        const chunk = this.reader.result as ArrayBuffer;
        const initialBytes = new Uint8Array(chunk);

        const expectedInitialBytes: number[][] = [];
        const fileExtension = ((fileName) => {
          const parts = fileName.toLowerCase().split('.');
          return `.${parts[parts.length - 1]}`;
        })(this.file.name);
        switch (fileExtension) {
          case '.jpg':
          case '.jpeg':
              expectedInitialBytes.push([ 0xFF, 0xD8, 0xFF ]);
              break;
          case '.png':
              expectedInitialBytes.push([ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ]);
              break;
          case '.gif':
              expectedInitialBytes.push([ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 ]);
              expectedInitialBytes.push([ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 ]);
              break;
          case '.pdf':
              expectedInitialBytes.push([ 0x25, 0x50, 0x44, 0x46, 0x2D ]);
              break;
          case '.qvw':
              expectedInitialBytes.push([ 0x70, 0x17 ]);
              break;
        }

        function sequenceEqual(base: number[], other: Uint8Array) {
          for (let i = 0; i < base.length; i += 1) {
            if (base[i] !== other[i]) {
              return false;
            }
          }
          return true;
        }
        const match = expectedInitialBytes.map((byteSequence) => {
          const initialBytesTrimmed = initialBytes.slice(0, byteSequence.length);
          return sequenceEqual(byteSequence, initialBytesTrimmed);
        }).reduce((cum, cur) => cum || cur, false);

        resolve(match);
      };
      this.reader.onerror = () => reject;
      this.reader.readAsArrayBuffer(this.file.slice(0, this.initialByteCount));
    });
  }
}
