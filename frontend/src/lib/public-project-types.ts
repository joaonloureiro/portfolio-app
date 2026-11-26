export type PublicProjectSummary = Readonly<{
  id: number;
  sortOrder: number;
  imageReference: string;
  repositoryUrl: string | null;
  liveUrl: string | null;
  technologies: readonly string[];
  title: string;
  summary: string;
}>;

export type PublicProject = PublicProjectSummary & Readonly<{
  details: string;
  featureHeading: string;
  features: readonly string[];
}>;

export type PublicProjectList = Readonly<{
  items: readonly PublicProjectSummary[];
  page: number;
  pageSize: number;
  hasNext: boolean;
}>;

export type PublicProjectResult<T> =
  | Readonly<{status: 'success'; data: T}>
  | Readonly<{status: 'not-found' | 'timeout' | 'malformed-response' | 'unavailable'; message: string}>;
