using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace WordCount
{
    public class InputFile
    {
        readonly object _streamReaderLock;
        readonly StreamReader _streamReader;

        public InputFile(string inputFileName)
        {
            _streamReaderLock = new object();
            _streamReader = new StreamReader(inputFileName);
        }
        public string? ReadInputFileLine()
        {
            string? line = string.Empty;

            lock (_streamReaderLock)
            {
                line = _streamReader.ReadLine();
            }

            return line;
        }
        
        public void CloseFile()
        {
            _streamReader.Close();
        }
    }
    public class OutputFile
    {
        readonly StreamWriter _streamWriter;
        public StreamWriter StreamWriter 
        { 
            get
            {
                return _streamWriter;
            }
        }

        public OutputFile(string outputFileName)
        {
            _streamWriter = new StreamWriter(outputFileName);
        }
        
        public void CloseFile()
        {
            _streamWriter.Close();
        }
    }

    public class WordList
    {
        readonly object sortedDictionaryLock;
        readonly SortedDictionary<string, int> sortedDictionary;

        public WordList()
        {
            sortedDictionaryLock = new object();
            sortedDictionary = new SortedDictionary<string, int>();
        }     

        public void AddWordList(string[] words)
        {
            int value = 1;

            lock (sortedDictionaryLock)
            {
                foreach (string word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        if (sortedDictionary.ContainsKey(word))
                        {
                            sortedDictionary.Remove(word, out value);
                            value++;
                        }

                        sortedDictionary.Add(word, value);
                    }
                }
            }
        }
        public void ProcessLine(string line)
        {
            Regex myRegex = new Regex(@"[\p{P}0-9\t]");

            string newLine = myRegex.Replace(line, "");

            string[] words = newLine.Split(' ');

            AddWordList(words);
        }

        public void WriteWordList(StreamWriter sw)
        {
            foreach (var item in sortedDictionary.OrderByDescending(key => key.Value))
            {
                sw.WriteLine($"{item.Key},{item.Value}");
            }
        }
    }

    internal class Program
    {       
        static void Process(InputFile inputFile, WordList wordList)
        {
            try
            {
                string? line = string.Empty;

                while ((line = inputFile.ReadInputFileLine()) != null)
                {
                    wordList.ProcessLine(line);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Input and output file name not provided");
                return;
            }

            if (File.Exists(args[0]) == false)
            {
                Console.WriteLine("Invalid input file name");
                return;
            }

            InputFile inputFile = new InputFile(args[0]);
            OutputFile outputFile = new OutputFile(args[1]);

            try
            {
                WordList wordList = new WordList();

                Thread Thread1 = new Thread(() => Process(inputFile, wordList));
                Thread1.Start();

                Thread Thread2 = new Thread(() => Process(inputFile, wordList));
                Thread2.Start();

                Thread1.Join();
                Thread2.Join();

                wordList.WriteWordList(outputFile.StreamWriter);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            { 
                inputFile.CloseFile();
                outputFile.CloseFile();
            }
        }
    }
}
