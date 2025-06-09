'use client';
import React, { useState, FormEvent } from 'react';
import { useTranslations } from 'next-intl';

export default function ContactForm() {
  const t = useTranslations('ContactForm');
  const [formData, setFormData] = useState<{ name: string; email: string; message: string }>({ name: '', email: '', message: '' });
  const [status, setStatus] = useState<string>('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setStatus(t('sending'));

    if (!formData.name ||!formData.email ||!formData.message) {
      setStatus(t('errorAllFields'));
      return;
    }
    if (!/\S+@\S+\.\S+/.test(formData.email)) {
      setStatus(t('errorInvalidEmail'));
      return;
    }

    try {
      // The backend will run on a different port or subpath, proxied by Nginx
      const response = await fetch('/api/email/send', { // Adjusted API path
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        setStatus(t('success'));
        setFormData({ name: '', email: '', message: '' });
      } else {
        const errorData = await response.json();
        setStatus(errorData.message || t('errorFailed'));
      }
    } catch (error) {
      console.error('Contact form error:', error);
      setStatus(t('errorGeneric'));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-w-lg mx-auto p-8 bg-white dark:bg-gray-800 shadow-xl rounded-lg">
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 dark:text-gray-300">{t('nameLabel')}</label>
        <input type="text" name="name" id="name" value={formData.name} onChange={handleChange} required
               className="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm dark:bg-gray-700 dark:text-white" />
      </div>
      <div>
        <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-300">{t('emailLabel')}</label>
        <input type="email" name="email" id="email" value={formData.email} onChange={handleChange} required
               className="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm dark:bg-gray-700 dark:text-white" />
      </div>
      <div>
        <label htmlFor="message" className="block text-sm font-medium text-gray-700 dark:text-gray-300">{t('messageLabel')}</label>
        <textarea name="message" id="message" rows={4} value={formData.message} onChange={handleChange} required
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm dark:bg-gray-700 dark:text-white"></textarea>
      </div>
      <div>
        <button type="submit"
                className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary hover:bg-primary-dark focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-dark transition-colors">
          {t('sendButton')}
        </button>
      </div>
      {status && <p className={`mt-4 text-sm text-center ${status.includes('success') || status.includes('sucesso') ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>{status}</p>}
    </form>
  );
}