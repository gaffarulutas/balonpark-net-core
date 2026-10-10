#!/usr/bin/env python3
"""Sync balonpark.com catalog into balapark.gr (Greek) and balapark.de (German).

Source database and source files are read-only. Target rows are inserted or updated.
Target-only rows are left in place. Product image files are uploaded to the target
FTP uploads folders; nothing is deleted on either side.
"""
from __future__ import annotations

import hashlib
import json
import os
import re
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from decimal import Decimal
from ftplib import FTP, error_perm, error_temp
from io import BytesIO
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote
from urllib.request import Request, urlopen

import pymssql

ROOT = Path(__file__).resolve().parents[1]
APPSETTINGS = ROOT / "BalonPark" / "appsettings.json"
STATE_PATH = ROOT / ".publish" / "catalog_sync_state.json"
CACHE_PATH = ROOT / ".publish" / "catalog_sync_translations.json"
SOURCE_DB = "balonpar_balon"
SOURCE_HTTP = "https://balonpark.com"
OPENAI_URL = "https://api.openai.com/v1/chat/completions"
MODEL = "gpt-4o-mini"
UA = "Mozilla/5.0 (compatible; BalonParkCatalogSync/1.0)"

TR_SPECIFIC = re.compile(r"[şŞğĞıİçÇ]")
GREEK_LETTERS = re.compile(r"[\u0370-\u03FF\u1F00-\u1FFF]")

TEXT_FIELDS = (
    "Name",
    "Description",
    "TechnicalDescription",
    "Summary",
    "WarrantyDescription",
    "AfterSalesService",
    "FanDescription",
    "ColorOptions",
    "DeliveryDays",
)
PRODUCT_SPECS = (
    "CategoryId",
    "SubCategoryId",
    "Price",
    "Stock",
    "IsActive",
    "DisplayOrder",
    "InflatedLength",
    "InflatedWidth",
    "InflatedHeight",
    "UserCount",
    "FanWeightKg",
    "PackagedLength",
    "PackagedDepth",
    "PackagedWeightKg",
    "PackagePalletCount",
    "HasCertificate",
    "RequiredPersonCount",
    "IsDiscounted",
    "IsPopular",
    "IsProjectSpecial",
    "IsFireResistant",
    "MaterialWeight",
    "InflatedWeightKg",
    "DeliveryDaysMin",
    "DeliveryDaysMax",
    "AssemblyTime",
    "MaterialWeightGrm2",
)
LIMITS = {
    "category": {"Name": 100, "Description": 500, "Slug": 200},
    "subcategory": {"Name": 100, "Description": 500, "Slug": 200},
    "product": {
        "Name": 200,
        "Slug": 200,
        "Summary": 500,
        "FanDescription": 200,
        "WarrantyDescription": 100,
        "AfterSalesService": 500,
        "DeliveryDays": 100,
        "ColorOptions": 200,
    },
}

STATE_LOCK = threading.Lock()
CACHE_LOCK = threading.Lock()


def log(msg: str) -> None:
    print(msg, flush=True)


def load_targets() -> list[dict]:
    cfg = json.loads(APPSETTINGS.read_text(encoding="utf-8"))
    targets = cfg["CatalogSync"]["Targets"]
    source_cs = cfg["ConnectionStrings"]["DefaultConnection"]
    if "balonpar_balon" not in source_cs:
        raise SystemExit("Refusing to run: source connection is not balonpar_balon")
    cleaned = []
    for t in targets:
        db = t["ConnectionString"]
        if "balonpar_balon" in db or "Database=balonpar_balon" in db:
            raise SystemExit(f"Refusing to write target {t.get('Name')}: points at source database")
        cleaned.append(t)
    return cleaned


def parse_cs(cs: str) -> dict:
    parts = {}
    for piece in cs.split(";"):
        if "=" not in piece:
            continue
        k, v = piece.split("=", 1)
        parts[k.strip().lower()] = v.strip()
    return dict(
        server=parts["server"],
        user=parts["user id"],
        password=parts["password"],
        database=parts["database"],
    )


def connect(cs: str):
    cfg = parse_cs(cs)
    if cfg["database"] == SOURCE_DB and os.environ.get("CATALOG_SYNC_ALLOW_SOURCE_WRITE") == "1":
        raise SystemExit("Source write flag is not used by this script")
    return pymssql.connect(
        server=cfg["server"],
        user=cfg["user"],
        password=cfg["password"],
        database=cfg["database"],
        login_timeout=30,
        timeout=180,
        charset="UTF-8",
    )


