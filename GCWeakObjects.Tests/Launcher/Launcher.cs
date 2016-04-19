using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Used to execute the test methods found on the available test classes.
	/// <para>
	/// If any test class is decorated with the <see cref="OnlyThisClassAttribute"/> attribute,
	/// then only those classes with this attribute are considered. If any test method is then
	/// decorated with the <see cref="OnlyThisMethodAttribute"/> attribute, only those methods
	/// are executed.</para>
	/// </summary>
	public class Launcher
	{
		/// <summary>
		/// Executes the test methods found on the available test classes on the calling
		/// assembly.
		/// <para>
		/// If any test class is decorated with the <see cref="OnlyThisClassAttribute"/> attribute,
		/// then only those classes with this attribute are considered. If any test method is then
		/// decorated with the <see cref="OnlyThisMethodAttribute"/> attribute, only those methods
		/// are executed.</para>
		/// </summary>
		public static void Execute()
		{
			var asm = Assembly.GetCallingAssembly();
			Execute(asm);
		}

		/// <summary>
		/// Executes the test methods found on the available test classes on the given assembly.
		/// <para>
		/// If any test class is decorated with the <see cref="OnlyThisClassAttribute"/> attribute,
		/// then only those classes with this attribute are considered. If any test method is then
		/// decorated with the <see cref="OnlyThisMethodAttribute"/> attribute, only those methods
		/// are executed.</para>
		/// </summary>
		/// <param name="asm"></param>
		public static void Execute(Assembly asm)
		{
			var types = FindTestClasses(asm);
			var dict = FindTestMethods(types);

			try
			{
				foreach (var kvp in dict)
				{
					var type = kvp.Key;
					var con = type.GetConstructor(Type.EmptyTypes);
					if (con == null) throw new NotFoundException(
						"No parameterless constructor found for '{0}'.".FormatWith(type.EasyName()));

					var obj = con.Invoke(null);
					if (obj == null) throw new CannotCreateException(
						"Cannot create an instance of the test class '{0}'."
						.FormatWith(type.EasyName()));

					foreach (var method in kvp.Value)
					{
						ConsoleEx.WriteLine("\n\n**********");
						ConsoleEx.WriteLine("Test: {0}", method.EasyName(chain: true));
						ConsoleEx.WriteLine("**********");

						if (Console.KeyAvailable)
						{
							var info = Console.ReadKey();
							if (info.Key == ConsoleKey.I) ConsoleEx.Interactive = true;
						}
						if (ConsoleEx.Interactive)
						{
							var s = ConsoleEx.ReadLine("Press [Enter] to execute... ");
							if (s.ToUpper() == "N") ConsoleEx.Interactive = false;
						}

						method.Invoke(obj, null);
					}

					if (obj is IDisposable) ((IDisposable)obj).Dispose();
				}
			}
			catch (Exception e)
			{
				var inner = e is TargetInvocationException && e.InnerException != null;
				if (inner) e = e.InnerException;
				e.ToConsoleEx("\n----- Exception: {0}");

				if (inner) throw e;
				throw;
			}
		}

		/// <summary>
		/// Returns the collection of available test methods ordered by the type of the
		/// test classes that contain them.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		static Dictionary<Type, List<MethodInfo>> FindTestMethods(List<Type> types)
		{
			var dict = new Dictionary<Type, List<MethodInfo>>();
			foreach (var type in types)
			{
				var methods = type.GetMethods()
					.Where(x => x.GetCustomAttributes<TestMethodAttribute>().Count() != 0)
					.ToList();

				if (methods.Count != 0) dict.Add(type, methods);
			}

			var temp = new Dictionary<Type, List<MethodInfo>>();
			foreach (var kvp in dict)
			{
				var list = kvp.Value
					.Where(x => x.GetCustomAttributes<OnlyThisMethodAttribute>().Count() != 0)
					.ToList();

				if (list.Count != 0) temp.Add(kvp.Key, list);
			}

			if (temp.Count != 0) dict = temp;
			return dict;
		}

		/// <summary>
		/// Returns a list with the available test classes.
		/// </summary>
		/// <param name="asm"></param>
		/// <returns></returns>
		static List<Type> FindTestClasses(Assembly asm)
		{
			var types = asm.GetTypes()
				.Where(x => x.GetCustomAttributes<TestClassAttribute>().Count() != 0)
				.ToList();

			var temp = types
				.Where(x => x.GetCustomAttributes<OnlyThisClassAttribute>().Count() != 0)
				.ToList();

			if (temp.Count != 0) types = temp;
			return types;
		}
	}
}
