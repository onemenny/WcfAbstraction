using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace WcfAbstraction.Validation
{
	/// <summary>
	/// Method arguments validator
	/// </summary>
	public static class ArgumentValidator
    {
        #region String

        /// <summary>
		/// Checks a string argument to ensure it isn't null or empty. Throws <see cref="ArgumentException"/>
		/// exception if <c>argumentValue</c> is null or empty string
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
		/// <exception cref="ArgumentNullException">When <paramref name="argumentValue"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="argumentValue"/> is empty string</exception>
		public static void NotNullOrEmptyString(string argumentValue, string argumentName)
		{
			NotNull(argumentValue, argumentName);

            if (argumentValue.Length == 0)
            {
                throw new ArgumentException("String cannot be empty", argumentName);
            }
        }

        /// <summary>
        /// Checks a string for length validation
        /// </summary>
        /// <param name="value">String to check</param>
        /// <param name="acceptNull">Value accepts null</param>
        /// <param name="minLength">String minimum length</param>
        /// <param name="maxLength">String maximum length</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentException">Thrown if the value does not fall within method arguments</exception>
        public static void StringLength(string value, bool acceptNull, int minLength, int maxLength, string argumentName)
        {
            StringLengthHelper(value, acceptNull, argumentName);

            if (value == null && acceptNull)
            {
                return;
            }

            if (value.Length < minLength || value.Length > maxLength)
            {
                throw new ArgumentException(
                    string.Format(
                        "String value length not in range (min: {0}, max: {1})", 
                        minLength, 
                        maxLength), 
                    argumentName);
            }
        }

        /// <summary>
        /// Checks a string for length validation
        /// </summary>
        /// <param name="value">String to check</param>
        /// <param name="acceptNull">Value accepts null</param>
        /// <param name="minLength">String minimum length</param>
        /// <param name="argumentName">The name of the argument</param>
        public static void StringMinLength(string value, bool acceptNull, int minLength, string argumentName)
        {
            StringLengthHelper(value, acceptNull, argumentName);

            if (value == null && acceptNull)
            {
                return;
            }

            if (value.Length < minLength)
            {
                throw new ArgumentException(
                    string.Format("String value minimum length not in range (min: {0})", minLength), 
                    argumentName);
            }
        }

        /// <summary>
        /// Checks a string for length validation
        /// </summary>
        /// <param name="value">String to check</param>
        /// <param name="acceptNull">Value accepts null</param>
        /// <param name="maxLength">String maximum length</param>
        /// <param name="argumentName">The name of the argument</param>
        public static void StringMaxLength(string value, bool acceptNull, int maxLength, string argumentName)
        {
            StringLengthHelper(value, acceptNull, argumentName);

            if (value == null && acceptNull)
            {
                return;
            }

            if (value.Length > maxLength)
            {
                throw new ArgumentException(
                    string.Format("String value maximum length not in range (max: {0})", maxLength),
                    argumentName);
            }
        }

        /// <summary>
        /// Validate the nullable string
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="acceptNull">Value accepts null</param>
        /// <param name="argumentName">Value argument name</param>
        private static void StringLengthHelper(string value, bool acceptNull, string argumentName)
        {
            if (!acceptNull && value == null)
            {
                throw new ArgumentException("Value is null", argumentName);
            }
        }

        #endregion

        #region Null or Defined

        /// <summary>
		/// Checks an argument to ensure it isn't null. Throws an <see cref="ArgumentNullException"/>
		/// if <c>argumentValue</c> is null
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
		/// <exception cref="ArgumentNullException">When <paramref name="argumentValue"/> is null</exception>
		public static void NotNull(object argumentValue, string argumentName)
		{
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
		}

        /// <summary>
        /// Checks an Enum argument to ensure that its value is defined by the specified Enum type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="argumentName">The name of the argument holding the value.</param>
		/// <exception cref="ArgumentException">Enum value is not defined</exception>
        public static void EnumValueIsDefined<T>(T value, string argumentName)
        {
            EnumValueIsDefined(typeof(T), value, argumentName);
        }

        /// <summary>
        /// Checks an Enum argument to ensure that its value is defined by the specified Enum type. 
        /// </summary>
        /// <param name="enumType">The Enum type the value should correspond to.</param>
        /// <param name="value">The value to check for.</param>
        /// <param name="argumentName">The name of the argument holding the value.</param>
		/// <exception cref="ArgumentException">Enum value is not defined</exception>
		public static void EnumValueIsDefined(Type enumType, object value, string argumentName)
		{
            if (enumType.IsDefined(typeof(FlagsAttribute), false))
            {
                var values = Enum.GetValues(enumType).Cast<object>()
                    .Select(o => ToUInt64(o))
                    .Where(i => IsValidFlag(i));

                ulong ulongValue = ToUInt64(value);

                foreach (var i in values)
                {
                    if ((i & ulongValue) == i)
                    {
                        ulongValue -= i;
                        if (ulongValue == 0)
                        {
                            break;
                        }
                    }
                }

                if (ulongValue != 0)
                {
                    throw new ArgumentException("Invalid enum flags combination", argumentName);
                }
            }
            else if (Enum.IsDefined(enumType, value) == false)
            {
                throw new ArgumentException("Invalid enum value", argumentName);
            }
        }

        private static bool IsValidFlag(ulong value)
        {
            double log = Math.Log((double)value, 2);
            return log == Math.Truncate(log);
        }

        private static ulong ToUInt64(object value)
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture);
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Type

        /// <summary>
        /// Verifies that an argument type is assignable from the provided type (meaning
        /// interfaces are implemented, or classes exist in the base class hierarchy).
        /// </summary>
        /// <param name="assignee">Target of assignment.</param>
        /// <param name="providedType">The type it must be assignable from.</param>
        /// <param name="argumentName">The argument name.</param>
        /// <exception cref="ArgumentException">When <c>providedType</c> cannot be cast to <c>assignee</c> type</exception>
		public static void TypeIsAssignableFromType(Type assignee, Type providedType, string argumentName)
		{
            if (!assignee.IsAssignableFrom(providedType))
            {
                var formatedString = string.Format(
                        CultureInfo.CurrentCulture,
                        "Incompatible type (assignee: {0}, provider {1})",
                        assignee,
                        providedType);

                throw new ArgumentException(
                    formatedString, 
                    argumentName);
            }
        }

        #endregion

        #region Numbers

        /// <summary>
        /// Determines whether the specified value is numeric.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="argumentName">Argument name</param>
        public static void NumericFormat(string value, string argumentName)
        {
            int dummy;
            if (!int.TryParse(value, out dummy))
            {
                throw new ArgumentException("String value is not numeric", argumentName);
            }
        }

        /// <summary>
        /// Verifies that an value is between the range
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="minRange">The min range.</param>
        /// <param name="maxRange">The max range.</param>
        /// <param name="argumentName">Argument name</param>
        public static void NumberInRange(int value, int minRange, int maxRange, string argumentName)
        {
            if (value < minRange || value > maxRange)
            {
                var formatedStirng = string.Format("Int value not in range (min: {0}, max: {1}", minRange, maxRange);
                throw new ArgumentOutOfRangeException(formatedStirng, argumentName);
            }
        }

        /// <summary>
        /// Verifies that an value is bigger than the minimum value
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="minRange">The min range.</param>
        /// <param name="argumentName">Name of the argument.</param>
		/// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> less than <paramref name="minRange"/></exception>
        public static void NumberMinRange(int value, int minRange, string argumentName)
        {
            if (value < minRange)
            {
                var formatedString = string.Format("Int value not in minimum range (min: {0})", minRange);
                throw new ArgumentOutOfRangeException(formatedString, argumentName);
            }
        }

        /// <summary>
        /// Verifies that an value is smaller than the maximum value
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="maxRange">The max range.</param>
        /// <param name="argumentName">Maximum range value</param>
        public static void NumberMaxRange(int value, int maxRange, string argumentName)
        {
            if (value > maxRange)
            {
                var formatedString = string.Format(CultureInfo.CurrentCulture, "Int value not in maximum range (max: {0})", maxRange);
                throw new ArgumentOutOfRangeException(formatedString, argumentName);
            }
        }

        #endregion

        #region Date Time

        /// <summary>
        /// Determines whether the specified value is a date.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="argumentName">Maximum range value</param>
        public static void DateFormat(string value, string argumentName)
        {
            DateTime date;
            if (!DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date))
            {
                throw new ArgumentException("Invalid date", argumentName);
            }
        }

        /// <summary>
        /// Validate if the given dates estublish proper dates range.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="argumentName">Name of the argument.</param>
        public static void DateRange(DateTime startDate, DateTime endDate, string argumentName)
        {
            if (startDate >= endDate)
            {
                throw new ArgumentException("Start date is same or bigger than end date", argumentName);
            }
        }

        /// <summary>
        /// Validate if the given dates estublish proper dates range.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="argumentName">Name of the argument.</param>
        public static void DateRange(DateTime? startDate, DateTime? endDate, string argumentName)
        {
            DateRange(startDate.Value, endDate.Value, argumentName);
        }

        #endregion

        #region Boolean

        /// <summary>
        /// Throw an ArgumentException when some general condition doesn't hold.
        /// </summary>
        /// <param name="value">Condition to evaluate</param>
        /// <param name="description">Description, which will appear in the exception</param>
        public static void IsTrue(bool value, string description)
        {
            if (!value)
            {
                throw new ArgumentException(description);
            }
        }

        #endregion

        #region RegExp

        /// <summary>
        /// Determines whether the value conforms to the given regular expression pattern.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="regexPattern">The regex pattern.</param>
        /// <returns><c>true</c> on success otherwise <c>false</c></returns>
        public static bool ValidFormat(string value, string regexPattern)
        {
            return Regex.IsMatch(value, regexPattern);
        }

        #endregion
    }
}
