let mysql = require('mysql');
let db_info = {
    host : 'diceanddeal.c9as6u8gu1w3.ap-northeast-2.rds.amazonaws.com',//db주소
    port : '3306',
    user : 'admin',//db유저
    password : 'kKGf7NIIiLSIEq96s31S',//db암호
    database : 'dad'//db이름
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
