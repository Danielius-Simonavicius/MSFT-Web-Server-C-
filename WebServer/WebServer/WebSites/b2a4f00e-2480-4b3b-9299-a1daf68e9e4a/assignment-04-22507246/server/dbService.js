const mysql = require('mysql');
const dotenv = require('dotenv');
let instance = null;
dotenv.config();


const connection = mysql.createConnection({
    host: process.env.HOST,
    user: process.env.USERNAME,
    password: process.env.PASSWORD,
    database: process.env.DATABASE,
    port: process.env.DB_PORT
})

connection.connect((err) => {
    if (err) {
        console.log(err.message);
    }
    //console.log('db' + connection.state)
});


class DbService {
    static getDbServiceInstance() {
        return instance ? instance : new DbService();
    }

    async getAllData() {
        try {
            const response = await new Promise((resolve, reject) => {
                const query = "SELECT * FROM names;";

                connection.query(query, (err, results) => {
                    if (err) reject(new Error(err.message));
                    resolve(results);
                })
            })
            console.log(response);
            return response;
        } catch (error) {
            console.log(error);
        }
    }

    async insertNewName(name) {
        try {
            const date_added = new Date();
            const insertId = await new Promise((resolve, reject) => {
                const query = "INSERT INTO names (name,date_added) VALUES (?,?);";

                connection.query(query, [name, date_added], (err, result) => {
                    if (err) {
                        reject(new Error(err.message));
                    } else {
                        resolve(result.insertId);
                    }
                });
            });
            console.log(insertId);
            return {
                id: insertId,
                name: name,
                dateAdded: date_added
            };
        }
        catch (error) {
            console.log(error);
        }
    }

    async deleteRowById(id) {
        try {
            id = parseInt(id, 10);
            const response = await new Promise((resolve, reject) => {
                const query = "DELETE FROM names WHERE id = ?;";

                connection.query(query, [id], (err, result) => {
                    if (err) {
                        reject(new Error(err.message));
                    } else {
                        resolve(result.affectedRows);
                    }
                });
            });
            return response === 1 ? true : false;
        } catch (erorr) {
            console.log(erorr);
            return false;
        }
    }

    async updateNameById(id, name) {
        try {
            id = parseInt(id);
            const response = await new Promise((resolve, reject) => {
                const query = "UPDATE names SET name = ? WHERE id = ?";

                connection.query(query, [name, id], (err, result) => {
                    if (err) {
                        reject(new Error(err.message));
                    } else {
                        resolve(result.affectedRows);
                    }
                });
            });
            return response === 1 ? true : false;
        } catch (erorr) {
            console.log(erorr);
            return false;
        }
    }
}


module.exports = DbService;