using Microsoft.VisualBasic.FileIO;
using MySqlConnector;
using System.Data.Common;
using System.Data;

public static partial class Program
{
	public static void Optiona()
	{
		string? option;

		Console.Clear();
		Console.WriteLine("""
		Selected option was A

		Prerequisites:
		1 - A CSV file named optionain.csv with 3 columns [mat,des,sku] at least
		2 - A TXT file named optionacnn.txt with the connection string to the MySQL database of an All Retail POS installation
		3 - All prerequisite files are stored next to this turitoys app

		Results:
		1 - A csv file named optionaout.csv.txt containing the result of the processing
		2 - A log file named optionalog.txt
		3 - All result files have the current date and time as preffix

		Press Y to compare and update
		Press V to just compare
		Press any other or empty to close
		""");
		option = Console.ReadLine();
		option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

		if (option != "y" && option != "v") { return; }

		string sessionID = DateTime.Now.ToString("yyyyMMddHHmmss");
		string logFileFullPath = sessionID + "_optionalog.txt";
		StreamWriter logWriter = File.CreateText(logFileFullPath);
		string message = "";

		logWriter.WriteLine("Option selected: " + option);

		message = "Reading CSV file, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine("\n" + message);

		DataTable csvData = new();

		try
		{
			using TextFieldParser csvReader = new("optionain.csv");
			csvReader.TextFieldType = FieldType.Delimited;
			csvReader.SetDelimiters([","]);
			csvReader.HasFieldsEnclosedInQuotes = true;
			string[]? colFields;
			bool tableCreated = false;
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
				message = "CSV: " + csvData.Rows.Count + " materials were found";
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


		message = "\nGetting all materials from DB to process locally, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string? cnnString = "";
		MySqlConnection cnn;

		try
		{
			using StreamReader read = new("optionacnn.txt");
			cnnString = read.ReadLine();
			message = String.IsNullOrEmpty(cnnString) ? "" : cnnString;
			read.Close();
		}
		catch (Exception e)
		{
			message = "MySQL: Error during reading optionacnn.txt file - " + e.Message;
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

		// SKU always must be first element, check linq.first statement when verifying
		message = "select itemcode as sku, itemname as des, s4h_id as mat from oitm;";
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
				item = [.. item, (string)(readerAll[col.ColumnName] == DBNull.Value ? "" : readerAll[col.ColumnName])];
			}
			resultAll.Add(item);
		}
		readerAll.Close();

		if (resultAll.Count == 0)
		{
			message = "MySQL: No materials found";
			logWriter.WriteLine(message);
			logWriter.Dispose();
			cnn.Close();
			Console.WriteLine(message);
			return;
		}
		else
		{
			message = "MySQL: " + resultAll.Count + " materials found";
			logWriter.WriteLine(message);
			Console.WriteLine(message);
		}


		message = "\nProcessing materials in CSV against DB, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string outFileFullPath = sessionID + "_optionaout.csv.txt";
		StreamWriter outWriter = File.CreateText(outFileFullPath);
		int notFoundItems = 0;
		int noCodeItems = 0;
		int diffCodeItems = 0;
		string? csvMaterialSKU = "";
		string? csvMaterialS4H = "";
		string? csvMaterialDES = "";
		IEnumerable<string[]> query;
		bool updateMaterial = false;
		int csvDataRowsCount = csvData.Rows.Count;
		List<string[]> dbMaterial;
		int updateResult = -1;
		string sqlUpdateMaterial = "update oitm set s4h_id='{0}' where itemcode='{1}';";

		outWriter.WriteLine("sku,mat,des,statusdb,result,prevcode");

		foreach (DataRow mat in csvData.Rows)
		{
			csvMaterialSKU = String.IsNullOrEmpty(mat["sku"].ToString()) ? "" : mat["sku"].ToString();
			csvMaterialS4H = String.IsNullOrEmpty(mat["mat"].ToString()) ? "" : mat["mat"].ToString();
			csvMaterialDES = String.IsNullOrEmpty(mat["des"].ToString()) ? "" : mat["des"].ToString();
			query = resultAll.Where(arr => arr.First() == csvMaterialSKU);

			if (query.Count() == 0)
			{
				notFoundItems++;

				message = csvMaterialSKU + " not found";
				logWriter.WriteLine(message);
				outWriter.WriteLine("\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\",not found," + "ignored" + "," + "");
			}
			else
			{
				updateMaterial = (option == "y");
				dbMaterial = query.ToList();
				if (String.IsNullOrEmpty(dbMaterial[0][2]))
				{
					noCodeItems++;

					logWriter.WriteLine("The material SKU " + csvMaterialSKU + " has no material code");
					message = "\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\",no code," + (updateMaterial ? "{0}" : "compared") + "," + "";
				}
				else if (dbMaterial[0][2] != csvMaterialS4H)
				{
					diffCodeItems++;

					logWriter.WriteLine("The material SKU " + csvMaterialSKU + " has a different material code(DB vs CSV): " + dbMaterial[0][2] + " <-> " + csvMaterialS4H);
					message = "\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\",diff code," + (updateMaterial ? "{0}" : "compared") + "," + dbMaterial[0][2];
				}
				else
				{
					logWriter.WriteLine("The material SKU " + csvMaterialSKU + " has the same material code(DB vs CSV)");
					message = "\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\",ok," + "ignored" + "," + "";
				}


				if (updateMaterial)
				{
					command = new MySqlCommand(string.Format(sqlUpdateMaterial, csvMaterialS4H, csvMaterialSKU), cnn);
					updateResult = command.ExecuteNonQuery();
					if (updateResult > 0)
					{
						logWriter.WriteLine("The material with SKU " + csvMaterialSKU + " was updated");
						message = string.Format(message, "updated");
					}
					else
					{
						logWriter.WriteLine("The material with SKU " + csvMaterialSKU + " could not be updated");
						message = string.Format(message, "error updating");
					}
					
				}

				updateMaterial = false;
				outWriter.WriteLine(message);
			}
		}
		cnn.Close();
		outWriter.Dispose();
		resultAll.TrimExcess();
		csvData.Dispose();

		if (csvDataRowsCount - notFoundItems - noCodeItems - diffCodeItems == csvDataRowsCount)
		{
			message = "\nAll materials in CSV have the same code in DB";
			logWriter.WriteLine(message);
			Console.WriteLine(message);
		}
		else
		{
			Console.WriteLine();
			logWriter.WriteLine();

			message = notFoundItems > 0 ? notFoundItems + " materials were not found in DB of " + csvDataRowsCount + " in CSV" : "All materials were found in DB";
			logWriter.WriteLine(message);
			Console.WriteLine(message);

			message = noCodeItems + " materials of " + (csvDataRowsCount - notFoundItems) + " found have no code in DB";
			logWriter.WriteLine(message);
			Console.WriteLine(message);

			message = diffCodeItems + " materials of " + (csvDataRowsCount - notFoundItems - noCodeItems) + " with code have a different code in DB";
			logWriter.WriteLine(message);
			Console.WriteLine(message);
		}
		logWriter.Dispose();

		return;
	}
}