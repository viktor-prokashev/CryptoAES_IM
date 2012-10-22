using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;

namespace HttpServerAES
{
    class Program
    {
        public static string log = "Server: Соединение прошло успешно\n";
        public static Dictionary<string, DateTime> onlineUsers = new Dictionary<string, DateTime>();//Список пользователей
        public static string ivAES = "";//Вектор инициализации
        public static string keyAES = "";//Ключ шифрования

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            //Шифрование
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            //Расшифровка
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            string plaintext = null;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        public static void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;//Запрос от колиента
            
            System.Security.Cryptography.X509Certificates.X509Certificate2 cerFromClient = context.Request.GetClientCertificate();
            X509CertificateValidator validator = new X509CertificateValidator();
            validator.Validate(cerFromClient);
            HttpListenerResponse response = context.Response;//Ответ клиенту
            System.IO.Stream inputStream = context.Request.InputStream;//Текст от клиента
            byte[] inputByte = new byte[request.ContentLength64];
            string responseString = "";
            byte[] message;
            if (request.HttpMethod == "GET")//Отправка ответа
            {
                if (!onlineUsers.ContainsKey(request.RemoteEndPoint.Address.ToString()))//Добавленике пользователя к списку
                {
                    onlineUsers.Add(request.RemoteEndPoint.Address.ToString(), DateTime.Now);
                }
                List<string> names = onlineUsers.Keys.ToList();
                for (int i = 0; i < onlineUsers.Count; i++)//Поиск выключенных пользователей
                {
                    TimeSpan off = (onlineUsers[names[i]] - DateTime.Now);
                    if (off.Seconds < -10)//Если 10 секунд пользователь не в сети
                    {
                        onlineUsers.Remove(names[i]);//Убираем пользователя из списка
                    }
                }
                string usersNow = ";";
                for (int i = 0; i < onlineUsers.Count; i++)
                {
                    usersNow = usersNow + names[i] + ";";//Формирование списка пользователей для клиента
                }

                Console.WriteLine("Connecting from IP: " + request.RemoteEndPoint.Address.ToString());
                message = EncryptStringToBytes_Aes(log + usersNow, Convert.FromBase64String(keyAES), Convert.FromBase64String(ivAES));
                responseString = Encoding.UTF8.GetString(message);
            }
            else//Получение сообщения от пользователя
            {
                inputStream.Read(inputByte, 0, (int)request.ContentLength64);
                string messageFromClient = DecryptStringFromBytes_Aes(inputByte, Convert.FromBase64String(keyAES), Convert.FromBase64String(ivAES));
                log = log + request.RemoteEndPoint.Address.ToString() + ":  " + messageFromClient + "\n";
                Console.WriteLine("Message from IP " + request.RemoteEndPoint.Address.ToString() + ":  " + messageFromClient);
                string usersNow = ";";
                List<string> names = onlineUsers.Keys.ToList();
                for (int i = 0; i < onlineUsers.Count; i++)
                {
                    usersNow = usersNow + names[i] + ";";//Формирование списка пользователей для клиента
                }
                message = EncryptStringToBytes_Aes(log + usersNow, Convert.FromBase64String(keyAES), Convert.FromBase64String(ivAES));
                
            }
            System.IO.Stream output = response.OutputStream;
            output.Write(message, 0, message.Length);//Отправка
            output.Close();//Закрытие соединения
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
        }

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener(); //Класс прослушивания http-протокола
            DirectoryInfo directory = new DirectoryInfo(".");
            if (File.Exists(directory.FullName + "\\settings.txt"))//Загрузка ключей
            {
                StreamReader loadSettings = new StreamReader(directory.FullName + "\\settings.txt", false);
                ivAES = loadSettings.ReadLine();
                keyAES = loadSettings.ReadLine();
            }
            listener.Prefixes.Add("http://*:80/");//Префикс для адреса сервера
            listener.Start();//Запуск прослушивания
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);//Асинхронный приём запросов
            Console.WriteLine("Server is running!!!\n");
            Console.ReadLine();
        }
    }
}
