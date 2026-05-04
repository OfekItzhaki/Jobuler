"use client";

/**
 * ShifterLogo — the Shifter app icon.
 * White background, blue "S" with sharper angles (less round than the old version).
 *
 * Usage:
 *   <ShifterLogo size={32} />   — sidebar (default)
 *   <ShifterLogo size={40} />   — auth pages
 */
export default function ShifterLogo({ size = 32 }: { size?: number }) {
  const radius = Math.round(size * 0.25); // 25% corner radius
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: radius,
        background: "#ffffff",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        flexShrink: 0,
        boxShadow: "0 1px 3px rgba(0,0,0,0.18)",
      }}
    >
      {/* Sharp-cornered "S" — straight lines with only slight curves at the tips */}
      <svg
        width={Math.round(size * 0.58)}
        height={Math.round(size * 0.58)}
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M17 7.5C17 6 15.5 5 12 5C8.5 5 7 6 7 8C7 10 9 10.5 12 11C15 11.5 17 12 17 14.5C17 17 15.5 18 12 18C8.5 18 7 17 7 15.5"
          stroke="#3b82f6"
          strokeWidth="2.2"
          strokeLinecap="square"
          strokeLinejoin="miter"
          fill="none"
        />
      </svg>
    </div>
  );
}
