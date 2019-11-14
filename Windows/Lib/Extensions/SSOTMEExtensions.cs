/*****************************
Project:    SSoTme - Open Source Tools (OST)
Created By: EJ Alexandra - 2016
            An Abstract Level, llc
License:    Mozilla Public License 2.0
*****************************/
using ExcelDataReader;
using HtmlAgilityPack;
using Newtonsoft.Json;
using PluralizationService;
using PluralizationService.English;
using SassyMQ.Lib.RabbitMQ;
using SSoTme.OST.Lib.DataClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SSoTme.OST.Lib.Extensions
{
    public static class SSOTMEExtensions
    {
        public static readonly IPluralizationApi Pluralizer;
        private static readonly CultureInfo CultureInfo;

        static SSOTMEExtensions()
        {

            var builder = new PluralizationApiBuilder();
            builder.AddEnglishProvider();

            Pluralizer = builder.Build();
            CultureInfo = new CultureInfo("en-US");
        }


        public static String XlsxToXml(this byte[] xlsx, String inputFileName)
        {

            var csvFileSet = xlsx.XslxToCsvFileSet(inputFileName, true);

            // - Convert them into one big file
            var xmlSB = new StringBuilder();
            var rootNode = Path.GetFileNameWithoutExtension(inputFileName);
            xmlSB.AppendFormat("<{0}>", rootNode);
            foreach (var csv in csvFileSet.FileSetFiles)
            {
                var csvXML = csv.GetFileSetFileContents().CsvToXml(csv.RelativePath);
                xmlSB.Append(csvXML);
            }
            xmlSB.AppendFormat("</{0}>", rootNode);

            var xml = xmlSB.ToString();

            return xml;
        }

        public static String ToFriendlyFullTypeName(this Type type)
        {
            return type.Name.SafeToString().ToTitle();
        }

        public static FileSet ToFileSet(this string fileSetXml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(FileSet));
            fileSetXml = fileSetXml.Substring(fileSetXml.IndexOf("<"));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fileSetXml);
            var fs = new FileSet();
            foreach (var fsfNode in doc.SelectNodes("//FileSetFile").OfType<XmlElement>())
            {
                var fsf = new FileSetFile();
                fs.FileSetFiles.Add(fsf);
                var xmlNode = fsfNode.SelectSingleNode("AlwaysOverwrite");
                if (!ReferenceEquals(xmlNode, null) && (xmlNode.InnerText == "true")) fsf.AlwaysOverwrite = true;

                xmlNode = fsfNode.SelectSingleNode("OverwriteMode");
                if (ReferenceEquals(xmlNode, null) || (xmlNode.InnerText != "Never")) fsf.AlwaysOverwrite = true;

                xmlNode = fsfNode.SelectSingleNode("RelativePath");
                if (!ReferenceEquals(xmlNode, null)) fsf.RelativePath = xmlNode.InnerText;

                xmlNode = fsfNode.SelectSingleNode("FileContents");
                if (!ReferenceEquals(xmlNode, null)) fsf.FileContents = HttpUtility.HtmlDecode(xmlNode.InnerXml);

                xmlNode = fsfNode.SelectSingleNode("SkipClean");
                if (!ReferenceEquals(xmlNode, null)) fsf.SkipClean = (xmlNode.InnerText == "true");

                xmlNode = fsfNode.SelectSingleNode("ZippedFileContents");
                if (!ReferenceEquals(xmlNode, null)) fsf.ZippedFileContents = Convert.FromBase64String(xmlNode.InnerXml);

                xmlNode = fsfNode.SelectSingleNode("BinaryFileContents");
                if (!ReferenceEquals(xmlNode, null)) fsf.BinaryFileContents = Convert.FromBase64String(xmlNode.InnerXml);

                xmlNode = fsfNode.SelectSingleNode("ZippedBinaryFileContents");
                if (!ReferenceEquals(xmlNode, null)) fsf.ZippedBinaryFileContents = Convert.FromBase64String(xmlNode.InnerXml);
            }
            return fs;
        }


        public static String ToName(this string nameCandidate)
        {
            string name = nameCandidate.SafeToString();
            name = name.ToCamelString();
            return name;
        }

        public static FileSet GetTranspilersAsJson(string screenName)
        {
            var fs = new FileSet();
            var fsf = fs.FileSetFiles.AddNew();
            fsf.RelativePath = String.Format("{0}-transpilers.json", screenName);
            fsf.AlwaysOverwrite = true;
            var wc = new WebClient();
            var myPublicTranspilersUrl = String.Format("http://ssot.me/Public/GetPublicAccounts?screenName={0}", screenName);
            var myTranspilersJson = wc.DownloadString(myPublicTranspilersUrl);
            fsf.FileContents = myTranspilersJson;
            return fs;
        }

        public static string ToTitle(this string name)
        {
            return name.ToCamelString().TitleFromCamel();
        }

        public static String ToXml(this FileSet fileSet)
        {
            var ser = new XmlSerializer(typeof(FileSet));
            var ms = new MemoryStream();
            ser.Serialize(ms, fileSet);
            ms.Position = 0;
            var fileSetXml = new StreamReader(ms).ReadToEnd();
            return fileSetXml;
        }

        public static String HtmlToText(this String html, String nodeId = "", String stopNodeId = "")
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.HtmlToText(nodeId, stopNodeId);
        }


        #region https://stackoverflow.com/questions/731649/how-can-i-convert-html-to-text-in-c
        // FROM https://stackoverflow.com/questions/731649/how-can-i-convert-html-to-text-in-c
        public static string HtmlToText(this HtmlDocument doc, String nodeId = "", String stopNodeId = "")
        {
            using (StringWriter sw = new StringWriter())
            {
                var node = doc.DocumentNode;

                if (!String.IsNullOrEmpty(nodeId))
                {
                    var rootNode = doc.DocumentNode.SelectSingleNode(String.Format("//*[normalize-space(@id) = '{0}']", nodeId));
                    if (!ReferenceEquals(rootNode, null) && !String.IsNullOrEmpty(stopNodeId))
                    {
                        node = rootNode;
                        var firstStopChild = node.SelectSingleNode(String.Format(".//*[normalize-space(@id) = '{0}']", stopNodeId));
                        if (!ReferenceEquals(firstStopChild, null))
                        {
                            while (!ReferenceEquals(firstStopChild.NextSibling, null))
                            {
                                firstStopChild.ParentNode.ChildNodes.Remove(firstStopChild.NextSibling);
                            }
                        }

                        var catlinks = node.SelectSingleNode(".//*[normalize-space(@id) = 'catlinks']");
                        if (!ReferenceEquals(catlinks, null)) catlinks.Remove();
                    }
                }

                ConvertTo(node, sw);
                sw.Flush();
                return sw.ToString();
            }
        }

        internal static void ConvertContentTo(HtmlNode node, TextWriter outText, PreceedingDomTextInfo textInfo)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText, textInfo);
            }
        }
        public static void ConvertTo(HtmlNode node, TextWriter outText)
        {
            ConvertTo(node, outText, new PreceedingDomTextInfo(false));
        }
        internal static void ConvertTo(HtmlNode node, TextWriter outText, PreceedingDomTextInfo textInfo)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;
                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText, textInfo);
                    break;
                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                    {
                        break;
                    }
                    // get text
                    html = ((HtmlTextNode)node).Text;
                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                    {
                        break;
                    }
                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Length == 0)
                    {
                        break;
                    }
                    if (!textInfo.WritePrecedingWhiteSpace || textInfo.LastCharWasSpace)
                    {
                        html = html.TrimStart();
                        if (html.Length == 0) { break; }
                        textInfo.IsFirstTextOfDocWritten.Value = textInfo.WritePrecedingWhiteSpace = true;
                    }
                    outText.Write(HtmlEntity.DeEntitize(Regex.Replace(html.TrimEnd(), @"\s{2,}", " ")));
                    if (textInfo.LastCharWasSpace = char.IsWhiteSpace(html[html.Length - 1]))
                    {
                        outText.Write(' ');
                    }
                    break;
                case HtmlNodeType.Element:
                    string endElementString = null;
                    bool isInline;
                    bool skip = false;
                    int listIndex = 0;
                    switch (node.Name)
                    {
                        case "nav":
                            skip = true;
                            isInline = false;
                            break;
                        case "body":
                        case "section":
                        case "article":
                        case "aside":
                        case "h1":
                        case "h2":
                        case "header":
                        case "footer":
                        case "address":
                        case "main":
                        case "div":
                        case "p": // stylistic - adjust as you tend to use
                            if (textInfo.IsFirstTextOfDocWritten)
                            {
                                outText.Write("\r\n");
                            }
                            endElementString = "\r\n";
                            isInline = false;
                            break;
                        case "br":
                            outText.Write("\r\n");
                            skip = true;
                            textInfo.WritePrecedingWhiteSpace = false;
                            isInline = true;
                            break;
                        case "a":
                            if (node.Attributes.Contains("href"))
                            {
                                string href = node.Attributes["href"].Value.Trim();
                                if (node.InnerText.IndexOf(href, StringComparison.InvariantCultureIgnoreCase) == -1)
                                {
                                    endElementString = "<" + href + ">";
                                }
                            }
                            isInline = true;
                            break;
                        case "li":
                            if (textInfo.ListIndex > 0)
                            {
                                outText.Write("\r\n{0}.\t", textInfo.ListIndex++);
                            }
                            else
                            {
                                outText.Write("\r\n*\t"); //using '*' as bullet char, with tab after, but whatever you want eg "\t->", if utf-8 0x2022
                            }
                            isInline = false;
                            break;
                        case "ol":
                            listIndex = 1;
                            goto case "ul";
                        case "ul": //not handling nested lists any differently at this stage - that is getting close to rendering problems
                            endElementString = "\r\n";
                            isInline = false;
                            break;
                        case "img": //inline-block in reality
                            if (node.Attributes.Contains("alt"))
                            {
                                outText.Write('[' + node.Attributes["alt"].Value);
                                endElementString = "]";
                            }
                            if (node.Attributes.Contains("src"))
                            {
                                outText.Write('<' + node.Attributes["src"].Value + '>');
                            }
                            isInline = true;
                            break;
                        default:
                            isInline = true;
                            break;
                    }
                    if (!skip && node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText, isInline ? textInfo : new PreceedingDomTextInfo(textInfo.IsFirstTextOfDocWritten) { ListIndex = listIndex });
                    }
                    if (endElementString != null)
                    {
                        outText.Write(endElementString);
                    }
                    break;
            }

        }
        internal class PreceedingDomTextInfo
        {
            public PreceedingDomTextInfo(BoolWrapper isFirstTextOfDocWritten)
            {
                IsFirstTextOfDocWritten = isFirstTextOfDocWritten;
            }
            public bool WritePrecedingWhiteSpace { get; set; }
            public bool LastCharWasSpace { get; set; }
            public readonly BoolWrapper IsFirstTextOfDocWritten;
            public int ListIndex { get; set; }
        }
        internal class BoolWrapper
        {
            public BoolWrapper() { }
            public bool Value { get; set; }
            public static implicit operator bool(BoolWrapper boolWrapper)
            {
                return boolWrapper.Value;
            }
            public static implicit operator BoolWrapper(bool boolWrapper)
            {
                return new BoolWrapper { Value = boolWrapper };
            }
        }
        #endregion

        public static void WriteTo(this FileSet fs, DirectoryInfo rootDirInfo)
        {
            fs.ToXml().SplitFileSetXml(false, rootDirInfo.FullName);
        }


        public static void CleanZippedFileSet(this byte[] zippedFileSet)
        {
            zippedFileSet.UnzipToString().CleanFileSet();
        }

        public static void CleanFileSet(this string fileSetXml)
        {

            if (!String.IsNullOrEmpty(fileSetXml) && fileSetXml.Contains("<"))
            {
                fileSetXml = fileSetXml.Substring(fileSetXml.IndexOf("<"));
                fileSetXml = fileSetXml.Replace("FileContents><?xml ", "FileContents>&lt;?xml");
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.LoadXml(fileSetXml);
                }
                catch { } // Ignore errors cleaning previous files up.  Sometimes this won't work.
                foreach (XmlElement fileSetFileElem in doc.SelectNodes("//FileSetFile"))
                {
                    // Delete the file if it matches the contents
                    XmlNode relPathElem = fileSetFileElem.SelectSingleNode("RelativePath");
                    if (!ReferenceEquals(relPathElem, null))
                    {
                        CleanFileByRelativeName(fileSetFileElem, relPathElem);
                    }
                }
                CleanEmptyFolders();
            }
        }

        private static void CleanEmptyFolders()
        {
            new DirectoryInfo(".").CleanEmptyFolders();
        }

        private static void CleanEmptyFolders(this DirectoryInfo di)
        {
            if (di.Exists)
            {
                foreach (DirectoryInfo diChildDir in di.GetDirectories())
                {
                    CleanEmptyFolders(diChildDir);
                }
                if (!di.GetDirectories().Any() && !di.GetFiles().Any())
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(150);
                        try
                        {
                            di.Delete();
                        }
                        catch
                        {
                            Thread.Sleep(2500);
                            try
                            {
                                di.Delete();
                            }
                            catch { } // ignore errors
                        }
                    });
                }
            }
        }

        private static void CleanFileByRelativeName(XmlElement fileSetFileElem, XmlNode relPathElem)
        {
            var skipElement = fileSetFileElem.SelectSingleNode(".//SkipClean");
            if (!ReferenceEquals(skipElement, null) && String.Equals(skipElement.InnerText, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            else
            {
                FileInfo fiToClean = new FileInfo(GetFullFileName(relPathElem.InnerText, new DirectoryInfo(".")));
                if (fiToClean.Exists)
                {
                    bool neverOverwrite = true;

                    XmlNode aoNode = fileSetFileElem.SelectSingleNode("AlwaysOverwrite");
                    if (!ReferenceEquals(aoNode, null))
                    {
                        if (aoNode.InnerText == "true") neverOverwrite = false;
                    }
                    else
                    {
                        XmlNode omNode = fileSetFileElem.SelectSingleNode("OverwriteMode");
                        if (!ReferenceEquals(omNode, null) && (omNode.InnerText != "Never"))
                        {
                            neverOverwrite = false;
                        }
                    }

                    XmlNode fileContentsNode = fileSetFileElem.SelectSingleNode("FileContents");
                    XmlNode zippedFileContents = fileSetFileElem.SelectSingleNode("ZippedTextFileContents");
                    XmlNode binaryFileContentsNode = fileSetFileElem.SelectSingleNode("BinaryFileContents");
                    byte[] data = new byte[] { };
                    bool binaryEquals = false;
                    if (!ReferenceEquals(binaryFileContentsNode, null))
                    {
                        data = Convert.FromBase64String(binaryFileContentsNode.InnerText);
                        byte[] binaryFileContents = File.ReadAllBytes(fiToClean.FullName);
                        binaryEquals = data.SequenceEqual(binaryFileContents);
                    }
                    String value = String.Empty;
                    if (!ReferenceEquals(fileContentsNode, null)) value = HttpUtility.HtmlDecode(fileContentsNode.InnerXml);
                    else if (!ReferenceEquals(zippedFileContents, null)) value = Convert.FromBase64String(zippedFileContents.InnerXml).UnzipToString();
                    if (!neverOverwrite || File.ReadAllText(fiToClean.FullName) == value || binaryEquals)
                    {
                        Console.WriteLine("SSoTme Cleaning {0}", fiToClean.FullName);
                        fiToClean.Delete();
                    }
                }
            }
        }

        public static String GetFileSetFileContents(this FileSetFile fileSetFile)
        {
            if (ReferenceEquals(fileSetFile, null)) return String.Empty;
            else
            {
                if (!String.IsNullOrEmpty(fileSetFile.FileContents)) return fileSetFile.FileContents;
                else if (!ReferenceEquals(fileSetFile.ZippedFileContents, null)) return fileSetFile.ZippedFileContents.UnzipToString();
                else return String.Empty;
            }
        }

        public static byte[] GetFileSetFileBinaryContents(this FileSetFile fileSetFile)
        {
            if (!ReferenceEquals(fileSetFile.ZippedBinaryFileContents, null))
            {
                return SSoTme.OST.Lib.Extensions.SSOTMEExtensions.Unzip(fileSetFile.ZippedBinaryFileContents);
            }
            else return new Byte[] { };
        }


        private static string GetFullFileName(string relativeFileName, DirectoryInfo rootDI)
        {
            if (rootDI == null) rootDI = new DirectoryInfo(".");
            relativeFileName = relativeFileName.SafeToString().Trim("\r\n\t \\/".ToCharArray());
            FileInfo fi = new FileInfo(Path.Combine(rootDI.FullName, relativeFileName));
            return fi.FullName;
        }

        public static FileSet ConvertXmlToXsdFileSet(string xml, String inputFileName)
        {
            XDocument srcDoc = XDocument.Parse(xml);

            XmlReader reader = XmlReader.Create(new StringReader(xml));
            XmlSchemaInference inference = new XmlSchemaInference();
            XmlSchemaSet schemaSet = inference.InferSchema(reader);

            // Display the inferred schema.
            var index = 1;
            FileSet fs = new FileSet();
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                MemoryStream sms = new MemoryStream();
                StreamWriter writer = new StreamWriter(sms);
                schema.Write(writer);
                String xsd = Encoding.Default.GetString(sms.GetBuffer(), 0, (int)sms.Length);

                inputFileName = Path.GetFileName(inputFileName);
                if (String.Equals(Path.GetExtension(inputFileName), ".xsd", StringComparison.OrdinalIgnoreCase))
                {
                    inputFileName = inputFileName.Substring(0, inputFileName.Length - 4);
                }

                var extension = ".xsd";
                if (index > 1) extension = index.ToString() + extension;
                index++;

                var dsFileName = String.Format("{0}{1}", inputFileName, extension);
                FileSetFile fsf = new FileSetFile();
                fsf.AlwaysOverwrite = true;
                fsf.RelativePath = dsFileName;
                fsf.FileContents = xsd;
                fs.FileSetFiles.Add(fsf);
            }

            return fs;
        }

        public static String LowerHyphenName(this string name)
        {
            return name.ToTitle().Replace(" ", "-").ToLower();
        }

        public static String StripParamNumber(this string fullParamString)
        {
            fullParamString = fullParamString.SafeToString();
            if (fullParamString.StartsWith("param") &&
                fullParamString.Substring(0, "paramNN=".Length).Contains("="))
            {
                fullParamString = fullParamString.Substring(fullParamString.IndexOf("=") + 1);
            }
            return fullParamString;
        }

        public static String ToSqlSafeString(this object theObject)
        {
            return theObject.SafeToString().Replace("'", "''");
        }
        public static String ToCamelString(this String titleString)
        {
            titleString = titleString.SafeToString().Replace("-", " ");
            var sb = new StringBuilder();
            var prevChar = ' ';
            foreach (char currentChar in titleString)
            {
                if (!Char.IsLetterOrDigit(currentChar))
                {
                    // Do nothing in this case
                }
                else if (prevChar == ' ') sb.Append(currentChar.SafeToString().ToUpper());
                else sb.Append(currentChar);

                prevChar = currentChar;
            }
            return sb.ToString().Replace(" ", "");
        }

        public static String TitleFromCamel(this String camelString)
        {
            camelString = String.Join("", camelString.SafeToString().ToList().Select(feChar => (Char.IsUpper(feChar) ? " " : "") + feChar.SafeToString()));
            return camelString.Trim();
        }

        public static string SHA256(this string password)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }




        public static List<XmlElement> FileSetFilesFromFileSetXml(this string fileSetXml)
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(fileSetXml);
            return xdoc.DocumentElement
                       .SelectNodes("//FileSetFile")
                       .OfType<XmlElement>()
                       .ToList();
        }


        public static String GetFileContents(this XmlElement fileSetFileElement)
        {
            var fileContentsElement = fileSetFileElement.SelectSingleNode(".//FileContents");
            if (!ReferenceEquals(fileContentsElement, null))
            {
                var fileContents = fileContentsElement.InnerXml;
                return UnwrapCDATA(fileContents);
            }
            else
            {
                var zippedFileContentsElement = fileSetFileElement.SelectSingleNode(".//ZippedTextFileContents");
                if (!ReferenceEquals(zippedFileContentsElement, null))
                {
                    var fileContents = Convert.FromBase64String(zippedFileContentsElement.InnerXml).UnzipToString();
                    return fileContents;
                }
            }

            // If we get here - return an empty string
            return String.Empty;
        }

        public static String ToSingleTextFileFileSetXml(this string inputString, String fileName)
        {
            if (String.IsNullOrEmpty(inputString)) inputString = String.Empty;
            var xdoc = new XmlDocument();
            var fileSet = xdoc.CreateElement("FileSet");
            xdoc.AppendChild(fileSet);
            var fileSetFiles = xdoc.CreateElement("FileSetFiles");
            fileSet.AppendChild(fileSetFiles);
            var fileSetFile = xdoc.CreateElement("FileSetFile");
            fileSetFiles.AppendChild(fileSetFile);
            var relativePath = xdoc.CreateElement("RelativePath");
            relativePath.InnerText = fileName;
            fileSetFile.AppendChild(relativePath);
            var fileContents = xdoc.CreateElement("ZippedTextFileContents");
            {
                fileContents.InnerXml = Convert.ToBase64String(inputString.Zip());
            }
            fileSetFile.AppendChild(fileContents);
            return xdoc.OuterXml;
        }


        private static void ConstructSchema(FileInfo theFile)
        {
            string schemaFileName = theFile.DirectoryName + @"\Schema.ini";
            var schemaFI = new FileInfo(schemaFileName);
            if (schemaFI.Exists) schemaFI.Delete();

            StringBuilder schema = new StringBuilder();
            DataTable data = LoadCSV(theFile);
            schema.AppendLine("[" + theFile.Name + "]");
            schema.AppendLine("ColNameHeader=True");
            for (int i = 0; i < data.Columns.Count; i++)
            {
                schema.AppendLine("col" + (i + 1).ToString() + "=\"" + data.Columns[i].ColumnName + "\" Memo");
            }
            TextWriter tw = new StreamWriter(schemaFileName);
            tw.WriteLine(schema.ToString());
            tw.Close();
        }

        private static DataTable LoadCSV(FileInfo theFile)
        {
            DataTable theCSV = new DataTable();
            if (theFile.Length > 0)
            {
                string sqlString = "Select * FROM [" + theFile.Name + "];";
                string conStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source="
                    + theFile.DirectoryName + ";" + "Extended Properties='text;HDR=YES;CharacterSet=65001;'";

                throw new Exception("Need to find .NET CORE Equivalent of OleDbDataAdapter.  Maybe: https://stackoverflow.com/questions/40438377/how-to-get-oledb-for-reading-excel-in-asp-net-core-project");
                //using (OleDbConnection conn = new OleDbConnection(conStr))
                //{
                //    using (OleDbCommand comm = new OleDbCommand(sqlString, conn))
                //    {
                //        using (OleDbDataAdapter adapter = new OleDbDataAdapter(comm))
                //        {
                //            adapter.Fill(theCSV);
                //        }
                //    }
                //}
            }
            return theCSV;
        }

        public static XElement GetXMLFromCSV(FileInfo theFile, string rootNodeName, string itemName)
        {
            XElement retVal;
            DataTable data = CreateCsvAndSchema(theFile);
            DataSet ds = new DataSet(rootNodeName);
            data.TableName = itemName;
            ds.Tables.Add(data);
            retVal = XElement.Parse(ds.GetXml());
            return retVal;
        }

        private static DataTable CreateCsvAndSchema(FileInfo theFile)
        {
            ConstructSchema(theFile);
            return LoadCSV(theFile);
        }

        public static FileSet XslxToCsvFileSet(this Byte[] xlsxFile, String fileName, bool alwaysOverwrite = false)
        {

            var result = default(DataSet);
            FileSet fs = new FileSet();
            if (fileName.ToLower().EndsWith(".xlsx"))
            {
                // Reading from a binary Excel file (format; *.xlsx)
                var stream = new MemoryStream(xlsxFile);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                result = excelReader.AsDataSet();
                excelReader.Close();
            }

            if (fileName.ToLower().EndsWith(".xls"))
            {
                // Reading from a binary Excel file ('97-2003 format; *.xls)
                var stream = new MemoryStream(xlsxFile);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                result = excelReader.AsDataSet();
                excelReader.Close();
            }

            if (ReferenceEquals(result, null)) throw new Exception("Can't read XLSX or XLS file for some reason..");

            var tableNames = result.Tables.OfType<DataTable>().Select(selectDT => selectDT.TableName);
            foreach (var tableName in tableNames)
            {
                FileSetFile fsf = new FileSetFile();
                fsf.RelativePath = String.Format("{0}.csv", tableName);
                var csv = getCSV(result, tableName);
                fsf.ZippedFileContents = csv.Zip();
                fsf.AlwaysOverwrite = alwaysOverwrite;
                fs.FileSetFiles.Add(fsf);
            }

            return fs;

        }

        private static string getCSV(DataSet xlsxDataSet, string table)
        {
            int row_no = 0;

            var output = new StringBuilder();

            while (row_no < xlsxDataSet.Tables[table].Rows.Count)
            {
                for (int i = 0; i < xlsxDataSet.Tables[table].Columns.Count; i++)
                {
                    var value = xlsxDataSet.Tables[table].Rows[row_no][i].ToString();
                    value = value.Replace("\"", "\"\"");
                    output.AppendFormat("\"{0}\",", value);
                }
                row_no++;
                output.AppendLine();
            }

            return output.ToString();
        }

        public static string CsvToXml(this String csvText, String fileName)
        {
            String tempFile = String.Format("temp\\{0}", fileName);
            FileInfo fi = new FileInfo(tempFile);
            if (!fi.Directory.Exists) fi.Directory.Create();
            File.WriteAllText(fi.FullName, csvText, new UTF8Encoding(false));
            try
            {

                fileName = Path.GetFileNameWithoutExtension(fileName);
                var pluralFileName = SSOTMEExtensions.Pluralizer.Pluralize(fileName);
                var singularFileName = SSOTMEExtensions.Pluralizer.Singularize(fileName);

                var xml = GetXMLFromCSV(fi, pluralFileName, singularFileName);
                return xml.ToString();
            }
            finally
            {
                File.Delete(tempFile);
            }

        }


        private static void CleanRow(DataRow feRow)
        {
            feRow.Table
                 .Columns
                 .OfType<DataColumn>()
                 .ToList()
                 .ForEach(feCol => CleanCol(feRow, feCol));
        }

        private static void CleanCol(DataRow feRow, DataColumn feCol)
        {
            var value = feRow[feCol].SafeToString();
            if (value.Any(anyChar => !Char.IsLetterOrDigit(anyChar) && !" ?~`!@#$%^&*()-_=+[{]}\\|;:',./?".Contains(anyChar)))
            {
                feRow[feCol] = String.Format("<![CDATA_START[{0}]CDATA_END]>", value);
                feRow.AcceptChanges();
            }
        }


        public static string ConvertJsonToXml(string inputFileName, string inputFileContents, bool removeAllExtensions = false)
        {
            var inputExtension = Path.GetExtension(inputFileName);

            var inputName = "root";
            if (!String.IsNullOrEmpty(inputFileName))
            {
                inputName = Path.GetFileNameWithoutExtension(inputFileName);
                while (removeAllExtensions && inputName.Contains("."))
                {
                    inputName = Path.GetFileNameWithoutExtension(inputName);
                }
            }

            if (String.IsNullOrEmpty(inputFileContents)) inputFileContents = "[]";

            // wrap if input doesn't start with inputName
            var expectedStartingToken = String.Format("{{\"{0}\":", inputName);
            bool startsAsExpected = inputFileContents.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").StartsWith(expectedStartingToken);
            if (!startsAsExpected)
            {
                inputFileContents = String.Format("{{ \"{0}\":{1}}}", inputName, inputFileContents);
            }


            var xml = JsonToXml(inputFileContents).OuterXml;
            return xml;
        }


        public static String FormatXml(this String Xml)
        {
            String Result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(Xml);

                writer.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                String FormattedXML = sReader.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException ex)
            {
                throw ex;
            }

            mStream.Close();
            writer.Close();

            return Result;
        }

        public static string FindClosest(this string fullFileName, string otherFile)
        {
            String dir = Path.GetDirectoryName(fullFileName);
            String combinedPath = Path.Combine(dir, otherFile);
            Uri absoluteUri = new Uri(String.Format("file:///{0}", combinedPath));
            //Console.WriteLine(String.Format("resolving: {0}", absoluteUri.ToString()));
            String fileName = absoluteUri.LocalPath;
            String relativeFileName = fullFileName.RelativeFromFull(fileName);
            bool lookDown = true;
            int count = 0;
            while (!File.Exists(fileName) && (count < 10))
            {
                if (relativeFileName.StartsWith("../") && lookDown) fileName = fullFileName.FullFromRelative(relativeFileName.Substring(3));
                else
                {
                    lookDown = false;
                    fileName = fullFileName.FullFromRelative("../" + relativeFileName);
                }

                relativeFileName = fullFileName.RelativeFromFull(fileName);
                count++;
            }

            if (!File.Exists(fileName)) fileName = absoluteUri.LocalPath;

            return fileName;
        }

        public static event EventHandler FileWritten;
        private static void OnFileWritten(String fileName)
        {
            if (!ReferenceEquals(FileWritten, null)) FileWritten(fileName, EventArgs.Empty);
        }

        private static void WriteAllBytes(string fileName, byte[] fileBytes)
        {
            File.WriteAllBytes(fileName, fileBytes);
            OnFileWritten(fileName);
        }

        public static String UnwrapCDATA(this String cdataText)
        {
            cdataText = cdataText.SafeToString();
            if (cdataText.StartsWith("<![CDATA[") && cdataText.EndsWith("]]>"))
            {
                var startPosition = "<![CDATA[".Length;
                var lengthToExclude = "<![CDATA[]]>".Length;
                cdataText = cdataText.Substring(startPosition, cdataText.Length - lengthToExclude);
            }
            return cdataText;
        }

        private static void WriteAllText(string fileName, string fileContents)
        {
            var fi = new FileInfo(fileName);
            if (!fi.Directory.Exists) fi.Directory.Create();
            using (StreamWriter sw = new StreamWriter(File.Open(fileName, FileMode.Create), new UTF8Encoding(false)))
            {
                sw.Write(fileContents.UnwrapCDATA());
            }
            OnFileWritten(fileName);
        }

        public static void SplitFileSetXml(this string fileSetXml, String basePath)
        {
            fileSetXml.SplitFileSetXml(false, basePath);
        }

        public static void SplitFileSetXml(this string fileSetXml, bool overwriteAll, String basePath)
        {
            if (String.IsNullOrEmpty(fileSetXml) || !fileSetXml.Contains("<")) return;

            fileSetXml = fileSetXml.Substring(fileSetXml.IndexOf("<"));
            fileSetXml = fileSetXml.Replace("FileContents><?xml ", "FileContents>&lt;?xml");
            XmlDocument doc = new XmlDocument();
            //doc.PreserveWhitespace = true;
            try
            {
                //if (!newFileContents.StartsWith("<?xml ")) newFileContents = "<?xml version=\"1.0\"?>" + newFileContents;
                int len = fileSetXml.Length;
                fileSetXml = fileSetXml.Trim((char)(65279));
                int len2 = fileSetXml.Length;
                doc.LoadXml(fileSetXml);
                if (doc.DocumentElement.Name == "FileSet")
                {
                    foreach (XmlElement elem in doc.DocumentElement.SelectNodes("//FileSetFile"))
                    {
                        bool neverOverwrite = true;

                        XmlNode aoNode = elem.SelectSingleNode("AlwaysOverwrite");
                        if (!ReferenceEquals(aoNode, null))
                        {
                            if (aoNode.InnerText == "true") neverOverwrite = false;
                        }
                        else
                        {
                            XmlNode omNode = elem.SelectSingleNode("OverwriteMode");
                            if (!ReferenceEquals(omNode, null) && (omNode.InnerText != "Never"))
                            {
                                neverOverwrite = false;
                            }
                        }

                        XmlNode contentsNode = elem.SelectSingleNode("FileContents");
                        XmlNode binaryContentsNode = elem.SelectSingleNode("BinaryFileContents");
                        XmlNode zippedBinaryContentsNode = elem.SelectSingleNode("ZippedBinaryFileContents");
                        XmlNode zippedTextContentsNode = elem.SelectSingleNode("ZippedTextFileContents");
                        if (ReferenceEquals(zippedTextContentsNode, null))
                        {
                            zippedTextContentsNode = elem.SelectSingleNode("ZippedFileContents");
                        }

                        if (!ReferenceEquals(contentsNode, null))
                        {
                            String contents = contentsNode.InnerXml;
                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                ProcessFileSetFile(String.Format("{0}/test.xml", basePath), overwriteAll, elem, contents, fileName, basePath);
                            }
                        }
                        else if (!ReferenceEquals(binaryContentsNode, null) ||
                                !ReferenceEquals(zippedBinaryContentsNode, null) ||
                                !ReferenceEquals(zippedTextContentsNode, null))
                        {

                            String contents = String.Empty;
                            if (!ReferenceEquals(binaryContentsNode, null)) contents = binaryContentsNode.InnerXml;
                            else if (!ReferenceEquals(zippedBinaryContentsNode, null)) contents = zippedBinaryContentsNode.InnerXml;
                            else contents = zippedTextContentsNode.InnerXml;

                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                byte[] data = Convert.FromBase64String(contents);

                                var finalName = Path.Combine(basePath, fileName.InnerText.SafeToString().Trim("/".ToCharArray()));
                                var fileInfo = new FileInfo(finalName);

                                if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

                                if (!fileInfo.Exists || !neverOverwrite)
                                {

                                    if (!ReferenceEquals(zippedBinaryContentsNode, null))
                                    {
                                        File.WriteAllBytes(fileInfo.FullName, data.Unzip());
                                    }
                                    else if (!ReferenceEquals(zippedTextContentsNode, null))
                                    {

                                        using (StreamWriter sw = new StreamWriter(File.Open(fileInfo.FullName, FileMode.Create), new UTF8Encoding(false)))
                                        {
                                            sw.WriteLine(data.UnzipToString());
                                        }
                                    }
                                    else
                                    {
                                        if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();
                                        var di = new DirectoryInfo(fileInfo.FullName);
                                        if (di.Exists) throw new Exception(String.Format("Invalid filename for result file - {0} is a directory", fileInfo.FullName));
                                        else File.WriteAllBytes(fileInfo.FullName, data);
                                    }

                                }
                            }
                        }
                        else
                        {
                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                var fileInfo = new FileInfo(fileName.InnerText);

                                if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

                                if (!fileInfo.Exists || !neverOverwrite)
                                {
                                    WriteAllText(fileName.InnerText, "Can't write binary contents.");
                                }
                            }

                        }
                    }

                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                // Ignore failures attempting to write extra files
                throw ex;
            }
        }


        private static void ProcessFileSetFile(String relativePathOfXml, bool overwriteAll, XmlElement elem, String contents, XmlElement fileName, String basePath)
        {
            String relativeFileName = fileName.InnerText;
            relativeFileName = FullFromRelative(relativePathOfXml, relativeFileName.Trim("\\/ \r\n".ToCharArray()));
            //lastOutputFiles.Add(fullFileName);
            String dir = Path.GetDirectoryName(relativeFileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            bool fileExists = File.Exists(relativeFileName);
            bool writeFile = true;
            bool neverOverwrite = true;

            XmlNode aoNode = elem.SelectSingleNode("AlwaysOverwrite");
            if (!ReferenceEquals(aoNode, null))
            {
                if (aoNode.InnerText == "true") neverOverwrite = false;
            }
            else
            {
                XmlNode omNode = elem.SelectSingleNode("OverwriteMode");
                if (!ReferenceEquals(omNode, null) && (omNode.InnerText != "Never"))
                {
                    neverOverwrite = false;
                }
            }

            if (fileExists && neverOverwrite)
            {
                writeFile = false;
                //lastOutputFilesSkipped.Add(fullFileName);
            }

            while (contents.Contains("[$$NEWUUID$$]"))
            {
                contents = String.Format("{0}{1}{2}",
                                    contents.Substring(0, contents.IndexOf("[$$NEWUUID$$]")),
                                    Guid.NewGuid(),
                                    contents.Substring(contents.IndexOf("[$$NEWUUID$$]") + "[$$NEWUUID$$]".Length));
            }
            String decodedContent = HttpUtility.HtmlDecode(contents);
            decodedContent = decodedContent.UnwrapCDATA();

            if (overwriteAll || writeFile)
            {
                if (overwriteAll ||
                    !File.Exists(relativeFileName) ||
                    (File.ReadAllText(relativeFileName) != decodedContent))
                {
                    WriteAllText(relativeFileName, decodedContent);
                }
            }
        }

        public static void SplitFileSetFile(this String fileSetFileName, String basePath)
        {
            String fileContents = File.ReadAllText(fileSetFileName);
            SplitFileSetXml(fileContents, false, basePath);
        }

        public static string FullFromRelative(this string rootFullPath, string relativeFileName)
        {
            relativeFileName = relativeFileName.SafeToString().Trim("\r\n\t \\/".ToCharArray());
            FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(rootFullPath), relativeFileName));
            return fi.FullName;
        }

        public static string RelativeFromFull(this String rootFullPath, String fullPath)
        {
            if (String.IsNullOrEmpty(fullPath)) return fullPath;
            // Trim the path until it doesn't match
            String dir = Path.GetDirectoryName(rootFullPath).Replace("/", "\\");
            String[] dirParts = dir.Split("\\".ToCharArray());
            String[] fullPathParts = fullPath.Replace("/", "\\").Split("\\".ToCharArray());
            int index = 0;
            while (index < fullPathParts.Length && index < dirParts.Length)
            {
                if (String.Equals(fullPathParts[index], dirParts[index], StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }
                else
                {
                    break;
                }
            }
            String left = String.Join("\\", dirParts.Take(index));
            String[] parts = left.Split("\\/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            String right = String.Join("\\", fullPathParts.Skip(index));
            int count = dirParts.Length - index;
            for (int i = 0; i < count; i++)
            {
                right = "../" + right;
            }
            return right.Replace("/", "\\");
        }

        public static string GetTextResourceContents(this Assembly assembly, String resourceName)
        {
            var fullResourceName = assembly.GetManifestResourceNames().FirstOrDefault(fod => fod.Contains(resourceName));
            if (String.IsNullOrEmpty(fullResourceName)) throw new Exception(String.Format("Can't find resource '{0}' in '{1}'", resourceName, assembly.FullName));
            else
            {
                var resourceStream = assembly.GetManifestResourceStream(fullResourceName);
                using (var streamReader = new StreamReader(resourceStream))
                {
                    resourceStream.Position = 0;
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static String HtmlToXml(this String htmlText)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            MemoryStream ms = new MemoryStream();
            doc.OptionOutputAsXml = true;
            var anchors = doc.DocumentNode
                            .SelectNodes("//a");

            if (!ReferenceEquals(anchors, null))
            {
                anchors.Where(whereNode => !ReferenceEquals(whereNode, null))
                        .ToList()
                        .ForEach(feAnchor => feAnchor.CleanSPECDOCLink());
            }

            doc.Save(ms);
            ms.Position = 0;
            var sr = (new StreamReader(ms));
            return sr.ReadToEnd();
        }

        public static String XmlToJson(this XmlDocument doc)
        {
            return JsonConvert.SerializeXmlNode(doc.DocumentElement, Newtonsoft.Json.Formatting.Indented);

        }

        public static XmlDocument JsonToXml(this String json)
        {
            try
            {
                var serializedJson = json;
                return JsonConvert.DeserializeXmlNode(serializedJson);
            }
            catch // (JsonSerializationException jse)
            {
                json = String.Format("{{ root: {0} }}", json);
                return JsonConvert.DeserializeXmlNode(json);
            }
        }

        public static String XmlToJson(this String xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return JsonConvert.SerializeXmlNode(doc.DocumentElement);
        }

        private static void CleanSPECDOCLink(this HtmlNode node)
        {
            if (!ReferenceEquals(node.Attributes, null))
            {
                var href = node.Attributes["href"];
                if (!ReferenceEquals(href, null))
                {
                    if (!String.IsNullOrEmpty(href.Value))
                    {
                        if (href.Value.Contains("://specdocs/"))
                        {
                            href.Value = href.Value.Substring(href.Value.IndexOf("://specdocs/") + "://specdocs/".Length);
                            var entityIndex = href.Value.ToLower().IndexOf("/entity/");
                            var viewResourceIndex = href.Value.ToLower().IndexOf("/view-resource/");
                            var simpleLinkIndex = href.Value.ToLower().IndexOf("/link/");
                            if (entityIndex >= 0)
                            {
                                var entityName = href.Value.Substring(entityIndex + "/entity/".Length);
                                var candidates = entityName.Split("/&?".ToCharArray());
                                var finalName = candidates.FirstOrDefault(fodCandidate => !String.IsNullOrEmpty(fodCandidate.SafeToString().Trim()));

                                href.Value = String.Format("$SDK_ROOT_URI$Docs/DataModel/Entity_{0}.html", finalName);
                            }
                            else if (viewResourceIndex >= 0)
                            {
                                var resourceName = href.Value.Substring(viewResourceIndex + "/view-resource/".Length);
                                var candidates = resourceName.Split("/&?".ToCharArray());
                                var finalName = candidates.FirstOrDefault(fodCandidate => !String.IsNullOrEmpty(fodCandidate.SafeToString().Trim()));

                                href.Value = String.Format("$SDK_ROOT_URI$Docs/AdditionalResources/Resource_{0}.html", finalName);
                            }
                            else if (simpleLinkIndex >= 0)
                            {
                                href.Value = HttpUtility.UrlDecode(href.Value.Substring(viewResourceIndex + "/link/".Length));
                            }
                            else
                            {
                                href.Value = "ERROR - COULD NOT FIND A MATCHING SPECDOCS// syntax for: '" + href.Value + "'";
                            }

                            if (href.Value.Contains(".html&")) href.Value = href.Value.Substring(0, href.Value.IndexOf(".html&") + ".html".Length);
                        }
                        else if (href.Value.StartsWith("https://www.google.com/url?q="))
                        {
                            var decodedUrl = HttpUtility.UrlDecode(href.Value.Substring("https://www.google.com/url?q=".Length)) + "&amp;";
                            href.Value = decodedUrl.Substring(0, decodedUrl.IndexOf("&amp;"));
                            node.SetAttributeValue("target", "_blank");
                        }
                        else
                        {
                            node.SetAttributeValue("target", "_blank");
                            object o = 1;
                        }
                    }
                }
            }
        }

        public static T GetFirst<T>(this string[] args)
            where T : class
        {
            if (typeof(T) == typeof(String))
            {
                if (ReferenceEquals(args, null)) return String.Empty as T;
                else if (!args.Any()) return String.Empty as T;
                else return args[0] as T;
            }
            else if (typeof(T) == typeof(Uri))
            {
                var uriString = args.GetFirst<String>();
                if (!String.IsNullOrEmpty(uriString))
                {
                    return new Uri(uriString) as T;
                }
            }
            else if (typeof(T) == typeof(FileInfo))
            {
                var fileNameString = args.GetFirst<String>();
                if (!String.IsNullOrEmpty(fileNameString))
                {
                    return new FileInfo(fileNameString) as T;
                }
            }
            else
            {
                var msg = String.Format("Handler not writtent to handle finding first argument of type '{0}'", typeof(T).Name);
                throw new NotImplementedException(msg);
            }

            // If we get here - return nothing
            return default(T);
        }


        public static byte[] Zip(this string str)
        {
            if (String.IsNullOrEmpty(str)) str = String.Empty;
            var bytes = new UTF8Encoding(false).GetBytes(str);
            return bytes.Zip();
        }

        public static byte[] Zip(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static bool IsBinaryFile(this FileInfo fi)
        {
            long length = fi.Length;
            if (length == 0) return false;

            using (StreamReader stream = new StreamReader(fi.FullName))
            {
                int ch;
                while ((ch = stream.Read()) != -1)
                {
                    if (isControlChar(ch))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool isControlChar(int ch)
        {
            return (ch > Chars.NUL && ch < Chars.BS)
                || (ch > Chars.CR && ch < Chars.SUB);
        }

        public static class Chars
        {
            public static char NUL = (char)0; // Null char
            public static char BS = (char)8; // Back Space
            public static char CR = (char)13; // Carriage Return
            public static char SUB = (char)26; // Substitute
        }


        public static bool IsAssignableTo(this Type childClass, Type parentClass)
        {
            return parentClass.IsAssignableFrom(childClass);
        }


        public static Type FindType(this String theTypeName)
        {
            return Type.GetType(theTypeName);
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }



        public static String Singularize(this String text)
        {
            var wordToSingularize = text.SafeToString().Contains(" ") ? text.Substring(text.LastIndexOf(" ") + 1) : text.SafeToString();
            var singular = Pluralizer.Singularize(wordToSingularize);
            String result = singular;
            if (wordToSingularize.Length != text.Length) result = text.Substring(0, text.Length - wordToSingularize.Length) + singular;
            return result;
        }

        public static String Pluralize(this String text)
        {
            var wordToPluralize = text.SafeToString().Contains(" ") ? text.Substring(text.LastIndexOf(" ") + 1) : text.SafeToString();
            var plural = Pluralizer.Pluralize(wordToPluralize);
            String result = plural;
            if (wordToPluralize.Length != text.Length) result = text.Substring(0, text.Length - wordToPluralize.Length) + plural;
            return result;
        }

        public static String SqlSafeToString(this object theObject)
        {
            return theObject.SafeToString().Replace("'", "''");
        }


        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] ToBytes(this Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }


        public static Bitmap ToGrayscale(this Image image)
        {
            Bitmap btm = new Bitmap(image);
            for (int i = 0; i < btm.Width; i++)
            {
                for (int j = 0; j < btm.Height; j++)
                {
                    int ser = (btm.GetPixel(i, j).R + btm.GetPixel(i, j).G + btm.GetPixel(i, j).B) / 3;
                    btm.SetPixel(i, j, Color.FromArgb(ser, ser, ser));
                }
            }
            return btm;
        }

        public static Byte[] Unzip(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public static string UnzipToString(this byte[] zippedBytes)
        {
            if (ReferenceEquals(zippedBytes, null)) return String.Empty;
            else
            {
                var unzippedBytes = zippedBytes.Unzip();

                return Encoding.UTF8.GetString(unzippedBytes);

            }
        }

        public static String ToTitleCase(this String text)
        {
            if (String.IsNullOrEmpty(text)) return String.Empty;
            else
            {
                text = text.SafeToString();

                if (Char.IsLower(text[0])) text = text.First().ToString().ToUpper() + text.Substring(1);

                return String.Join(String.Empty,
                    text.SafeToString()
                           .Select(c => (Char.IsUpper(c) ? " " + c.ToString() : c.ToString()))
                           .ToArray())
                        .Trim();
            }
        }

        public static object GetTranspilersAsJson(object screenName)
        {
            throw new NotImplementedException();
        }
    }
}

namespace CoreLibrary.SassyMQ
{
    public class PHClass { }
}