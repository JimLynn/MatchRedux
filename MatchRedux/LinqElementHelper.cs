using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace MatchRedux
{
	public static class LinqElementHelper
	{
		public static string GetFieldValue(this XElement element, string fieldName)
		{
			return element.Elements("field").First(e => e.Attribute("name").Value == fieldName).Value;
		}

		public static int GetFieldInt(this XElement element, string fieldName)
		{
			return Convert.ToInt32(element.Elements("field").First(e => e.Attribute("name").Value == fieldName).Value);
		}
		public static int GetElementInt(this XElement element, string fieldName)
		{
			return Convert.ToInt32(element.Element(fieldName).Value);
		}
		public static bool GetElementBool(this XElement element, string fieldName)
		{
			return Convert.ToBoolean(element.Element(fieldName).Value);
		}
		public static bool GetAttributeBool(this XElement element, string attribute)
		{
			return Convert.ToBoolean(element.Attribute(attribute).Value);
		}
		public static int GetAttributeInt(this XElement element, string fieldName)
		{
			return Convert.ToInt32(element.Attribute(fieldName).Value);
		}

        /// <summary>
        /// Gets a date from an element in the XML document.
        /// We assume UTC but take timezone info in the date into account
        /// The result is in Universal Time
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <returns>The DateTime value in Universal time</returns>
		public static DateTime GetElementDate(this XElement element, string fieldName)
		{
            CultureInfo provider = CultureInfo.InvariantCulture;
            return DateTime.Parse(element.Element(fieldName).Value, provider, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
		}

	}
}
