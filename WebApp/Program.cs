using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


DBManager db = new DBManager();
HistoryManager historyManager = new HistoryManager();
GronsfeldCipher gronsfeldCipher = new GronsfeldCipher();
GRWebAdapter gr = new GRWebAdapter(gronsfeldCipher, historyManager);
TextManager textManager = new TextManager();
TextManagerAdapter tm = new TextManagerAdapter(textManager, historyManager);


app.MapGet("/", () => "Welcome to the Gronsfeld Cipher service!");

app.MapPost("/encrypt", [Authorize] (string text, string key, HttpContext context) => gr.Encrypt(text, key, context));
app.MapPost("/decrypt", [Authorize] (string text, string key, HttpContext context) => gr.Decrypt(text, key, context));
app.MapPost("/add-text", [Authorize] (string text, HttpContext context) => tm.AddText(context, text));
app.MapPatch("/update-text", [Authorize] (int textIndex, string newText, HttpContext context) => tm.UpdateText(context, textIndex, newText));
app.MapDelete("/delete-text", [Authorize] (int textIndex, HttpContext context) => tm.DeleteText(context, textIndex));
app.MapGet("/get-text", [Authorize] (int textIndex, HttpContext context) => tm.ViewText(context, textIndex));
app.MapGet("/get-all-texts", [Authorize] (HttpContext context) => tm.ViewAllTexts(context));

app.MapGet("/history", [Authorize] (HttpContext context) => {
    string? login = context.User.Identity?.Name;
    if (login == null)
        return Results.Unauthorized();

    var history = historyManager.GetRequestHistory(login);
    if (history.Any())
    {
        string result = string.Join("\n", history);
        return Results.Text(result);
    }
    else
    {
        return Results.NoContent();
    }
});

app.MapDelete("/delete", [Authorize] (HttpContext context) => {
    string? login = context.User.Identity?.Name;
    if (string.IsNullOrEmpty(login))
    {
        return Results.Unauthorized();
    }

    bool result = historyManager.DeleteRequestHistory(login);
    if (result)
    {
        return Results.Ok("История успешно удалена.");
    }
    else
    {
        return Results.Problem("Ошибка удаления истории. История не найдена.");
    }
});

app.MapPost("/signup", (string login, string password) => {
    if (db.CheckLogin(login))
        return Results.Problem("Логин уже существует", statusCode: 409);

    if (db.AddUser(login, password))
        return Results.Ok("Пользователь " + login + " успешно зарегистрирован");
    else
        return Results.Problem("Ошибка регистрации пользователя " + login);
});


app.MapPost("/login", async (string login, string password, HttpContext context) => {
    if (!db.CheckUser(login, password)) return Results.Unauthorized();

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, login) };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
    return Results.Ok();
});

app.MapPatch("/change-password", [Authorize] (string oldPassword, string newPassword, HttpContext context) => {
    string username = context.User.Identity?.Name ?? "";
    if (string.IsNullOrEmpty(username) || !db.ChangePassword(username, oldPassword, newPassword)) return Results.Unauthorized();

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

    context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

    historyManager.AddRequestToHistory(username, "Изменение пароля");
    return Results.Ok("Пароль успешно изменен");
});

const string DB_PATH = "/home/ilya/Рабочий стол/App (1)/users.db";
if (!db.ConnectToDB(DB_PATH)) {
    Console.WriteLine("Failed to connect to db " + DB_PATH);
    Console.WriteLine("ShutDown!");
    return;
}

app.Run();
db.Disconnect();


public class GRWebAdapter
{
    private readonly GronsfeldCipher _gronsfeldCipher;
    private readonly HistoryManager _historyManager;

    public GRWebAdapter(GronsfeldCipher gronsfeldCipher, HistoryManager historyManager)
    {
        _gronsfeldCipher = gronsfeldCipher;
        _historyManager = historyManager;
    }

    public IResult Encrypt(string text, string key, HttpContext context)
    {
        string? username = context.User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Results.Unauthorized();
        }

        string encryptedText = _gronsfeldCipher.Encrypt(text, key);
        _historyManager.AddRequestToHistory(username, "Шифрование текста");

        return Results.Text(encryptedText);
    }

    public IResult Decrypt(string text, string key, HttpContext context)
    {
        string? username = context.User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Results.Unauthorized();
        }

        string decryptedText = _gronsfeldCipher.Decrypt(text, key);
        _historyManager.AddRequestToHistory(username, "Дешифрование текста");

        return Results.Text(decryptedText);
    }
}
public class GronsfeldCipher
{
    private static readonly string RussianAlphabetLower = "абвгдеёжзийклмнопрстуфхцчшщьъыэюя";
    private static readonly string EnglishAlphabetLower = "abcdefghijklmnopqrstuvwxyz";

    // Метод шифрования
    public string Encrypt(string text, string key)
    {
        string result = "";
        int keyIndex = 0;

        foreach (char c in text)
        {
            if (EnglishAlphabetLower.Contains(char.ToLower(c)))
            {
                bool isUpper = char.IsUpper(c);
                char letter = char.ToLower(c);
                int shift = key[keyIndex % key.Length] - '0';
                char encryptedLetter = (char)((((letter - 'a') + shift) % 26 + 26) % 26 + 'a');

                result += isUpper ? char.ToUpper(encryptedLetter) : encryptedLetter;
                keyIndex++;
            }
            else if (RussianAlphabetLower.Contains(char.ToLower(c)))
            {
                bool isUpper = char.IsUpper(c);
                char letter = char.ToLower(c);
                int shift = key[keyIndex % key.Length] - '0';
                char encryptedLetter = (char)((((letter - 'а') + shift) % 33 + 33) % 33 + 'а');

                result += isUpper ? char.ToUpper(encryptedLetter) : encryptedLetter;
                keyIndex++;
            }
            else
            {
                result += c;
            }
        }

        return result;
    }

