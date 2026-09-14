/** URL may be a local blob or a Worker endpoint. Resolver owns resource cleanup. */
export interface ResolvedMedia {
  url: string;
  location: "local" | "remote";
  release(): void;
}
export interface MediaResolver {
  resolve(mediaId: string, signal?: AbortSignal): Promise<ResolvedMedia>;
}
