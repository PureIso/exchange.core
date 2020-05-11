//System: exchange
print(db.system.users.find())
//db.getUser('<userName>')
/**
 * Create super user taht will be checked against the Admin database
 * All-Database Roles
 * These roles lie on the admin database and provide privileges which apply to all databases.
 * readAnyDatabase – The same as ‘read’ role but applies to all databases
 * readWriteAnyDatabase – The same as ‘readWrite’ role but applies to all databases
 * userAdminAnyDatabase – The same as ‘userAdmin’ role but applies to all databases
 * dbAdminAnyDatabase – The same as ‘dbAdmin’ role but applies to all databases
 */
//db.createUser({user: "allDatabaseAdmin",pwd: "allDatabaseAdmin",roles: [{role:"dbAdminAnyDatabase",db:"admin"}, {role:"dbOwner",db:"celery"}, {role:"dbOwner",db:"kombu_default"}]})
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
/**
 * Database Administration Roles
 * The database administration roles we can use are the following:
 * dbAdmin – Grant privileges to perform administrative tasks
 * userAdmin – Allows you to create and modify users and roles on the current database
 * dbOwner – This role combines the following:
 *           readWrite
 *           dbAdmin
 *           userAdmin
 * NB: Cannot create other databases
 */
//db.createUser({user:"databaseAdmin", pwd:"databaseAdmin", roles:[{role:"dbOwner",db:"celery"}, {role:"dbOwner",db:"kombu_default"}]})
/**
 * Cluster Administration Roles
 * The admin database includes the following roles for administering the whole system rather than just a single database.
 * These roles include but are not limited to replica set and sharded cluster administrative functions.
 */

db.createUser({user:"celery", pwd:"celery", roles:[{role:"dbOwner",db:"celery"}, {role:"dbOwner",db:"kombu_default"}]})
/**
 * Database User Roles
 * The roles available at the database level are:
 * read – Read data on all non-system collections
 * readWrite – Include all ‘read’ role privileges and the ability to write data on all non-system collections
 **/

//db.createUser({user:"test", pwd:"test", roles:[{role:"read",db:"celery"}]})
//print(db.system.users.find())
//db.logout()