    // Метод дешифрования
    public string Decrypt(string text, string key)
    {
        string result = "";
        int keyIndex = 0;

        foreach (char c in text)
        {
            if (EnglishAlphabetLower.Contains(char.ToLower(c)))
            {
                bool isUpper = char.IsUpper(c);
                char letter = char.ToLower(c);
                int shift = key[keyIndex % key.Length] - '0';
                char decryptedLetter = (char)((((letter - 'a') - shift + 26) % 26 + 26) % 26 + 'a');

                result += isUpper ? char.ToUpper(decryptedLetter) : decryptedLetter;
                keyIndex++;
            }
            else if (RussianAlphabetLower.Contains(char.ToLower(c)))
            {
                bool isUpper = char.IsUpper(c);
                char letter = char.ToLower(c);
                int shift = key[keyIndex % key.Length] - '0';
                char decryptedLetter = (char)((((letter - 'а') - shift + 33) % 33 + 33) % 33 + 'а');

                result += isUpper ? char.ToUpper(decryptedLetter) : decryptedLetter;
                keyIndex++;
            }
            else
            {
                result += c;
            }
        }

        return result;
    }
}


public class TextManagerAdapter
{
    private readonly TextManager _textManager;
    private readonly HistoryManager _historyManager;

    public TextManagerAdapter(TextManager textManager, HistoryManager historyManager)
    {
        _textManager = textManager;
        _historyManager = historyManager;
    }

    private static string? GetUsername(HttpContext context)
    {
        return context.User.Identity?.Name;
    }

    public IResult AddText(HttpContext context, string text)
    {
        string? username = GetUsername(context);
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        _textManager.AddText(username, text);
        _historyManager.AddRequestToHistory(username, "Добавление текста");

        return Results.Ok("Текст успешно добавлен");
    }

    public IResult UpdateText(HttpContext context, int textIndex, string newText)
    {
        string? username = GetUsername(context);
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        bool success = _textManager.UpdateText(username, textIndex, newText);
        if (!success)
            return Results.Problem("Текст не найден", statusCode: 404);

        _historyManager.AddRequestToHistory(username, $"Изменение текста номер {textIndex}");
        return Results.Ok($"Текст номер {textIndex} изменен");
    }

    public IResult DeleteText(HttpContext context, int textIndex)
    {
        string? username = GetUsername(context);
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        bool success = _textManager.DeleteText(username, textIndex);
        if (!success)
            return Results.Problem("Текст не найден", statusCode: 404);

        _historyManager.AddRequestToHistory(username, $"Удаление текста номер {textIndex}");
        return Results.Ok($"Текст номер {textIndex} удален");
    }

    public IResult ViewText(HttpContext context, int textIndex)
    {
        string? username = GetUsername(context);
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        string? text = _textManager.ViewText(username, textIndex);
        if (text == null)
            return Results.Problem("Текст не найден", statusCode: 404);

        _historyManager.AddRequestToHistory(username, $"Просмотр текста номер {textIndex}");
        return Results.Ok(text);
    }

    public IResult ViewAllTexts(HttpContext context)
    {
        string? username = GetUsername(context);
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        var texts = _textManager.ViewAllTexts(username);
        _historyManager.AddRequestToHistory(username, "Просмотр всех текстов");

        return Results.Json(texts);
    }
}



public class TextManager
{
    private readonly Dictionary<string, List<string>> _userTexts = new();

    public void AddText(string username, string text)
    {
        if (!_userTexts.ContainsKey(username))
        {
            _userTexts[username] = new List<string>();
        }
        _userTexts[username].Add(text);
    }

    public bool UpdateText(string username, int textIndex, string newText)
    {
        int index = textIndex - 1;
        if (_userTexts.ContainsKey(username) && index >= 0 && index < _userTexts[username].Count)
        {
            _userTexts[username][index] = newText;
            return true;
        }
        return false;
    }

    public bool DeleteText(string username, int textIndex)
    {
        int index = textIndex - 1;
        if (_userTexts.ContainsKey(username) && index >= 0 && index < _userTexts[username].Count)
        {
            _userTexts[username].RemoveAt(index);
            return true;
        }
        return false;
    }

    public string? ViewText(string username, int textIndex)
    {
        int index = textIndex - 1;
        if (_userTexts.ContainsKey(username) && index >= 0 && index < _userTexts[username].Count)
        {
            return _userTexts[username][index];
        }
        return null;
    }

    public List<string> ViewAllTexts(string username)
    {
        return _userTexts.ContainsKey(username) ? _userTexts[username] : new List<string>();
    }
}





public class HistoryManager
{
    private readonly Dictionary<string, List<string>> _userRequestHistory = new();

    public void AddRequestToHistory(string username, string request)
    {
        if (!_userRequestHistory.ContainsKey(username))
        {
            _userRequestHistory[username] = new List<string>();
        }

        _userRequestHistory[username].Add($"{DateTime.UtcNow}: {request}");
    }

    public List<string> GetRequestHistory(string username)
    {
        if (_userRequestHistory.ContainsKey(username))
        {
            return _userRequestHistory[username];
        }

        return new List<string>();
    }

    public bool DeleteRequestHistory(string username)
    {
        if (_userRequestHistory.ContainsKey(username))
        {
            _userRequestHistory.Remove(username);
            return true;
        }

        return false;
    }
}


