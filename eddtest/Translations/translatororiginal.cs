/*
 * Copyright © 2020-2024 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class TranslatorExtensions
{
    static public string TxID(this string s, Enum id)               // given english text and enumeration, translate
    {
        return BaseUtils.Translator.Instance.Translate(s, id.ToString().Replace("_", "."));
    }
    static public bool TxDefined(Enum id)                           // is it defined?
    {
        return BaseUtils.Translator.Instance.IsDefined(id.ToString().Replace("_", "."));
    }

    static public string TxID(this string s, Type type, string id)    // given english text, type for base name, and id for rest, translate
    {
        return BaseUtils.Translator.Instance.Translate(s, type.Name + "." + id);
    }

    static public string TxID(this string s, string name, string id)    // given english text, string for base name, and id for rest, translate
    {
        return BaseUtils.Translator.Instance.Translate(s, name + "." + id);
    }
}

namespace BaseUtils
{
    // specials : if text in a control = <code> its presumed its a code filled in entry and not suitable for translation
    // in translator file, .Label means use the previous first word prefix stored, for shortness
    // using Label: "English" @ means for debug, replace @ with <english> as the foreign word in the debug build. In release, just use the in-code text

    public class Translator
    {
        static public Translator Instance
        {
            get
            {
                if (instance == null)
                    instance = new Translator();
                return instance;
            }
        }

        static Translator instance;

        public bool OutputIDs { get; set; } = false;             // for debugging

        private LogToFile logger = null;

        private Dictionary<string, string> translations = null;         // translation id -> translation. Translation result can be null, which means, use the in-game english string
        public Dictionary<string, string> originalenglish = null;      // optional load - translation id -> english
        private Dictionary<string, string> originalfile = null;         // optional load - translation id -> file
        private Dictionary<string, int> originalline = null;            // optional load - translation id -> line
        private Dictionary<string, bool> inuse = null;                  // optional load - translation id -> use flag

        private List<Type> ExcludedControls = new List<Type>();

        public IEnumerable<string> EnumerateKeys { get { return translations.Keys; } }

        public Translator() // only use via debugging
        {
        }

        public bool CompareTranslatedToCode { get; set; } = false;      // if set, will moan if the translate does not match the code
        public bool Translating { get { return translations != null; } }

        public bool IsDefined(string fullid) => translations != null && translations.ContainsKey(fullid);
        public bool TryGetValue(string fullid, out string text)     // true if translation is defined and non null
        {
            text = translations != null && translations.ContainsKey(fullid) ? translations[fullid] : null;
            return text != null;
        }
        public string GetTranslation(string fullid) => translations[fullid];         // ensure its there first!
        public string GetOriginalEnglish(string fullid) => originalenglish?[fullid] ?? "?";         // ensure its there first!
        public string GetOriginalFile(string fullid) => originalfile?[fullid] ?? "?";         // ensure its there first!
        public int GetOriginalLine(string fullid) => originalline?[fullid] ?? -1;         // ensure its there first!
        public void UnDefine(string fullid) { translations.Remove(fullid); }        // debug
        public void ReDefine(string fullid, string newdefine) { translations[fullid] = newdefine; }        // for edtools
        public bool Rename(string from, string to, string file, int line = 0)
        {
            if (translations.ContainsKey(from))
            {
                translations[to] = translations[from];
                translations.Remove(from);
                if (originalenglish != null)
                {
                    originalenglish[to] = originalenglish[from];
                    originalfile[to] = file;
                    originalline[to] = line;
                    originalenglish.Remove(from);
                    originalfile.Remove(from);
                    originalline.Remove(from);
                }
                return true;
            }
            else
                return false;
        }
        public string FindTranslation(string text) { var txlist = translations.ToList(); var txpos = txlist.FindIndex(x => x.Value == text); return txpos >= 0 ? txlist[txpos].Key : null; }
        public string FindTranslation(string text, string primaryfile, string currentfile, bool rootonlynamesprimary)
        {
            var txlist = translations.ToList();
            foreach (var kvp in txlist)
            {
                if (kvp.Value == text)
                {
                    if (currentfile != primaryfile && originalfile[kvp.Key] == primaryfile && (!rootonlynamesprimary || !kvp.Key.Contains(".")))
                        return kvp.Key;
                    else if (originalfile[kvp.Key] == currentfile)
                        return kvp.Key;
                }
            }
            return null;
        }

        public string[] FileList()
        {
            return originalfile.Values.Distinct().ToArray();
        }

        public List<string> NotUsed()                                               // if track use on, whats not used
        {
            if (inuse != null)
            {
                List<string> res = new List<string>();
                foreach (var kvp in translations)
                {
                    if (!inuse.ContainsKey(kvp.Key))
                        res.Add(kvp.Key);
                }
                return res;
            }
            else
                return null;
        }


        // You can call this multiple times if required for debugging purposes
        public bool LoadTranslation(string language,
                                    CultureInfo uicurrent,
                                    string[] txfolders,
                                    int includesearchupdepth,
                                    string logdir = null,       // if non null, logger is active
                                    bool storesourceinfo = false,      // if true, accumulate info on where IDs come from
                                    string includefolderreject = "\\bin"       // use to reject include files in specific locations - for debugging
                                    )
        {
            if (logdir != null)
            {
                if (logger != null)
                    logger.Dispose();

                logger = new LogToFile();
                logger.SetFile(logdir, "translator-ids.log", false);
            }

            translations = null;        // forget any
            originalenglish = null;
            originalfile = null;
            originalline = null;
            inuse = null;

            List<Tuple<string, string>> languages = EnumerateLanguages(txfolders);

            //  uicurrent = CultureInfo.CreateSpecificCulture("it"); // debug

            Tuple<string, string> langsel = null;

            if (language == "Auto")
            {
                langsel = FindISOLanguage(languages, uicurrent.Name);

                if (langsel == null)
                    langsel = FindISOLanguage(languages, uicurrent.TwoLetterISOLanguageName);
            }
            else
            {
                langsel = languages.Find(x => Path.GetFileNameWithoutExtension(x.Item2).Equals(language, StringComparison.InvariantCultureIgnoreCase));
            }

            if (langsel == null)
                return false;

            System.Diagnostics.Debug.WriteLine("Load Language " + langsel.Item2);
            logger?.WriteLine("Read " + langsel.Item2 + " from " + langsel.Item1);

            string tlfile = Path.Combine(langsel.Item1, langsel.Item2);

            // tlx faster load

            if (Path.GetExtension(tlfile).Equals(".tlx", StringComparison.InvariantCultureIgnoreCase))
            {
                return ReadFromFile(tlfile);
            }

            using (LineReader lr = new LineReader())
            {
                if (lr.Open(tlfile))
                {
                    translations = new Dictionary<string, string>();
                    if (storesourceinfo)
                    {
                        originalenglish = new Dictionary<string, string>();
                        originalfile = new Dictionary<string, string>();
                        originalline = new Dictionary<string, int>();
                        inuse = new Dictionary<string, bool>();
                    }

                    string prefix = "";
                    int commentblankcount = 1;
                    bool lastwassection = false;

                    string line = null;
                    while ((line = lr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0 || line.StartsWith("//"))
                        {
                            if (storesourceinfo && !lastwassection)         // if comment, and we are not between a SECTION and an ID
                            {
                                string id = "COMMENTBLANK:" + commentblankcount++;      // record the information
                                translations[id] = null;
                                originalenglish[id] = line;
                                originalfile[id] = lr.CurrentFile;
                                originalline[id] = lr.CurrentLine;
                            }
                            lastwassection = false;
                        }
                        else if (line.StartsWith("SECTION ", StringComparison.InvariantCultureIgnoreCase))  // if a section..
                        {
                            StringParser s = new StringParser(line);
                            s.NextWord();
                            prefix = s.NextQuotedWord(" /");
                            lastwassection = true;      // flag used to indicate if next line is blank, not to store
                        }
                        else if (line.StartsWith("Include", StringComparison.InvariantCultureIgnoreCase))
                        {
                            lastwassection = false;
                            if (storesourceinfo)                            // we store these as comments so they can be spitted back out by translatenormalise
                            {
                                string id = "COMMENTBLANK:" + commentblankcount++;
                                translations[id] = null;
                                originalenglish[id] = line;
                                originalfile[id] = lr.CurrentFile;
                                originalline[id] = lr.CurrentLine;
                            }

                            line = line.Mid(7).Trim();

                            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(tlfile));

                            string filename = null;

                            string fileinroot = Path.Combine(di.FullName, line);

                            if (File.Exists(fileinroot))   // first we prefer files in the same folder..
                                filename = fileinroot;
                            else
                            {
                                di = di.GetDirectoryAbove(includesearchupdepth);        // then search the tree, first jump up search depth amount

                                try
                                {
                                    FileInfo[] allFiles = Directory.EnumerateFiles(di.FullName, line, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.LastWriteTime).ToArray();

                                    if (allFiles.Length > 0)
                                    {
                                        var selected = allFiles.Where((x) => !x.DirectoryName.Contains(includefolderreject));       // reject folders with this pattern..files
                                        if (selected.Count() > 0)
                                            filename = selected.First().FullName;
                                    }
                                }
                                catch { }
                            }

                            if (filename == null || !lr.Open(filename))     // if no file found, or can't open..
                            {
                                System.Diagnostics.Debug.WriteLine("*** Cannot include " + line);
                                logger?.WriteLine(string.Format("*** Cannot include {0}", line));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Reading file " + filename);
                                logger?.WriteLine("Readingfile " + filename);
                            }
                        }
                        else
                        {
                            lastwassection = false;

                            StringParser s = new StringParser(line);
                            string id = s.NextWord(" :");

                            if (id.StartsWith(".") && prefix.HasChars())
                                id = prefix + id;
                            else
                                prefix = id.Word(new char[] { '.' });

                            if (s.IsCharMoveOn(':'))
                            {
                                string orgenglish = s.NextQuotedWord(replaceescape: true);  // first is the original english version

                                string foreign = null;
                                bool err = false;

                                if (s.IsStringMoveOn("=>"))
                                {
                                    foreign = s.NextQuotedWord(replaceescape: true);
                                    err = foreign == null;
                                    if (err)
                                    {
                                        logger?.WriteLine(string.Format("*** Translator ID but no translation text {0}", id));
                                        System.Diagnostics.Debug.WriteLine("*** Translator ID but no translation text {0}", id);
                                    }
                                }
                                else if (s.IsCharMoveOn('@'))
                                    foreign = null;
                                else if (s.IsCharMoveOn('='))
                                {
                                    string keyword = s.NextWord();
                                    if (translations.ContainsKey(keyword))
                                    {
                                        foreign = translations[keyword];
                                    }
                                    else
                                    {
                                        logger?.WriteLine(string.Format("*** Translator ID with reference = but no reference found {0}", id));
                                        System.Diagnostics.Debug.WriteLine("*** Translator ID reference = but no reference found {0}", id);
                                        err = true;
                                    }

                                }
                                else
                                {
                                    logger?.WriteLine(string.Format("*** Translator ID bad format {0}", id));
                                    System.Diagnostics.Debug.WriteLine("*** Translator ID bad format {0}", id);
                                    err = true;
                                }

                                if (err != true)
                                {
                                    if (!translations.ContainsKey(id))
                                    {
                                        //                                            System.Diagnostics.Debug.WriteLine($"{lr.CurrentFile} {lr.CurrentLine} {id} => {orgenglish} => {foreign} ");
                                        if (logger != null)
                                            logger?.WriteLine(string.Format("New {0}: \"{1}\" => \"{2}\"", id, orgenglish, foreign));

                                        translations[id] = foreign;
                                        if (storesourceinfo)
                                        {
                                            originalenglish[id] = orgenglish;
                                            originalfile[id] = lr.CurrentFile;
                                            originalline[id] = lr.CurrentLine;
                                        }
                                    }
                                    else
                                    {
                                        string errt = $"Translator Repeat {lr.CurrentFile}:{lr.CurrentLine} '{id}";
                                        logger?.WriteLine(errt);
                                        System.Diagnostics.Debug.WriteLine(errt);
                                    }
                                }
                            }
                            else
                            {
                                string errt = $"Line misformat {lr.CurrentFile}:{lr.CurrentLine} '{line}";
                                logger?.WriteLine(errt);
                                System.Diagnostics.Debug.WriteLine(errt);
                            }
                        }
                    }

                    return true;
                }
                else
                    return false;
            }
        }

        // write keys to a file - much faster to load than normal version
        public bool WriteToFile(string filename)
        {
            StringBuilder sp = new StringBuilder(200000);
            foreach (var v in translations)
            {
                sp.Append(v.Key);
                sp.Append(":");
                if (v.Value != null)
                    sp.Append(v.Value);
                else
                { }
                sp.Append('\0');
            }

            return FileHelpers.TryWriteToFile(filename, sp.ToString());
        }

        // faster load
        public bool ReadFromFile(string filename)
        {
            translations = new Dictionary<string, string>();
            string st = FileHelpers.TryReadAllTextFromFile(filename);
            if (st != null)
            {
                StringParser sp = new StringParser(st);
                while (!sp.IsEOL)
                {
                    string key = sp.NextWord(':');
                    sp.MoveOn(1);
                    string value = sp.NextWord('\0');
                    sp.MoveOn(1);
                    if (value.Length > 0)
                        translations[key] = value;
                    else
                        translations[key] = null;
                }
                return true;
            }
            return false;
        }


        public void AddExcludedControls(Type[] s)
        {
            ExcludedControls.AddRange(s);
        }

        static public List<Tuple<string, string>> EnumerateLanguages(string[] txfolders)        // return folder, language file, without repeats
        {
            List<Tuple<string, string>> languages = new List<Tuple<string, string>>();

            foreach (string folder in txfolders)
            {
                try
                {
                    List<FileInfo> allFiles = Directory.EnumerateFiles(folder, "*.tlf", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.LastWriteTime).ToList();
                    allFiles.AddRange(Directory.EnumerateFiles(folder, "*.tlx", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.LastWriteTime).ToList());
                    System.Diagnostics.Debug.WriteLine("Translator Check folder " + folder);
                    foreach (FileInfo f in allFiles)
                    {
#if !DEBUG
                        if ( f.Name.Contains("example-ex"))
                            continue;
#endif

                        if (languages.Find(x => x.Item2.Equals(f.Name)) == null)        // if not already found this language, add
                            languages.Add(new Tuple<string, string>(folder, f.Name));   // folder and name
                    }
                }
                catch { }
            }

            return languages;
        }

        static public List<string> EnumerateLanguageNames(string[] txfolders)
        {
            List<Tuple<string, string>> languages = BaseUtils.Translator.EnumerateLanguages(txfolders);
            return (from x in languages select Path.GetFileNameWithoutExtension(x.Item2)).ToList();
        }

        static public Tuple<string, string> FindISOLanguage(List<Tuple<string, string>> lang, string isoname)    // take filename part only, see if filename text-<iso> is present
        {
            return lang.Find(x =>
            {
                string filename = Path.GetFileNameWithoutExtension(x.Item2);
                int dash = filename.IndexOf('-');
                return dash != -1 && filename.Substring(dash + 1).Equals(isoname);
            });
        }

        // Translate string. Normal is the english text, ID is the ID to look up.
        // if translator is off english is returned.
        // any <code> items are returned as is.

        public string Translate(string english, string id)
        {
            if (id == "DataGridView.WordWrap")
            {

            }
            if (translations != null && !english.StartsWith("<code"))
            {
                string key = id;
                if (OutputIDs)
                {
                    string tx = "ID lookup " + key + " Value " + (translations.ContainsKey(key) ? (translations[key] ?? "Null") : "Missing");
                    System.Diagnostics.Debug.WriteLine(tx);
                    logger?.WriteLine(tx);
                }

                if (translations.ContainsKey(key))
                {
                    if (inuse != null)
                        inuse[key] = true;


                    if (CompareTranslatedToCode && originalenglish != null && originalenglish.ContainsKey(key) && originalenglish[key] != english)
                    {
                        var orgeng = originalenglish[key];
                        logger?.WriteLine($"Difference Key {key} code `{english}` translation `{orgeng}`");
                    }

#if DEBUG
                    return translations[key] ?? english.QuoteFirstAlphaDigit();     // debug more we quote them to show its not translated, else in release we just print
#else
                    return translations[key] ?? english;
#endif
                }
                else
                {
                    logger?.WriteLine($"{id}: {english.EscapeControlChars().AlwaysQuoteString()} @");
                    english = "! " + english + " !";          // no id at all, use ! to indicate
                    translations.Add(key, english);
                    System.Diagnostics.Trace.WriteLine($"*** Missing Translate ID: {id}: {english.EscapeControlChars().AlwaysQuoteString()} @");
                    return english;
                }
            }
            else
                return english;
        }


    }
}