def fetch_all(cs: str, sql: str, params=None) -> list[dict]:
    cn = connect(cs)
    try:
        cur = cn.cursor()
        cur.execute(sql, params or ())
        cols = [d[0] for d in cur.description]
        return [dict(zip(cols, row)) for row in cur.fetchall()]
    finally:
        cn.close()


def source_api_key(source_cs: str) -> str:
    env = (os.environ.get("OPENAI_API_KEY") or "").strip()
    if env:
        return env
    rows = fetch_all(source_cs, "SELECT TOP 1 ChatGPTApiKey FROM Settings")
    key = (rows[0].get("ChatGPTApiKey") or "").strip() if rows else ""
    if len(key) < 20 or "your-api-key" in key.lower():
        raise SystemExit("ChatGPT API key missing in Settings")
    return key


def canon(value):
    if isinstance(value, bool):
        return int(value)
    if value is None:
        return None
    if isinstance(value, str):
        text = value.strip()
        return None if text == "" else text
    if isinstance(value, Decimal):
        return value.normalize()
    return value


def same(a, b) -> bool:
    return canon(a) == canon(b)


def clip(value: str | None, limit: int | None) -> str | None:
    if value is None:
        return None
    text = str(value).strip()
    if not text:
        return None
    if limit and len(text) > limit:
        return text[: limit - 1].rstrip() + "…"
    return text


def slugify(text: str, entity_id: int) -> str:
    mapped = (
        text.replace("ä", "ae")
        .replace("ö", "oe")
        .replace("ü", "ue")
        .replace("ß", "ss")
        .replace("Ä", "ae")
        .replace("Ö", "oe")
        .replace("Ü", "ue")
        .replace("İ", "i")
        .replace("I", "i")
        .replace("ı", "i")
        .replace("Ğ", "g")
        .replace("ğ", "g")
        .replace("Ü", "u")
        .replace("ü", "u")
        .replace("Ş", "s")
        .replace("ş", "s")
        .replace("Ö", "o")
        .replace("ö", "o")
        .replace("Ç", "c")
        .replace("ç", "c")
    )
    mapped = mapped.lower()
    mapped = re.sub(r"[^a-z0-9\s-]", "", mapped)
    mapped = re.sub(r"\s+", "-", mapped.strip())
    mapped = re.sub(r"-+", "-", mapped).strip("-")
    if not mapped:
        mapped = f"item-{entity_id}"
    return mapped[:200]


def needs_translation(locale: str, source_name: str, target_name: str | None, target_blob: str) -> bool:
    if target_name is None:
        return True
    if TR_SPECIFIC.search(target_blob or ""):
        return True
    if (target_name or "").strip() == (source_name or "").strip() and TR_SPECIFIC.search(source_name or ""):
        return True
    if locale == "el" and TR_SPECIFIC.search(source_name or "") and not GREEK_LETTERS.search(target_name or ""):
        return True
    return False


def system_prompt(locale: str) -> str:
    greek = locale == "el"
    lang = "Greek" if greek else "German"
    market = "Greece (balapark.gr)" if greek else "Germany (balapark.de)"
    return (
        "You are a professional e-commerce copywriter for inflatable playgrounds and bounce houses.\n"
        f"Brand: Bala Park. Market: {market}.\n"
        f"Translate Turkish shop catalog text into natural, sales-ready {lang}.\n"
        "Rules:\n"
        "- Keep HTML tags intact. Do not translate URLs, emails, phones, codes, or measurements like 5x4x3m.\n"
        "- Replace Balon Park / Balapark / BalonPark in prose with Bala Park.\n"
        "- Slug: lowercase a-z0-9 and hyphens only (transliterate; German ä→ae ö→oe ü→ue ß→ss; Greek to Latin).\n"
        "- The Name value MUST be written in the target language, not left in Turkish or Latin transliteration.\n"
        "- Reply ONLY with a JSON object using the same keys as the input."
    )


def load_json(path: Path, default):
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return default


def save_json(path: Path, data) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
    tmp.replace(path)


def cache_key(locale: str, kind: str, entity_id: int, fields: dict) -> str:
    raw = json.dumps({"locale": locale, "kind": kind, "id": entity_id, "fields": fields}, ensure_ascii=False, sort_keys=True)
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()


