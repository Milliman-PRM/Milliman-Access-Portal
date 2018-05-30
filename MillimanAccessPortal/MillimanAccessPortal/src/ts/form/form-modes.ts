import { SubmissionGroup } from "./form-submission";

export interface SubmissionMode {
  name: string,
  groups: Array<SubmissionGroup<any>>,
  sparse: boolean,
}

export enum AccessMode {
  Read,
  Write,
  WriteDisabled,
  Defer,
}
