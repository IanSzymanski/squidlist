export type RepeatMode = "off" | "one" | "all";
export interface QueueEntry { id: string; mediaId: string; }
export interface QueueState {
  entries: readonly QueueEntry[];
  currentEntryId: string | null;
  repeat: RepeatMode;
  shuffle: boolean;
}
/** Selection belongs to queue logic, independent of playback implementation. */
export interface QueueSource {
  next(currentEntryId: string | null): Promise<QueueEntry | null>;
}
