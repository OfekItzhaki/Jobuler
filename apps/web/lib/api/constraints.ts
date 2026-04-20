import { apiClient } from "./client";

export interface ConstraintDto {
  id: string;
  scopeType: string;
  scopeId: string | null;
  severity: string;
  ruleType: string;
  rulePayloadJson: string;
  isActive: boolean;
  effectiveFrom: string | null;
  effectiveUntil: string | null;
}

export async function getConstraints(spaceId: string): Promise<ConstraintDto[]> {
  const { data } = await apiClient.get(`/spaces/${spaceId}/constraints`);
  return data;
}

export async function createConstraint(
  spaceId: string,
  scopeType: string,
  scopeId: string | null,
  severity: string,
  ruleType: string,
  rulePayloadJson: string,
  effectiveFrom: string | null,
  effectiveUntil: string | null
): Promise<{ id: string }> {
  const { data } = await apiClient.post(`/spaces/${spaceId}/constraints`, {
    scopeType, scopeId, severity, ruleType, rulePayloadJson,
    effectiveFrom, effectiveUntil,
  });
  return data;
}
