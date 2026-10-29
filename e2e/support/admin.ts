import path from 'node:path';

/** Saved admin session written by tests/auth.setup.ts and used by admin.spec.ts. */
export const ADMIN_STATE = path.join(__dirname, '..', '.auth', 'admin.json');
