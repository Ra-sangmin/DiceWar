let mysql = require('mysql');
const { db } = require('./dbConfig');

let db_info = {
    host : db.host,//db주소
    port : db.port,
    user : db.user,//db유저
    password : db.password,//db암호
    database : db.database//db이름
}

module.exports = {

    init: function () {
        return mysql.createConnection(db_info);
    },
    connect: function (conn) {
        conn.connect(function(err) {
            if(err) console.error('mysql connection error : ' + err);
            else console.log('mysql is connected successfully!');
        });
    }
   
}
