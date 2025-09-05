using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

public class CSVReader
{
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    public static List<Dictionary<string, object>> Read(string file)
    {
        var list = new List<Dictionary<string, object>>();
        TextAsset data = Resources.Load(file) as TextAsset;

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        var header = Regex.Split(lines[0], SPLIT_RE);

        for (var k = 0; k < header.Length; k++)
        {
            //LocalizeManager.Instance.data_Header.Add(header[k]);
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                object finalvalue = value;
                int n;
                float f;
                if (int.TryParse(value, out n))
                {
                    finalvalue = n;
                }
                else if (float.TryParse(value, out f))
                {
                    finalvalue = f;
                }
                entry[header[j]] = finalvalue;
            }
            list.Add(entry);
        }
        return list;
    }

    public static List<T> ReadAutoData<T>(string text) where T : new()
    {
        var dataList = new List<T>();

        var lines = Regex.Split(text, LINE_SPLIT_RE);

        if (lines.Length <= 1)
            return dataList;

        var header = Regex.Split(lines[0], SPLIT_RE);

        for (int i = 0; i < header.Length; i++)
        {
            header[i] = header[i].Replace("\"", "");
        }

        for (var i = 1; i < lines.Length; i++)
        {
            T classData = new T();

            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "")
                continue;

            DataFieldType dataFieldType = DataFieldType.String;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                dataFieldType = DataFieldType.String;

                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\n", "\n");
                object finalvalue = value;
                int n;
                float f;
                LocalizeStatus s;

				if (int.TryParse(value, out n))
                {
                    finalvalue = n;
                    dataFieldType = DataFieldType.Int;
                }
                else if (float.TryParse(value, out f))
                {
                    finalvalue = f;
                    dataFieldType = DataFieldType.Float;
                }

				if (classData.GetType().GetField(header[j]) == null)
                {
                    Debug.LogWarning(" header null = " + header[j]);
                }
                else
                {
                    FieldInfo fi = classData.GetType().GetField(header[j]);

                    if (dataFieldType == DataFieldType.String && (string)finalvalue == "")
                    {
                        if (fi.FieldType != typeof(string))
                        {
                            finalvalue = 0;
                        }
                    }

                    classData.GetType().GetField(header[j]).SetValue(classData, finalvalue);
                }
            }

            dataList.Add(classData);
        }
        return dataList;
    }

    public enum DataFieldType
    {
        Int,
        Float,
        String,
    }
}
