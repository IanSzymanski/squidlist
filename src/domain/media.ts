/** Provider identity is data; credentials never belong in domain models. */
export interface MediaSource {
  provider: string;
  fileId: string;
  mimeType: string;
  version?: string;
}
interface MediaBase {
  id: string;
  title: string;
  /** Duration in seconds. Null until metadata is available. */
  duration: number | null;
  source: MediaSource;
}
export interface AudioMediaItem extends MediaBase {
  type: "audio";
  artistIds: string[];
  albumId?: string;
  trackNumber?: number;
  discNumber?: number;
}
export interface VideoMediaItem extends MediaBase {
  type: "video";
  width?: number;
  height?: number;
}
export type MediaItem = AudioMediaItem | VideoMediaItem;
