import { SubmissionGroup } from "./form-submission";

export interface SubmissionMode {
  name: string,
  group: SubmissionGroup<any>,
}

export enum AccessMode {
  Read,
  Write,
  Defer,
}
