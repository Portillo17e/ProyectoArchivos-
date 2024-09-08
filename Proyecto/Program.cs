using Newtonsoft.Json;
using System;
using System.IO;

public class FATTable
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public bool IsRecycled { get; set; } = false;
    public int CharCount { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime ModificationDate { get; set; }
    public DateTime? DeletionDate { get; set; }
}

public class FATFile
{
    public string Data { get; set; } // Máximo de 20 caracteres
    public string NextFilePath { get; set; }
    public bool EOF { get; set; }
}

public class FATSystem
{
    // Crear un archivo nuevo
    public void CreateFile(string fileName, string data)
    {
        // Crear la tabla FAT
        FATTable fatTable = new FATTable
        {
            FileName = fileName,
            FilePath = $"./{fileName}_part1.json",
            CharCount = data.Length,
            CreationDate = DateTime.Now,
            ModificationDate = DateTime.Now
        };

        // Serializar tabla FAT
        string fatTableJson = JsonConvert.SerializeObject(fatTable);
        File.WriteAllText($"./{fileName}_FAT.json", fatTableJson);

        // Dividir datos en segmentos de 20 caracteres y crear archivos FAT
        int partCount = (int)Math.Ceiling((double)data.Length / 20);
        for (int i = 0; i < partCount; i++)
        {
            FATFile fatFile = new FATFile
            {
                Data = data.Substring(i * 20, Math.Min(20, data.Length - i * 20)),
                NextFilePath = (i == partCount - 1) ? null : $"./{fileName}_part{i + 2}.json",
                EOF = (i == partCount - 1)
            };

            string fatFileJson = JsonConvert.SerializeObject(fatFile);
            File.WriteAllText($"./{fileName}_part{i + 1}.json", fatFileJson);
        }
    }

    // Leer un archivo
    public string ReadFile(string fileName)
    {
        string fatTableJson = File.ReadAllText($"./{fileName}_FAT.json");
        FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(fatTableJson);

        string content = string.Empty;
        string currentFilePath = fatTable.FilePath;

        while (!string.IsNullOrEmpty(currentFilePath))
        {
            string fatFileJson = File.ReadAllText(currentFilePath);
            FATFile fatFile = JsonConvert.DeserializeObject<FATFile>(fatFileJson);

            content += fatFile.Data;
            currentFilePath = fatFile.NextFilePath;
        }

        return content;
    }

    // Modificar un archivo
    public void ModifyFile(string fileName, string newData)
    {
        FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(File.ReadAllText($"./{fileName}_FAT.json"));
        string currentFilePath = fatTable.FilePath;

        // Eliminar fragmentos antiguos
        while (!string.IsNullOrEmpty(currentFilePath))
        {
            File.Delete(currentFilePath);
            FATFile fatFile = JsonConvert.DeserializeObject<FATFile>(File.ReadAllText(currentFilePath));
            currentFilePath = fatFile.NextFilePath;
        }

        // Crear nuevos fragmentos con los nuevos datos
        CreateFile(fileName, newData);
    }

    // Eliminar un archivo (marcar como reciclado)
    public void DeleteFile(string fileName)
    {
        FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(File.ReadAllText($"./{fileName}_FAT.json"));
        fatTable.IsRecycled = true;
        fatTable.DeletionDate = DateTime.Now;

        string fatTableJson = JsonConvert.SerializeObject(fatTable);
        File.WriteAllText($"./{fileName}_FAT.json", fatTableJson);
    }

    // Recuperar un archivo
    public void RecoverFile(string fileName)
    {
        FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(File.ReadAllText($"./{fileName}_FAT.json"));
        fatTable.IsRecycled = false;
        fatTable.ModificationDate = DateTime.Now;

        string fatTableJson = JsonConvert.SerializeObject(fatTable);
        File.WriteAllText($"./{fileName}_FAT.json", fatTableJson);
    }

    // Listar archivos no eliminados
    public void ListFiles()
    {
        string[] files = Directory.GetFiles("./", "*_FAT.json");
        int index = 1;

        foreach (string file in files)
        {
            FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(File.ReadAllText(file));
            if (!fatTable.IsRecycled)
            {
                Console.WriteLine($"{index}. {fatTable.FileName} - Tamaño: {fatTable.CharCount} caracteres - Creado: {fatTable.CreationDate} - Modificado: {fatTable.ModificationDate}");
                index++;
            }
        }
    }

    // Listar archivos en la papelera de reciclaje
    public void ListDeletedFiles()
    {
        string[] files = Directory.GetFiles("./", "*_FAT.json");
        int index = 1;

        foreach (string file in files)
        {
            FATTable fatTable = JsonConvert.DeserializeObject<FATTable>(File.ReadAllText(file));
            if (fatTable.IsRecycled)
            {
                Console.WriteLine($"{index}. {fatTable.FileName} - Tamaño: {fatTable.CharCount} caracteres - Eliminado: {fatTable.DeletionDate}");
                index++;
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        FATSystem fatSystem = new FATSystem();

        while (true)
        {
            Console.WriteLine("1. Crear un archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir un archivo");
            Console.WriteLine("4. Modificar un archivo");
            Console.WriteLine("5. Eliminar un archivo");
            Console.WriteLine("6. Recuperar un archivo");
            Console.WriteLine("7. Listar archivos eliminados");
            Console.WriteLine("8. Salir");

            int option = Convert.ToInt32(Console.ReadLine());

            switch (option)
            {
                case 1:
                    Console.Write("Nombre del archivo: ");
                    string fileName = Console.ReadLine();
                    Console.Write("Datos (texto): ");
                    string data = Console.ReadLine();
                    fatSystem.CreateFile(fileName, data);
                    break;

                case 2:
                    fatSystem.ListFiles();
                    break;

                case 3:
                    Console.Write("Nombre del archivo a abrir: ");
                    string fileToRead = Console.ReadLine();
                    string content = fatSystem.ReadFile(fileToRead);
                    Console.WriteLine($"Contenido del archivo {fileToRead}: {content}");
                    break;

                case 4:
                    Console.Write("Nombre del archivo a modificar: ");
                    string fileToModify = Console.ReadLine();
                    Console.Write("Nuevos datos: ");
                    string newData = Console.ReadLine();
                    fatSystem.ModifyFile(fileToModify, newData);
                    break;

                case 5:
                    Console.Write("Nombre del archivo a eliminar: ");
                    string fileToDelete = Console.ReadLine();
                    fatSystem.DeleteFile(fileToDelete);
                    break;

                case 6:
                    Console.Write("Nombre del archivo a recuperar: ");
                    string fileToRecover = Console.ReadLine();
                    fatSystem.RecoverFile(fileToRecover);
                    break;

                case 7:
                    fatSystem.ListDeletedFiles();
                    break;

                case 8:
                    return;
            }
        }
    }
}
