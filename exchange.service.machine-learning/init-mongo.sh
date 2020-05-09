#!/bin/bash
mongo
# admin to create users
use admin
db.createUser({user:"siteAdmin", pwd:"password",roles:[{role:"userAdminAnyDatabase",db:"admin"}]},{unique:true})
# all databases but not admin
use admin
db.createUser({user:"databaseAdmin", pwd:"password", roles:["readWriteAnyDatabase"]},{unique:true})
# test user
use test
db.createUser({ user:"test", pwd:"test", roles:["readWrite"]},{unique:true})