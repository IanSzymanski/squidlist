import type { QueueEntry, QueueSource, QueueState } from "../domain/queue";
/** Contract only; does not control audio nodes or resolve media. */
export interface QueueManager {
  getState(): QueueState;
  setSource(source: QueueSource): void;
  next(): Promise<QueueEntry | null>;
  previous(): Promise<QueueEntry | null>;
}
