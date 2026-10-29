import { expect, request, test, type APIRequestContext } from '@playwright/test';

// MCP smoke over streamable HTTP: the endpoint answers with the compose key, refuses a bad one, and
// the tool surface advertises the media tool and the relation fields (read-only; no content writes).
// The key matches WebWayCMS.TestHost/docker-compose.yml (a throwaway test stack), like auth.setup.ts.
const apiKey = process.env.MCP_APIKEY ?? '9d7437edbe9cd29b4b71f53a51b66378bc3b1808355a7e47';

const INITIALIZE = {
  jsonrpc: '2.0',
  id: 1,
  method: 'initialize',
  params: {
    protocolVersion: '2025-03-26',
    capabilities: {},
    clientInfo: { name: 'e2e-smoke', version: '1.0' },
  },
};

let session = '';

/** The headers an MCP POST needs: the bearer key, both content types, and the session once opened. */
function headers(key: string = apiKey): Record<string, string> {
  return {
    Authorization: `Bearer ${key}`,
    Accept: 'application/json, text/event-stream',
    ...(session ? { 'Mcp-Session-Id': session } : {}),
  };
}

/** Parses the single `data: …` SSE line of a streamable-HTTP MCP response. */
async function sseJson(res: Awaited<ReturnType<APIRequestContext['post']>>): Promise<any> {
  const body = await res.text();
  const line = body.split('\n').reverse().find((l) => l.startsWith('data: '));
  expect(line, 'SSE data line').not.toBeUndefined();
  return JSON.parse(line!.slice('data: '.length));
}

async function callTool(request: APIRequestContext, name: string, args: Record<string, unknown>): Promise<any> {
  const res = await request.post('/mcp', {
    headers: headers(),
    data: { jsonrpc: '2.0', id: 2, method: 'tools/call', params: { name, arguments: args } },
  });
  expect(res.status(), `status of ${name}`).toBe(200);
  const json = await sseJson(res);
  expect(json.result.isError, `${name} failed`).toBeFalsy();
  return JSON.parse(json.result.content[0].text);
}

test.beforeAll(async () => {
  const context = await request.newContext({ baseURL: process.env.BASE_URL ?? 'http://localhost:45847' });
  const res = await context.post('/mcp', { headers: headers(), data: INITIALIZE });
  expect(res.status(), 'initialize').toBe(200);
  // The server may run stateless and issue no session id; the header is only sent when present.
  session = res.headers()['mcp-session-id'] ?? '';
  await context.post('/mcp', {
    headers: headers(),
    data: { jsonrpc: '2.0', method: 'notifications/initialized' },
  });
  await context.dispose();
});

test('a bad key is refused (fail closed)', async ({ request }) => {
  const res = await request.post('/mcp', {
    headers: headers('not-the-key'),
    data: { jsonrpc: '2.0', id: 1, method: 'tools/list' },
  });
  expect(res.status()).toBe(401);
});

test('the tool surface includes the media list and relation-aware tools', async ({ request }) => {
  const res = await request.post('/mcp', {
    headers: headers(),
    data: { jsonrpc: '2.0', id: 2, method: 'tools/list' },
  });
  expect(res.status()).toBe(200);
  const names: string[] = (await sseJson(res)).result.tools.map((t: { name: string }) => t.name);
  expect(names).toContain('list_media');
  expect(names).toContain('describe_content_type');
  expect(names).toContain('create_child');
});

test('describe_content_type advertises the containment relations', async ({ request }) => {
  const faqs = await callTool(request, 'describe_content_type', { contentType: 'faqs' });
  const topic = faqs.fields.find((f: { name: string }) => f.name === 'faqTopicId');
  expect(topic.relation).toEqual({ kind: 'containment', target: 'faqtopics', many: false, required: false });

  const pages = await callTool(request, 'describe_content_type', { contentType: 'pages' });
  const parent = pages.fields.find((f: { name: string }) => f.name === 'parentNodeId');
  expect(parent.relation).toEqual({ kind: 'containment', target: 'pages', many: false, required: false });
});

test('describe_content_type leaves plain types relation-free', async ({ request }) => {
  const blocks = await callTool(request, 'describe_content_type', { contentType: 'contentblocks' });
  expect(blocks.fields.every((f: { relation: unknown }) => f.relation === null)).toBe(true);
});
