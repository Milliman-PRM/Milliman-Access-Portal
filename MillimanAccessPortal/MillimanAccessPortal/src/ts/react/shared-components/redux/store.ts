/**
 * Alias for a string indexed object.
 */
export interface Dict<T> {
  [key: string]: T;
}

/**
 * State attached to card column filters.
 */
export interface FilterState {
  text: string;
}

/**
 * State attached to modals.
 */
export interface ModalState {
  isOpen: boolean;
}
