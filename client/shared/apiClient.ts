import type { ApiProblem } from './types';

declare global {
  interface Window {
    __API_BASE_URL__?: string;
  }
}

function baseUrl(): string {
  const url = window.__API_BASE_URL__;
  if (!url) {
    throw new Error('window.__API_BASE_URL__ is not set — check _Layout.cshtml\'s injection.');
  }
  return url;
}

// Maps the API's actual error shape: 400 validation, 404 not found, 409 conflict,
// all as { status, detail } from ExceptionHandlingMiddleware. Anything else (e.g. an
// unhandled 500) falls back to a generic message rather than showing raw JSON/HTML.
export async function toErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as ApiProblem;
    if (body?.detail) return body.detail;
  } catch {
    // response body wasn't JSON — fall through to the generic message
  }
  return `Request failed (${response.status}).`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl()}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!response.ok) {
    throw new Error(await toErrorMessage(response));
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (path: string) => request<void>(path, { method: 'DELETE' }),
};
