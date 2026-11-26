export type AdminIdentity = Readonly<{
  subject: string;
}>;

export type AdminFailureStatus =
  | 'unauthorized'
  | 'forbidden'
  | 'timeout'
  | 'malformed-response'
  | 'unavailable';

export type AdminIdentityResult =
  | Readonly<{
      status: 'success';
      identity: AdminIdentity;
    }>
  | Readonly<{
      status: AdminFailureStatus;
      message: string;
    }>;

export type PostLocale = 'en' | 'pt';

export type LocaleCode = 'en' | 'pt';

export type LocaleCatalogValue = string | readonly string[];

export type AdminLocaleEntry = Readonly<{
  key: string;
  type: 'string' | 'string-list';
  en: LocaleCatalogValue;
  pt: LocaleCatalogValue;
}>;

export type AdminLocaleCatalog = Readonly<{
  revision: number;
  entries: readonly AdminLocaleEntry[];
}>;

export type AdminLocaleChange = Readonly<{
  en: LocaleCatalogValue;
  pt: LocaleCatalogValue;
}>;

export type AdminLocaleFailureStatus =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'conflict'
  | 'timeout'
  | 'malformed-response'
  | 'unavailable';

export type AdminLocaleResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{
      status: AdminLocaleFailureStatus;
      message: string;
      errorKey?: string;
    }>;

export type AdminPostContent = Readonly<{
  title: string;
  excerpt: string;
  body: string;
  coverImageAlt: string | null;
}>;

export type AdminPostContentInputLocale = {
  title: string;
  excerpt: string;
  body: string;
  coverImageAlt?: string | null;
};

export type AdminPostContentInput = {
  en: AdminPostContentInputLocale;
  pt: AdminPostContentInputLocale;
};

export type AdminPostStatus = 'draft' | 'published' | 'unpublished';

export type AdminPostSummary = Readonly<{
  id: string;
  slug: string;
  status: AdminPostStatus;
  publishedAt: string | null;
  updatedAt: string;
  revision: number;
}>;

export type AdminPost = AdminPostSummary & Readonly<{
  content: Readonly<Record<PostLocale, AdminPostContent>>;
  coverImageUrl: string | null;
  tags: readonly string[];
}>;

export type AdminPostList = Readonly<{
  items: readonly AdminPostSummary[];
  page: number;
  pageSize: number;
  hasNext: boolean;
}>;

export type AdminPostFailureStatus =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'not-found'
  | 'conflict'
  | 'timeout'
  | 'malformed-response'
  | 'unavailable';

export type AdminPostResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{
      status: AdminPostFailureStatus;
      message: string;
      errorKey?: string;
    }>;

export type AdminPostEditorInput = {
  slug: string;
  content: AdminPostContentInput;
  coverImageUrl: string;
  tags: string[];
};

export type AdminProjectStatus = 'draft' | 'published' | 'unpublished';

export type AdminProjectLocalizedContent = Readonly<{
  title: string;
  summary: string;
  details: string;
  featureHeading: string;
  features: readonly string[];
}>;

export type AdminProjectContentInput = {
  en: AdminProjectLocalizedContent;
  pt: AdminProjectLocalizedContent;
};

export type AdminProjectSummary = Readonly<{
  id: number;
  sortOrder: number;
  status: AdminProjectStatus;
  imageReference: string;
  titles: Readonly<Record<PostLocale, string>>;
  publishedAt: string | null;
  updatedAt: string;
  revision: number;
}>;

export type AdminProject = AdminProjectSummary & Readonly<{
  repositoryUrl: string | null;
  liveUrl: string | null;
  technologies: readonly string[];
  content: Readonly<Record<PostLocale, AdminProjectLocalizedContent>>;
}>;

export type AdminProjectList = Readonly<{
  items: readonly AdminProjectSummary[];
  page: number;
  pageSize: number;
  hasNext: boolean;
}>;

export type AdminProjectEditorInput = {
  sortOrder: number;
  imageReference: string;
  repositoryUrl?: string;
  liveUrl?: string;
  technologies: readonly string[];
  content?: AdminProjectContentInput;
};

export type AdminProjectReorderItem = Readonly<{
  id: number;
  expectedRevision: number;
}>;

export type AdminProjectResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{
      status: AdminPostFailureStatus;
      message: string;
      errorKey?: string;
    }>;

export type AdminEmailDeliveryPolicy = Readonly<{
  enabled: boolean;
  recipientAddress: string;
  senderDisplayName: string;
  subjectPrefix: string;
  revision: number;
  transportConfigured: boolean;
}>;

export type AdminEmailDeliveryInput = Readonly<{
  enabled: boolean;
  recipientAddress: string;
  senderDisplayName: string;
  subjectPrefix: string;
}>;

export type AdminEmailDeliveryFailureStatus =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'conflict'
  | 'throttled'
  | 'timeout'
  | 'malformed-response'
  | 'unavailable';

export type AdminEmailDeliveryResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{
      status: AdminEmailDeliveryFailureStatus;
      message: string;
      errorKey?: string;
    }>;
