nohup bash -c 'cd /opt/appointman-backoffice && git fetch origin && git reset --hard origin/main && docker compose down -v && docker compose up -d --build' > /tmp/backoffice_up.log 2>&1 &
nohup bash -c 'cd /opt/AppointMan && docker compose up -d --build' > /tmp/mobil_up.log 2>&1 &
echo "LAUNCHED_BOTH_BG"
