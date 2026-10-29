import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import path from 'node:path';

/**
 * The seeded fixtures, read straight from the files the TestHost boots with, so content strings are
 * never duplicated in the specs. Expected orders and counts stay as literals in the specs: those are
 * the rendering rules under test.
 */

const hostRoot = path.join(__dirname, '..', '..');

interface SeedItem {
  id: string;
  contentType: string;
  publish?: boolean;
  fields: Record<string, unknown>;
}

function load(relativePath: string): SeedItem[] {
  return JSON.parse(readFileSync(path.join(hostRoot, relativePath), 'utf8')).items as SeedItem[];
}

const faqItems = load('Fixtures/faqs.contentseed.json');
const contentItems = load('contentseed/content.json');
const pageItems = load('contentseed/pages.json');

export interface SeedFaq {
  slug: string;
  title: string;
  question: string;
  answer: string;
  rating: number;
  published: boolean;
  /** Slug of the FAQ's topic (its `faqTopicId` containment), or '' when it has none. */
  topic: string;
}

export interface SeedFaqTopic {
  id: string;
  slug: string;
  title: string;
}

export const faqTopics: SeedFaqTopic[] = faqItems
  .filter((i) => i.contentType === 'faqtopics')
  .map((i) => ({ id: i.id, slug: String(i.fields.slug), title: String(i.fields.title) }));

function topicSlug(ref: unknown): string {
  const id = typeof ref === 'string' ? ref.replace(/^@seed:/, '') : '';
  return faqTopics.find((t) => t.id === id)?.slug ?? '';
}

export const faqs: SeedFaq[] = faqItems.filter((i) => i.contentType === 'faqs').map((i) => ({
  slug: String(i.fields.slug),
  title: String(i.fields.title),
  question: String(i.fields.question),
  answer: String(i.fields.answer),
  rating: Number(i.fields.helpfulnessRating),
  published: i.publish !== false,
  topic: topicSlug(i.fields.faqTopicId),
}));

export function faq(slug: string): SeedFaq {
  const found = faqs.find((f) => f.slug === slug);
  if (!found) throw new Error(`No seeded FAQ with slug '${slug}'`);
  return found;
}

/** Fields of a seeded content item (content block, image, article list, article) by slug. */
export function content(slug: string): Record<string, string> {
  const found = contentItems.find((i) => i.fields.slug === slug);
  if (!found) throw new Error(`No seeded content with slug '${slug}'`);
  return found.fields as Record<string, string>;
}

export interface SeedPage {
  slug: string;
  title: string;
  config: Record<string, unknown>;
}

export const pages: SeedPage[] = pageItems.map((i) => ({
  slug: String(i.fields.slug),
  title: String(i.fields.title),
  config: JSON.parse(String(i.fields.configurationJson ?? '{}')),
}));

export function page(slug: string): SeedPage {
  const found = pages.find((p) => p.slug === slug);
  if (!found) throw new Error(`No seeded page with slug '${slug}'`);
  return found;
}

/** Visible text of a seeded rich-text value (tags stripped). */
export function textOf(html: string): string {
  return html.replace(/<[^>]+>/g, '').trim();
}

/** The seeded image file, its SHA-256 (the media URL's content address) and its PNG dimensions. */
export const testPattern = (() => {
  const bytes = readFileSync(path.join(hostRoot, 'contentseed', 'media', 'test-pattern.png'));
  return {
    bytes,
    hash: createHash('sha256').update(bytes).digest('hex'),
    width: bytes.readUInt32BE(16),
    height: bytes.readUInt32BE(20),
  };
})();

export const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
