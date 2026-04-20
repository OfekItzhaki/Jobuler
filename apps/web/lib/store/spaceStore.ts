import { create } from "zustand";
import { persist } from "zustand/middleware";

interface SpaceState {
  currentSpaceId: string | null;
  currentSpaceName: string | null;
  setCurrentSpace: (id: string, name: string) => void;
  clearSpace: () => void;
}

export const useSpaceStore = create<SpaceState>()(
  persist(
    (set) => ({
      currentSpaceId: null,
      currentSpaceName: null,
      setCurrentSpace: (id, name) => set({ currentSpaceId: id, currentSpaceName: name }),
      clearSpace: () => set({ currentSpaceId: null, currentSpaceName: null }),
    }),
    { name: "jobuler-space" }
  )
);
