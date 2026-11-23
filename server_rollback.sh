#!/bin/bash
echo "=== MOBIL /opt/AppointMan: reset ==="
cd /opt/AppointMan && git fetch origin 2>&1 | tail -2
cd /opt/AppointMan && git reset --hard origin/main 2>&1 | tail -2
echo "=== MOBIL down -v (fresh DB) ==="
cd /opt/AppointMan && docker compose down -v 2>&1 | tail -5
echo "=== BACKOFFICE /opt/appointman-backoffice: reset ==="
cd /opt/appointman-backoffice && git fetch origin 2>&1 | tail -2
cd /opt/appointman-backoffice && git reset --hard origin/main 2>&1 | tail -2
echo "=== BACKOFFICE down -v (fresh DB) ==="
cd /opt/appointman-backoffice && docker compose down -v 2>&1 | tail -5
echo "=== launch up (bg) MOBIL ==="
cd /opt/AppointMan && nohup docker compose up -d --build > /tmp/mobil_up.log 2>&1 &
echo "=== launch up (bg) BACKOFFICE ==="
cd /opt/appointman-backoffice && nohup docker compose up -d --build > /tmp/backoffice_up.log 2>&1 &
echo "ROLLBACK_LAUNCHED"
