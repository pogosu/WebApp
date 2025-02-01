using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

public class DBManager {
    private SqliteConnection? connection = null;

    private string HashPassword(string password) {
        using (var algorithm = SHA256.Create()) {
            var bytes_hash = algorithm.ComputeHash(Encoding.Unicode.GetBytes(password));
            return Encoding.Unicode.GetString(bytes_hash);
        }
    }

    public bool ConnectToDB(string path) {
        Console.WriteLine("Connection to db...");

        try
        {
            connection = new SqliteConnection("Data Source =" + path);
            connection.Open();
            
            if (connection.State != ConnectionState.Open) {
                Console.WriteLine("Failed!");
                return false;
            }
        }
        catch (Exception exp) {
            Console.WriteLine(exp.Message);
            return false;
        }
        Console.WriteLine("Done!");
        return true;
    }

    public void Disconnect() {
        if (connection == null)
            return;

        if (connection.State != ConnectionState.Open)
            return;

        connection.Close();
        Console.WriteLine("Disconnect from db");
    }

    public bool AddUser(string login, string password) {
        if (connection == null || connection.State != ConnectionState.Open)
            return false;

        string REQUEST = "INSERT INTO users (Login, Password) VALUES (@login, @password)";
        var command = new SqliteCommand(REQUEST, connection);
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", HashPassword(password));

        int result = 0;
        try
        {
            result = command.ExecuteNonQuery();
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
            return false;
        }

        return result == 1;
    }

    public bool CheckUser(string login, string password) {
        if (connection == null || connection.State != ConnectionState.Open)
            return false;

        string REQUEST = "SELECT Login, Password FROM users WHERE Login=@login AND Password=@password";
        var command = new SqliteCommand(REQUEST, connection);
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", HashPassword(password));

        try
        {
            var reader = command.ExecuteReader();

            if (reader.HasRows)
                return true;
            else
                return false;
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
            return false;
        }
    }

    public bool CheckLogin(string login)
    {
        if (connection == null || connection.State != ConnectionState.Open)
            return false;

        string REQUEST = "SELECT COUNT(*) FROM users WHERE Login=@login";
        var command = new SqliteCommand(REQUEST, connection);
        command.Parameters.AddWithValue("@login", login);

        try
        {
            var result = command.ExecuteScalar();
            if (result != null && Convert.ToInt32(result) > 0)
                return true; // Логин уже существует
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
        }

        return false;
    }

    public bool ChangePassword(string login, string oldPassword, string newPassword)
    {
        if (connection == null || connection.State != ConnectionState.Open)
            return false;

        if (!CheckUser(login, oldPassword))
            return false;

        string REQUEST = "UPDATE users SET Password=@newPassword WHERE Login=@login";
        var command = new SqliteCommand(REQUEST, connection);
        command.Parameters.AddWithValue("@newPassword", HashPassword(newPassword));
        command.Parameters.AddWithValue("@login", login);

        int result = 0;
        try
        {
            result = command.ExecuteNonQuery();
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
            return false;
        }

        return result == 1;
    }
}