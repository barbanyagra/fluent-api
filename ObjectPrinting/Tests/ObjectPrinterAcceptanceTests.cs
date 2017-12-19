using System;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;

namespace ObjectPrinting.Tests
{
	[TestFixture]
	public class ObjectPrinterShould
	{
		private Person person;

		[SetUp]
		public void SetUp()
		{
			person = new Person { Id = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), Name = "Alex", Age = 19, Height = 179};
		}
		
		[Test]
		public void Demo()
		{
			var printer = ObjectPrinter.For<Person>()
				//1. Исключить из сериализации свойства определенного типа
				.ExcludeType<Guid>()
				//2. Указать альтернативный способ сериализации для определенного типа
				.ConfigureType<Guid>()
					.SetSerializer(config => config.ToString())
				//3. Для числовых типов (int, double, long) указать культуру
				.ConfigureType<int>()
					.SetSerializer(i => "")
				.ConfigureType<int>()
					.SetCulture(CultureInfo.CurrentUICulture)
				//4. Настроить сериализацию конкретного свойства
				.ConfigureProperty(obj => obj.Name)
					.SetSerializer(e => e.ToString())
				//5. Настроить обрезание строковых свойств 
				.ConfigureType<string>()
					.ShrinkToLength(10)
			    //   (метод должен быть виден только для строковых свойств)
				//6. Исключить из сериализации конкретного свойства
				.ExcludeProperty(obj => obj.Name);
            
            string s1 = printer.PrintToString(person);

			//7. Синтаксический сахар в виде метода расширения, сериализующего по-умолчанию		
			string s2 = person.PrintToString();
			//8. ...с конфигурированием
			string s3 = person.PrintToString(o =>
				o.ConfigureType<int>().SetCulture(CultureInfo.CurrentCulture));
		}

		[Test]
		public void ExcludeTypes()
		{
			var result = person.PrintToString(o => o.ExcludeType<string>());
			result.Should().NotContain("Name");
			result.Should().NotContain("Alex");
			result.Should().Contain("Age");
			result.Should().Contain("19");
		}

		[Test]
		public void ExcludeFields()
		{
			var result = person.PrintToString(o => o.ExcludeProperty(x => x.Age));
			result.Should().NotContain("Age");
			result.Should().NotContain("19");
			result.Should().Contain("Name");
			result.Should().Contain("Alex");
		}

		[Test]
		public void ExcludeBothTypesAndFields()
		{
			var result = person.PrintToString(o => o.ExcludeProperty(x => x.Age).ExcludeType<string>());
			result.Should().NotContain("Age");
			result.Should().NotContain("19");
			result.Should().NotContain("Name");
			result.Should().NotContain("Alex");
			result.Should().Contain("Height");
			result.Should().Contain("179");
		}

		[Test]
		public void SupportCustomFieldSerializers()
		{
			var result = person.PrintToString(o => o.ConfigureProperty(x => x.Age).SetSerializer(x => $"KE{x % 10}EK"));
			result.Should().NotContain("19");
			result.Should().Contain("Age");
			result.Should().Contain("KE9EK");
		}

		[Test]
		public void SupportCustomTypeSerializers()
		{
			var result = person.PrintToString(o => o.ConfigureType<string>().SetSerializer(x => $"KE{x.Truncate(2)}EK"));
			result.Should().NotContain("Alex");
			result.Should().Contain("Name");
			result.Should().Contain("KEAlEK");
		}

		[Test]
		public void SupportConfiguration()
		{
			var result = person.PrintToString(o => o.ConfigureType<string>().ShrinkToLength(2));
			result.Should().NotContain("Alex");
			result.Should().Contain("Al");
			result.Should().Contain("Name");
		}
		

		[Test]
		public void BeImmutable()
		{
			var printer = ObjectPrinter.For<Person>();

			var kekPrinter = printer.ConfigureType<string>().SetSerializer(_ => "KEK");
			var lolPrinter = printer.ConfigureType<string>().SetSerializer(_ => "LOL");

			kekPrinter.PrintToString(person).Should().Contain("KEK").And.NotContain("LOL");
			lolPrinter.PrintToString(person).Should().Contain("LOL").And.NotContain("KEK");

		}
		
	}
}