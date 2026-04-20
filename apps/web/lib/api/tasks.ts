import { apiClient } from "./client";

export interface TaskTypeDto {
  id: string;
  name: string;
  description: string | null;
  burdenLevel: string;
  defaultPriority: number;
  allowsOverlap: boolean;
  isActive: boolean;
}

export interface TaskSlotDto {
  id: string;
  taskTypeId: string;
  taskTypeName: string;
  startsAt: string;
  endsAt: string;
  requiredHeadcount: number;
  priority: number;
  status: string;
}

export async function getTaskTypes(spaceId: string): Promise<TaskTypeDto[]> {
  const { data } = await apiClient.get(`/spaces/${spaceId}/task-types`);
  return data;
}

export async function createTaskType(
  spaceId: string,
  name: string,
  description: string | null,
  burdenLevel: string,
  defaultPriority: number,
  allowsOverlap: boolean
): Promise<{ id: string }> {
  const { data } = await apiClient.post(`/spaces/${spaceId}/task-types`, {
    name, description, burdenLevel, defaultPriority, allowsOverlap,
  });
  return data;
}

export async function getTaskSlots(
  spaceId: string, from?: string, to?: string
): Promise<TaskSlotDto[]> {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const { data } = await apiClient.get(
    `/spaces/${spaceId}/task-slots?${params.toString()}`
  );
  return data;
}

export async function createTaskSlot(
  spaceId: string,
  taskTypeId: string,
  startsAt: string,
  endsAt: string,
  requiredHeadcount: number,
  priority: number,
  location: string | null
): Promise<{ id: string }> {
  const { data } = await apiClient.post(`/spaces/${spaceId}/task-slots`, {
    taskTypeId, startsAt, endsAt, requiredHeadcount, priority,
    requiredRoleIds: [], requiredQualificationIds: [], location,
  });
  return data;
}