def translate_fields(api_key: str, locale: str, kind: str, entity_id: int, fields: dict, cache: dict) -> dict:
    payload = {k: v for k, v in fields.items() if isinstance(v, str) and v.strip()}
    if not payload:
        return dict(fields)
    key = cache_key(locale, kind, entity_id, payload)
    with CACHE_LOCK:
        hit = cache.get(key)
    if hit:
        return hit

    body = {
        "model": MODEL,
        "temperature": 0.2,
        "response_format": {"type": "json_object"},
        "messages": [
            {"role": "system", "content": system_prompt(locale)},
            {
                "role": "user",
                "content": f"Translate the following {kind} fields from Turkish. Return JSON with exactly the same keys.\n\n"
                + json.dumps(payload, ensure_ascii=False),
            },
        ],
    }
    last_error = None
    for attempt in range(1, 5):
        try:
            req = Request(
                OPENAI_URL,
                data=json.dumps(body).encode("utf-8"),
                headers={
                    "Authorization": f"Bearer {api_key}",
                    "Content-Type": "application/json",
                    "User-Agent": UA,
                },
                method="POST",
            )
            with urlopen(req, timeout=120) as resp:
                raw = resp.read().decode("utf-8")
            content = json.loads(raw)["choices"][0]["message"]["content"]
            parsed = json.loads(content)
            if not isinstance(parsed, dict):
                raise RuntimeError("translation payload is not an object")
            name = str(parsed.get("Name") or "")
            if locale == "el" and TR_SPECIFIC.search(fields.get("Name") or "") and not GREEK_LETTERS.search(name):
                raise RuntimeError(f"Greek name missing Greek letters: {name[:80]}")
            if TR_SPECIFIC.search(name):
                raise RuntimeError(f"translated name still has Turkish letters: {name[:80]}")
            merged = dict(fields)
            for field, value in parsed.items():
                if isinstance(value, str) and value.strip():
                    merged[field] = value.strip()
            with CACHE_LOCK:
                cache[key] = merged
                save_json(CACHE_PATH, cache)
            return merged
        except HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")[:300]
            last_error = f"HTTP {exc.code} {detail}"
            if exc.code in (401, 403):
                raise SystemExit(f"OpenAI auth failed: {last_error}") from exc
            time.sleep(min(2 ** attempt, 20))
        except Exception as exc:  # noqa: BLE001
            last_error = str(exc)
            time.sleep(min(2 ** attempt, 12))
    raise RuntimeError(f"translation failed for {kind} {entity_id}/{locale}: {last_error}")


def unique_slug(cur, table: str, slug: str, row_id: int) -> str:
    if table not in {"Categories", "SubCategories", "Products"}:
        raise RuntimeError(f"unexpected table {table}")
    base = (slug or f"item-{row_id}")[:200]
    candidate = base
    for ntry in range(30):
        cur.execute(f"SELECT COUNT(1) FROM {table} WHERE Slug=%s AND Id<>%s", (candidate, row_id))
        if cur.fetchone()[0] == 0:
            return candidate
        suffix = f"-{row_id}" if ntry == 0 else f"-{row_id}-{ntry}"
        candidate = (base[: 200 - len(suffix)] + suffix)[:200]
    return candidate


def apply_text(kind: str, translated: dict, entity_id: int) -> dict:
    limits = LIMITS[kind]
    name = clip(translated.get("Name"), limits.get("Name")) or f"item-{entity_id}"
    out = {
        "Name": name,
        "Slug": slugify(translated.get("Slug") or name, entity_id)[: limits["Slug"]],
    }
    for field, limit in limits.items():
        if field in ("Name", "Slug"):
            continue
        if field in translated:
            out[field] = clip(translated.get(field), limit)
    for field in TEXT_FIELDS:
        if field in translated and field not in out:
            out[field] = translated.get(field)
    return out


