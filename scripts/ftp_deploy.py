#!/usr/bin/env python3
"""FTP deploy for balonpark.com: wipe httpdocs (keep wwwroot/uploads), then upload publish output."""
from __future__ import annotations

import argparse
import os
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor
from ftplib import FTP, error_perm, error_temp
from pathlib import Path
from typing import Callable


PRESERVE_PREFIX = "/httpdocs/wwwroot/uploads"
REMOTE_ROOT = "/httpdocs"
SKIP_NAMES = frozenset({"appsettings.Development.json"})
SKIP_SUFFIXES = frozenset({".pdb"})


class FtpSession:
    """FTP session with reconnect + cached MKD."""

    def __init__(self, host: str, user: str, password: str, port: int = 21) -> None:
        self.host = host
        self.user = user
        self.password = password
        self.port = port
        self.ftp: FTP | None = None
        self._known_dirs: set[str] = set()
        self.connect()

    def connect(self) -> None:
        if self.ftp is not None:
            try:
                self.ftp.close()
            except Exception:
                pass
        ftp = FTP()
        ftp.connect(self.host, self.port, timeout=120)
        ftp.login(self.user, self.password)
        ftp.set_pasv(True)
        ftp.encoding = "utf-8"
        self.ftp = ftp
        self._known_dirs.clear()
        print(f"FTP connected: {self.host}:{self.port}  PWD={ftp.pwd()}", flush=True)

    def close(self) -> None:
        if self.ftp is None:
            return
        try:
            self.ftp.quit()
        except Exception:
            try:
                self.ftp.close()
            except Exception:
                pass
        self.ftp = None

    def alive(self) -> bool:
        assert self.ftp is not None
        try:
            self.ftp.voidcmd("NOOP")
            return True
        except Exception:
            return False

    def ensure_alive(self) -> None:
        if not self.alive():
            print("  FTP connection dropped, reconnecting...", flush=True)
            self.connect()

    def with_retry(self, op: Callable[[], None], attempts: int = 3, label: str = "") -> None:
        last: Exception | None = None
        for i in range(1, attempts + 1):
            try:
                self.ensure_alive()
                op()
                return
            except (error_temp, error_perm, OSError, EOFError, ConnectionError) as ex:
                last = ex
                print(f"  retry {i}/{attempts}{(' ' + label) if label else ''}: {ex}", flush=True)
                time.sleep(min(2 * i, 6))
                self.connect()
        assert last is not None
        raise last


def progress_bar(done: int, total: int, width: int = 28) -> str:
    if total <= 0:
        pct = 100
    else:
        pct = min(100, int(done * 100 / total))
    filled = int(width * pct / 100)
    return f"[{('#' * filled) + ('-' * (width - filled))}] {pct:3d}% ({done}/{total})"


def parse_list_line(line: str) -> tuple[str, str] | None:
    if "<DIR>" in line:
        name = line.split("<DIR>")[-1].strip()
        return ("dir", name) if name else None
    parts = line.split()
    if len(parts) < 4:
        return None
    name = " ".join(parts[3:])
    return ("file", name) if name else None


def list_children(session: FtpSession, path: str) -> list[tuple[str, str]]:
    assert session.ftp is not None
    session.ftp.cwd(path)
    lines: list[str] = []
    session.ftp.retrlines("LIST", lines.append)
    out: list[tuple[str, str]] = []
    for line in lines:
        parsed = parse_list_line(line)
        if not parsed:
            continue
        kind, name = parsed
        if name in (".", ".."):
            continue
        out.append((kind, name))
    return out


def is_preserved(path: str) -> bool:
    p = path.rstrip("/")
    return p == PRESERVE_PREFIX or p.startswith(PRESERVE_PREFIX + "/")


def collect_deletable(session: FtpSession, root: str = REMOTE_ROOT) -> tuple[list[str], list[str]]:
    files: list[str] = []
    dirs: list[str] = []
    stack = [root]
    while stack:
        cur = stack.pop()
        if is_preserved(cur):
            continue
        try:
            children = list_children(session, cur)
        except error_perm:
            continue
        for kind, name in children:
            path = f"{cur.rstrip('/')}/{name}"
            if is_preserved(path):
                continue
            if kind == "dir":
                dirs.append(path)
                stack.append(path)
            else:
                files.append(path)
    dirs.sort(key=lambda p: p.count("/"), reverse=True)
    return files, dirs


