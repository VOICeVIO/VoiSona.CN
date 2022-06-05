using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace VoiSona.CN.Translator
{
    internal class Program
    {
        private const string CnCsv = "VoiSona-cn.csv";
        private const string JpCsv = "VoiSona-jp.csv";
        private const string VoiDefaultPath = @"C:\Program Files\Techno-Speech\VoiSona\VoiSona.exe";
        private const string CsvDownloadUrl = @"https://raw.githubusercontent.com/VOICeVIO/VoiSona.CN/main/text/VoiSona-cn.csv";

        static void Main(string[] args)
        {
            Console.WriteLine("VoiSona.CN - VoiSona汉化工具");
            Console.WriteLine("by Ulysses, wdwxy12345@gmail.com");
            Console.WriteLine();

            Console.WriteLine(@"注意事项:
  建议以管理员权限运行本工具。
  建议您将软件目录下的reporter目录重命名，以取消崩溃信息上报。
  若软件更新，需要重新运行本工具。
  本工具仅进行文本汉化。使用本工具产生的一切后果需自负。
  如果您的账号因此被禁用，请保持淡定并通过邮件与我们联系。
  
  本工具基于CC BY-NC-SA 4.0（署名-非商业性使用-相同方式共享）协议提供，
  如果您使用了本工具，您需要：
  保持原作者信息；保证不用于盈利；对文本和程序的修改需要开源。
[按 Enter 键同意上述说明，并继续操作]");
            Console.ReadLine();

            var exePath = "VoiSona.exe";

            if (args.Length > 0 && File.Exists(args[0]))
            {
                exePath = args[0];
            }
            else if (!File.Exists(exePath))
            {
                if (File.Exists(VoiDefaultPath))
                {
                    exePath = VoiDefaultPath;
                }
                else
                {
                    Console.WriteLine("[ERROR] Cannot found VoiSona.exe!");
                    Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine($"Found VoiSona: {exePath}");

            
            var vstPath = "VoiSona Song.vst3"; 
            if (args.Length > 1 && File.Exists(args[1]))
            {
                vstPath = args[1];
            }
            else if (!File.Exists(vstPath))
            {
                var commonFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                if (!string.IsNullOrEmpty(commonFiles))
                {
                    var vstPath2 = Path.Combine(commonFiles, "VST3", "Techno-Speech", "VoiSona Song.vst3");
                    if (File.Exists(vstPath2))
                    {
                        vstPath = vstPath2;
                    }
                }
            }

            if (!File.Exists(vstPath))
            {
                Console.WriteLine("[ERROR] Cannot found VoiSona Song.vst3!");
                Console.ReadLine();
                return;
            }
            Console.WriteLine($"Found VST: {vstPath}");

            if (!File.Exists(CnCsv))
            {
                Console.WriteLine("[WARNING] 没有中文文本可供加载。是否尝试下载汉化文本？（需联网到github）");
                Console.WriteLine("[输入 1 联网下载；直接按 Enter 跳过下载（不汉化，只提取文本）]");
                var r = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(r) && r.Trim() == "1")
                {
                    bool ok = false;
                    for (int i = 0; i < 2; i++)
                    {
                        if (DownloadCsv())
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        Console.WriteLine("[WARNING] 下载中文文本失败，因此只提取文本，不进行汉化。");
                    }
                }
            }

            var textDic = new Dictionary<string, string>();
            Process("VoiSona", exePath, textDic);
            Process("VoiSona Song", vstPath, textDic);

            Console.WriteLine("Saving text CSV...");
            SaveCsv(textDic, JpCsv);
            if (File.Exists(CnCsv))
            {
                MergeCsv(textDic, CnCsv);
            }
            
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        public static void Process(string fileName, string path, Dictionary<string, string>? textCollection = null)
        {
            Console.WriteLine($"Start processing {fileName}");
            if (!File.Exists(path))
            {
                Console.WriteLine($"[ERROR] 找不到 {fileName} 程序!");
                Console.ReadLine();
                return;
            }

            var binary = File.ReadAllBytes(path);
            var jpMark = Encoding.UTF8.GetBytes("language: Japanese \r\n");
            var pos = binary.FindIndexOf(jpMark);
            int maxLength = 0;
            if (pos > 0)
            {
                Console.WriteLine("Searching for JP text...");
                var endPos = binary.FindIndexOf(new byte[4], pos + 1);
                if (endPos > 0)
                {
                    maxLength = (int)(endPos - pos);
                    Console.WriteLine($"Found JP text at {pos}, length: {maxLength}");
                }

                if (endPos <= 0 || maxLength <= 0)
                {
                    Console.WriteLine("[ERROR] 找不到程序中的日语文本。");
                    Console.ReadLine();
                    return;
                }
                
                Console.WriteLine("Applying font...");
                long fontPos = 1;
                int fontHitCount = 0;
                //var cnFont = Encoding.UTF8.GetBytes("微软雅黑\0"); //not working
                var jpFont = Encoding.UTF8.GetBytes("MS UI Gothic\0\0\0\0");
                //var cnFont = Encoding.UTF8.GetBytes("Microsoft YaHei\0");
                var cnFont = Encoding.UTF8.GetBytes("SimHei\0");
                while (fontPos > 0)
                {
                    fontPos = binary.FindIndexOf(jpFont);
                    if (fontPos <= 0)
                    {
                        break;
                    }

                    fontHitCount++;
                    Console.WriteLine($"Set CN font at {fontPos}");
                    var fontSpan = binary.AsSpan((int)fontPos, jpFont.Length);
                    fontSpan.Fill(0);
                    cnFont.CopyTo(fontSpan);
                }
                Console.WriteLine($"Fix font usages for {fontHitCount} times.");

                Console.WriteLine("Fixing VST reference...");
                var oriVst = Encoding.UTF8.GetBytes(" Song.vst3\0");
                var modVst = Encoding.UTF8.GetBytes("_CN-S.vst3\0");
                var vstPos = binary.FindIndexOf(oriVst);
                if (vstPos <= 0)
                {
                    Console.WriteLine("[ERROR] 找不到VST引用。");
                    Console.ReadLine();
                    return;
                }
                Console.WriteLine($"Set VST reference at {vstPos}");
                var vstSpan = binary.AsSpan((int)vstPos, oriVst.Length);
                vstSpan.Fill(0);
                modVst.CopyTo(vstSpan);

                var oriEnJp = Encoding.UTF8.GetBytes("\"Japanese\" = \"Japanese\"");
                var modEnJp = Encoding.UTF8.GetBytes("\"Japanese\" = \"Chinese\" ");
                var enJpPos = binary.FindIndexOf(oriEnJp);
                if (enJpPos > 0)
                {
                    Console.WriteLine($"Set Language ComboBox at {enJpPos}");
                    var enJpSpan = binary.AsSpan((int)enJpPos, oriEnJp.Length);
                    modEnJp.CopyTo(enJpSpan);
                }

                var oriManual = Encoding.UTF8.GetBytes("https://voisona.com/manual_song/\0");
                var modManual = Encoding.UTF8.GetBytes("https://voicevio.github.io/vsn/\0");
                //var modManual = Encoding.UTF8.GetBytes("https://github.com/voicevio/\0");
                var manualPos = binary.FindIndexOf(oriManual);
                if (manualPos > 0)
                {
                    Console.WriteLine($"Set Manual URL at {manualPos}");
                    var manualSpan = binary.AsSpan((int)manualPos, oriManual.Length);
                    manualSpan.Fill(0);
                    modManual.CopyTo(manualSpan);
                }

                var oriVerInfo = Encoding.UTF8.GetBytes("\n(c) 2022 Techno-Speech. \nAll rights reserved.\0");
                var modVerInfo = Encoding.UTF8.GetBytes("\n中文汉化 by VOICeVIO\nwdwxy12345@gmail.com\0");
                var verPos = binary.FindIndexOf(oriVerInfo);
                if (verPos > 0)
                {
                    var verSpan = binary.AsSpan((int)verPos, oriVerInfo.Length);
                    verSpan.Fill(0);
                    modVerInfo.CopyTo(verSpan);
                }

                var textSpan = binary.AsSpan((int)pos..(int)endPos);
                Console.WriteLine("Extracting texts...");
                var jpText = Encoding.UTF8.GetString(textSpan);
                File.WriteAllText($"{fileName}-jp.txt", jpText);

                var textDic = ParseJuceLocalizationText(jpText, out _, out _);
                if (textCollection != null)
                {
                    foreach (var kv in textDic)
                    {
                        if (textCollection.ContainsKey(kv.Key))
                        {
                            if (textCollection[kv.Key] == kv.Value)
                            {
                                continue;
                            }
                            else
                            {
                                Console.WriteLine($"文本冲突: {kv.Key} has different value:\r\n {textCollection[kv.Key]}\r\n {kv.Value}");
                            }
                        }
                        else
                        {
                            textCollection[kv.Key] = kv.Value;
                        }
                    }
                }

                if (File.Exists(CnCsv))
                {
                    Console.WriteLine("Loading CN CSV...");
                    var cnDic = LoadCsv(CnCsv);
                    var cnText = BuildJuceLocalizationText(cnDic, originalTextDic: textDic);
                    //var cnText = BuildJuceLocalizationText(cnDic, "Japanese", "ja");
                    var cnBytes = Encoding.UTF8.GetBytes(cnText);
                    if (cnBytes.Length > maxLength)
                    {
                        Console.WriteLine("[ERROR] 中文文本太长。请简化翻译。");
                        Console.ReadLine();
                        return;
                    }

                    Console.WriteLine("Saving CN binary...");
                    textSpan.Fill(0);
                    cnBytes.CopyTo(textSpan);

                    try
                    {
                        if (fileName == "VoiSona Song")
                        {
                            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "VoiSona_CN-S.vst3"), binary);
                        }
                        else
                        {
                            File.WriteAllBytes(Path.ChangeExtension(path, ".CN.exe"), binary);
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine("[ERROR] 权限不足，无法保存文件，请以管理员权限再次运行本工具。");
                        Console.ReadLine();
                        return;
                    }

                    Console.WriteLine($"[OK] {fileName} is processed.");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("[WARNING] 没有中文文本可供加载，因此只提取文本，不进行汉化。");
                }
            }
        }

        private static bool DownloadCsv()
        {
            const int timeout = 60;
            Console.WriteLine($"尝试下载中文文本（{timeout}秒后超时）...");
            using var client = new HttpClient();
            try
            {
                var task = client.GetStringAsync(CsvDownloadUrl);
                if (task.Wait(timeout * 1000))
                {
                    var bin = task.Result;
                    File.WriteAllText(CnCsv, bin);
                    Console.WriteLine("[OK] 下载成功。");
                    return true;
                }

                Console.WriteLine("[ERROR] 下载超时，请检查网络连接，以及是否可访问github。");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[ERROR] 下载中文文本时发生错误。");
                Console.WriteLine(e);
            }

            return false;
        }

        public static Dictionary<string, string> ParseJuceLocalizationText(string text, out string lang, out string locale)
        {
            lang = "Japanese";
            locale = "ja";
            var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var dic = new Dictionary<string, string>(lines.Length);
            foreach (var line in lines)
            {
                if (line.StartsWith("language:"))
                {
                    lang = line[(line.IndexOf(":", StringComparison.Ordinal) + 1) ..].Trim();
                }
                else if (line.StartsWith("countries:"))
                {
                    locale = line[(line.IndexOf(":", StringComparison.Ordinal) + 1) ..].Trim();
                }
                else if (line.Contains('='))
                {
                    var kv = line.Split('=');
                    if (kv.Length < 2)
                    {
                        continue;
                    }

                    var key = kv[0].Trim(' ', '"');
                    var value = kv[1].Trim(' ', '"');
                    dic[key] = value;
                }
            }

            return dic;
        }

        public static string BuildJuceLocalizationText(Dictionary<string, string> textDic, string lang = "Chinese", string locale = "zh", Dictionary<string, string>? originalTextDic = null)
        {
            //TODO: Environment.NewLine on Linux?
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"language: {lang}");
            sb.AppendLine($"countries: {locale}");
            sb.AppendLine();

            if (originalTextDic != null)
            {
                foreach (var kv in originalTextDic)
                {
                    if (textDic.ContainsKey(kv.Key))
                    {
                        sb.AppendLine($"\"{kv.Key}\" = \"{textDic[kv.Key]}\"");
                    }
                    else
                    {
                        sb.AppendLine($"\"{kv.Key}\" = \"{kv.Value}\"");
                    }
                }
            }
            else
            {
                foreach (var kv in textDic)
                {
                    sb.AppendLine($"\"{kv.Key}\" = \"{kv.Value}\"");
                }
            }


            return sb.ToString();
        }

        private static void SaveCsv(Dictionary<string, string> texts, string path = "text.csv", bool saveAll = true)
        {
            var units = texts.Select(pair => new Unit(pair.Key, pair.Value));
            using var fs = File.Create(path);
            using var writer = new StreamWriter(fs, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(saveAll ? units : units.Where(u => !string.IsNullOrEmpty(u.Trans)));
        }

        private static Dictionary<string, string> LoadCsv(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("CSV file not found", path);
            }

            using var reader = new StreamReader(path, new UTF8Encoding(true));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {Delimiter = ","});
            var records = csv.GetRecords<Unit>();
            return new List<Unit>(records).ToDictionary(u => u.Origin, u => u.Trans);
        }

        private static void MergeCsv(Dictionary<string, string> textDic, string from)
        {
            var count = 0;
            var units = LoadCsv(from);
            foreach (var kv in textDic)
            {
                if (!units.ContainsKey(kv.Key))
                {
                    Console.WriteLine($"New text: {kv.Key} = {kv.Value}");
                    units[kv.Key] = kv.Value;
                    count++;
                }
            }

            if (count > 0)
            {
                Console.WriteLine($"Merged {count} new texts into CN CSV.");
                SaveCsv(units, from, true);
            }
        }
    }
}