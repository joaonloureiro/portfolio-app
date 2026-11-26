'use client';

import Link from 'next/link';
import {useEffect, useMemo, useState, useTransition} from 'react';
import {updateAdminLocaleCatalog} from '@/lib/admin-locale-client-api';
import type {AdminLocaleCatalog, AdminLocaleEntry, LocaleCatalogValue, LocaleCode} from './admin-types';
import {cloneLocaleValue, LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH, LOCALE_CATALOG_MAX_LIST_ITEMS, LOCALE_CATALOG_MAX_SCALAR_LENGTH} from '@/lib/locale-catalog-validation';

type DraftEntry = {
  key: string;
  type: AdminLocaleEntry['type'];
  en: LocaleCatalogValue;
  pt: LocaleCatalogValue;
};

type Props = {
  catalog: AdminLocaleCatalog;
};

function cloneEntries(entries: readonly AdminLocaleEntry[]): DraftEntry[] {
  return entries.map((entry) => ({
    key: entry.key,
    type: entry.type,
    en: cloneLocaleValue(entry.en),
    pt: cloneLocaleValue(entry.pt)
  }));
}

function inputId(key: string, locale: LocaleCode) {
  return `locale-${locale}-${key.replaceAll('.', '-')}`;
}

function isLongText(key: string, value: string) {
  return value.length > 140 || key.includes('description') || key.includes('paragraph') || key.includes('intro') || key.includes('subtitle') || key.endsWith('purpose');
}

function listValue(entry: DraftEntry, locale: LocaleCode): readonly string[] {
  const value = entry[locale];
  return Array.isArray(value) ? value : [];
}

function hasInvalidValue(entry: DraftEntry): boolean {
  if (entry.type === 'string') {
    return typeof entry.en !== 'string' || typeof entry.pt !== 'string' ||
      entry.en.length > LOCALE_CATALOG_MAX_SCALAR_LENGTH || entry.pt.length > LOCALE_CATALOG_MAX_SCALAR_LENGTH ||
      entry.en.includes('<') || entry.en.includes('>') || entry.pt.includes('<') || entry.pt.includes('>');
  }

  return [entry.en, entry.pt].some((value) => !Array.isArray(value) || value.length < 1 || value.length > LOCALE_CATALOG_MAX_LIST_ITEMS || value.some((item) => item.length > LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH || item.includes('<') || item.includes('>')));
}

