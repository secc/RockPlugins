// One-shot: remap data.json ConfigurationName from backup-DB names
// to production rockbeta dropdown labels. Drops configs with no
// production match. Renumbers Id contiguously. Run from this directory:
//   node remap-config-names.mjs
import { readFileSync, writeFileSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const here = dirname(fileURLToPath(import.meta.url));
const file = join(here, 'data.json');

const rename = {
    'BB Kids': 'BB Kids and Volunteers',
    'BC Kids': 'BC Kids and Volunteers',
    'BT Kids': 'BT Kids and Volunteers',
    'CW Kids': 'CW Kids and Volunteers',
    'ET Kids': 'ET Kids and Volunteers',
    'FR Kids': 'FR Kids & Volunteers',
    'IN Kids': 'IN Kids and Volunteers',
    'LA Kids': 'LA Kids & Volunteers',
    'NC Kids': 'NC Kids & Volunteers',
    'PR Kids': 'PR Kids & Volunteers',
    'SC Kids': 'SC Kids and Volunteers',
    'SL Kids': 'SL Kids and Volunteers',
    'SW Kids': 'SW Kids and Volunteers',
    'Medication Check-in': 'Medication Kiosk'
};

const drop = new Set([
    'BB Student',                  // no exact match (Groups / Super Check-in)
    'Events Check-In',             // ambiguous (Events AC..SW)
    'Group Check-in',              // not in production dropdown
    'Sports and Fitness Childcare' // not in production dropdown
]);

const data = JSON.parse(readFileSync(file, 'utf8'));

const before = data.length;
const out = data
    .filter(r => !drop.has(r.ConfigurationName))
    .map(r => ({ ...r, ConfigurationName: rename[r.ConfigurationName] ?? r.ConfigurationName }))
    .map((r, i) => ({ ...r, Id: i + 1 }));

writeFileSync(file, JSON.stringify(out));

const counts = {};
for (const r of out) counts[r.ConfigurationName] = (counts[r.ConfigurationName] ?? 0) + 1;

console.log(`Rows: ${before} -> ${out.length} (${before - out.length} dropped)`);
console.log('Per-config:');
for (const [name, n] of Object.entries(counts).sort((a, b) => b[1] - a[1])) {
    console.log(`  ${n.toString().padStart(5)}  ${name}`);
}
