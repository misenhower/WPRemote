using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Komodex.Common.SampleData
{
#if DEBUG
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
#if NETFX_CORE
            List<PropertyInfo> properties = new List<PropertyInfo>();
            var type = obj.GetType();
            while (type != null)
            {
                var typeInfo = type.GetTypeInfo();
                properties.AddRange(typeInfo.DeclaredProperties);
                type = typeInfo.BaseType;
            }
#else
            var properties = obj.GetType().GetProperties();
#endif
            foreach (var property in properties)
            {
                // Make sure we can set this property
#if NETFX_CORE
                if (property.SetMethod == null)
                    continue;
#else
                if (property.GetSetMethod() == null)
                    continue;
#endif
                // Make sure this isn't an indexer
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // Try to get a value
                var sampleValue = GetSampleDataForType(property.PropertyType);
                if (sampleValue != null)
                {
                    property.SetValue(obj, sampleValue, null);
                    continue;
                }

                // List
                if (fillLists && level < 2)
                {
                    Type subListItemType = GetGenericListType(property.PropertyType);
                    if (subListItemType != null)
                    {
                        Type subListType = property.PropertyType;
#if NETFX_CORE
                        if (subListType.GetTypeInfo().IsInterface)
#else
                        if (subListType.IsInterface)
#endif
                            subListType = typeof(List<>).MakeGenericType(subListItemType);

                        object subList = Activator.CreateInstance(subListType);

                        FillSampleData(subList, fillLists, level + 1);
                        property.SetValue(obj, subList, null);
                        continue;
                    }
                }

                // SampleDataBase object
#if NETFX_CORE
                if (typeof(SampleDataBase).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
#else
                if (typeof(SampleDataBase).IsAssignableFrom(property.PropertyType))
#endif
                {
                    property.SetValue(obj, Activator.CreateInstance(property.PropertyType), null);
                    continue;
                }
            }

            // If this is a list, add items
            Type listItemType = GetGenericListType(obj.GetType());
            if (listItemType != null)
            {
#if NETFX_CORE
                var addMethod = obj.GetType().GetTypeInfo().GetDeclaredMethod("Add");
#else
                var addMethod = obj.GetType().GetMethod("Add");
#endif

                int listItemCount = (level < 2) ? 10 : 5;
                for (int i = 0; i < listItemCount; i++)
                {
                    object item = GetSampleDataForType(listItemType);
                    if (item == null)
                        break;
                    FillSampleData(item, fillLists, level + 1);
                    addMethod.Invoke(obj, new object[] { item });
                }
            }
        }

        private static Type GetGenericListType(Type type)
        {
#if NETFX_CORE
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            // If this is an interface, only return true if it's an IList<T>
            if (typeInfo.IsInterface)
            {
                if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
#if NETFX_CORE
                    return typeInfo.GenericTypeArguments[0];
#else
                    return typeInfo.GetGenericArguments()[0];
#endif
                return null;
            }

            // Return true if the type inherits from List<T>
#if NETFX_CORE
            var interfaces = type.GetTypeInfo().ImplementedInterfaces;
#else
            var interfaces = type.GetInterfaces();
#endif
            foreach (var interfaceType in interfaces)
            {
                Type genericType = GetGenericListType(interfaceType);
                if (genericType != null)
                    return genericType;
            }

            return null;
        }

        private static object GetSampleDataForType(Type type)
        {
            // String
            if (type == typeof(string))
                return GetRandomString(2, 5);

            // Int
            if (type == typeof(int))
                return GetRandomInt(0, 100);

            // Bool
            if (type == typeof(bool))
                return GetRandomBool();

            // TimeSpan
            if (type == typeof(TimeSpan))
                return TimeSpan.FromSeconds(GetRandomInt(0, 7200));

            // SampleDataBase
#if NETFX_CORE
            if (typeof(SampleDataBase).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
#else
            if (typeof(SampleDataBase).IsAssignableFrom(type))
#endif
                return Activator.CreateInstance(type);

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
#endif
}
