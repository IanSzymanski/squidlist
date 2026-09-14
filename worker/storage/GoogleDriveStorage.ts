import type { MediaSource } from "../../src/domain/media";
import type { MediaStorage, StorageMetadata, StorageStream, StreamOptions } from "./MediaStorage";

export class StorageNotImplementedError extends Error {
  constructor() {
    super("Google Drive storage is not implemented. Authentication and streaming are scheduled for P1.");
    this.name = "StorageNotImplementedError";
  }
}
/** P0 placeholder: makes no network calls and accepts no credentials. */
export class GoogleDriveStorage implements MediaStorage {
  readonly provider = "google-drive";
  async getMetadata(_source: MediaSource, _signal?: AbortSignal): Promise<StorageMetadata> {
    throw new StorageNotImplementedError();
  }
  async openStream(_source: MediaSource, _options?: StreamOptions): Promise<StorageStream> {
    throw new StorageNotImplementedError();
  }
}
