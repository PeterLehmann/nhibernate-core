using System.Collections;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.Pagination
{
	[TestFixture]
	public class CustomDialectFixture : TestCase
	{
		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}

		protected override IList Mappings
		{
			get { return new[] {"Pagination.DataPoint.hbm.xml"}; }
		}

		protected override void Configure(Configuration configuration)
		{
			if (!(Dialect is Dialect.MsSql2005Dialect))
				Assert.Ignore("Test is for SQL dialect only");

			cfg.SetProperty(Environment.Dialect, typeof(CustomMsSqlDialect).AssemblyQualifiedName);
			cfg.SetProperty(Environment.ConnectionDriver, typeof(CustomMsSqlDriver).AssemblyQualifiedName);
		}

		private CustomMsSqlDialect CustomDialect
		{
			get { return (CustomMsSqlDialect)Sfi.Dialect; }
		}

		private CustomMsSqlDriver CustomDriver
		{
			get { return (CustomMsSqlDriver)Sfi.ConnectionProvider.Driver; }
		}

		protected override void OnSetUp()
		{
			base.OnSetUp();

			CustomDriver.CustomMsSqlDialect = CustomDialect;

			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				s.Save(new DataPoint() { X = 5 });
				s.Save(new DataPoint() { X = 6 });
				s.Save(new DataPoint() { X = 7 });
				s.Save(new DataPoint() { X = 8 });
				s.Save(new DataPoint() { X = 9 });
				t.Commit();
			}
		}

		protected override void OnTearDown()
		{

			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				s.Delete("from DataPoint");
				t.Commit();
			}
			base.OnTearDown();
		}

		[Test]
		public void LimitFirst()
		{
			using (ISession s = OpenSession())
			{
				CustomDialect.ForcedSupportsVariableLimit = true;
				CustomDialect.ForcedBindLimitParameterFirst = true;

				var points =
					s.CreateCriteria<DataPoint>()
						.Add(Restrictions.Gt("X", 5.1d))
						.AddOrder(Order.Asc("X"))
						.SetFirstResult(1)
						.SetMaxResults(2)
						.List<DataPoint>();

				Assert.That(points.Count, Is.EqualTo(2));
				Assert.That(points[0].X, Is.EqualTo(7d));
				Assert.That(points[1].X, Is.EqualTo(8d));
			}
		}

		[Test]
		public void LimitFirstMultiCriteria()
		{
			using (ISession s = OpenSession())
			{
				CustomDialect.ForcedSupportsVariableLimit = true;
				CustomDialect.ForcedBindLimitParameterFirst = true;

				var criteria =
					s.CreateMultiCriteria()
						.Add<DataPoint>(
							s.CreateCriteria<DataPoint>()
								.Add(Restrictions.Gt("X", 5.1d))
								.AddOrder(Order.Asc("X"))
								.SetFirstResult(1)
								.SetMaxResults(2));

				var points = (IList<DataPoint>)criteria.List()[0];

				Assert.That(points.Count, Is.EqualTo(2));
				Assert.That(points[0].X, Is.EqualTo(7d));
				Assert.That(points[1].X, Is.EqualTo(8d));
			}
		}
	}
}