import type { MediaSource } from "../../src/domain/media";

export interface StorageMetadata {
  size: number;
  mimeType: string;
  etag?: string;
  lastModified?: string;
}
export interface StreamOptions {
  /** Raw HTTP Range header. Providers must validate or deliberately reject it. */
  range?: string;
  signal?: AbortSignal;
}
export interface StorageStream {
  body: ReadableStream<Uint8Array> | null;
  status: 200 | 206 | 416;
  /** Preserve Content-Range, Content-Length, Accept-Ranges and validators. */
  headers: Headers;
}
/** Server-only provider adapter. Never returns credentials or buffers entire files. */
export interface MediaStorage {
  readonly provider: string;
  getMetadata(source: MediaSource, signal?: AbortSignal): Promise<StorageMetadata>;
  openStream(source: MediaSource, options?: StreamOptions): Promise<StorageStream>;
}
