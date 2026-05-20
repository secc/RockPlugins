import * as dotenv from 'dotenv';
import * as fs from 'fs';
import * as path from 'path';

async function globalSetup() {
    dotenv.config({
        path: '.env',
        override: true
    });

    const dataUrl = process.env.CHECK_IN_DATA_URL;
    if (!dataUrl) {
        console.log('CHECK_IN_DATA_URL not set; using local check-in/data/data.json');
        return;
    }

    const dest = path.resolve(__dirname, 'check-in/data/data.json');
    console.log(`Downloading test data from ${dataUrl} -> ${dest}`);

    const res = await fetch(dataUrl);
    if (!res.ok) {
        throw new Error(`Failed to download ${dataUrl}: ${res.status} ${res.statusText}`);
    }
    const body = await res.text();

    JSON.parse(body); // bail before write if response isn't valid JSON

    fs.writeFileSync(dest, body, { encoding: 'utf8' });
    console.log(`Wrote ${body.length} bytes to ${dest}`);
}

module.exports = globalSetup;
