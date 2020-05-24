#!/bin/bash
echo "Running init.sh"
echo "Setting database directory permission"
chmod 775 ./data/db ./data/configdb
echo "Running mongo with auth"
mongod --bind_ip_all --port 27017 --dbpath /data/db &
sleep 5
echo "Initializing users"
mongo admin /docker-entrypoint-initdb.d/mongo-users-init.js
sleep 5
echo "Stopping mongodb service"
mongod --shutdown
sleep 5
echo "Restarting"
numactl mongod --bind_ip_all --port 27017 --dbpath /data/db --auth
echo "Complete"
