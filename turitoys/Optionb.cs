using Microsoft.VisualBasic.FileIO;
using MySqlConnector;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;

public static partial class Program
{
	public static void Optionb()
	{
		string? option;

		Console.Clear();
		Console.WriteLine("""
		Selected option was B

		Prerequisites:
		1 - A CSV file named optionbin.csv exported from s4h in simple format containing only the following columns
		1.1 - ConditionTable
		1.2 - Material
		1.3 - ConditionRecord
		1.4 - ConditionValidityStartDate
		1.5 - ConditionValidityEndDate
		1.6 - ConditionRateValueText
		2 - A text file named optionacnn.txt with the connection string to the MySQL database of an All Retail POS installation
		3 - All prerequisite files are stored next to this turitoys app

		Results:
		1 - A csv file named optionaout.csv.txt containing the result of the processing
		2 - A log file named optionalog.txt
		3 - All result files have the current date and time as preffix

		Press Y to compare
		Press any other or empty to close
		""");
		option = Console.ReadLine();
		option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

		if (option != "y") { return; }

		string sessionID = DateTime.Now.ToString("yyyyMMddHHmmss");
		string logFileFullPath = sessionID + "_optionblog.txt";
		StreamWriter logWriter = File.CreateText(logFileFullPath);
		string message = "";

		logWriter.WriteLine("Option selected: " + option);

		message = "Reading CSV file, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine("\n" + message);

		DataTable csvData = new();

		try
		{
			using TextFieldParser csvReader = new("optionbin.csv");
			csvReader.TextFieldType = FieldType.Delimited;
			csvReader.SetDelimiters([","]);
			csvReader.HasFieldsEnclosedInQuotes = true;
			string[]? colFields;
			bool tableCreated = false;
			// Avoid the first line that is the description of the field
			csvReader.ReadFields();
			colFields = csvReader.ReadFields();

			if (colFields != null)
			{
				while (tableCreated == false)
				{
					foreach (string column in colFields)
					{
						DataColumn datecolumn = new(column)
						{
							AllowDBNull = true
						};
						csvData.Columns.Add(datecolumn);
					}
					tableCreated = true;
				}
				while (!csvReader.EndOfData)
				{
					string[]? itemFields;
					itemFields = csvReader.ReadFields();
					if (itemFields != null)
					{
						csvData.Rows.Add(itemFields);
					}
				}
				message = "CSV: " + csvData.Rows.Count + " material prices were found";

				logWriter.WriteLine(message);
				Console.WriteLine(message);
			}
			else
			{
				message = "CSV: No columns info in first row";
				logWriter.WriteLine(message);
				logWriter.Dispose();
				Console.WriteLine(message);
				return;
			}
		}
		catch (Exception e)
		{
			message = "CSV: Error during reading - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}

		
		message = "\nGetting only unique combinations in CSV from DB to process locally, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string combinations = "";
		bool first = true;
		string? cnnString = "";
		MySqlConnection cnn;

		var result = csvData.AsEnumerable()
			.Select(row => new
			{
				ConditionTable = row.Field<string>("ConditionTable")
			}).Distinct().ToList();

		foreach (var item in result)
		{
			combinations += first ? item.ConditionTable : item.ConditionTable + ",";
			first = false;
		}
		logWriter.WriteLine("Unique ConditionTable: " + combinations);

		try
		{
			using StreamReader read = new("optionbcnn.txt");
			cnnString = read.ReadLine();
			message = String.IsNullOrEmpty(cnnString) ? "" : cnnString;
			read.Close();
		}
		catch (Exception e)
		{
			message = "MySQL: Error during reading optionbcnn.txt file - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}
		cnn = new(message);
		message = "Connection to use is: " + message;
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		try
		{
			cnn.OpenAsync().Wait();
		}
		catch (Exception e)
		{
			message = "MySQL: Error during connection - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}

		MySqlCommand command;
		MySqlDataReader readerAll;
		List<string[]> resultAll = [];
		IReadOnlyCollection<DbColumn> columns;

		message = "select o.S4H_Id, cast(pricelist as char) as pricelist, cast(truncate(price, 2) as char), DATE_FORMAT(fechainicio,'%Y%m%d') as fechainicio, DATE_FORMAT(fechafin,'%Y%m%d') as fechafin, conditionrecord\r\nfrom itm1 i\r\ninner join oitm o on o.ItemCode = i.ItemCode\r\nwhere pricelist in (" + combinations + ");";
		logWriter.WriteLine(message);
		command = new MySqlCommand(message, cnn);
		readerAll = command.ExecuteReader();
		resultAll = [];
		columns = readerAll.GetColumnSchema();

		while (readerAll.Read())
		{
			string[] item = [];
			foreach (DbColumn col in columns)
			{
				item = [.. item, (string)(readerAll[col.ColumnName] == DBNull.Value ? "" : readerAll[col.ColumnName].ToString())];
			}
			resultAll.Add(item);
		}
		readerAll.Close();

		if (resultAll.Count == 0)
		{
			message = "MySQL: No prices with combinations in CSV were found";
			logWriter.WriteLine(message);
			logWriter.Dispose();
			cnn.Close();
			Console.WriteLine(message);
			return;
		}
		else
		{
			message = "MySQL: " + resultAll.Count + " prices with combinations in CSV were found";
			logWriter.WriteLine(message);
			Console.WriteLine(message);
		}
		cnn.Close();


		message = "\nVeryfing prices in CSV against DB, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string outFileFullPath = sessionID + "_optionbout.csv.txt";
		StreamWriter outWriter = File.CreateText(outFileFullPath);
		IEnumerable<string[]> query;
		string? csvMaterialS4H = "";
		string? csvMaterialCOM = "";
		string? csvMaterialPRC = "";
		string? csvMaterialINI = "";
		string? csvMaterialFIN = "";
		string? csvMaterialCRD = "";
		int noCoincidence = 0;

		outWriter.WriteLine("s4hid,combination,price,fini,ffin,condrecord,result");

		foreach (DataRow mat in csvData.Rows)
		{
			csvMaterialS4H = String.IsNullOrEmpty(mat["Material"].ToString()) ? "" : mat["Material"].ToString();
			csvMaterialCOM = String.IsNullOrEmpty(mat["ConditionTable"].ToString()) ? "" : mat["ConditionTable"].ToString();
			csvMaterialPRC = String.IsNullOrEmpty(mat["ConditionRateValueText"].ToString()) ? "" : mat["ConditionRateValueText"].ToString();
			csvMaterialINI = String.IsNullOrEmpty(mat["ConditionValidityStartDate"].ToString()) ? "" : mat["ConditionValidityStartDate"].ToString();
			csvMaterialFIN = String.IsNullOrEmpty(mat["ConditionValidityEndDate"].ToString()) ? "" : mat["ConditionValidityEndDate"].ToString();
			csvMaterialCRD = String.IsNullOrEmpty(mat["ConditionRecord"].ToString()) ? "" : mat["ConditionRecord"].ToString();
			query = resultAll.Where(arr => arr[0] == csvMaterialS4H && arr[1] == csvMaterialCOM && arr[2] == csvMaterialPRC && arr[3] == csvMaterialINI && arr[4] == csvMaterialFIN );
			message = csvMaterialS4H + ", " + csvMaterialCOM + ", " + csvMaterialPRC + ", " + csvMaterialINI + ", " + csvMaterialFIN + ", \"" + csvMaterialCRD + "\"";
			if (query.Count() == 0)
			{
				outWriter.WriteLine(message + ",not found");
				message = "The combination |" + message + "| in CSV was not found";
				logWriter.WriteLine(message);
			}
			else
			{
				outWriter.WriteLine(message + ",found");
				message = "The combination |" + message + "| in CSV was found";
				logWriter.WriteLine(message);
			}

		}

		outWriter.Close();
		logWriter.Close();
	}
}