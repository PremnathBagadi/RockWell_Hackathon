using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using NHunspell;

namespace ROCKWELL_WIN
{
    enum  Sentiment
    {
        Positive,
        Negative,
        Neutral
    }
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }
        Hunspell hunspell = null;
        SQLServerSoundEx sqlServerSoundEX = null;
        ConcurrentBag<string> feedBacks = null;
        ConcurrentBag<string> outputFeedBacks = null;
        Dictionary<int, Dictionary<string, ConcurrentBag<string>>> dictMasterBagNegative = null;        
        Dictionary<int, Dictionary<string, ConcurrentBag<string>>> dictMasterBagPositive = null;
        ConcurrentBag<string> soundexNegative = null;
        ConcurrentBag<string> soundexPositive = null;
        ConcurrentBag<string> unwantedWords = null;

        string buffer = string.Empty;
        string[] bufferSplit = null;
               

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    txtFilePath.Text = ofd.FileName;
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtFilePath.Text.Trim()))
                {
                    MessageBox.Show("Invalid Path");
                    return;
                }
                buffer = File.ReadAllText(txtFilePath.Text).Trim();
                bufferSplit = buffer.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                if(bufferSplit.LongLength<=0)
                {
                    MessageBox.Show("No data in file, please try with valid data");
                    return;
                }
                else if(bufferSplit.LongLength==1)
                {
                    if(bufferSplit[0]==string.Empty)
                    {
                        MessageBox.Show("No data in file, please try with valid data");
                        return;
                    }
                }
                feedBacks = new ConcurrentBag<string>();
                foreach(string custFeedbackItem in bufferSplit)
                {
                    feedBacks.Add(custFeedbackItem);
                }
                lblStatus.Text = "Feedback loaded successfully";
                
            }
            catch(Exception ex)
            {
                lblStatus.Text = "Error occured while loading feedback";
                MessageBox.Show(ex.Message);
            }
            
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                LoadEngine();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadUnwantedWords()
        {
            unwantedWords = new ConcurrentBag<string>();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("UNWANTED.xml");
            string temp = string.Empty;

            foreach (XmlNode xe in xDoc.SelectNodes("/words/word"))
            {
                temp = string.Empty;
                temp = xe.InnerText.Trim();
                if (!string.IsNullOrEmpty(temp))
                {
                    if (!unwantedWords.Contains(temp))
                    {
                        unwantedWords.Add(temp);
                    }
                }
            }

        }
        private void LoadEngine()
        {
            hunspell= new Hunspell("en_us.aff", "en_us.dic");
            sqlServerSoundEX = new SQLServerSoundEx();
            LoadUnwantedWords();
            LoadNegativeWords();
            LoadPositiveWords();
            LoadSoundexNegative();
            LoadSoundexPositive();

        }

        private void LoadSoundexNegative()
        {
            soundexNegative = new ConcurrentBag<string>();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("SOUNDEXNEGATIVE.xml");
            string temp = string.Empty;
            
            foreach (XmlNode xe in xDoc.SelectNodes("/words/word"))
            {
                temp = string.Empty;
                temp = xe.InnerText.Trim();
                soundexNegative.Add(temp);
            }
        }

        private void LoadSoundexPositive()
        {
            soundexPositive = new ConcurrentBag<string>();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("SOUNDEXPOSITIVE.xml");
            string temp = string.Empty;

            foreach (XmlNode xe in xDoc.SelectNodes("/words/word"))
            {
                temp = string.Empty;
                temp = xe.InnerText.Trim();
                soundexPositive.Add(temp);
            }
        }
        private void LoadNegativeWords()
        {
            dictMasterBagNegative = new Dictionary<int, Dictionary<string, ConcurrentBag<string>>>();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("NEGATIVE.xml");
            string temp = string.Empty;
            string tempStartEndString = string.Empty;
            int len = 0;
            ConcurrentBag<string> tempConcurrentBag = null;
            Dictionary<string, ConcurrentBag<string>> tempStartEndBag = null;
            
            foreach (XmlNode xe in xDoc.SelectNodes("/words/word"))
            {
                temp = string.Empty;
                len = 0;
                temp = xe.InnerText.Trim();
                len = temp.Length;

                if (len >= 3)
                {
                    
                    tempStartEndString = temp[0].ToString() + temp[len - 1].ToString();
                    if (dictMasterBagNegative.ContainsKey(len))
                    {
                        tempStartEndBag = dictMasterBagNegative[len];
                        if (tempStartEndBag.ContainsKey(tempStartEndString))
                        {
                            tempConcurrentBag = tempStartEndBag[tempStartEndString];
                            if (!tempConcurrentBag.Contains(temp))
                            {
                                tempConcurrentBag.Add(temp);
                            }
                        }
                        else
                        {
                            tempConcurrentBag = new ConcurrentBag<string>();
                            tempConcurrentBag.Add(temp);
                            tempStartEndBag.Add(tempStartEndString, tempConcurrentBag);
                        }
                    }
                    else
                    {

                        tempStartEndBag = new Dictionary<string, ConcurrentBag<string>>();
                        tempConcurrentBag = new ConcurrentBag<string>();
                        tempConcurrentBag.Add(temp);
                        tempStartEndBag.Add(tempStartEndString, tempConcurrentBag);
                        dictMasterBagNegative.Add(len, tempStartEndBag);
                    }
                }

            }
        }

        private void LoadPositiveWords()
        {
            
            dictMasterBagPositive = new Dictionary<int, Dictionary<string, ConcurrentBag<string>>>();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("POSITIVE.xml");
            string temp = string.Empty;
            string tempStartEndString = string.Empty;
            int len = 0;
            ConcurrentBag<string> tempConcurrentBag = null;
            Dictionary<string, ConcurrentBag<string>> tempStartEndBag = null;
            foreach (XmlNode xe in xDoc.SelectNodes("/words/word"))
            {
                temp = string.Empty;
                len = 0;
                temp = xe.InnerText.Trim();
                len = temp.Length;

                if (len >= 3)
                {
                    tempStartEndString = temp[0].ToString() + temp[len - 1].ToString();
                    if (dictMasterBagPositive.ContainsKey(len))
                    {
                        tempStartEndBag = dictMasterBagPositive[len];
                        if (tempStartEndBag.ContainsKey(tempStartEndString))
                        {
                            tempConcurrentBag = tempStartEndBag[tempStartEndString];
                            if (!tempConcurrentBag.Contains(temp))
                            {
                                tempConcurrentBag.Add(temp);
                            }
                        }
                        else
                        {
                            tempConcurrentBag = new ConcurrentBag<string>();
                            tempConcurrentBag.Add(temp);
                            tempStartEndBag.Add(tempStartEndString, tempConcurrentBag);
                        }
                    }
                    else
                    {

                        tempStartEndBag = new Dictionary<string, ConcurrentBag<string>>();
                        tempConcurrentBag = new ConcurrentBag<string>();
                        tempConcurrentBag.Add(temp);
                        tempStartEndBag.Add(tempStartEndString, tempConcurrentBag);
                        dictMasterBagPositive.Add(len, tempStartEndBag);
                    }
                }

            }
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            try
            {
                string[] tempWords = null;
                int negativeCount = 0;
                int positiveCount = 0;
                Sentiment sentiment = Sentiment.Neutral;
                string tempWord = string.Empty;
                string searchString = string.Empty;
                outputFeedBacks = new ConcurrentBag<string>();
                foreach (string feedBack in feedBacks)
                {
                    tempWords = feedBack.Split(' ');
                    sentiment = Sentiment.Neutral;
                    negativeCount = 0;
                    positiveCount = 0;
                    foreach (string tempBufferWord in tempWords)
                    {

                        tempWord = tempBufferWord.Trim();
                        tempWord = tempWord.ToLower();
                        if (tempWord.Length >= 3)
                        {
                            if (!unwantedWords.Contains(tempWord))
                            {
                                if (IsNegativeWord(tempWord))
                                {
                                    negativeCount++;
                                }
                                else if (IsPositiveWord(tempWord))
                                {
                                    positiveCount++;
                                }
                                else if (IsNegativeWordReCheck(tempWord))
                                {
                                    negativeCount++;
                                }
                                else if (IsPositiveWordReCheck(tempWord))
                                {
                                    positiveCount++;
                                }
                                else if (IsNegativeSound(tempWord))
                                {
                                    negativeCount++;
                                }
                                else if (IsPositivSound(tempWord))
                                {
                                    positiveCount++;
                                }

                            }
                        }
                    }
                    if (negativeCount > 0)
                    {
                        sentiment = Sentiment.Negative;
                    }
                    else if (positiveCount > 0)
                    {
                        sentiment = Sentiment.Positive;
                    }
                    else
                    {
                        sentiment = Sentiment.Neutral;
                    }
                    outputFeedBacks.Add(sentiment.ToString());
                }
                File.WriteAllLines("output.txt", outputFeedBacks);
                lblStatus.Text = "Successfully scanned";
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private bool IsNegativeSound(string searchWord)
        {
            bool IsNegativeSound = false;
            string temp = RemoveSpecialCharacters(searchWord);
            string[] tempWords = temp.Split(' ');
            foreach (string tempSoundWord in tempWords)
            {
                if (!hunspell.Spell(tempSoundWord))
                {
                    foreach (string tempStr in soundexNegative)
                    {
                        if (sqlServerSoundEX.GenerateSoundEx(tempSoundWord) == sqlServerSoundEX.GenerateSoundEx(tempStr))
                        {
                            IsNegativeSound = true;
                            break;
                        }
                    }
                }
            }
           
            
            return IsNegativeSound;
        }

        private bool IsPositivSound(string searchWord)
        {
            bool IsPositiveSound = false;
            string temp = RemoveSpecialCharacters(searchWord);
            string[] tempWords = temp.Split(' ');
            
            foreach(string tempSoundWord in tempWords)
            {
                if (!hunspell.Spell(tempSoundWord))
                {
                    foreach (string tempStr in soundexPositive)
                    {
                        if (sqlServerSoundEX.GenerateSoundEx(tempSoundWord) == sqlServerSoundEX.GenerateSoundEx(tempStr))
                        {
                            IsPositiveSound = true;
                            break;
                        }
                    }
                }
            }
            

            return IsPositiveSound;
        }
        private bool IsNegativeWord(string searchWord)
        {
            bool IsNegative = false;
            string temp = searchWord.Trim();
            if(!string.IsNullOrEmpty(temp))
            {
                int tempLength = temp.Length;
                string tempStartEndString = temp[0].ToString() + temp[tempLength - 1].ToString();
                if (dictMasterBagNegative.ContainsKey(tempLength))
                {
                    Dictionary<string, ConcurrentBag<string>> tempStartEndBag = dictMasterBagNegative[tempLength];
                    if (tempStartEndBag.ContainsKey(tempStartEndString))
                    {
                        ConcurrentBag<string> tempConcurrentBag = tempStartEndBag[tempStartEndString];
                        if (tempConcurrentBag.Contains(temp))
                        {
                            IsNegative = true;
                        }
                    }
                }
            }
            
            return IsNegative;
        }

        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }
        private bool IsNegativeWordReCheck(string searchWord)
        {
            bool IsNegative = false;
            string temp = RemoveSpecialCharacters(searchWord);
            string[] tempWords = temp.Split(' ');
            int negativeCount = 0;
            foreach(string word in tempWords)
            {
                if(IsNegativeWord(word))
                {
                    negativeCount++;
                }
            }
            if(negativeCount>0)
            {
                IsNegative = true;
            }
            return IsNegative;
        }

        private bool IsPositiveWordReCheck(string searchWord)
        {
            bool IsPositive = false;
            string temp = RemoveSpecialCharacters(searchWord);
            string[] tempWords = temp.Split(' ');
            int positiveCount = 0;
            foreach (string word in tempWords)
            {
                if (IsPositiveWord(word))
                {
                    positiveCount++;
                }
            }
            if (positiveCount > 0)
            {
                IsPositive = true;
            }
            return IsPositive;
        }

        private bool IsPositiveWord(string searchWord)
        {
            bool IsPositive = false;
            string temp = searchWord.Trim();
            if(!string.IsNullOrEmpty(temp))
            {
                int tempLength = temp.Length;
                string tempStartEndString = temp[0].ToString() + temp[tempLength - 1].ToString();
                if (dictMasterBagPositive.ContainsKey(tempLength))
                {
                    Dictionary<string, ConcurrentBag<string>> tempStartEndBag = dictMasterBagPositive[tempLength];
                    if (tempStartEndBag.ContainsKey(tempStartEndString))
                    {
                        ConcurrentBag<string> tempConcurrentBag = tempStartEndBag[tempStartEndString];
                        if (tempConcurrentBag.Contains(temp))
                        {
                            IsPositive = true;
                        }
                    }
                }
            }
            return IsPositive;
        }
        

        private bool IgnoreWord(string searchString)
        {
            return false;
            
        }

        public string soundex(string word)
        {
            const int MaxSoundexCodeLength = 4;

            var soundexCode = new StringBuilder();
            var previousWasHOrW = false;

            word = Regex.Replace(
                word == null ? string.Empty : word.ToUpper(),
                    @"[^\w\s]",
                        string.Empty);

            if (string.IsNullOrEmpty(word))
                return string.Empty.PadRight(MaxSoundexCodeLength, '0');

            soundexCode.Append(word.First());

            for (var i = 1; i < word.Length; i++)
            {
                var numberCharForCurrentLetter =
                    GetCharNumberForLetter(word[i]);

                if (i == 1 &&
                        numberCharForCurrentLetter ==
                            GetCharNumberForLetter(soundexCode[0]))
                    continue;

                if (soundexCode.Length > 2 && previousWasHOrW &&
                        numberCharForCurrentLetter ==
                            soundexCode[soundexCode.Length - 2])
                    continue;

                if (soundexCode.Length > 0 &&
                        numberCharForCurrentLetter ==
                            soundexCode[soundexCode.Length - 1])
                    continue;

                soundexCode.Append(numberCharForCurrentLetter);

                previousWasHOrW = "HW".Contains(word[i]);
            }

            return soundexCode
                    .Replace("0", string.Empty)
                        .ToString()
                            .PadRight(MaxSoundexCodeLength, '0')
                                .Substring(0, MaxSoundexCodeLength);
        }

        private char GetCharNumberForLetter(char letter)
        {
            if ("BFPV".Contains(letter)) return '1';
            if ("CGJKQSXZ".Contains(letter)) return '2';
            if ("DT".Contains(letter)) return '3';
            if ('L' == letter) return '4';
            if ("MN".Contains(letter)) return '5';
            if ('R' == letter) return '6';

            return '0';
        }
    }
}
