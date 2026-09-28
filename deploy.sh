#!/usr/bin/env bash
# balonpark.com Release deploy (Windows shared hosting / Plesk)
#
# Flow:
#   1) CSS build
#   2) win-x86 self-contained publish + InProcess web.config
#   3) FTP: clean httpdocs (EXCEPT wwwroot/uploads, with retry) then upload
#   4) Live HTTP health check (with retry)
#
# Credentials come from .deploy.env (gitignored) or environment variables.
#   cp .deploy.env.example .deploy.env
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="${SCRIPT_DIR}/BalonPark"
PUBLISH_DIR="${SCRIPT_DIR}/.publish/balonpark-tr"
FTP_SCRIPT="${SCRIPT_DIR}/scripts/ftp_deploy.py"
DEPLOY_ENV="${SCRIPT_DIR}/.deploy.env"

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
CYAN=$'\033[0;36m'
YELLOW=$'\033[0;33m'
NC=$'\033[0m'

log()  { printf '%s%s%s\n' "${CYAN}" "$*" "${NC}"; }
ok()   { printf '%s%s%s\n' "${GREEN}" "$*" "${NC}"; }
warn() { printf '%s%s%s\n' "${YELLOW}" "$*" "${NC}"; }
err()  { printf '%s%s%s\n' "${RED}" "$*" "${NC}" >&2; }

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || { err "Required command is missing: $1"; exit 1; }
}

# Load secrets without printing them
if [[ -f "${DEPLOY_ENV}" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "${DEPLOY_ENV}"
  set +a
elif [[ -f "${SCRIPT_DIR}/.deploy.env.example" ]]; then
  warn ".deploy.env is missing. Example: cp .deploy.env.example .deploy.env"
fi

FTP_HOST="${FTP_HOST:-balonpark.com}"
FTP_USER="${FTP_USER:-balonpar}"
FTP_PORT="${FTP_PORT:-21}"
SITE_URL="${SITE_URL:-https://balonpark.com}"

if [[ -z "${FTP_PASS:-}" ]]; then
  err "FTP_PASS is not set. Create .deploy.env or run FTP_PASS=... ./deploy.sh"
  exit 1
fi

require_cmd dotnet
require_cmd npm
require_cmd python3
require_cmd curl

if [[ ! -f "${FTP_SCRIPT}" ]]; then
  err "FTP script not found: ${FTP_SCRIPT}"
  exit 1
fi

echo ""
log "=============================================="
log "  balonpark.com deploy"
log "  host: ${FTP_HOST}:${FTP_PORT}"
log "  user: ${FTP_USER}"
log "  site: ${SITE_URL}"
log "  preserve: /httpdocs/wwwroot/uploads"
log "=============================================="
echo ""

# --- 1) CSS ---
log "[1/4] Tailwind CSS build..."
cd "${APP_DIR}"
if [[ ! -f package.json ]]; then
  err "package.json missing in BalonPark/ (needed for npm run build:css)"
  exit 1
fi
if [[ ! -d node_modules ]]; then
  warn "node_modules is missing, running npm install"
  npm install
fi
npm run build:css
ok "CSS is ready."
echo ""

# --- 2) Publish ---
log "[2/4] dotnet publish (Release, win-x86, self-contained)..."
rm -rf "${PUBLISH_DIR}"
mkdir -p "${PUBLISH_DIR}"
dotnet publish "${APP_DIR}/BalonPark.csproj" \
  -c Release \
  -r win-x86 \
  --self-contained true \
  -o "${PUBLISH_DIR}" \
  --nologo

if [[ ! -f "${PUBLISH_DIR}/BalonPark.exe" ]]; then
  err "Publish failed: BalonPark.exe is missing"
  exit 1
fi

# Shared hosting: InProcess + x86 (OutOfProcess burada policy ile engelleniyor)
# ASCII, no BOM
cat > "${PUBLISH_DIR}/web.config" << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\BalonPark.exe" arguments="" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors existingResponse="PassThrough" />
    </system.webServer>
  </location>
</configuration>
EOF

mkdir -p "${PUBLISH_DIR}/logs"
: > "${PUBLISH_DIR}/logs/.gitkeep"
rm -f "${PUBLISH_DIR}/"*.pdb "${PUBLISH_DIR}/appsettings.Development.json" 2>/dev/null || true
rm -rf "${PUBLISH_DIR}/wwwroot/uploads"

FILE_COUNT="$(find "${PUBLISH_DIR}" -type f | wc -l | tr -d ' ')"
SIZE_H="$(du -sh "${PUBLISH_DIR}" | awk '{print $1}')"
ok "Publish is ready: ${FILE_COUNT} files, ${SIZE_H}"
echo ""

# --- 3) FTP wipe + upload (password via env only — not argv / ps) ---
log "[3/4] FTP: cleanup (uploads kept) and upload..."
warn "Shared hosting: the process cannot be stopped, so locked DLLs are retried."
echo ""

export FTP_HOST FTP_USER FTP_PASS FTP_PORT
python3 "${FTP_SCRIPT}" \
  --host "${FTP_HOST}" \
  --user "${FTP_USER}" \
  --port "${FTP_PORT}" \
  --local "${PUBLISH_DIR}"

echo ""

# --- 4) Health check with retries (ANCM recycle) ---
log "[4/4] Live check (${SITE_URL})..."
HTTP_CODE="000"
for attempt in 1 2 3 4 5 6; do
  HTTP_CODE="$(curl -s -o /dev/null -w '%{http_code}' --max-time 25 "${SITE_URL}/" || true)"
  if [[ "${HTTP_CODE}" == "200" ]]; then
    ok "${SITE_URL} → HTTP ${HTTP_CODE}"
    break
  fi
  warn "deneme ${attempt}/6 → HTTP ${HTTP_CODE:-???}, 5s bekleniyor..."
  sleep 5
done

if [[ "${HTTP_CODE}" != "200" ]]; then
  err "Health check failed (HTTP ${HTTP_CODE}). Check httpdocs/logs/stdout_* and the Plesk ANCM settings."
  exit 3
fi

echo ""
ok "Deploy bitti."
ok "Site:  ${SITE_URL}"
ok "Admin: ${SITE_URL}/Admin/Login"
echo ""
