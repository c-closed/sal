import hashlib, os, sys

install_dir = sys.argv[1]
expected_sha = 'ef909f6d0a328fd90ad8334a5dd400650566c576b8195060825ccac094ed75c2'

# Check base_library.zip specifically
zf = os.path.join(install_dir, '_internal', 'base_library.zip')
if os.path.exists(zf):
    with open(zf, 'rb') as f:
        sha = hashlib.sha256(f.read()).hexdigest()
        print(f'base_library.zip: {sha}')
else:
    print('base_library.zip not found')

# Check Sboard 접속기.exe
exe_path = None
for root, dirs, files in os.walk(install_dir):
    for f in files:
        if f.endswith('.exe') and 'Sboard' in f:
            exe_path = os.path.join(root, f)
            with open(exe_path, 'rb') as fh:
                sha = hashlib.sha256(fh.read()).hexdigest()
                print(f'{f}: {sha}')

# Full SHA
_skip = {'updater.exe', 'unins000.exe', 'unins000.dat', 'Sboard_Updated.zip'}
h = hashlib.sha256()
entries = []
for root, dirs, files in os.walk(install_dir):
    rel = os.path.relpath(root, install_dir)
    for f in sorted(files):
        if f in _skip:
            continue
        if f == 'Sboard_Updated.zip':
            continue
        rpath = os.path.join(rel, f) if rel != '.' else f
        entries.append(rpath)
for e in sorted(entries):
    fp = os.path.join(install_dir, e)
    with open(fp, 'rb') as fh:
        while True:
            chunk = fh.read(65536)
            if not chunk:
                break
            h.update(chunk)
sha = h.hexdigest()
print(f'Full SHA: {sha}')
print(f'Expected: {expected_sha}')
print(f'Match: {sha == expected_sha}')