class TargetWriter:
    def __init__(self, target: dict):
        self.target = target
        self.cs = target["ConnectionString"]
        self.name = target["Name"]
        self.locale = target["Locale"]
        db = parse_cs(self.cs)["database"]
        if db == SOURCE_DB:
            raise SystemExit(f"{self.name} resolves to source database")
        self.cn = connect(self.cs)
        self.cur = self.cn.cursor()

    def close(self) -> None:
        try:
            self.cn.close()
        except Exception:
            pass

    def commit(self) -> None:
        self.cn.commit()

    def rollback(self) -> None:
        self.cn.rollback()

    def upsert_category(self, row: dict, text: dict | None) -> str:
        self.cur.execute("SELECT Id, Name, IsActive, DisplayOrder FROM Categories WHERE Id=%s", (row["Id"],))
        existing = self.cur.fetchone()
        if text is None and existing:
            if same(existing[2], row["IsActive"]) and same(existing[3], row["DisplayOrder"]):
                return "skip"
            self.cur.execute(
                "UPDATE Categories SET IsActive=%s, DisplayOrder=%s, UpdatedAt=GETDATE() WHERE Id=%s",
                (row["IsActive"], row["DisplayOrder"], row["Id"]),
            )
            self.commit()
            return "specs"
        if text is None:
            raise RuntimeError(f"category {row['Id']} missing and not translated")
        slug = unique_slug(self.cur, "Categories", text["Slug"], row["Id"])
        if existing:
            self.cur.execute(
                """
                UPDATE Categories
                SET Name=%s, Slug=%s, Description=%s, IsActive=%s, DisplayOrder=%s, UpdatedAt=GETDATE()
                WHERE Id=%s
                """,
                (text["Name"], slug, text.get("Description"), row["IsActive"], row["DisplayOrder"], row["Id"]),
            )
            self.commit()
            return "translated"
        self.cur.execute("SET IDENTITY_INSERT Categories ON")
        try:
            self.cur.execute(
                """
                INSERT INTO Categories (Id, Name, Slug, Description, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
                VALUES (%s, %s, %s, %s, %s, %s, %s, GETDATE())
                """,
                (row["Id"], text["Name"], slug, text.get("Description"), row["IsActive"], row["DisplayOrder"], row["CreatedAt"]),
            )
            self.cur.execute("SET IDENTITY_INSERT Categories OFF")
            self.commit()
        except Exception:
            self.rollback()
            try:
                self.cur.execute("SET IDENTITY_INSERT Categories OFF")
                self.commit()
            except Exception:
                self.rollback()
            raise
        return "inserted"

    def upsert_subcategory(self, row: dict, text: dict | None) -> str:
        self.cur.execute(
            "SELECT Id, Name, CategoryId, IsActive, DisplayOrder FROM SubCategories WHERE Id=%s",
            (row["Id"],),
        )
        existing = self.cur.fetchone()
        if text is None and existing:
            if same(existing[2], row["CategoryId"]) and same(existing[3], row["IsActive"]) and same(existing[4], row["DisplayOrder"]):
                return "skip"
            self.cur.execute(
                """
                UPDATE SubCategories
                SET CategoryId=%s, IsActive=%s, DisplayOrder=%s, UpdatedAt=GETDATE()
                WHERE Id=%s
                """,
                (row["CategoryId"], row["IsActive"], row["DisplayOrder"], row["Id"]),
            )
            self.commit()
            return "specs"
        if text is None:
            raise RuntimeError(f"subcategory {row['Id']} missing and not translated")
        slug = unique_slug(self.cur, "SubCategories", text["Slug"], row["Id"])
        if existing:
            self.cur.execute(
                """
                UPDATE SubCategories
                SET CategoryId=%s, Name=%s, Slug=%s, Description=%s, IsActive=%s, DisplayOrder=%s, UpdatedAt=GETDATE()
                WHERE Id=%s
                """,
                (
                    row["CategoryId"],
                    text["Name"],
                    slug,
                    text.get("Description"),
                    row["IsActive"],
                    row["DisplayOrder"],
                    row["Id"],
                ),
            )
            self.commit()
            return "translated"
        self.cur.execute("SET IDENTITY_INSERT SubCategories ON")
        try:
            self.cur.execute(
                """
                INSERT INTO SubCategories (Id, CategoryId, Name, Slug, Description, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, GETDATE())
                """,
                (
                    row["Id"],
                    row["CategoryId"],
                    text["Name"],
                    slug,
                    text.get("Description"),
                    row["IsActive"],
                    row["DisplayOrder"],
                    row["CreatedAt"],
                ),
            )
            self.cur.execute("SET IDENTITY_INSERT SubCategories OFF")
            self.commit()
        except Exception:
            self.rollback()
            try:
                self.cur.execute("SET IDENTITY_INSERT SubCategories OFF")
                self.commit()
            except Exception:
                self.rollback()
            raise
        return "inserted"

    def upsert_product(self, row: dict, text: dict | None, specs_changed: bool) -> str:
        self.cur.execute("SELECT Id FROM Products WHERE Id=%s", (row["Id"],))
        existing = self.cur.fetchone()
        if text is None and existing:
            if not specs_changed:
                return "skip"
            assignments = ", ".join(f"{col}=%s" for col in PRODUCT_SPECS)
            values = [row[col] for col in PRODUCT_SPECS] + [row["Id"]]
            self.cur.execute(
                f"UPDATE Products SET {assignments}, UpdatedAt=GETDATE() WHERE Id=%s",
                tuple(values),
            )
            self.commit()
            return "specs"
        if text is None:
            raise RuntimeError(f"product {row['Id']} missing and not translated")
        slug = unique_slug(self.cur, "Products", text["Slug"], row["Id"])
        if existing:
            sets = [
                "Name=%s",
                "Slug=%s",
                "Description=%s",
                "TechnicalDescription=%s",
                "Summary=%s",
                "WarrantyDescription=%s",
                "AfterSalesService=%s",
                "FanDescription=%s",
                "ColorOptions=%s",
                "DeliveryDays=%s",
            ]
            params = [
                text["Name"],
                slug,
                text.get("Description"),
                text.get("TechnicalDescription"),
                text.get("Summary"),
                text.get("WarrantyDescription"),
                text.get("AfterSalesService"),
                text.get("FanDescription"),
                text.get("ColorOptions"),
                text.get("DeliveryDays"),
            ]
            for col in PRODUCT_SPECS:
                sets.append(f"{col}=%s")
                params.append(row[col])
            params.append(row["Id"])
            self.cur.execute(
                f"UPDATE Products SET {', '.join(sets)}, UpdatedAt=GETDATE() WHERE Id=%s",
                tuple(params),
            )
            self.commit()
            return "translated"
        columns = [
            "Id",
            "CategoryId",
            "SubCategoryId",
            "Name",
            "Slug",
            "Description",
            "TechnicalDescription",
            "Summary",
            "Price",
            "Stock",
            "DisplayOrder",
            "IsActive",
            "CreatedAt",
            "ViewCount",
            "InflatedLength",
            "InflatedWidth",
            "InflatedHeight",
            "UserCount",
            "AssemblyTime",
            "RequiredPersonCount",
            "FanDescription",
            "FanWeightKg",
            "PackagedLength",
            "PackagedDepth",
            "PackagedWeightKg",
            "PackagePalletCount",
            "HasCertificate",
            "WarrantyDescription",
            "AfterSalesService",
            "IsDiscounted",
            "IsPopular",
            "IsProjectSpecial",
            "DeliveryDays",
            "DeliveryDaysMin",
            "DeliveryDaysMax",
            "IsFireResistant",
            "MaterialWeight",
            "MaterialWeightGrm2",
            "ColorOptions",
            "InflatedWeightKg",
        ]
        values = {
            **row,
            "Name": text["Name"],
            "Slug": slug,
            "Description": text.get("Description"),
            "TechnicalDescription": text.get("TechnicalDescription"),
            "Summary": text.get("Summary"),
            "WarrantyDescription": text.get("WarrantyDescription"),
            "AfterSalesService": text.get("AfterSalesService"),
            "FanDescription": text.get("FanDescription"),
            "ColorOptions": text.get("ColorOptions"),
            "DeliveryDays": text.get("DeliveryDays"),
        }
        self.cur.execute("SET IDENTITY_INSERT Products ON")
        try:
            placeholders = ", ".join(["%s"] * len(columns))
            self.cur.execute(
                f"INSERT INTO Products ({', '.join(columns)}, UpdatedAt) VALUES ({placeholders}, GETDATE())",
                tuple(values[col] for col in columns),
            )
            self.cur.execute("SET IDENTITY_INSERT Products OFF")
            self.commit()
        except Exception:
            self.rollback()
            try:
                self.cur.execute("SET IDENTITY_INSERT Products OFF")
                self.commit()
            except Exception:
                self.rollback()
            raise
        return "inserted"

    def replace_images(self, product_id: int, images: list[dict]) -> None:
        self.cur.execute("DELETE FROM ProductImages WHERE ProductId=%s", (product_id,))
        for image in images:
            self.cur.execute(
                """
                INSERT INTO ProductImages
                    (ProductId, FileName, OriginalPath, LargePath, ThumbnailPath, IsMainImage, DisplayOrder, CreatedAt)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                """,
                (
                    product_id,
                    image["FileName"],
                    image["OriginalPath"],
                    image["LargePath"],
                    image["ThumbnailPath"],
                    image["IsMainImage"],
                    image["DisplayOrder"],
                    image["CreatedAt"],
                ),
            )
        self.commit()


