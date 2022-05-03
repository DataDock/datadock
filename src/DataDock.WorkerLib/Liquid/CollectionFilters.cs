using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataDock.Worker.Liquid
{
    public static class CollectionFilters
    {
        private static readonly Regex regex1 = new Regex(@"_(\p{Lu})");

        public static IEnumerable Where(IEnumerable collection, string property, string value)
        {
            var propertyName = regex1.Replace(property, m => m.Groups[1].Value.ToUpperInvariant());
            propertyName = char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1);
            foreach (var o in collection)
            {
                var p = o.GetType().GetProperty(propertyName);
                if (p != null)
                {
                    var propertyValue = p.GetValue(o)?.ToString();
                    if (propertyValue != null && propertyValue.Equals(value)) yield return o;
                }
            }
        }

        public static IEnumerable LastN(IEnumerable collection, int n)
        {
            var elements = new List<object>(collection.Cast<object>());
            return elements.Count < n ? elements : elements.GetRange(elements.Count - n, n);
        }
    }
}
