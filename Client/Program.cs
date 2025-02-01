﻿using System.Net;
using System.Text;
using System.Text.Json;

CookieContainer cookies = new CookieContainer();
HttpClientHandler handler = new HttpClientHandler();
HttpClient client = new HttpClient(handler);
handler.CookieContainer = cookies;

bool LoginOnServer(string? username, string? password)
{
    try
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Имя пользователя или пароль не могут быть пустыми");
            return false;
        }

        string request = $"/login?login={username}&password={password}";
        var response = client.PostAsync(request, null).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Авторизация прошла успешно");
            //IEnumerable<Cookie> responseCookies = cookies.GetAllCookies();
            //foreach (Cookie cookie in responseCookies)
            //{
            //    Console.WriteLine("Cookie: " + cookie.Name + ": " + cookie.Value);
            //}
            return true;
        }
        else
        {
            Console.WriteLine("Авторизация провалена");
            return false;
        }
    }
    catch (Exception exp)
    {
        Console.WriteLine($"Ошибка при авторизации: {exp.Message}");
        return false;
    }
}

bool RegisterOnServer(string? username, string? password)
{
    try
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Имя пользователя или пароль не могут быть пустыми");
            return false;
        }

        string request = $"/signup?login={username}&password={password}";
        var response = client.PostAsync(request, null).Result;

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            Console.WriteLine("Ошибка: данный пользователь уже существует. Авторизуйтесь или используйте другое имя пользователя");
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Регистрация прошла успешно");
            return true;
        }
        else
        {
            Console.WriteLine("Регистрация провалена");
            return false;
        }
    }
    catch (Exception exp)
    {
        Console.WriteLine($"Ошибка при регистрации: {exp.Message}");
        return false;
    }
}

void ShowAuthenticationMenu()
{
    while (true)
    {
        Console.WriteLine("Выберите действие:");
        Console.WriteLine("1 - Авторизоваться");
        Console.WriteLine("2 - Зарегистрироваться");
        Console.WriteLine("3 - Выйти");
        string? choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                LoginProcess();
                break;
            case "2":
                RegisterProcess();
                break;
            case "3":
                Console.WriteLine("Выход из программы...");
                Environment.Exit(0);
                break; 
            default:
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                break;
        }
    }
}

// Авторизация
void LoginProcess()
{
    Console.Write("Введите логин: ");
    string? username = Console.ReadLine();
    Console.Write("Введите пароль: ");
    string? password = Console.ReadLine();

    if (LoginOnServer(username, password))
    {
        ProcessCommands();
    }
    else
    {
        Console.WriteLine("Авторизация не удалась. Попробуйте снова.");
        ShowAuthenticationMenu();
    }
}

// Регистрация
void RegisterProcess()
{
    Console.Write("Введите логин: ");
    string? username = Console.ReadLine();
    Console.Write("Введите пароль: ");
    string? password = Console.ReadLine();

    if (RegisterOnServer(username, password))
    {
        Console.WriteLine("Регистрация прошла успешно, теперь можете авторизоваться.");
        LoginProcess();
    }
    else
    {
        Console.WriteLine("Ошибка регистрации. Попробуйте снова.");
        ShowAuthenticationMenu();
    }
}