def image_paths(images: list[dict]) -> list[str]:
    found = []
    seen = set()
    for image in images:
        for key in ("OriginalPath", "LargePath", "ThumbnailPath"):
            rel = str(image.get(key) or "").replace("\\", "/").strip().lstrip("/")
            if rel.lower().startswith("uploads/") and rel not in seen:
                seen.add(rel)
                found.append(rel)
    return found


def download(rel: str) -> bytes | None:
    url = f"{SOURCE_HTTP}/{quote(rel, safe='/')}"
    last = None
    for attempt in range(1, 5):
        try:
            req = Request(url, headers={"User-Agent": UA})
            with urlopen(req, timeout=90) as resp:
                data = resp.read()
            if not data:
                raise RuntimeError("empty body")
            return data
        except HTTPError as exc:
            if exc.code == 404:
                return None
            last = exc
            time.sleep(min(2 ** attempt, 15))
        except (URLError, TimeoutError, OSError) as exc:
            last = exc
            time.sleep(min(2 ** attempt, 15))
    raise RuntimeError(f"download failed for {rel}: {last}")


class FtpUploader:
    def __init__(self, target: dict):
        self.host = target["FtpHost"]
        self.user = target["FtpUser"]
        self.password = target["FtpPass"]
        self.port = int(target.get("FtpPort") or 21)
        self.root = "/" + "/".join(
            p for p in str(target.get("FtpUploadsRoot") or "/httpdocs/wwwroot").replace("\\", "/").split("/") if p
        )
        if "balonpark.com" in self.host.lower():
            raise SystemExit("Refusing to upload to balonpark.com")
        self.ftp: FTP | None = None
        self.dirs: set[str] = set()
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
        self.ftp = ftp
        self.dirs.clear()

    def ensure(self) -> None:
        assert self.ftp is not None
        try:
            self.ftp.voidcmd("NOOP")
        except Exception:
            self.connect()

    def mkdir_p(self, path: str) -> None:
        assert self.ftp is not None
        cur = ""
        for part in [p for p in path.split("/") if p]:
            cur += "/" + part
            if cur in self.dirs:
                continue
            try:
                self.ftp.mkd(cur)
            except error_perm:
                pass
            self.dirs.add(cur)

    def remote_size(self, path: str) -> int | None:
        assert self.ftp is not None
        try:
            return self.ftp.size(path)
        except Exception:
            return None

    def upload(self, rel: str, data: bytes) -> str:
        remote = f"{self.root}/{rel}".replace("//", "/")
        parent = remote.rsplit("/", 1)[0]
        name = remote.rsplit("/", 1)[1]
        last = None
        for attempt in range(1, 4):
            try:
                self.ensure()
                assert self.ftp is not None
                size = self.remote_size(remote)
                if size == len(data):
                    return "skipped"
                self.mkdir_p(parent)
                self.ftp.cwd(parent)
                self.ftp.storbinary(f"STOR {name}", BytesIO(data))
                return "uploaded"
            except (error_temp, error_perm, OSError, EOFError) as exc:
                last = exc
                time.sleep(min(2 ** attempt, 12))
                try:
                    self.connect()
                except Exception:
                    pass
        raise RuntimeError(f"FTP upload failed for {rel}: {last}")

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


