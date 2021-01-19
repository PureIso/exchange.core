var db = connect('127.0.0.1:27017/admin');
if(db.getUser("celery") == null || db.getUser("machinelearning") == null ||db.getUser("test") == null ){
    db.createUser({user: "allDatabaseAdmin",pwd: "allDatabaseAdmin",roles: [{role:"dbAdminAnyDatabase",db:"admin"}]});
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"celery"}]});
    db.createUser({user:"celery", pwd:"celery", roles:[{role:"dbOwner",db:"celery"},{role:"clusterMonitor",db:"admin"}]});
    db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"celery"}]});
    db.createUser({user:"graylog", pwd:"graylog", roles:[{role:"dbOwner",db:"graylog"}]});
    db.createUser({user:"machinelearning", pwd:"machinelearning", roles:[{role:"dbOwner",db:"machinelearning"}]});
};
var db = connect('127.0.0.1:27017/celery');
if(db.getUser("test") == null){
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"celery"}]});
    db.createUser({user:"celery", pwd:"celery", roles:[{role:"dbOwner",db:"celery"},{role:"clusterMonitor",db:"admin"}]});
    db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"celery"}]});
};
var db = connect('127.0.0.1:27017/graylog');
if(db.getUser("graylog") == null){
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"graylog"}]});
    db.createUser({user:"graylog", pwd:"graylog", roles:[{role:"dbOwner",db:"graylog"},{role:"clusterMonitor",db:"admin"}]});
};
var db = connect('127.0.0.1:27017/machinelearning');
if(db.getUser("machinelearning") == null){
    db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"admin"},{role:"dbOwner",db:"machinelearning"}]});
    db.createUser({user:"machinelearning", pwd:"machinelearning", roles:[{role:"dbOwner",db:"machinelearning"},{role:"clusterMonitor",db:"admin"}]});
};