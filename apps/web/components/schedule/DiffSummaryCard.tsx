"use client";

import { DiffSummaryDto } from "@/lib/api/schedule";

interface DiffSummaryCardProps {
  diff: DiffSummaryDto;
}

export default function DiffSummaryCard({ diff }: DiffSummaryCardProps) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-4 space-y-3">
      <h3 className="font-semibold text-sm text-gray-700">Changes vs baseline</h3>

      <div className="grid grid-cols-3 gap-3 text-center">
        <div className="rounded-md bg-green-50 p-3">
          <div className="text-2xl font-bold text-green-600">{diff.addedCount}</div>
          <div className="text-xs text-green-700 mt-1">Added</div>
        </div>
        <div className="rounded-md bg-red-50 p-3">
          <div className="text-2xl font-bold text-red-600">{diff.removedCount}</div>
          <div className="text-xs text-red-700 mt-1">Removed</div>
        </div>
        <div className="rounded-md bg-amber-50 p-3">
          <div className="text-2xl font-bold text-amber-600">{diff.changedCount}</div>
          <div className="text-xs text-amber-700 mt-1">Changed</div>
        </div>
      </div>

      {diff.stabilityScore !== null && (
        <div className="text-xs text-gray-500">
          Stability penalty: <span className="font-mono">{diff.stabilityScore?.toFixed(2)}</span>
        </div>
      )}
    </div>
  );
}
