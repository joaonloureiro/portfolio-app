export default function AdminLoading() {
  return (
    <main className="admin-loading" role="status" aria-live="polite">
      <span className="admin-loading-mark" aria-hidden="true" />
      <p>Loading your Admin workspace…</p>
    </main>
  );
}
