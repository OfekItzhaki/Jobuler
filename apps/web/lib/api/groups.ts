import { apiClient } from "./client";

export interface GroupTypeDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface GroupDto {
  id: string;
  groupTypeId: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export async function getSpaceRoles(spaceId: string) {
  const { data } = await apiClient.get(`/spaces/${spaceId}/roles`);
  return data as Array<{ id: string; name: string; description: string | null; isActive: boolean }>;
}

export async function createSpaceRole(
  spaceId: string, name: string, description: string | null
) {
  const { data } = await apiClient.post(`/spaces/${spaceId}/roles`, { name, description });
  return data as { id: string };
}
