using Ionic.Zip;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BruteForcer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum ArchiveMode
        {
            rar,
            zip
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void TextBox1DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        public static ArchiveMode mode = ArchiveMode.rar;
        private void TextBox1DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array array = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (array != null)
                {
                    string text = array.GetValue(0).ToString();
                    int num = text.LastIndexOf(".", StringComparison.Ordinal);
                    if (num != -1)
                    {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".rar")
                        {
                            mode = ArchiveMode.rar;
                            Activate();
                            RarPath = text;
                            RarT.Content = text;
                            InfoL.Visibility = Visibility.Hidden;
                        }
                        else if(text2 == ".zip")
                            {
                            mode = ArchiveMode.zip;
                            Activate();
                            RarPath = text;
                            RarT.Content = text;
                            InfoL.Visibility = Visibility.Hidden;
                        }
                        else if(text2 == ".txt")
                        {
                            Activate();
                            path = text;
                            WordT.Content = text;
                            InfoL.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
            catch
            {
            }
        }
        public static string RarPath = "";

        public  string path = "";
        public  string password = "";
        public  int PasswordAmount = 0;
        private string newpath = "";
        public System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            watch.Reset();
            Progress.Value = 0;
            List<Task> Tasklist = new List<Task>();
            CancellationTokenSource token = new CancellationTokenSource();
            int NumThread = Environment.ProcessorCount;
            NbrThread.Content = string.Format("Number of threads : {0}", NumThread);
            //Splitting wordlist in x part
            List<string> Wordlist = System.IO.File.ReadAllLines(path).ToList();
            PasswordAmount = Wordlist.Count;
            Progress.Maximum = PasswordAmount;
            List <IEnumerable<string>> splitted = Wordlist.Split(NumThread).ToList();
            newpath = System.IO.Path.GetDirectoryName(RarPath) + @"\RarDump";
            //Clear
            if (Directory.Exists(newpath))
            {
                string[] files = Directory.GetFiles(newpath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(newpath);
            }

           
            watch.Start();
            foreach (IEnumerable<string> lst in splitted)
            {
                Task check = new Task(() => StartBruteForceAsync(lst.ToList(), token, splitted.IndexOf(lst)));
                check.Start();
               
                Tasklist.Add(check);
            }
   
        }
        public void StartBruteForceAsync(List<string> WordList, CancellationTokenSource cancel, int ThreadNum)
        {
            switch(mode)
            {
                case ArchiveMode.zip:

                    using (ZipFile archive = new ZipFile(RarPath))
                    {
                        archive.Encryption = EncryptionAlgorithm.None;
                        for (int i = 0; i < WordList.Count - 1; i++)
                        {
                            if (!cancel.IsCancellationRequested)
                            {
                                archive.Password = WordList[i];
                                try
                                {
                                    Interlocked.Decrement(ref PasswordAmount);
                                    Progress.Dispatcher.Invoke(new Action(() => Progress.Value++));
                                    PasswordT.Dispatcher.Invoke(new Action(() => PasswordT.Content = (string.Format("Password left  : {0} \n", PasswordAmount))));
                                    TestingT.Dispatcher.Invoke(new Action(() => TestingT.Content = string.Format("Testing password  : {0} \n", WordList[i])));
                                    archive.ExtractAll(newpath, ExtractExistingFileAction.OverwriteSilently);
                                    cancel.Cancel();
                                    password = WordList[i];
                                    watch.Stop();
                                    MessageBox.Show(string.Format("Done ! Elapsed time : {0}, password is : {1} \n", watch.Elapsed.TotalSeconds, string.IsNullOrEmpty(password) == false ? password : "Not found ..."), "", MessageBoxButton.OK, MessageBoxImage.Information);
                                    FinalPass.Dispatcher.Invoke(new Action(() => FinalPass.Visibility = Visibility.Visible));
                                    FinalPass.Dispatcher.Invoke(new Action(() => FinalPass.Content = string.Format("Password is : {0}", WordList[i])));

                                    Progress.Dispatcher.Invoke(new Action(() => Progress.Value = Progress.Maximum));
                                  //  return;
                                }
                                catch
                                {

                                }
                            }

                        }
                    }
                    break;
                    
                case ArchiveMode.rar:
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    //Put the path of installed winrar.exe
                    proc.StartInfo.FileName = @"C:\Program Files\WinRAR\unrar.exe";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.EnableRaisingEvents = true;


                    //PWD: Password if the file has any
                    //SRC: The path of your rar file. e.g: c:\temp\abc.rar
                    //DES: The path you want it to be extracted. e.g: d:\extracted

                    //ATTENTION: DESTINATION FOLDER MUST EXIST!

                    for (int i = 0; i < WordList.Count - 1; i++)
                    {
                        if (!cancel.IsCancellationRequested)
                        {
                            proc.StartInfo.Arguments = String.Format("x -p{0} {1} {2}", WordList[i], RarPath, newpath);
                            proc.Start();
                            string output = proc.StandardOutput.ReadToEnd();
                            proc.WaitForExit();
                            Interlocked.Decrement(ref PasswordAmount);
                            Progress.Dispatcher.Invoke(new Action(() => Progress.Value++));
                            PasswordT.Dispatcher.Invoke(new Action(() => PasswordT.Content = (string.Format("Password left  : {0} \n", PasswordAmount))));
                            TestingT.Dispatcher.Invoke(new Action(() => TestingT.Content = string.Format("Testing password  : {0} \n", WordList[i])));
                            if (output.Contains("OK"))
                            {
                                cancel.Cancel();
                                password = WordList[i];
                                watch.Stop();
                                MessageBox.Show(string.Format("Done ! Elapsed time : {0}, password is : {1} \n", watch.Elapsed.TotalSeconds, string.IsNullOrEmpty(password) == false ? password : "Not found ..."), "", MessageBoxButton.OK, MessageBoxImage.Information);

                                //        MessageBox.Show("Password Found", "Success !", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                                FinalPass.Dispatcher.Invoke(new Action(() => FinalPass.Visibility = Visibility.Visible));
                                FinalPass.Dispatcher.Invoke(new Action(() => FinalPass.Content = string.Format("Password is : {0}", WordList[i])));

                                Progress.Dispatcher.Invoke(new Action(() => Progress.Value = Progress.Maximum));

                            }
                        }
                    }
                    break;
              
                default:
                    break;
            }
         
            
        }
        public static List<List<T>> splitList<T>(List<T> locations, int nSize = 30)
        {
            var list = new List<List<T>>();

            for (int i = 0; i < locations.Count; i += nSize)
            {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }

            return list;
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {

        }
    }
    static class LinqExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
        {
            int i = 0;
            var splits = from item in list
                         group item by i++ % parts into part
                         select part.AsEnumerable();
            return splits;
        }
    }
}
