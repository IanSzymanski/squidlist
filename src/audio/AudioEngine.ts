import type { ResolvedMedia } from "../media/MediaResolver";
/** Contract only. No queue, playlist, storage-provider or React dependencies.
 * A source offset is allowed; future timeline orchestration is a separate concern.
 */
export interface AudioEngine {
  load(media: ResolvedMedia, sourceStartSeconds?: number): Promise<void>;
  play(): Promise<void>;
  pause(): void;
  seek(seconds: number): void;
  setVolume(volume: number): void;
  dispose(): void;
}
