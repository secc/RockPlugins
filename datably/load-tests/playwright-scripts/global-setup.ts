import * as dotenv from 'dotenv';
import * as fs from 'fs';
import * as path from 'path';

// Maps the ConfigurationName values that ship in data.json (backup-DB names)
// to the exact Kiosk Type dropdown labels on the target server. The check-in
// script selects the kiosk type by these labels, so a mismatch makes
// selectOption hang. Anything not renamed and not dropped is passed through
// unchanged (it already matches a dropdown option, e.g. "BB Other Volunteers").
const rename: Record<string, string> = {
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
    'Medication Check-in': 'Medication Kiosk',
    'BB Student': 'BB Student Groups',
};

// Configs with no production dropdown match; excluded from the load test.
const drop = new Set<string>([
    'Events Check-In',              // ambiguous (Events AC..SW)
    'Group Check-in',               // not in production dropdown
    'Sports and Fitness Childcare', // not in production dropdown
]);

type Row = { Id: number; ConfigurationName: string; [k: string]: unknown };

function normalize(rows: Row[]): Row[] {
    return rows
        .filter(r => !drop.has(r.ConfigurationName))
        .map(r => ({ ...r, ConfigurationName: rename[r.ConfigurationName] ?? r.ConfigurationName }))
        .map((r, i) => ({ ...r, Id: i + 1 })); // renumber contiguously for VM sharding
}

async function globalSetup() {
    dotenv.config({
        path: '.env',
        override: true
    });

    const dest = path.resolve(__dirname, 'check-in/data/data.json');
    const dataUrl = process.env.CHECK_IN_DATA_URL;

    let body: string;
    if (dataUrl) {
        console.log(`Downloading test data from ${dataUrl} -> ${dest}`);
        const res = await fetch(dataUrl);
        if (!res.ok) {
            throw new Error(`Failed to download ${dataUrl}: ${res.status} ${res.statusText}`);
        }
        body = await res.text();
    } else {
        console.log('CHECK_IN_DATA_URL not set; using local check-in/data/data.json');
        body = fs.readFileSync(dest, { encoding: 'utf8' });
    }

    const rows = JSON.parse(body) as Row[]; // bail before write if response isn't valid JSON
    const normalized = normalize(rows);

    const dropped = rows.length - normalized.length;
    fs.writeFileSync(dest, JSON.stringify(normalized), { encoding: 'utf8' });
    console.log(`Normalized ${rows.length} rows -> ${normalized.length} (${dropped} dropped) and wrote ${dest}`);
}

module.exports = globalSetup;