def is_preserve_ancestor(path: str) -> bool:
    """Never delete an ancestor of uploads, such as wwwroot."""
    p = path.rstrip("/")
    return PRESERVE_PREFIX.startswith(p + "/")


def open_worker_ftp(host: str, user: str, password: str, port: int) -> FTP:
    ftp = FTP()
    ftp.connect(host, port, timeout=60)
    ftp.login(user, password)
    ftp.set_pasv(True)
    ftp.encoding = "utf-8"
    return ftp


def parallel_delete(
    host: str,
    user: str,
    password: str,
    port: int,
    paths: list[str],
    kind: str,
    workers: int = 12,
    label: str = "item",
) -> list[str]:
    """Each worker owns its FTP connection and does not reconnect on access denied."""
    if not paths:
        return []

    tls = threading.local()
    lock = threading.Lock()
    failed: list[str] = []
    done = 0
    total = len(paths)
    pool = min(workers, total)

    def client() -> FTP:
        ftp = getattr(tls, "ftp", None)
        if ftp is None:
            ftp = open_worker_ftp(host, user, password, port)
            tls.ftp = ftp
        return ftp

    def reset() -> None:
        ftp = getattr(tls, "ftp", None)
        if ftp is not None:
            try:
                ftp.close()
            except Exception:
                pass
        tls.ftp = None

    def one(path: str) -> None:
        nonlocal done
        parent, name = path.rsplit("/", 1)
        err = ""
        ok = False
        for attempt in (1, 2):
            try:
                ftp = client()
                ftp.cwd(parent)
                if kind == "dir":
                    ftp.rmd(name)
                else:
                    ftp.delete(name)
                ok = True
                break
            except Exception as ex:
                err = str(ex)
                denied = "Access is denied" in err or "550" in err
                if denied:
                    break
                reset()
                if attempt == 2:
                    break
        with lock:
            done += 1
            if not ok:
                failed.append(path)
            if done % 40 == 0 or done == total:
                print(
                    f"\r  {label} {progress_bar(done, total)}  fail={len(failed)}  ",
                    end="",
                    flush=True,
                )

    print(f"  {label}: {total} entries over {pool} parallel connections", flush=True)
    with ThreadPoolExecutor(max_workers=pool) as pool_ex:
        list(pool_ex.map(one, paths))
    print(flush=True)
    return failed


def wipe(session: FtpSession, max_rounds: int = 8, settle_seconds: float = 4.0, workers: int = 12) -> bool:
    print("=== 1) Deleting remote content (kept: wwwroot/uploads) ===", flush=True)
    print("  Order: directories first (deepest to shallowest), then files, then empty directories", flush=True)
    creds = (session.host, session.user, session.password, session.port)

    for round_no in range(1, max_rounds + 1):
        session.ensure_alive()
        files, dirs = collect_deletable(session)
        dirs = [d for d in dirs if not is_preserve_ancestor(d)]
        if not files and not dirs:
            print(f"  {progress_bar(1, 1)}  nothing to delete", flush=True)
            return True

        print(
            f"  Round {round_no}/{max_rounds}: {len(dirs)} directories, {len(files)} files",
            flush=True,
        )

        # 1) Directories first, deepest ones. Non empty ones answer 550 and are not retried.
        parallel_delete(*creds, dirs, "dir", workers=workers, label="directory")
        # 2) Files in parallel
        parallel_delete(*creds, files, "file", workers=workers, label="dosya")
        # 3) Directories that just became empty
        session.ensure_alive()
        _, dirs_left = collect_deletable(session)
        dirs_left = [d for d in dirs_left if not is_preserve_ancestor(d)]
        if dirs_left:
            parallel_delete(*creds, dirs_left, "dir", workers=workers, label="empty directory")

        session.ensure_alive()
        left_files, left_dirs = collect_deletable(session)
        leftover_dirs = [d for d in left_dirs if not is_preserve_ancestor(d)]
        print(
            f"  Verification: files left={len(left_files)}  directories left={len(leftover_dirs)}",
            flush=True,
        )
        if not left_files and not leftover_dirs:
            print("  Temizlik tamam (wwwroot/uploads korundu).", flush=True)
            return True

        print(f"  {len(left_files) + len(leftover_dirs)} locked entries, waiting {settle_seconds:.0f}s...", flush=True)
        time.sleep(settle_seconds)

    session.ensure_alive()
    left_files, left_dirs = collect_deletable(session)
    leftover_dirs = [d for d in left_dirs if not is_preserve_ancestor(d)]
    print("  ERROR: entries that could not be deleted:", flush=True)
    for p in (left_files + leftover_dirs)[:40]:
        print(f"    - {p}", flush=True)
    extra = len(left_files) + len(leftover_dirs) - 40
    if extra > 0:
        print(f"    ... +{extra} daha", flush=True)
    return False


