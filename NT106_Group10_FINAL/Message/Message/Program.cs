using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
namespace Message
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormLogin());
            string connectionString = "Data Source=database.db;Version=3;";
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                // Xoá bảng nếu tồn tại (Tuỳ chọn, chỉ thực hiện nếu không có dữ liệu quan trọng trong bảng)
                string dropTableQuery = "DROP TABLE IF EXISTS Login;";
                using (SQLiteCommand dropCmd = new SQLiteCommand(dropTableQuery, con))
                {
                    dropCmd.ExecuteNonQuery();
                }

                // Tạo lại bảng với cột confirmpass
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Login (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                email TEXT UNIQUE NOT NULL,
                password TEXT NOT NULL,
                confirmpass TEXT NOT NULL,
                image BLOB
                );";

                using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, con))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Database và bảng Login đã được tạo thành công!");
                }
            }
        }
    }
}
