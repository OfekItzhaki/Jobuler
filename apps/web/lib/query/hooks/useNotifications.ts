import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getNotifications, dismissNotification, dismissAllNotifications } from "@/lib/api/notifications";
import { queryKeys } from "../keys";

export function useNotifications(spaceId: string | null) {
  return useQuery({
    queryKey: queryKeys.notifications(spaceId ?? ""),
    queryFn: () => getNotifications(spaceId!),
    enabled: !!spaceId,
    refetchInterval: 5_000,   // poll every 5s so solver results appear quickly
    refetchIntervalInBackground: false,
  });
}

/** Call this after any action that may produce a new notification (solver trigger, etc.) */
export function useRefetchNotifications(spaceId: string | null) {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: queryKeys.notifications(spaceId ?? "") });
}

export function useDismissNotification(spaceId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => dismissNotification(spaceId!, id),
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: queryKeys.notifications(spaceId ?? "") });
      const prev = qc.getQueryData(queryKeys.notifications(spaceId ?? ""));
      qc.setQueryData(queryKeys.notifications(spaceId ?? ""), (old: any[]) =>
        (old ?? []).filter(n => n.id !== id)
      );
      return { prev };
    },
    onError: (_err, _id, ctx) => {
      if (ctx?.prev) qc.setQueryData(queryKeys.notifications(spaceId ?? ""), ctx.prev);
    },
  });
}

export function useDismissAllNotifications(spaceId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => dismissAllNotifications(spaceId!),
    onSuccess: () => qc.setQueryData(queryKeys.notifications(spaceId ?? ""), []),
  });
}