void EncryptText()
{
    try
    {
        Console.WriteLine("Выберите вариант:");
        Console.WriteLine("1 - Выбрать текст из существующих");
        Console.WriteLine("2 - Ввести новый текст");
        string? choice = Console.ReadLine();

        string textToEncrypt;

        if (choice == "1")
        {
            ViewAllTexts();

            Console.WriteLine("Введите номер текста для шифрования:");
            string? index = Console.ReadLine();

            string request = $"/get-text?textIndex={index}";
            var response = client.GetAsync(request).Result;
            if (response.IsSuccessStatusCode)
            {
                textToEncrypt = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Выбранный текст: " + textToEncrypt);
            }
            else
            {
                Console.WriteLine("Ошибка получения текста.");
                return;
            }
        }
        else if (choice == "2")
        {
            Console.Write("Введите текст для шифрования: ");
            string? inputText = Console.ReadLine();
            
            if (inputText == null)
            {
                Console.WriteLine("Ошибка: введен пустой текст.");
                return;
            }
            textToEncrypt = inputText;
        }
        else
        {
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            return;
        }

        Console.WriteLine("Введите ключ для шифрования:");
        string? key = Console.ReadLine();

        if (key != null && key.Any(c => Char.IsLetter(c)))
        {
            Console.WriteLine("Ошибка: ключ не должен содержать буквы.");
            return;
        }

        string requestEncrypt = $"/encrypt?text={textToEncrypt}&key={key}";
        var responseEncrypt = client.PostAsync(requestEncrypt, null).Result;

        if (responseEncrypt.IsSuccessStatusCode)
        {
            string encryptedText = responseEncrypt.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Шифрование выполнено. Результат: " + encryptedText); 
        }
        else
        {
            Console.WriteLine("Ошибка шифрования");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Произошла ошибка: " + ex.Message);
    }
}

void DecryptText()
{
    try
    {
        Console.WriteLine("Введите текст для дешифрования:");
        string? text = Console.ReadLine();
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine("Текст для дешифрования не может быть пустым.");
            return;
        }

        Console.WriteLine("Введите ключ:");
        string? key = Console.ReadLine();
        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Ключ не может быть пустым.");
            return;
        }

        if (key.Any(c => Char.IsLetter(c)))
        {
            Console.WriteLine("Ошибка: ключ не должен содержать буквы.");
            return;
        }

        string request = $"/decrypt?text={text}&key={key}";
        var response = client.PostAsync(request, null).Result;

        if (response.IsSuccessStatusCode)
        {
            string decryptedText = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Дешифрование выполнено. Результат: " + decryptedText);
        }
        else
        {
            Console.WriteLine("Ошибка дешифрования. Статус: " + response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Произошла ошибка: " + ex.Message);
    }
}




void ChangePassword()
{
    try
    {
        Console.WriteLine("Введите старый пароль:");
        string? oldPassword = Console.ReadLine();
        Console.WriteLine("Введите новый пароль:");
        string? newPassword = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            Console.WriteLine("Пароль не может быть пустым.");
            return;
        }

        string request = $"/change-password?oldPassword={oldPassword}&newPassword={newPassword}";
        var response = client.PatchAsync(request, null).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Пароль успешно изменен");
        }
        else
        {
            Console.WriteLine("Ошибка изменения пароля");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}

void AddText()
{
    try
    {
        Console.WriteLine("Введите текст для добавления:");
        string? text = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("Текст не может быть пустым.");
            return;
        }

        string request = $"/add-text?text={text}";
        var response = client.PostAsync(request, null).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Текст успешно добавлен");
        }
        else
        {
            Console.WriteLine("Ошибка добавления текста");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}

void ViewAllTexts()
{
    try
    {
        string request = "/get-all-texts";
        var response = client.GetAsync(request).Result;
        string json = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Ошибка получения текстов. Код: {response.StatusCode}");
            return;
        }

        var texts = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
        
        if (texts == null || texts.Count == 0)
        {
            Console.WriteLine("Нет сохраненных текстов.");
            return;
        }

        Console.WriteLine("Список текстов:");
        for (int i = 0; i < texts.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {texts[i]}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}



void ViewText()
{
    try
    {
        Console.WriteLine("Введите номер текста:");
        string? index = Console.ReadLine();

        string request = $"/get-text?textIndex={index}";
        var response = client.GetAsync(request).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Текст: " + response.Content.ReadAsStringAsync().Result);
        }
        else
        {
            Console.WriteLine("Ошибка получения текста");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}

void DeleteText()
{
    try
    {
        ViewAllTexts();

        Console.WriteLine("Введите номер текста для удаления:");
        string? index = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(index) || !int.TryParse(index, out _))
        {
            Console.WriteLine("Ошибка: Некорректный номер.");
            return;
        }

        string request = $"/delete-text?textIndex={index}";
        var response = client.DeleteAsync(request).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Текст успешно удален.");
        }
        else
        {
            Console.WriteLine("Ошибка: Текст с указанным номером не найден.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка выполнения операции удаления: " + ex.Message);
    }
}
void UpdateText()
{
    try
    {
        ViewAllTexts();

        Console.WriteLine("Введите номер текста для изменения:");
        string? index = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(index) || !int.TryParse(index, out _))
        {
            Console.WriteLine("Ошибка: Некорректный номер.");
            return;
        }

        Console.WriteLine("Введите новый текст:");
        string? newText = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(newText))
        {
            Console.WriteLine("Ошибка: Текст не может быть пустым.");
            return;
        }

        string request = $"/update-text?textIndex={index}&newText={newText}";
        var response = client.PatchAsync(request, null).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Текст успешно изменен.");
        }
        else
        {
            Console.WriteLine("Ошибка: Текст с указанным номером не найден.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка выполнения операции изменения: " + ex.Message);
    }
}

