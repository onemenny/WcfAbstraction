using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WcfAbstraction.Reflection
{
    /// <summary>
    /// PRovides type extentions and helper methods
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Creates the type of the instance of generic.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="type">The type.</param>
        /// <param name="genericType">Type of the generic.</param>
        /// <returns></returns>
        public static T CreateInstanceOfGenericType<T>(this Type type, Type genericType)
        {
            Type constructed = type.MakeGenericType(genericType);

            return (T)Activator.CreateInstance(constructed);
        }

        /// <summary>
        /// Try case the specified object to the requested type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="castObject">The cast object.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool TryCast<T>(this object castObject, out T value)
        {
            bool ret = false;
            value = default(T);

            if (castObject != null)
            {
                Type castType = castObject.GetType();
                Type castValueType = typeof(T);

                if (castValueType.IsAssignableFrom(castType))
                {
                    value = (T)castObject;
                    ret = true;
                }
            }

            return ret;
        }

        /// <summary>
        /// Tries the perform an action for a given type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="castObject">The cast object.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static bool TryPerform<T>(this object castObject, Action<T> action)
        {
            T cast;
            if (TryCast<T>(castObject, out cast))
            {
                action(cast);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Return the name of the properties, primitive types and other according to lambda 
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="target">The target.</param>
        /// <param name="propertyExpression">The property expression.</param>
        /// <returns></returns>
        public static string NameOf<T>(this T target, Expression<Func<T, object>> propertyExpression)
        {
            MemberExpression body = null;
            if (propertyExpression.Body is UnaryExpression)
            {
                var unary = propertyExpression.Body as UnaryExpression;
                if (unary.Operand is MemberExpression)
                {
                    body = unary.Operand as MemberExpression;
                }
            }
            else if (propertyExpression.Body is MemberExpression)
            {
                body = propertyExpression.Body as MemberExpression;
            }

            if (body == null)
            {
                throw new ArgumentException("'propertyExpression' should be a member expression");
            }

            // Extract the right part (after "=>")
            var expression = body.Expression as ConstantExpression;

            // Extract the name of the property to raise a change on
            return body.Member.Name;
        }

        /// <summary>
        /// Gets the implemented <see cref="Type"/> of the given the interface type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns></returns>
        public static Type GetGenericInterface(this Type type, Type interfaceType)
        {
            if (!interfaceType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("The interface type must be a generic type definition.", "interfaceType");
            }

            return type.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType).FirstOrDefault();
        }

        /// <summary>
        /// Maps a list of named properties to type, returning subset of properties that exist in given
        /// type and their values converted to required type when possible.
        /// </summary>
        /// <param name="type">Type to map properties to</param>
        /// <param name="props">Dictionary of property names and values to map</param>
        /// <returns>New dictionaty with a subset of properties mapped to given type</returns>
        /// <remarks>
        /// Passing <c>null</c> as <paramref name="props"/> parameter, returns an empty dictionary.
        /// <para>
        /// Returned dictionary contains all properties that are found on specified type. Note, only
        /// public instance fields and properties are inspected. Read-only properties are not returned either
        /// </para>
        /// </remarks>
        public static Dictionary<string, object> MapProperties(this Type type, IDictionary<string, object> props)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (props == null)
            {
                return ret;
            }

            foreach (string key in props.Keys)
            {
                string propertyName = key;

                //check if property with such name exists in target type
                Type memberType = null;
                PropertyInfo propertyInfo = type.GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    memberType = propertyInfo.PropertyType;
                }

                //if no such property, we check for public field
                if (memberType == null)
                {
                    FieldInfo fieldInfo = type.GetField(key, BindingFlags.Instance | BindingFlags.Public);
                    if (fieldInfo != null)
                    {
                        memberType = fieldInfo.FieldType;
                    }
                }

                //if we couldn't find exact match, maybe try case-insensitive?
                if (memberType == null)
                {
                    propertyInfo = type.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        memberType = propertyInfo.PropertyType;
                        propertyName = propertyInfo.Name;
                    }

                    //if no such property, we check for public field
                    if (memberType == null)
                    {
                        FieldInfo fieldInfo = type.GetField(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (fieldInfo != null)
                        {
                            memberType = fieldInfo.FieldType;
                            propertyName = fieldInfo.Name;
                        }
                    }
                }

                //finally we try to convert from string to target type if possible
                if (memberType != null && props[key] != null)
                {
                    //if this is a nullable type, we should get its underlying type instead
                    memberType = Nullable.GetUnderlyingType(memberType) ?? memberType;

                    object newValue;
                    if (memberType.IsAssignableFrom(props[key].GetType()))
                    {
                        newValue = props[key];
                    }
                    else if (memberType.IsEnum && props[key] is string)
                    {
                        newValue = Enum.Parse(memberType, props[key].ToString(), true);
                    }
                    else
                    {
                        newValue = Convert.ChangeType(props[key], memberType);
                    }

                    ret.Add(propertyName, newValue);
                }
            }

            return ret;
        }

        /// <summary>
        /// Sets mapped properties of specified type instance
        /// </summary>
        /// <param name="type">Type to map properties to</param>
        /// <param name="instance">Object instance to set properties on</param>
        /// <param name="props">Dictionary of properties to set</param>
        /// <seealso cref="MapProperties"/>
        public static void SetProperties(this Type type, object instance, IDictionary<string, object> props)
        {
            Dictionary<string, object> mappedProps = type.MapProperties(props);
            foreach (var key in mappedProps.Keys)
            {
                type.InvokeMember(
                    key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.SetField,
                    null,
                    instance,
                    new object[] { mappedProps[key] });
            }
        }

        /// <summary>
        /// Returns assembly-qualified name of class, excluding assembly version and public key token
        /// </summary>
        /// <param name="type">Type to get name for</param>
        /// <returns>Assembly-qualified name of class or <c>null</c> if <b>type</b> was null.</returns>
        /// <remarks>
        /// <see cref="Type.AssemblyQualifiedName"/> always returns version and public key token
        /// of the assembly, which is not needed in most cases. This method will return full class name
        /// and its assembly name only.
        /// </remarks>
        public static string GetAssemblyQualifiedName(this Type type)
        {
            if (type == null)
            {
                return null;
            }

            return String.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
        }

        /// <summary>
        /// Returns display name of class. See Remarks for more information.
        /// </summary>
        /// <param name="type">Type to return display name for</param>
        /// <returns>Display name of class</returns>
        /// <remarks>
        /// Method will find display name using following rules:
        /// <list type="bullet">
        ///		<item><description>
        ///			If <c>null</c> is passed, returned value is "(null)".
        ///		</description></item>
        ///		<item><description>
        ///			If <see cref="DisplayNameAttribute"/> is specified, value of its <c>DisplayName</c> property is returned
        ///		</description></item>
        ///		<item><description>
        ///			If <see cref="DescriptionAttribute"/> is specified, value of its <c>Description</c> property is returned
        ///		</description></item>
        ///		<item><description>
        ///			If everything above fails, class name (excluding namespace) is returned
        ///		</description></item>
        /// </list>
        /// </remarks>
        public static string GetDisplayName(this Type type)
        {
            if (type == null)
            {
                return "(null)";
            }

            object[] attributes;

            attributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            if (attributes.Length > 0)
            {
                return ((DisplayNameAttribute)attributes[0]).DisplayName;
            }

            attributes = type.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return ((DescriptionAttribute)attributes[0]).Description;
            }

            return type.Name;
        }

        /// <summary>
        /// Determines whether the type to check is subclass of the generic type.
        /// <para>
        /// 		<example>
        /// example: boll res = myType.IsSubclassOfRawGeneric(typoeof(Proxy&lt;&gt;))
        /// </example>
        /// 	</para>
        /// </summary>
        /// <param name="toCheck">To type to check is its a subclass of generic type.</param>
        /// <param name="generic">The generic type.</param>
        /// <returns>
        /// 	<c>true</c> if [is subclass of raw generic] [the specified to check]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }
    }
}
