#!/bin/bash
echo "Running init.sh"
echo "Running repair"
mongod --repair;
echo "Setting database directory permission"
chmod 775 ./data/db ./data/configdb;
echo "Removing journal logs - recovery issue"
rm ./data/db/journal/*
echo "Running mongo with auth"
mongod --bind_ip_all --port 27017 --dbpath /data/db;
echo "Initializing users"
mongo admin /docker-entrypoint-initdb.d/mongo-users-init.js;
echo "Stopping mongodb service"
mongod --shutdown;
echo "Restarting"
numactl mongod --bind_ip_all --port 27017 --dbpath /data/db --auth & sleep 15;
echo "Complete"