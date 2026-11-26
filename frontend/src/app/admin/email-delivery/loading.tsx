export default function EmailDeliveryLoading() {
  return (
    <main className="admin-loading" aria-busy="true">
      <span className="admin-loading-mark" aria-hidden="true" />
      <p>Loading email delivery settings…</p>
    </main>
  );
}