export default function AdminLocaleEditor({catalog}: Props) {
  const [entries, setEntries] = useState<DraftEntry[]>(() => cloneEntries(catalog.entries));
  const [revision, setRevision] = useState(catalog.revision);
  const [isDirty, setIsDirty] = useState(false);
  const [feedback, setFeedback] = useState<{kind: 'success' | 'error'; text: string; canReload?: boolean} | null>(null);
  const [isPending, startTransition] = useTransition();

  const groups = useMemo(() => {
    const grouped = new Map<string, DraftEntry[]>();
    for (const entry of entries) {
      const namespace = entry.key.split('.')[0] ?? 'catalog';
      const group = grouped.get(namespace) ?? [];
      group.push(entry);
      grouped.set(namespace, group);
    }
    return [...grouped.entries()];
  }, [entries]);

  useEffect(() => {
    if (!isDirty) return;
    const warnBeforeDiscard = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = '';
    };
    window.addEventListener('beforeunload', warnBeforeDiscard);
    return () => window.removeEventListener('beforeunload', warnBeforeDiscard);
  }, [isDirty]);

  const updateValue = (key: string, locale: LocaleCode, value: LocaleCatalogValue) => {
    setEntries((current) => current.map((entry) => entry.key === key ? {...entry, [locale]: value} : entry));
    setIsDirty(true);
    setFeedback(null);
  };

  const updateListItem = (key: string, locale: LocaleCode, index: number, value: string) => {
    const entry = entries.find((item) => item.key === key);
    if (!entry) return;
    const values = [...listValue(entry, locale)];
    values[index] = value;
    updateValue(key, locale, values);
  };

  const addListItem = (key: string, locale: LocaleCode) => {
    const entry = entries.find((item) => item.key === key);
    if (!entry) return;
    updateValue(key, locale, [...listValue(entry, locale), '']);
  };

  const removeListItem = (key: string, locale: LocaleCode, index: number) => {
    const entry = entries.find((item) => item.key === key);
    if (!entry) return;
    updateValue(key, locale, listValue(entry, locale).filter((_, itemIndex) => itemIndex !== index));
  };

  const moveListItem = (key: string, locale: LocaleCode, index: number, direction: -1 | 1) => {
    const entry = entries.find((item) => item.key === key);
    if (!entry) return;
    const values = [...listValue(entry, locale)];
    const target = index + direction;
    if (target < 0 || target >= values.length) return;
    [values[index], values[target]] = [values[target], values[index]];
    updateValue(key, locale, values);
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const invalidEntries = entries.filter(hasInvalidValue);
    if (invalidEntries.length > 0) {
      setFeedback({kind: 'error', text: `Check the highlighted fields. Lists need 1–${LOCALE_CATALOG_MAX_LIST_ITEMS} items and every value has a visible length limit.`});
      return;
    }
    if (!window.confirm('Publish this complete English and Portuguese catalog now?')) return;

    const changes: Record<string, {en: LocaleCatalogValue; pt: LocaleCatalogValue}> = {};
    for (const entry of entries) {
      changes[entry.key] = {en: entry.en, pt: entry.pt};
    }

    setFeedback(null);
    startTransition(async () => {
      const result = await updateAdminLocaleCatalog(revision, changes);
      if (result.status === 'success') {
        setEntries(cloneEntries(result.data.entries));
        setRevision(result.data.revision);
        setIsDirty(false);
        setFeedback({kind: 'success', text: `Catalog published as revision ${result.data.revision}.`});
      } else {
        setFeedback({kind: 'error', text: result.message, canReload: result.status === 'conflict'});
      }
    });
  };

  return (
    <main className="admin-main admin-locale-editor-main">
      <div className="admin-page-heading">
        <div>
          <Link href="/admin" className="admin-back-link">← Admin home</Link>
          <h1>Locales</h1>
          <p>Edit the existing public English and Portuguese copy together. Projects, posts, routing, and Admin wording are managed elsewhere.</p>
        </div>
        <span className="admin-revision-label">Revision {revision}</span>
      </div>

      <form className="admin-locale-form" onSubmit={handleSubmit}>
        <p className="admin-locale-contract-note">Plain text only. Every field is submitted as one bilingual change set and published with optimistic concurrency.</p>

        {groups.map(([namespace, namespaceEntries]) => (
          <fieldset className="admin-locale-namespace" key={namespace} disabled={isPending}>
            <legend>{namespace}</legend>
            <div className="admin-locale-entry-list">
              {namespaceEntries.map((entry) => {
                const hasError = hasInvalidValue(entry);
                return (
                  <div className="admin-locale-entry" data-invalid={hasError} key={entry.key}>
                    <div className="admin-locale-entry-heading">
                      <code>{entry.key}</code>
                      <span>{entry.type === 'string' ? `String · max ${LOCALE_CATALOG_MAX_SCALAR_LENGTH.toLocaleString('en-US')} characters` : `Ordered list · ${LOCALE_CATALOG_MAX_LIST_ITEMS} items max`}</span>
                    </div>
                    <div className="admin-locale-values">
                      {(['en', 'pt'] as const).map((locale) => {
                        const id = inputId(entry.key, locale);
                        const label = locale === 'en' ? 'English' : 'Português';
                        if (entry.type === 'string') {
                          const value = typeof entry[locale] === 'string' ? entry[locale] : '';
                          const longText = isLongText(entry.key, value);
                          return (
                            <label className="admin-locale-field" htmlFor={id} key={locale}>
                              <span>{label}</span>
                              {longText ? (
                                <textarea id={id} value={value} maxLength={LOCALE_CATALOG_MAX_SCALAR_LENGTH} rows={4} onChange={(event) => updateValue(entry.key, locale, event.target.value)} aria-invalid={hasError} aria-describedby={`${id}-help`} />
                              ) : (
                                <input id={id} value={value} maxLength={LOCALE_CATALOG_MAX_SCALAR_LENGTH} onChange={(event) => updateValue(entry.key, locale, event.target.value)} aria-invalid={hasError} aria-describedby={`${id}-help`} />
                              )}
                              <small id={`${id}-help`}>Up to {LOCALE_CATALOG_MAX_SCALAR_LENGTH.toLocaleString('en-US')} characters.</small>
                            </label>
                          );
                        }

                        const values = listValue(entry, locale);
                        return (
                          <div className="admin-locale-field admin-locale-list-field" key={locale}>
                            <span>{label}</span>
                            <div className="admin-repeat-list">
                              {values.map((value, index) => {
                                const itemId = `${id}-${index}`;
                                return (
                                  <div className="admin-repeat-row" key={itemId}>
                                    <label htmlFor={itemId} className="sr-only">{label} {entry.key} item {index + 1}</label>
                                    <input id={itemId} value={value} maxLength={LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH} onChange={(event) => updateListItem(entry.key, locale, index, event.target.value)} aria-invalid={hasError} aria-describedby={`${id}-help`} />
                                    <div className="admin-repeat-controls">
                                      <button type="button" className="admin-button admin-button-quiet" onClick={() => moveListItem(entry.key, locale, index, -1)} disabled={index === 0} aria-label={`Move ${label} ${entry.key} item ${index + 1} up`}>↑</button>
                                      <button type="button" className="admin-button admin-button-quiet" onClick={() => moveListItem(entry.key, locale, index, 1)} disabled={index === values.length - 1} aria-label={`Move ${label} ${entry.key} item ${index + 1} down`}>↓</button>
                                      <button type="button" className="admin-button admin-button-danger" onClick={() => removeListItem(entry.key, locale, index)} aria-label={`Remove ${label} ${entry.key} item ${index + 1}`}>Remove</button>
                                    </div>
                                  </div>
                                );
                              })}
                            </div>
                            <button type="button" className="admin-button admin-button-quiet" onClick={() => addListItem(entry.key, locale)} disabled={values.length >= LOCALE_CATALOG_MAX_LIST_ITEMS}>+ Add item</button>
                            <small>1–{LOCALE_CATALOG_MAX_LIST_ITEMS} items; {LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH} characters per item.</small>
                          </div>
                        );
                      })}
                    </div>
                    {hasError && <p className="admin-field-error" role="alert">This value is outside the allowed shape or length.</p>}
                  </div>
                );
              })}
            </div>
          </fieldset>
        ))}

        {feedback && <p className={`admin-form-feedback admin-form-feedback-${feedback.kind}`} role={feedback.kind === 'error' ? 'alert' : 'status'}>{feedback.text}</p>}
        {feedback?.kind === 'error' && feedback.canReload && <button type="button" className="admin-button admin-button-quiet" onClick={() => window.location.reload()}>Reload latest catalog</button>}
        <div className="admin-form-actions">
          <button type="submit" className="admin-button admin-button-accent" disabled={isPending || !isDirty}>{isPending ? 'Publishing…' : 'Publish catalog'}</button>
          {isDirty && <span className="admin-unsaved-note" role="status">Unsaved changes</span>}
        </div>
      </form>
    </main>
  );
}
