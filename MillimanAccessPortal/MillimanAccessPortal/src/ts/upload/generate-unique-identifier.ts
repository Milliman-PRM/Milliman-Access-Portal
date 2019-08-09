import { randomBytes } from "crypto";

export function generateUniqueId(purpose: string): string {
  return `${purpose}-${randomBytes(8).toString('hex')}`;
}
