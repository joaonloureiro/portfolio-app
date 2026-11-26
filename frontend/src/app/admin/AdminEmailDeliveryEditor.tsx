'use client';

import {useState} from 'react';
import type {FormEvent} from 'react';
import type {AdminEmailDeliveryInput, AdminEmailDeliveryPolicy, AdminEmailDeliveryResult} from './admin-types';
import {sendAdminEmailDeliveryTest, updateAdminEmailDeliveryPolicy} from '@/lib/admin-email-delivery-client-api';

type Props = {policy: AdminEmailDeliveryPolicy};

function valuesFromPolicy(policy: AdminEmailDeliveryPolicy): AdminEmailDeliveryInput {
  return {
    enabled: policy.enabled,
    recipientAddress: policy.recipientAddress,
    senderDisplayName: policy.senderDisplayName,
    subjectPrefix: policy.subjectPrefix
  };
}

function feedbackClass(result: AdminEmailDeliveryResult<unknown>) {
  return result.status === 'success' ? 'admin-form-feedback admin-form-feedback-success' : 'admin-form-feedback admin-form-feedback-error';
}

export default function AdminEmailDeliveryEditor({policy}: Props) {
  const [form, setForm] = useState<AdminEmailDeliveryInput>(() => valuesFromPolicy(policy));
  const [revision, setRevision] = useState(policy.revision);
  const [savedEnabled, setSavedEnabled] = useState(policy.enabled);
  const [saveResult, setSaveResult] = useState<AdminEmailDeliveryResult<AdminEmailDeliveryPolicy> | null>(null);
  const [testResult, setTestResult] = useState<AdminEmailDeliveryResult<{message: string}> | null>(null);
  const [pending, setPending] = useState<'save' | 'test' | null>(null);

  const updateField = <K extends keyof AdminEmailDeliveryInput>(field: K, value: AdminEmailDeliveryInput[K]) => {
    setForm((current) => ({...current, [field]: value}));
    setSaveResult(null);
  };

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (pending) return;
    if (savedEnabled && !form.enabled && !window.confirm('Disable contact email delivery? Public visitors will no longer be able to send contact messages.')) return;

    setPending('save');
    const result = await updateAdminEmailDeliveryPolicy(revision, form);
    setSaveResult(result);
    if (result.status === 'success') {
      setRevision(result.data.revision);
      setSavedEnabled(result.data.enabled);
      setForm(valuesFromPolicy(result.data));
    }
    setPending(null);
  };

  const sendTest = async () => {
    if (pending) return;
    setPending('test');
    setTestResult(await sendAdminEmailDeliveryTest());
    setPending(null);
  };

  return (
    <main className="admin-main admin-email-delivery-main">
      <div className="admin-page-heading">
        <div>
          <p className="admin-status-code">OPERATIONS / EMAIL</p>
          <h1>Email delivery</h1>
          <p>Control the safe contact-delivery policy. Transport credentials and connection details remain deployment-managed.</p>
        </div>
        <span className="admin-revision-label">Revision {revision}</span>
      </div>

      <section className="admin-email-delivery-boundary" aria-labelledby="transport-boundary-title">
        <div>
          <h2 id="transport-boundary-title">SMTP transport</h2>
          <p>Credentials, host, port, TLS mode, and envelope sender are configured by deployment. They are never editable or revealed here.</p>
        </div>
        <span className={`admin-status-badge ${policy.transportConfigured ? 'admin-status-published' : 'admin-status-unpublished'}`}>
          {policy.transportConfigured ? 'Configured' : 'Unavailable'}
        </span>
      </section>

      <form className="admin-email-delivery-form" onSubmit={save} noValidate>
        <fieldset disabled={pending !== null}>
          <legend>Delivery policy</legend>
          <label className="admin-email-toggle">
            <input type="checkbox" checked={form.enabled} onChange={(event) => updateField('enabled', event.target.checked)} />
            <span>Enable public contact delivery</span>
          </label>
          <p className="admin-field"><label htmlFor="email-recipient">Recipient address</label><input id="email-recipient" type="email" required maxLength={320} value={form.recipientAddress} onChange={(event) => updateField('recipientAddress', event.target.value)} /></p>
          <p className="admin-field"><label htmlFor="email-display-name">Sender display name</label><input id="email-display-name" type="text" required maxLength={120} value={form.senderDisplayName} onChange={(event) => updateField('senderDisplayName', event.target.value)} /></p>
          <p className="admin-field"><label htmlFor="email-subject-prefix">Subject prefix</label><input id="email-subject-prefix" type="text" required maxLength={120} value={form.subjectPrefix} onChange={(event) => updateField('subjectPrefix', event.target.value)} /><small>The visitor name is appended after this prefix for contact messages.</small></p>
        </fieldset>

        {saveResult && <p className={feedbackClass(saveResult)} role="status">{saveResult.status === 'success' ? 'Delivery policy saved.' : saveResult.message}</p>}
        <div className="admin-form-actions">
          <button className="admin-button admin-button-accent" type="submit" disabled={pending !== null}>{pending === 'save' ? 'Saving…' : 'Save policy'}</button>
        </div>
      </form>

      <section className="admin-email-delivery-test" aria-labelledby="email-test-title">
        <div>
          <h2 id="email-test-title">Test delivery</h2>
          <p>This sends a fixed test message only to the currently saved recipient address. It never accepts an arbitrary target or message.</p>
        </div>
        <button className="admin-button" type="button" onClick={sendTest} disabled={pending !== null || !savedEnabled}>{pending === 'test' ? 'Sending…' : 'Send test message'}</button>
        {testResult && <p className={feedbackClass(testResult)} role="status">{testResult.status === 'success' ? testResult.data.message : testResult.message}</p>}
      </section>
    </main>
  );
}
