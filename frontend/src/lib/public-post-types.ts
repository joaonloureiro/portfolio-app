export type PublicPostSummary = Readonly<{
  slug: string;
  publishedAt: string;
  title: string;
  excerpt: string;
  coverImageUrl: string | null;
  coverImageAlt: string | null;
  tags: readonly string[];
}>;

export type PublicPost = PublicPostSummary & Readonly<{
  body: string;
}>;

export type PublicPostList = Readonly<{
  items: readonly PublicPostSummary[];
  page: number;
  pageSize: number;
  hasNext: boolean;
}>;

export type PublicPostResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{status: 'not-found' | 'timeout' | 'malformed-response' | 'unavailable'; message: string}>;
