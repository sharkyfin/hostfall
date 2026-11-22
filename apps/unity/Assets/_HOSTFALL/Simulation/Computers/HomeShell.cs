using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Hostfall.Simulation.Computers
{
    [Serializable] public sealed class HomeShellData { public string pagerPath="";public int pagerLine; }
    public readonly struct ShellCompletion
    {public string Text{get;}public string[] Candidates{get;}public ShellCompletion(string text,string[] candidates){Text=text;Candidates=candidates;}}
    // Read-only virtual shell. No Process, reflection, host IO, interpreter or executable lookup.
    public sealed class HomeShell
    {
        private static readonly string[] commands={"help","ls","cd","pwd","cat","less","grep","find","clear","history","man"};
        private int historyCursor;private string historyDraft="";
        public VirtualComputer Machine{get;}
        public string PagerPath{get;private set;}="";
        public int PagerLine{get;private set;}
        public bool Paging=>PagerPath.Length>0;
        public string Prompt=>"operator@home:"+(Machine.WorkingDirectory=="/home/operator"||Machine.WorkingDirectory.StartsWith("/home/operator/",StringComparison.Ordinal)?"~"+Machine.WorkingDirectory.Substring(14):Machine.WorkingDirectory)+"$";
        public event Action Changed;public event Action<string> DocumentRead;
        public HomeShell(VirtualComputer machine){Machine=machine??throw new ArgumentNullException(nameof(machine));historyCursor=machine.Terminal.History.Count;}
        public void SetDraft(string text){SaveValidation.Text(text,4096,"Home input exceeds limit");Machine.Terminal.Draft=text;Changed?.Invoke();}
        public CommandResult Execute(string input)
        {
            if(input==null||input.Length>4096)return new CommandResult("Команда длиннее 4096 символов",1);
            var memory=Machine.Terminal;memory.AddCommand(input);memory.AddLine(Prompt+" "+input,"command-line");memory.Draft="";historyCursor=memory.History.Count;PagerPath="";PagerLine=0;
            CommandResult result;
            try{result=Run(Tokenize(input));}catch(ArgumentException e){result=new CommandResult(e.Message,1);}
            if(result.Clear)memory.Clear();else if(result.Text.Length>0)memory.AddLine(result.Text,result.ExitCode==0?"result":"error");Changed?.Invoke();return result;
        }
        private CommandResult Run(List<string> args)
        {
            if(args.Count==0)return Ok("");string command=args[0];args.RemoveAt(0);
            switch(command)
            {
                case "help":case "man":return args.Count<=1?Ok(Help(args.Count==0?"":args[0])):Error("help [команда]");
                case "pwd":return args.Count==0?Ok(Machine.WorkingDirectory):Error("pwd не принимает аргументов");
                case "clear":return args.Count==0?new CommandResult("",clear:true):Error("clear не принимает аргументов");
                case "history":return args.Count==0?Ok(string.Join("\n",Machine.Terminal.History.Select((s,i)=>(i+1)+"  "+s))):Error("history не принимает аргументов");
                case "cd":
                    if(args.Count>1)return Error("cd [каталог]");string directory=Resolve(args.Count==0?"~":args[0]);
                    if(!Machine.IsDirectory(directory))return Error("cd: каталог не найден: "+directory);Machine.WorkingDirectory=directory;return Ok("");
                case "ls":return List(args);
                case "cat":
                    if(args.Count==0)return Error("cat <файл> [файл...]");var content=new List<string>();
                    foreach(string name in args){string path=Resolve(name);if(!Machine.TryRead(path,out string text))return Error("cat: файл не найден: "+path);content.Add(text);DocumentRead?.Invoke(path);}return Ok(string.Join("\n",content));
                case "less":
                    if(args.Count!=1)return Error("less <файл>");PagerPath=Resolve(args[0]);
                    if(!Machine.TryRead(PagerPath,out _)){PagerPath="";return Error("less: файл не найден");}DocumentRead?.Invoke(PagerPath);return Ok(PagerText);
                case "grep":return Grep(args);
                case "find":return Find(args);
                default:return Error(command+": команда не найдена. Список — help.");
            }
        }
        private CommandResult List(List<string> args)
        {
            bool all=false,details=false;string path=Machine.WorkingDirectory;bool hasPath=false;
            foreach(string arg in args)
            {
                if(arg.StartsWith("-",StringComparison.Ordinal))foreach(char c in arg.Substring(1)){if(c=='a')all=true;else if(c=='l')details=true;else return Error("ls: неизвестный параметр -"+c);}
                else {if(hasPath)return Error("ls [-la] [каталог]");path=Resolve(arg);hasPath=true;}
            }
            if(Machine.TryRead(path,out string file))return Ok(details?"-r--r--r-- operator "+Encoding.UTF8.GetByteCount(file)+" "+Basename(path):Basename(path));
            if(!Machine.IsDirectory(path))return Error("ls: путь не найден: "+path);
            return Ok(string.Join("\n",Machine.List(path).Where(n=>all||!n.StartsWith(".",StringComparison.Ordinal)).Select(n=>
            {if(!details)return n;bool dir=n.EndsWith("/",StringComparison.Ordinal);string full=VirtualComputer.Resolve(path,n);Machine.TryRead(full,out var text);return(dir?"dr-xr-xr-x":"-r--r--r--")+" operator "+(dir?0:Encoding.UTF8.GetByteCount(text??""))+" "+n;})));
        }
        private CommandResult Grep(List<string> args)
        {
            bool ignore=false,numbers=false;int index=0;
            while(index<args.Count&&args[index].StartsWith("-",StringComparison.Ordinal)){foreach(char c in args[index++].Substring(1)){if(c=='i')ignore=true;else if(c=='n')numbers=true;else return Error("grep: допустимы -i и -n");}}
            if(args.Count-index<2)return Error("grep [-in] <текст> <файл> [файл...]");string pattern=args[index++];var lines=new List<string>();int count=args.Count-index;
            foreach(string arg in args.Skip(index))
            {
                string path=Resolve(arg);if(!Machine.TryRead(path,out var text))return Error("grep: файл не найден: "+path);int number=0;
                foreach(string line in text.Replace("\r","").Split('\n')){number++;if(line.IndexOf(pattern,ignore?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal)>=0)lines.Add((count>1?path+":":"")+(numbers?number+":":"")+line);}
            }
            return lines.Count>0?Ok(string.Join("\n",lines)):new CommandResult("Совпадений нет",1);
        }
        private CommandResult Find(List<string> args)
        {
            string path=Machine.WorkingDirectory,pattern="*",type="";int i=0;if(args.Count>0&&!args[0].StartsWith("-",StringComparison.Ordinal))path=Resolve(args[i++]);
            while(i<args.Count){string option=args[i++];if(i>=args.Count)return Error("find [путь] [-name шаблон] [-type f|d]");string value=args[i++];if(option=="-name")pattern=value;else if(option=="-type"&&(value=="f"||value=="d"))type=value;else return Error("find: неизвестный параметр");}
            if(!Machine.IsDirectory(path)&&!Machine.TryRead(path,out _))return Error("find: путь не найден: "+path);
            var snapshot=Machine.Capture();bool Under(string p)=>p==path||p.StartsWith(path=="/"?"/":path+"/",StringComparison.Ordinal);
            var values=(type=="f"?Enumerable.Empty<string>():snapshot.directories).Concat(type=="d"?Enumerable.Empty<string>():snapshot.files.Select(f=>f.path));
            return Ok(string.Join("\n",values.Where(p=>Under(p)&&Glob(Basename(p),pattern)).OrderBy(p=>p,StringComparer.Ordinal)));
        }
        public string PagerText
        {get{if(!Paging||!Machine.TryRead(PagerPath,out string text))return "";var lines=text.Replace("\r","").Split('\n');return string.Join("\n",lines.Skip(PagerLine).Take(18))+"\n\n"+PagerPath+" · "+(PagerLine+1)+"/"+lines.Length+" · Пробел дальше · Q закрыть";}}
        public void Page(int pages)
        {if(!Paging)return;Machine.TryRead(PagerPath,out var text);int last=Math.Max(0,(text??"").Replace("\r","").Split('\n').Length-1);PagerLine=Math.Max(0,Math.Min(last,PagerLine+pages*18));Changed?.Invoke();}
        public void ClosePager(){PagerPath="";PagerLine=0;Changed?.Invoke();}
        public string BrowseHistory(int direction,string draft)
        {
            var history=Machine.Terminal.History;if(historyCursor==history.Count)historyDraft=draft;historyCursor=Math.Max(0,Math.Min(history.Count,historyCursor+Math.Sign(direction)));return historyCursor==history.Count?historyDraft:history[historyCursor];
        }
        public ShellCompletion Complete(string input)
        {
            if(input==null||input.Length>4096)return new ShellCompletion(input??"",Array.Empty<string>());
            int start=0;char quote='\0';
            for(int i=0;i<input.Length;i++)
            {
                char c=input[i];
                if(quote!='\0'){if(c==quote)quote='\0';}
                else if(c=='\''||c=='"')quote=c;
                else if(char.IsWhiteSpace(c))start=i+1;
            }
            string token=input.Substring(start);char opener=token.Length>0&&(token[0]=='\''||token[0]=='"')?token[0]:'\0';
            bool quoted=opener!='\0';string raw=quoted?token.Substring(1):token;
            if(quoted&&raw.EndsWith(opener.ToString(),StringComparison.Ordinal))raw=raw.Substring(0,raw.Length-1);
            string[] matches;string prefix="";
            if(start==0)matches=commands.Where(c=>c.StartsWith(raw,StringComparison.Ordinal)).ToArray();
            else{int slash=raw.LastIndexOf('/');prefix=slash<0?"":raw.Substring(0,slash+1);string fragment=slash<0?raw:raw.Substring(slash+1);string parent=Resolve(prefix.Length==0?".":prefix);matches=Machine.IsDirectory(parent)?Machine.List(parent).Where(c=>c.StartsWith(fragment,StringComparison.Ordinal)).ToArray():Array.Empty<string>();}
            if(matches.Length==0)return new ShellCompletion(input,matches);string common=matches[0];foreach(string m in matches){int n=0;while(n<common.Length&&n<m.Length&&common[n]==m[n])n++;common=common.Substring(0,n);}
            string completed=prefix+common;
            if(!quoted&&completed.Any(char.IsWhiteSpace)){quoted=true;opener='"';}
            string close=quoted&&matches.Length==1&&!common.EndsWith("/",StringComparison.Ordinal)?opener.ToString():"";
            return new ShellCompletion(input.Substring(0,start)+(quoted?opener.ToString():"")+completed+close,matches);
        }
        public HomeShellData Capture()=>new(){pagerPath=PagerPath,pagerLine=PagerLine};
        public void Restore(HomeShellData data)
        {
            string path=data?.pagerPath??"";int line=data?.pagerLine??0;
            SaveValidation.Require(path.Length<=1024&&line>=0&&line<=65536,"Invalid pager state");
            if(path.Length>0){SaveValidation.Path(path);SaveValidation.Require(Machine.TryRead(path,out var text)&&line<text.Replace("\r","").Split('\n').Length,"Missing pager document");}
            else SaveValidation.Require(line==0,"Invalid closed pager");
            PagerPath=path;PagerLine=line;historyCursor=Machine.Terminal.History.Count;historyDraft=Machine.Terminal.Draft;
        }
        private string Resolve(string path)=>VirtualComputer.Resolve(Machine.WorkingDirectory,path);
        private static string Basename(string path)=>path=="/"?"/":path.Substring(path.LastIndexOf('/')+1);
        private static CommandResult Ok(string text)=>new(text.Length>8192?text.Substring(0,8150)+"\n[Вывод сокращён: используйте less]":text);
        private static CommandResult Error(string text)=>new(text,1);
        private static string Help(string command)=>command switch
        {
            ""=>"Домашняя оболочка\n"+string.Join("  ",commands)+"\nTab — дополнение, ↑/↓ — история. Все файлы принадлежат виртуальному компьютеру.",
            "ls"=>"ls [-l] [-a] [путь]\n-l — подробности, -a — скрытые имена. Пример: ls -la ~/archive",
            "cd"=>"cd [каталог]\n.. — выше, ~ — домашний каталог. Пример: cd archive",
            "pwd"=>"pwd\nПоказывает текущий абсолютный путь.","cat"=>"cat <файл> [файл...]\nПечатает содержимое. Пример: cat ~/archive/incident_01.log",
            "less"=>"less <файл>\nПостраничное чтение: Пробел дальше, Q закрыть.","grep"=>"grep [-i] [-n] <текст> <файл>\nПоиск буквального текста; -i без учёта регистра, -n номера строк.",
            "find"=>"find [путь] [-name шаблон] [-type f|d]\nШаблоны * и ?. Пример: find ~ -name '*.log'",
            "history"=>"history\nПоказывает последние команды. Стрелки возвращают их в строку ввода.","clear"=>"clear\nОчищает вывод, сохраняя историю.","help" or "man"=>"help [команда] или man <команда>\nСправка доступна без обучения.",_=>"Справка не найдена: "+command
        };
        private static List<string> Tokenize(string text)
        {
            var result=new List<string>();var token=new StringBuilder();char quote='\0';bool started=false;
            foreach(char c in text)
            {
                if(c=='\0'||c=='\r'||c=='\n')throw new ArgumentException("Недопустимый символ в команде");
                if(quote!='\0'){if(c==quote)quote='\0';else token.Append(c);started=true;}
                else if(c=='\''||c=='"'){quote=c;started=true;}
                else if("|><;&`$".IndexOf(c)>=0)throw new ArgumentException("Эта оболочка поддерживает только виртуальные команды чтения");
                else if(char.IsWhiteSpace(c)){if(started){result.Add(token.ToString());token.Clear();started=false;}}
                else{token.Append(c);started=true;}
                if(result.Count>128)throw new ArgumentException("Слишком много аргументов");
            }
            if(quote!='\0')throw new ArgumentException("Незакрытая кавычка");if(started)result.Add(token.ToString());return result;
        }
        internal static bool Glob(string value,string pattern)
        {
            int i=0,j=0,star=-1,matched=0;
            while(i<value.Length){if(j<pattern.Length&&(pattern[j]=='?'||pattern[j]==value[i])){i++;j++;}else if(j<pattern.Length&&pattern[j]=='*'){star=j++;matched=i;}else if(star>=0){j=star+1;i=++matched;}else return false;}
            while(j<pattern.Length&&pattern[j]=='*')j++;return j==pattern.Length;
        }
    }
}
