'use client';

import {XMarkIcon} from '@heroicons/react/24/outline';
import {useTranslations} from 'next-intl';
import toast, {resolveValue, ToastBar, Toaster} from 'react-hot-toast';

export default function PortfolioToaster() {
  const t = useTranslations('notifications');

  return (
    <Toaster
      position="bottom-right"
      gutter={12}
      containerStyle={{bottom: 'clamp(1rem, 3vw, 2rem)', right: 'clamp(1rem, 3vw, 2rem)'}}
      toastOptions={{
        duration: 5000,
        className: 'portfolio-toast-bar',
        style: {
          maxWidth: 'none',
          padding: 0,
          background: 'transparent',
          color: 'inherit',
          borderRadius: 0,
          boxShadow: 'none'
        },
        success: {
          iconTheme: {
            primary: 'var(--accent-apricot)',
            secondary: 'var(--surface-ink)'
          }
        },
        error: {
          iconTheme: {
            primary: 'var(--error)',
            secondary: 'var(--surface-ink)'
          }
        }
      }}
    >
      {(currentToast) => {
        const status = currentToast.type === 'success'
          ? t('success')
          : currentToast.type === 'error'
            ? t('error')
            : currentToast.type === 'loading'
              ? t('loading')
              : t('update');

        return (
          <ToastBar toast={currentToast}>
            {({icon}) => (
              <div className="portfolio-toast" data-type={currentToast.type}>
                <div className="portfolio-toast-icon" aria-hidden="true">{icon}</div>
                <div className="portfolio-toast-content">
                  <div className="portfolio-toast-meta">
                    <span>{t('context')}</span>
                    <span>{status}</span>
                  </div>
                  <div className="portfolio-toast-message" {...currentToast.ariaProps}>
                    {resolveValue(currentToast.message, currentToast)}
                  </div>
                </div>
                <button
                  type="button"
                  className="portfolio-toast-dismiss"
                  onClick={() => toast.dismiss(currentToast.id)}
                  aria-label={t('dismiss')}
                >
                  <XMarkIcon aria-hidden="true" />
                </button>
              </div>
            )}
          </ToastBar>
        );
      }}
    </Toaster>
  );
}
