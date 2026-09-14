import { expect, it } from "vitest";
import { GoogleDriveStorage, StorageNotImplementedError } from "../worker/storage/GoogleDriveStorage";
it("explicitly rejects unimplemented Drive operations", async () => {
  const storage = new GoogleDriveStorage();
  const source = { provider: "google-drive", fileId: "example", mimeType: "audio/mpeg" };
  expect(storage.provider).toBe("google-drive");
  await expect(storage.getMetadata(source)).rejects.toBeInstanceOf(StorageNotImplementedError);
  await expect(storage.openStream(source, { range: "bytes=0-99" })).rejects.toBeInstanceOf(StorageNotImplementedError);
});
