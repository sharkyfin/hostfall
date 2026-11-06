import { readFile, writeFile, rm, readdir } from 'node:fs/promises';
import { resolve, sep } from 'node:path';
import { spawnSync } from 'node:child_process';

// Package only Vite's public output. Never synchronize the source checkout.
const output = resolve('dist');
let html = await readFile(resolve(output, 'index.html'), 'utf8');
async function asset(url) {
  const path = resolve(output, url.replace(/^\.\//, ''));
  if (!path.startsWith(output + sep)) throw new Error('Asset escaped the output directory');
  return readFile(path, 'utf8');
}
const scriptTags = [...html.matchAll(/<script\b[^>]*\bsrc="([^"]+)"[^>]*><\/script>/g)];
const styleTags = [...html.matchAll(/<link\b[^>]*\bhref="([^"]+\.css)"[^>]*>/g)];
for (const match of scriptTags) {
  const code = (await asset(match[1])).replace(/<\/script/gi, '<\\/script');
  // A function replacement preserves literal $&, $` and $' in compiled JS.
  html = html.replace(match[0], () => `<script type="module">${code}</script>`);
}
for (const match of styleTags) {
  const css = await asset(match[1]);
  html = html.replace(match[0], () => `<style>${css}</style>`);
}
const shell = html.replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '').replace(/<style>[\s\S]*?<\/style>/g, '');
if (/<script\b[^>]*\bsrc=|<link\b[^>]*\bhref="[^\"]+\.css/.test(shell)) throw new Error('Unresolved browser dependency');
const embeddedCode = html.match(/<script type="module">([\s\S]*?)<\/script>/)?.[1];
if (!embeddedCode) throw new Error('Missing embedded game module');
const syntax = spawnSync(process.execPath, ['--input-type=module', '--check'], { input: embeddedCode, encoding: 'utf8' });
if (syntax.status !== 0) throw new Error(`Embedded module syntax check failed: ${syntax.stderr}`);
await writeFile(resolve(output, 'index.html'), html);
for (const entry of await readdir(output)) if (entry !== 'index.html') await rm(resolve(output, entry), { recursive: true, force: true });
console.info(`Websim bundle: dist/index.html · ${Math.round(Buffer.byteLength(html) / 1024)} KB · no runtime CDN dependencies`);
