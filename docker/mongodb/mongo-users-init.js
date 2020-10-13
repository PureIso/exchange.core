var db = connect('127.0.0.1:27017/admin');
if(db.getUser("celery") == null){
    db.createUser({user: "allDatabaseAdmin",pwd: "allDatabaseAdmin",roles: [{role:"dbAdminAnyDatabase",db:"admin"}]});
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"celery"}]});
    db.createUser({user:"celery", pwd:"celery", roles:[{role:"dbOwner",db:"celery"},{role:"clusterMonitor",db:"admin"}]});
    db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"celery"}]});
};
var db = connect('127.0.0.1:27017/celery');
if(db.getUser("test") == null){
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"celery"}]});
    db.createUser({user:"celery", pwd:"celery", roles:[{role:"dbOwner",db:"celery"},{role:"clusterMonitor",db:"admin"}]});
    db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"celery"}]});
};
var db = connect('127.0.0.1:27017/graylog');
if(db.getUser("graylog") == null){
    db.createUser({user: "allDatabaseAdmin",pwd: "allDatabaseAdmin",roles: [{role:"dbAdminAnyDatabase",db:"admin"}]});
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"graylog"}]});
    db.createUser({user:"graylog", pwd:"graylog", roles:[{role:"dbOwner",db:"graylog"},{role:"clusterMonitor",db:"admin"}]});
    db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"graylog"}]});
};