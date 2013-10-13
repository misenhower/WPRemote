using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DEBUG
namespace Komodex.Common.SampleData
{
    public static class SampleDataHelper
    {
        private readonly static Random _random = new Random();
        private readonly static string[] _words = "aenean aliquam class cras curabitur curae donec duis etiam fusce in integer lorem maecenas mauris morbi nam nulla nullam nunc pellentesque phasellus praesent proin quisque sed suspendisse ut vestibulum vivamus a ac accumsan ad adipiscing aliquam aliquet amet ante aptent arcu at auctor augue bibendum blandit commodo condimentum congue consectetuer consequat conubia convallis cubilia cursus dapibus diam dictum dictumst dignissim dis dolor egestas eget eleifend elementum elit enim erat eros est et eu euismod facilisi facilisis fames faucibus felis fermentum feugiat fringilla gravida habitant habitasse hac hendrerit himenaeos iaculis id imperdiet in inceptos interdum ipsum justo lacinia lacus laoreet lectus leo libero ligula litora lobortis lorem luctus magna magnis malesuada massa mattis mauris metus mi mollis montes morbi mus nascetur natoque nec neque netus nibh nisi nisl non nostra nulla nunc odio orci ornare parturient pede pellentesque penatibus per pharetra placerat platea porta porttitor posuere potenti pretium primis pulvinar purus quam quis rhoncus risus rutrum sagittis sapien scelerisque sed sem semper senectus sit sociis sociosqu sodales sollicitudin suscipit taciti tellus tempor tempus tincidunt torquent tortor tristique turpis ullamcorper ultrices ultricies urna ut varius vehicula vel velit venenatis vestibulum vitae viverra volutpat vulputate".Split(' ');

        public static void FillSampleData(object obj, bool fillLists = true)
        {
            FillSampleData(obj, fillLists, 0);
        }

        private static void FillSampleData(object obj, bool fillLists, int level)
        {
            // Assign values to the object's properties
            var properties = obj.GetType().GetProperties();
            foreach (var property in properties)
            {
                // Make sure we can set this property
                if (property.GetSetMethod() == null)
                    continue;
                // Make sure this isn't an indexer
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // String
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(obj, GetRandomString(2, 5), null);
                    continue;
                }

                // Int
                if (property.PropertyType == typeof(int))
                {
                    property.SetValue(obj, GetRandomInt(0, 100), null);
                    continue;
                }

                // Bool
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(obj, GetRandomBool(), null);
                    continue;
                }

                // List
                if (fillLists && level < 2)
                {
                    Type subListItemType = GetGenericListType(property.PropertyType);
                    if (subListItemType != null)
                    {
                        Type subListType = property.PropertyType;
                        if (subListType.IsInterface)
                            subListType = typeof(List<>).MakeGenericType(subListItemType);

                        object subList = Activator.CreateInstance(subListType);

                        FillSampleData(subList, fillLists, level + 1);
                        property.SetValue(obj, subList, null);
                        continue;
                    }
                }
            }

            // If this is a list, add items
            Type listItemType = GetGenericListType(obj.GetType());
            if (listItemType != null)
            {
                var addMethod = obj.GetType().GetMethod("Add");

                int listItemCount = (level < 2) ? 10 : 5;
                for (int i = 0; i < listItemCount; i++)
                {
                    object item = Activator.CreateInstance(listItemType);
                    FillSampleData(item, fillLists, level + 1);
                    addMethod.Invoke(obj, new object[] { item });
                }
            }
        }

        private static Type GetGenericListType(Type type)
        {
            // If this is an interface, only return true if it's an IList<T>
            if (type.IsInterface)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                    return type.GetGenericArguments()[0];
                return null;
            }

            // Return true if the type inherits from List<T>
            var interfaces = type.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                Type genericType = GetGenericListType(interfaceType);
                if (genericType != null)
                    return genericType;
            }

            return null;
        }

        #region Random Data Generators

        private static int GetRandomInt(int min, int max)
        {
            return _random.Next(min, max + 1);
        }

        private static bool GetRandomBool()
        {
            return (GetRandomInt(0, 1) == 1);
        }

        private static string GetRandomString(int minWords, int maxWords)
        {
            int words = GetRandomInt(minWords, maxWords);
            string result = string.Join(" ", _words.OrderBy(s => _random.Next()).Take(words));
            result = char.ToUpper(result[0]) + result.Substring(1);
            return result;
        }

        #endregion
    }
}
#endif