_thread_ftp = threading.local()


def ftp_for_thread(target: dict) -> FtpUploader:
    key = (target["Name"], target["FtpHost"])
    current = getattr(_thread_ftp, "uploader", None)
    current_key = getattr(_thread_ftp, "key", None)
    if current is None or current_key != key:
        if current is not None:
            current.close()
        current = FtpUploader(target)
        _thread_ftp.uploader = current
        _thread_ftp.key = key
    return current


def copy_one(target: dict, rel: str) -> tuple[str, str]:
    data = download(rel)
    if data is None:
        return rel, "missing"
    status = ftp_for_thread(target).upload(rel, data)
    return rel, status


def sync_files(target: dict, paths: list[str], workers: int = 4) -> tuple[int, int, int, int]:
    if not paths:
        return 0, 0, 0, 0
    uploaded = skipped = missing = errors = 0
    with ThreadPoolExecutor(max_workers=max(1, workers)) as pool:
        futures = {pool.submit(copy_one, target, rel): rel for rel in paths}
        done = 0
        for fut in as_completed(futures):
            rel = futures[fut]
            done += 1
            try:
                _rel, status = fut.result()
            except Exception as exc:
                errors += 1
                log(f"  FTP ERR {rel}: {exc}")
                continue
            if status == "uploaded":
                uploaded += 1
            elif status == "skipped":
                skipped += 1
            else:
                missing += 1
            if done % 100 == 0 or done == len(paths):
                log(f"  FTP files {done}/{len(paths)} uploaded={uploaded} skipped={skipped} missing={missing} errors={errors}")
    return uploaded, skipped, missing, errors


