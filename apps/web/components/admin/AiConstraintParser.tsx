"use client";

import { useState } from "react";
import { apiClient } from "@/lib/api/client";
import { useSpaceStore } from "@/lib/store/spaceStore";

interface ParsedConstraint {
  parsed: boolean;
  ruleType: string | null;
  scopeType: string | null;
  scopeHint: string | null;
  rulePayloadJson: string | null;
  confidenceNote: string | null;
  rawInput: string;
}

interface AiConstraintParserProps {
  onConfirm: (constraint: ParsedConstraint) => void;
}

export default function AiConstraintParser({ onConfirm }: AiConstraintParserProps) {
  const { currentSpaceId } = useSpaceStore();
  const [input, setInput] = useState("");
  const [result, setResult] = useState<ParsedConstraint | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleParse() {
    if (!currentSpaceId || !input.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const { data } = await apiClient.post(
        `/spaces/${currentSpaceId}/ai/parse-constraint`,
        { input }
      );
      setResult(data);
    } catch {
      setError("Failed to parse. Please try again or enter the constraint manually.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-4 rounded-lg border border-blue-200 bg-blue-50 p-4">
      <div className="flex items-center gap-2">
        <span className="text-xs font-semibold text-blue-700 uppercase tracking-wide">
          AI Assistant
        </span>
        <span className="text-xs text-blue-500">
          Describe a constraint in plain language
        </span>
      </div>

      <div className="flex gap-2">
        <input
          type="text"
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === "Enter" && handleParse()}
          placeholder='e.g. "Ofek cannot do kitchen for 10 days"'
          className="flex-1 border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
        <button
          onClick={handleParse}
          disabled={loading || !input.trim()}
          className="bg-blue-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
        >
          {loading ? "..." : "Parse"}
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {result && (
        <div className="space-y-3 rounded-lg border border-blue-300 bg-white p-3">
          {result.parsed ? (
            <>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div>
                  <span className="text-gray-500 text-xs">Rule type</span>
                  <p className="font-mono font-medium">{result.ruleType}</p>
                </div>
                <div>
                  <span className="text-gray-500 text-xs">Scope</span>
                  <p className="font-medium">{result.scopeType} — {result.scopeHint}</p>
                </div>
              </div>

              {result.rulePayloadJson && (
                <div>
                  <span className="text-gray-500 text-xs">Payload</span>
                  <pre className="text-xs bg-gray-50 rounded p-2 mt-1 overflow-x-auto">
                    {result.rulePayloadJson}
                  </pre>
                </div>
              )}

              {result.confidenceNote && (
                <p className="text-xs text-amber-600 italic">{result.confidenceNote}</p>
              )}

              <div className="flex gap-2 pt-1">
                <button
                  onClick={() => onConfirm(result)}
                  className="bg-green-600 text-white text-xs px-3 py-1.5 rounded-lg hover:bg-green-700"
                >
                  Confirm and save
                </button>
                <button
                  onClick={() => setResult(null)}
                  className="text-xs text-gray-500 hover:underline"
                >
                  Discard
                </button>
              </div>
            </>
          ) : (
            <div className="space-y-1">
              <p className="text-sm text-amber-700">Could not parse automatically.</p>
              {result.confidenceNote && (
                <p className="text-xs text-gray-500">{result.confidenceNote}</p>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
