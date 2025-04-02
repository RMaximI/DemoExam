using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Npgsql;

namespace BetterDemoExam
{
	public class DbConnectionManager
	{
		public static NpgsqlConnection Connection = new NpgsqlConnection(
            "Host=localhost;Port=5432;Database=DemoExamen;Username=postgres;Password=1234"
        );

		public static void Initialize()
		{
			Connection.Open();
		}

		public static NpgsqlCommand Command(string query)
		{
			return new NpgsqlCommand(query, DbConnectionManager.Connection);
		}

		public static NpgsqlDataAdapter DataAdapter(string query)
		{
			return new NpgsqlDataAdapter(query, DbConnectionManager.Connection);
		} 
	}
}