def product_spec_changed(source: dict, target: dict | None) -> bool:
    if target is None:
        return True
    return any(not same(source[col], target.get(col)) for col in PRODUCT_SPECS)


def main() -> int:
    cfg = json.loads(APPSETTINGS.read_text(encoding="utf-8"))
    source_cs = cfg["ConnectionStrings"]["DefaultConnection"]
    if parse_cs(source_cs)["database"] != SOURCE_DB:
        raise SystemExit("Source database guard failed")
    targets = load_targets()
    api_key = source_api_key(source_cs)
    log(f"API key loaded ({len(api_key)} chars). Source reads only: {SOURCE_DB}")

    categories = fetch_all(source_cs, "SELECT * FROM Categories ORDER BY Id")
    subcategories = fetch_all(source_cs, "SELECT * FROM SubCategories ORDER BY Id")
    products = fetch_all(source_cs, "SELECT * FROM Products ORDER BY Id")
    images = fetch_all(source_cs, "SELECT * FROM ProductImages ORDER BY ProductId, DisplayOrder, Id")
    images_by_product: dict[int, list[dict]] = {}
    for image in images:
        images_by_product.setdefault(image["ProductId"], []).append(image)
    log(
        f"Source catalog: {len(categories)} categories, {len(subcategories)} subcategories, "
        f"{len(products)} products, {len(images)} images"
    )

    cache = load_json(CACHE_PATH, {})
    state = load_json(STATE_PATH, {})
    failures = []

    for target in targets:
        locale = target["Locale"]
        name = target["Name"]
        log(f"\n=== {name} ({locale}) database ===")
        tgt_categories = {r["Id"]: r for r in fetch_all(target["ConnectionString"], "SELECT * FROM Categories")}
        tgt_subs = {r["Id"]: r for r in fetch_all(target["ConnectionString"], "SELECT * FROM SubCategories")}
        tgt_products = {r["Id"]: r for r in fetch_all(target["ConnectionString"], "SELECT * FROM Products")}
        tgt_images = fetch_all(
            target["ConnectionString"],
            "SELECT ProductId, OriginalPath, LargePath, ThumbnailPath FROM ProductImages",
        )
        tgt_paths: dict[int, set[str]] = {}
        for row in tgt_images:
            bucket = tgt_paths.setdefault(row["ProductId"], set())
            for key in ("OriginalPath", "LargePath", "ThumbnailPath"):
                rel = str(row.get(key) or "").replace("\\", "/").strip().lstrip("/")
                if rel:
                    bucket.add(rel)

        writer = TargetWriter(target)
        stats = {"inserted": 0, "translated": 0, "specs": 0, "skip": 0, "images": 0}
        ftp_ids = set(state.get(f"ftp_pending_{locale}", []))
        try:
            for row in categories:
                current = tgt_categories.get(row["Id"])
                blob = "" if current is None else f"{current.get('Name') or ''} {current.get('Description') or ''}"
                translate = needs_translation(locale, row.get("Name") or "", None if current is None else current.get("Name"), blob)
                text = None
                if translate:
                    translated = translate_fields(
                        api_key,
                        locale,
                        "category",
                        row["Id"],
                        {"Name": row.get("Name"), "Description": row.get("Description"), "Slug": row.get("Slug")},
                        cache,
                    )
                    text = apply_text("category", translated, row["Id"])
                action = writer.upsert_category(row, text)
                stats[action] = stats.get(action, 0) + 1
                if text:
                    log(f"  category {row['Id']} {action}: {text['Name']}")

            for row in subcategories:
                current = tgt_subs.get(row["Id"])
                blob = "" if current is None else f"{current.get('Name') or ''} {current.get('Description') or ''}"
                translate = needs_translation(locale, row.get("Name") or "", None if current is None else current.get("Name"), blob)
                text = None
                if translate:
                    translated = translate_fields(
                        api_key,
                        locale,
                        "subcategory",
                        row["Id"],
                        {"Name": row.get("Name"), "Description": row.get("Description"), "Slug": row.get("Slug")},
                        cache,
                    )
                    text = apply_text("subcategory", translated, row["Id"])
                action = writer.upsert_subcategory(row, text)
                stats[action] = stats.get(action, 0) + 1
                if action != "skip":
                    log(f"  subcategory {row['Id']} {action}: {(text or {}).get('Name', row.get('Name'))}")

            image_jobs: list[tuple[int, list[str]]] = []
            to_translate: list[dict] = []
            for row in products:
                current = tgt_products.get(row["Id"])
                blob = ""
                if current is not None:
                    blob = " ".join(str(current.get(field) or "") for field in ("Name", "Description", "Summary"))
                translate = current is None or needs_translation(
                    locale, row.get("Name") or "", current.get("Name"), blob
                )
                if (
                    current is not None
                    and (current.get("Description") or "") == (row.get("Description") or "")
                    and TR_SPECIFIC.search(row.get("Description") or "")
                ):
                    translate = True
                if translate:
                    to_translate.append(row)
            log(f"  translating {len(to_translate)} products")
            translated_products: dict[int, dict] = {}

            def _translate_product(row: dict) -> tuple[int, dict]:
                fields = {field: row.get(field) for field in ("Slug", *TEXT_FIELDS)}
                fields["Name"] = row.get("Name")
                translated = translate_fields(api_key, locale, "product", row["Id"], fields, cache)
                return row["Id"], apply_text("product", translated, row["Id"])

            if to_translate:
                with ThreadPoolExecutor(max_workers=4) as pool:
                    futures = {pool.submit(_translate_product, row): row["Id"] for row in to_translate}
                    done = 0
                    for fut in as_completed(futures):
                        product_id = futures[fut]
                        try:
                            pid, text = fut.result()
                        except Exception as exc:
                            raise RuntimeError(f"product {product_id} translation: {exc}") from exc
                        translated_products[pid] = text
                        done += 1
                        if done % 10 == 0 or done == len(to_translate):
                            log(f"  translated {done}/{len(to_translate)}")

            for index, row in enumerate(products, start=1):
                current = tgt_products.get(row["Id"])
                text = translated_products.get(row["Id"])
                specs_changed = product_spec_changed(row, current)
                action = writer.upsert_product(row, text, specs_changed)
                stats[action] = stats.get(action, 0) + 1
                src_paths = image_paths(images_by_product.get(row["Id"], []))
                if set(src_paths) != tgt_paths.get(row["Id"], set()):
                    writer.replace_images(row["Id"], images_by_product.get(row["Id"], []))
                    stats["images"] += 1
                    ftp_ids.add(row["Id"])
                    image_jobs.append((row["Id"], src_paths))
                elif row["Id"] in ftp_ids:
                    image_jobs.append((row["Id"], src_paths))
                if action != "skip" and (index % 25 == 0 or action in {"inserted", "translated"}):
                    log(f"  product {row['Id']} {action}: {(text or {}).get('Name', '')[:70]}")
                if index % 40 == 0:
                    log(f"  ... {index}/{len(products)} products")

            state[f"ftp_pending_{locale}"] = sorted(ftp_ids)
            save_json(STATE_PATH, state)
            log(f"  db stats: {stats}; ftp products={len(image_jobs)}")

            all_paths: list[str] = []
            seen_paths: set[str] = set()
            for _product_id, paths in image_jobs:
                for rel in paths:
                    if rel not in seen_paths:
                        seen_paths.add(rel)
                        all_paths.append(rel)
            log(f"  FTP {name}: {len(all_paths)} files from {len(image_jobs)} products")
            uploaded, skipped, missing, errors = sync_files(target, all_paths)
            if errors == 0:
                ftp_ids.clear()
            state[f"ftp_pending_{locale}"] = sorted(ftp_ids)
            save_json(STATE_PATH, state)
            log(f"  FTP done {name}: uploaded={uploaded} skipped={skipped} missing={missing} errors={errors}")
            if errors:
                failures.append(f"{name} ftp errors={errors}")
        except Exception as exc:
            writer.rollback()
            failures.append(f"{name}: {exc}")
            log(f"ERR {name}: {exc}")
        finally:
            writer.close()

    log("\n=== verify ===")
    src_ids = {row["Id"] for row in products}
    for target in targets:
        ids = {row["Id"] for row in fetch_all(target["ConnectionString"], "SELECT Id FROM Products")}
        missing = sorted(src_ids - ids)
        log(f"{target['Name']} missing products: {len(missing)} {missing[:20]}")
    if failures:
        log("FAILURES:")
        for item in failures:
            log(f"  {item}")
        return 1
    log("SYNC OK")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        log("interrupted")
        raise SystemExit(130)
