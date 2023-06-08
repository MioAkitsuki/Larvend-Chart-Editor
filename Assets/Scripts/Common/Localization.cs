using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using System;

namespace Larvend
{
    public class Localization
    {
        public static string GetString(string lang, string key)
        {
            string path = Application.streamingAssetsPath + $"/I18n/{ lang }.xml";

            if (File.Exists(path))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(path);
                XmlNodeList nodeList = xmlDoc.SelectSingleNode("Localization")?.ChildNodes;

                if (nodeList != null)
                {
                    foreach (XmlElement node in nodeList)
                    {
                        if (node.Name == key)
                        {
                            return node.InnerText.Replace("\\n", Environment.NewLine);
                        }
                    }
                }

                return "Item Not Found";
            }

            return "Localization File Not Found";
        }
    }
}