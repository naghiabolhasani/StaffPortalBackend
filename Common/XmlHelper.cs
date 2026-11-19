using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StaffPortal.Common;

public static class XmlHelper
{
    /// <summary>
    /// حذف کاراکترهای نامعتبر XML (surrogate pairs و کاراکترهای کنترلی)
    /// </summary>
    public static string CleanInvalidXmlChars(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return xml;

        return Regex.Replace(xml, @"&#x([0-9A-Fa-f]+);", match =>
        {
            try
            {
                int num = Convert.ToInt32(match.Groups[1].Value, 16);

                if (num >= 0xD800 && num <= 0xDFFF)
                    return " ";

                if (num < 0x20 && num != 0x09 && num != 0x0A && num != 0x0D)
                    return " ";

                return match.Value;
            }
            catch
            {
                return " ";
            }
        });
    }

    /// <summary>
    /// حذف تمام namespaceها از XML تا دسترسی به المان‌ها ساده شود
    /// </summary>
    public static XDocument RemoveNamespaces(XDocument xdoc)
    {
        if (xdoc?.Root == null)
            return xdoc;

        foreach (var element in xdoc.Descendants().ToList())
        {
            // حذف namespace از نام المان
            element.Name = element.Name.LocalName;

            // حذف attributeهای namespace
            var nsAttrs = element.Attributes()
                .Where(a => a.IsNamespaceDeclaration)
                .ToList();

            foreach (var attr in nsAttrs)
                attr.Remove();
        }

        return xdoc;
    }
}