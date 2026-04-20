import { apiClient } from "./client";

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  displayName: string;
  preferredLocale: string;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>("/auth/login", { email, password });
  return data;
}

export async function register(
  email: string,
  displayName: string,
  password: string,
  preferredLocale = "he"
): Promise<{ userId: string }> {
  const { data } = await apiClient.post("/auth/register", {
    email, displayName, password, preferredLocale,
  });
  return data;
}

export async function logout(refreshToken: string): Promise<void> {
  await apiClient.post("/auth/logout", { refreshToken });
}
