export default function AdminPostsLoading() {
  return (
    <main className="admin-main admin-main-status" aria-busy="true" aria-live="polite">
      <section className="admin-loading">
        <span className="admin-loading-mark" aria-hidden="true" />
        <p>Loading posts…</p>
      </section>
    </main>
  );
}
