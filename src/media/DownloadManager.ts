/** Contract only. Persistence and concurrency are P6 work. */
export interface DownloadManager {
  download(mediaId: string, signal?: AbortSignal): Promise<void>;
  remove(mediaId: string): Promise<void>;
}