def ensure_dir(session: FtpSession, path: str) -> None:
    assert session.ftp is not None
    if path in session._known_dirs:
        return
    parts = [p for p in path.split("/") if p]
    cur = ""
    for p in parts:
        cur += "/" + p
        if cur in session._known_dirs:
            continue
        try:
            session.ftp.mkd(cur)
        except error_perm:
            pass
        session._known_dirs.add(cur)
    session._known_dirs.add(path)


def collect_local_files(local_root: Path) -> list[Path]:
    files: list[Path] = []
    for p in local_root.rglob("*"):
        if not p.is_file():
            continue
        if p.name in SKIP_NAMES or p.suffix.lower() in SKIP_SUFFIXES:
            continue
        rel = p.relative_to(local_root).as_posix()
        if rel == "wwwroot/uploads" or rel.startswith("wwwroot/uploads/"):
            continue
        files.append(p)
    files.sort()
    return files


def upload(session: FtpSession, local_root: Path) -> None:
    print("=== 2) Uploading the publish output ===", flush=True)
    files = collect_local_files(local_root)
    if not files:
        raise RuntimeError("There is nothing to upload, check the publish output")

    total = len(files)
    total_bytes = sum(f.stat().st_size for f in files)
    print(f"  {total} dosya, {total_bytes / 1024 / 1024:.1f} MB", flush=True)

    sent_bytes = 0
    t0 = time.time()
    for i, path in enumerate(files, 1):
        rel = path.relative_to(local_root).as_posix()
        remote_parent = (
            REMOTE_ROOT if "/" not in rel else f"{REMOTE_ROOT}/{os.path.dirname(rel)}"
        ).replace("//", "/")
        size = path.stat().st_size

        def _upload(p: Path = path, parent: str = remote_parent) -> None:
            assert session.ftp is not None
            ensure_dir(session, parent)
            session.ftp.cwd(parent)
            with open(p, "rb") as fh:
                session.ftp.storbinary(f"STOR {p.name}", fh)

        session.with_retry(_upload, attempts=3, label=rel)
        sent_bytes += size

        if i % 20 == 0 or i == total:
            elapsed = max(time.time() - t0, 0.001)
            speed = (sent_bytes / 1024 / 1024) / elapsed * 60
            print(
                f"\r  {progress_bar(i, total)}  "
                f"{sent_bytes/1024/1024:.1f}/{total_bytes/1024/1024:.1f} MB  "
                f"{speed:.0f} MB/min  ",
                end="",
                flush=True,
            )
    print(flush=True)
    print("  Upload tamam.", flush=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="balonpark.com FTP wipe + upload")
    parser.add_argument("--host", default=os.environ.get("FTP_HOST", ""), required=False)
    parser.add_argument("--user", default=os.environ.get("FTP_USER", ""), required=False)
    parser.add_argument("--password", default="", help="Prefer FTP_PASS env (not argv)")
    parser.add_argument("--port", type=int, default=int(os.environ.get("FTP_PORT", "21")))
    parser.add_argument("--local", required=True, help="Publish output directory")
    parser.add_argument("--wipe-only", action="store_true")
    parser.add_argument("--upload-only", action="store_true")
    args = parser.parse_args()

    host = args.host or os.environ.get("FTP_HOST", "")
    user = args.user or os.environ.get("FTP_USER", "")
    password = args.password or os.environ.get("FTP_PASS", "")
    if not host or not user or not password:
        print("FTP_HOST / FTP_USER / FTP_PASS are required (.deploy.env or environment).", file=sys.stderr)
        return 1

    local = Path(args.local)
    if not args.wipe_only and not local.is_dir():
        print(f"Publish folder is missing: {local}", file=sys.stderr)
        return 1

    if not args.wipe_only:
        exe = local / "BalonPark.exe"
        cfg = local / "web.config"
        if not exe.is_file() or not cfg.is_file():
            print(f"Publish eksik: BalonPark.exe / web.config → {local}", file=sys.stderr)
            return 1

    session = FtpSession(host, user, password, args.port)
    try:
        if not args.upload_only:
            if not wipe(session):
                return 2
        if not args.wipe_only:
            upload(session, local)
    finally:
        session.close()

    print("=== FTP deploy steps finished ===", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
