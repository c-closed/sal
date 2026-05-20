import sys, time, os, subprocess, shutil, tempfile

def main():
    if len(sys.argv) < 4:
        sys.exit(1)
    installer = sys.argv[1]
    install_dir = sys.argv[2]
    main_exe = sys.argv[3]
    old_pid = int(sys.argv[4]) if len(sys.argv) > 4 else None

    if old_pid:
        while True:
            try:
                os.kill(old_pid, 0)
                time.sleep(0.3)
            except OSError:
                break

    extract_dir = tempfile.mkdtemp()
    try:
        subprocess.run([installer, f"/EXTRACT:{extract_dir}", "/NOICONS", "/SUPPRESSMSGBOXES"], check=True, capture_output=True)

        src_dir = None
        for root, dirs, files in os.walk(extract_dir):
            if main_exe in files:
                src_dir = root
                break
        if not src_dir:
            for item in os.listdir(extract_dir):
                item_path = os.path.join(extract_dir, item)
                if os.path.isdir(item_path):
                    candidate = os.path.join(item_path, main_exe)
                    if os.path.exists(candidate):
                        src_dir = item_path
                        break
        if not src_dir:
            for item in os.listdir(extract_dir):
                item_path = os.path.join(extract_dir, item)
                if os.path.isdir(item_path):
                    src_dir = item_path
                    break
        if not src_dir:
            src_dir = extract_dir

        updater_path = os.path.join(install_dir, "updater.exe")
        for item in os.listdir(src_dir):
            src = os.path.join(src_dir, item)
            dst = os.path.join(install_dir, item)
            if os.path.abspath(src).lower() == os.path.abspath(updater_path).lower():
                continue
            if os.path.isdir(src):
                if os.path.exists(dst):
                    shutil.rmtree(dst, ignore_errors=True)
                shutil.copytree(src, dst, ignore_dangling_symlinks=True)
            else:
                shutil.copy2(src, dst)

        for item in os.listdir(install_dir):
            item_path = os.path.join(install_dir, item)
            src_check = os.path.join(src_dir, item)
            if os.path.abspath(item_path).lower() == os.path.abspath(updater_path).lower():
                continue
            if not os.path.exists(src_check):
                if os.path.isdir(item_path):
                    shutil.rmtree(item_path, ignore_errors=True)
                else:
                    try:
                        os.remove(item_path)
                    except:
                        pass
    finally:
        shutil.rmtree(extract_dir, ignore_errors=True)
        try:
            os.remove(installer)
        except:
            pass

    subprocess.Popen([os.path.join(install_dir, main_exe)])

if __name__ == "__main__":
    main()
