import { apiClient } from "./client";

export interface SpaceDto {
  id: string;
  name: string;
  description: string | null;
  locale: string;
  isActive: boolean;
}

export async function getMySpaces(): Promise<SpaceDto[]> {
  const { data } = await apiClient.get("/spaces");
  return data;
}

export async function createSpace(
  name: string, description: string | null, locale: string
): Promise<{ spaceId: string }> {
  const { data } = await apiClient.post("/spaces", { name, description, locale });
  return data;
}
