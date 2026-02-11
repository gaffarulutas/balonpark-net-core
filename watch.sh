#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="${SCRIPT_DIR}/BalonPark"
PORTS=(5152 7120 5000 5001 18764 44300)

echo "=== 1. Çalışan portları durduruyoruz ==="
for port in "${PORTS[@]}"; do
  if lsof -ti:"$port" >/dev/null 2>&1; then
    echo "Port $port kapatılıyor..."
    lsof -ti:"$port" | xargs kill -9 2>/dev/null || true
  fi
done
echo "Portlar temizlendi."
echo ""

echo "=== 2. dotnet build ==="
cd "$APP_DIR"
dotnet build
echo ""

echo "=== 3. Tailwind CSS build ==="
npm run build:css
echo ""

echo "=== 4. dotnet watch başlatılıyor ==="
dotnet watch
