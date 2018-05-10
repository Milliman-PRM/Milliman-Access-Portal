import { EntityForm } from "./entity-form";
import { FormElement } from "./form-element";
import { confirmAndContinueForm } from "../shared";

export class EntityFormSubmission extends FormElement {
  _cssClasses = {
    main: 'button-container',
    title: '',
    extension: '',
  };

  private _disabled = false;
  private _submissionMode: SubmissionMode;
  public get submissionMode(): SubmissionMode {
    return this._submissionMode;
  };
  public set submissionMode(submissionMode: SubmissionMode) {
    this._submissionMode = submissionMode;
    if (this.$entryPoint.is(this.activeButtonSelector)) {
      this.$entryPoint.find('button').show();
    } else {
      this.$entryPoint.find('button').hide();
    }
  }
  private get activeButtonSelector(): string {
    return `.button-container-${this.submissionMode.name}`;
  }

  get modified() {
    return false;
  }
  set modified(value: boolean) {
    if (value) {
      this.$entryPoint.show();
    } else {
      this.$entryPoint.hide();
    }
    this._disabled = value;
  }

  setCallbacks(allGroups: Array<{group: EntityFormSubmissionGroup<any>, mode: string}>, form: EntityForm) {
    // Find the first group (if any) that matches
    const filteredGroups = allGroups.filter((group) =>
      this.$entryPoint.is(`.button-container-${group.mode}`));
    if (!filteredGroups.length) {
      return;
    }
    const group = filteredGroups[0];

    // Set submit callback
    this.$entryPoint
      .find('.button-submit')
      .off('click')
      .on('click', () => group.group.submit(form));

    // Set reset callback
    this.$entryPoint
      .find('.button-reset')
      .off('click')
      .on('click', () => confirmAndContinueForm(() => {
        form.sections.forEach((section) => {
          section.inputs.forEach((input) => {
            input.reset();
          });
        });
        this.modified = form.modified;
      }));
  }
}

export class EntityFormSubmissionGroup<T> {
  constructor(
    readonly sections: Array<string>,
    readonly url: string,
    readonly method: string,
    readonly callback: (response: T, form?: EntityForm) => void,
  ) { }

  public chain<U>(that: EntityFormSubmissionGroup<U>): EntityFormSubmissionGroup<T> {
    const chainedGroup = new EntityFormSubmissionGroup<T>(
      this.sections,
      this.url,
      this.method,
      (response: T, form: EntityForm) => {
        this.callback(response);
        that.submit(form);
      },
    );
    return chainedGroup;
  }

  public submit(form: EntityForm) {
    $.ajax({
      method: this.method,
      url: this.url,
      data: form.serialize(this.sections),
    }).done((response: T) => {
      this.callback(response, form);
    }).fail((response) => {
      // TODO: do something on fail
    });
  }
}

export interface SubmissionMode {
  name: string,
  sections: Array<string>,
}
