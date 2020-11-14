﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace Core.Storage
{
    public class FileOperations
    {
        public static async Task WriteObjectToFile(string path, object obj)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                var str = JsonConvert.SerializeObject(obj);
                var bytes = System.Text.Encoding.Default.GetBytes(str);
                await fileStream.WriteAsync(bytes);
            }
        }

        public static Task CheckOrCreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        public static Task RenameDirectory(string oldPath, string newName)
        {
            //Пиздец (
            try
            {
                    FileSystem.RenameDirectory(oldPath, newName);
            }
            catch
            {
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
        
    }
}