const cfb = require('cfb');
const fs = require('fs');

const msiPath = process.argv[2];
const cabPath = process.argv[3];

const msiData = fs.readFileSync(msiPath);
let doc = cfb.read(msiData, {type: 'buffer'});

// Find ALL streams with MSCF content (CAB data)
const cabNames = [];
doc.FileIndex.forEach((f, i) => {
    if (f.content && f.content.length > 100000 &&
        f.content[0] === 0x4D && f.content[1] === 0x53 &&
        f.content[2] === 0x43 && f.content[3] === 0x46) {
        cabNames.push(f.name);
        console.log('Found CAB stream: name=' + f.name + ' hex=' + Buffer.from(f.name, 'utf16le').toString('hex') + ' size=' + f.content.length);
    }
});

// Read the original CAB file
const cabData = fs.readFileSync(cabPath);
console.log('Original CAB: ' + cabData.length + ' bytes');

// Remove ALL old CAB streams
cabNames.forEach((name, i) => {
    console.log('Removing [' + i + ']: ' + name);
    try { cfb.utils.cfb_del(doc, name); } catch(e) {
        console.log('  Failed: ' + e.message);
    }
});

// Add the new CAB stream with the CORRECT name
// The stream must be named exactly 'Binary.SetupCab' for the Media table #Binary.SetupCab reference
console.log('Adding Binary.SetupCab...');
cfb.utils.cfb_add(doc, '/Binary.SetupCab', cabData, {type: 'buffer'});

// Write MSI
const newMsiData = cfb.write(doc, {type: 'buffer'});
fs.writeFileSync(msiPath, newMsiData);
console.log('Wrote MSI: ' + newMsiData.length + ' bytes');

// Verify
doc = cfb.read(newMsiData, {type: 'buffer'});
let found = [];
doc.FileIndex.forEach((f, i) => {
    if (f.content && f.content.length > 100000 &&
        f.content[0] === 0x4D && f.content[1] === 0x53 &&
        f.content[2] === 0x43 && f.content[3] === 0x46) {
        const nameHex = Buffer.from(f.name, 'utf16le').toString('hex');
        found.push({name: f.name, hex: nameHex, size: f.content.length, index: i});
    }
});

if (found.length === 1 && found[0].hex === '420069006e006100720079002e0053006500740075007000430061006200') {
    console.log('SUCCESS: Binary.SetupCab stream is correct!');
    console.log('  Name hex: ' + found[0].hex);
    console.log('  Size: ' + found[0].size + ' bytes');
    process.exit(0);
} else {
    console.log('ERROR: Expected exactly 1 CAB stream with name Binary.SetupCab');
    console.log('  Found ' + found.length + ' CAB streams:');
    found.forEach(f => console.log('  - hex=' + f.hex + ' size=' + f.size));
    process.exit(1);
}