void History()
{
    try
    {
        string request = $"/history";
        var response = client.GetAsync(request).Result;

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("История запросов:\n" + response.Content.ReadAsStringAsync().Result);
        }
        else
        {
            Console.WriteLine("Ошибка получения истории запросов");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка: " + ex.Message);
    }
}

void DeleteHistory()
{
    Console.WriteLine("Вы действительно хотите удалить всю историю запросов? (y/n)");
    string? choice = Console.ReadLine();

    if (choice?.ToLower() == "y")
    {
        try
        {
            string request = "/delete";
            var response = client.DeleteAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("История запросов успешно удалена.");
            }
            else
            {
                Console.WriteLine("Ошибка при удалении истории запросов.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
    else
    {
        Console.WriteLine("Удаление истории отменено.");
    }
}

void ProcessCommands()
{
    while (true)
    {
        Console.WriteLine("\nВыберите команду:");
        Console.WriteLine("1 - Шифрование текста");
        Console.WriteLine("2 - Дешифрование текста");
        Console.WriteLine("3 - Изменение пароля");
        Console.WriteLine("4 - Добавить текст");
        Console.WriteLine("5 - Просмотр всех текстов");
        Console.WriteLine("6 - Просмотр одного текста");
        Console.WriteLine("7 - Удалить текст");
        Console.WriteLine("8 - Изменить текст");
        Console.WriteLine("9 - Просмотр истории запросов");
        Console.WriteLine("10 - Удалить историю запросов");
        Console.WriteLine("11 - Выход");

        string? command = Console.ReadLine();

        switch (command)
        {
            case "1":
                EncryptText();
                break;
            case "2":
                DecryptText();
                break;
            case "3":
                ChangePassword();
                break;
            case "4":
                AddText();
                break;
            case "5":
                ViewAllTexts();
                break;
            case "6":
                ViewText();
                break;
            case "7":
                DeleteText();
                break;
            case "8":
                UpdateText();
                break;
            case "9":
                History();
                break;
            case "10":
                DeleteHistory();
                break;
            case "11":
                Console.WriteLine("Выход из приложения...");
                Environment.Exit(0);
                break;;
            default:
                Console.WriteLine("Неверная команда. Попробуйте снова.");
                break;
        }
    }
}

const string DEFAULT_SERVER_URL = "http://localhost:5000";
Console.Write("Введите URL сервера (http://localhost:5000 по умолчанию):");
string? server_url = Console.ReadLine();

if (string.IsNullOrEmpty(server_url))
{
    server_url = DEFAULT_SERVER_URL;
}

try
{
    client.BaseAddress = new Uri(server_url);
    ShowAuthenticationMenu();
}
catch (Exception exp)
{
    Console.WriteLine("Ошибка: " + exp.Message);
}